using AzProxy.BanList;
using AzProxy.Context;
using AzProxy.DataTransform;
using AzProxy.Middleware;
using AzProxy.Requests;
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

        private class FunctionResponse
        {
            public string Uri { get; init; } = string.Empty;
            public string SasToken { get; init; } = string.Empty;
        }

        public static void Main(string[] args)
        {
            var app = GetBuiltApp(args);

            // Apply any pending database migrations
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

            app.UseHttpsRedirection();
            app.UseCors("FromGitHubPages");
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<RequestValidator>();  

            app.MapGet("/", () => "Proxy is up.");
            app.MapGet("/secure-link",
                async (
                    HttpContext context,
                    [FromServices] RequestHandler requestHandler,
                    [FromServices] IHttpClientFactory httpClientFactory,
                    [FromServices] IConfiguration config,
                    [FromServices] ILogger<ProxyServer> logger) =>
                {
                    try
                    {
                        // get client IP
                        string? clientIP = context.Connection.RemoteIpAddress?.ToString();
                        if (string.IsNullOrEmpty(clientIP))
                        {
                            logger.LogWarning("Request received without IP address");
                            context.Response.StatusCode = StatusCodes.Status400BadRequest;
                            await context.Response.WriteAsync("Invalid request.");
                            return;
                        }

                        // reject if banned
                        if (!await requestHandler.ValidateRequest(clientIP, RequestType.GenSAS))
                        {
                            logger.LogInformation("Request from address {clientIP} was rejected due to ban or rate restriction.", clientIP);
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            await context.Response.WriteAsync("Request denied. This has been rate restricted or banned.");
                            return;
                        }

                        // get az function key
                        var azFuncURL = config["AzureFunctionURL"];
                        var azFuncKey = config["AzureFunctionKey"];

                        if (string.IsNullOrEmpty(azFuncURL) || string.IsNullOrEmpty(azFuncKey))
                        {
                            logger.LogInformation("Azure function forwarding incorrectly configured. Request failed.");
                            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                            await context.Response.WriteAsync("Server configuration error.");
                            return;
                        }

                        // forward to az function
                        try
                        {
                            // use Query string 
                            string clientQuery = context.Request.QueryString.Value ?? string.Empty;
                            string azTarget = $"{azFuncURL}{clientQuery}";

                            using var azClient = httpClientFactory.CreateClient();
                            azClient.DefaultRequestHeaders.Add("x-functions-key", azFuncKey);

                            var azResponse = await azClient.GetAsync(azTarget);

                            if (!azResponse.IsSuccessStatusCode)
                            {
                                logger.LogError("Azure Function returned an error: {StatusCode}", azResponse.StatusCode);
                                context.Response.StatusCode = (int)azResponse.StatusCode;
                                await context.Response.WriteAsync("External dependency failed.");
                                return;
                            }

                            var funcContent = await azResponse.Content.ReadAsStringAsync();
                            var funcJson = JsonConvert.DeserializeObject<FunctionResponse>(funcContent);
                            if (funcJson == null)
                            {
                                logger.LogError("Azure Function did not return a valid response.");
                                context.Response.StatusCode = StatusCodes.Status417ExpectationFailed;
                                await context.Response.WriteAsync("External dependency failed.");
                                return;
                            }

                            string redirectURL = $"{funcJson.Uri}?{funcJson.SasToken}";
                            context.Response.StatusCode = 302;
                            context.Response.Headers.Append("Location", redirectURL);
                            await context.Response.CompleteAsync();

                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "There was an error while forwarding request to Azure function: {message}.", ex.Message);
                            context.Response.StatusCode = StatusCodes.Status502BadGateway;
                            await context.Response.WriteAsync("Error processing request while trying to fetch an SAS token for secure connection to storage.");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        logger.LogWarning("Request was canceled by client.");
                        context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
                    }
                    catch (ArgumentException argEx)
                    {
                        logger.LogWarning(argEx, "A configuration or argument error occurred: {Message}", argEx.Message);
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Bad request: invalid configuration or input.");
                    }
                    catch (HttpRequestException httpEx)
                    {
                        logger.LogError(httpEx, "An error occurred while making an HTTP request: {Message}", httpEx.Message);
                        context.Response.StatusCode = StatusCodes.Status502BadGateway;
                        await context.Response.WriteAsync("Failed to process the request due to an external service error.");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "An unexpected error occurred: {Message}", ex.Message);
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        await context.Response.WriteAsync("An unexpected server error occurred.");
                    }
                });
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
            //app.MapGet("leaderboard/", async (
            //        HttpContext context,
            //        [FromServices] RequestHandler requestHandler,
            //        [FromServices] StorageManager storageManager,
            //        [FromServices] IHttpClientFactory httpClientFactory,
            //        [FromServices] IConfiguration config,
            //        [FromServices] ILogger<ProxyServer> logger) =>
            //{
                
            //});

            app.MapPost("/sync-stats",
                async (HttpContext context,
                    [FromServices] RequestHandler requestHandler,
                    [FromServices] DbTransformer transformer,
                    [FromServices] IHttpClientFactory httpClientFactory,
                    [FromServices] IConfiguration config,
                    [FromServices] ILogger<ProxyServer> logger) =>
                {
                    // get client IP
                    string? clientIP = context.Connection.RemoteIpAddress?.ToString();
                    if (string.IsNullOrEmpty(clientIP))
                    {
                        logger.LogWarning("Request received without IP address");
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Invalid request.");
                        return;
                    }

                    if (!await requestHandler.ValidateRequest(clientIP, RequestType.Sync))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsync("Request denied.");
                        return;
                    }

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
        private async static Task<bool> VerifyRequest(HttpContext context, ILogger logger, RequestHandler requestHandler)
        {
            // get client IP
            string? clientIP = context.Connection.RemoteIpAddress?.ToString();
            if (string.IsNullOrEmpty(clientIP))
            {
                logger.LogWarning("Request received without IP address");
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Invalid request.");
                return false;
            }

            // reject if banned
            if (!await requestHandler.ValidateRequest(clientIP, RequestType.GenSAS))
            {
                logger.LogInformation("Request from address {clientIP} was rejected due to ban or rate restriction.", clientIP);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Request denied. This has been rate restricted or banned.");
                return false;
            }

            return true;
        }
    }
}
