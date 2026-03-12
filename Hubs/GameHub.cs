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
        if (!_gameManager.Games.TryGetValue(roomId, out var game))
        {
            Console.WriteLine($"[ERROR] No se encontró la sala: {roomId}");
            return;
        }

        if (game.IsGameOver)
        {
            Console.WriteLine("[RECHAZADO] El juego ya terminó.");
            return;
        }

        if (game.Board[position] != null)
        {
            Console.WriteLine($"[RECHAZADO] La casilla {position} ya está ocupada.");
            return;
        }

        if (game.CurrentTurnConnectionId != Context.ConnectionId)
        {
            Console.WriteLine($"[RECHAZADO] Intento de trampa o error de turno. Turno actual: {game.CurrentTurnConnectionId} | Quien intentó tirar: {Context.ConnectionId}");
            return;
        }

        // --- Si pasa todas las validaciones, hace el movimiento normal ---
        Console.WriteLine($"[ÉXITO] Movimiento válido en posición {position} para la sala {roomId}");
        
        var isPlayer1 = game.Player1.ConnectionId == Context.ConnectionId;
        game.Board[position] = isPlayer1 ? game.Player1.Symbol : game.Player2.Symbol;

        CheckWinCondition(game);

        if (!game.IsGameOver)
        {
            game.CurrentTurnConnectionId = isPlayer1 ? game.Player2.ConnectionId : game.Player1.ConnectionId;
        }

        await Clients.Group(roomId).SendAsync("UpdateBoard", game);
    }
    // 4. Reiniciar la partida
    public async Task RestartGame(string roomId)
    {
        if (!_gameManager.Games.TryGetValue(roomId, out var game)) return;

        // Solo permitimos reiniciar si la partida actual ya terminó
        if (!game.IsGameOver) return;

        // Limpiamos el tablero y reseteamos el estado
        game.Board = new string[9]; 
        game.IsGameOver = false;
        game.WinnerConnectionId = string.Empty;
        
        // Hacemos que el Jugador 1 empiece esta nueva ronda
        game.CurrentTurnConnectionId = game.Player1.ConnectionId;

        // Avisamos a ambos jugadores que el juego se ha reiniciado
        await Clients.Group(roomId).SendAsync("GameRestarted", game);
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