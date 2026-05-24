# TeamConnect - Root Copilot Context

This project is called TeamConnect.

It is a full-stack dissertation project:
- Frontend: Angular 20 (standalone components), inside UI/
- Backend: ASP.NET Core Web API (.NET 8) with MongoDB + JWT, inside TeamConnect.Api/

The app supports remote teams and focuses on team cohesion.
This is an academic/demo project, not a commercial platform.

## Repository Structure

- UI/ -> Angular frontend
- TeamConnect.Api/ -> .NET backend API

When working from the repository root, keep changes scoped to the correct app:
- Frontend/UI work goes in UI/
- API/domain/data work goes in TeamConnect.Api/

## Local Run Targets

Frontend:
- Folder: UI/
- Typical command: npm install, then ng serve
- URL: http://localhost:4200

Backend:
- Folder: TeamConnect.Api/
- Typical command: dotnet run
- Base API URL (dev): http://localhost:5217/api (HTTPS also available at https://localhost:7241/api)

## Backend Overview (.NET 8)

Key configuration:
- JSON property name matching is case-sensitive in Program.cs
- CORS policy AllowAngularApp allows origin http://localhost:4200
- Authentication: JWT Bearer
- Authorization: enabled globally via [Authorize] on protected controllers
- MongoDB configured via appsettings.json

Important settings in TeamConnect.Api/appsettings.json:
- MongoDb: ConnectionString mongodb://localhost:27017
- MongoDb: DatabaseName TeamConnectDb
- Jwt: Key/Issuer/Audience configured for local development

Mongo collections exposed in MongoDbContext:
- Users
- Teams
- FeedPosts
- FeedPostLikes
- FeedPostComments
- Feedbacks

## API Endpoints

Auth (public):
- POST /api/auth/register
- POST /api/auth/login

Users (auth required):
- GET /api/users

Feed (auth required):
- GET /api/feed
- POST /api/feed
- POST /api/feed/{postId}/like
- DELETE /api/feed/{postId}/like
- GET /api/feed/{postId}/comments
- POST /api/feed/{postId}/comments

Feedback (auth required):
- POST /api/feedback
- GET /api/feedback/received

Dashboard (auth required):
- GET /api/dashboard/cohesion

Teams (auth required):
- GET /api/teams
- POST /api/teams
- POST /api/teams/{teamId}/add/{userId}

## Backend DTO Contracts (source of truth)

Auth DTOs:
- RegisterDto: FullName, Email, Password
- LoginDto: Email, Password

Feedback DTOs:
- CreateFeedbackDto: ToUserId, Message
- FeedbackResponseDto:
  Id, FromUserId, ToUserId, Message, CreatedAt,
  FromUserFullName, FromUserEmail, ToUserFullName, ToUserEmail

Feed DTOs:
- CreateFeedPostDto: Content
- CreateFeedPostCommentDto: Content
- FeedPostCommentDto:
  Id, Content, CreatedAt, AuthorId, AuthorFullName, AuthorEmail
- FeedPostReactionStatsDto: PostId, LikesCount, LikedByCurrentUser
- FeedPostResponseDto:
  Id, Content, CreatedAt, AuthorId, AuthorFullName, AuthorEmail,
  LikesCount, CommentsCount, LikedByCurrentUser, RecentComments

Dashboard DTOs:
- CohesionDashboardDto: TotalFeedbacks, Users
- UserFeedbackStatsDto: UserId, Email, FeedbackReceived

## Frontend Overview (Angular 20)

Architecture rules:
- Use Angular standalone components.
- Do NOT use NgModules.
- Keep API calls in services.
- Use route guards for protected pages.
- Use HTTP interceptor for auth token handling.

Environment:
- UI/src/environments/environment.ts
- apiUrl = http://localhost:5217/api

Tailwind note:
- Tailwind utility prefix is tw: (example: tw:grid)

Current route structure:
- /login -> login page via AuthLayout
- Protected routes under MainLayout:
  /home, /feedback, /feed, /dashboard

## Frontend Auth Rules

Token handling:
- JWT token storage key: token (localStorage)
- Always attach token as Authorization: Bearer <token> on API calls
- On 401 (except login/register calls), force logout and redirect to /login

Guard behavior:
- Unauthenticated users should be redirected to /login
- Preserve returnUrl when redirecting from protected routes

Authentication API usage:
- Register: POST /api/auth/register
- Login: POST /api/auth/login

## Frontend Service Contracts

Auth service should expose and/or keep behavior for:
- register()
- login()
- logout()
- getToken()
- authenticated state (signal/computed)

Core data services:
- FeedService -> /feed endpoints
- FeedbackService -> /feedback endpoints
- DashboardService -> /dashboard/cohesion
- UsersService -> /users

## Cross-Stack Development Rules

- Keep frontend and backend DTO naming aligned.
- If backend response shape changes, update matching TypeScript interfaces.
- Preserve existing endpoint paths unless intentionally versioning/migrating.
- Prefer clear, maintainable code over clever or over-abstracted solutions.
- Add short comments only when logic is non-obvious.
- Keep UI simple, functional, and demo-ready.

## Practical Notes For Copilot

When asked to implement a feature from repository root:
1. Determine if change is frontend, backend, or both.
2. Update backend contracts/endpoints first when API changes are needed.
3. Then update frontend models/services/pages to match.
4. Validate auth flow still works (token attach + 401 logout + guard redirect).
5. Keep changes minimal and consistent with existing code style.

When adding new API endpoints:
- Add/extend DTOs in TeamConnect.Api/Shared/DTOs/
- Add controller action in the correct module under TeamConnect.Api/Modules/
- Reuse MongoDbContext collections and existing auth pattern
- Expose matching Angular service methods in UI/src/app/services/

This root context should be used when Copilot is running at repository root so both apps are considered together.
