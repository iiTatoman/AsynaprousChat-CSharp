namespace ChatTrackerApi.Models;

public sealed class ChatMessage
{
    public required string From { get; init; }
    public string? To { get; init; }
    public required string Text { get; init; }
    public required string Ts { get; init; }
    public string? Peer { get; init; }
    public string? Channel { get; init; }
}
