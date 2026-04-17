namespace ChatPeerClient.Models;

public sealed class PeerInfo
{
    public string Ip { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Joined { get; set; } = string.Empty;
}
