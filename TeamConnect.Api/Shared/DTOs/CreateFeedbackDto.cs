using TeamConnect.Api.Shared.Models;

namespace TeamConnect.Api.Shared.DTOs
{
    public class CreateFeedbackDto
    {
        public string ToUserId { get; set; }
        public string Message { get; set; }
        public FeedbackCategory Category { get; set; }
        public FeedbackTone Tone { get; set; }
    }
}
