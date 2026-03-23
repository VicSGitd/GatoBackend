using Microsoft.EntityFrameworkCore;
using GatoBackend.Hubs;
using GatoBackend.Services;
using GatoBackend.Data;
using GatoBackend.Models;
using BCrypt.Net; // Para encriptar contraseñas

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURACIÓN DE SERVICIOS ---

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
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("AllowMyFrontend");

// --- 3. ENDPOINTS DE AUTENTICACIÓN (Minimal APIs) ---

// Registro
app.MapPost("/api/register", async (User loginData, AppDbContext db) =>
{
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
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == loginData.Username);
    
    // Verificamos que el usuario exista y que la contraseña coincida con el hash
    if (user == null || !BCrypt.Net.BCrypt.Verify(loginData.PasswordHash, user.PasswordHash))
        return Results.Unauthorized();

    // Si es exitoso, devolvemos los datos del usuario (sin el password)
    return Results.Ok(new { 
        user.Username, 
        user.GamesWon, 
        user.GamesLost, 
        user.Draws 
    });
});

// --- 4. SIGNALR ---
app.MapHub<GameHub>("/gamehub");

app.Run();