using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Recam.Models.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.DataAccess.Data
{
    public class MongoDbContext
    {
        private IMongoDatabase _database;
        public MongoDbContext(IOptions<MongoDbSettings> settings) 
        {
            var client = new MongoClient(settings.Value.ConnectionStrings);
            _database = client.GetDatabase(settings.Value.DatabaseName);
        }
    }
}
