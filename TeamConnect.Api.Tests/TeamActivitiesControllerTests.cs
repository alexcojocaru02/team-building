using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TeamConnect.Api.Modules.TeamActivities;
using TeamConnect.Api.Modules.Teams;
using TeamConnect.Api.Shared.DTOs;
using TeamConnect.Api.Shared.Models;
using TeamConnect.Api.Shared.Repositories;
using TeamConnect.Api.Tests.Fixtures;
using Xunit;

namespace TeamConnect.Api.Tests;

public class TeamActivitiesControllerTests : IClassFixture<MongoFixture>
{
    private readonly MongoFixture _fixture;

    public TeamActivitiesControllerTests(MongoFixture fixture)
    {
        _fixture = fixture;
    }

    private static void SetUser(ControllerBase controller, string userId, string role = null)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
        if (!string.IsNullOrWhiteSpace(role)) claims.Add(new Claim(ClaimTypes.Role, role));
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private TeamActivitiesController BuildController() =>
        new TeamActivitiesController(new TeamActivitiesService(
            new TeamActivityRepository(_fixture.Context),
            new UserRepository(_fixture.Context)));

    private TeamsService BuildTeamsService() =>
        new TeamsService(
            new TeamRepository(_fixture.Context),
            new UserRepository(_fixture.Context));

    [Fact]
    public async Task GetAll_NonMember_IsForbid()
    {
        var db = _fixture.Context;

        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);
        await db.Teams.DeleteManyAsync(FilterDefinition<Team>.Empty);

        var owner = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "owner@example.com" };
        await db.Users.InsertOneAsync(owner);

        var team = new Team { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), OwnerId = owner.Id, MemberIds = new List<string>() };
        await db.Teams.InsertOneAsync(team);

        var controller = BuildController();
        SetUser(controller, "random-user");

        var result = await controller.GetAll(team.Id, BuildTeamsService());

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Create_PollWithLessThanTwoOptions_ReturnsBadRequest()
    {
        var db = _fixture.Context;

        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);
        await db.Teams.DeleteManyAsync(FilterDefinition<Team>.Empty);

        var owner = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "owner2@example.com" };
        await db.Users.InsertOneAsync(owner);

        var team = new Team { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), OwnerId = owner.Id, MemberIds = new List<string>() };
        await db.Teams.InsertOneAsync(team);

        var controller = BuildController();
        SetUser(controller, owner.Id);

        var dto = new CreateTeamActivityDto
        {
            ActivityType = ActivityType.Poll,
            Title = "Test Poll",
            Description = "desc",
            Options = new List<string> { "only one" }
        };

        var result = await controller.Create(team.Id, dto, BuildTeamsService());

        Assert.IsType<BadRequestObjectResult>(result);
        var bad = result as BadRequestObjectResult;
        Assert.NotNull(bad);
        Assert.Contains("at least two", bad.Value?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_DueAtInPast_ReturnsBadRequest()
    {
        var db = _fixture.Context;

        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);
        await db.Teams.DeleteManyAsync(FilterDefinition<Team>.Empty);

        var owner = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "owner4@example.com" };
        await db.Users.InsertOneAsync(owner);

        var team = new Team { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), OwnerId = owner.Id, MemberIds = new List<string>() };
        await db.Teams.InsertOneAsync(team);

        var controller = BuildController();
        SetUser(controller, owner.Id);

        var dto = new CreateTeamActivityDto
        {
            ActivityType = ActivityType.Prompt,
            Title = "Past Due Prompt",
            Description = "desc",
            ScheduledEndAt = DateTime.UtcNow.AddMinutes(-1)
        };

        var result = await controller.Create(team.Id, dto, BuildTeamsService());

        Assert.IsType<BadRequestObjectResult>(result);
        var bad = result as BadRequestObjectResult;
        Assert.NotNull(bad);
        Assert.Contains("future", bad.Value?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_DueAtWithoutTimezone_ReturnsBadRequest()
    {
        var db = _fixture.Context;

        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);
        await db.Teams.DeleteManyAsync(FilterDefinition<Team>.Empty);

        var owner = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "owner5@example.com" };
        await db.Users.InsertOneAsync(owner);

        var team = new Team { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), OwnerId = owner.Id, MemberIds = new List<string>() };
        await db.Teams.InsertOneAsync(team);

        var controller = BuildController();
        SetUser(controller, owner.Id);

        var dto = new CreateTeamActivityDto
        {
            ActivityType = ActivityType.Prompt,
            Title = "Unspecified Due Prompt",
            Description = "desc",
            ScheduledEndAt = DateTime.SpecifyKind(DateTime.UtcNow.AddHours(1), DateTimeKind.Unspecified)
        };

        var result = await controller.Create(team.Id, dto, BuildTeamsService());

        Assert.IsType<BadRequestObjectResult>(result);
        var bad = result as BadRequestObjectResult;
        Assert.NotNull(bad);
        Assert.Contains("timezone", bad.Value?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Respond_SubmittingTwice_ReplacesPreviousResponse()
    {
        var db = _fixture.Context;

        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);
        await db.Teams.DeleteManyAsync(FilterDefinition<Team>.Empty);
        await db.TeamActivities.DeleteManyAsync(FilterDefinition<TeamActivity>.Empty);

        var owner = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "owner3@example.com" };
        var participant = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "user3@example.com" };
        await db.Users.InsertManyAsync(new[] { owner, participant });

        var team = new Team { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), OwnerId = owner.Id, MemberIds = new List<string> { participant.Id } };
        await db.Teams.InsertOneAsync(team);

        var activity = new TeamActivity
        {
            TeamId = team.Id,
            CreatedByUserId = owner.Id,
            ActivityType = ActivityType.Poll,
            Title = "Poll",
            Description = "Choose",
            Options = new List<string> { "A", "B" },
            Points = 10,
            Status = "Open",
            CreatedAt = DateTime.UtcNow
        };
        await db.TeamActivities.InsertOneAsync(activity);

        var controller = BuildController();
        SetUser(controller, participant.Id);

        var teamsService = BuildTeamsService();

        var resp1 = new SubmitTeamActivityResponseDto { SelectedOptionIndex = 0 };
        var r1 = await controller.Respond(team.Id, activity.Id, resp1, teamsService);
        Assert.IsType<OkObjectResult>(r1);

        var resp2 = new SubmitTeamActivityResponseDto { SelectedOptionIndex = 1 };
        var r2 = await controller.Respond(team.Id, activity.Id, resp2, teamsService);
        Assert.IsType<OkObjectResult>(r2);

        var updated = await db.TeamActivities.Find(a => a.Id == activity.Id).FirstOrDefaultAsync();
        Assert.NotNull(updated);
        Assert.Single(updated.Participations);
        Assert.Equal(1, updated.Participations.First().SelectedOptionIndex);
    }
}

