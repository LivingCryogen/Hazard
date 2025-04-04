using Azure.Core;
using Azure.Identity;
using System.Threading.Tasks;

namespace AzProxy
{
    public class ProxyServer
    {
        public static void Main(string[] args)
        {
            var app = GetBuiltApp(args);
            app.UseHttpsRedirection();
            app.UseCors("FromGitHubPages");

            app.MapGet("/", () => "Proxy is up.");
            app.MapGet("/secure-url",
                async (HttpContext context,
                    RequestHandler requestHandler,
                    IHttpClientFactory httpClientFactory,
                    IConfiguration config,
                    ILogger<ProxyServer> logger,
                    TokenCredential azCredential) =>
                {
                    try {
                        // get client IP
                        string? clientIP = context.Connection.RemoteIpAddress?.ToString();
                        if (string.IsNullOrEmpty(clientIP)) {
                            logger.LogWarning("Request received without IP address");
                            context.Response.StatusCode = StatusCodes.Status400BadRequest;
                            await context.Response.WriteAsync("Invalid request.");
                            return;
                        }

                        // reject if banned
                        if (!requestHandler.ValidateRequest(clientIP)) {
                            logger.LogInformation("Request from address {clientIP} was rejected due to ban or rate restriction.", clientIP);
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            await context.Response.WriteAsync("Request denied.");
                            return;
                        }

                        // get az credential token
                        var azFuncURL = config["AzureFunctionURL"];
                        var azFuncScope = config["AzureFunctionScope"];
                        if (string.IsNullOrEmpty(azFuncURL) ||
                            string.IsNullOrEmpty(azFuncScope)) {
                            logger.LogInformation("Azure forwarding incorrectly configured. Request failed.");
                            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                            await context.Response.WriteAsync("Server configuration error.");
                            return;
                        }

                        AccessToken accessToken;
                        try {
                            accessToken = await GetCredentialToken(azCredential, azFuncScope, logger, context.RequestAborted);
                            logger.LogInformation("Acquired valid access token!");
                        } catch (Exception ex) {
                            logger.LogError(ex, "Failed to acquire access token. See previous logs for details.");
                            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                            await context.Response.WriteAsync("Authentication error. See server logs for details.");
                            return;
                        }

                        // forward to az function
                        try {
                            if (string.IsNullOrEmpty(azFuncURL)) {
                                logger.LogError("Azure Function URL was not configured.");
                                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                                await context.Response.WriteAsync("Server configuration error.");
                                return;
                            }

                            // use Query string 
                            string clientQuery = context.Request.QueryString.Value ?? string.Empty;
                            string azTarget = $"{azFuncURL}{clientQuery}";

                            using var azClient = httpClientFactory.CreateClient();
                            azClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken.Token);

                            var azResponse = await azClient.GetAsync(azTarget);

                            // Forward request info to azClient
                            context.Response.StatusCode = (int)azResponse.StatusCode;
                            foreach (var header in azResponse.Headers)
                                if (!header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                                    context.Response.Headers[header.Key] = header.Value.ToArray();

                            if (azResponse.Content.Headers.ContentType != null)
                                context.Response.ContentType = azResponse.Content.Headers.ContentType.ToString();

                            // Forward response body
                            var azBody = await azResponse.Content.ReadAsByteArrayAsync();
                            await context.Response.Body.WriteAsync(azBody);
                        } catch (Exception ex) {
                            logger.LogError(ex, "There was an error while forwarding request to Azure: {message}.", ex.Message);
                            context.Response.StatusCode = StatusCodes.Status502BadGateway;
                            await context.Response.WriteAsync("Error processing request.");
                        }
                    }
                    catch (OperationCanceledException) {
                        logger.LogWarning("Request was canceled by client.");
                        context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
                    } catch (ArgumentException argEx) {
                        logger.LogWarning(argEx, "A configuration or argument error occurred: {Message}", argEx.Message);
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Bad request: invalid configuration or input.");
                    } catch (HttpRequestException httpEx) {
                        logger.LogError(httpEx, "An error occurred while making an HTTP request: {Message}", httpEx.Message);
                        context.Response.StatusCode = StatusCodes.Status502BadGateway;
                        await context.Response.WriteAsync("Failed to process the request due to an external service error.");
                    } catch (Exception ex) {
                        logger.LogError(ex, "An unexpected error occurred: {Message}", ex.Message);
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        await context.Response.WriteAsync("An unexpected server error occurred.");
                    }
                });
            app.Run();
        }

        private static async Task<AccessToken> GetCredentialToken(TokenCredential azCredential, string azureScope, ILogger logger, CancellationToken cancel)
        {
            TokenRequestContext tokenContext;
            try {
                logger.LogInformation("Attempting to acquire token context with scope: {scope}...", azureScope);
                tokenContext = new TokenRequestContext([azureScope]);
            } catch (Exception ex) {
                logger.LogError(ex, "There was an error establishing TokenRequestContext with scope value {scope}:" +
                        "{message} Source: {src} Inner Exception: {inner} Data: {data} StackTrace: {trace}"
                    , azureScope, ex.Message, ex.Source, ex.InnerException, ex.Data, ex.StackTrace);
                throw;
            }

            AccessToken token;
            try {
                logger.LogInformation("Attempting to acquire token with context {context}...", tokenContext);
                token = await azCredential.GetTokenAsync(tokenContext, cancel);
            } catch (Exception ex) {
                logger.LogError(ex, "There was an error fetching the Access Token under request context {context}:" +
                        "{message} Source: {src} Inner Exception: {inner} Data: {data} StackTrace: {trace}"
                    , tokenContext, ex.Message, ex.Source, ex.InnerException, ex.Data, ex.StackTrace);
                throw;
            }

            return token;
        }

        private static WebApplication GetBuiltApp(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddEnvironmentVariables();
            builder.Services.AddCors(options => {
                options.AddPolicy("FromGitHubPages", policy =>
                {
                    policy.WithOrigins("https://livingcryogen.github.io")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<TokenCredential, DefaultAzureCredential>();
            builder.Services.AddSingleton<IBanCache, BanListCache>();
            builder.Services.AddHostedService<BanListTableManager>();
            builder.Services.AddSingleton<BanService>();
            builder.Services.AddSingleton<RequestHandler>();
            builder.Services.AddLogging();
            return builder.Build();
        }
    }
}
