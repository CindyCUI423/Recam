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
        Task<MediaAsset?> GetHeroByListingCaseId(int listingCaseId);
        Task<MediaAsset?> GetMediaAssetById(int id);
        void UpdateMediaAsset(MediaAsset mediaAsset);
        Task<int> DeleteMediaAsset(int id);

        Task SaveChangesAsync();
    }
}
