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
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage;

namespace HazardPublishSAS
{
    public static class hazardsasgen
    {
        [FunctionName("hazardgen")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string accountName = Environment.GetEnvironmentVariable("StorageAccountName");
            string containerName = Environment.GetEnvironmentVariable("ContainerName");
            string blobName = "";

            if (name == "S-C_User")
                blobName = Environment.GetEnvironmentVariable("SelfContainedBlobName");
            else if (name == "F-D_User")
                blobName = Environment.GetEnvironmentVariable("FrameworkDependentBlobName");

            if (string.IsNullOrEmpty(blobName))
                return new BadRequestObjectResult("Invalid user type specified.");

            string connectionString = Environment.GetEnvironmentVariable("StorageConnectionString");

            // Create Blob Client
            BlobServiceClient blobServiceClient = new BlobServiceClient(Environment.GetEnvironmentVariable("StorageConnectionString"));
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

            return new OkObjectResult(accessURL);
        }
    }
}
