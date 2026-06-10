# Raport tehnic — Backend TeamConnect.Api (ASP.NET Core 8 / MongoDB)

## 1. Structura proiectului și organizarea pe straturi

```
TeamConnect.Api/
├── Modules/
│   ├── Auth/          → AuthController, AuthService, JwtService
│   ├── Dashboard/     → DashboardController, DashboardService
│   ├── Feed/          → FeedController, FeedService, FeedRepository, IFeedRepository
│   ├── Feedback/      → FeedbackController, FeedbackService, FeedbackRepository, IFeedbackRepository
│   ├── Gamification/  → GamificationController, GamificationService
│   ├── TeamActivities/→ TeamActivitiesController, TeamActivitiesService, TeamActivityRepository, ITeamActivityRepository
│   ├── Teams/         → TeamsController, TeamsService, TeamRepository/ITeamRepository, TeamJoinRequestRepository/ITeamJoinRequestRepository
│   └── Users/         → UsersController, UsersService
└── Shared/
    ├── DTOs/          → 21 clase DTO
    ├── Models/        → 9 entități MongoDB (User, Team, FeedPost, FeedPostComment, FeedPostLike, Feedback, TeamActivity, TeamJoinRequest, SchemaMigrationRecord)
    ├── Repositories/  → UserRepository, IUserRepository
    └── Services/      → MongoDbContext, SmtpEmailSender/IEmailSender, NotificationService/INotificationService, ProfileAndTeamMigrationRunner, SchemaMigrationRecord
```

**Concluzii arhitecturale:**
- **Repository pattern**: da — există interfețe (`IUserRepository`, `ITeamRepository`, `IFeedRepository`, `IFeedbackRepository`, `ITeamActivityRepository`, `ITeamJoinRequestRepository`) și implementări separate, injectate prin DI.
- **Servicii separate de controllere**: da — fiecare modul are un `XxxService` care conține logica de business, controllerul doar orchestrează cererea/răspunsul.
- **DTO-uri separate de entități**: da — folder dedicat `Shared/DTOs` cu 21 clase, distinct de `Shared/Models`.
- Organizarea este pe **module funcționale** (vertical slicing), nu pe straturi orizontale clasice (Controllers/Services/Models la rădăcină).

## 2. Configurarea aplicației (Program.cs)

**Înregistrări DI:**

| Tip | Metodă | Interfață → Implementare |
|---|---|---|
| MongoDbContext | `AddSingleton` | direct |
| Email | `AddScoped` | `IEmailSender` → `SmtpEmailSender` |
| Notificări | `AddScoped` | `INotificationService` → `NotificationService` |
| Migrare date | `AddScoped` | `ProfileAndTeamMigrationRunner` (direct) |
| Repos | `AddScoped` | `IUserRepository`→`UserRepository`, `ITeamRepository`→`TeamRepository`, `IFeedRepository`→`FeedRepository`, `IFeedbackRepository`→`FeedbackRepository`, `ITeamActivityRepository`→`TeamActivityRepository`, `ITeamJoinRequestRepository`→`TeamJoinRequestRepository` |
| Servicii | `AddScoped` | `AuthService`, `JwtService`, `TeamsService`, `FeedService`, `UsersService`, `FeedbackService`, `DashboardService`, `TeamActivitiesService`, `GamificationService` (toate directe) |

**Politici de autorizare:**
```csharp
options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
options.AddPolicy("AdminOrTeamOwner", policy => policy.RequireRole("Admin", "TeamOwner"));
```

**Conexiune MongoDB** (`MongoDbContext.cs`):
- numele bazei de date și connection string-ul sunt citite din configurație, prin cheile `MongoDb:ConnectionString` și `MongoDb:DatabaseName`
- constructorul construiește `MongoClient` din connection string și apelează `GetDatabase(databaseName)`
- aplicația aruncă `InvalidOperationException` la pornire dacă oricare dintre valori lipsește

**Pipeline middleware (ordine):**
1. `UseSwagger`/`UseSwaggerUI` (doar Development)
2. `UseHsts` (non-Development)
3. `UseHttpsRedirection`
4. `UseDefaultFiles`/`UseStaticFiles` (non-Development)
5. `UseCors("AllowAngularApp")`
6. `UseAuthentication`
7. `UseAuthorization`
8. `MapHealthChecks("/api/health")`
9. `MapControllers`
10. `MapFallbackToFile("index.html")` (non-Development)

CORS citește originile permise din `Cors:AllowedOrigins` (cu fallback la `http://localhost:4200`).

## 3. Modelul de date

| Entitate | Colecție Mongo | Câmpuri principale | Atribute BSON |
|---|---|---|---|
| **User** | `Users` | Id, FullName, Email, PasswordHash, Role, TeamIds:List\<string\>, Bio?, AvatarUrl?, Department?, Location?, Timezone?, Pronouns?, PreferredWorkStyle?, Hobbies:List\<string\>, Strengths:List\<string\>, Icebreaker?, UpdatedAt? | `[BsonId]`, `[BsonRepresentation(BsonType.ObjectId)]` pe Id |
| **Team** | `Teams` | Id, Name, OwnerId?, Description?, CreatedAt?, UpdatedAt?, MemberIds:List\<string\> | `[BsonId]`, `[BsonRepresentation(BsonType.ObjectId)]` |
| **FeedPost** | `FeedPosts` | Id, AuthorId, Content, CreatedAt | `[BsonId]`, `[BsonRepresentation(BsonType.ObjectId)]` |
| **FeedPostComment** | `FeedPostComments` | Id, FeedPostId, UserId, Content, CreatedAt | idem |
| **FeedPostLike** | `FeedPostLikes` | Id, FeedPostId, UserId, CreatedAt | idem |
| **Feedback** | `Feedbacks` | Id, FromUserId, ToUserId, Message, Category (enum FeedbackCategory), Tone (enum FeedbackTone), CreatedAt | idem |
| **TeamActivity** | `TeamActivities` | Id, TeamId, CreatedByUserId, ActivityType (enum), Title, Description, Options:List\<string\>, Points:int, ScheduledAt?, ScheduledEndAt?, MeetingLink?, Status, CreatedAt, CompletedAt?, Participations:List\<TeamActivityParticipation\> | `[BsonRepresentation(BsonType.String)]` pe enum-uri, plus clasă imbricată `TeamActivityParticipation` |
| **TeamJoinRequest** | `TeamJoinRequests` | Id, TeamId, UserId, Status (default Pending), CreatedAt | `[BsonId]`, `[BsonRepresentation(BsonType.ObjectId)]` |
| **SchemaMigrationRecord** | `SchemaMigrations` | folosit pentru a urmări migrările de schemă | — |

Toate Id-urile sunt `string` cu `[BsonId] [BsonRepresentation(BsonType.ObjectId)]`.

## 4. Controllere și endpoint-uri

| Controller | Endpoint | Verb | Rol/Autorizare | Descriere |
|---|---|---|---|---|
| Auth (`api/auth`) | `register` | POST | — | înregistrare cont, returnează token |
| | `login` | POST | — | autentificare, returnează token |
| Teams (`api/teams`) | `/` | POST | `[Authorize]` | creează echipă (utilizatorul devine owner) |
| | `{teamId}/add/{userId}` | POST | AdminOrTeamOwner | adaugă utilizator în echipă |
| | `{teamId}/members/{userId}` | DELETE | AdminOrTeamOwner | elimină membru |
| | `/` | GET | `[Authorize]` | listă echipe |
| | `{id}` | GET | `[Authorize]` | detalii echipă |
| | `{id}` | PUT | AdminOrTeamOwner | actualizare echipă |
| | `{id}` | DELETE | AdminOnly | ștergere echipă |
| | `{teamId}/join-requests` | POST | `[Authorize]` | cerere de aderare |
| | `{teamId}/join-requests` | GET | AdminOrTeamOwner | listă cereri pentru echipă |
| | `join-requests` | GET | AdminOnly | toate cererile |
| | `join-requests/{id}/approve` | PUT | AdminOrTeamOwner | aprobă cerere |
| | `join-requests/{id}/reject` | PUT | AdminOrTeamOwner | respinge cerere |
| Users (`api/users`) | `/` | GET | `[Authorize]` | listă utilizatori |
| | `teammates` | GET | `[Authorize]` | colegi de echipă |
| | `teammates/{teamId}` | GET | `[Authorize]` | colegi pentru echipă specifică |
| | `{id}` | GET | `[Authorize]` | profil utilizator |
| | `me` | GET | `[Authorize]` | profilul propriu |
| | `{id}` | PUT | `[Authorize]` | actualizare profil |
| | `{id}/promote-admin` | POST | `Roles=Admin` | promovare la rol Admin |
| | `me` | DELETE | `[Authorize]` | ștergere cont propriu |
| | `me` | PUT | `[Authorize]` | actualizare profil propriu |
| Feed (`api/feed`) | `/` | POST | `[Authorize]` | creare postare |
| | `/` | GET | `[Authorize]` | feed complet |
| | `{postId}/like` | POST/DELETE | `[Authorize]` | apreciere/retragere apreciere |
| | `{postId}/comments` | GET/POST | `[Authorize]` | listă/adăugare comentarii |
| | `{postId}` | DELETE | `[Authorize]` | ștergere postare (autor sau Admin) |
| Feedback (`api/feedback`) | `/` | POST | `[Authorize]` | trimite feedback (validează: nu către sine, doar colegi) |
| | `received` | GET | `[Authorize]` | feedback primit |
| TeamActivities (`api/teams/{teamId}/activities`) | `/` | GET | `[Authorize]` + check CanAccessTeam | listă activități |
| | `summary` | GET | idem | sumar activități |
| | `/` | POST | check CanManageTeam | creare activitate |
| | `{activityId}/responses` | POST | check CanAccessTeam | trimitere răspuns |
| | `{activityId}/complete` | POST | check CanManageTeam | finalizare activitate |
| Dashboard (`api/dashboard`) | `cohesion/{teamId}` | GET | `[Authorize]` + Admin/owner check | statistici de coeziune echipă |
| Gamification (`api/gamification`) | `leaderboard/{teamId}` | GET | `[Authorize]` + Admin/membru check | clasament punctaje |

## 5. Autentificare și autorizare

**Generare JWT** — `JwtService.Generate(User user)`:
- claims: `ClaimTypes.NameIdentifier` (Id), `ClaimTypes.Name` (FullName), `ClaimTypes.Email`, `ClaimTypes.Role` (rol normalizat)
- algoritm: `HmacSha256`, semnat cu `SymmetricSecurityKey`
- expirare: 4 ore (`DateTime.UtcNow.AddHours(4)`)
- validare la consum: `ValidateIssuer`, `ValidateAudience`, `ValidateLifetime`, `ValidateIssuerSigningKey` toate true, `ClockSkew = 0`
- cheia, issuer și audience vin din configurație (`Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`) — fără valori hardcodate

**Hashing parolă**: librăria **BCrypt.Net-Next** (`BCrypt.Net.BCrypt.HashPassword(...)` la înregistrare, `BCrypt.Net.BCrypt.Verify(...)` la autentificare)

**Roluri**: `Admin`, `User` (implicit), `TeamOwner`, normalizate prin `UserRoles.Normalize()`. Autorizarea folosește atât `[Authorize(Roles=...)]` cât și politici (`AdminOnly`, `AdminOrTeamOwner`), plus verificări manuale în servicii (ex. `CanAccessTeam`, `CanManageTeam`).

## 6. Accesul la date (operații MongoDB)

Toate operațiile sunt **asincrone** (`async`/`await`, returnând `Task`/`Task<T>`). Exemple:
- `UserRepository.FindByEmailAsync`: `_context.Users.Find(u => u.Email == email).FirstOrDefaultAsync()`
- `UserRepository.InsertAsync`: `_context.Users.InsertOneAsync(user)`
- `UserRepository.UpdateProfileAsync`: `Builders<User>.Update.Set(...)` + `UpdateOneAsync`
- `UserRepository.DeleteAsync`: `_context.Users.DeleteOneAsync(...)`
- `FeedRepository.DeletePostAsync`: ștergere în cascadă cu `DeleteManyAsync` pe like-uri/comentarii, apoi `DeleteOneAsync` pe postare
- `TeamActivityRepository.SubmitResponseAsync`: `FindOneAndUpdateAsync` cu `ReturnDocument.After`

## 7. Validare și tratarea erorilor

- **Nu există** atribute `[Required]`/data annotations sau FluentValidation pe DTO-uri (confirmat: nu există în cod)
- Validarea se face **manual**, în controllere și servicii (verificări `string.IsNullOrWhiteSpace`, reguli de business — ex. „nu poți trimite feedback ție însuți”, validări specifice tipului de activitate la TeamActivities)
- **Nu există** middleware global de excepții (confirmat: nu există în cod) — controllerele prind excepții punctual (ex. `UnauthorizedAccessException` → `Forbid()`, excepții la login → 500)
- Coduri de stare folosite: 200, 204, 400, 401, 403, 404, 409 (`ProblemDetails` la conflict de înregistrare), 500

## 8. Flux complet — endpoint de login (`POST /api/auth/login`)

```
AuthController.Login(LoginDto)
  → AuthService.Login(dto)
      → UserRepository.FindByEmailAsync(email)
          → MongoDB: Users.Find(u => u.Email == email).FirstOrDefaultAsync()
      → BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash)
  → JwtService.Generate(user)
  → return Ok(new { token })
```

```csharp
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginDto dto)
{
    try
    {
        var user = await _auth.Login(dto);
        if (user == null) return Unauthorized();
        return Ok(new { token = _jwt.Generate(user) });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Login failed for {Email}", dto?.Email);
        return StatusCode(StatusCodes.Status500InternalServerError, "Login failed");
    }
}
```

## 9. Tehnologii și pachete

- **.NET 8.0** (`net8.0`), `Nullable` și `ImplicitUsings` activate
- **MongoDB.Driver** 3.6.0
- **Microsoft.AspNetCore.Authentication.JwtBearer** 8.0.0
- **BCrypt.Net-Next** 4.0.3
- **Swashbuckle.AspNetCore** 6.6.2 (Swagger/OpenAPI)
- target de build personalizat `IncludeAngularDist` pentru a include front-end-ul Angular în publish

---

## Rezumat — particularități reale ale implementării

1. **Organizare verticală pe module funcționale** (`Modules/Auth`, `Modules/Feed` etc.), nu separare orizontală clasică Controllers/Services/Repositories la rădăcină — un detaliu arhitectural care merită menționat în lucrare.
2. **Repository pattern aplicat consecvent** prin interfețe injectate, dar nu pentru toate modulele uniform (ex. Users și Gamification nu au repository dedicat — serviciile lor folosesc `IUserRepository` direct).
3. **Autorizare hibridă**: combină `[Authorize(Roles=...)]`, politici declarative (`AdminOnly`, `AdminOrTeamOwner`) și verificări de business custom în servicii (`CanAccessTeam`, `CanManageTeam`, verificare „sunt colegi de echipă”).
4. **Aplicație monolitică deployabilă cu front-end inclus**: `Program.cs` configurează `UseStaticFiles`/`MapFallbackToFile("index.html")` și un target MSBuild custom `IncludeAngularDist`, deci API-ul servește și SPA-ul Angular în producție.
5. **Lipsă completă de validare declarativă**: nu există `[Required]`/FluentValidation — toată validarea de intrare e manuală, codificată în controllere/servicii (risc de inconsistență, dar simplu de urmărit în cod).
6. **Lipsă de middleware global de excepții**: gestionarea erorilor e ad-hoc, per-acțiune, cu blocuri `try/catch` (vizibil mai ales în `AuthController.Login`).
7. **Token JWT cu durată de viață scurtă** (4 ore) și validare strictă (`ClockSkew = 0`), plus handler-e de evenimente JWT pentru logging la autentificare eșuată/challenge/validare reușită.
8. **Hashing parole cu BCrypt** (cost implicit), nu PBKDF2/Argon2 — alegere standard, dar de menționat explicit ca decizie tehnică.
9. **Modelare bogată pentru activități de echipă**: `TeamActivity` conține o clasă imbricată `TeamActivityParticipation` și mai multe enum-uri (`ActivityType`, `RsvpStatus`) reprezentate ca string în BSON — un model de date relativ complex comparativ cu restul entităților.
10. **Sistem de migrare a schemei propriu**: există `SchemaMigrationRecord` și `ProfileAndTeamMigrationRunner` înregistrate în DI — un mecanism custom (nu o librărie externă) pentru evoluția schemei MongoDB la pornirea aplicației.
