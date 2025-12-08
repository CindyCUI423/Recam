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
    public class CaseHistoryRepository : ICaseHistoryRepository
    {
        private readonly MongoDbContext _mongoDbContext;

        public CaseHistoryRepository(MongoDbContext mongoDbContext)
        {
            _mongoDbContext = mongoDbContext;
        }

        public async Task Insert(CaseHistory caseHistory)
        {
            await _mongoDbContext.CaseHistories.InsertOneAsync(caseHistory);
        }
    }
}
