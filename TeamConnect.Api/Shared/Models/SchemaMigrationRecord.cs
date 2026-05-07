using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TeamConnect.Api.Shared.Models
{
    public class SchemaMigrationRecord
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        public string Version { get; set; } = string.Empty;
        public DateTime AppliedAtUtc { get; set; }
        public string Mode { get; set; } = string.Empty;
        public string SummaryJson { get; set; } = string.Empty;
    }
}