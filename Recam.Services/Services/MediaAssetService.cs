using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Recam.Models.Collections;
using Recam.Models.Entities;
using Recam.Models.Enums;
using Recam.Repositories.Interfaces;
using Recam.Services.DTOs;
using Recam.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static Recam.Services.DTOs.CreateMediaAssetResponse;
using static Recam.Services.DTOs.CreateMediaAssetsBatchResponse;
using static Recam.Services.DTOs.DeleteMediaAssetResponse;
using static Recam.Services.DTOs.DownloadListingCaseMediaZipResponse;
using static Recam.Services.DTOs.GetFinalSelectedMediaResponse;
using static Recam.Services.DTOs.SelectMediaResponse;
using static Recam.Services.DTOs.SetHeroMediaResponse;

namespace Recam.Services.Services
{
    public class MediaAssetService : IMediaAssetService
    {
        private readonly IMediaAssetRepository _mediaAssetRepository;
        private readonly IListingCaseRepository _listingCaseRepository;
        private readonly IMediaAssetHistoryRepository _mediaAssetHistoryRepo;
        private IMapper _mapper;
        private readonly IAuthorizationService _authorizationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBlobStorageService _blobStorageService;

        public MediaAssetService(IMediaAssetRepository mediaAssetRepository, 
            IListingCaseRepository listingCaseRepository,
            IMediaAssetHistoryRepository mediaAssetHistoryRepo,
            IMapper mapper, IAuthorizationService authorizationService, 
            IUnitOfWork unitOfWork,
            IBlobStorageService blobStorageService)
        {
            _mediaAssetRepository = mediaAssetRepository;
            _listingCaseRepository = listingCaseRepository;
            _mediaAssetHistoryRepo = mediaAssetHistoryRepo;
            _mapper = mapper;
            _authorizationService = authorizationService;
            _unitOfWork = unitOfWork;
            _blobStorageService = blobStorageService;
        }

        public async Task<CreateMediaAssetResponse> CreateMediaAsset(int id, CreateMediaAssetRequest request, ClaimsPrincipal user)
        {
            // Cet the listing case
            var listingCase = await _listingCaseRepository.GetListingCaseById(id);

            // Check if the listing case exists
            if (listingCase == null)
            {
                return new CreateMediaAssetResponse
                {
                    Result = CreateMediaAssetResult.BadRequest,
                    ErrorMessage = "Unable to find the resource. Please provide a valid listing case id."
                };
            }

            // Check resource-based Authorization
            var authResult = await _authorizationService.AuthorizeAsync(user, listingCase, "ListingCaseAccess");

            if (!authResult.Succeeded)
            {
                return new CreateMediaAssetResponse
                {
                    Result = CreateMediaAssetResult.Forbidden,
                    ErrorMessage = "You are not allowed to create media asset for this listing case."
                };
            }

            var mediaAsset = _mapper.Map<MediaAsset>(request);

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            mediaAsset.UploadedAt = DateTime.UtcNow;
            mediaAsset.IsSelect = false;
            mediaAsset.IsHero = false;
            mediaAsset.ListingCaseId = id;
            mediaAsset.UserId = userId;
            mediaAsset.IsDeleted = false;

            await _mediaAssetRepository.AddMediaAsset(mediaAsset);
            await _mediaAssetRepository.SaveChangesAsync();

            // Log the creation of this media asset
            await LogMediaAssetHistory(mediaAsset.Id, mediaAsset.MediaUrl, id, listingCase.Title, "Creation", null, userId);

            return new CreateMediaAssetResponse
            {
                Result = CreateMediaAssetResult.Success,
                MediaAssetId = mediaAsset.Id
            };
        }

        public async Task<CreateMediaAssetsBatchResponse> CreateMediaAssetsBatch(int id, CreateMediaAssetsBatchRequest request, ClaimsPrincipal user)
        {
            // Cet the listing case
            var listingCase = await _listingCaseRepository.GetListingCaseById(id);

            // Check if the listing case exists
            if (listingCase == null)
            {
                return new CreateMediaAssetsBatchResponse
                {
                    Result = CreateMediaAssetsBatchResult.BadRequest,
                    ErrorMessage = "Unable to find the resource. Please provide a valid listing case id."
                };
            }

            // Check resource-based Authorization
            var authResult = await _authorizationService.AuthorizeAsync(user, listingCase, "ListingCaseAccess");

            if (!authResult.Succeeded)
            {
                return new CreateMediaAssetsBatchResponse
                {
                    Result = CreateMediaAssetsBatchResult.Forbidden,
                    ErrorMessage = "You are not allowed to create media asset for this listing case."
                };
            }

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            // Build Media Assets
            var assets = request.MediaUrls.Select(url => new MediaAsset
            {
                MediaType = request.MediaType,
                MediaUrl = url,
                UploadedAt = DateTime.UtcNow,
                IsSelect = false,
                IsHero = false,
                ListingCaseId = id,
                UserId = userId,
                IsDeleted = false,
            }).ToList();

            // Transaction
            await _unitOfWork.BeginTransaction();

            try
            {
                await _mediaAssetRepository.AddMediaAssets(assets);
                await _mediaAssetRepository.SaveChangesAsync();

                // Log the creation of media assets
                foreach (var asset in assets)
                {
                    await LogMediaAssetHistory(asset.Id, asset.MediaUrl, id, listingCase.Title, "Creation", null, userId);
                }

                await _unitOfWork.Commit();

                var assetIds = assets.Select(asset => asset.Id).ToList();

                return new CreateMediaAssetsBatchResponse
                {
                    Result = CreateMediaAssetsBatchResult.Success,
                    MediaAssetIds = assetIds,
                };
            }
            catch (Exception)
            {
                await _unitOfWork.Rollback();
                throw;
            }
        }

        public async Task<DeleteMediaAssetResponse> DeleteMediaAsset(int id, ClaimsPrincipal user)
        {
            // Get the media asset
            var media = await _mediaAssetRepository.GetMediaAssetById(id);

            // Check if the media asset exists
            if (media == null)
            {
                return new DeleteMediaAssetResponse
                {
                    Result = DeleteMediaAssetResult.BadRequest,
                    ErrorMessage = "Unable to find the resource. Please provide a valid media asset id."
                };
            }

            // Check resource-based Authorization
            var authResult = await _authorizationService.AuthorizeAsync(user, media, "MediaAssetAccess");

            if (!authResult.Succeeded)
            {
                return new DeleteMediaAssetResponse
                {
                    Result = DeleteMediaAssetResult.Forbidden,
                    ErrorMessage = "You are not allowed to access this media asset."
                };
            }

            // Get the blob name
            var blobName = ExtractBlobNameFromUrl(media.MediaUrl);
            if (string.IsNullOrWhiteSpace(blobName))
            {
                return new DeleteMediaAssetResponse
                {
                    Result = DeleteMediaAssetResult.Error,
                    ErrorMessage = "Unable to extract the valid blob name from media url."
                };
            }

            // Delete from DB
            var result = await _mediaAssetRepository.DeleteMediaAsset(id);
            await _mediaAssetRepository.SaveChangesAsync();

            // If failed to delete media asset
            if (result == 0)
            {
                throw new Exception("Failed to delete media asset.");
            }
            else
            {
                var listingCaseTitle = media.ListingCase.Title;
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                
                // Log media asset deletion on success
                await LogMediaAssetHistory(media.Id, media.MediaUrl, media.ListingCaseId, listingCaseTitle, "Deletion", null, userId);
            }

            try
            {
                // Delete from Blob Storage
                var deleted = await _blobStorageService.Delete(blobName);

                // TODO: logger
                // if (!deleted) means the blob not found...log this event
            }
            catch (Exception ex)
            {
                // TODO: logger
                // do not throw the exception --> do not make this API request failed
                // log this event
            }

            return new DeleteMediaAssetResponse
            {
                Result = DeleteMediaAssetResult.Success,
            };

        }

        public async Task<GetMediaAssetsResponse> GetMediaAssetsByListingCaseId(int id, ClaimsPrincipal user)
        {
            // Get the listing case
            var listingCase = await _listingCaseRepository.GetListingCaseById(id);

            // Check if the listing case exists
            if (listingCase == null)
            {
                return new GetMediaAssetsResponse
                {
                    Result = GetMediaAssetsResult.BadRequest,
                    ErrorMessage = "Unable to find the resource. Please provide a valid listing case id."
                };
            }

            // Check resource-based Authorization
            var authResult = await _authorizationService.AuthorizeAsync(user, listingCase, "ListingCaseAccess");

            if (!authResult.Succeeded)
            {
                return new GetMediaAssetsResponse
                {
                    Result = GetMediaAssetsResult.Forbidden,
                    ErrorMessage = "You are not allowed to access this media assets of this listing case."
                };
            }

            var assets = await _mediaAssetRepository.GetMediaAssetsByListingCaseId(id);

            var mappedAssets = _mapper.Map<List<MediaAssetDto>>(assets);

            return new GetMediaAssetsResponse
            {
                Result = GetMediaAssetsResult.Success,
                MediaAssets = mappedAssets
            };
        }

        public async Task<SetHeroMediaResponse> SetHeroMedia(int listingCaseId, int mediaAssetId, ClaimsPrincipal user)
        {
            // Get the listing case
            var listingCase = await _listingCaseRepository.GetListingCaseById(listingCaseId);

            // Check if the listing case exists
            if (listingCase == null)
            {
                return new SetHeroMediaResponse
                {
                    Result = SetHeroMediaResult.BadRequest,
                    ErrorMessage = "Unable to find the resource. Please provide a valid listing case id."
                };
            }

            // Check resource-based Authorization
            var authResult = await _authorizationService.AuthorizeAsync(user, listingCase, "ListingCaseAccess");

            if (!authResult.Succeeded)
            {
                return new SetHeroMediaResponse
                {
                    Result = SetHeroMediaResult.Forbidden,
                    ErrorMessage = "You are not allowed to access this media assets of this listing case."
                };
            }

            var asset = await _mediaAssetRepository.GetMediaAssetById(mediaAssetId);

            if (asset == null)
            {
                return new SetHeroMediaResponse
                {
                    Result = SetHeroMediaResult.BadRequest,
                    ErrorMessage = "Unable to find the resource. Please provide a valid media asset id."
                };
            }

            // Verify this media asset belongs to the specified listing case
            var listingCaseIdResult = asset.ListingCaseId;

            if (listingCaseIdResult != listingCaseId)
            {
                return new SetHeroMediaResponse
                {
                    Result = SetHeroMediaResult.BadRequest,
                    ErrorMessage = $"Media Asset {mediaAssetId} does not belong to the Listing Case {listingCaseId}."
                };
            }

            // Reset exsting Hero media if it exists
            var existingHero = await _mediaAssetRepository.GetHeroByListingCaseId(listingCaseId);

            if (existingHero != null) 
            {
                existingHero.IsHero = false;
                _mediaAssetRepository.UpdateMediaAsset(existingHero);
            }

            // Set this media asset to Hero
            asset.IsHero = true;
            _mediaAssetRepository.UpdateMediaAsset(asset);

            await _mediaAssetRepository.SaveChangesAsync();

            return new SetHeroMediaResponse
            {
                Result = SetHeroMediaResult.Success
            };
        }

        public async Task<SelectMediaResponse> SelectMediaBatch(int id, SelectMediaRequest request, ClaimsPrincipal user)
        {
            // Get the listing case
            var listingCase = await _listingCaseRepository.GetListingCaseById(id);

            // Check if the listing case exists
            if (listingCase == null)
            {
                return new SelectMediaResponse
                {
                    Result = SelectMediaResult.BadRequest,
                    ErrorMessage = "Unable to find the resource. Please provide a valid listing case id."
                };
            }

            // Check resource-based Authorization
            var authResult = await _authorizationService.AuthorizeAsync(user, listingCase, "ListingCaseAccess");

            if (!authResult.Succeeded)
            {
                return new SelectMediaResponse
                {
                    Result = SelectMediaResult.Forbidden,
                    ErrorMessage = "You are not allowed to access this media assets of this listing case."
                };
            }

            var selected = (request.SelectedId ?? new List<int>()).Distinct().ToHashSet();
            var unselected = (request.UnselectedId ?? new List<int>()).Distinct().ToHashSet();

            var allIds = selected.Union(unselected).ToList();

            // Transaction
            await _unitOfWork.BeginTransaction();

            try
            {
                var assets = await _mediaAssetRepository.GetMediaAssetsByIds(id, allIds);

                if (assets.Count != allIds.Count)
                {
                    await _unitOfWork.Rollback();

                    return new SelectMediaResponse
                    {
                        Result = SelectMediaResult.BadRequest,
                        ErrorMessage = "Unable to find the resource. Please provide valid listing case id or media asset id."
                    };
                }

                // Check the selected media count is not greater than 10
                var selectedCount = await _mediaAssetRepository.CountSelectedMediaForListingCase(id);

                var willIncrease = assets.Count(a => selected.Contains(a.Id) && a.IsSelect == false);
                var willDecrease = assets.Count(a => unselected.Contains(a.Id) && a.IsSelect == true);

                var totalCount = selectedCount - willDecrease + willIncrease;

                if (totalCount > 10)
                {
                    await _unitOfWork.Rollback();

                    return new SelectMediaResponse
                    {
                        Result = SelectMediaResult.BadRequest,
                        ErrorMessage = $"You can select up to 10 media assets for a listing case to display. Current selection would become {totalCount}."
                    };
                }

                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

                // Update and save changes
                foreach (var asset in assets)
                {
                    if (selected.Contains(asset.Id))
                    {
                        asset.IsSelect = true;

                        // Log the selection event
                        await LogMediaAssetHistory(asset.Id, asset.MediaUrl, id, listingCase.Title, "Selection", null, userId);

                    }

                    else if (unselected.Contains(asset.Id))
                    {
                        asset.IsSelect = false;

                        // Log the unselection event
                        await LogMediaAssetHistory(asset.Id, asset.MediaUrl, id, listingCase.Title, "Cancel Selection", null, userId);
                    }
                }

                await _unitOfWork.Commit();

                return new SelectMediaResponse
                {
                    Result = SelectMediaResult.Success
                };

            }
            catch (Exception)
            {
                await _unitOfWork.Rollback();
                throw;
            }
        }

        public async Task<GetFinalSelectedMediaResponse> GetFinalSelectedMediaForListingCase(int listingCaseId, ClaimsPrincipal user)
        {
            // Get the listing case
            var listingCase = await _listingCaseRepository.GetListingCaseById(listingCaseId);

            // Check if the listing case exists
            if (listingCase == null)
            {
                return new GetFinalSelectedMediaResponse
                {
                    Result = GetFinalSelectedMediaResult.BadRequest,
                    ErrorMessage = "Unable to find the resource. Please provide a valid listing case id."
                };
            }

            // Check resource-based Authorization
            var authResult = await _authorizationService.AuthorizeAsync(user, listingCase, "ListingCaseAccess");

            if (!authResult.Succeeded)
            {
                return new GetFinalSelectedMediaResponse
                {
                    Result = GetFinalSelectedMediaResult.Forbidden,
                    ErrorMessage = "You are not allowed to access this media assets of this listing case."
                };
            }

            var assets = await _mediaAssetRepository.GetFinalSelectedMediaForListingCase(listingCaseId);

            var mappedAssets = _mapper.Map<List<MediaAssetDto>>(assets);

            return new GetFinalSelectedMediaResponse
            {
                Result = GetFinalSelectedMediaResult.Success,
                SelectedMediaAssets = mappedAssets,
            };
        }

        public async Task<DownloadListingCaseMediaZipResponse> DownloadListingCaseMediaZip(int listingCaseId, ClaimsPrincipal user, CancellationToken ct)
        {
            // Get the listing case
            var listingCase = await _listingCaseRepository.GetListingCaseById(listingCaseId);

            // Check if the listing case exists
            if (listingCase == null)
            {
                return new DownloadListingCaseMediaZipResponse
                {
                    Result = DownloadZipResult.BadRequest,
                    ErrorMessage = "Unable to find the resource. Please provide a valid listing case id."
                };
            }

            // Check resource-based Authorization
            var authResult = await _authorizationService.AuthorizeAsync(user, listingCase, "ListingCaseAccess");

            if (!authResult.Succeeded)
            {
                return new DownloadListingCaseMediaZipResponse
                {
                    Result = DownloadZipResult.Forbidden,
                    ErrorMessage = "You are not allowed to access this media assets of this listing case."
                };
            }

            var assets = await _mediaAssetRepository.GetMediaAssetsByListingCaseId(listingCaseId);

            var zipTempPath = Path.Combine(Path.GetTempPath(), $"listing-case-{listingCaseId}-media-{Guid.NewGuid():N}.zip"); // Create a temporary path to write zip
            var zipFileStream = new FileStream(
                zipTempPath,
                FileMode.CreateNew,
                FileAccess.ReadWrite,
                FileShare.Read,
                bufferSize: 1024 * 64,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.DeleteOnClose
                );

            // Prepare a list for manifest.txt
            var manifestLines = new List<string>
            { 
                $"GeneratedAtUTC: {DateTime.UtcNow}",
                $"ListingCaseId: {listingCaseId}",
                $"TotalAssets: {assets.Count}",
            };

            // Leave the stream open to return the zip stream to the controller
            using (var zip = new ZipArchive(zipFileStream, ZipArchiveMode.Create, leaveOpen: true))
            {

                for (int i = 0; i < assets.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    // Get the blobName from the media url
                    var blobName = ExtractBlobNameFromUrl(assets[i].MediaUrl);

                    if (string.IsNullOrWhiteSpace(blobName))
                    {
                        // Record the skip in the manifest.txt
                        manifestLines.Add($"[SKIP] Index={i} MediaAssetId={assets[i]} Reason=BlobNameExtractionFailure MediaUrl={assets[i].MediaUrl ?? null}");

                        continue;
                    }

                    var fileName = $"{i+1:D3}-{SanitizeFileName(Path.GetFileName(blobName))}";

                    try
                    {
                        // Download the media asset from Blob Storage
                        var (blobStream, contentType) = await _blobStorageService.Download(blobName);

                        // Create a media asser file in zip
                        var entry = zip.CreateEntry(fileName, CompressionLevel.Fastest);
                        await using (blobStream)
                        await using (var entryStream = entry.Open())
                        {
                            await blobStream.CopyToAsync(entryStream, 1024 * 64, ct);
                        }

                        // Record the success in the manifest.txt
                        manifestLines.Add($"[SUCCESS] Index={i} MediaAssetId={assets[i]} MediaUrl={assets[i].MediaUrl}");
                    }
                    catch (Exception ex) 
                    {
                        // Record the error in the manifest.txt
                        manifestLines.Add($"[FAIL] Index={i} MediaAssetId={assets[i]} Exception={ex.GetType().Name}: {ex.Message} MediaUrl={assets[i].MediaUrl}");

                        continue;
                    }
                }

                // Write the manifest.txt into the zip file
                var manifestEntry = zip.CreateEntry("manifest.txt", CompressionLevel.Fastest);
                await using (var manifestStream = manifestEntry.Open())
                await using (var writer = new StreamWriter(manifestStream, Encoding.UTF8, bufferSize: 1024, leaveOpen: false))
                {
                    foreach (var line in manifestLines) 
                    {
                        await writer.WriteLineAsync(line);
                    }
                }
            }

            // Reset the pointer to the beginning of the stream, otherwise the downloaded zip will be empty
            zipFileStream.Position = 0;

            return new DownloadListingCaseMediaZipResponse
            {
                Result = DownloadZipResult.Success,
                ZipStream = zipFileStream,
                ZipFileName = $"listing-case-{listingCaseId}-media.zip"
            };
        }



        private static string? ExtractBlobNameFromUrl(string url)
        {
            // Get uri: "/container-name/blob-name"
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return null;
            }

            var path = uri.AbsolutePath.Trim('/');
            var parts = path.Split('/', 2); // can only be split into up to 2 parts
            if (parts.Length < 2) return null;

            return parts[1]; // parts[0]
        }

        private static string SanitizeFileName(string fileName)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "media_asset";
            }

            return fileName;
        }

        private async Task LogMediaAssetHistory(
            int mediaAssetId,
            string mediaUrl,
            int listingCaseId,
            string listingCaseTitle,
            string change,
            string? description,
            string userId
            )
        {
            var log = new MediaAssetHistory
            {
                MediaAssetId = mediaAssetId,
                MediaUrl = mediaUrl,
                ListingCaseId = listingCaseId,
                ListingCaseTitle = listingCaseTitle,
                Change = change,
                Description = description,
                UserId = userId,
                OccurredAt = DateTime.UtcNow
            };

            try
            {
                await _mediaAssetHistoryRepo.Insert(log);
            }
            catch (Exception ex)
            {
                // TODO: add failure into logger...?
            }
        }



    }
}
