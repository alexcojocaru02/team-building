using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TeamConnect.Api.Shared.Models
{
    public class FeedPost
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string AuthorId { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
