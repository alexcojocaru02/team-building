using System;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using TeamConnect.Api.Shared.Models;
using TeamConnect.Api.Shared.Services;
using TeamConnect.Api.Tests.Fixtures;
using Xunit;

namespace TeamConnect.Api.Tests;

public class NotificationServiceTests : IClassFixture<MongoFixture>
{
    private readonly MongoFixture _fixture;

    public NotificationServiceTests(MongoFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_SendsMessageToUser()
    {
        var db = _fixture.Context;
        await ClearCollectionsAsync(db);

        var user = new User
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Email = "new-user@example.com",
            FullName = "New User"
        };

        await db.Users.InsertOneAsync(user);

        var sender = new CapturingEmailSender();
        var service = new NotificationService(db, sender, NullLogger<NotificationService>.Instance);

        await service.SendWelcomeEmailAsync(user.Id);

        Assert.Single(sender.Messages);
        var message = sender.Messages[0];
        Assert.Equal(user.Email, message.ToEmail);
        Assert.Contains("Welcome to TeamConnect", message.Subject);
        Assert.Contains("New User", message.TextBody);
    }

    [Fact]
    public async Task SendTeamActivityCreatedEmailAsync_NotifiesMembersExceptCreator()
    {
        var db = _fixture.Context;
        await ClearCollectionsAsync(db);

        var creator = new User
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Email = "creator@example.com",
            FullName = "Creator User"
        };

        var member = new User
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Email = "member@example.com",
            FullName = "Member User"
        };

        await db.Users.InsertManyAsync(new[] { creator, member });

        var team = new Team
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Name = "Design Team",
            OwnerId = creator.Id,
            MemberIds = new List<string> { creator.Id, member.Id }
        };

        await db.Teams.InsertOneAsync(team);

        var activity = new TeamActivity
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            TeamId = team.Id,
            CreatedByUserId = creator.Id,
            ActivityType = ActivityType.Prompt,
            Title = "Weekly check-in",
            Description = "Share your progress",
            Status = "Open",
            CreatedAt = DateTime.UtcNow
        };

        await db.TeamActivities.InsertOneAsync(activity);

        var sender = new CapturingEmailSender();
        var service = new NotificationService(db, sender, NullLogger<NotificationService>.Instance);

        await service.SendTeamActivityCreatedEmailAsync(team.Id, activity.Id, creator.Id);

        Assert.Single(sender.Messages);
        var message = sender.Messages[0];
        Assert.Equal(member.Email, message.ToEmail);
        Assert.Contains("Design Team", message.Subject);
        Assert.Contains("Weekly check-in", message.TextBody);
    }

    private static async Task ClearCollectionsAsync(MongoDbContext db)
    {
        await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty);
        await db.Teams.DeleteManyAsync(FilterDefinition<Team>.Empty);
        await db.TeamActivities.DeleteManyAsync(FilterDefinition<TeamActivity>.Empty);
    }

    private sealed class CapturingEmailSender : IEmailSender
    {
        public List<EmailMessage> Messages { get; } = new();

        public Task<bool> SendAsync(string toEmail, string subject, string htmlBody, string? textBody = null)
        {
            Messages.Add(new EmailMessage(toEmail, subject, htmlBody, textBody ?? string.Empty));
            return Task.FromResult(true);
        }
    }

    private sealed record EmailMessage(string ToEmail, string Subject, string HtmlBody, string TextBody);
}