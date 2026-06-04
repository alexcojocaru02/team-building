using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TeamConnect.Api.Modules.Teams;
using TeamConnect.Api.Modules.Users;
using TeamConnect.Api.Shared.Models;
using TeamConnect.Api.Shared.Repositories;
using TeamConnect.Api.Tests.Fixtures;
using Xunit;

namespace TeamConnect.Api.Tests;

public class UsersControllerTests : IClassFixture<MongoFixture>
{
    private readonly MongoFixture _fixture;

    public UsersControllerTests(MongoFixture fixture)
    {
        _fixture = fixture;
    }

    private static void SetUser(ControllerBase controller, string userId)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new System.Security.Claims.ClaimsPrincipal(identity) }
        };
    }

    private UsersController BuildController() =>
        new UsersController(new UsersService(
            new UserRepository(_fixture.Context),
            new TeamRepository(_fixture.Context)));

    [Fact]
    public async Task GetTeammatesForTeam_NonMember_ReturnsForbidden()
    {
        var db = _fixture.Context;
        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);
        await db.Teams.DeleteManyAsync(FilterDefinition<Team>.Empty);

        var owner = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "owner@test.com" };
        var outsider = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "outsider@test.com" };
        await db.Users.InsertManyAsync(new[] { owner, outsider });

        var team = new Team
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            OwnerId = owner.Id,
            MemberIds = new List<string> { owner.Id }
        };
        await db.Teams.InsertOneAsync(team);

        var controller = BuildController();
        SetUser(controller, outsider.Id);

        var result = await controller.GetTeammatesForTeam(team.Id);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetTeammatesForTeam_Member_ReturnsOtherMembers()
    {
        var db = _fixture.Context;
        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);
        await db.Teams.DeleteManyAsync(FilterDefinition<Team>.Empty);

        var member1 = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "m1@test.com" };
        var member2 = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "m2@test.com" };
        await db.Users.InsertManyAsync(new[] { member1, member2 });

        var team = new Team
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            OwnerId = member1.Id,
            MemberIds = new List<string> { member1.Id, member2.Id }
        };
        await db.Teams.InsertOneAsync(team);

        var controller = BuildController();
        SetUser(controller, member1.Id);

        var result = await controller.GetTeammatesForTeam(team.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var teammates = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value);
        var list = new List<object>();
        foreach (var item in teammates) list.Add(item);
        Assert.Single(list);
    }

    [Fact]
    public async Task GetTeammatesForTeam_UnknownTeam_ReturnsForbidden()
    {
        var db = _fixture.Context;
        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);
        await db.Teams.DeleteManyAsync(FilterDefinition<Team>.Empty);

        var user = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "u@test.com" };
        await db.Users.InsertOneAsync(user);

        var controller = BuildController();
        SetUser(controller, user.Id);

        var result = await controller.GetTeammatesForTeam(MongoDB.Bson.ObjectId.GenerateNewId().ToString());

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, objectResult.StatusCode);
    }
}
