using GatoBackend.Models;

namespace GatoBackend.Services;

public class GameManager
{
    public Dictionary<string, GameRoom> Games { get; set; } = new();
    public Queue<Player> WaitingPlayers { get; set; } = new();
}