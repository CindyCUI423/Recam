using AutoMapper;
using DnsClient.Internal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<MediaAssetService> _logger;

        public MediaAssetService(IMediaAssetRepository mediaAssetRepository,
            IListingCaseRepository listingCaseRepository,
            IMediaAssetHistoryRepository mediaAssetHistoryRepo,
            IMapper mapper, IAuthorizationService authorizationService,
            IUnitOfWork unitOfWork,
            IBlobStorageService blobStorageService,
            ILogger<MediaAssetService> logger)
        {
            _mediaAssetRepository = mediaAssetRepository;
            _listingCaseRepository = listingCaseRepository;
            _mediaAssetHistoryRepo = mediaAssetHistoryRepo;
            _mapper = mapper;
            _authorizationService = authorizationService;
            _unitOfWork = unitOfWork;
            _blobStorageService = blobStorageService;
            _logger = logger;
        }

        public async Task<CreateMediaAssetResponse> CreateMediaAsset(int id, CreateMediaAssetRequest request, ClaimsPrincipal user)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            _logger.LogInformation(
                "Start creating media asset. ListingCaseId={ListingCaseId}, UserId={UserId}",
                id,
                userId);
            
            // Cet the listing case
            var listingCase = await _listingCaseRepository.GetListingCaseById(id);

            // Check if the listing case exists
            if (listingCase == null)
            {
                _logger.LogWarning(
                    "Listing case not found when creating media asset. ListingCaseId={ListingCaseId}, UserId={UserId}",
                    id,
                    userId);

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
                _logger.LogWarning(
                    "Authorization failed when creating media asset. ListingCaseId={ListingCaseId}, UserId={UserId}",
                    id,
                    userId);

                return new CreateMediaAssetResponse
                {
                    Result = CreateMediaAssetResult.Forbidden,
                    ErrorMessage = "You are not allowed to create media asset for this listing case."
                };
            }

            var mediaAsset = _mapper.Map<MediaAsset>(request);

            mediaAsset.UploadedAt = DateTime.UtcNow;
            mediaAsset.IsSelect = false;
            mediaAsset.IsHero = false;
            mediaAsset.ListingCaseId = id;
            mediaAsset.UserId = userId;
            mediaAsset.IsDeleted = false;

            _logger.LogInformation(
                "Persisting media asset. ListingCaseId={ListingCaseId}, MediaUrl={MediaUrl}",
                id,
                mediaAsset.MediaUrl);

            await _mediaAssetRepository.AddMediaAsset(mediaAsset);
            await _mediaAssetRepository.SaveChangesAsync();

            // Log the creation of this media asset
            await LogMediaAssetHistory(mediaAsset.Id, mediaAsset.MediaUrl, id, listingCase.Title, "Creation", null, userId);

            _logger.LogInformation(
                "CreateMediaAsset completed. MediaAssetId={MediaAssetId}, ListingCaseId={ListingCaseId}",
                mediaAsset.Id,
                id);

            return new CreateMediaAssetResponse
            {
                Result = CreateMediaAssetResult.Success,
                MediaAssetId = mediaAsset.Id
            };
        }

        public async Task<CreateMediaAssetsBatchResponse> CreateMediaAssetsBatch(int id, CreateMediaAssetsBatchRequest request, ClaimsPrincipal user)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            _logger.LogInformation(
                "Start creating multiple media assets. ListingCaseId={ListingCaseId}, UserId={UserId}",
                id,
                userId);
            
            // Cet the listing case
            var listingCase = await _listingCaseRepository.GetListingCaseById(id);

            // Check if the listing case exists
            if (listingCase == null)
            {
                _logger.LogWarning(
                   "Listing case not found when creating media asset. ListingCaseId={ListingCaseId}, UserId={UserId}",
                   id,
                   userId);

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
                _logger.LogWarning(
                    "Authorization failed when creating media assets. ListingCaseId={ListingCaseId}, UserId={UserId}",
                    id,
                    userId);

                return new CreateMediaAssetsBatchResponse
                {
                    Result = CreateMediaAssetsBatchResult.Forbidden,
                    ErrorMessage = "You are not allowed to create media asset for this listing case."
                };
            }

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

                    _logger.LogInformation(
                    "Persisting media asset. ListingCaseId={ListingCaseId}, MediaUrl={MediaUrl}",
                    id,
                    asset.MediaUrl);
                }

                await _unitOfWork.Commit();

                var assetIds = assets.Select(asset => asset.Id).ToList();

                _logger.LogInformation(
                    "CreateMediaAssetsBatch completed. ListingCaseId={ListingCaseId}, MediaAssetId={MediaAssetId}",
                    id,
                    assetIds);

                return new CreateMediaAssetsBatchResponse
                {
                    Result = CreateMediaAssetsBatchResult.Success,
                    MediaAssetIds = assetIds,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to add media assets to db. ListingCaseId={ListingCaseId}",
                    id);
                
                await _unitOfWork.Rollback();
                throw;
            }
        }

        public async Task<DeleteMediaAssetResponse> DeleteMediaAsset(int id, ClaimsPrincipal user)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            _logger.LogInformation(
                "Start deleting media asset. MediaAssetId={MediaAssetId}, UserId={UserId}",
                id,
                userId);

            // Get the media asset
            var media = await _mediaAssetRepository.GetMediaAssetById(id);

            // Check if the media asset exists
            if (media == null)
            {
                _logger.LogWarning(
                   "Media asset not found when deleting media asset. MediaAssetId={MediaAssetId}, UserId={UserId}",
                   id,
                   userId);

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
                _logger.LogWarning(
                    "Authorization failed when deleting media asset. MediaAssetId={MediaAssetId}, UserId={UserId}",
                    id,
                    userId);

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
                _logger.LogWarning(
                    "Failed to extract the blob name from media url when deleting media asset. MediaAssetId={MediaAssetId}, MediaUrl={MediaUrl}",
                    id,
                    media.MediaUrl);

                return new DeleteMediaAssetResponse
                {
                    Result = DeleteMediaAssetResult.Error,
                    ErrorMessage = "Unable to extract the valid blob name from media url."
                };
            }

            // Delete from DB
            _logger.LogInformation(
                    "Deleting media asset from db. MediaAssetId={MediaAssetId}",
                    id);

            var result = await _mediaAssetRepository.DeleteMediaAsset(id);
            await _mediaAssetRepository.SaveChangesAsync();

            // If failed to delete media asset
            if (result == 0)
            {
                _logger.LogError(
                    "Failed to delete media asset from db. MediaAssetId={MediaAssetId}",
                    id);

                throw new Exception("Failed to delete media asset.");
            }
            else
            {
                var listingCaseTitle = media.ListingCase.Title;
                
                // Log media asset deletion on success
                await LogMediaAssetHistory(media.Id, media.MediaUrl, media.ListingCaseId, listingCaseTitle, "Deletion", null, userId);

                _logger.LogInformation(
                    "Delete media asset from db successfully. MediaAssetId={MediaAssetId}",
                    id);
            }

            try
            {
                _logger.LogInformation(
                    "Deleting media asset from blob storage. MediaAssetId={MediaAssetId}, MediaUrl={MediaUrl}",
                    id,
                    media.MediaUrl);

                // Delete from Blob Storage
                var deleted = await _blobStorageService.Delete(blobName);

                // If the blob not found
                if (!deleted)
                {
                    _logger.LogWarning(
                    "Unable to found the media when deleting it from the blob storage. MediaAssetId={MediaAssetId}, MediaUrl={MediaUrl}",
                    id,
                    media.MediaUrl);
                }

                _logger.LogInformation(
                    "Delete media asset from blob storage successfully. MediaAssetId={MediaAssetId}, MediaUrl={MediaUrl}",
                    id,
                    media.MediaUrl);

            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to delete media asset from blob storage. MediaAssetId={MediaAssetId}, MediaUrl={MediaUrl}",
                    id,
                    media.MediaUrl);
            }

            _logger.LogInformation(
                    "DeleteMediaAsset completed successfully. MediaAssetId={MediaAssetId}",
                    id);

            return new DeleteMediaAssetResponse
            {
                Result = DeleteMediaAssetResult.Success,
            };

        }

        public async Task<GetMediaAssetsResponse> GetMediaAssetsByListingCaseId(int id, ClaimsPrincipal user)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            _logger.LogInformation(
                "Start retrieving media assets. ListingCaseId={ListingCaseId}, UserId={UserId}",
                id,
                userId);

            // Get the listing case
            var listingCase = await _listingCaseRepository.GetListingCaseById(id);

            // Check if the listing case exists
            if (listingCase == null)
            {
                _logger.LogWarning(
                  "Listing case not found when retrieving media assets. ListingCaseId={ListingCaseId}, UserId={UserId}",
                  id,
                  userId);

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
                _logger.LogWarning(
                    "Authorization failed when retrieving media assets. ListingCaseId={ListingCaseId}, UserId={UserId}",
                    id,
                    userId);

                return new GetMediaAssetsResponse
                {
                    Result = GetMediaAssetsResult.Forbidden,
                    ErrorMessage = "You are not allowed to access this media assets of this listing case."
                };
            }

            _logger.LogInformation(
                    "Retrieving media assets. ListingCaseId={ListingCaseId}",
                    id);

            var assets = await _mediaAssetRepository.GetMediaAssetsByListingCaseId(id);

            var mappedAssets = _mapper.Map<List<MediaAssetDto>>(assets);

            _logger.LogInformation(
                    "GetMediaAssetsByListingCaseId completed successfully. ListingCaseId={ListingCaseId}",
                    id);

            return new GetMediaAssetsResponse
            {
                Result = GetMediaAssetsResult.Success,
                MediaAssets = mappedAssets
            };
        }

        public async Task<SetHeroMediaResponse> SetHeroMedia(int listingCaseId, int mediaAssetId, ClaimsPrincipal user)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            _logger.LogInformation(
                "Start setting hero media. ListingCaseId={ListingCaseId}, MediaAssetId={MediaAssetId}, UserId={UserId}",
                listingCaseId,
                mediaAssetId,
                userId);

            // Get the listing case
            var listingCase = await _listingCaseRepository.GetListingCaseById(listingCaseId);

            // Check if the listing case exists
            if (listingCase == null)
            {
                _logger.LogWarning(
                  "Listing case not found when seting hero media. ListingCaseId={ListingCaseId}, UserId={UserId}",
                  listingCaseId,
                  userId);

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
                _logger.LogWarning(
                 "Authorization failed when setting hero media asset. ListingCaseId={ListingCaseId}, UserId={UserId}",
                 listingCaseId,
                 userId);

                return new SetHeroMediaResponse
                {
                    Result = SetHeroMediaResult.Forbidden,
                    ErrorMessage = "You are not allowed to access this media assets of this listing case."
                };
            }

            var asset = await _mediaAssetRepository.GetMediaAssetById(mediaAssetId);

            if (asset == null)
            {
                _logger.LogWarning(
                  "Media asset not found when setting hero media. ListingCaseId={ListingCaseId}, MediaAssetId={MediaAssetId}",
                  listingCaseId,
                  mediaAssetId);

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
                _logger.LogWarning(
                  "Media asset does not belong to the provided listing case id when seting hero media. ListingCaseId={ListingCaseId}, MediaAssetId={MediaAssetId}",
                  listingCaseId,
                  mediaAssetId);

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

                _logger.LogInformation(
                    "Reset the existing hero media's IsHero to false. MediaAssetId={MediaAssetId}",
                    existingHero.Id);
            }

            // Set this media asset to Hero
            _logger.LogInformation(
                    "Setting the hero media. MediaAssetId={MediaAssetId}",
                    mediaAssetId);

            asset.IsHero = true;
            _mediaAssetRepository.UpdateMediaAsset(asset);

            await _mediaAssetRepository.SaveChangesAsync();

            _logger.LogInformation(
                    "SetHeroMedia completed. ListingCaseId={ListingCaseId}, MediaAssetId={MediaAssetId}",
                    listingCaseId,
                    mediaAssetId);

            return new SetHeroMediaResponse
            {
                Result = SetHeroMediaResult.Success
            };
        }

        public async Task<SelectMediaResponse> SelectMediaBatch(int id, SelectMediaRequest request, ClaimsPrincipal user)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            _logger.LogInformation(
                "Start selecting media assets. ListingCaseId={ListingCaseId}, UserId={UserId}",
                id,
                userId);

            // Get the listing case
            var listingCase = await _listingCaseRepository.GetListingCaseById(id);

            // Check if the listing case exists
            if (listingCase == null)
            {
                _logger.LogWarning(
                  "Listing case not found when selecting media assets. ListingCaseId={ListingCaseId}, UserId={UserId}",
                  id,
                  userId);

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
                _logger.LogWarning(
                "Authorization failed when selecting media assets. ListingCaseId={ListingCaseId}, UserId={UserId}",
                id,
                userId);

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

                var assetIds = assets.Select(x => x.Id).ToList();

                if (assets.Count != allIds.Count)
                {
                    await _unitOfWork.Rollback();

                    _logger.LogWarning(
                        "Unable to find all the media assets when selecting media assets. ListingCaseId={ListingCaseId}, ExpectedMediaAssetIds={ExpectedMediaAssetIds}, ActualMediaAssetIds={ActualMediaAssetIds}",
                        id,
                        allIds,
                        assetIds);

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

                    _logger.LogWarning(
                        "The total selected media assets number is greater than 10. ListingCaseId={ListingCaseId}, TotalSelectedCount={TotalSelectedCount}",
                        id,
                        totalCount);

                    return new SelectMediaResponse
                    {
                        Result = SelectMediaResult.BadRequest,
                        ErrorMessage = $"You can select up to 10 media assets for a listing case to display. Current selection would become {totalCount}."
                    };
                }

                // Update and save changes
                foreach (var asset in assets)
                {
                    if (selected.Contains(asset.Id))
                    {
                        asset.IsSelect = true;

                        // Log the selection event
                        await LogMediaAssetHistory(asset.Id, asset.MediaUrl, id, listingCase.Title, "Selection", null, userId);

                        _logger.LogWarning(
                            "Selecting the media asset. MediaAssetId={MediaAssetId}, ListingCaseId={ListingCaseId}",
                            asset.Id,
                            id);

                    }

                    else if (unselected.Contains(asset.Id))
                    {
                        asset.IsSelect = false;

                        // Log the unselection event
                        await LogMediaAssetHistory(asset.Id, asset.MediaUrl, id, listingCase.Title, "Cancel Selection", null, userId);

                        _logger.LogWarning(
                            "Unselecting the media asset. MediaAssetId={MediaAssetId}, ListingCaseId={ListingCaseId}",
                            asset.Id,
                            id);
                    }
                }

                await _unitOfWork.Commit();

                _logger.LogWarning(
                    "SelectMediaBatch completed. ListingCaseId={ListingCaseId}",
                    id);

                return new SelectMediaResponse
                {
                    Result = SelectMediaResult.Success
                };

            }
            catch (Exception ex)
            {
                await _unitOfWork.Rollback();

                _logger.LogError(
                    ex,
                    "Failed to update IsSelect when selecting media assets. ListingCaseId={ListingCaseId}",
                    id);

                throw;
            }
        }

        public async Task<GetFinalSelectedMediaResponse> GetFinalSelectedMediaForListingCase(int listingCaseId, ClaimsPrincipal user)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            _logger.LogInformation(
                "Start retrieving the final media asset selection. ListingCaseId={ListingCaseId}, UserId={UserId}",
                listingCaseId,
                userId);

            // Get the listing case
            var listingCase = await _listingCaseRepository.GetListingCaseById(listingCaseId);

            // Check if the listing case exists
            if (listingCase == null)
            {
                _logger.LogWarning(
                  "Listing case not found when retrieving the final media asset selection. ListingCaseId={ListingCaseId}, UserId={UserId}",
                  listingCaseId,
                  userId);

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
                _logger.LogWarning(
                  "Authorization failed when retrieving the final media asset selection. ListingCaseId={ListingCaseId}, UserId={UserId}",
                  listingCaseId,
                  userId);

                return new GetFinalSelectedMediaResponse
                {
                    Result = GetFinalSelectedMediaResult.Forbidden,
                    ErrorMessage = "You are not allowed to access this media assets of this listing case."
                };
            }

            _logger.LogInformation(
                "Retrieving the final media asset selection. ListingCaseId={ListingCaseId}",
                listingCaseId);

            var assets = await _mediaAssetRepository.GetFinalSelectedMediaForListingCase(listingCaseId);

            var mappedAssets = _mapper.Map<List<MediaAssetDto>>(assets);

            _logger.LogInformation(
                "GetFinalSelectedMediaForListingCase completed. ListingCaseId={ListingCaseId}",
                listingCaseId);

            return new GetFinalSelectedMediaResponse
            {
                Result = GetFinalSelectedMediaResult.Success,
                SelectedMediaAssets = mappedAssets,
            };
        }

        public async Task<DownloadListingCaseMediaZipResponse> DownloadListingCaseMediaZip(int listingCaseId, ClaimsPrincipal user, CancellationToken ct)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            _logger.LogInformation(
                "Start downloading the media assets in a zip file. ListingCaseId={ListingCaseId}, UserId={UserId}",
                listingCaseId,
                userId);

            // Get the listing case
            var listingCase = await _listingCaseRepository.GetListingCaseById(listingCaseId);

            // Check if the listing case exists
            if (listingCase == null)
            {
                _logger.LogWarning(
                  "Listing case not found when downloading the media assets in a zip file. ListingCaseId={ListingCaseId}, UserId={UserId}",
                  listingCaseId,
                  userId);

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
                _logger.LogWarning(
                  "Authorization failed when when downloading the media assets in a zip file. ListingCaseId={ListingCaseId}, UserId={UserId}",
                  listingCaseId,
                  userId);

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

                        _logger.LogWarning(
                            "Failed to download the media asset from blob storage due to the blob name extraction error. MediaAssetId={MediaAssetId}, MediaUrl={MediaUrl}",
                            assets[i],
                            assets[i].MediaUrl);

                        continue;
                    }

                    var fileName = $"{i+1:D3}-{SanitizeFileName(Path.GetFileName(blobName))}";

                    try
                    {
                        _logger.LogInformation(
                            "Downloading the media asset from the blob storage. MediaAssetId={MediaAssetId}, MediaUrl={MediaUrl}",
                            assets[i],
                            assets[i].MediaUrl);

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

                        _logger.LogError(
                            ex,
                            "Failed to download the media asset from blob storage. MediaAssetId={MediaAssetId}, MediaUrl={MediaUrl}",
                            assets[i],
                            assets[i].MediaUrl);

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

            _logger.LogInformation(
                            "DownloadListingCaseMediaZip completed. ListingCaseId={ListingCaseId}",
                            listingCaseId);

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
                _logger.LogError(
                    ex,
                    "Failed to log media asset history. " +
                    "MediaAssetId={MediaAssetId}, ListingCaseId={ListingCaseId}, Change={Change}, UserId={UserId}",
                    mediaAssetId,
                    listingCaseId,
                    change,
                    userId
                );
            }
        }

    }
}
