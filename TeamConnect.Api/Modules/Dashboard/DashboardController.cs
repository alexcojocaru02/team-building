using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TeamConnect.Api.Shared.DTOs;
using TeamConnect.Api.Shared.Services;

namespace TeamConnect.Api.Modules.Dashboard
{
    [ApiController]
    [Route("api/dashboard")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly MongoDbContext _context;

        public DashboardController(MongoDbContext context)
        {
            _context = context;
        }

        [HttpGet("cohesion")]
        public async Task<IActionResult> GetCohesion()
        {
            var feedbacks = await _context.Feedbacks
                .Find(_ => true)
                .ToListAsync();

            var users = await _context.Users
                .Find(_ => true)
                .Project(u => new { u.Id, u.Email })
                .ToListAsync();

            var userStats = users.Select(u => new UserFeedbackStatsDto
            {
                UserId = u.Id,
                Email = u.Email,
                FeedbackReceived = feedbacks.Count(f => f.ToUserId == u.Id)
            })
            .OrderByDescending(u => u.FeedbackReceived)
            .ToList();

            var dashboard = new CohesionDashboardDto
            {
                TotalFeedbacks = feedbacks.Count,
                Users = userStats
            };

            return Ok(dashboard);
        }
    }

}
