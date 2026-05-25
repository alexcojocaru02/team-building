using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;
using TeamConnect.Api.Shared.DTOs;
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
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create(CreateTeamDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Team name is required.");

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            var normalizedTeamName = dto.Name.Trim();

            var team = new Team
            {
                Name = normalizedTeamName,
                Description = dto.Description,
                OwnerId = currentUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                MemberIds = new List<string> { currentUserId }
            };

            await _context.Teams.InsertOneAsync(team);

            // Add team to creator's TeamIds
            var updateUser = Builders<User>.Update
                .AddToSet(u => u.TeamIds, team.Id)
                .Set(u => u.UpdatedAt, DateTime.UtcNow);
            
            await _context.Users.UpdateOneAsync(u => u.Id == currentUserId, updateUser);

            return Ok(MapToDetailDto(team));
        }

        [HttpPost("{teamId}/add/{userId}")]
        [Authorize(Policy = "AdminOrTeamOwner")]
        public async Task<IActionResult> AddUser(string teamId, string userId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            var team = await _context.Teams.Find(t => t.Id == teamId).FirstOrDefaultAsync();
            if (team == null)
                return NotFound("Team not found");

            // Check authorization: must be admin or team owner
            var isAdmin = currentUserRole == UserRoles.Admin;
            var isTeamOwner = team.OwnerId == currentUserId;

            if (!isAdmin && !isTeamOwner)
                return Forbid();

            // Check if user exists
            var user = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null)
                return BadRequest("User not found");

            // Keep the operation idempotent so membership drift does not block a re-add.
            if (team.MemberIds.Contains(userId))
            {
                var repairUser = Builders<User>.Update
                    .AddToSet(u => u.TeamIds, teamId)
                    .Set(u => u.UpdatedAt, DateTime.UtcNow);

                await _context.Users.UpdateOneAsync(u => u.Id == userId, repairUser);

                return Ok(new { message = "User added to team" });
            }

            // Add to team
            var updateTeam = Builders<Team>.Update
                .AddToSet(t => t.MemberIds, userId)
                .Set(t => t.UpdatedAt, DateTime.UtcNow);
            
            await _context.Teams.UpdateOneAsync(t => t.Id == teamId, updateTeam);

            // Add team to user's TeamIds
            var updateUser = Builders<User>.Update
                .AddToSet(u => u.TeamIds, teamId)
                .Set(u => u.UpdatedAt, DateTime.UtcNow);
            
            await _context.Users.UpdateOneAsync(u => u.Id == userId, updateUser);

            return Ok(new { message = "User added to team" });
        }

        [HttpDelete("{teamId}/members/{userId}")]
        [Authorize(Policy = "AdminOrTeamOwner")]
        public async Task<IActionResult> RemoveUser(string teamId, string userId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            var team = await _context.Teams.Find(t => t.Id == teamId).FirstOrDefaultAsync();
            if (team == null)
                return NotFound("Team not found");

            // Check authorization: must be admin or team owner
            var isAdmin = currentUserRole == UserRoles.Admin;
            var isTeamOwner = team.OwnerId == currentUserId;

            if (!isAdmin && !isTeamOwner)
                return Forbid();

            var user = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            var userExists = user != null;

            // Keep the operation idempotent so membership drift does not block a remove.
            if (team.MemberIds.Contains(userId))
            {
                var updateTeam = Builders<Team>.Update
                    .Pull(t => t.MemberIds, userId)
                    .Set(t => t.UpdatedAt, DateTime.UtcNow);

                await _context.Teams.UpdateOneAsync(t => t.Id == teamId, updateTeam);

                if (userExists)
                {
                    var updateUser = Builders<User>.Update
                        .Pull(u => u.TeamIds, teamId)
                        .Set(u => u.UpdatedAt, DateTime.UtcNow);

                    await _context.Users.UpdateOneAsync(u => u.Id == userId, updateUser);
                }

                return Ok(new { message = "User removed from team" });
            }

            if (userExists)
            {
                // Remove team from user's TeamIds when the team no longer references the user.
                var updateUser = Builders<User>.Update
                    .Pull(u => u.TeamIds, teamId)
                    .Set(u => u.UpdatedAt, DateTime.UtcNow);

                await _context.Users.UpdateOneAsync(u => u.Id == userId, updateUser);
            }

            return Ok(new { message = "User removed from team" });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var teams = await _context.Teams.Find(_ => true).ToListAsync();
            var dtos = teams.Select(MapToDetailDto).ToList();
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var team = await _context.Teams.Find(t => t.Id == id).FirstOrDefaultAsync();
            if (team == null)
                return NotFound();

            return Ok(MapToDetailDto(team));
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOrTeamOwner")]
        public async Task<IActionResult> Update(string id, CreateTeamDto dto)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            var team = await _context.Teams.Find(t => t.Id == id).FirstOrDefaultAsync();
            if (team == null)
                return NotFound();

            // Check authorization: must be admin or team owner
            var isAdmin = currentUserRole == UserRoles.Admin;
            var isTeamOwner = team.OwnerId == currentUserId;

            if (!isAdmin && !isTeamOwner)
                return Forbid();

            var update = Builders<Team>.Update
                .Set(t => t.Name, dto.Name)
                .Set(t => t.Description, dto.Description)
                .Set(t => t.UpdatedAt, DateTime.UtcNow);

            await _context.Teams.UpdateOneAsync(t => t.Id == id, update);

            var updated = await _context.Teams.Find(t => t.Id == id).FirstOrDefaultAsync();
            return Ok(MapToDetailDto(updated));
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _context.Teams.DeleteOneAsync(t => t.Id == id);
            if (result.DeletedCount == 0)
                return NotFound();

            // Remove this team from all users' TeamIds
            var updateFilter = Builders<User>.Update.Pull("TeamIds", id);
            await _context.Users.UpdateManyAsync(
                Builders<User>.Filter.AnyEq(u => u.TeamIds, id),
                updateFilter);

            return Ok(new { message = "Team deleted" });
        }

        private static TeamDetailDto MapToDetailDto(Team team)
        {
            return new TeamDetailDto
            {
                Id = team.Id,
                Name = team.Name,
                OwnerId = team.OwnerId,
                Description = team.Description,
                CreatedAt = team.CreatedAt,
                UpdatedAt = team.UpdatedAt,
                MemberIds = team.MemberIds
            };
        }
    }
}
