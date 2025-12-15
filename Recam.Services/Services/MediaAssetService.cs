using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Recam.Models.Entities;
using Recam.Repositories.Interfaces;
using Recam.Services.DTOs;
using Recam.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.Services
{
    public class MediaAssetService : IMediaAssetService
    {
        private readonly IMediaAssetRepository _mediaAssetRepository;
        private IMapper _mapper;

        public MediaAssetService(IMediaAssetRepository mediaAssetRepository, IMapper mapper)
        {
            _mediaAssetRepository = mediaAssetRepository;
            _mapper = mapper;
        }

        public async Task<int> CreateMediaAsset(int id, CreateMediaAssetRequest request, string userId)
        {
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

            mediaAsset.UploadedAt = DateTime.UtcNow;
            mediaAsset.IsSelect = false;
            mediaAsset.ListingCaseId = id;
            mediaAsset.UserId = userId;
            mediaAsset.IsDeleted = false;

            await _mediaAssetRepository.AddMediaAsset(mediaAsset);
            await _mediaAssetRepository.SaveChangesAsync();

            return mediaAsset.Id;
        }
    }
}
