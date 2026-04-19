using System;
using System.Collections.Generic;

/// <summary>
/// Static generic event bus. Publish/subscribe without direct references.
/// Events are structs (zero GC allocation). Subscribe in OnEnable, unsubscribe in OnDisable.
/// </summary>
public static class GameEventBus
{
    private static readonly Dictionary<Type, Delegate> handlers = new();

    public static void Subscribe<T>(Action<T> handler) where T : struct
    {
        var type = typeof(T);
        if (handlers.TryGetValue(type, out var existing))
            handlers[type] = Delegate.Combine(existing, handler);
        else
            handlers[type] = handler;
    }

    public static void Unsubscribe<T>(Action<T> handler) where T : struct
    {
        var type = typeof(T);
        if (handlers.TryGetValue(type, out var existing))
        {
            var updated = Delegate.Remove(existing, handler);
            if (updated == null)
                handlers.Remove(type);
            else
                handlers[type] = updated;
        }
    }

    public static void Publish<T>(T eventData) where T : struct
    {
        if (handlers.TryGetValue(typeof(T), out var existing))
            ((Action<T>)existing)?.Invoke(eventData);
    }

    /// <summary>
    /// Clear all subscriptions. Call on scene transitions to prevent stale references.
    /// </summary>
    public static void Clear()
    {
        handlers.Clear();
    }
}
