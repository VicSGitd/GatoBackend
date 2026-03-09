using Microsoft.AspNetCore.SignalR;
using GatoBackend.Models;
using GatoBackend.Services;

namespace GatoBackend.Hubs;

public class GameHub : Hub
{
    private readonly GameManager _gameManager;

    public GameHub(GameManager gameManager)
    {
        _gameManager = gameManager;
    }


}