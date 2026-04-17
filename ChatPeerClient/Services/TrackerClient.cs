using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ChatPeerClient.Models;

namespace ChatPeerClient.Services;

public sealed class TrackerClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly CookieContainer _cookieContainer = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public string? SessionToken { get; private set; }

    public TrackerClient(string baseUrl)
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = _cookieContainer,
            UseCookies = true
        };

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/")
        };
    }

    public async Task<bool> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("login", new { username, password }, cancellationToken);
        var payload = await ReadEnvelopeAsync(response, cancellationToken);

        if (!response.IsSuccessStatusCode || !string.Equals(payload?.Status, "ok", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (payload is null)
        {
            return false;
        }

        if (payload.Data.TryGetProperty("token", out var tokenElement))
        {
            SessionToken = tokenElement.GetString();
        }

        return !string.IsNullOrWhiteSpace(SessionToken);
    }

    public async Task RegisterPeerAsync(string ip, int port, CancellationToken cancellationToken = default)
    {
        await _httpClient.PostAsJsonAsync("submit-info", new { ip, port }, cancellationToken);
    }

    public async Task<TrackerSnapshot> FetchPeersAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("get-list", cancellationToken);
        var payload = await ReadEnvelopeAsync(response, cancellationToken);
        if (payload is null || !payload.Data.ValueKind.Equals(JsonValueKind.Object))
        {
            return new TrackerSnapshot();
        }

        return JsonSerializer.Deserialize<TrackerSnapshot>(payload.Data.GetRawText(), _jsonOptions) ?? new TrackerSnapshot();
    }

    public async Task JoinChannelAsync(string channel, string peerId, CancellationToken cancellationToken = default)
    {
        await _httpClient.PostAsJsonAsync("add-list", new { channel, peer_id = peerId }, cancellationToken);
    }

    public async Task NotifyConnectAsync(string fromPeerId, string toPeerId, CancellationToken cancellationToken = default)
    {
        await _httpClient.PostAsJsonAsync("connect-peer", new { from = fromPeerId, to = toPeerId }, cancellationToken);
    }

    public async Task StoreBroadcastAsync(string channel, string text, string peerId, CancellationToken cancellationToken = default)
    {
        await _httpClient.PostAsJsonAsync("broadcast-peer", new { channel, text, peer_id = peerId }, cancellationToken);
    }

    public async Task StoreDirectMessageFallbackAsync(string toPeerId, string text, CancellationToken cancellationToken = default)
    {
        await _httpClient.PostAsJsonAsync("send-peer", new { to = toPeerId, text }, cancellationToken);
    }

    public async Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string channel, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"messages?channel={Uri.EscapeDataString(channel)}", cancellationToken);
        var payload = await ReadEnvelopeAsync(response, cancellationToken);
        if (payload is null || !payload.Data.TryGetProperty("messages", out var messageElement))
        {
            return Array.Empty<ChatMessage>();
        }

        return JsonSerializer.Deserialize<List<ChatMessage>>(messageElement.GetRawText(), _jsonOptions) ?? new List<ChatMessage>();
    }

    private async Task<TrackerEnvelope?> ReadEnvelopeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<TrackerEnvelope>(json, _jsonOptions);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
