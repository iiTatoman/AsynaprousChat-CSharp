using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using ChatPeerClient.Models;

namespace ChatPeerClient.Services;

public sealed class PeerNode : IAsyncDisposable
{
    private readonly TrackerClient _trackerClient;
    private readonly ConcurrentDictionary<string, TcpClient> _connections = new(StringComparer.OrdinalIgnoreCase);
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private TcpListener? _listener;
    private CancellationTokenSource? _listenerCts;

    public string Username { get; }
    public string ListenHost { get; }
    public int ListenPort { get; }
    public string PeerId => $"{ListenHost}:{ListenPort}";
    public string CurrentChannel { get; private set; }
    public List<ChatMessage> Inbox { get; } = new();

    public PeerNode(string username, string listenHost, int listenPort, string trackerBaseUrl, string channel)
    {
        Username = username;
        ListenHost = listenHost;
        ListenPort = listenPort;
        CurrentChannel = channel;
        _trackerClient = new TrackerClient(trackerBaseUrl);
    }

    public async Task<bool> LoginAsync(string password, CancellationToken cancellationToken = default)
    {
        return await _trackerClient.LoginAsync(Username, password, cancellationToken);
    }

    public async Task RegisterAsync(CancellationToken cancellationToken = default)
    {
        await _trackerClient.RegisterPeerAsync(ListenHost, ListenPort, cancellationToken);
    }

    public async Task JoinChannelAsync(string channel, CancellationToken cancellationToken = default)
    {
        CurrentChannel = channel;
        await _trackerClient.JoinChannelAsync(channel, PeerId, cancellationToken);
    }

    public async Task<TrackerSnapshot> FetchPeersAsync(CancellationToken cancellationToken = default)
    {
        return await _trackerClient.FetchPeersAsync(cancellationToken);
    }

    public async Task StartServerAsync(CancellationToken cancellationToken = default)
    {
        _listenerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listener = new TcpListener(IPAddress.Parse(ListenHost), ListenPort);
        _listener.Start();

        Console.WriteLine($"[Peer] P2P server on {ListenHost}:{ListenPort}");

        _ = Task.Run(() => AcceptLoopAsync(_listenerCts.Token), _listenerCts.Token);
        await Task.CompletedTask;
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        if (_listener is null)
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                _ = Task.Run(() => HandleInboundClientAsync(client, cancellationToken), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Peer] Accept error: {ex.Message}");
            }
        }
    }

    private async Task HandleInboundClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        var remote = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        Console.WriteLine($"[Peer] Inbound P2P from {remote}");

        try
        {
            using var reader = new StreamReader(client.GetStream(), Encoding.UTF8, leaveOpen: false);
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(line))
                {
                    break;
                }

                var packet = JsonSerializer.Deserialize<PeerPacket>(line, _jsonOptions);
                if (packet is null)
                {
                    continue;
                }

                var message = new ChatMessage
                {
                    From = packet.From,
                    Channel = packet.Channel,
                    Text = packet.Text,
                    Ts = packet.Ts
                };

                lock (Inbox)
                {
                    Inbox.Add(message);
                }

                Console.WriteLine($"[Peer] <<< [{packet.Channel}] {packet.From}: {packet.Text}");
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Peer] Inbound error: {ex.Message}");
        }
        finally
        {
            client.Dispose();
        }
    }

    public async Task ConnectToPeerAsync(string peerId, string peerIp, int peerPort, CancellationToken cancellationToken = default)
    {
        if (string.Equals(peerId, PeerId, StringComparison.OrdinalIgnoreCase) || _connections.ContainsKey(peerId))
        {
            return;
        }

        try
        {
            var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Parse(peerIp), peerPort, cancellationToken);
            _connections[peerId] = client;
            await _trackerClient.NotifyConnectAsync(PeerId, peerId, cancellationToken);
            Console.WriteLine($"[Peer] Connected to {peerId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Peer] Cannot connect to {peerId}: {ex.Message}");
        }
    }

    public async Task BroadcastAsync(string text, CancellationToken cancellationToken = default)
    {
        var packet = new PeerPacket
        {
            From = Username,
            Channel = CurrentChannel,
            Text = text,
            Ts = DateTime.Now.ToString("HH:mm:ss")
        };

        var line = JsonSerializer.Serialize(packet) + "\n";
        var bytes = Encoding.UTF8.GetBytes(line);
        var deadConnections = new List<string>();

        foreach (var pair in _connections)
        {
            try
            {
                var stream = pair.Value.GetStream();
                await stream.WriteAsync(bytes, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }
            catch
            {
                deadConnections.Add(pair.Key);
            }
        }

        foreach (var dead in deadConnections)
        {
            if (_connections.TryRemove(dead, out var client))
            {
                client.Dispose();
            }
        }

        await _trackerClient.StoreBroadcastAsync(CurrentChannel, text, PeerId, cancellationToken);
    }

    public async Task SendDirectMessageAsync(string peerId, string text, CancellationToken cancellationToken = default)
    {
        if (_connections.TryGetValue(peerId, out var client))
        {
            try
            {
                var packet = new PeerPacket
                {
                    From = Username,
                    Channel = $"dm:{peerId}",
                    Text = text,
                    Ts = DateTime.Now.ToString("HH:mm:ss")
                };

                var line = JsonSerializer.Serialize(packet) + "\n";
                var bytes = Encoding.UTF8.GetBytes(line);
                var stream = client.GetStream();
                await stream.WriteAsync(bytes, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                return;
            }
            catch
            {
                if (_connections.TryRemove(peerId, out var staleClient))
                {
                    staleClient.Dispose();
                }
            }
        }

        await _trackerClient.StoreDirectMessageFallbackAsync(peerId, text, cancellationToken);
        Console.WriteLine($"[Peer] Stored fallback DM through tracker for {peerId}");
    }

    public async Task PrintHistoryAsync(string? channel = null, CancellationToken cancellationToken = default)
    {
        var targetChannel = string.IsNullOrWhiteSpace(channel) ? CurrentChannel : channel;
        var messages = await _trackerClient.GetMessagesAsync(targetChannel!, cancellationToken);

        Console.WriteLine($"[Peer] History for {targetChannel}:");
        foreach (var message in messages)
        {
            Console.WriteLine($"  [{message.Ts}] {message.From}: {message.Text}");
        }
    }

    public async Task BootstrapConnectionsAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await FetchPeersAsync(cancellationToken);
        foreach (var pair in snapshot.Peers)
        {
            if (string.Equals(pair.Key, PeerId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            await ConnectToPeerAsync(pair.Key, pair.Value.Ip, pair.Value.Port, cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _listenerCts?.Cancel();
        _listener?.Stop();

        foreach (var client in _connections.Values)
        {
            client.Dispose();
        }
        _connections.Clear();

        _trackerClient.Dispose();
        await Task.CompletedTask;
    }
}
