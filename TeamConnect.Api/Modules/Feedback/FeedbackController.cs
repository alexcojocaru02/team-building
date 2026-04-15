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

            var userIds = new[] { feedback.FromUserId, feedback.ToUserId }
                .Distinct()
                .ToList();

            var users = await _context.Users
                .Find(u => userIds.Contains(u.Id))
                .Project(u => new { u.Id, u.Email })
                .ToListAsync();

            var userEmailsById = users.ToDictionary(u => u.Id, u => u.Email);

            var result = new FeedbackResponseDto
            {
                Id = feedback.Id,
                FromUserId = feedback.FromUserId,
                ToUserId = feedback.ToUserId,
                Message = feedback.Message,
                CreatedAt = feedback.CreatedAt,
                FromUserEmail = userEmailsById.TryGetValue(feedback.FromUserId, out var fromEmail)
                    ? fromEmail
                    : "Unknown",
                ToUserEmail = userEmailsById.TryGetValue(feedback.ToUserId, out var toEmail)
                    ? toEmail
                    : "Unknown"
            };

            return Ok(result);
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

            var userIds = feedbacks
                .SelectMany(f => new[] { f.FromUserId, f.ToUserId })
                .Distinct()
                .ToList();

            var users = await _context.Users
                .Find(u => userIds.Contains(u.Id))
                .Project(u => new { u.Id, u.Email })
                .ToListAsync();

            var userEmailsById = users.ToDictionary(u => u.Id, u => u.Email);

            var result = feedbacks.Select(f => new FeedbackResponseDto
            {
                Id = f.Id,
                FromUserId = f.FromUserId,
                ToUserId = f.ToUserId,
                Message = f.Message,
                CreatedAt = f.CreatedAt,
                FromUserEmail = userEmailsById.TryGetValue(f.FromUserId, out var fromEmail)
                    ? fromEmail
                    : "Unknown",
                ToUserEmail = userEmailsById.TryGetValue(f.ToUserId, out var toEmail)
                    ? toEmail
                    : "Unknown"
            });

            return Ok(result);
        }

    }
}
