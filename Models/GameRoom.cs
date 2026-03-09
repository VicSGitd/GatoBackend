namespace GatoBackend.Models;

public class GameRoom
{
    public string RoomId { get; set; } = string.Empty;
    public Player Player1 { get; set; } = null!;
    public Player Player2 { get; set; } = null!;
    //Arreglo de 9 posiciones para representar el tablero del gato, cada posición puede ser "X", "O" o "" (vacía)
    public string[] Board { get; set; } = new string[9]; 
    public string CurrentTurnConnectionId { get; set; } = string.Empty;
    public bool IsGameOver { get; set; }
    public string WinnerConnectionId { get; set; } = string.Empty;
}