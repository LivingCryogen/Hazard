
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Services.Configuration;

namespace Model.Stats;

/// <summary>
/// Handles internet connection to Azure Web App (front door for Az Database).
/// </summary>
public class WebConnectionHandler(IOptions<AppConfig> options, ILogger<WebConnectionHandler> logger)
{
    private readonly ILogger<WebConnectionHandler> _logger = logger;
    private readonly string _baseUrl = options.Value.AzConnectInfo.BaseURL;
    private readonly Guid _installID = options.Value.InstallInfo.InstallId;
    private readonly HttpClient _syncClient = new();

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
            var webAppEndpoint = $"{_baseUrl}/health/{_installID}";

            // add api key?

            var response = await _syncClient.GetAsync(webAppEndpoint);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("Connection to Azure or Azure Web App failed: {Message}", ex.Message);
            return false;
        }
    }
}
