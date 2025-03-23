using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Azure.Storage;

#pragma warning disable CS1998 // AZ Functions defaults to async even if everything we're doing is sync

namespace SecureLinkFunc
{
    public static class SecureLink
    {
        [FunctionName("hazardgamesecurelink")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requesterName = req.Query["name"];
            string expectedName = Environment.GetEnvironmentVariable("UserName");
            
            if (string.IsNullOrEmpty(requesterName) || requesterName != expectedName)
                return new UnauthorizedResult(); // return 401 Unauthorized if not provided the expected security key

            string accountName = Environment.GetEnvironmentVariable("StorageAccountName");
            string containerName = Environment.GetEnvironmentVariable("ContainerName");
            string blobName = Environment.GetEnvironmentVariable("BlobName");
            string connectionString = Environment.GetEnvironmentVariable("StorageConnectionString");

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
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1) // Token valid for 1 hour
                };
                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                string storageKey = Environment.GetEnvironmentVariable("StorageKey");
                if (string.IsNullOrEmpty(storageKey))
                    return new BadRequestObjectResult("Invalid storage account key.");

                StorageSharedKeyCredential storageCredential = new(accountName, storageKey);
                var sasObject = sasBuilder.ToSasQueryParameters(storageCredential);
                string sasToken = sasObject.ToString();

                string accessURL = $"{blobClient.Uri}?{sasToken}";

                var jsonResult = new
                {
                    URL = accessURL,
                    SAS = sasToken
                };

                return new OkObjectResult(accessURL);
            }
            catch (Exception ex) {
                log.LogError($"Error generating SAS token and/or secure URL: {ex.Message}.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
