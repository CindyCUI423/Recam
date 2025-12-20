using Microsoft.EntityFrameworkCore;
using Recam.DataAccess.Data;
using Recam.Models.Entities;
using Recam.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Repositories.Repositories
{
    public class MediaAssetRepository : IMediaAssetRepository
    {
        private RecamDbContext _dbContext;

        public MediaAssetRepository(RecamDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddMediaAsset(MediaAsset mediaAsset)
        {
            await _dbContext.MediaAssets.AddAsync(mediaAsset);
        }

        public async Task AddMediaAssets(List<MediaAsset> mediaAssets)
        {
            await _dbContext.MediaAssets.AddRangeAsync(mediaAssets);
        }
        
        public async Task<MediaAsset?> GetHeroByListingCaseId(int listingCaseId)
        {
            return await _dbContext.MediaAssets
                .FirstOrDefaultAsync(m =>
                    m.ListingCaseId == listingCaseId &&
                    m.IsHero &&
                    !m.IsDeleted);
        }

        public async Task<List<MediaAsset>> GetMediaAssetsByListingCaseId(int id)
        {
            return await _dbContext.MediaAssets
                .AsNoTracking()
                .Where(m => m.ListingCaseId == id && !m.IsDeleted)
                .OrderBy(m => m.MediaType)
                .ToListAsync();
        }

        public async Task<MediaAsset?> GetMediaAssetById(int id)
        {
            return await _dbContext.MediaAssets
                .AsNoTracking()
                .Include(m => m.ListingCase)
                    .ThenInclude(l => l.AgentListingCases)
                        .ThenInclude(al => al.Agent)
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
        }

        public async Task<List<MediaAsset>> GetMediaAssetsByIds(int listingCaseId, List<int> mediaAssetIds)
        {
            return await _dbContext.MediaAssets
                .Where(m => m.ListingCaseId == listingCaseId && mediaAssetIds.Contains(m.Id) && !m.IsDeleted)
                .ToListAsync();
        }

        public async Task<int> CountSelectedMediaForListingCase(int listingCaseId)
        {
            return await _dbContext.MediaAssets
                .Where(m => m.ListingCaseId == listingCaseId && m.IsSelect && !m.IsDeleted)
                .CountAsync();
        }

        public async Task<List<MediaAsset>> GetFinalSelectedMediaForListingCase(int listingCaseId)
        {
            return await _dbContext.MediaAssets
                .AsNoTracking()
                .Where(m => m.ListingCaseId == listingCaseId && m.IsSelect && !m.IsDeleted)
                .ToListAsync();
        }

        public void UpdateMediaAsset(MediaAsset mediaAsset)
        {
            _dbContext.MediaAssets.Update(mediaAsset);
        }

        public void UpdateMediaAssets(List<MediaAsset> mediaAssets)
        {
            _dbContext.MediaAssets.UpdateRange(mediaAssets);
        }

        public async Task<int> DeleteMediaAsset(int id)
        {
            return await _dbContext.MediaAssets
                .Where(m => m.Id == id)
                .ExecuteDeleteAsync();
        }
        


        public async Task SaveChangesAsync()
        {
            await _dbContext.SaveChangesAsync();
        }

        
    }
}
