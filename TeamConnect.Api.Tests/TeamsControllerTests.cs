using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using TeamConnect.Api.Modules.Auth;
using TeamConnect.Api.Modules.Teams;
using TeamConnect.Api.Shared.DTOs;
using TeamConnect.Api.Shared.Models;
using TeamConnect.Api.Shared.Repositories;
using TeamConnect.Api.Tests.Fixtures;
using Xunit;

namespace TeamConnect.Api.Tests;

public class TeamsControllerTests : IClassFixture<MongoFixture>
{
    private readonly MongoFixture _fixture;

    public TeamsControllerTests(MongoFixture fixture)
    {
        _fixture = fixture;
    }

    private static void SetUser(ControllerBase controller, string userId, string? role = null)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
        if (!string.IsNullOrWhiteSpace(role)) claims.Add(new Claim(ClaimTypes.Role, role));
        var identity = new ClaimsIdentity(claims, "test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    private TeamsService BuildTeamsService() =>
        new TeamsService(
            new TeamRepository(_fixture.Context),
            new UserRepository(_fixture.Context),
            new TeamJoinRequestRepository(_fixture.Context),
            BuildJwtService());

    private static JwtService BuildJwtService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Jwt:Key", "super-secret-test-key-32-chars-ok!" },
                { "Jwt:Issuer", "test-issuer" },
                { "Jwt:Audience", "test-audience" }
            })
            .Build();
        return new JwtService(config, NullLogger<JwtService>.Instance);
    }

    private TeamsController BuildController() =>
        new TeamsController(BuildTeamsService());

    private async Task<(User user, Team team)> SeedOwnerAndTeam(string ownerEmail)
    {
        var db = _fixture.Context;
        var owner = new User
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Email = ownerEmail,
            Role = UserRoles.TeamOwner
        };
        await db.Users.InsertOneAsync(owner);
        var team = new Team
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            OwnerId = owner.Id,
            MemberIds = new List<string> { owner.Id }
        };
        await db.Teams.InsertOneAsync(team);
        return (owner, team);
    }

    // ---- Create Team ----

    [Fact]
    public async Task Create_RegularUser_ReturnsOkAndUpgradesRole()
    {
        var db = _fixture.Context;
        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);
        await db.Teams.DeleteManyAsync(FilterDefinition<Team>.Empty);

        var user = new User
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Email = "newowner@example.com",
            Role = UserRoles.User
        };
        await db.Users.InsertOneAsync(user);

        var controller = BuildController();
        SetUser(controller, user.Id);

        var result = await controller.Create(new CreateTeamDto { Name = "My Team" });

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<CreateTeamResponseDto>(ok.Value);
        Assert.Equal("My Team", response.Team.Name);
        Assert.Equal(user.Id, response.Team.OwnerId);
        Assert.NotNull(response.NewToken);

        // Role should have been upgraded in DB
        var updatedUser = await db.Users.Find(u => u.Id == user.Id).FirstOrDefaultAsync();
        Assert.Equal(UserRoles.TeamOwner, updatedUser?.Role);
    }

    [Fact]
    public async Task Create_TeamOwnerCreatesSecondTeam_NoNewToken()
    {
        var db = _fixture.Context;
        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);
        await db.Teams.DeleteManyAsync(FilterDefinition<Team>.Empty);

        var user = new User
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Email = "existingowner@example.com",
            Role = UserRoles.TeamOwner
        };
        await db.Users.InsertOneAsync(user);

        var controller = BuildController();
        SetUser(controller, user.Id);

        var result = await controller.Create(new CreateTeamDto { Name = "Second Team" });

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<CreateTeamResponseDto>(ok.Value);
        Assert.Equal("Second Team", response.Team.Name);
        Assert.Null(response.NewToken); // Already TeamOwner, no token refresh needed
    }

    [Fact]
    public async Task Create_MissingName_ReturnsBadRequest()
    {
        var controller = BuildController();
        SetUser(controller, MongoDB.Bson.ObjectId.GenerateNewId().ToString());

        var result = await controller.Create(new CreateTeamDto { Name = "" });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ---- Request to Join ----

    [Fact]
    public async Task RequestJoin_NonMember_ReturnsOk()
    {
        var db = _fixture.Context;
        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);
        await db.Teams.DeleteManyAsync(FilterDefinition<Team>.Empty);
        await db.TeamJoinRequests.DeleteManyAsync(FilterDefinition<TeamJoinRequest>.Empty);

        var (_, team) = await SeedOwnerAndTeam("owner1@join.com");
        var requester = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "req1@join.com" };
        await db.Users.InsertOneAsync(requester);

        var controller = BuildController();
        SetUser(controller, requester.Id);

        var result = await controller.RequestJoin(team.Id);

        Assert.IsType<OkObjectResult>(result);

        var saved = await db.TeamJoinRequests.Find(r => r.TeamId == team.Id && r.UserId == requester.Id).FirstOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal(JoinRequestStatus.Pending, saved.Status);
    }

    [Fact]
    public async Task RequestJoin_AlreadyMember_ReturnsBadRequest()
    {
        var db = _fixture.Context;
        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);
        await db.Teams.DeleteManyAsync(FilterDefinition<Team>.Empty);
        await db.TeamJoinRequests.DeleteManyAsync(FilterDefinition<TeamJoinRequest>.Empty);

        var (owner, team) = await SeedOwnerAndTeam("owner2@join.com");

        var controller = BuildController();
        SetUser(controller, owner.Id); // owner is already a member

        var result = await controller.RequestJoin(team.Id);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("already a member", bad.Value?.ToString() ?? string.Empty, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RequestJoin_DuplicateRequest_ReturnsBadRequest()
    {
        var db = _fixture.Context;
        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);
        await db.Teams.DeleteManyAsync(FilterDefinition<Team>.Empty);
        await db.TeamJoinRequests.DeleteManyAsync(FilterDefinition<TeamJoinRequest>.Empty);

        var (_, team) = await SeedOwnerAndTeam("owner3@join.com");
        var requester = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "req3@join.com" };
        await db.Users.InsertOneAsync(requester);

        var controller = BuildController();
        SetUser(controller, requester.Id);

        await controller.RequestJoin(team.Id); // first request
        var result = await controller.RequestJoin(team.Id); // duplicate

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("already have a pending", bad.Value?.ToString() ?? string.Empty, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RequestJoin_UnknownTeam_ReturnsNotFound()
    {
        var db = _fixture.Context;
        await db.TeamJoinRequests.DeleteManyAsync(FilterDefinition<TeamJoinRequest>.Empty);

        var controller = BuildController();
        SetUser(controller, MongoDB.Bson.ObjectId.GenerateNewId().ToString());

        var result = await controller.RequestJoin(MongoDB.Bson.ObjectId.GenerateNewId().ToString());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ---- Approve Join Request ----

    [Fact]
    public async Task ApproveJoinRequest_TeamOwner_AddsUserToTeam()
    {
        var db = _fixture.Context;
        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);
        await db.Teams.DeleteManyAsync(FilterDefinition<Team>.Empty);
        await db.TeamJoinRequests.DeleteManyAsync(FilterDefinition<TeamJoinRequest>.Empty);

        var (owner, team) = await SeedOwnerAndTeam("owner4@join.com");
        var requester = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "req4@join.com" };
        await db.Users.InsertOneAsync(requester);

        var joinRequest = new TeamJoinRequest
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            TeamId = team.Id,
            UserId = requester.Id,
            Status = JoinRequestStatus.Pending
        };
        await db.TeamJoinRequests.InsertOneAsync(joinRequest);

        var controller = BuildController();
        SetUser(controller, owner.Id, UserRoles.TeamOwner);

        var result = await controller.ApproveJoinRequest(joinRequest.Id);

        Assert.IsType<OkObjectResult>(result);

        var updated = await db.TeamJoinRequests.Find(r => r.Id == joinRequest.Id).FirstOrDefaultAsync();
        Assert.Equal(JoinRequestStatus.Approved, updated?.Status);

        var updatedTeam = await db.Teams.Find(t => t.Id == team.Id).FirstOrDefaultAsync();
        Assert.Contains(requester.Id, updatedTeam?.MemberIds ?? new List<string>());
    }

    [Fact]
    public async Task ApproveJoinRequest_NonOwnerNonAdmin_ReturnsForbid()
    {
        var db = _fixture.Context;
        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);
        await db.Teams.DeleteManyAsync(FilterDefinition<Team>.Empty);
        await db.TeamJoinRequests.DeleteManyAsync(FilterDefinition<TeamJoinRequest>.Empty);

        var (_, team) = await SeedOwnerAndTeam("owner5@join.com");
        var requester = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "req5@join.com" };
        var stranger = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "stranger5@join.com" };
        await db.Users.InsertManyAsync(new[] { requester, stranger });

        var joinRequest = new TeamJoinRequest
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            TeamId = team.Id,
            UserId = requester.Id,
            Status = JoinRequestStatus.Pending
        };
        await db.TeamJoinRequests.InsertOneAsync(joinRequest);

        var controller = BuildController();
        SetUser(controller, stranger.Id, UserRoles.User);

        var result = await controller.ApproveJoinRequest(joinRequest.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task ApproveJoinRequest_Admin_AddsUserToTeam()
    {
        var db = _fixture.Context;
        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);
        await db.Teams.DeleteManyAsync(FilterDefinition<Team>.Empty);
        await db.TeamJoinRequests.DeleteManyAsync(FilterDefinition<TeamJoinRequest>.Empty);

        var (_, team) = await SeedOwnerAndTeam("owner6@join.com");
        var requester = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "req6@join.com" };
        var admin = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "admin6@join.com", Role = UserRoles.Admin };
        await db.Users.InsertManyAsync(new[] { requester, admin });

        var joinRequest = new TeamJoinRequest
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            TeamId = team.Id,
            UserId = requester.Id,
            Status = JoinRequestStatus.Pending
        };
        await db.TeamJoinRequests.InsertOneAsync(joinRequest);

        var controller = BuildController();
        SetUser(controller, admin.Id, UserRoles.Admin);

        var result = await controller.ApproveJoinRequest(joinRequest.Id);

        Assert.IsType<OkObjectResult>(result);

        var updatedTeam = await db.Teams.Find(t => t.Id == team.Id).FirstOrDefaultAsync();
        Assert.Contains(requester.Id, updatedTeam?.MemberIds ?? new List<string>());
    }

    // ---- Reject Join Request ----

    [Fact]
    public async Task RejectJoinRequest_TeamOwner_SetsStatusRejected()
    {
        var db = _fixture.Context;
        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);
        await db.Teams.DeleteManyAsync(FilterDefinition<Team>.Empty);
        await db.TeamJoinRequests.DeleteManyAsync(FilterDefinition<TeamJoinRequest>.Empty);

        var (owner, team) = await SeedOwnerAndTeam("owner7@join.com");
        var requester = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "req7@join.com" };
        await db.Users.InsertOneAsync(requester);

        var joinRequest = new TeamJoinRequest
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            TeamId = team.Id,
            UserId = requester.Id,
            Status = JoinRequestStatus.Pending
        };
        await db.TeamJoinRequests.InsertOneAsync(joinRequest);

        var controller = BuildController();
        SetUser(controller, owner.Id, UserRoles.TeamOwner);

        var result = await controller.RejectJoinRequest(joinRequest.Id);

        Assert.IsType<OkObjectResult>(result);

        var updated = await db.TeamJoinRequests.Find(r => r.Id == joinRequest.Id).FirstOrDefaultAsync();
        Assert.Equal(JoinRequestStatus.Rejected, updated?.Status);

        // User should NOT have been added to team
        var updatedTeam = await db.Teams.Find(t => t.Id == team.Id).FirstOrDefaultAsync();
        Assert.DoesNotContain(requester.Id, updatedTeam?.MemberIds ?? new List<string>());
    }

    [Fact]
    public async Task RejectJoinRequest_NotFound_ReturnsNotFound()
    {
        var db = _fixture.Context;
        await db.TeamJoinRequests.DeleteManyAsync(FilterDefinition<TeamJoinRequest>.Empty);

        var controller = BuildController();
        SetUser(controller, MongoDB.Bson.ObjectId.GenerateNewId().ToString(), UserRoles.Admin);

        var result = await controller.RejectJoinRequest(MongoDB.Bson.ObjectId.GenerateNewId().ToString());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ---- Get Join Requests ----

    [Fact]
    public async Task GetTeamJoinRequests_Owner_ReturnsPendingList()
    {
        var db = _fixture.Context;
        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);
        await db.Teams.DeleteManyAsync(FilterDefinition<Team>.Empty);
        await db.TeamJoinRequests.DeleteManyAsync(FilterDefinition<TeamJoinRequest>.Empty);

        var (owner, team) = await SeedOwnerAndTeam("owner8@join.com");
        var requester = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "req8@join.com" };
        await db.Users.InsertOneAsync(requester);

        await db.TeamJoinRequests.InsertOneAsync(new TeamJoinRequest
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            TeamId = team.Id,
            UserId = requester.Id,
            Status = JoinRequestStatus.Pending
        });

        var controller = BuildController();
        SetUser(controller, owner.Id, UserRoles.TeamOwner);

        var result = await controller.GetTeamJoinRequests(team.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<TeamJoinRequestDto>>(ok.Value);
        Assert.Single(list);
        Assert.Equal(requester.Id, list[0].UserId);
    }

    [Fact]
    public async Task GetAllJoinRequests_Admin_ReturnsAllPending()
    {
        var db = _fixture.Context;
        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);
        await db.Teams.DeleteManyAsync(FilterDefinition<Team>.Empty);
        await db.TeamJoinRequests.DeleteManyAsync(FilterDefinition<TeamJoinRequest>.Empty);

        var (_, teamA) = await SeedOwnerAndTeam("ownerA@all.com");
        var (_, teamB) = await SeedOwnerAndTeam("ownerB@all.com");

        var reqA = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "reqA@all.com" };
        var reqB = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "reqB@all.com" };
        var admin = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "admin@all.com" };
        await db.Users.InsertManyAsync(new[] { reqA, reqB, admin });

        await db.TeamJoinRequests.InsertManyAsync(new[]
        {
            new TeamJoinRequest { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), TeamId = teamA.Id, UserId = reqA.Id, Status = JoinRequestStatus.Pending },
            new TeamJoinRequest { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), TeamId = teamB.Id, UserId = reqB.Id, Status = JoinRequestStatus.Pending },
        });

        var controller = BuildController();
        SetUser(controller, admin.Id, UserRoles.Admin);

        var result = await controller.GetAllJoinRequests();

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<TeamJoinRequestDto>>(ok.Value);
        Assert.Equal(2, list.Count);
    }
}
