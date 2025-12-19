using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;

namespace AzProxy.Services;

public class SASGenerator(ILogger<SASGenerator> logger,
    IHttpClientFactory httpClientFactory, 
    IConfiguration config)
{
    private class FunctionResponse
    {
        public string Uri { get; init; } = string.Empty;
        public string SasToken { get; init; } = string.Empty;
    }

    public async Task<IResult> GenerateAsync(HttpRequest request)
    {
        // get az function key
        var azFuncURL = config["AzureFunctionURL"];
        var azFuncKey = config["AzureFunctionKey"];
        
        if (string.IsNullOrEmpty(azFuncURL) || string.IsNullOrEmpty(azFuncKey))
        {
            logger.LogInformation("Azure function forwarding incorrectly configured. Request failed.");
            return Results.Problem("Server configuration error.", statusCode: StatusCodes.Status500InternalServerError);
        }

        // forward to az function
        try
        {
            // use Query string 
            string clientQuery = request.QueryString.Value ?? string.Empty;
            string azTarget = $"{azFuncURL}{clientQuery}";
        
            using var azClient = httpClientFactory.CreateClient();
            azClient.DefaultRequestHeaders.Add("x-functions-key", azFuncKey);
        
            var azResponse = await azClient.GetAsync(azTarget);
        
            if (!azResponse.IsSuccessStatusCode)
            {
                logger.LogError("Azure Function returned an error: {StatusCode}", azResponse.StatusCode);
                return Results.StatusCode((int)azResponse.StatusCode);
            }
        
            var funcContent = await azResponse.Content.ReadAsStringAsync();
            var funcJson = JsonConvert.DeserializeObject<FunctionResponse>(funcContent);
            if (funcJson == null ||
                string.IsNullOrWhiteSpace(funcJson.Uri) ||
                string.IsNullOrWhiteSpace(funcJson.SasToken))
            {
                logger.LogError("Azure Function did not return a valid response.");
                return Results.StatusCode((int)azResponse.StatusCode);
            }
        
            string redirectURL = $"{funcJson.Uri}?{funcJson.SasToken}";
            return Results.Redirect(redirectURL);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Request was canceled by client.");
            return Results.Problem("Client canceled the request.", statusCode: StatusCodes.Status499ClientClosedRequest);
        }
        catch (ArgumentException argEx)
        {
            logger.LogWarning(argEx, "A configuration or argument error occurred: {Message}", argEx.Message);
            return Results.Problem("Bad request: invalid configuration or input.", statusCode: StatusCodes.Status400BadRequest);
        }
        catch (HttpRequestException httpEx)
        {
            logger.LogError(httpEx, "An error occurred while making an HTTP request: {Message}", httpEx.Message);
            return Results.Problem("Failed to process the request due to an external service error.", statusCode: StatusCodes.Status502BadGateway);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unexpected error occurred: {Message}", ex.Message);
            return Results.Problem("An unexpected server error occurred.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
