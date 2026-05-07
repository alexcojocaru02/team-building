namespace TeamConnect.Api.Shared.DTOs
{
    public class UpdateProfileDto
    {
        public string? Bio { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Department { get; set; }
        public string? Location { get; set; }
        public string? Timezone { get; set; }
        public string? Pronouns { get; set; }
        public string? PreferredWorkStyle { get; set; }
        public List<string>? Hobbies { get; set; }
        public List<string>? Strengths { get; set; }
        public string? Icebreaker { get; set; }
    }
}
