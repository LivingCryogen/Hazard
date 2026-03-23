using HazardBackend.Middleware;
using HazardBackend.Requests;
using HazardBackend.Services;
using HazardBackend.Storage;
using HazardBackend.Storage.AzureDB;
using HazardBackend.Storage.AzureDB.Context;
using HazardBackend.Storage.AzureDB.DataTransform;
using HazardBackend.Storage.AzureDB.DataTransform.DTOs;
using HazardBackend.Storage.AzureDB.Services;
using HazardBackend.Storage.AzureTables;
using HazardBackend.Storage.AzureTables.BanList;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HazardBackend
{
    public class HazardBackend
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
        private static readonly ForwardedHeadersOptions _forwardedHeadersOptions = new() // This is needed to correctly capture client IPs when behind a reverse proxy or load balancer
        {
            ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
        };


        public static void Main(string[] args)
        {
            var app = GetBuiltApp(args);

            // Apply any pending database migrations
            GetAndApplyMigrations(app);

            app.UseForwardedHeaders(_forwardedHeadersOptions);
            app.UseHttpsRedirection();
            app.UseCors("FromGitHubPages");
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<RequestValidator>();


            app.MapGet("/", () => "Proxy is up.");
            app.MapGet("/secure-link", GenSasRequest);


            app.MapGet("/leaderboard", GetLeaderboard);

            app.MapPost("/sync-stats",
                async (HttpContext context,
                    [FromServices] RequestHandler requestHandler,
                    [FromServices] DbTransformer transformer,
                    [FromServices] IHttpClientFactory httpClientFactory,
                    [FromServices] IConfiguration config,
                    [FromServices] ILogger<HazardBackend> logger) =>
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
            app.MapPost("/prune", ManualPruneAzDB).RequireAuthorization("AdminOnly");
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
            builder.Services.AddScoped<Pruner>();
            builder.Services.AddSingleton<AzDBManager>();
            builder.Services.AddHostedService<StorageManager>();
            builder.Services.AddScoped<SASGenerator>();
            builder.Services.AddSingleton<BanService>();
            builder.Services.AddSingleton<RequestHandler>();
            builder.Services.AddDbContext<GameStatsDbContext>(options => options.UseAzureSql(builder.Configuration.GetConnectionString("AzDbConnectionString")));
            builder.Services.AddScoped<DbTransformer>();
            builder.Services.AddLogging();
            return builder.Build();
        }

        // Apply any pending database migrations
        private static void GetAndApplyMigrations(WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<HazardBackend>>();
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

        private static async Task<IResult> GenSasRequest(HttpContext context,
            [FromServices] ILogger<HazardBackend> logger,
            [FromServices] RequestHandler requestHandler,
            [FromServices] SASGenerator sasGenerator)
        {
            return await sasGenerator.GenerateAsync(context.Request);
        }

        private static async Task<IResult> GetLeaderboard(string? sortBy,
            [FromServices] AzDBManager dBManager)
        {
            if (string.IsNullOrEmpty(sortBy))
                sortBy = "wins"; // Default leaderboard type
            await dBManager.HandleClientQuery("leaderboard", sortBy);
            
            // parse response and return appropriate result
            
            //var topPlayers = await dbContext.PlayerStats
            //    .OrderByDescending(ps => ps.TotalScore)
            //    .Take(10)
            //    .Select(ps => new { ps.PlayerName, ps.TotalScore })
            //    .ToListAsync();
            //return Results.Ok(topPlayers);
        }

        [Authorize(Policy = "AdminOnly")]
        private static async Task<IResult> ManualPruneAzDB([AsParameters] PruneRequest pruneRequest, StorageManager storeManager)
        {
            if (await storeManager.TryDBPruneAsync(pruneRequest))
                return Results.Ok($"Manual prune completed, {(pruneRequest.PruneDemos ? "" : "NOT ")} including demo entities.");
            else
                return Results.Problem("Manual prune failed or skipped.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
