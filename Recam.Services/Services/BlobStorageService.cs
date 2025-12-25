using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DnsClient.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<BlobStorageService> _logger;
        public BlobStorageService(BlobServiceClient blobServiceClient, IConfiguration configuration, ILogger<BlobStorageService> logger)
        {
            _blobServiceClient = blobServiceClient;
            _containerName = configuration.GetSection("AzureBlobStorage")["ContainerName"];
            _logger = logger;
        }

        public async Task<string> Upload(Stream fileStream, string fileName, string contentType)
        {
            try
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
            catch (Exception ex) 
            {
                _logger.LogError(
                    ex,
                    "Blob upload failed. Container={ContainerName}, BlobName={BlobName}, ContentType={ContentType}",
                    _containerName,
                    fileName,
                    contentType);

                throw;
            }
            
        }

        public async Task<(Stream Stream, string ContentType)> Download(string fileName)
        {
            try
            {
                // Get the container
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

                // Get the blob using the file url
                var blobClient = containerClient.GetBlobClient(fileName);

                var props = await blobClient.GetPropertiesAsync();

                var response = await blobClient.DownloadStreamingAsync();

                return (response.Value.Content, props.Value.ContentType ?? "application/octet-stream");
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogWarning(
                    ex,
                    "Blob not found when downloading. Container={ContainerName}, BlobName={BlobName}",
                    _containerName,
                    fileName);

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Blob download failed. Container={ContainerName}, BlobName={BlobName}",
                    _containerName,
                    fileName);

                throw;
            }
            
        }

        public async Task<bool> Delete(string fileName)
        {
            try
            {
                // Get the container
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

                // Get the blob using the file url
                var blobClient = containerClient.GetBlobClient(fileName);

                var response = await blobClient.DeleteIfExistsAsync(
                    snapshotsOption: DeleteSnapshotsOption.IncludeSnapshots,
                    conditions: null
                    );

                return response.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Blob delete failed. Container={ContainerName}, BlobName={BlobName}",
                    _containerName,
                    fileName);

                throw;
            }
            
        }
    }
}
