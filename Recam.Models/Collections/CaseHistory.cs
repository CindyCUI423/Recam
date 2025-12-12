using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Models.Collections
{
    public class CaseHistory
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("listingCaseId")]
        public int ListingCaseId { get; set; }

        [BsonElement("caseTitle")]
        public string CaseTitle { get; set; }

        [BsonElement("change")]
        public string Change { get; set; }

        [BsonElement("description")]
        public string? Description { get; set; }

        [BsonElement("userId")]
        public string UserId { get; set; }

        [BsonElement("occurredAt")]
        public DateTime OccurredAt { get; set; }

        

    }
}
