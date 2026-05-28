# TeamConnect Profile & Team Management API

## Overview

This document describes the new profile management and team assignment features added to TeamConnect.Api, along with the data migration procedure.

---

## Email Notifications

TeamConnect.Api can send notification emails for registration, team membership changes, feedback, and team activity creation.

### Gmail SMTP Setup for Local Development

Use your Gmail address as the sender and provide the app password through an environment variable instead of committing it to source control:

```bash
set Email__Smtp__Password=your-gmail-app-password
set Email__Enabled=true
```

The dev config already points to Gmail SMTP using:

- Host: `smtp.gmail.com`
- Port: `587`
- SSL: `true`
- Username: `teamconnectapp1@gmail.com`
- From address: `teamconnectapp1@gmail.com`

### Local Smoke Test

1. Start the API in development mode.
2. Register a user or add a user to a team to trigger an email.
3. Check the recipient inbox for the message.
4. If Gmail rejects the send, verify the account has 2-step verification enabled and that the password is a Gmail app password, not the normal account password.

### Production / Release

The release environment should use `appsettings.Production.json` plus a secret environment variable for the Gmail app password:

- `Email__Smtp__Password=your-gmail-app-password`

The deployed app will read the same Gmail SMTP host, port, username, and sender address as local development.

---

## Schema Changes

### User Model Extensions

New optional fields added to `User` model to enable interpersonal relationship building:

- **Bio** (string, nullable): Short personal summary
- **AvatarUrl** (string, nullable): URL to user's profile image
- **Department** (string, nullable): User's department or role
- **Location** (string, nullable): City or timezone info
- **Timezone** (string, nullable): Preferred timezone
- **Pronouns** (string, nullable): Preferred pronouns (e.g., "he/him")
- **PreferredWorkStyle** (string, nullable): Work style preference (e.g., "async", "sync")
- **Hobbies** (string[], default: []): List of hobbies or interests
- **Strengths** (string[], default: []): Key skills or strengths
- **Icebreaker** (string, nullable): Fun fact or icebreaker question
- **UpdatedAt** (DateTime, nullable): Last profile update timestamp
- **Role** (string, changed from "Admin"/"User" to normalized enum): Now supports "Admin", "User", "TeamOwner"

### Team Model Extensions

New fields added to `Team` model for ownership and governance:

- **OwnerId** (string, nullable): ID of team owner (admin or team owner role)
- **Description** (string, nullable): Team description
- **CreatedAt** (DateTime, nullable): Team creation timestamp
- **UpdatedAt** (DateTime, nullable): Last team update timestamp

---

## New Endpoints

### User Profile Management

#### Get User Profile
```
GET /api/users/{id}
Authorization: Bearer <token>
Response: UserProfileDto
```

Returns full user profile with interpersonal fields. Accessible to all authenticated users.

#### Update User Profile
```
PUT /api/users/{id}
Authorization: Bearer <token>
Content-Type: application/json

Request Body: UpdateProfileDto {
  bio?: string,
  avatarUrl?: string,
  department?: string,
  location?: string,
  timezone?: string,
  pronouns?: string,
  preferredWorkStyle?: string,
  hobbies?: string[],
  strengths?: string[],
  icebreaker?: string
}

Response: UserProfileDto
```

Users can update their own profile. Admins can update any user's profile.

### Team Management

#### Create Team
```
POST /api/teams
Authorization: Bearer <token> (Admin role required)
Content-Type: application/json

Request Body: CreateTeamDto {
  name: string,
  description?: string
}

Response: TeamDetailDto
```

Creates a new team. Only admins can create teams. Creator is automatically assigned as team owner.

#### Get All Teams
```
GET /api/teams
Authorization: Bearer <token>
Response: TeamDetailDto[]
```

Lists all teams. Accessible to all authenticated users.

#### Get Team Details
```
GET /api/teams/{id}
Authorization: Bearer <token>
Response: TeamDetailDto
```

Returns full team details including owner and members.

#### Update Team
```
PUT /api/teams/{id}
Authorization: Bearer <token> (Admin or Team Owner required)
Content-Type: application/json

Request Body: CreateTeamDto {
  name: string,
  description?: string
}

Response: TeamDetailDto
```

Only admins or team owner can update team information.

#### Add Member to Team
```
POST /api/teams/{teamId}/add/{userId}
Authorization: Bearer <token> (Admin or Team Owner required)
Response: { message: "User added to team" }
```

Adds a user to a team. Admin or team owner can manage membership.

#### Remove Member from Team
```
DELETE /api/teams/{teamId}/members/{userId}
Authorization: Bearer <token> (Admin or Team Owner required)
Response: { message: "User removed from team" }
```

Removes a user from a team. Admin or team owner can manage membership.

#### Delete Team
```
DELETE /api/teams/{id}
Authorization: Bearer <token> (Admin role required)
Response: { message: "Team deleted" }
```

Deletes a team. Only admins can delete teams. Automatically removes team from all members' team lists.

---

## Data Migration

### Overview

A one-off migration process normalizes existing user/team data, adds new fields, and reconciles team membership. The migration is **not automatic** and must be run manually.

### Migration Modes

#### Dry Run (Analysis Only)
```bash
cd TeamConnect.Api
dotnet run --migration-mode=dry-run --migration-version=2026-05-06-profile-team-v1
```

Analyzes data without making changes. Outputs:
- Total user and team counts
- Count of users missing profile fields
- Count of users with invalid role strings
- Count of teams missing owner/metadata
- Count of users with membership drift (TeamIds vs Team.MemberIds mismatch)
- Sample IDs of users with membership drift (for manual inspection)

**Run this first on staging to inspect the data report.**

#### Apply (Execute Migration)
```bash
cd TeamConnect.Api
dotnet run --migration-mode=apply --migration-version=2026-05-06-profile-team-v1
```

**⚠️ BACKUP YOUR DATABASE BEFORE RUNNING THIS!**

Applies all data transformations:
1. Normalizes role strings to "Admin", "User", or "TeamOwner"
2. Initializes missing profile fields with null/empty defaults
3. Backfills User.TeamIds from Team.MemberIds (canonical source)
4. Auto-assigns first admin member as team owner (if no owner exists)
5. Records migration state in `SchemaMigrations` collection

Output: Full migration report with counts and applied changes.

#### Verify (Post-Migration Check)
```bash
cd TeamConnect.Api
dotnet run --migration-mode=verify --migration-version=2026-05-06-profile-team-v1
```

Re-analyzes data post-migration to confirm:
- Zero critical mismatches
- All role values normalized
- All required fields initialized
- Membership consistency achieved

**Always run verify after apply to confirm success.**

### Ownership Bootstrap Policy

During migration:
- If a team has no `OwnerId`, the migration finds the first member with "Admin" role and assigns them as owner
- If no admin member exists in the team, `OwnerId` remains null (marked for manual admin assignment)

Teams queued for manual assignment should be assigned by an admin in the admin dashboard (future feature).

### Idempotency & Rollback

- The migration runner is **idempotent** — running it multiple times is safe
- Each version is tracked in the `SchemaMigrations` collection to prevent accidental re-runs with the same version
- To rollback: restore your database backup and re-run migration with a new version number

---

## Authorization Policies

Two new policies control team and user management:

| Policy | Description | Endpoints |
|--------|-------------|-----------|
| `AdminOnly` | Only users with "Admin" role | Create team, delete team, admin-level operations |
| `AdminOrTeamOwner` | Admins or the team owner | Update team, add/remove members |

---

## Role Model

Three roles are now supported:

| Role | Capabilities |
|------|--------------|
| **Admin** | Create/delete teams, update any profile, manage any team, assign team owners |
| **User** | Update own profile, view teams, view other profiles (soon: request to join teams) |
| **TeamOwner** | Manage their owned team, add/remove members, update team info |

> Note: "TeamOwner" is separate from team membership. A user with TeamOwner role can own and manage their assigned team.

---

## DTO Reference

### UserProfileDto
```csharp
{
  id: string,
  fullName: string,
  email: string,
  role: string,                        // "Admin", "User", or "TeamOwner"
  teamIds: string[],                   // List of team IDs user belongs to
  bio?: string,
  avatarUrl?: string,
  department?: string,
  location?: string,
  timezone?: string,
  pronouns?: string,
  preferredWorkStyle?: string,
  hobbies: string[],
  strengths: string[],
  icebreaker?: string,
  updatedAt?: DateTime
}
```

### UpdateProfileDto
```csharp
{
  bio?: string,
  avatarUrl?: string,
  department?: string,
  location?: string,
  timezone?: string,
  pronouns?: string,
  preferredWorkStyle?: string,
  hobbies?: string[],
  strengths?: string[],
  icebreaker?: string
}
```

### CreateTeamDto
```csharp
{
  name: string,
  description?: string
}
```

### TeamDetailDto
```csharp
{
  id: string,
  name: string,
  ownerId?: string,
  description?: string,
  createdAt?: DateTime,
  updatedAt?: DateTime,
  memberIds: string[]
}
```

---

## Implementation Roadmap

| Phase | Status | Description |
|-------|--------|-------------|
| 1-2 | ✅ Done | Migration design & tooling, role enum, profile fields |
| 3 | ✅ Done | Operational safety (backup, rollback) |
| 4 | ✅ Done | Backend DTOs and endpoints |
| 5 | 🔄 In Progress | Frontend models, services, profile UI, role guards |
| 6 | ⏳ Planned | Advanced features: peer feedback, trust signals, team icebreakers |

---

## Example Workflows

### Scenario: Admin creates a team and assigns members

1. Admin calls `POST /api/teams` → creates team, auto-becomes owner
2. Admin calls `POST /api/teams/{teamId}/add/{userId}` for each member
3. Each member sees team in their profile (`teamIds[]`)
4. Team owner can call `PUT /api/teams/{teamId}` to update description

### Scenario: User updates their profile with hobbies and availability

1. User calls `PUT /api/users/{userId}` with `UpdateProfileDto` containing:
   - `hobbies: ["photography", "hiking"]`
   - `pronouns: "they/them"`
   - `timezone: "EST"`
2. Profile is updated; other team members see these fields when viewing the user

### Scenario: Team ownership inference during migration

1. Team "Engineering" has members: User A (Admin), User B (User), User C (User)
2. During migration, User A (first admin member) is auto-assigned as OwnerId
3. Post-migration, User A can manage team membership

---

## Troubleshooting

### Q: Migration says "teams missing owner" — what now?
**A:** Those teams have no admin members. Manually assign an admin to the team via admin dashboard, or manually update the team document.

### Q: Can I roll back the migration?
**A:** Yes. Restore your database backup from before the migration. To re-migrate with fixes, change the version number.

### Q: JWT role claim not updated after migration?
**A:** JWT is generated at login. Users need to log out and log back in to get the normalized role claim.

### Q: I want to assign a user to multiple teams — can they?
**A:** Yes. A user's `teamIds` is a list; they can belong to multiple teams simultaneously.

---

## Future Enhancements

1. **Team Invitations**: Allow users to request team membership
2. **Peer Feedback**: Collect trust/cohesion feedback from teammates
3. **Trust Badges**: Display badges like "Reliable", "Great Communicator"
4. **Team Analytics**: Cohesion metrics, participation rates, sentiment analysis
5. **Icebreaker Generator**: Suggest team-building questions based on shared interests
6. **Pairing Recommendations**: AI-suggested pairs based on complementary skills
