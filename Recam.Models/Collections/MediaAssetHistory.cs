using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Models.Collections
{
    public class MediaAssetHistory
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("mediaAssetId")]
        public int MediaAssetId { get; set; }

        [BsonElement("mediaAssetUrl")]
        public string MediaUrl { get; set; }

        [BsonElement("listingCaseId")]
        public int ListingCaseId { get; set; }

        [BsonElement("listingCaseTitle")]
        public string ListingCaseTitle { get; set; }

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
