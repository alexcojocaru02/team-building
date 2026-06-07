using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeamConnect.Api.Modules.Teams;
using TeamConnect.Api.Shared.Models;

namespace TeamConnect.Api.Modules.Gamification
{
    [ApiController]
    [Route("api/gamification")]
    [Authorize]
    public class GamificationController : ControllerBase
    {
        private readonly GamificationService _gamificationService;
        private readonly TeamsService _teamsService;

        public GamificationController(GamificationService gamificationService, TeamsService teamsService)
        {
            _gamificationService = gamificationService;
            _teamsService = teamsService;
        }

        [HttpGet("leaderboard/{teamId}")]
        public async Task<IActionResult> GetLeaderboard(string teamId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var team = await _teamsService.GetById(teamId);
            if (team == null) return NotFound("Team not found");

            var isAdmin = userRole == UserRoles.Admin;
            var isMember = team.MemberIds.Contains(userId);

            if (!isAdmin && !isMember)
                return StatusCode(403, "Only team members can view the leaderboard");

            return Ok(await _gamificationService.GetLeaderboard(teamId, team.MemberIds));
        }
    }
}
