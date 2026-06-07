using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using TeamConnect.Api.Shared.Models;

namespace TeamConnect.Api.Shared.DTOs
{
    public class CreateTeamActivityDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        [Required]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ActivityType ActivityType { get; set; }

        public List<string> Options { get; set; } = new();
        public int Points { get; set; } = 10;
        public DateTime? ScheduledAt { get; set; }
        public DateTime? ScheduledEndAt { get; set; }
        public string? MeetingLink { get; set; }
    }
}