using System;
using System.Collections.Generic;

/// <summary>
/// A simple, decoupled global event bus.
/// Scripts can raise and listen to events without knowing about each other.
/// Usage:
///   EventBus.Subscribe&lt;GameStartedEvent&gt;(OnGameStarted);
///   EventBus.Publish(new GameStartedEvent());
///   EventBus.Unsubscribe&lt;GameStartedEvent&gt;(OnGameStarted);
/// </summary>
public static class EventBus
{
    private static readonly Dictionary<Type, List<Delegate>> _handlers = new();

    public static void Subscribe<T>(Action<T> handler)
    {
        var type = typeof(T);
        if (!_handlers.ContainsKey(type))
            _handlers[type] = new List<Delegate>();
        _handlers[type].Add(handler);
    }

    public static void Unsubscribe<T>(Action<T> handler)
    {
        var type = typeof(T);
        if (_handlers.TryGetValue(type, out var list))
            list.Remove(handler);
    }

    public static void Publish<T>(T evt)
    {
        var type = typeof(T);
        if (!_handlers.TryGetValue(type, out var list)) return;

        // Copy to avoid modification during iteration
        var copy = new List<Delegate>(list);
        foreach (var handler in copy)
            (handler as Action<T>)?.Invoke(evt);
    }

    public static void Clear()
    {
        _handlers.Clear();
    }
}

// ──────────────────────────────────────────────────────────────
// Game-wide event definitions — add new events here.
// ──────────────────────────────────────────────────────────────

public struct GameStateChangedEvent
{
    public GameState Previous;
    public GameState Current;
}

public struct PlayerDiedEvent
{
    public ulong ClientId;
}

public struct PlayerSpawnedEvent
{
    public ulong ClientId;
}

public struct SceneLoadStartedEvent
{
    public string SceneName;
}

public struct SceneLoadCompletedEvent
{
    public string SceneName;
}

// ── Lobby events ──────────────────────────────────────────────

public struct LobbyPlayerJoinedEvent
{
    public ulong ClientId;
}

public struct LobbyPlayerLeftEvent
{
    public ulong ClientId;
}

public struct GameStartedEvent { }
