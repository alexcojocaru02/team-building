This project is called TeamConnect.

It is an Angular 20 frontend for a dissertation project.
The backend is an ASP.NET Core Web API (.NET 8) using JWT authentication and MongoDB.

The application supports remote teams and focuses on team cohesion.
This is an academic project, not a commercial one.

Backend base URL (dev): http://localhost:5217/api

Authentication:
- JWT Bearer token
- Token is stored in localStorage
- Token must be sent in Authorization header:
  Authorization: Bearer <token>

Main API endpoints:

POST   /api/auth/register
POST   /api/auth/login

GET    /api/feed
POST   /api/feed

POST   /api/feedback
GET    /api/feedback/received

GET    /api/dashboard/cohesion

Use Angular 20 standalone components.
Do NOT use NgModules.

Use HttpClient for API calls.
Use services for API logic.
Use simple routing with guards for authentication.

Keep UI simple and functional.
No complex UI libraries required (Angular Material optional but not mandatory).
Focus on clarity and demo-readiness.

Auth handling:
- JWT token is stored in localStorage under key: "token"
- AuthService exposes:
  - login()
  - register()
  - logout()
  - isAuthenticated()

Use an HTTP interceptor to attach the JWT token to all API requests.

The UI must include the following pages:

1. Login page
2. Register page
3. Feed page
   - List posts
   - Create new post
4. Feedback page
   - Send feedback to another user
   - View received feedback
5. Cohesion Dashboard page
   - Show total feedback count
   - Show feedback received per user

Routing should restrict access to authenticated users only.

Use TypeScript best practices.
Use async/await or RxJS observables cleanly.
Prefer readable, maintainable code over clever optimizations.
Add short comments when logic is non-obvious.

Aditional notes:
Tailwind uses tw: prefix (eg: tw:grid)

====================================
AUTH DTOs
====================================

export interface RegisterRequestDto {
  email: string;
  password: string;
}

export interface LoginRequestDto {
  email: string;
  password: string;
}

export interface AuthResponseDto {
  token: string;
}

====================================
USER DTOs
====================================

export interface UserDto {
  id: string;
  email: string;
}

====================================
FEED DTOs
====================================

export interface CreateFeedPostDto {
  content: string;
}

export interface FeedPostDto {
  id: string;
  content: string;
  createdAt: string;

  authorId: string;
  authorEmail: string;
}

====================================
FEEDBACK DTOs (Peer-to-peer)
====================================

export interface CreateFeedbackDto {
  toUserId: string;
  message: string;
}

export interface FeedbackDto {
  id: string;
  fromUserId: string;
  toUserId: string;
  message: string;
  createdAt: string;
}

====================================
DASHBOARD DTOs
====================================

export interface CohesionDashboardDto {
  totalFeedbacks: number;
  users: UserFeedbackStatsDto[];
}

export interface UserFeedbackStatsDto {
  userId: string;
  email: string;
  feedbackReceived: number;
}
