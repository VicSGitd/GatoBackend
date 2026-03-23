namespace GatoBackend.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int GamesWon { get; set; } = 0;
    public int GamesLost { get; set; } = 0;
    public int Draws { get; set; } = 0;
}