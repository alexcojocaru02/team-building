using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeamConnect.Api.Modules.Teams;
using TeamConnect.Api.Shared.DTOs;
using TeamConnect.Api.Shared.Models;

namespace TeamConnect.Api.Modules.Feedback
{
    [ApiController]
    [Route("api/feedback")]
    [Authorize]
    public class FeedbackController : ControllerBase
    {
        private readonly FeedbackService _feedbackService;
        private readonly TeamsService _teamsService;

        public FeedbackController(FeedbackService feedbackService, TeamsService teamsService)
        {
            _feedbackService = feedbackService;
            _teamsService = teamsService;
        }

        [HttpPost]
        public async Task<IActionResult> Send([FromBody] CreateFeedbackDto dto)
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

        [HttpGet("team/{teamId}/member/{userId}")]
        [Authorize(Policy = "AdminOrTeamOwner")]
        public async Task<IActionResult> GetReceivedForTeamMember(string teamId, string userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);
            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            var team = await _teamsService.GetById(teamId);
            if (team == null) return NotFound("Team not found");

            var isAdmin = currentUserRole == UserRoles.Admin;
            var isTeamOwner = team.OwnerId == currentUserId;
            if (!isAdmin && !isTeamOwner) return Forbid();

            if (!team.MemberIds.Contains(userId))
                return BadRequest("User is not a member of this team");

            return Ok(await _feedbackService.GetReceived(userId));
        }
    }
}
