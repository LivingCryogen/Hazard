
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Services.Configuration;

namespace Model.Stats;

/// <summary>
/// Handles internet connection to Azure Web App (front door for Az Database).
/// </summary>
public class WebConnectionHandler(IOptions<AppConfig> options, ILogger<WebConnectionHandler> logger) : IDisposable
{
    private readonly ILogger<WebConnectionHandler> _logger = logger;
    private readonly string _baseUrl = options.Value.AzConnectInfo.BaseURL;
    private readonly HttpClient _syncClient = new(
        new HttpClientHandler() 
        {
            AllowAutoRedirect = true,
            UseProxy = true,
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        }) 
            {
                Timeout = TimeSpan.FromSeconds(30) 
            };
    /// <summary>
    /// Verifies the availability of an internet connection by attempting to access a known URL.
    /// </summary>
    /// <remarks>Sends a request to a Google server and checks if the response indicates success.
    /// Logs a warning if the verification fails due to an exception.</remarks>
    /// <returns><see langword="true"/> if the internet connection is verified successfully; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> VerifyInternetConnection()
    {
        try
        {
            using var response = await _syncClient.GetAsync("http://clients3.google.com/generate_204", HttpCompletionOption.ResponseHeadersRead);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Internet connection verification failed: {Message}", ex.Message);
            return false;
        }
    }
    /// <summary>
    /// Verifies the connection to an Azure Web App.
    /// </summary>
    /// <remarks>Attempts to connect to the Azure Web App using the configured base URL. It logs a
    /// warning if the connection fails or times out.</remarks>
    /// <returns><see langword="true"/> if the connection to the Azure Web App is successful; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> VerifyAzureWebAppConnection()
    {
        try
        {
            var webAppEndpoint = $"{_baseUrl}/";

            // add api key?

            var response = await _syncClient.GetAsync(webAppEndpoint, HttpCompletionOption.ResponseHeadersRead);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("Connection to Azure or Azure Web App failed: {Message}", ex.Message);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning("Connection to Azure Web App timed out: {Message}", ex.Message);
            return false;
        }
    }
    /// <summary>
    /// Attempt to POST a game session's stats to the Azure Web App.
    /// </summary>
    /// <param name="gameSessionJson">A properly formed JSON (should match GameSessionDto).</param>
    /// <returns><see langword="true"/> if the POST was successful (Web App returned Code 200); otherwise, <see langword="false"/></returns>
    public async Task<bool> PostGameSession(string gameSessionJson)
    {
        if (!await VerifyInternetConnection())
        {
            _logger.LogWarning("Internet connection verification failed during GameSession POST");
            return false;
        }

        if (!await VerifyAzureWebAppConnection())
        {
            _logger.LogWarning("Azure Web App connection verification failed during GameSession POST");
            return false;
        }

        try
        {
            var requestURL = $"{_baseUrl}/sync-stats";

            // format string Content for HTTP Body
            StringContent bodyContent = new(gameSessionJson, System.Text.Encoding.UTF8, "application/json");

            _logger.LogInformation("Posting game session stats to {url}", requestURL);
            var response = await _syncClient.PostAsync(requestURL, bodyContent);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully posted game session!");
                return true;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.MultiStatus)
            {
                _logger.LogWarning("Game session posted with partial success (207)");
                return true; // Treat as success for now
            }
            else
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to post game session. Status: {status}, Response: {resp}",
                    response.StatusCode, responseBody);
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError("HTTP exception posting game session: {Message}", ex.Message);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError("Timeout posting game session: {Message}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError("Unexpected exception posting game session: {Message}", ex.Message);
            return false;
        }
    }

    void IDisposable.Dispose()
    {
        _syncClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}
