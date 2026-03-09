//Ventanilla de comunicación inicial
//Aquí se escribirá las acciones de Angular, se pedirá en el backend, a través de SingalR

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