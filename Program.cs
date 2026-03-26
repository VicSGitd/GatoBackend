using Microsoft.EntityFrameworkCore;
using GatoBackend.Hubs;
using GatoBackend.Services;
using GatoBackend.Data;
using GatoBackend.Models;
using BCrypt.Net; // Para encriptar contraseñas
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// --- JWT: Authentication Configuration ---
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/gamehub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// --- 1. CONFIGURACIÓN DE SERVICIOS ---

builder.Services.AddRateLimiter(options => {
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
    options.RejectionStatusCode = 429;
});

// Conectar PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSignalR();
builder.Services.AddSingleton<GameManager>();

builder.Services.AddCors(options => {
    options.AddPolicy("AllowMyFrontend", policy => {
        policy.WithOrigins("http://localhost:4200", "http://localhost:5100")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// --- 2. PIPELINE Y MIDDLEWARES ---

// Seguridad: Middleware de Headers (OWASP)
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdnjs.cloudflare.com; style-src 'self' 'unsafe-inline';");
    await next();
});

app.UseRateLimiter(); // Seguridad: Rate Limiting
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("AllowMyFrontend");

app.UseAuthentication();
app.UseAuthorization();

// --- 3. ENDPOINTS DE AUTENTICACIÓN (Minimal APIs) ---

// Registro
app.MapPost("/api/register", async (User loginData, AppDbContext db) =>
{
    // Seguridad: Validación de entradas (OWASP)
    if (string.IsNullOrWhiteSpace(loginData.Username) || loginData.Username.Length < 3 || loginData.Username.Length > 50)
        return Results.BadRequest("Nombre de usuario inválido (debe tener entre 3 y 50 caracteres).");
        
    if (string.IsNullOrWhiteSpace(loginData.PasswordHash) || loginData.PasswordHash.Length < 4)
        return Results.BadRequest("La contraseña debe tener al menos 4 caracteres.");

    if (await db.Users.AnyAsync(u => u.Username == loginData.Username))
        return Results.BadRequest("El usuario ya existe.");

    var newUser = new User
    {
        Username = loginData.Username,
        // Encriptamos la contraseña antes de guardarla
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(loginData.PasswordHash) 
    };

    db.Users.Add(newUser);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "Usuario registrado exitosamente" });
});

// Login
app.MapPost("/api/login", async (User loginData, AppDbContext db) =>
{
    // Seguridad: Validación de entradas (OWASP)
    if (string.IsNullOrWhiteSpace(loginData.Username) || string.IsNullOrWhiteSpace(loginData.PasswordHash))
        return Results.BadRequest("Los campos no pueden estar vacíos.");

    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == loginData.Username);
    
    // Verificamos que el usuario exista y que la contraseña coincida con el hash
    if (user == null || !BCrypt.Net.BCrypt.Verify(loginData.PasswordHash, user.PasswordHash))
        return Results.Unauthorized();

    // JWT: Generamos el Token
    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new[] { new Claim(ClaimTypes.Name, user.Username) };

    var token = new JwtSecurityToken(
        issuer: builder.Configuration["Jwt:Issuer"],
        audience: builder.Configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.Now.AddHours(2),
        signingCredentials: credentials);

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

    // Si es exitoso, devolvemos los datos del usuario (sin el password) y su token
    return Results.Ok(new { 
        token = tokenString,
        user.Username, 
        user.GamesWon, 
        user.GamesLost, 
        user.Draws 
    });
});

// --- 4. SIGNALR ---
app.MapHub<GameHub>("/gamehub");

app.Run();