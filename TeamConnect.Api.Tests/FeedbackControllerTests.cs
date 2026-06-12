using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TeamConnect.Api.Modules.Feedback;
using TeamConnect.Api.Modules.Teams;
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
        new FeedbackController(
            new FeedbackService(
                new FeedbackRepository(_fixture.Context),
                new UserRepository(_fixture.Context)),
            new TeamsService(
                new TeamRepository(_fixture.Context),
                new UserRepository(_fixture.Context)));

    private static (User sender, User recipient) MakeTeammates(string teamId) => (
        new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = $"s-{teamId}@ex.com", TeamIds = new List<string> { teamId } },
        new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = $"r-{teamId}@ex.com", TeamIds = new List<string> { teamId } }
    );

    // ── Send: validation ────────────────────────────────────────────────────

    [Fact]
    public async Task Send_ToSelf_ReturnsBadRequest()
    {
        var db = _fixture.Context;
        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);

        var user = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "self@example.com", TeamIds = new List<string> { "team-x" } };
        await db.Users.InsertOneAsync(user);

        var controller = BuildController();
        SetUser(controller, user.Id);

        var result = await controller.Send(new CreateFeedbackDto { ToUserId = user.Id, Message = "Self-feedback" });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Send_EmptyMessage_ReturnsBadRequest()
    {
        var db = _fixture.Context;
        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);

        var user = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "empty-msg@example.com", TeamIds = new List<string> { "team-y" } };
        var other = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "other@example.com", TeamIds = new List<string> { "team-y" } };
        await db.Users.InsertManyAsync(new[] { user, other });

        var controller = BuildController();
        SetUser(controller, user.Id);

        var result = await controller.Send(new CreateFeedbackDto { ToUserId = other.Id, Message = "   " });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Send_EmptyToUserId_ReturnsBadRequest()
    {
        var db = _fixture.Context;
        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);

        var user = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "no-target@example.com", TeamIds = new List<string> { "team-z" } };
        await db.Users.InsertOneAsync(user);

        var controller = BuildController();
        SetUser(controller, user.Id);

        var result = await controller.Send(new CreateFeedbackDto { ToUserId = "", Message = "Hello" });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Send_NonTeammates_ReturnsForbidden()
    {
        var db = _fixture.Context;
        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);
        await db.Feedbacks.DeleteManyAsync(FilterDefinition<Feedback>.Empty);

        var sender = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "sender@example.com", TeamIds = new List<string> { "team-a" } };
        var recipient = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "recipient@example.com", TeamIds = new List<string> { "team-b" } };
        await db.Users.InsertManyAsync(new[] { sender, recipient });

        var controller = BuildController();
        SetUser(controller, sender.Id);

        var result = await controller.Send(new CreateFeedbackDto { ToUserId = recipient.Id, Message = "Great work!" });

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, objectResult.StatusCode);
    }

    // ── Send: happy path ────────────────────────────────────────────────────

    [Fact]
    public async Task Send_Teammates_ReturnsOk()
    {
        var db = _fixture.Context;
        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);
        await db.Feedbacks.DeleteManyAsync(FilterDefinition<Feedback>.Empty);

        var teamId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var (sender, recipient) = MakeTeammates(teamId);
        await db.Users.InsertManyAsync(new[] { sender, recipient });

        var controller = BuildController();
        SetUser(controller, sender.Id);

        var result = await controller.Send(new CreateFeedbackDto { ToUserId = recipient.Id, Message = "Great work!" });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Send_WithCategoryAndTone_PersistedInResponse()
    {
        var db = _fixture.Context;
        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);
        await db.Feedbacks.DeleteManyAsync(FilterDefinition<Feedback>.Empty);

        var teamId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var (sender, recipient) = MakeTeammates(teamId);
        await db.Users.InsertManyAsync(new[] { sender, recipient });

        var controller = BuildController();
        SetUser(controller, sender.Id);

        var result = await controller.Send(new CreateFeedbackDto
        {
            ToUserId = recipient.Id,
            Message = "Excellent delivery on the sprint.",
            Category = FeedbackCategory.Delivery,
            Tone = FeedbackTone.Positive,
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<FeedbackResponseDto>(ok.Value);
        Assert.Equal(FeedbackCategory.Delivery, dto.Category);
        Assert.Equal(FeedbackTone.Positive, dto.Tone);
        Assert.Equal("Excellent delivery on the sprint.", dto.Message);
    }

    [Fact]
    public async Task Send_DefaultCategoryAndTone_ReturnsCommunicationPositive()
    {
        var db = _fixture.Context;
        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);
        await db.Feedbacks.DeleteManyAsync(FilterDefinition<Feedback>.Empty);

        var teamId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var (sender, recipient) = MakeTeammates(teamId);
        await db.Users.InsertManyAsync(new[] { sender, recipient });

        var controller = BuildController();
        SetUser(controller, sender.Id);

        var result = await controller.Send(new CreateFeedbackDto { ToUserId = recipient.Id, Message = "Good job." });

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<FeedbackResponseDto>(ok.Value);
        Assert.Equal(FeedbackCategory.Communication, dto.Category);
        Assert.Equal(FeedbackTone.Positive, dto.Tone);
    }

    // ── GetReceived ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetReceived_ReturnsOnlyFeedbackAddressedToCurrentUser()
    {
        var db = _fixture.Context;
        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);
        await db.Feedbacks.DeleteManyAsync(FilterDefinition<Feedback>.Empty);

        var teamId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var (sender, recipient) = MakeTeammates(teamId);
        var unrelated = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "unrelated@ex.com", TeamIds = new List<string> { teamId } };
        await db.Users.InsertManyAsync(new[] { sender, recipient, unrelated });

        // Send feedback to recipient and to unrelated
        var senderCtrl = BuildController();
        SetUser(senderCtrl, sender.Id);
        await senderCtrl.Send(new CreateFeedbackDto { ToUserId = recipient.Id, Message = "For recipient" });
        await senderCtrl.Send(new CreateFeedbackDto { ToUserId = unrelated.Id, Message = "For unrelated" });

        // GetReceived as recipient
        var recipientCtrl = BuildController();
        SetUser(recipientCtrl, recipient.Id);
        var result = await recipientCtrl.GetReceived();

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<FeedbackResponseDto>>(ok.Value);
        var items = new List<FeedbackResponseDto>(list);

        Assert.Single(items);
        Assert.Equal("For recipient", items[0].Message);
        Assert.Equal(sender.Id, items[0].FromUserId);
    }

    [Fact]
    public async Task GetReceived_EmptyWhenNoFeedbackSent()
    {
        var db = _fixture.Context;
        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);
        await db.Feedbacks.DeleteManyAsync(FilterDefinition<Feedback>.Empty);

        var user = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "lonely@ex.com", TeamIds = new List<string>() };
        await db.Users.InsertOneAsync(user);

        var controller = BuildController();
        SetUser(controller, user.Id);

        var result = await controller.GetReceived();

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<FeedbackResponseDto>>(ok.Value);
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetReceived_MultipleFeedback_AllReturned()
    {
        var db = _fixture.Context;
        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);
        await db.Feedbacks.DeleteManyAsync(FilterDefinition<Feedback>.Empty);

        var teamId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var (sender, recipient) = MakeTeammates(teamId);
        var sender2 = new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Email = "sender2@ex.com", TeamIds = new List<string> { teamId } };
        await db.Users.InsertManyAsync(new[] { sender, recipient, sender2 });

        var ctrl1 = BuildController(); SetUser(ctrl1, sender.Id);
        await ctrl1.Send(new CreateFeedbackDto { ToUserId = recipient.Id, Message = "First", Category = FeedbackCategory.Leadership, Tone = FeedbackTone.Constructive });

        var ctrl2 = BuildController(); SetUser(ctrl2, sender2.Id);
        await ctrl2.Send(new CreateFeedbackDto { ToUserId = recipient.Id, Message = "Second", Category = FeedbackCategory.Collaboration, Tone = FeedbackTone.Critical });

        var recipientCtrl = BuildController(); SetUser(recipientCtrl, recipient.Id);
        var result = await recipientCtrl.GetReceived();

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = new List<FeedbackResponseDto>(Assert.IsAssignableFrom<IEnumerable<FeedbackResponseDto>>(ok.Value));

        Assert.Equal(2, list.Count);
        Assert.Contains(list, f => f.Category == FeedbackCategory.Leadership && f.Tone == FeedbackTone.Constructive);
        Assert.Contains(list, f => f.Category == FeedbackCategory.Collaboration && f.Tone == FeedbackTone.Critical);
    }
}
