using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeamConnect.Api.Modules.Teams;
using TeamConnect.Api.Shared.Models;

namespace TeamConnect.Api.Modules.Dashboard
{
    [ApiController]
    [Route("api/dashboard")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly DashboardService _dashboardService;
        private readonly TeamsService _teamsService;

        public DashboardController(DashboardService dashboardService, TeamsService teamsService)
        {
            _dashboardService = dashboardService;
            _teamsService = teamsService;
        }

        [HttpGet("cohesion/{teamId}")]
        public async Task<IActionResult> GetCohesion(string teamId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var team = await _teamsService.GetById(teamId);
            if (team == null) return NotFound("Team not found");

            var isAdmin = userRole == UserRoles.Admin;
            var isOwner = team.OwnerId == userId;

            if (!isAdmin && !isOwner)
                return StatusCode(403, "Only team owners can access the cohesion dashboard");

            return Ok(await _dashboardService.GetCohesion(team.MemberIds));
        }
    }
}
