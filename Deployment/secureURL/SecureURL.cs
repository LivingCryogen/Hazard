using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Azure.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Azure.Core;

namespace SecureURL
{
    public class SecureURL(ILogger<SecureURL> logger, TokenCredential credential)
    {
        [Function("hazardgamesecurelink")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
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
            string? storageKey = Environment.GetEnvironmentVariable("StorageKey");
            string? blobPrefix = Environment.GetEnvironmentVariable("BlobPrefix");
            string? blobExtension = Environment.GetEnvironmentVariable("BlobExtension");

            if (string.IsNullOrEmpty(storageString) ||
                string.IsNullOrEmpty(accountName) ||
                string.IsNullOrEmpty(containerName) ||
                string.IsNullOrEmpty(storageKey) ||
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
                BlobServiceClient blobServiceClient = new(storageUri, credential, options);
                BlobContainerClient blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
                BlobClient blobClient = blobContainerClient.GetBlobClient(blobName);

                // Generate SAS Token
                BlobSasBuilder sasBuilder = new() {
                    BlobContainerName = containerName,
                    BlobName = blobName,
                    Resource = "b", // b means blob (here, an individual file)
                    ExpiresOn = DateTimeOffset.UtcNow.AddSeconds(20) // Token valid for 20 seconds
                };
                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                StorageSharedKeyCredential storageCredential = new(accountName, storageKey);
                var sasObject = sasBuilder.ToSasQueryParameters(storageCredential);
                string sasToken = sasObject.ToString();
                string accessURL = $"{blobClient.Uri}?{sasToken}";

                return new RedirectResult(accessURL, false);
            } catch (Exception ex) {
                logger.LogError(ex, "Error generating SAS token and/or secure URL: {message}.", ex.Message);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
