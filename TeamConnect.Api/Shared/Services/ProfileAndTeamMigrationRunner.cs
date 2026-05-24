using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;
using TeamConnect.Api.Shared.Models;

namespace TeamConnect.Api.Shared.Services
{
    public class ProfileAndTeamMigrationRunner
    {
        private readonly MongoDbContext _context;

        public ProfileAndTeamMigrationRunner(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<MigrationReport> RunAsync(string version, string mode, CancellationToken ct = default)
        {
            var normalizedMode = (mode ?? string.Empty).Trim().ToLowerInvariant();

            if (normalizedMode is not ("dry-run" or "apply" or "verify"))
            {
                throw new ArgumentException("Mode must be one of: dry-run, apply, verify.", nameof(mode));
            }

            var report = await AnalyzeAsync(ct);
            report.Mode = normalizedMode;
            report.Version = version;

            if (normalizedMode == "apply")
            {
                await ApplyAsync(report, ct);
                report = await AnalyzeAsync(ct);
                report.Mode = normalizedMode;
                report.Version = version;
                report.AppliedAtUtc = DateTime.UtcNow;

                var summaryJson = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = false });
                var existing = await _context.SchemaMigrations.Find(x => x.Version == version).FirstOrDefaultAsync(ct);

                if (existing == null)
                {
                    await _context.SchemaMigrations.InsertOneAsync(new SchemaMigrationRecord
                    {
                        Version = version,
                        AppliedAtUtc = report.AppliedAtUtc.Value,
                        Mode = normalizedMode,
                        SummaryJson = summaryJson
                    }, cancellationToken: ct);
                }
                else
                {
                    var update = Builders<SchemaMigrationRecord>.Update
                        .Set(x => x.AppliedAtUtc, report.AppliedAtUtc.Value)
                        .Set(x => x.Mode, normalizedMode)
                        .Set(x => x.SummaryJson, summaryJson);

                    await _context.SchemaMigrations.UpdateOneAsync(x => x.Id == existing.Id, update, cancellationToken: ct);
                }
            }

            return report;
        }

        private async Task<MigrationReport> AnalyzeAsync(CancellationToken ct)
        {
            var report = new MigrationReport();
            var users = await _context.Database.GetCollection<BsonDocument>("Users").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync(ct);
            var teams = await _context.Database.GetCollection<BsonDocument>("Teams").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync(ct);

            report.TotalUsers = users.Count;
            report.TotalTeams = teams.Count;

            var normalizedRoleByUserId = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var user in users)
            {
                var id = user.GetValue("_id", BsonNull.Value).ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(id)) continue;

                var currentRole = user.Contains("Role") && user["Role"].IsString
                    ? user["Role"].AsString
                    : UserRoles.User;
                var normalizedRole = UserRoles.Normalize(currentRole);
                normalizedRoleByUserId[id] = normalizedRole;

                if (!string.Equals(currentRole, normalizedRole, StringComparison.Ordinal))
                {
                    report.UsersWithInvalidRole++;
                }

                foreach (var field in RequiredUserFields)
                {
                    if (!user.Contains(field)) report.UsersMissingProfileFields++;
                }
            }

            var teamMembership = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

            foreach (var team in teams)
            {
                if (!team.Contains("OwnerId") || team["OwnerId"].IsBsonNull || (team["OwnerId"].IsString && string.IsNullOrWhiteSpace(team["OwnerId"].AsString)))
                {
                    report.TeamsMissingOwner++;
                }

                var hasMissingMetadata = false;
                foreach (var field in RequiredTeamFields)
                {
                    if (!team.Contains(field))
                    {
                        hasMissingMetadata = true;
                    }
                }

                if (hasMissingMetadata)
                {
                    report.TeamsMissingMetadata++;
                }

                if (!team.Contains("MemberIds") || !team["MemberIds"].IsBsonArray) continue;

                foreach (var member in team["MemberIds"].AsBsonArray)
                {
                    var userId = member.ToString();
                    if (string.IsNullOrWhiteSpace(userId)) continue;

                    if (!teamMembership.TryGetValue(userId, out var set))
                    {
                        set = new HashSet<string>(StringComparer.Ordinal);
                        teamMembership[userId] = set;
                    }

                    var teamId = team.GetValue("_id", BsonNull.Value).ToString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(teamId))
                    {
                        set.Add(teamId);
                    }
                }
            }

            foreach (var user in users)
            {
                var id = user.GetValue("_id", BsonNull.Value).ToString();
                if (string.IsNullOrWhiteSpace(id)) continue;

                var expected = teamMembership.TryGetValue(id, out var set)
                    ? set
                    : new HashSet<string>(StringComparer.Ordinal);

                var current = new HashSet<string>(StringComparer.Ordinal);
                if (user.Contains("TeamIds") && user["TeamIds"].IsBsonArray)
                {
                    foreach (var teamId in user["TeamIds"].AsBsonArray)
                    {
                        if (!string.IsNullOrWhiteSpace(teamId.ToString()))
                        {
                            current.Add(teamId.ToString() ?? string.Empty);
                        }
                    }
                }

                if (!current.SetEquals(expected))
                {
                    report.UsersWithTeamMembershipDrift++;
                    if (report.SampleUserIdsWithMembershipDrift.Count < 25)
                    {
                        report.SampleUserIdsWithMembershipDrift.Add(id);
                    }
                }
            }

            return report;
        }

        private async Task ApplyAsync(MigrationReport report, CancellationToken ct)
        {
            var usersCollection = _context.Database.GetCollection<BsonDocument>("Users");
            var teamsCollection = _context.Database.GetCollection<BsonDocument>("Teams");

            var users = await usersCollection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync(ct);
            var teams = await teamsCollection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync(ct);

            var normalizedRoleByUserId = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var user in users)
            {
                var userId = user.GetValue("_id", BsonNull.Value).ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(userId)) continue;

                var currentRole = user.Contains("Role") && user["Role"].IsString
                    ? user["Role"].AsString
                    : UserRoles.User;
                var normalizedRole = UserRoles.Normalize(currentRole);
                normalizedRoleByUserId[userId] = normalizedRole;
                var updates = new List<UpdateDefinition<BsonDocument>>
                {
                    Builders<BsonDocument>.Update.Set("Role", normalizedRole)
                };

                EnsureField(updates, user, "Bio", BsonNull.Value);
                EnsureField(updates, user, "AvatarUrl", BsonNull.Value);
                EnsureField(updates, user, "Department", BsonNull.Value);
                EnsureField(updates, user, "Location", BsonNull.Value);
                EnsureField(updates, user, "Timezone", BsonNull.Value);
                EnsureField(updates, user, "Pronouns", BsonNull.Value);
                EnsureField(updates, user, "PreferredWorkStyle", BsonNull.Value);
                EnsureField(updates, user, "Icebreaker", BsonNull.Value);
                EnsureField(updates, user, "Hobbies", new BsonArray());
                EnsureField(updates, user, "Strengths", new BsonArray());
                EnsureField(updates, user, "TeamIds", new BsonArray());
                EnsureField(updates, user, "UpdatedAt", DateTime.UtcNow);

                if (updates.Count > 0)
                {
                    await usersCollection.UpdateOneAsync(
                        Builders<BsonDocument>.Filter.Eq("_id", user["_id"]),
                        Builders<BsonDocument>.Update.Combine(updates),
                        cancellationToken: ct);
                }
            }

            var membershipsByUser = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (var team in teams)
            {
                var teamId = team.GetValue("_id", BsonNull.Value).ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(teamId)) continue;

                var teamUpdates = new List<UpdateDefinition<BsonDocument>>();
                EnsureField(teamUpdates, team, "Description", BsonNull.Value);
                EnsureField(teamUpdates, team, "CreatedAt", DateTime.UtcNow);
                teamUpdates.Add(Builders<BsonDocument>.Update.Set("UpdatedAt", DateTime.UtcNow));
                EnsureField(teamUpdates, team, "MemberIds", new BsonArray());

                var hasOwner = team.Contains("OwnerId")
                    && !team["OwnerId"].IsBsonNull
                    && (!team["OwnerId"].IsString || !string.IsNullOrWhiteSpace(team["OwnerId"].AsString));

                if (!hasOwner && team.Contains("MemberIds") && team["MemberIds"].IsBsonArray)
                {
                    string? chosenOwnerId = null;
                    foreach (var member in team["MemberIds"].AsBsonArray)
                    {
                        var memberId = member.ToString();
                        if (string.IsNullOrWhiteSpace(memberId)) continue;

                        if (normalizedRoleByUserId.TryGetValue(memberId, out var role) && role == UserRoles.Admin)
                        {
                            chosenOwnerId = memberId;
                            break;
                        }
                    }

                    var ownerValue = chosenOwnerId == null
                        ? (BsonValue)BsonNull.Value
                        : new BsonString(chosenOwnerId);
                    teamUpdates.Add(Builders<BsonDocument>.Update.Set("OwnerId", ownerValue));
                }

                if (team.Contains("MemberIds") && team["MemberIds"].IsBsonArray)
                {
                    foreach (var member in team["MemberIds"].AsBsonArray)
                    {
                        var memberId = member.ToString();
                        if (string.IsNullOrWhiteSpace(memberId)) continue;

                        if (!membershipsByUser.TryGetValue(memberId, out var set))
                        {
                            set = new HashSet<string>(StringComparer.Ordinal);
                            membershipsByUser[memberId] = set;
                        }
                        set.Add(teamId);
                    }
                }

                if (teamUpdates.Count > 0)
                {
                    await teamsCollection.UpdateOneAsync(
                        Builders<BsonDocument>.Filter.Eq("_id", team["_id"]),
                        Builders<BsonDocument>.Update.Combine(teamUpdates),
                        cancellationToken: ct);
                }
            }

            foreach (var user in users)
            {
                var userId = user.GetValue("_id", BsonNull.Value).ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(userId)) continue;

                membershipsByUser.TryGetValue(userId, out var set);
                set ??= new HashSet<string>(StringComparer.Ordinal);

                var teamIdArray = new BsonArray(set.OrderBy(x => x));

                var update = Builders<BsonDocument>.Update
                    .Set("TeamIds", teamIdArray)
                    .Set("UpdatedAt", DateTime.UtcNow);

                await usersCollection.UpdateOneAsync(
                    Builders<BsonDocument>.Filter.Eq("_id", user["_id"]),
                    update,
                    cancellationToken: ct);
            }

            report.AppliedAtUtc = DateTime.UtcNow;
        }

        private static void EnsureField(List<UpdateDefinition<BsonDocument>> updates, BsonDocument doc, string fieldName, BsonValue defaultValue)
        {
            if (!doc.Contains(fieldName))
            {
                updates.Add(Builders<BsonDocument>.Update.Set(fieldName, defaultValue));
            }
        }

        private static readonly string[] RequiredUserFields =
        {
            "Bio", "AvatarUrl", "Department", "Location", "Timezone", "Pronouns",
            "PreferredWorkStyle", "Hobbies", "Strengths", "Icebreaker", "UpdatedAt"
        };

        private static readonly string[] RequiredTeamFields =
        {
            "Description", "CreatedAt", "UpdatedAt", "OwnerId"
        };
    }

    public class MigrationReport
    {
        public string Version { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;
        public DateTime? AppliedAtUtc { get; set; }

        public int TotalUsers { get; set; }
        public int TotalTeams { get; set; }

        public int UsersMissingProfileFields { get; set; }
        public int UsersWithInvalidRole { get; set; }
        public int UsersWithTeamMembershipDrift { get; set; }

        public int TeamsMissingOwner { get; set; }
        public int TeamsMissingMetadata { get; set; }

        public List<string> SampleUserIdsWithMembershipDrift { get; set; } = new();
    }
}