using AzProxy.Middleware;
using AzProxy.Requests;
using AzProxy.Services;
using AzProxy.Storage;
using AzProxy.Storage.AzureDB.Context;
using AzProxy.Storage.AzureDB.DataTransform;
using AzProxy.Storage.AzureTables;
using AzProxy.Storage.AzureTables.BanList;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AzProxy
{
    public class ProxyServer
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };



        public static void Main(string[] args)
        {
            var app = GetBuiltApp(args);

            // Apply any pending database migrations
            GetAndApplyMigrations(app);

            app.UseHttpsRedirection();
            app.UseCors("FromGitHubPages");
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<RequestValidator>();  

            app.MapGet("/", () => "Proxy is up.");
            app.MapGet("/secure-link", GenSasRequest);

            app.MapGet("/prune",
                [Authorize(Policy = "AdminOnly")]
                async (HttpContext context,
                    [FromServices] StorageManager storageManager,
                    [FromServices] ILogger<ProxyServer> logger,
                    [FromServices] IConfiguration config
                    ) =>
            {
                try { 
                    // Get and validate admin token from query
                    var query = context.Request.Query;
                    if (!query.TryGetValue("adminPass", out var passValue) || !Guid.TryParse(passValue, out var givenPass))
                    {
                        logger.LogWarning("Missing or invalid admin token.");
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync("Unauthorized.");
                        return;
                    }

                    if (givenPass != Guid.Parse(config["AdminPass"] ?? string.Empty))
                    {
                        logger.LogWarning("Unauthorized prune attempt detected.");
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync("Unauthorized.");
                        return;
                    }

                    // Get pruneDemos flag from query
                    bool pruneDemos = false;
                    if (query.TryGetValue("pruneDemos", out var demoValue))
                    {
                        if (bool.TryParse(demoValue, out bool parsedDemosFlag))
                            pruneDemos = parsedDemosFlag;
                    }

                    // Get forcePrune flag from query
                    bool forcedPrune = false;
                    if (query.TryGetValue("force", out var force))
                    {
                        if (bool.TryParse(force, out bool forced))
                            forcedPrune = forced;
                    }

                    AppVarEntry appVarLastPrune = storageManager.ShouldPruneDataBase(forcedPrune) 
                        ?? throw new InvalidOperationException("ShouldPrune method returned null even during manual prune flow!");
                    var prunedTime = await storageManager.PruneDataBase(pruneDemos, forcedPrune);
                    if (prunedTime != null) {
                        appVarLastPrune.Value = ((DateTime)prunedTime).ToString("o");
                        await storageManager.UpdateAppVarTableEntry(appVarLastPrune);
                   
                        logger.LogInformation("Prune successful; Demos : {demoflag}. Forced : {forcedPrune}.", pruneDemos, forcedPrune);
                        context.Response.StatusCode = StatusCodes.Status200OK;
                        await context.Response.WriteAsync("Prune completed.");
                    }
                    else
                    {
                        logger.LogInformation("Prune skipped or failed; Demos : {demoflag}. Forced : {forcedPrune}.", pruneDemos, forcedPrune);
                        context.Response.StatusCode = StatusCodes.Status202Accepted;
                        await context.Response.WriteAsync("Prune skipped or failed.");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred during the prune operation: {Message}", ex.Message);
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync("An unexpected server error occurred.");
                }
            });
            app.MapGet("/leaderboard", 
                async (
                    HttpContext context,
                    [FromServices] RequestHandler requestHandler,
                    [FromServices] StorageManager storageManager,
                    [FromServices] IHttpClientFactory httpClientFactory,
                    [FromServices] IConfiguration config,
                    [FromServices] ILogger<ProxyServer> logger) =>
                {
                    try
                    {

                    }
                    catch (Exception ex)
                    {

                    }
                });

            app.MapPost("/sync-stats",
                async (HttpContext context,
                    [FromServices] RequestHandler requestHandler,
                    [FromServices] DbTransformer transformer,
                    [FromServices] IHttpClientFactory httpClientFactory,
                    [FromServices] IConfiguration config,
                    [FromServices] ILogger<ProxyServer> logger) =>
                {
                    var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();

                    if (string.IsNullOrEmpty(requestBody))
                    {
                        logger.LogWarning("Sync request received without body.");
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Invalid request.");
                        return;
                    }

                    // Deserialize payload early. This lets us have data (like InstallID etc) for logging
                    GameSessionDto sessionData;
                    try
                    {
                        if (string.IsNullOrEmpty(requestBody))
                            throw new ArgumentException("Invalid RequestBody.");

                        sessionData = System.Text.Json.JsonSerializer.Deserialize<GameSessionDto>(requestBody, _jsonSerializerOptions) ?? throw new InvalidDataException("Failed to deserialize GameSession from json.");
                    }
                    catch (System.Text.Json.JsonException jsonEx)
                    {
                        logger.LogWarning("JSON deserialization error: {Message}", jsonEx.Message);
                        context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
                        await context.Response.WriteAsync("Unable to process the provided data.");
                        return;
                    }
                    catch (ArgumentException argEx)
                    {
                        logger.LogWarning("Bad request for sync: {Message}", argEx.Message);
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Invalid request data.");
                        return;
                    }

                    Guid installID = sessionData.InstallId;
                    try
                    {
                        int actualActionCount = sessionData.Attacks.Count + sessionData.Moves.Count + sessionData.Trades.Count;

                        await transformer.TransformFromSessionDto(sessionData);

                        context.Response.StatusCode = StatusCodes.Status200OK;
                        await context.Response.WriteAsync("Sync completed successfully!");
                    }
                    catch (ArgumentException argEx)
                    {
                        // Client sent bad data
                        logger.LogWarning("Bad request for sync from {installId}: {Message}", installID, argEx.Message);
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Invalid request data.");
                    }
                    catch (InvalidDataException dataEx)
                    {
                        // JSON deserialization failed or data integrity issues
                        logger.LogWarning("Invalid data in sync request from {installId}: {Message}", installID, dataEx.Message);
                        context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
                        await context.Response.WriteAsync("Unable to process the provided data.");
                    }
                    catch (DbUpdateException dbEx)
                    {
                        // Database constraint violations, connection issues
                        logger.LogError("Database error during sync for {installId}: {Message}", installID, dbEx.Message);
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        await context.Response.WriteAsync("Database error occurred.");
                    }
                    catch (PartialFailureException partialEx)
                    {
                        logger.LogWarning("Partial sync failure for {installID}: {failures}", installID, partialEx.Message);
                        context.Response.StatusCode = StatusCodes.Status207MultiStatus;
                        await context.Response.WriteAsync("Sync completed with warnings.");
                    }
                    catch (Exception ex)
                    {
                        // Unexpected errors
                        logger.LogError("Unexpected error during sync for {installId}: {Message}", installID, ex.Message);
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        await context.Response.WriteAsync("An unexpected error occurred.");
                    }

                });
            app.Run();
        }

        private static WebApplication GetBuiltApp(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.SetMinimumLevel(LogLevel.Information);
            builder.Configuration.AddEnvironmentVariables();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("FromGitHubPages", policy =>
                {
                    policy.WithOrigins("https://livingcryogen.github.io")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
            // Add custom Authentication Scheme for Admin access
            builder.Services.AddAuthentication("ApiKeyScheme")
                .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticator>("ApiKeyScheme", null);
            // Add Authorization policy for Admin role
            builder.Services.AddAuthorizationBuilder()
                .AddPolicy("AdminOnly", policy =>
                {
                    policy.RequireRole("Admin");
                });
            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<IBanCache, BanListCache>();
            builder.Services.AddHostedService<StorageManager>();
            builder.Services.AddSingleton<StorageManager>();
            builder.Services.AddSingleton<BanService>();
            builder.Services.AddSingleton<RequestHandler>();
            builder.Services.AddDbContext<GameStatsDbContext>(options => options.UseAzureSql(builder.Configuration.GetConnectionString("AzDbConnectionString")));
            builder.Services.AddScoped<DbTransformer>();
            builder.Services.AddLogging();
            return builder.Build();
        }

        private static async Task<IResult> GenSasRequest(HttpContext context, SASGenerator sasGenerator)
        {
            return await sasGenerator.GenerateAsync(context);
        }

        // Apply any pending database migrations
        private static void GetAndApplyMigrations(WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<ProxyServer>>();
                var dbContext = scope.ServiceProvider.GetRequiredService<GameStatsDbContext>();

                try
                {
                    var pendingMigrations = dbContext.Database.GetPendingMigrations();

                    if (pendingMigrations.Any())
                    {
                        logger.LogInformation("Applying {Count} pending migrations: {Migrations}",
                            pendingMigrations.Count(),
                            string.Join(", ", pendingMigrations));
                        dbContext.Database.Migrate();
                        logger.LogInformation("Database migrations applied successfully");
                    }
                    else
                    {
                        logger.LogInformation("Database is up to date - no migrations needed");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to check/apply database migrations: {Message}", ex.Message);
                    // Don't crash on first-time connection issues, but log clearly
                }
            }
        }
}
