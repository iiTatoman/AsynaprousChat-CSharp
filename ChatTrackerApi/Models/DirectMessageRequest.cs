namespace ChatTrackerApi.Models;

public sealed class DirectMessageRequest
{
    public string? To { get; set; }
    public string? Text { get; set; }
}
