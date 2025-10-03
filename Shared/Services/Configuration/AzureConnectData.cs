using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Services.Configuration;

/// <summary>
/// Represents the connection data required to interact with an Azure service.
/// </summary>
/// <remarks>This class encapsulates the base URL and API key necessary for authenticating and making requests to
/// an Azure service.</remarks>
public class AzureConnectData
{
    /// <summary>
    /// Gets or sets the base URL used for API requests.
    /// </summary>
    public string BaseURL { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the API key used for authentication.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}
