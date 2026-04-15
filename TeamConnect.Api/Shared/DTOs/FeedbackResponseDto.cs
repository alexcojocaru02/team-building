namespace TeamConnect.Api.Shared.DTOs
{
    public class FeedbackResponseDto
    {
        public string Id { get; set; }
        public string FromUserId { get; set; }
        public string ToUserId { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public string FromUserEmail { get; set; }
        public string ToUserEmail { get; set; }
    }
}