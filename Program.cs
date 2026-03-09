using GatoBackend.Hubs;
using GatoBackend.Services;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURACIÓN DE SERVICIOS (Dependency Injection) ---

// Habilitar SignalR
builder.Services.AddSignalR();

// Registrar el estado del juego como Singleton (único para todo el servidor)
builder.Services.AddSingleton<GameManager>();

// Configurar CORS para permitir que Angular/Blazor se conecte al backend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMyFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200", "http://localhost:5100") // Puertos comunes de Angular y Blazor
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // Muy importante para SignalR
        });
});

var app = builder.Build();

// --- 2. CONFIGURACIÓN DEL PIPELINE (Middleware) ---
// ¡El orden aquí es importante!

// Habilitar la lectura de archivos estáticos (como tu index.html en wwwroot)
app.UseDefaultFiles();
app.UseStaticFiles();

// Aplicar la política de CORS que creamos arriba
app.UseCors("AllowMyFrontend");

// Mapear la ruta de tu Hub de SignalR
app.MapHub<GameHub>("/gamehub");

// Iniciar el servidor
app.Run();