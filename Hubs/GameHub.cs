using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using GatoBackend.Models;
using GatoBackend.Services;
using GatoBackend.Data;

namespace GatoBackend.Hubs;

public class GameHub : Hub
{
    private readonly GameManager _gameManager;
    private readonly AppDbContext _dbContext; // Agregamos la conexión a la base de datos

    public GameHub(GameManager gameManager, AppDbContext dbContext)
    {
        _gameManager = gameManager;
        _dbContext = dbContext;
    }

    // --- NUEVO: Función que actualiza las estadísticas en PostgreSQL ---
    private async Task UpdateGameStatsAsync(GameRoom game)
    {
        // Buscamos a los dos usuarios en la base de datos usando el nombre que pasaron al conectarse
        var user1 = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == game.Player1.Name);
        var user2 = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == game.Player2.Name);

        // Si por alguna razón no los encuentra (ej. usuarios fantasma), no hacemos nada
        if (user1 == null || user2 == null) return;

        if (game.WinnerConnectionId == "Empate")
        {
            user1.Draws++;
            user2.Draws++;
        }
        else if (game.WinnerConnectionId == game.Player1.ConnectionId)
        {
            // Ganó el Jugador 1
            user1.GamesWon++;
            user2.GamesLost++;
        }
        else if (game.WinnerConnectionId == game.Player2.ConnectionId)
        {
            // Ganó el Jugador 2
            user2.GamesWon++;
            user1.GamesLost++;
        }

        // Guardamos los cambios en PostgreSQL
        await _dbContext.SaveChangesAsync();
        Console.WriteLine($"[BASE DE DATOS] Estadísticas actualizadas para {user1.Username} y {user2.Username}.");
    }

    private async Task HandlePlayerForfeit(string connectionId)
    {
        var activeGame = _gameManager.Games.Values.FirstOrDefault(g => 
            !g.IsGameOver && 
            (g.Player1.ConnectionId == connectionId || g.Player2.ConnectionId == connectionId));

        if (activeGame != null)
        {
            activeGame.IsGameOver = true;
            activeGame.WinnerConnectionId = activeGame.Player1.ConnectionId == connectionId 
                ? activeGame.Player2.ConnectionId 
                : activeGame.Player1.ConnectionId;

            Console.WriteLine($"[PARTIDA TERMINADA] Jugador {connectionId} abandonó.");

            // ¡ACTUALIZAMOS LA BD CUANDO ALGUIEN HUYE! (El que huye suma derrota, el otro victoria)
            await UpdateGameStatsAsync(activeGame);

            await Clients.Group(activeGame.RoomId).SendAsync("UpdateBoard", activeGame);
            _gameManager.Games.Remove(activeGame.RoomId);
        }
    }

    public async Task FindMatch(string playerName)
    {
        var connectionId = Context.ConnectionId;

        if (_gameManager.WaitingPlayers.Any(p => p.ConnectionId == connectionId)) return;

        var isAlreadyPlaying = _gameManager.Games.Values.Any(g => 
            !g.IsGameOver && 
            (g.Player1.ConnectionId == connectionId || g.Player2.ConnectionId == connectionId));

        if (isAlreadyPlaying)
        {
            await HandlePlayerForfeit(connectionId);
            return;
        }

        var player = new Player { ConnectionId = connectionId, Name = playerName };

        if (_gameManager.WaitingPlayers.Count > 0)
        {
            var opponent = _gameManager.WaitingPlayers[0];
            _gameManager.WaitingPlayers.RemoveAt(0); 
            
            opponent.Symbol = "X";
            player.Symbol = "O";

            var roomId = Guid.NewGuid().ToString();
            var game = new GameRoom
            {
                RoomId = roomId,
                Player1 = opponent,
                Player2 = player,
                CurrentTurnConnectionId = opponent.ConnectionId 
            };

            _gameManager.Games.Add(roomId, game);

            await Groups.AddToGroupAsync(player.ConnectionId, roomId);
            await Groups.AddToGroupAsync(opponent.ConnectionId, roomId);

            await Clients.Group(roomId).SendAsync("GameStarted", game);
        }
        else
        {
            _gameManager.WaitingPlayers.Add(player);
            await Clients.Caller.SendAsync("WaitingForOpponent");
        }
    }

    public async Task MakeMove(string roomId, int position)
    {
        if (!_gameManager.Games.TryGetValue(roomId, out var game)) return;
        if (game.IsGameOver || game.Board[position] != null || game.CurrentTurnConnectionId != Context.ConnectionId) return;

        var isPlayer1 = game.Player1.ConnectionId == Context.ConnectionId;
        game.Board[position] = isPlayer1 ? game.Player1.Symbol : game.Player2.Symbol;

        CheckWinCondition(game);

        if (game.IsGameOver)
        {
            // ¡ACTUALIZAMOS LA BD CUANDO ALGUIEN GANA O EMPATA CON UN MOVIMIENTO!
            await UpdateGameStatsAsync(game);
        }
        else
        {
            game.CurrentTurnConnectionId = isPlayer1 ? game.Player2.ConnectionId : game.Player1.ConnectionId;
        }

        await Clients.Group(roomId).SendAsync("UpdateBoard", game);
    }

    public async Task RestartGame(string roomId)
    {
        if (!_gameManager.Games.TryGetValue(roomId, out var game))
        {
            await Clients.Caller.SendAsync("OpponentLeft");
            return;
        }

        if (!game.IsGameOver) return;

        game.Board = new string[9]; 
        game.IsGameOver = false;
        game.WinnerConnectionId = string.Empty;
        game.CurrentTurnConnectionId = game.Player1.ConnectionId;

        await Clients.Group(roomId).SendAsync("GameRestarted", game);
    }

    private void CheckWinCondition(GameRoom game)
    {
        var b = game.Board;
        int[][] winningCombos = new int[][]
        {
            new int[] {0, 1, 2}, new int[] {3, 4, 5}, new int[] {6, 7, 8}, 
            new int[] {0, 3, 6}, new int[] {1, 4, 7}, new int[] {2, 5, 8}, 
            new int[] {0, 4, 8}, new int[] {2, 4, 6}                       
        };

        foreach (var combo in winningCombos)
        {
            if (b[combo[0]] != null && b[combo[0]] == b[combo[1]] && b[combo[1]] == b[combo[2]])
            {
                game.IsGameOver = true;
                game.WinnerConnectionId = game.CurrentTurnConnectionId;
                return;
            }
        }

        if (!b.Any(x => x == null))
        {
            game.IsGameOver = true;
            game.WinnerConnectionId = "Empate";
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        _gameManager.WaitingPlayers.RemoveAll(p => p.ConnectionId == connectionId);
        
        // Si se desconecta, se rinde, pierde, y se actualiza la BD
        await HandlePlayerForfeit(connectionId);
        await base.OnDisconnectedAsync(exception);
    }
}