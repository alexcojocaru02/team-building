using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TeamConnect.Api.Modules.Feedback;
using TeamConnect.Api.Shared.DTOs;
using TeamConnect.Api.Shared.Models;
using TeamConnect.Api.Shared.Repositories;
using TeamConnect.Api.Tests.Fixtures;
using Xunit;

namespace TeamConnect.Api.Tests;

public class FeedbackControllerTests : IClassFixture<MongoFixture>
{
    private readonly MongoFixture _fixture;

    public FeedbackControllerTests(MongoFixture fixture)
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

    private FeedbackController BuildController() =>
        new FeedbackController(new FeedbackService(
            new FeedbackRepository(_fixture.Context),
            new UserRepository(_fixture.Context)));

    [Fact]
    public async Task Send_NonTeammates_ReturnsForbidden()
    {
        var db = _fixture.Context;
        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);
        await db.Feedbacks.DeleteManyAsync(FilterDefinition<Feedback>.Empty);

        var sender = new User
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Email = "sender@example.com",
            TeamIds = new List<string> { "team-a" }
        };
        var recipient = new User
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Email = "recipient@example.com",
            TeamIds = new List<string> { "team-b" }
        };
        await db.Users.InsertManyAsync(new[] { sender, recipient });

        var controller = BuildController();
        SetUser(controller, sender.Id);

        var result = await controller.Send(new CreateFeedbackDto
        {
            ToUserId = recipient.Id,
            Message = "Great work!"
        });

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, objectResult.StatusCode);
    }

    [Fact]
    public async Task Send_Teammates_ReturnsOk()
    {
        var db = _fixture.Context;
        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);
        await db.Feedbacks.DeleteManyAsync(FilterDefinition<Feedback>.Empty);

        var sharedTeamId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var sender = new User
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Email = "sender2@example.com",
            TeamIds = new List<string> { sharedTeamId }
        };
        var recipient = new User
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Email = "recipient2@example.com",
            TeamIds = new List<string> { sharedTeamId }
        };
        await db.Users.InsertManyAsync(new[] { sender, recipient });

        var controller = BuildController();
        SetUser(controller, sender.Id);

        var result = await controller.Send(new CreateFeedbackDto
        {
            ToUserId = recipient.Id,
            Message = "Great work!"
        });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Send_ToSelf_ReturnsBadRequest()
    {
        var db = _fixture.Context;
        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);

        var user = new User
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Email = "self@example.com",
            TeamIds = new List<string> { "team-x" }
        };
        await db.Users.InsertOneAsync(user);

        var controller = BuildController();
        SetUser(controller, user.Id);

        var result = await controller.Send(new CreateFeedbackDto
        {
            ToUserId = user.Id,
            Message = "Self-feedback"
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
