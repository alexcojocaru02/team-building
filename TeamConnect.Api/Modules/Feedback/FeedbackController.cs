using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;
using TeamConnect.Api.Shared.DTOs;
using TeamConnect.Api.Shared.Services;
using FeedbackModel = TeamConnect.Api.Shared.Models.Feedback;


namespace TeamConnect.Api.Modules.Feedback
{
    [ApiController]
    [Route("api/feedback")]
    [Authorize]
    public class FeedbackController : ControllerBase
    {
        private readonly MongoDbContext _context;

        public FeedbackController(MongoDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Send(CreateFeedbackDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Message))
                return BadRequest("Feedback message is required");

            if (string.IsNullOrEmpty(dto.ToUserId))
                return BadRequest("Target user is required");

            var fromUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (fromUserId == dto.ToUserId)
                return BadRequest("You cannot send feedback to yourself");

            var feedback = new FeedbackModel
            {
                FromUserId = fromUserId,
                ToUserId = dto.ToUserId,
                Message = dto.Message,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Feedbacks.InsertOneAsync(feedback);
            return Ok(feedback);
        }

        // feedback primit de userul curent
        [HttpGet("received")]
        public async Task<IActionResult> GetReceived()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var feedbacks = await _context.Feedbacks
                .Find(f => f.ToUserId == userId)
                .SortByDescending(f => f.CreatedAt)
                .ToListAsync();

            return Ok(feedbacks);
        }

    }
}
