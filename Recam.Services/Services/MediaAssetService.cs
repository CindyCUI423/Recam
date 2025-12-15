using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Recam.Models.Collections;
using Recam.Models.Entities;
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
using static Recam.Services.DTOs.DeleteMediaAssetResponse;

namespace Recam.Services.Services
{
    public class MediaAssetService : IMediaAssetService
    {
        private readonly IMediaAssetRepository _mediaAssetRepository;
        private readonly IListingCaseRepository _listingCaseRepository;
        private readonly IMediaAssetHistoryRepository _mediaAssetHistoryRepo;
        private IMapper _mapper;
        private readonly IAuthorizationService _authorizationService;

        public MediaAssetService(IMediaAssetRepository mediaAssetRepository, 
            IListingCaseRepository listingCaseRepository,
            IMediaAssetHistoryRepository mediaAssetHistoryRepo,
            IMapper mapper, IAuthorizationService authorizationService)
        {
            _mediaAssetRepository = mediaAssetRepository;
            _listingCaseRepository = listingCaseRepository;
            _mediaAssetHistoryRepo = mediaAssetHistoryRepo;
            _mapper = mapper;
            _authorizationService = authorizationService;
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

            // If IsHero is "true", check if there is an existing hero media asset for the listing case
            if (request.IsHero)
            {
                var existingHero = await _mediaAssetRepository.GetHeroByListingCaseId(id);

                if (existingHero != null)
                {
                    // If there is an existing hero, set its IsHero property to false first
                    existingHero.IsHero = false;
                    _mediaAssetRepository.UpdateMediaAsset(existingHero);
                }

                mediaAsset.IsHero = true;
            }

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            mediaAsset.UploadedAt = DateTime.UtcNow;
            mediaAsset.IsSelect = false;
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
                throw new Exception("Failed to delete listing case.");
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
