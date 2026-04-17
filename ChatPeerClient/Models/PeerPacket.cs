namespace ChatPeerClient.Models;

public sealed class PeerPacket
{
    public string From { get; set; } = string.Empty;
    public string Channel { get; set; } = "general";
    public string Text { get; set; } = string.Empty;
    public string Ts { get; set; } = string.Empty;
}
