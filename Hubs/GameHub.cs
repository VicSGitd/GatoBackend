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

    // 1. Emparejamiento de jugadores
    public async Task FindMatch(string playerName)
    {
        var player = new Player { ConnectionId = Context.ConnectionId, Name = playerName };

        if (_gameManager.WaitingPlayers.Count > 0)
        {
            // Sacamos al jugador que estaba esperando
            var opponent = _gameManager.WaitingPlayers.Dequeue();
            
            // Asignamos símbolos
            opponent.Symbol = "X";
            player.Symbol = "O";

            var roomId = Guid.NewGuid().ToString();
            var game = new GameRoom
            {
                RoomId = roomId,
                Player1 = opponent,
                Player2 = player,
                CurrentTurnConnectionId = opponent.ConnectionId // Siempre empieza la X
            };

            _gameManager.Games.Add(roomId, game);

            // Metemos a ambos en un grupo de SignalR
            await Groups.AddToGroupAsync(player.ConnectionId, roomId);
            await Groups.AddToGroupAsync(opponent.ConnectionId, roomId);

            // Avisamos a los dos que el juego empezó y les mandamos el estado inicial
            await Clients.Group(roomId).SendAsync("GameStarted", game);
        }
        else
        {
            // Si no hay nadie, te toca esperar
            _gameManager.WaitingPlayers.Enqueue(player);
            await Clients.Caller.SendAsync("WaitingForOpponent");
        }
    }

    // 2. Lógica de movimientos
    public async Task MakeMove(string roomId, int position)
    {
        // Buscamos la partida
        if (!_gameManager.Games.TryGetValue(roomId, out var game)) return;

        // Validaciones: que el juego no haya terminado, que la casilla esté vacía y que sea tu turno
        if (game.IsGameOver || game.Board[position] != null || game.CurrentTurnConnectionId != Context.ConnectionId)
        {
            return; 
        }

        // Registrar el movimiento
        var isPlayer1 = game.Player1.ConnectionId == Context.ConnectionId;
        game.Board[position] = isPlayer1 ? game.Player1.Symbol : game.Player2.Symbol;

        // Revisar si con este movimiento alguien ganó
        CheckWinCondition(game);

        if (!game.IsGameOver)
        {
            // Si nadie ha ganado, cambiamos el turno
            game.CurrentTurnConnectionId = isPlayer1 ? game.Player2.ConnectionId : game.Player1.ConnectionId;
        }

        // Enviamos el tablero actualizado a ambos jugadores
        await Clients.Group(roomId).SendAsync("UpdateBoard", game);
    }

    // 3. Lógica matemática para ganar o empatar
    private void CheckWinCondition(GameRoom game)
    {
        var b = game.Board;
        
        // Todas las combinaciones posibles para ganar en un arreglo de 0 a 8
        int[][] winningCombos = new int[][]
        {
            new int[] {0, 1, 2}, new int[] {3, 4, 5}, new int[] {6, 7, 8}, // Horizontales
            new int[] {0, 3, 6}, new int[] {1, 4, 7}, new int[] {2, 5, 8}, // Verticales
            new int[] {0, 4, 8}, new int[] {2, 4, 6}                       // Diagonales
        };

        foreach (var combo in winningCombos)
        {
            // Si la primera casilla no está vacía y es igual a la segunda y a la tercera... ¡Hay ganador!
            if (b[combo[0]] != null && b[combo[0]] == b[combo[1]] && b[combo[1]] == b[combo[2]])
            {
                game.IsGameOver = true;
                game.WinnerConnectionId = game.CurrentTurnConnectionId;
                return;
            }
        }

        // Si no hay ganador, revisamos si hay empate (es decir, si ya no hay casillas en null)
        if (!b.Any(x => x == null))
        {
            game.IsGameOver = true;
            game.WinnerConnectionId = "Empate";
        }
    }
}