using Recam.DataAccess.Collections;
using Recam.Models.Collections;
using Recam.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Repositories.Repositories
{
    public class MediaAssetHistoryRepository : IMediaAssetHistoryRepository
    {
        private readonly MongoDbContext _mongoDbContext;

        public MediaAssetHistoryRepository(MongoDbContext mongoDbContext)
        {
            _mongoDbContext = mongoDbContext;
        }

        public async Task Insert(MediaAssetHistory mediaAssetHistory)
        {
            await _mongoDbContext.MediaAssetHistories.InsertOneAsync(mediaAssetHistory);
        }
    }
}
