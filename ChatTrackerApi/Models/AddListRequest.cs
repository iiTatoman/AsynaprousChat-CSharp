namespace ChatTrackerApi.Models;

public sealed class AddListRequest
{
    public string? Channel { get; set; }
    public string? PeerId { get; set; }
}
