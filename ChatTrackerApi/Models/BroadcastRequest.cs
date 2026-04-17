namespace ChatTrackerApi.Models;

public sealed class BroadcastRequest
{
    public string? Channel { get; set; }
    public string? Text { get; set; }
    public string? PeerId { get; set; }
}
