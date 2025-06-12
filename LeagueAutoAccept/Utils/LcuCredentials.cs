namespace LeagueAutoAccept.Utils;

public class LcuCredentials
{
    public required string Port { get; set; }
    public required string Password { get; set; }
    public string Protocol { get; set; } = "http";
} 