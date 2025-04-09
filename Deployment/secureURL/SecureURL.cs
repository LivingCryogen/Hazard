using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Azure.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Azure.Core;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SecureURL
{
    public class SecureURL(ILogger<SecureURL> logger)
    {
        [Function("hazardgamesecurelink")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            logger.LogInformation("C# HTTP trigger function processed a request.");

            string architecture = req.QueryString.Value switch {
                string value when value.Contains("x64") => "x64/",
                string value when value.Contains("arm64") => "ARM64/",
                _ => string.Empty
            };

            string? storageString = Environment.GetEnvironmentVariable("StorageUri");
            string? storageKey = Environment.GetEnvironmentVariable("StorageKey");
            string? accountName = Environment.GetEnvironmentVariable("AccountName");
            string? containerName = Environment.GetEnvironmentVariable("ContainerName");
            string? blobPrefix = Environment.GetEnvironmentVariable("BlobPrefix");
            string? blobExtension = Environment.GetEnvironmentVariable("BlobExtension");

            if (string.IsNullOrEmpty(storageString) ||
                string.IsNullOrEmpty(storageKey) ||
                string.IsNullOrEmpty(accountName) ||
                string.IsNullOrEmpty(containerName) ||
                string.IsNullOrEmpty(blobPrefix) ||
                string.IsNullOrEmpty(blobExtension) ||
                string.IsNullOrEmpty(architecture)) {
                logger.LogError("An application setting was invalid.");
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

                // Generate SharedKeyCredential
                StorageSharedKeyCredential credential;
                try {
                    credential = new StorageSharedKeyCredential(accountName, storageKey);
                } catch (Exception ex) {
                    logger.LogError(ex, "Azure Function failed to authenticate with Storage account {name}.", accountName);
                    return new StatusCodeResult(StatusCodes.Status401Unauthorized);
                }

                // Create Blob Client
                BlobServiceClient blobServiceClient;
                try {
                    blobServiceClient = new(storageUri, credential);
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

                // Generate SAS Token
                BlobSasBuilder sasBuilder;
                try {
                    sasBuilder = new() {
                        BlobContainerName = containerName,
                        BlobName = blobName,
                        Resource = "b", // b means blob (here, an individual file)
                        ExpiresOn = DateTimeOffset.UtcNow.AddSeconds(300) // Token valid for 5 minutes
                    };
                    sasBuilder.SetPermissions(BlobSasPermissions.Read);
                } catch (Exception ex) {
                    logger.LogError(ex, "There was an error instantiating SAS Builder: {message}", ex.Message);
                    return new StatusCodeResult(StatusCodes.Status403Forbidden);
                }

                var sasToken = sasBuilder.ToSasQueryParameters(credential).ToString();
                string accessURL = $"{blobClient.Uri}?{sasToken}";

                return new RedirectResult(accessURL, false);
            } catch (Exception ex) {
                logger.LogError(ex, "Error generating SAS token and/or secure URL: {message}.", ex.Message);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
