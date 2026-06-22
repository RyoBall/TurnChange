using System;
using UnityEngine;

public enum GameAudioEventType
{
    MouseClick,
    ButtonHoverEnter,
    ButtonHoverExit,
    LevelScroll,
    CombatDamage,
    CombatBuffGain,
    CombatDebuffGain,
    CharacterSwitch,
    CombatTurnSkipped
}

public readonly struct GameAudioEvent
{
    public GameAudioEvent(
        GameAudioEventType eventType,
        UnityEngine.Object source = null,
        UnityEngine.Object target = null,
        int amount = 0)
    {
        EventType = eventType;
        Source = source;
        Target = target;
        Amount = amount;
    }

    public GameAudioEventType EventType { get; }
    public UnityEngine.Object Source { get; }
    public UnityEngine.Object Target { get; }
    public int Amount { get; }
}

public static class GameAudioEvents
{
    public static event Action<GameAudioEvent> Raised;

    public static void Raise(
        GameAudioEventType eventType,
        UnityEngine.Object source = null,
        UnityEngine.Object target = null,
        int amount = 0)
    {
        Raised?.Invoke(new GameAudioEvent(eventType, source, target, amount));
    }
}