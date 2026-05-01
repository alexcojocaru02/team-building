using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TeamConnect.Api.Shared.Models
{
    public class FeedPostComment
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string FeedPostId { get; set; }
        public string UserId { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
