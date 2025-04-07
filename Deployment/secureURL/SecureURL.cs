using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Azure.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Azure.Core;
using Azure.Storage.Blobs.Models;
using System.Threading.Tasks;
using Azure.Identity;

namespace SecureURL
{
    public class SecureURL(ILogger<SecureURL> logger, TokenCredential credential)
    {
        [Function("hazardgamesecurelink")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            logger.LogInformation("C# HTTP trigger function processed a request.");

            string architecture = req.QueryString.Value switch {
                string value when value.Contains("x64") => "x64/",
                string value when value.Contains("arm64") => "ARM64/",
                _ => string.Empty
            };

            string? storageString = Environment.GetEnvironmentVariable("StorageUri");
            string? accountName = Environment.GetEnvironmentVariable("AccountName");
            string? containerName = Environment.GetEnvironmentVariable("ContainerName");
            string? blobPrefix = Environment.GetEnvironmentVariable("BlobPrefix");
            string? blobExtension = Environment.GetEnvironmentVariable("BlobExtension");

            if (string.IsNullOrEmpty(storageString) ||
                string.IsNullOrEmpty(accountName) ||
                string.IsNullOrEmpty(containerName) ||
                string.IsNullOrEmpty(blobPrefix) ||
                string.IsNullOrEmpty(blobExtension) ||
                string.IsNullOrEmpty(architecture)) {
                return new UnprocessableEntityResult();
            }

            Uri storageUri = new(storageString);
            string blobName = $"{architecture.ToLower()}{blobPrefix}{architecture}{blobExtension}";

            try {
                // Create options for Blob Client
                var options = new BlobClientOptions() {
                    Retry = {
                        MaxRetries = 3,
                        Mode = RetryMode.Exponential,
                    },
                    Diagnostics = {
                        IsLoggingEnabled = true,
                    }
                };

                // Create Blob Client
                BlobServiceClient blobServiceClient;
                try {
                    blobServiceClient = new(storageUri, credential, options);
                }
                catch (Exception ex) {
                    logger.LogError(ex, "Blob service client failed to instantiate with uri {uri}.", storageUri);
                    return new UnprocessableEntityResult();
                }

                BlobContainerClient blobContainerClient;
                try {
                    blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
                } catch (Exception ex) {
                    logger.LogError(ex, "Blob container client failed to instantiate with container name {name}.", containerName);
                    return new UnprocessableEntityResult();
                }
                bool containerExists = await blobContainerClient.ExistsAsync();
                if (!containerExists) {
                    logger.LogError("Blob container named {name} did not exist.", containerName);
                    return new UnprocessableEntityResult();
                }

                BlobClient blobClient;
                try {
                    blobClient = blobContainerClient.GetBlobClient(blobName);
                } catch (Exception ex) {
                    logger.LogError(ex, "Blob client failed to instantiate with blob name {name}.", blobName);
                    return new UnprocessableEntityResult();
                }
                bool blobExists = await blobClient.ExistsAsync();
                if (!blobExists) {
                    logger.LogError("Blob named {name} did not exist.", blobName);
                    return new UnprocessableEntityResult();
                }

                if (!blobContainerClient.CanGenerateSasUri) {
                    string failReason = await FindAuthFailReason(blobContainerClient, credential);
                    logger.LogError("Blob container client could not authenticate for SAS Uri generation. Reason: {reason}", failReason);
                    return new StatusCodeResult(StatusCodes.Status403Forbidden);
                }

                // Generate SAS Token
                BlobSasBuilder sasBuilder = new() {
                    BlobContainerName = containerName,
                    BlobName = blobName,
                    Resource = "b", // b means blob (here, an individual file)
                    ExpiresOn = DateTimeOffset.UtcNow.AddSeconds(20) // Token valid for 20 seconds
                };
                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                // Get Delegate Key
                UserDelegationKey delegationKey = await blobServiceClient.GetUserDelegationKeyAsync(
                    DateTimeOffset.UtcNow, 
                    DateTimeOffset.UtcNow.AddMinutes(5));


                string sasToken = sasBuilder.ToSasQueryParameters(delegationKey, accountName).ToString();
                string accessURL = $"{blobClient.Uri}?{sasToken}";

                return new RedirectResult(accessURL, false);
            } catch (Exception ex) {
                logger.LogError(ex, "Error generating SAS token and/or secure URL: {message}.", ex.Message);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        private async static Task<string> FindAuthFailReason(BlobContainerClient containerClient, TokenCredential credential)
        {
            string reason = "Unknown.";
            if (credential is DefaultAzureCredential) {
                try {
                    // Try to get a token to test the credential
                    var token = await credential.GetTokenAsync(
                        new TokenRequestContext(new[] { "https://storage.azure.com/.default" }),
                        CancellationToken.None);

                    if (string.IsNullOrEmpty(token.Token)) {
                        reason = "Managed identity returned empty token";
                    }
                    else {
                        // If we got a token but still can't generate SAS,
                        // it's likely a permissions issue
                        reason = "Managed identity has insufficient permissions to generate SAS";
                    }
                } catch (Exception credEx) {
                    reason = $"Managed identity authentication failed: {credEx.Message}";
                }
            }
            else if (credential == null) {
                reason = "No provided credential (was null).";
            }

            return reason;
        }
    }
}
