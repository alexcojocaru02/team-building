using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TeamConnect.Api.Modules.Dashboard;
using TeamConnect.Api.Modules.Feedback;
using TeamConnect.Api.Modules.Teams;
using TeamConnect.Api.Shared.Models;
using TeamConnect.Api.Shared.Repositories;
using TeamConnect.Api.Tests.Fixtures;
using Xunit;

namespace TeamConnect.Api.Tests;

public class DashboardControllerTests : IClassFixture<MongoFixture>
{
    private readonly MongoFixture _fixture;

    public DashboardControllerTests(MongoFixture fixture)
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
            HttpContext = new DefaultHttpContext { User = new System.Security.Claims.ClaimsPrincipal(identity) }
        };
    }

    private DashboardController BuildController() =>
        new DashboardController(
            new DashboardService(
                new FeedbackRepository(_fixture.Context),
                new UserRepository(_fixture.Context)),
            new TeamsService(
                new TeamRepository(_fixture.Context),
                new UserRepository(_fixture.Context)));

    [Fact]
    public async Task GetCohesion_NonOwnerNonAdmin_ReturnsForbidden()
    {
        var db = _fixture.Context;
        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);
        await db.Teams.DeleteManyAsync(FilterDefinition<Team>.Empty);

        var owner = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "owner@dash.com" };
        var other = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "other@dash.com" };
        await db.Users.InsertManyAsync(new[] { owner, other });

        var team = new Team
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            OwnerId = owner.Id,
            MemberIds = new List<string> { owner.Id, other.Id }
        };
        await db.Teams.InsertOneAsync(team);

        var controller = BuildController();
        SetUser(controller, other.Id);

        var result = await controller.GetCohesion(team.Id);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetCohesion_Owner_ReturnsOk()
    {
        var db = _fixture.Context;
        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);
        await db.Teams.DeleteManyAsync(FilterDefinition<Team>.Empty);
        await db.Feedbacks.DeleteManyAsync(FilterDefinition<Feedback>.Empty);

        var owner = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "owner2@dash.com" };
        await db.Users.InsertOneAsync(owner);

        var team = new Team
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            OwnerId = owner.Id,
            MemberIds = new List<string> { owner.Id }
        };
        await db.Teams.InsertOneAsync(team);

        var controller = BuildController();
        SetUser(controller, owner.Id);

        var result = await controller.GetCohesion(team.Id);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetCohesion_Admin_ReturnsOk()
    {
        var db = _fixture.Context;
        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);
        await db.Teams.DeleteManyAsync(FilterDefinition<Team>.Empty);
        await db.Feedbacks.DeleteManyAsync(FilterDefinition<Feedback>.Empty);

        var owner = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "owner3@dash.com" };
        var admin = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "admin@dash.com" };
        await db.Users.InsertManyAsync(new[] { owner, admin });

        var team = new Team
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            OwnerId = owner.Id,
            MemberIds = new List<string> { owner.Id }
        };
        await db.Teams.InsertOneAsync(team);

        var controller = BuildController();
        SetUser(controller, admin.Id, UserRoles.Admin);

        var result = await controller.GetCohesion(team.Id);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetCohesion_UnknownTeam_ReturnsNotFound()
    {
        var db = _fixture.Context;
        await db.Teams.DeleteManyAsync(FilterDefinition<Team>.Empty);

        var controller = BuildController();
        SetUser(controller, MongoDB.Bson.ObjectId.GenerateNewId().ToString());

        var result = await controller.GetCohesion(MongoDB.Bson.ObjectId.GenerateNewId().ToString());

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
