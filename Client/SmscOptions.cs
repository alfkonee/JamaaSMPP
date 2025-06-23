namespace Elyfe.Smpp.Client;

public class SmscOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 2775;
    public string SystemId { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool Reconnect { get; set; } = true;
    public int ReconnectInterval { get; set; } = 5000; // 5 seconds
}
