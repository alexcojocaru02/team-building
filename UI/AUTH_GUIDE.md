# Authentication Implementation Guide

## Overview
This Angular application now includes a complete authentication system with login, register, and route guards.

## What Was Implemented

### 1. **Models** (`src/app/models/auth.models.ts`)
- `LoginDto`: Email and password for login
- `RegisterDto`: Email, password, and optional name for registration
- `AuthResponse`: Token returned from API
- `User`: User information decoded from JWT

### 2. **Auth Service** (`src/app/services/auth.service.ts`)
- `register()`: Register new user
- `login()`: Login existing user
- `logout()`: Clear token and redirect to login
- `getToken()`: Get current JWT token
- `currentUser`: Signal with current user info
- `isAuthenticated`: Signal indicating if user is logged in

### 3. **Login Page** (`src/app/pages/login-page/`)
- Beautiful responsive login/register form
- Toggles between login and register modes
- Error handling
- Uses TailwindCSS for styling

### 4. **Auth Guard** (`src/app/guards/auth.guard.ts`)
- Protects routes from unauthenticated access
- Redirects to login page with return URL

### 5. **Auth Interceptor** (`src/app/interceptors/auth.interceptor.ts`)
- Automatically adds JWT token to all HTTP requests
- Sets `Authorization: Bearer {token}` header

### 6. **Protected Routes** (`src/app/app.routes.ts`)
All routes except `/login` are protected with `authGuard`:
- `/` - Home (protected)
- `/feedback` - Feedback (protected)
- `/activities` - Activities (protected)
- `/growth` - Growth (protected)
- `/login` - Login/Register (public)

## Configuration

### Update API URL
Edit `src/app/services/auth.service.ts` and update the `apiUrl`:

```typescript
private apiUrl = 'https://your-api-url.com/api/auth';
```

### JWT Token Claims
The service expects these JWT claims:
- `sub` or `nameid`: User ID
- `email`: User email
- `name` or `unique_name`: User name

If your JWT structure is different, modify the `loadUserFromToken()` method in `auth.service.ts`.

## Usage

### Login Flow
1. User navigates to `/login`
2. Enters email and password
3. On success, JWT token is stored in `localStorage`
4. User is redirected to home page
5. All subsequent API calls include the JWT token

### Register Flow
1. User navigates to `/login` and clicks "Sign Up"
2. Enters name (optional), email, and password
3. On success, JWT token is stored and user is logged in
4. User is redirected to home page

### Logout
- Click the "Logout" button on the home page
- Token is removed from `localStorage`
- User is redirected to `/login`

### Protected Routes
- Any attempt to access protected routes without authentication redirects to login
- After login, user is redirected back to the originally requested URL

## Features

### Auto-Login
- Token is stored in `localStorage`
- On page refresh, user remains logged in
- Token is automatically decoded to restore user information

### Token Expiration
- Currently, the app doesn't check token expiration
- To add expiration checking, modify `loadUserFromToken()` to check the `exp` claim

### Error Handling
- Login errors show "Invalid email or password"
- Register errors show "User may already exist"
- Network errors are caught and displayed

## Security Notes

1. **HTTPS**: Always use HTTPS in production
2. **Token Storage**: Tokens are stored in `localStorage`. For higher security, consider using `httpOnly` cookies
3. **CORS**: Ensure your ASP.NET API has CORS configured for your Angular app URL
4. **Token Expiration**: Consider implementing token refresh logic for better UX

## ASP.NET API CORS Configuration

Add this to your `Program.cs` or `Startup.cs`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200") // Your Angular URL
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

app.UseCors("AllowAngularApp");
```

## Testing

1. Start your ASP.NET API
2. Update the API URL in `auth.service.ts`
3. Run `npm start` in the Angular project
4. Navigate to `http://localhost:4200`
5. You should be redirected to `/login`
6. Try registering a new account
7. Try logging in
8. Verify you can access protected routes

## Troubleshooting

### "User exists" error on register
- The email is already registered in the database

### "Invalid email or password" on login
- Verify credentials are correct
- Check API is running and accessible

### Redirects to login immediately after logging in
- Check browser console for errors
- Verify token is being stored in `localStorage`
- Check JWT is valid and not expired

### API calls fail with 401
- Verify interceptor is adding the token
- Check token format is correct (`Bearer {token}`)
- Verify API accepts the JWT

## Next Steps

Consider implementing:
- [ ] Token refresh mechanism
- [ ] Remember me functionality
- [ ] Password reset flow
- [ ] Email verification
- [ ] Role-based access control
- [ ] Token expiration checking
- [ ] Better error messages
- [ ] Loading states
