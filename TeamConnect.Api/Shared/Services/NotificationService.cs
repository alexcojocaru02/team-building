using MongoDB.Driver;
using System.Net;
using TeamConnect.Api.Shared.Models;

namespace TeamConnect.Api.Shared.Services
{
    public class NotificationService : INotificationService
    {
        private readonly MongoDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(MongoDbContext context, IEmailSender emailSender, ILogger<NotificationService> logger)
        {
            _context = context;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task SendWelcomeEmailAsync(string userId)
        {
            var user = await GetUserAsync(userId);
            if (user == null)
            {
                return;
            }

            await SendAsync(
                user.Email,
                $"Welcome to TeamConnect, {DisplayName(user)}",
                $@"
<h2>Welcome to TeamConnect</h2>
<p>Hi {Encode(DisplayName(user))},</p>
<p>Your account is ready. You can now sign in and start collaborating with your team.</p>
<p>Weâ€™re glad to have you on board.</p>
<p>TeamConnect</p>",
                $"Welcome to TeamConnect\n\nHi {DisplayName(user)},\nYour account is ready. You can now sign in and start collaborating with your team.\n\nTeamConnect");
        }

        public async Task SendTeamMemberAddedEmailAsync(string teamId, string userId, string addedByUserId)
        {
            var team = await GetTeamAsync(teamId);
            var user = await GetUserAsync(userId);
            var addedByUser = await GetUserAsync(addedByUserId);

            if (team == null || user == null)
            {
                return;
            }

            var actorName = addedByUser != null ? DisplayName(addedByUser) : "a team admin";

            await SendAsync(
                user.Email,
                $"You were added to {team.Name}",
                $@"
<h2>You were added to a team</h2>
<p>Hi {Encode(DisplayName(user))},</p>
<p>{Encode(actorName)} added you to <strong>{Encode(team.Name)}</strong>.</p>
<p>You can now view the team in TeamConnect and start participating right away.</p>
<p>TeamConnect</p>",
                $"Hi {DisplayName(user)},\n\n{actorName} added you to {team.Name}. You can now view the team in TeamConnect and start participating right away.\n\nTeamConnect");
        }

        public async Task SendFeedbackReceivedEmailAsync(string fromUserId, string toUserId, string message)
        {
            var fromUser = await GetUserAsync(fromUserId);
            var toUser = await GetUserAsync(toUserId);

            if (fromUser == null || toUser == null)
            {
                return;
            }

            var senderName = DisplayName(fromUser);

            await SendAsync(
                toUser.Email,
                $"New feedback from {senderName}",
                $@"
<h2>New feedback received</h2>
<p>Hi {Encode(DisplayName(toUser))},</p>
<p>You received new feedback from <strong>{Encode(senderName)}</strong>.</p>
<blockquote style='border-left: 4px solid #ccc; margin: 16px 0; padding-left: 12px;'>{Encode(message)}</blockquote>
<p>Open TeamConnect to reply or review it.</p>
<p>TeamConnect</p>",
                $"Hi {DisplayName(toUser)},\n\nYou received new feedback from {senderName}.\n\n{message}\n\nOpen TeamConnect to reply or review it.\n\nTeamConnect");
        }

        public async Task SendTeamActivityCreatedEmailAsync(string teamId, string activityId, string createdByUserId)
        {
            var team = await GetTeamAsync(teamId);
            var activity = await GetActivityAsync(activityId);
            var creator = await GetUserAsync(createdByUserId);

            if (team == null || activity == null || creator == null)
            {
                return;
            }

            var recipientIds = team.MemberIds
                .Where(memberId => !string.IsNullOrWhiteSpace(memberId) && memberId != createdByUserId)
                .Distinct()
                .ToList();

            if (recipientIds.Count == 0)
            {
                return;
            }

            var recipients = await GetUsersAsync(recipientIds);

            if (activity.ActivityType == ActivityType.SyncMeeting)
            {
                var meetingSubject = $"New sync meeting in {team.Name}: {activity.Title}";

                foreach (var recipient in recipients.Values)
                {
                    await SendAsync(
                        recipient.Email,
                        meetingSubject,
                        $@"
<h2>New sync meeting scheduled</h2>
<p>Hi {Encode(DisplayName(recipient))},</p>
<p><strong>{Encode(DisplayName(creator))}</strong> scheduled a new sync meeting in <strong>{Encode(team.Name)}</strong>.</p>
<p><strong>{Encode(activity.Title)}</strong></p>
<p>{Encode(activity.Description)}</p>
{BuildScheduledAtBlock(activity)}
{BuildMeetingLinkBlock(activity)}
<p>Open TeamConnect to RSVP.</p>
<p>TeamConnect</p>",
                        $"Hi {DisplayName(recipient)},\n\n{DisplayName(creator)} scheduled a new sync meeting in {team.Name}.\n\n{activity.Title}\n{activity.Description}\n{BuildScheduledAtText(activity)}\n{BuildMeetingLinkText(activity)}\n\nOpen TeamConnect to RSVP.\n\nTeamConnect");
                }

                return;
            }

            var subject = $"New {activity.ActivityType} activity in {team.Name}";

            foreach (var recipient in recipients.Values)
            {
                await SendAsync(
                    recipient.Email,
                    subject,
                    $@"
<h2>New team activity</h2>
<p>Hi {Encode(DisplayName(recipient))},</p>
<p><strong>{Encode(DisplayName(creator))}</strong> created a new activity in <strong>{Encode(team.Name)}</strong>.</p>
<p><strong>{Encode(activity.Title)}</strong></p>
<p>{Encode(activity.Description)}</p>
{BuildDueDateBlock(activity)}
<p>Open TeamConnect to respond.</p>
<p>TeamConnect</p>",
                    $"Hi {DisplayName(recipient)},\n\n{DisplayName(creator)} created a new activity in {team.Name}.\n\n{activity.Title}\n{activity.Description}\n{BuildDueDateText(activity)}\n\nOpen TeamConnect to respond.\n\nTeamConnect");
            }
        }

        private async Task SendAsync(string toEmail, string subject, string htmlBody, string textBody)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("Skipping notification because the recipient email is empty.");
                return;
            }

            await _emailSender.SendAsync(toEmail, subject, htmlBody, textBody);
        }

        private async Task<User?> GetUserAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            return await _context.Users.Find(user => user.Id == userId).FirstOrDefaultAsync();
        }

        private async Task<Team?> GetTeamAsync(string teamId)
        {
            if (string.IsNullOrWhiteSpace(teamId))
            {
                return null;
            }

            return await _context.Teams.Find(team => team.Id == teamId).FirstOrDefaultAsync();
        }

        private async Task<TeamActivity?> GetActivityAsync(string activityId)
        {
            if (string.IsNullOrWhiteSpace(activityId))
            {
                return null;
            }

            return await _context.TeamActivities.Find(activity => activity.Id == activityId).FirstOrDefaultAsync();
        }

        private async Task<Dictionary<string, User>> GetUsersAsync(List<string> userIds)
        {
            var users = await _context.Users
                .Find(user => userIds.Contains(user.Id))
                .ToListAsync();

            return users.ToDictionary(user => user.Id, user => user);
        }

        private static string DisplayName(User user)
        {
            return string.IsNullOrWhiteSpace(user.FullName) ? user.Email : user.FullName;
        }

        private static string Encode(string? value)
        {
            return WebUtility.HtmlEncode(value ?? string.Empty);
        }

        private static string BuildDueDateBlock(TeamActivity activity)
        {
            if (!activity.ScheduledEndAt.HasValue)
            {
                return string.Empty;
            }

            return $"<p><strong>Due:</strong> {Encode(activity.ScheduledEndAt.Value.ToUniversalTime().ToString("u"))}</p>";
        }

        private static string BuildDueDateText(TeamActivity activity)
        {
            return activity.ScheduledEndAt.HasValue
                ? $"Due: {activity.ScheduledEndAt.Value.ToUniversalTime():u}"
                : string.Empty;
        }

        private static string BuildScheduledAtBlock(TeamActivity activity)
        {
            if (!activity.ScheduledAt.HasValue)
            {
                return string.Empty;
            }

            var when = Encode(activity.ScheduledAt.Value.ToUniversalTime().ToString("u"));
            if (activity.ScheduledEndAt.HasValue)
            {
                when += $" â€“ {Encode(activity.ScheduledEndAt.Value.ToUniversalTime().ToString("u"))}";
            }

            return $"<p><strong>When:</strong> {when}</p>";
        }

        private static string BuildScheduledAtText(TeamActivity activity)
        {
            if (!activity.ScheduledAt.HasValue)
            {
                return string.Empty;
            }

            var when = $"{activity.ScheduledAt.Value.ToUniversalTime():u}";
            if (activity.ScheduledEndAt.HasValue)
            {
                when += $" â€“ {activity.ScheduledEndAt.Value.ToUniversalTime():u}";
            }

            return $"When: {when}";
        }

        private static string BuildMeetingLinkBlock(TeamActivity activity)
        {
            if (string.IsNullOrWhiteSpace(activity.MeetingLink))
            {
                return string.Empty;
            }

            var encodedLink = Encode(activity.MeetingLink);
            return $"<p><a href=\"{encodedLink}\">Join the meeting</a></p>";
        }

        private static string BuildMeetingLinkText(TeamActivity activity)
        {
            return string.IsNullOrWhiteSpace(activity.MeetingLink)
                ? string.Empty
                : $"Join the meeting: {activity.MeetingLink}";
        }
    }
}
