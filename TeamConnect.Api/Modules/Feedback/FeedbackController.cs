using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeamConnect.Api.Shared.DTOs;

namespace TeamConnect.Api.Modules.Feedback
{
    [ApiController]
    [Route("api/feedback")]
    [Authorize]
    public class FeedbackController : ControllerBase
    {
        private readonly FeedbackService _feedbackService;

        public FeedbackController(FeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        [HttpPost]
        public async Task<IActionResult> Send(CreateFeedbackDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Message))
                return BadRequest("Feedback message is required");

            if (string.IsNullOrEmpty(dto.ToUserId))
                return BadRequest("Target user is required");

            var fromUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(fromUserId))
                return Unauthorized();

            if (fromUserId == dto.ToUserId)
                return BadRequest("You cannot send feedback to yourself");

            if (!await _feedbackService.AreTeammatesAsync(fromUserId, dto.ToUserId))
                return StatusCode(403, "You can only send feedback to members of your team");

            var result = await _feedbackService.Send(dto, fromUserId);
            return Ok(result);
        }

        [HttpGet("received")]
        public async Task<IActionResult> GetReceived()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            return Ok(await _feedbackService.GetReceived(userId));
        }
    }
}
