using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Azure.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SecureURL
{
    public class SecureURL
    {
        private readonly ILogger<SecureURL> _logger;

        public SecureURL(ILogger<SecureURL> logger)
        {
            _logger = logger;
        }

        [Function("hazardgamesecurelink")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string? accountName = Environment.GetEnvironmentVariable("StorageAccountName");
            string? containerName = Environment.GetEnvironmentVariable("ContainerName");
            string? blobName = Environment.GetEnvironmentVariable("BlobName");
            string? connectionString = Environment.GetEnvironmentVariable("StorageConnectionString");
            string? storageKey = Environment.GetEnvironmentVariable("StorageKey");

            if (string.IsNullOrEmpty(accountName) ||
                string.IsNullOrEmpty(containerName) ||
                string.IsNullOrEmpty(blobName) ||
                string.IsNullOrEmpty(connectionString) ||
                string.IsNullOrEmpty(storageKey)) {
                return new UnprocessableEntityResult();
            }

            try {
                // Create Blob Client
                BlobServiceClient blobServiceClient = new(connectionString);
                BlobContainerClient blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
                BlobClient blobClient = blobContainerClient.GetBlobClient(blobName);

                // Generate SAS Token
                BlobSasBuilder sasBuilder = new() {
                    BlobContainerName = containerName,
                    BlobName = blobName,
                    Resource = "b", // b means blob (here, an individual file)
                    ExpiresOn = DateTimeOffset.UtcNow.AddSeconds(10) // Token valid for 10 seconds
                };
                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                if (string.IsNullOrEmpty(storageKey))
                    return new BadRequestObjectResult("Invalid storage account key.");

                StorageSharedKeyCredential storageCredential = new(accountName, storageKey);
                var sasObject = sasBuilder.ToSasQueryParameters(storageCredential);
                string sasToken = sasObject.ToString();
                string accessURL = $"{blobClient.Uri}?{sasToken}";

                return new OkObjectResult(accessURL);
            } catch (Exception ex) {
                _logger.LogError($"Error generating SAS token and/or secure URL: {ex.Message}.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
