using GatoBackend.Hubs;
using GatoBackend.Services;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
builder.Services.AddSingleton<GameManager>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularClient",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});
var app = builder.Build();
app.UseCors("AllowAngularClient");
app.MapHub<GameHub>("/gamehub");
app.MapGet("/", () => "¡Servidor del Gato funcionando! Conéctate por SignalR en /gamehub");
app.Run();