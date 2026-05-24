# TeamConnect Profile & Team Management - Implementation Summary

## Overview

This document summarizes the implementation of user profile management, team assignment, and role-based access control (RBAC) for the TeamConnect application. The feature is deployed in phases to ensure zero-downtime migration and backward compatibility.

---

## What's New

### User Profiles (Interpersonal Relationship Building)

Users can now have rich profiles with personal information that helps build team trust and cohesion:

- **Bio**: Short personal summary (e.g., "Data scientist passionate about ML")
- **Avatar URL**: Profile image link
- **Department**: Team or department name
- **Location**: City or timezone
- **Timezone**: Preferred timezone (e.g., EST, PST)
- **Pronouns**: Preferred pronouns (e.g., he/him, they/them)
- **Preferred Work Style**: Work preference (Asynchronous, Synchronous, Flexible)
- **Hobbies & Interests**: List of personal interests
- **Strengths & Skills**: Key competencies
- **Icebreaker/Fun Fact**: Conversation starter

All profile fields are **optional** and can be filled in gradually by users.

### Team Assignment & Ownership

- **Team Ownership**: Teams now have an owner (Admin or TeamOwner role) who can manage membership
- **Auto-Assignment During Migration**: First admin member found in a team is automatically assigned as owner
- **Flexible Membership**: Users can belong to multiple teams
- **Team Metadata**: Teams now track creation date, last update, and optional description

### Role-Based Access Control (RBAC)

Three distinct roles control application features:

| Role | Capabilities |
|------|--------------|
| **Admin** | Create/delete teams, update any profile, manage any team, assign team owners |
| **User** | Update own profile, view teams, view other profiles |
| **TeamOwner** | Manage their owned team, add/remove members, update team info |

---

## User Workflows

### User Updates Their Profile

1. User navigates to `/profile` → views their current profile
2. Clicks "Edit Profile" → `/profile/edit` form
3. Updates fields: avatar, bio, hobbies, strengths, pronouns, work style, icebreaker
4. Saves changes → profile updated, redirected to `/profile` view
5. Team members can view the updated profile

### Admin Creates a Team and Assigns Members

1. Admin navigates to `/teams` → sees all teams and "Create Team" button
2. Clicks "Create Team" → enters team name and description
3. Admin is automatically assigned as team owner
4. Admin clicks "Manage Members" on the team card
5. Admin adds users to the team by selecting from available users
6. Each added user sees the team in their profile

### Team Owner Manages Team Membership

1. TeamOwner user sees their team marked with "Owner" badge on `/teams`
2. Clicks "Manage Members" to add/remove team members
3. Can only manage their own team (not other teams)
4. Admins can manage any team

### Admin Views System Dashboard

1. Admin navigates to `/admin` (role-protected route)
2. Sees dashboard with:
   - Total user count
   - Total team count
   - Count of teams without assigned owners
   - Table of all users with roles
   - List of teams and their owners
   - Warning badge for unassigned teams

---

## Technical Architecture

### Backend (C# / .NET 8)

**New Models:**
- `UserRole` enum: Admin, User, TeamOwner
- `User.Profile*` fields: Bio, AvatarUrl, Department, Location, Timezone, Pronouns, WorkStyle, Hobbies[], Strengths[], Icebreaker, UpdatedAt
- `Team.OwnerId`, `Team.Description`, `Team.CreatedAt`, `Team.UpdatedAt`

**New Endpoints:**
- `GET /api/users/{id}` — get user profile
- `PUT /api/users/{id}` — update profile (self or admin)
- `GET /api/teams/{id}` — get team details
- `PUT /api/teams/{id}` — update team (owner or admin)
- `DELETE /api/teams/{id}` — delete team (admin only)
- `POST /api/teams/{id}/add/{userId}` — add member (owner or admin)
- `DELETE /api/teams/{id}/members/{userId}` — remove member (owner or admin)

**Authorization:**
- `[Authorize(Policy = "AdminOnly")]` — endpoints restricted to admins
- `[Authorize(Policy = "AdminOrTeamOwner")]` — endpoints for team management

**Migration:**
- `ProfileAndTeamMigrationRunner` — one-off, manual migration tool
- Modes: `dry-run`, `apply`, `verify`
- Auto-assigns first admin member as team owner (if no owner exists)
- Normalizes role strings to "Admin", "User", "TeamOwner"
- Backfills `User.TeamIds` from `Team.MemberIds` (canonical source)

### Frontend (Angular 19+, Standalone)

**New Models & Services:**
- `UserDto` now includes role, profile fields, teamIds
- `UsersService` exposes profile + team endpoints
- `AuthService` extracts role from JWT, exposes `isAdmin()`, `isTeamOwner()` computed signals
- Token expiry validation before API calls

**New Components:**
- `/profile` → `ProfileViewComponent` — display full profile with badges
- `/profile/edit` → `ProfileEditComponent` — form to edit all fields
- `/teams` → `TeamsListComponent` — list teams, manage membership (admin/owner)
- `/admin` → `AdminDashboardComponent` — system dashboard (admin only)

**Authorization:**
- `roleGuard(['Admin'])` — protect admin routes
- `RoleGuardService` for custom role checks in components

---

## Database Migration

### Migration Procedure

**Step 1: Run Dry-Run (Analysis Only)**
```bash
cd TeamConnect.Api
dotnet run --migration-mode=dry-run --migration-version=2026-05-06-profile-team-v1
```

Review the report for:
- Data quality issues (invalid roles, missing fields)
- Membership drift (TeamIds vs Team.MemberIds mismatch)
- Teams without owners

**Step 2: Backup Database**
```bash
# MongoDB backup
mongodump --uri="mongodb://localhost:27017/TeamConnectDb" --out=./backup-before-migration
```

**Step 3: Apply Migration**
```bash
dotnet run --migration-mode=apply --migration-version=2026-05-06-profile-team-v1
```

The migration:
- Normalizes all role strings to "Admin", "User", "TeamOwner"
- Initializes missing profile fields with null/empty defaults
- Backfills `User.TeamIds` from `Team.MemberIds`
- Auto-assigns first admin member as team owner (or queues for manual assignment)
- Records migration state in `SchemaMigrations` collection

**Step 4: Verify Migration**
```bash
dotnet run --migration-mode=verify --migration-version=2026-05-06-profile-team-v1
```

Review the final report to confirm:
- All role values normalized
- All required fields initialized
- Membership consistency achieved
- Zero critical errors

---

## Testing Checklist

- [ ] **Backend Compilation**: `dotnet build` succeeds with 0 errors
- [ ] **Migration Dry-Run**: Analysis runs, report shows expected counts
- [ ] **Migration Apply**: Changes applied successfully, report generated
- [ ] **Migration Verify**: Post-migration state is clean, zero critical mismatches
- [ ] **API Tests**: Auth endpoints still work, new endpoints return correct DTOs
- [ ] **Role Extraction**: JWT claims include normalized role, visible in authService
- [ ] **Profile Management**: Users can GET/PUT their profile; admins can PUT any profile
- [ ] **Team Management**: Admins can CRUD teams; owners can manage members
- [ ] **Authorization**: Non-admin users get 403 Forbidden on team/admin endpoints
- [ ] **Frontend Routes**: `/profile`, `/profile/edit`, `/teams`, `/admin` load correctly
- [ ] **Role Guards**: Unauthenticated users redirected to login; non-admins blocked from `/admin`
- [ ] **Token Expiry**: Expired tokens cleared on app load; API calls fail on 401
- [ ] **Profile UI**: Profile form displays all fields, edits persist after save
- [ ] **Teams UI**: Team list shows owner badges, creation/deletion works for admins
- [ ] **Admin Dashboard**: Displays correct user/team counts, warns about unassigned owners

---

## Deployment Steps

### Pre-Deployment

1. Notify users that profile features are coming (opt-in to fill in profile)
2. Set up MongoDB backup schedule
3. Run migration dry-run in production backup to verify data
4. Review migration report; identify teams needing manual owner assignment

### Deployment

1. **Deploy backend** (API + migration tooling)
   - Push code to main branch
   - Build API
   - No API downtime — existing endpoints unchanged
2. **Run migration** (after backend deployed)
   - Backup production database
   - Run `dotnet run --migration-mode=dry-run` one final time
   - Run `dotnet run --migration-mode=apply`
   - Run `dotnet run --migration-mode=verify` and review report
   - Keep backup for 30 days minimum
3. **Deploy frontend** (gradual rollout)
   - Push UI code to main branch
   - Build and deploy to production
   - Release new routes: `/profile`, `/profile/edit`, `/teams`, `/admin`
4. **Monitor** (first 24 hours)
   - Check API logs for errors
   - Verify role extraction in JWT claims
   - Monitor profile/team API endpoints for 5xx errors
   - Run a smoke test: login → view profile → edit profile → view teams

### Post-Deployment

1. **Manual Admin Tasks** (one-time)
   - Review teams without owners from migration report
   - Assign admins to those teams via admin dashboard
2. **User Communication**
   - Announce new profile features in in-app notification
   - Link to profile guide/FAQ
3. **Monitor** (ongoing)
   - Track profile completion rate (analytics)
   - Monitor API error rates
   - Collect feedback from early adopters

---

## Rollback Plan

**If migration fails or critical issues found:**

1. Restore MongoDB from backup taken before migration
2. Redeploy API with fix
3. Re-run migration with new version number (idempotent)

**If frontend deployment fails:**

1. Revert UI code to previous version (no backend impact)
2. Existing API and user data unaffected

---

## Future Enhancements

1. **Team Invitations**: Allow users to request to join teams (with approval flow)
2. **Peer Feedback**: Collect 360-degree feedback from teammates
3. **Trust Badges**: Display earned badges like "Great Communicator", "Reliable Performer"
4. **Cohesion Metrics**: Dashboard showing team trust/engagement scores
5. **Icebreaker Generator**: AI-powered suggestions for team-building questions
6. **Pairing Recommendations**: ML-based suggestions for productive pairs based on skills/interests
7. **Profile Privacy**: Allow users to mark fields as private (visible only to team members)

---

## FAQ

**Q: Can users change their role?**
A: No. Roles are assigned by admins only. Users cannot self-assign Admin or TeamOwner roles.

**Q: What if a team has no admin members during migration?**
A: The team's `OwnerId` remains null. An admin must manually assign an owner via the admin dashboard.

**Q: Can a user belong to multiple teams?**
A: Yes. `User.TeamIds` is a list; users can belong to any number of teams.

**Q: What happens if I delete a team?**
A: The team is removed from the system and automatically deleted from all members' TeamIds.

**Q: Can TeamOwner delete a team?**
A: No. Only Admins can delete teams. TeamOwners can only manage membership.

**Q: How long does the migration take?**
A: Depends on data size. For ~1000 users and ~50 teams, typically < 1 second. Verify by running dry-run first.

---

## Support

For questions or issues, see:
- API Reference: [PROFILE_AND_TEAM_API.md](../TeamConnect.Api/PROFILE_AND_TEAM_API.md)
- Code: [TeamConnect.Api/Modules/](../TeamConnect.Api/Modules/), [UI/src/app/](../src/app/)
- Migration Logs: Check `SchemaMigrations` collection after running migration
