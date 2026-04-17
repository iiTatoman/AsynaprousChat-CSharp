namespace ChatTrackerApi.Models;

public sealed class ConnectPeerRequest
{
    public string? From { get; set; }
    public string? To { get; set; }
}
