using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Recam.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.Services
{
    public class BlobStorageService: IBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient; // Blob Storage Account
        private readonly string _containerName;
        public BlobStorageService(BlobServiceClient blobServiceClient, IConfiguration configuration)
        {
            _blobServiceClient = blobServiceClient;
            _containerName = configuration.GetSection("AzureBlobStorage")["ContainerName"];
        }

        public async Task<string> Upload(Stream fileStream, string fileName, string contentType)
        {
            // Get the container
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

            // Create a reference/object for this file
            var blobClient = containerClient.GetBlobClient(fileName);

            // Delete the existing blob with the same name
            await blobClient.DeleteIfExistsAsync();

            await blobClient.UploadAsync(
                fileStream,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = contentType
                    }
                });

            return blobClient.Uri.ToString();
        }

        public async Task<(Stream Stream, string ContentType)> Download(string fileName)
        {
            // Get the container
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

            // Get the blob using the file url
            var blobClient = containerClient.GetBlobClient(fileName);

            var props = await blobClient.GetPropertiesAsync();

            var response = await blobClient.DownloadStreamingAsync();

            return (response.Value.Content, props.Value.ContentType ?? "application/octet-stream");
        }

        public async Task Delete(string fileName)
        {
            // Get the container
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

            // Get the blob using the file url
            var blobClient = containerClient.GetBlobClient(fileName);

            await blobClient.DeleteAsync();
        }
    }
}
