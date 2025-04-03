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
                    ILogger<ProxyServer> logger) =>
                {
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
                    var accessToken = await GetCredentialToken(azFuncScope);

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
                    }
                    catch (Exception ex) {
                        logger.LogError(ex, "There was an error while forwarding request to Azure: {message}.", ex.Message);
                        context.Response.StatusCode = StatusCodes.Status502BadGateway;
                        await context.Response.WriteAsync("Error processing request.");
                    }
                });
            app.Run();
        }

        private static async Task<AccessToken> GetCredentialToken(string azureScope)
        {
            var credential = new DefaultAzureCredential();
            var tokenContext = new TokenRequestContext([azureScope]);
            return await credential.GetTokenAsync(tokenContext);
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
            builder.Services.AddSingleton<IBanCache, BanListCache>();
            builder.Services.AddHostedService<BanListTableManager>();
            builder.Services.AddSingleton<BanService>();
            builder.Services.AddSingleton<RequestHandler>();
            builder.Services.AddLogging();
            return builder.Build();
        }
    }
}
