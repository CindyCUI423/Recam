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
    public class UserActivityLogRepository : IUserActivityLogRepository
    {
        private readonly MongoDbContext _mongoDbContext;

        public UserActivityLogRepository(MongoDbContext mongoDbContext)
        {
            _mongoDbContext = mongoDbContext;
        }

        public async Task Insert(UserActivityLog log)
        {
            await _mongoDbContext.UserActivityLogs.InsertOneAsync(log);
        }
    }
}
