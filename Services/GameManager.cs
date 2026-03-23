using GatoBackend.Models;

namespace GatoBackend.Services;

public class GameManager
{
    public Dictionary<string, GameRoom> Games { get; set; } = new();
    
    // Cambiamos Queue por List para poder eliminar jugadores fácilmente si se desconectan
    public List<Player> WaitingPlayers { get; set; } = new(); 
}