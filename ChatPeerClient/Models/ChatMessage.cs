namespace ChatPeerClient.Models;

public sealed class ChatMessage
{
    public string From { get; set; } = string.Empty;
    public string? To { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Ts { get; set; } = string.Empty;
    public string? Peer { get; set; }
    public string? Channel { get; set; }
}
