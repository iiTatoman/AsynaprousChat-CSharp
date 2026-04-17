namespace ChatTrackerApi.Models;

public sealed class PeerInfo
{
    public required string Ip { get; init; }
    public required int Port { get; init; }
    public required string Username { get; init; }
    public required string Joined { get; init; }
}
