using System.Text.Json;

namespace ChatPeerClient.Models;

public sealed class TrackerEnvelope
{
    public string Status { get; set; } = string.Empty;
    public JsonElement Data { get; set; }
    public string? Message { get; set; }
}
