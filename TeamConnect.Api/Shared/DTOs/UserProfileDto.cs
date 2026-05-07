namespace TeamConnect.Api.Shared.DTOs
{
    public class UserProfileDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public List<string> TeamIds { get; set; } = new();

        // Interpersonal profile fields
        public string? Bio { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Department { get; set; }
        public string? Location { get; set; }
        public string? Timezone { get; set; }
        public string? Pronouns { get; set; }
        public string? PreferredWorkStyle { get; set; }
        public List<string> Hobbies { get; set; } = new();
        public List<string> Strengths { get; set; } = new();
        public string? Icebreaker { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
