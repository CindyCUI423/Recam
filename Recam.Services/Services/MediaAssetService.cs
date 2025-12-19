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
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static Recam.Services.DTOs.CreateMediaAssetResponse;
using static Recam.Services.DTOs.CreateMediaAssetsBatchResponse;
using static Recam.Services.DTOs.DeleteMediaAssetResponse;
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

        public MediaAssetService(IMediaAssetRepository mediaAssetRepository, 
            IListingCaseRepository listingCaseRepository,
            IMediaAssetHistoryRepository mediaAssetHistoryRepo,
            IMapper mapper, IAuthorizationService authorizationService, 
            IUnitOfWork unitOfWork)
        {
            _mediaAssetRepository = mediaAssetRepository;
            _listingCaseRepository = listingCaseRepository;
            _mediaAssetHistoryRepo = mediaAssetHistoryRepo;
            _mapper = mapper;
            _authorizationService = authorizationService;
            _unitOfWork = unitOfWork;
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

                await _unitOfWork.Commit();

                // Log the creation of media assets
                foreach (var asset in assets)
                {
                    await LogMediaAssetHistory(asset.Id, asset.MediaUrl, id, listingCase.Title, "Creation", null, userId);
                }

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

            // Verify this media asset belongs to the specified listing case
            var listingCaseIdResult = asset.ListingCase.Id;

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
