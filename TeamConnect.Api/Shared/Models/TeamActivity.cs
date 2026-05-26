using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TeamConnect.Api.Shared.Models
{
    public class TeamActivity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonRepresentation(BsonType.ObjectId)]
        public string TeamId { get; set; } = string.Empty;

        [BsonRepresentation(BsonType.ObjectId)]
        public string CreatedByUserId { get; set; } = string.Empty;

        [BsonRepresentation(BsonType.String)]
        public ActivityType ActivityType { get; set; } = ActivityType.Prompt;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public int Points { get; set; }
        public DateTime? DueAt { get; set; }
        public string Status { get; set; } = "Open";
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<TeamActivityParticipation> Participations { get; set; } = new();
    }

    public class TeamActivityParticipation
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; } = string.Empty;

        public string? TextResponse { get; set; }
        public int? SelectedOptionIndex { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
}