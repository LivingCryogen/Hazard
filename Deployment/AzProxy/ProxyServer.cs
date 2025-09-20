using AzProxy.Context;
using AzProxy.DataTransform;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzProxy
{
    public class ProxyServer
    {
        private class FunctionResponse
        {
            public string Uri { get; init; } = string.Empty;
            public string SasToken { get; init; } = string.Empty;
        }

        public static void Main(string[] args)
        {
            var app = GetBuiltApp(args);
            app.UseHttpsRedirection();
            app.UseCors("FromGitHubPages");

            app.MapGet("/", () => "Proxy is up.");
            app.MapGet("/secure-link",
                async (HttpContext context,
                        RequestHandler requestHandler,
                        IHttpClientFactory httpClientFactory,
                        IConfiguration config,
                        ILogger<ProxyServer> logger) =>
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
                        if (!requestHandler.ValidateRequest(clientIP, RequestType.GenSAS))
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
            app.MapPost("/sync-stats",
                async (HttpContext context, 
                    RequestHandler requestHandler,
                    DbTransformer transformer,
                    IHttpClientFactory httpClientFactory, 
                    IConfiguration config, 
                    ILogger<ProxyServer> logger) =>
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

                    if (!requestHandler.ValidateRequest(clientIP, RequestType.Sync))
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

                    // get Query strings
                    string? installID = context.Request.Query["installID"];
                    string? trackedActions = context.Request.Query["trackedActions"];
                    
                    if (string.IsNullOrEmpty(installID))
                    {
                        logger.LogWarning("Sync request received without Install Id query parameter");
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Invalid request.");
                        return;
                    }


                    int actions = 0;
                    if (!string.IsNullOrEmpty(trackedActions))
                        if (int.TryParse(trackedActions, out int parsed))
                            actions = parsed;

                    try
                    {
                        await transformer.TransformFromJson(requestBody, installID, actions);

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
            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<IBanCache, BanListCache>();
            builder.Services.AddHostedService<BanListTableManager>();
            builder.Services.AddSingleton<BanService>();
            builder.Services.AddSingleton<RequestHandler>();
            builder.Services.AddDbContext<GameStatsDbContext>(options => options.UseAzureSql(builder.Configuration.GetConnectionString("AzDbConnectionString")));
            builder.Services.AddScoped<DbTransformer>();
            builder.Services.AddLogging();
            return builder.Build();
        }
    }
}
