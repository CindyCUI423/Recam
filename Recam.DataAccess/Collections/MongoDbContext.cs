using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Recam.Models.Collections;
using Recam.Models.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.DataAccess.Collections
{
    public class MongoDbContext
    {
        private IMongoDatabase _database;
        public MongoDbContext(IOptions<MongoDbSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionStrings);
            _database = client.GetDatabase(settings.Value.DatabaseName);
        }

        public IMongoCollection<UserActivityLog> UserActivityLogs => _database.GetCollection<UserActivityLog>("UserActivityLogs");
        public IMongoCollection<CaseHistory> CaseHistories => _database.GetCollection<CaseHistory>("CaseHistories");
        public IMongoCollection<MediaAssetHistory > MediaAssetHistories => _database.GetCollection<MediaAssetHistory>("MediaAssetHistories");
    }
}
