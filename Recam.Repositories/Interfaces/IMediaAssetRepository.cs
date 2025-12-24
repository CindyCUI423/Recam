using Recam.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Repositories.Interfaces
{
    public interface IMediaAssetRepository
    {
        Task AddMediaAsset(MediaAsset mediaAsset);
        Task AddMediaAssets(List<MediaAsset> mediaAssets);
        Task<MediaAsset?> GetHeroByListingCaseId(int listingCaseId);
        Task<MediaAsset?> GetMediaAssetById(int id);
        Task<List<MediaAsset>> GetMediaAssetsByIds(int listingCaseId, List<int> mediaAssetIds);
        Task<int> CountSelectedMediaForListingCase(int listingCaseId);
        Task<List<MediaAsset>> GetFinalSelectedMediaForListingCase(int listingCaseId);
        void UpdateMediaAsset(MediaAsset mediaAsset);
        void UpdateMediaAssets(List<MediaAsset> mediaAssets);
        Task<int> DeleteMediaAsset(int id);
        Task<List<MediaAsset>> GetMediaAssetsByListingCaseId(int id);
        Task SaveChangesAsync();
    }
}
