namespace ChatPeerClient.Models;

public sealed class TrackerSnapshot
{
    public Dictionary<string, PeerInfo> Peers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<string>> Channels { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
