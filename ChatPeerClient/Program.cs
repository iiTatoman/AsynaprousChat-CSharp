using ChatPeerClient.Services;

if (args.Length < 5)
{
    Console.WriteLine("Usage: dotnet run -- <username> <password> <listenHost> <listenPort> <trackerBaseUrl> [channel]");
    return;
}

var username = args[0];
var password = args[1];
var listenHost = args[2];
if (!int.TryParse(args[3], out var listenPort))
{
    Console.WriteLine("Invalid listenPort.");
    return;
}

var trackerBaseUrl = args[4];
var channel = args.Length >= 6 ? args[5] : "general";

await using var peer = new PeerNode(username, listenHost, listenPort, trackerBaseUrl, channel);

if (!await peer.LoginAsync(password))
{
    Console.WriteLine("[Peer] Login failed.");
    return;
}

await peer.RegisterAsync();
await peer.JoinChannelAsync(channel);
await peer.StartServerAsync();
await peer.BootstrapConnectionsAsync();

Console.WriteLine($"[Peer] Logged in as {username}");
Console.WriteLine("Type /help for commands.");

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }

    if (string.Equals(input, "/quit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    if (string.Equals(input, "/help", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("/peers");
        Console.WriteLine("/connect <peerId>");
        Console.WriteLine("/join <channel>");
        Console.WriteLine("/broadcast <message>");
        Console.WriteLine("/dm <peerId> <message>");
        Console.WriteLine("/history [channel]");
        Console.WriteLine("/quit");
        continue;
    }

    if (string.Equals(input, "/peers", StringComparison.OrdinalIgnoreCase))
    {
        var snapshot = await peer.FetchPeersAsync();
        Console.WriteLine("[Peer] Active peers:");
        foreach (var item in snapshot.Peers)
        {
            Console.WriteLine($"  {item.Key} ({item.Value.Username})");
        }
        continue;
    }

    if (input.StartsWith("/connect ", StringComparison.OrdinalIgnoreCase))
    {
        var peerId = input[9..].Trim();
        var snapshot = await peer.FetchPeersAsync();
        if (snapshot.Peers.TryGetValue(peerId, out var targetPeer))
        {
            await peer.ConnectToPeerAsync(peerId, targetPeer.Ip, targetPeer.Port);
        }
        else
        {
            Console.WriteLine("[Peer] Peer not found.");
        }
        continue;
    }

    if (input.StartsWith("/join ", StringComparison.OrdinalIgnoreCase))
    {
        var nextChannel = input[6..].Trim();
        if (!string.IsNullOrWhiteSpace(nextChannel))
        {
            await peer.JoinChannelAsync(nextChannel);
            Console.WriteLine($"[Peer] Joined channel {nextChannel}");
        }
        continue;
    }

    if (input.StartsWith("/broadcast ", StringComparison.OrdinalIgnoreCase))
    {
        var message = input[11..].Trim();
        await peer.BroadcastAsync(message);
        continue;
    }

    if (input.StartsWith("/dm ", StringComparison.OrdinalIgnoreCase))
    {
        var parts = input.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
        {
            Console.WriteLine("Usage: /dm <peerId> <message>");
            continue;
        }

        await peer.SendDirectMessageAsync(parts[1], parts[2]);
        continue;
    }

    if (input.StartsWith("/history", StringComparison.OrdinalIgnoreCase))
    {
        var pieces = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var historyChannel = pieces.Length > 1 ? pieces[1] : null;
        await peer.PrintHistoryAsync(historyChannel);
        continue;
    }

    await peer.BroadcastAsync(input);
}
