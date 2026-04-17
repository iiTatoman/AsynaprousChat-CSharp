using System.Security.Cryptography;
using System.Text;
using ChatTrackerApi.Models;
using ChatTrackerApi.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ChatState>();

var app = builder.Build();
var state = app.Services.GetRequiredService<ChatState>();

app.UseDefaultFiles();
app.UseStaticFiles();

static string Ts() => DateTime.Now.ToString("HH:mm:ss");

static string DmChannel(string userA, string userB)
{
    var users = new[] { userA.Trim(), userB.Trim() }
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    return $"__dm__:{string.Join(":", users)}";
}

static string? ResolveUser(HttpRequest request, ChatState state)
{
    if (request.Headers.TryGetValue("Authorization", out var authHeader))
    {
        var value = authHeader.ToString();
        if (value.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var encoded = value[6..].Trim();
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                var parts = decoded.Split(':', 2);
                if (parts.Length == 2 && state.UserDb.TryGetValue(parts[0], out var password) && password == parts[1])
                {
                    return parts[0];
                }
            }
            catch
            {
                return null;
            }
        }
    }

    if (request.Cookies.TryGetValue("session_id", out var token) && !string.IsNullOrWhiteSpace(token))
    {
        if (state.SessionStore.TryGetValue(token, out var sessionUser))
        {
            return sessionUser;
        }
    }

    return null;
}

IResult Ok(object? data = null)
{
    var payload = new Dictionary<string, object?>
    {
        ["status"] = "ok"
    };

    if (data is not null)
    {
        payload["data"] = data;
    }

    return Results.Json(payload);
}

IResult Error(string message, int statusCode = StatusCodes.Status400BadRequest)
{
    return Results.Json(new { status = "error", message }, statusCode: statusCode);
}

app.MapPost("/login", (LoginRequest request, HttpContext context) =>
{
    var username = request.Username ?? request.User;
    var password = request.Password ?? request.Pass;

    if (string.IsNullOrWhiteSpace(username) ||
        string.IsNullOrWhiteSpace(password) ||
        !state.UserDb.TryGetValue(username, out var storedPassword) ||
        storedPassword != password)
    {
        return Error("Invalid username or password", StatusCodes.Status401Unauthorized);
    }

    var tokenBytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{username}:{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}:chat"));
    var token = Convert.ToHexString(tokenBytes).ToLowerInvariant();
    state.SessionStore[token] = username;

    context.Response.Cookies.Append("session_id", token, new CookieOptions
    {
        HttpOnly = true,
        SameSite = SameSiteMode.Lax
    });

    Console.WriteLine($"[Tracker] Login: user={username} token={token[..8]}...");
    return Ok(new { token, username });
});

app.MapGet("/whoami", (HttpRequest request) =>
{
    var username = ResolveUser(request, state);
    if (username is null)
    {
        return Error("Unauthorized", StatusCodes.Status401Unauthorized);
    }

    return Ok(new { username });
});

app.MapGet("/users", (HttpRequest request) =>
{
    var username = ResolveUser(request, state);
    if (username is null)
    {
        return Error("Unauthorized", StatusCodes.Status401Unauthorized);
    }

    var users = state.SessionStore.Values
        .Concat(state.PeerRegistry.Values.Select(p => p.Username))
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
        .ToList();

    return Ok(new { users });
});

app.MapPost("/submit-info", (PeerRegistrationRequest request, HttpRequest httpRequest) =>
{
    var username = ResolveUser(httpRequest, state);
    if (username is null)
    {
        return Error("Unauthorized", StatusCodes.Status401Unauthorized);
    }

    if (string.IsNullOrWhiteSpace(request.Ip) || request.Port is null)
    {
        return Error("Missing ip or port");
    }

    var peerId = $"{request.Ip}:{request.Port.Value}";
    state.PeerRegistry[peerId] = new PeerInfo
    {
        Ip = request.Ip,
        Port = request.Port.Value,
        Username = username,
        Joined = Ts()
    };

    Console.WriteLine($"[Tracker] Registered peer {peerId} ({username})");
    return Ok(new { peer_id = peerId });
});

app.MapGet("/get-list", (HttpRequest request) =>
{
    var username = ResolveUser(request, state);
    if (username is null)
    {
        return Error("Unauthorized", StatusCodes.Status401Unauthorized);
    }

    return Ok(new
    {
        peers = state.PeerRegistry,
        channels = state.GetChannelSnapshot()
    });
});

app.MapPost("/add-list", (AddListRequest request, HttpRequest httpRequest) =>
{
    var username = ResolveUser(httpRequest, state);
    if (username is null)
    {
        return Error("Unauthorized", StatusCodes.Status401Unauthorized);
    }

    var channel = string.IsNullOrWhiteSpace(request.Channel) ? "general" : request.Channel;
    var peerId = string.IsNullOrWhiteSpace(request.PeerId) ? $"web:{username}" : request.PeerId;

    state.AddPeerToChannel(channel, peerId);
    Console.WriteLine($"[Tracker] {peerId} joined channel={channel}");
    return Ok(new { channel, members = state.GetChannelMembers(channel) });
});

app.MapPost("/connect-peer", (ConnectPeerRequest request, HttpRequest httpRequest) =>
{
    var username = ResolveUser(httpRequest, state);
    if (username is null)
    {
        return Error("Unauthorized", StatusCodes.Status401Unauthorized);
    }

    if (string.IsNullOrWhiteSpace(request.From) || string.IsNullOrWhiteSpace(request.To))
    {
        return Error("Missing from or to");
    }

    Console.WriteLine($"[Tracker] P2P: {request.From} <-> {request.To}");
    return Ok(new { connected = new[] { request.From, request.To } });
});

app.MapPost("/broadcast-peer", (BroadcastRequest request, HttpRequest httpRequest) =>
{
    var username = ResolveUser(httpRequest, state);
    if (username is null)
    {
        return Error("Unauthorized", StatusCodes.Status401Unauthorized);
    }

    var channel = string.IsNullOrWhiteSpace(request.Channel) ? "general" : request.Channel;
    var message = new ChatMessage
    {
        From = username,
        Peer = request.PeerId,
        Text = request.Text ?? string.Empty,
        Ts = Ts(),
        Channel = channel
    };

    state.AddMessage(channel, message);
    Console.WriteLine($"[Tracker] Broadcast [{channel}] {username}: {request.Text}");
    return Ok(new { message });
});

app.MapPost("/send-peer", (DirectMessageRequest request, HttpRequest httpRequest) =>
{
    var username = ResolveUser(httpRequest, state);
    if (username is null)
    {
        return Error("Unauthorized", StatusCodes.Status401Unauthorized);
    }

    if (string.IsNullOrWhiteSpace(request.To))
    {
        return Error("Missing destination peer");
    }

    var channel = $"__dm__{username}__{request.To}";
    var message = new ChatMessage
    {
        From = username,
        To = request.To,
        Text = request.Text ?? string.Empty,
        Ts = Ts(),
        Channel = channel
    };

    state.AddMessage(channel, message);
    Console.WriteLine($"[Tracker] DM {username} -> {request.To}: {request.Text}");
    return Ok(new { message });
});

app.MapPost("/web/send-dm", (DirectMessageRequest request, HttpRequest httpRequest) =>
{
    var username = ResolveUser(httpRequest, state);
    if (username is null)
    {
        return Error("Unauthorized", StatusCodes.Status401Unauthorized);
    }

    if (string.IsNullOrWhiteSpace(request.To))
    {
        return Error("Missing destination username");
    }

    var channel = DmChannel(username, request.To);
    var message = new ChatMessage
    {
        From = username,
        To = request.To,
        Text = request.Text ?? string.Empty,
        Ts = Ts(),
        Channel = channel
    };

    state.AddMessage(channel, message);
    Console.WriteLine($"[Tracker] Web DM {username} -> {request.To}: {request.Text}");
    return Ok(new { message, channel });
});

app.MapGet("/web/dm-messages", (string user, HttpRequest request) =>
{
    var username = ResolveUser(request, state);
    if (username is null)
    {
        return Error("Unauthorized", StatusCodes.Status401Unauthorized);
    }

    if (string.IsNullOrWhiteSpace(user))
    {
        return Error("Missing user");
    }

    var channel = DmChannel(username, user);
    return Ok(new
    {
        channel,
        messages = state.GetMessages(channel)
    });
});

app.MapGet("/messages", (string? channel, HttpRequest request) =>
{
    var username = ResolveUser(request, state);
    if (username is null)
    {
        return Error("Unauthorized", StatusCodes.Status401Unauthorized);
    }

    var finalChannel = string.IsNullOrWhiteSpace(channel) ? "general" : channel;
    return Ok(new
    {
        channel = finalChannel,
        messages = state.GetMessages(finalChannel)
    });
});

app.MapGet("/health", () => Results.Text("Asynaprous Chat Tracker API is running."));

app.Run("http://0.0.0.0:5000");
