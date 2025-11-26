using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Recam.Models.Collections
{
    public class UserActivityLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        [BsonElement("userId")]
        public string UserId { get; set; }
        [BsonElement("userName")]
        public string UserName { get; set; }
        [BsonElement("occurredAt")]
        public DateTime OccurredAt { get; set; }
        [BsonElement("action")]
        public string Action { get; set; }
        [BsonElement("isSuccessful")]
        public bool IsSuccessful { get; set; }
    }
}
