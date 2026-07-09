# Raport tehnic — Frontend Angular „TeamConnect" (UI/)

> Bazat exclusiv pe codul din `UI/`. Unde nu există dovezi în cod, se menționează explicit „nu există în cod".

---

## 1. Structura proiectului

### 1.1 Arbore de directoare (relevant)

```
UI/src/
├── app/
│   ├── app.ts / app.html / app.scss          → componenta rădăcină (App)
│   ├── app.config.ts                          → providers globali (router, http, animații, change detection)
│   ├── app.routes.ts                          → configurația rutelor
│   ├── components/
│   │   └── side-panel/                        → panou lateral de navigare (singura componentă "reutilizabilă" generică)
│   ├── guards/
│   │   ├── auth.guard.ts                      → authGuard (CanActivateFn)
│   │   └── role.guard.ts                      → roleGuard (factory) + RoleGuardService
│   ├── interceptors/
│   │   └── auth.interceptor.ts                → authInterceptor (HttpInterceptorFn)
│   ├── layouts/
│   │   ├── main-layout.component.*            → layout cu side-panel pentru rute autentificate
│   │   └── auth-layout.component.*            → layout simplu pentru pagina de login
│   ├── models/
│   │   └── auth.models.ts                     → toate DTO-urile/interfețele (auth, user, team, join-request)
│   ├── pages/
│   │   ├── admin-page/
│   │   ├── feed-page/
│   │   ├── feedback-page/  (+ send-feedback-dialog.ts)
│   │   ├── gamification-page/
│   │   ├── growth-page/        (componenta se numește CohesionDashboard)
│   │   ├── home-page/
│   │   ├── login-page/
│   │   ├── profile-page/   (profile-view + profile-edit)
│   │   └── team-activities-page/ (+ create-team-activity-dialog, sync-meeting-dialog)
│   │   └── teams-page/      (teams-list + confirm-dialog, delete-team-dialog, manage-members-dialog, team-edit-dialog)
│   ├── services/
│   │   ├── auth.service.ts
│   │   ├── dashboard.service.ts
│   │   ├── feed.service.ts
│   │   ├── feedback.service.ts
│   │   ├── gamification.service.ts
│   │   ├── team-activities.service.ts
│   │   └── users.service.ts
│   └── shared/
│       ├── colleague-profile-dialog.component.*
│       └── icons.ts
├── environments/
│   ├── environment.ts
│   └── environment.production.ts
├── index.html / main.ts / test.ts
```

Nu există directoare separate de tipul `interfaces/`, `interceptors-http/` etc. în afara celor listate; toate DTO-urile auth/user/team sunt centralizate în `models/auth.models.ts`, în timp ce DTO-urile specifice unei funcționalități (feed, feedback, activities, gamification, dashboard) sunt declarate direct în fișierul serviciului corespunzător (ex. `FeedPostDto` în `feed.service.ts`).

### 1.2 Versiunea Angular și pachetele cheie (din `UI/package.json` și `node_modules`)

- **Angular runtime instalat:** `@angular/core@20.3.16`, `@angular/material@20.2.14` (intervalele declarate în `package.json` sunt `^20.0.0` / `^20.0.2`)
- `@angular/animations`, `@angular/cdk`, `@angular/common`, `@angular/compiler`, `@angular/forms`, `@angular/platform-browser`, `@angular/router` — toate `^20.0.x`
- `@angular/build` și `@angular/cli` — `^20.0.1` (folosesc noul builder Angular, nu Webpack/`@angular-devkit/build-angular`)
- `rxjs@~7.8.0`
- `zone.js@~0.15.0`
- `tailwindcss@^4.1.8` + `@tailwindcss/postcss@^4.1.8` + `postcss@^8.5.4` (configurate via `.postcssrc.json`)
- `tslib@^2.3.0`
- Dev/test: `karma`, `karma-jasmine`, `karma-chrome-launcher`, `jasmine-core`, `typescript@~5.8.2`

Nu există `@ngrx/*`, `ngxs`, `akita` sau alte librării de state management în `package.json` — gestionarea stării se face exclusiv cu `signal`/`computed` din Angular și `RxJS`/`HttpClient`.

### 1.3 Organizare și stil de componente

- **Organizare pe feature-uri**: directorul `pages/` grupează fiecare ecran (și dialogurile lui) într-un singur folder (ex. `teams-page/` conține lista de echipe + 4 dialoguri asociate), nu există separare pe tip de fișier (ex. `components/`, `models/` globale per feature).
- **100% standalone components** — fiecare componentă din proiect declară `standalone: true` (sau este implicit standalone, conform Angular ≥17 default) și își importă explicit modulele necesare (`CommonModule`, `MatButtonModule`, `RouterModule` etc.). Nu există niciun `NgModule` de feature/aplicație — `app.config.ts` configurează providerii prin `ApplicationConfig` și `bootstrapApplication`.
- Routing-ul folosește atât componente eager (`component: ...`) cât și `loadComponent` (lazy loading) pentru paginile mai mari (vezi secțiunea 3).

---

## 2. Componente și pagini

### 2.1 Pagini principale

| Componentă | Rol |
|---|---|
| `LoginPage` (`login-page`) | Formular unic de login/înregistrare cu comutare de mod (`isLoginMode`) |
| `HomePage` (`home-page`) | Pagina de start după autentificare |
| `FeedPage` (`feed-page`) | Feed social: postări, like-uri, comentarii |
| `FeedbackPage` (`feedback-page`) | Trimitere/primire feedback între colegi, cu analiză pe categorie/ton |
| `TeamActivitiesPage` (`team-activities-page`) | Activități de echipă (prompt-uri, sondaje, mini-provocări, trivia, întâlniri sincrone) |
| `CohesionDashboard` (`growth-page`) | Dashboard de „coeziune" a echipei (statistici de feedback per utilizator) |
| `GamificationPage` (`gamification-page`) | Clasament (leaderboard) per echipă |
| `ProfileViewComponent` / `ProfileEditComponent` (`profile-page`) | Vizualizare și editare profil utilizator |
| `TeamsListComponent` (`teams-page`) | Listă echipe, creare/editare/ștergere echipă, gestionare membri și cereri de înscriere |
| `AdminDashboardComponent` (`admin-page`) | Panou de administrare (utilizatori, echipe, echipe fără owner) — accesibil doar rolului `Admin` |

### 2.2 Componente reutilizabile / structurale

- `SidePanelComponent` — panou lateral de navigare, afișează echipele utilizatorului curent, comută stare extins/restrâns persistată în `localStorage` (`sideCollapsed`), expune `canSeeDashboard()` în funcție de rol/owner
- `MainLayoutComponent` — layout cu `SidePanelComponent` + `<router-outlet>`, folosit pentru toate rutele protejate
- `AuthLayoutComponent` — layout minimal pentru ruta `/login`
- `App` (`app.ts`) — componenta rădăcină, importă `RouterModule` și `MatButtonModule`

### 2.3 Componente de tip dialog/modal (toate folosesc `MatDialog`/`MAT_DIALOG_DATA`)

- `SendFeedbackDialog` (`feedback-page/send-feedback-dialog.ts`)
- `CreateTeamActivityDialogComponent`, `SyncMeetingDialogComponent` (`team-activities-page/`)
- `ConfirmDialogComponent`, `DeleteTeamDialogComponent`, `ManageMembersDialogComponent`, `TeamEditDialogComponent` (`teams-page/`)
- `ColleagueProfileDialogComponent` (`shared/`) — dialog comun pentru afișarea profilului unui coleg, reutilizat din mai multe pagini

---

## 3. Rutare și protecția rutelor

Configurația completă este în `app.routes.ts`:

- Două ramuri principale:
  - `''` → `MainLayoutComponent` (cu `SidePanelComponent`), conține toate rutele aplicației propriu-zise
  - `'login'` → `AuthLayoutComponent`, conține doar `LoginPage`
- Rută implicită: `{ path: '', redirectTo: 'home', pathMatch: 'full' }`
- Rută wildcard: `{ path: '**', redirectTo: 'home' }`

**Rute eager (componentă încărcată direct):** `home`, `feed`, `teams/:teamId/feedback`, `teams/:teamId/dashboard`, `login`

**Rute lazy-loaded (`loadComponent`)**:
- `teams/:teamId/activities` → `TeamActivitiesPage`
- `teams/:teamId/leaderboard` → `GamificationPage`
- `profile`, `profile/edit` → `ProfileViewComponent` / `ProfileEditComponent`
- `teams` → `TeamsListComponent`
- `admin` → `AdminDashboardComponent`

### Guards

- **`authGuard`** (`guards/auth.guard.ts`) — `CanActivateFn` funcțional; verifică `authService.isAuthenticated()` (un `computed` bazat pe existența unui token în `tokenSignal`); dacă utilizatorul nu e autentificat, redirecționează la `/login` cu `queryParams: { returnUrl: state.url }`. Aplicat pe **toate** rutele din `MainLayoutComponent`.
- **`roleGuard(allowedRoles: string[])`** (`guards/role.guard.ts`) — factory ce produce un `CanActivateFn`; citește `authService.currentUserRole()` și redirecționează la `/` dacă rolul curent nu este în lista permisă. Folosit explicit doar pentru `admin`: `canActivate: [authGuard, roleGuard(['Admin'])]`.
- Există și o clasă `RoleGuardService` (injectabilă, `providedIn: 'root'`) cu metodele `canActivateAdmin()` și `canActivateAdminOrTeamOwner()`, dar **nu este referențiată în `app.routes.ts`** — pare cod auxiliar/neutilizat în configurația curentă de rutare (nu există în cod nicio rută care să o folosească).

---

## 4. Comunicarea cu backend-ul

### 4.1 Servicii și endpoint-uri consumate

Toate serviciile sunt `@Injectable({ providedIn: 'root' })`, injectează `HttpClient` și folosesc `environment.apiUrl` ca bază (`https://localhost:7241/api` în dezvoltare).

| Serviciu | Endpoint-uri |
|---|---|
| `AuthService` | `POST /auth/register`, `POST /auth/login`, `DELETE /users/me` |
| `UsersService` | `GET/PUT /users`, `/users/me`, `/users/{id}`, `/users/teammates`, `/users/teammates/{teamId}`, `GET/POST /teams`, `GET/PUT/DELETE /teams/{id}`, `POST /teams/{id}/join-requests`, `GET /teams/join-requests`, `GET /teams/{id}/join-requests`, `PUT /teams/join-requests/{id}/approve|reject`, `POST /teams/{id}/add/{userId}`, `DELETE /teams/{id}/members/{userId}` |
| `FeedService` | `GET/POST /feed`, `POST/DELETE /feed/{id}/like`, `POST/GET /feed/{id}/comments`, `DELETE /feed/{id}` |
| `FeedbackService` | `POST /feedback`, `GET /feedback/received` |
| `TeamActivitiesService` | `GET /teams/{id}/activities`, `GET /teams/{id}/activities/summary`, `POST /teams/{id}/activities`, `POST /teams/{id}/activities/{activityId}/responses`, `POST /teams/{id}/activities/{activityId}/complete` |
| `GamificationService` | `GET /gamification/leaderboard/{teamId}` |
| `DashboardService` | `GET /dashboard/cohesion/{teamId}` |

### 4.2 Interceptorul HTTP de autentificare

`authInterceptor` (funcțional, `HttpInterceptorFn`, înregistrat în `app.config.ts` prin `provideHttpClient(withInterceptors([authInterceptor]))`):

- preia tokenul din `authService.getToken()`
- dacă există token, clonează request-ul și adaugă header-ul `Authorization: Bearer <token>`
- identifică cererile de auth (`/auth/login`, `/auth/register`) prin `req.url.includes(...)` pentru a evita auto-logout pe ele

### 4.3 Tratarea erorilor HTTP

Tratarea erorilor 401 este realizată **chiar în `authInterceptor`**, nu printr-un interceptor de erori separat:

```ts
return next(clonedRequest).pipe(
  catchError((error) => {
    if (error?.status === 401 && !isAuthRequest) {
      authService.logout();
    }
    return throwError(() => error);
  })
);
```

— la primirea unui `401` (în afara cererilor de login/register), se apelează `authService.logout()`, care șterge tokenul/utilizatorul și navighează la `/login`. Nu există un interceptor de erori dedicat (separat de cel de autentificare) și nu există un mecanism global de notificare/toast pentru erori — gestionarea suplimentară se face local, la nivel de componentă (ex. `errorMessage` în `LoginPage`).

---

## 5. Gestionarea stării și a sesiunii

- **Stocarea tokenului JWT**: în `localStorage`, sub cheia `'token'` (`AuthService.handleAuthSuccess`/`updateToken`/`clearAuthState`), și în paralel într-un `signal<string | null>` (`tokenSignal`), pentru reactivitate în UI.
- **Starea de autentificare și utilizatorul curent** sunt menținute exclusiv în `AuthService`, prin `signal`-uri și `computed`-uri:
  - `currentUserSignal: signal<User | null>`
  - `tokenSignal: signal<string | null>`
  - `currentUser`, `isAuthenticated`, `currentUserRole`, `isAdmin`, `isTeamOwner` — toate `computed()` derivate din semnalele de mai sus
- La inițializarea serviciului (constructor), tokenul este recitit din `localStorage`; dacă a expirat (verificat prin decodarea claim-ului `exp` din JWT, cu un buffer de 60 de secunde), este șters și starea curățată.
- **Datele utilizatorului** (`User`/`UserDto`) nu provin dintr-un apel API la pornire, ci sunt **derivate direct din payload-ul JWT** (decodare base64url manuală + extragere de claim-uri standard/Microsoft: `sub`/`nameid`, `unique_name`/`name`, `email`, `role`).
- **State management**: nu există NgRx/NgXs/Akita (confirmat — lipsesc din `package.json`). Starea globală (sesiune, utilizator) e ținută cu Angular **signals**; comunicarea cu API-ul și fluxurile asincrone folosesc **RxJS** (`Observable`, operatorii `tap`, `catchError`, `throwError`). Componentele de pagină (ex. `feed-page`, `team-activities-page`) folosesc de asemenea `signal()` local (cel puțin 5 apeluri fiecare, conform grep) pentru starea UI (liste încărcate, loading, formulare).
- `SidePanelComponent` persistă starea de „colaps" a panoului în `localStorage` (`sideCollapsed`).

---

## 6. Flux complet — exemplu de autentificare (login)

**Formular** (`login-page.ts` / `.html`): un singur `LoginPage` standalone gestionează atât login cât și register, comutate prin `isLoginMode = signal(true)`. Datele formularului sunt legate prin `[(ngModel)]` (folosind `FormsModule`) la obiectele simple `loginDto`/`registerDto` (nu se folosesc Reactive Forms aici — vezi secțiunea 7).

**Submit → `AuthService` → API → stocare token → redirect:**

```ts
// login-page.ts
onSubmit(): void {
  this.errorMessage.set('');
  this.isLoading.set(true);

  if (this.isLoginMode()) {
    this.authService.login(this.loginDto).subscribe({
      next: () => { this.isLoading.set(false); },
      error: (error) => {
        this.isLoading.set(false);
        this.errorMessage.set('Invalid email or password');
      }
    });
  }
  // ...
}
```

```ts
// auth.service.ts
login(dto: LoginDto): Observable<AuthResponse> {
  return this.http.post<AuthResponse>(`${this.apiUrl}/auth/login`, {
    email: dto.email,
    password: dto.password,
  }).pipe(
    tap(response => this.handleAuthSuccess(response))
  );
}

private handleAuthSuccess(response: AuthResponse): void {
  this.tokenSignal.set(response.token);
  localStorage.setItem('token', response.token);
  this.loadUserFromToken(response.token);
  this.router.navigate(['/']);
}
```

Fluxul: `LoginPage.onSubmit()` → `AuthService.login()` (`POST /auth/login`) → la succes, `handleAuthSuccess` setează `tokenSignal`, scrie tokenul în `localStorage`, decodează JWT-ul pentru a popula `currentUserSignal` (rol, nume, email), apoi `router.navigate(['/'])` → ruta rădăcină redirecționează către `home` (protejată de `authGuard`, care acum trece pentru că `isAuthenticated()` e `true`).

---

## 7. Tehnologii și pachete

- **Framework**: Angular 20 (`@angular/core@20.3.16`), CLI/build cu `@angular/build@^20.0.1` (noul builder ESBuild/Vite, nu Webpack)
- **UI library**: Angular Material (`@angular/material@20.2.14`) + Angular CDK (`@angular/cdk@^20.0.2`) — folosite pentru butoane (`MatButtonModule`), iconuri (`MatIconModule`), dialoguri (`MatDialog`)
- **Stilizare utilitară**: Tailwind CSS v4 (`tailwindcss@^4.1.8`, `@tailwindcss/postcss@^4.1.8`), configurat prin PostCSS (`.postcssrc.json`); SCSS per-componentă pentru stiluri specifice (`styleUrls`)
- **Formulare**: `FormsModule` cu `[(ngModel)]` (template-driven forms) — **nu** s-a găsit `ReactiveFormsModule`/`FormGroup`/`FormBuilder` în paginile examinate (login, etc.); deci aplicația folosește forms template-driven, nu reactive forms
- **HTTP**: `HttpClient` (provideHttpClient cu interceptori funcționali, fără `HttpClientModule`)
- **Reactivitate**: `signal`/`computed` (Angular Signals) + RxJS `~7.8.0`
- **Change detection**: configurabil condiționat — `provideZonelessChangeDetection()` dacă `Zone` nu e definit, altfel `provideZoneChangeDetection()` (cod în `app.config.ts`); proiectul include totuși `zone.js@~0.15.0` ca dependență
- **Animații**: `provideAnimationsAsync()` din `@angular/platform-browser/animations/async`
- **Teste unitare**: Karma + Jasmine (`karma`, `karma-jasmine`, `jasmine-core`, `karma-chrome-launcher`, `karma-coverage`)
- **Teste E2E**: Playwright, într-un proiect separat la rădăcina repo-ului (`e2e/`), cu suite pentru `auth`, `feed`, `feedback`, `home`, `profile`, `team-join-requests`, `teams`

---

## 8. Etapele dezvoltării frontend-ului

Pe baza istoricului `git log --oneline --reverse -- UI/` (de la primul commit care atinge `UI/` până la cel mai recent), evoluția se poate grupa astfel:

### Etapa 1 — Bootstrap și autentificare de bază
Primele commit-uri introduc proiectul UI și fluxul minim de autentificare: `move ui ad add api`, `Add auth UI and enforce JSON case sensitivity`, `Pute auth routes to api/auth + add copilot context`, `Update copilot context with be dtos`. Aici se pun bazele: rutele de `/auth`, formularul de login/register, primele DTO-uri.

### Etapa 2 — Layout, navigare și stilizare
Urmează un grup de commit-uri axat pe structura vizuală: `Refactor API URL to use environment variables`, `Enhance UI with Tailwind CSS hover effects and add SVG icons`, `Refactor activities and feedback pages to use standalone components and signals`, `Enhance back button design with SVG icon...`, `Implement user management features with user service...`. În această etapă apare integrarea Tailwind CSS, primele iconuri SVG, și **migrarea explicită spre standalone components + signals** pentru paginile de activități și feedback.

### Etapa 3 — Funcționalități sociale (feed, comentarii, like-uri)
`Add full name support for users...`, `Add author email to feed posts and feedback responses...`, `Replace ActivitiesPage with FeedPage, update routing and add new components`, `Implement comments and likes functionality for feed posts...`, `Update send/button style in feed page...`. Se construiește feed-ul social ca înlocuitor al unei pagini anterioare de „Activities", cu like și comentarii.

### Etapa 4 — Navigare laterală și refactorizare de layout
`Add side navigation panel with toggle functionality and update routing`, `Refactor layout structure by removing side panel and implementing main and auth layouts`, `Remove placeholder text from login and registration forms`, `Add explicit void return types to toggle()...`, `Remove utility prefix from Tailwind CSS configuration`. Aici apare **`SidePanelComponent`** și separarea în `MainLayoutComponent`/`AuthLayoutComponent` (refactorizare majoră a structurii de layout, care rămâne arhitectura curentă).

### Etapa 5 — Administrare și gestionare echipe
`add admin dashboard component with user and team data loading`, `refactor admin dashboard to use signals...`, `update button styles ... to use Angular Material buttons`, `enhance team and user profile handling with validation...`, `implement team management dialogs and enhance team member handling`, `Typo`, `enhance team member management with user existence checks...`, `add team activity management features`. Aceasta este etapa de extindere funcțională majoră: panoul de admin, dialogurile de gestionare a echipelor/membrilor și activitățile de echipă (Prompt/Poll/Trivia/MiniChallenge/SyncMeeting).

### Etapa 6 — Stabilizare CI și testare
`Potential fix for pull request finding`, `Fix comments`, `Enhance application configuration and logging`, `CI: stabilize frontend tests by installing Chromium and running under xvfb; make Angular dist path configurable; add CI badge`, `fix`, `Implement email notification system with SMTP support and add tests`, `remove some text & document deployment`, `remove comments`. Etapă centrată pe maturizarea pipeline-ului CI (Chromium/xvfb pentru Karma headless) și pe documentarea deployment-ului.

### Etapa 7 — Testare E2E și funcționalități finale
`feat: add delete post/account + Playwright E2E tests with CI integration`, `Add logo icons`, `side pannel reorganization`, `Improve separation between teams`, `make pages team separated`, `Improvements to the feed page design`. **Aici apare suita Playwright** (`e2e/`) și integrarea ei în CI, plus funcționalitatea de ștergere postare/cont, și separarea conținutului pe echipe (teams-scoped pages).

### Etapa 8 — Feedback avansat, gamification și ajustări fine
`feat: redesign feedback page with category/tone analytics and dialog flow`, `Add/adapt tests for new feedback`, `add leader board and cleaning for e2e created teams add profile dialog for other users`, urmate de o serie de mici îmbunătățiri (`Activities page sync ... and other improvements`, `Sall adjustments to activites page`, `change badges with icons`, `smallimprovements`). Se adaugă pagina de gamification/leaderboard, dialogul de profil al colegilor (`ColleagueProfileDialogComponent`), redesign-ul paginii de feedback cu analize pe categorie/ton, și sincronizarea întâlnirilor (sync meetings) în activitățile de echipă.

---

## Rezumat — particularități reale ale implementării frontend

- **Sesiune fără apel „whoami"**: utilizatorul curent (nume, email, rol, id) nu vine printr-un endpoint de profil la autentificare, ci este **decodat manual din payload-ul JWT** (Base64URL → JSON, cu mapare explicită pe claim-urile standard și pe cele de tip Microsoft `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`). Acest lucru face `AuthService` dependent de structura exactă a token-ului emis de backend.
- **Verificare locală a expirării tokenului**: `isTokenExpired()` decodează `exp` din JWT și aplică un buffer de 60 de secunde, fără a contacta backend-ul — o decizie complet client-side.
- **Logica de eroare 401 trăiește în interceptorul de autentificare**, nu într-un interceptor de erori separat — `authInterceptor` face dublă treabă (atașare token + auto-logout pe 401), ceea ce e neobișnuit față de pattern-ul comun „un interceptor per responsabilitate".
- **Forms template-driven, nu reactive**: pagina de login (și aparent restul, din câte s-a verificat) folosesc `FormsModule`/`[(ngModel)]`, nu `ReactiveFormsModule` — alegere arhitecturală simplă, dar mai puțin tipică pentru aplicații Angular Material complexe.
- **Change detection condiționat**: `app.config.ts` alege dinamic, la runtime, între `provideZonelessChangeDetection()` și `provideZoneChangeDetection()` în funcție de existența globalului `Zone` — o configurație hibridă neobișnuită, având în vedere că `zone.js` rămâne totuși o dependență declarată.
- **`RoleGuardService` neutilizat**: există o clasă completă de guard bazată pe servicii (`canActivateAdmin`, `canActivateAdminOrTeamOwner`), dar configurația curentă de rutare folosește exclusiv guard-uri funcționale (`authGuard`, `roleGuard`) — cod rezidual/candidat la curățare.
- **DTO-uri descentralizate**: doar entitățile „nucleu" (auth, user, team, join-request) sunt în `models/auth.models.ts`; restul DTO-urilor (feed, feedback, activități, gamification, dashboard) sunt definite direct lângă serviciul care le consumă — organizare pragmatică pe feature, dar inconsistentă ca locație.
- **Arhitectură 100% standalone**, fără niciun `NgModule`, cu lazy loading selectiv prin `loadComponent` pentru paginile mai „grele" (profil, echipe, admin, activități, leaderboard), dar eager loading pentru `home`, `feed`, `feedback`, `dashboard`.
- **Testare pe două niveluri**: unit/component cu Karma+Jasmine (limitat la câteva pagini: `app`, `feed-page`, `feedback-page`, `growth-page`, `home-page`, `login-page`, `auth.service`) și E2E cu Playwright într-un proiect separat la rădăcina repo-ului (`e2e/`), integrat în CI cu instalare Chromium și rulare sub `xvfb`.
