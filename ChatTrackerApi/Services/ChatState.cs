using System.Collections.Concurrent;
using ChatTrackerApi.Models;

namespace ChatTrackerApi.Services;

public sealed class ChatState
{
    public ConcurrentDictionary<string, string> UserDb { get; } = new(
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["admin"] = "admin123",
            ["user1"] = "password",
            ["alice"] = "alice123"
        });

    public ConcurrentDictionary<string, string> SessionStore { get; } = new();
    public ConcurrentDictionary<string, PeerInfo> PeerRegistry { get; } = new();
    public ConcurrentDictionary<string, HashSet<string>> Channels { get; } = new();
    public ConcurrentDictionary<string, List<ChatMessage>> Messages { get; } = new();

    private readonly object _channelLock = new();
    private readonly object _messageLock = new();

    public ChatState()
    {
        EnsureChannel("general");
    }

    public void EnsureChannel(string channel)
    {
        Channels.TryAdd(channel, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        Messages.TryAdd(channel, new List<ChatMessage>());
    }

    public IReadOnlyDictionary<string, List<string>> GetChannelSnapshot()
    {
        lock (_channelLock)
        {
            return Channels.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToList());
        }
    }

    public void AddPeerToChannel(string channel, string peerId)
    {
        EnsureChannel(channel);
        lock (_channelLock)
        {
            Channels[channel].Add(peerId);
        }
    }

    public IReadOnlyList<string> GetChannelMembers(string channel)
    {
        EnsureChannel(channel);
        lock (_channelLock)
        {
            return Channels[channel].ToList();
        }
    }

    public void AddMessage(string channel, ChatMessage message)
    {
        EnsureChannel(channel);
        lock (_messageLock)
        {
            Messages[channel].Add(message);
        }
    }

    public IReadOnlyList<ChatMessage> GetMessages(string channel)
    {
        EnsureChannel(channel);
        lock (_messageLock)
        {
            return Messages[channel].ToList();
        }
    }
}
