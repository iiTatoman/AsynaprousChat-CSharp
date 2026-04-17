namespace ChatTrackerApi.Models;

public sealed class PeerRegistrationRequest
{
    public string? Ip { get; set; }
    public int? Port { get; set; }
}
