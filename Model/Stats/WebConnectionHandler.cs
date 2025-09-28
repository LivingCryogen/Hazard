
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
    private readonly Guid _installID = options.Value.InstallInfo.InstallId;
    private readonly HttpClient _syncClient = new() { Timeout = TimeSpan.FromSeconds(30) };

    public async Task<bool> VerifyInternetConnection()
    {
        try
        {
            using var response = await _syncClient.GetAsync("http://neverssl.com/", HttpCompletionOption.ResponseHeadersRead);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

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

    public async Task<bool> PostGameSession(string gameSessionJson, int trackedActions)
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
            //  build request URL with Query Parameters
            var queryParams = $"?installID={_installID}&trackedActions={trackedActions}";
            var requestURL = $"{_baseUrl}/sync-stats{queryParams}";

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
