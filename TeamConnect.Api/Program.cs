using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;
using TeamConnect.Api.Modules.Auth;
using TeamConnect.Api.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        // Make JSON property name matching case-sensitive
        opts.JsonSerializerOptions.PropertyNameCaseInsensitive = false;
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// Configure CORS for Angular app
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        // Read allowed origins from configuration (set via appsettings or environment variables).
        // Guard against an empty or whitespace-only setting which would produce an empty
        // origins array and cause WithOrigins() to throw. Fall back to the default
        // localhost origin and log a warning so misconfiguration is visible.
        var origins = builder.Configuration["Cors:AllowedOrigins"] ?? string.Empty;
        var originArray = origins.Split(',').Select(o => o.Trim()).Where(o => !string.IsNullOrEmpty(o)).ToArray();

        if (originArray.Length == 0)
        {
            originArray = new[] { "http://localhost:4200" };
            Console.WriteLine("Warning: 'Cors:AllowedOrigins' is empty; falling back to default http://localhost:4200");
        }

        policy.WithOrigins(originArray)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TeamConnect.Api", Version = "v1" });

    // Generate operationIds which are required by Azure API management

    c.CustomOperationIds(e =>
    {
        // Safely get controller name
        var controller = "UnknownController";
        if (e.ActionDescriptor.RouteValues != null &&
            e.ActionDescriptor.RouteValues.TryGetValue("controller", out var cName) &&
            !string.IsNullOrWhiteSpace(cName))
        {
            controller = cName;
        }

        var method = string.IsNullOrWhiteSpace(e.HttpMethod) ? "UNKNOWN" : e.HttpMethod.ToUpperInvariant();
        var path = e.RelativePath ?? string.Empty;

        // Build a raw id and replace path separators with underscore
        var raw = $"{controller}_{method}_{path.Replace('/', '_')}";

        // Keep only alphanumeric and underscore, collapse repeated underscores, trim and limit length
        var sanitized = Regex.Replace(raw, "[^A-Za-z0-9_]", "_");
        sanitized = Regex.Replace(sanitized, "_+", "_").Trim('_');
        if (sanitized.Length > 120) sanitized = sanitized.Substring(0, 120);

        return string.IsNullOrWhiteSpace(sanitized) ? "operation_unknown" : sanitized;
    });
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Enter JWT as: {your token}",

        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    c.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, Array.Empty<string>() }
    });
});
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ProfileAndTeamMigrationRunner>();

// Repositories
builder.Services.AddScoped<TeamConnect.Api.Shared.Repositories.IUserRepository, TeamConnect.Api.Shared.Repositories.UserRepository>();
builder.Services.AddScoped<TeamConnect.Api.Modules.Teams.ITeamRepository, TeamConnect.Api.Modules.Teams.TeamRepository>();
builder.Services.AddScoped<TeamConnect.Api.Modules.Feed.IFeedRepository, TeamConnect.Api.Modules.Feed.FeedRepository>();
builder.Services.AddScoped<TeamConnect.Api.Modules.Feedback.IFeedbackRepository, TeamConnect.Api.Modules.Feedback.FeedbackRepository>();
builder.Services.AddScoped<TeamConnect.Api.Modules.TeamActivities.ITeamActivityRepository, TeamConnect.Api.Modules.TeamActivities.TeamActivityRepository>();

// Services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<TeamConnect.Api.Modules.Teams.TeamsService>();
builder.Services.AddScoped<TeamConnect.Api.Modules.Feed.FeedService>();
builder.Services.AddScoped<TeamConnect.Api.Modules.Users.UsersService>();
builder.Services.AddScoped<TeamConnect.Api.Modules.Feedback.FeedbackService>();
builder.Services.AddScoped<TeamConnect.Api.Modules.Dashboard.DashboardService>();
builder.Services.AddScoped<TeamConnect.Api.Modules.TeamActivities.TeamActivitiesService>();

// Validate critical JWT configuration at startup and fail fast when missing.
// This prevents the app from starting with an empty signing key which would
// silently produce authentication errors or insecure behavior.
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("Configuration value 'Jwt:Key' is missing or empty. Set Jwt:Key in appsettings or environment variables.");
}
var jwtIssuer = builder.Configuration["Jwt:Issuer"]; 
var jwtAudience = builder.Configuration["Jwt:Audience"]; 
if (string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException("Configuration values 'Jwt:Issuer' and/or 'Jwt:Audience' are missing or empty. Set Jwt:Issuer and Jwt:Audience in appsettings or environment variables.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            ),

            // tighten clock skew for testing; increase as needed in production
            ClockSkew = TimeSpan.Zero
        };

        // Helpful for diagnosing invalid_token causes - writes exception to console
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine("JWT authentication failed: " + ctx.Exception?.Message);
                return Task.CompletedTask;
            },
            OnChallenge = ctx =>
            {
                // keep default behavior but log
                Console.WriteLine("JWT challenge: " + (ctx.Error ?? "no-error") + " - " + (ctx.ErrorDescription ?? ""));
                return Task.CompletedTask;
            },
            OnTokenValidated = ctx =>
            {
                Console.WriteLine("JWT token validated for " + ctx.Principal?.Identity?.Name);
                return Task.CompletedTask;
            }
        };
    });

// Register Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("AdminOrTeamOwner", policy => policy.RequireRole("Admin", "TeamOwner"));
});

var app = builder.Build();

var migrationModeArg = args.FirstOrDefault(a => a.StartsWith("--migration-mode=", StringComparison.OrdinalIgnoreCase));
if (!string.IsNullOrWhiteSpace(migrationModeArg))
{
    var mode = migrationModeArg.Split('=', 2)[1].Trim();
    var versionArg = args.FirstOrDefault(a => a.StartsWith("--migration-version=", StringComparison.OrdinalIgnoreCase));
    var version = !string.IsNullOrWhiteSpace(versionArg)
        ? versionArg.Split('=', 2)[1].Trim()
        : "2026-05-06-profile-team-v1";

    using var scope = app.Services.CreateScope();
    var runner = scope.ServiceProvider.GetRequiredService<ProfileAndTeamMigrationRunner>();

    var report = await runner.RunAsync(version, mode);
    var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine(json);

    return;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// In production ensure HSTS is enabled and HTTPS redirection runs before static file middleware
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

if (!app.Environment.IsDevelopment())
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

// CORS must be called before Authentication and Authorization
app.UseCors("AllowAngularApp");

// Authentication must run before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (!app.Environment.IsDevelopment())
{
    app.MapFallbackToFile("index.html");
}

app.Run();
