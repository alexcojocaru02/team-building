using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TeamConnect.Api.Shared.Models;
using TeamConnect.Api.Shared.Services;

namespace TeamConnect.Api.Modules.Teams
{
    [ApiController]
    [Route("api/teams")]
    [Authorize]
    public class TeamsController : ControllerBase
    {
        private readonly MongoDbContext _context;

        public TeamsController(MongoDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Create(string name)
        {
            var team = new Team { Name = name };
            await _context.Teams.InsertOneAsync(team);
            return Ok(team);
        }

        [HttpPost("{teamId}/add/{userId}")]
        public async Task<IActionResult> AddUser(string teamId, string userId)
        {
            var update = Builders<Team>.Update.AddToSet(t => t.MemberIds, userId);
            await _context.Teams.UpdateOneAsync(t => t.Id == teamId, update);
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var teams = await _context.Teams.Find(_ => true).ToListAsync();
            return Ok(teams);
        }
    }

}
