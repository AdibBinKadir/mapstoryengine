using Odyssey;
using UnityEngine;

// What the Sphinx mocks: deaths and the last thing that hurt the player, gathered from GameEvents.
public static class SphinxMemory
{
    public static int Deaths { get; private set; }
    public static string LastEvent { get; private set; } = "";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        Deaths = 0;
        LastEvent = "";
        GameEvents.PlayerDied -= OnDied;
        GameEvents.PlayerDied += OnDied;
        GameEvents.EncounterEnded -= OnEncounterEnded;
        GameEvents.EncounterEnded += OnEncounterEnded;
    }

    static void OnDied(string cause)
    {
        Deaths++;
        LastEvent = cause ?? "";
    }

    static void OnEncounterEnded(EncounterType type, bool won)
    {
        if (type == EncounterType.Sphinx) return;
        LastEvent = type.ToString().ToLowerInvariant() + (won ? "_won" : "_lost");
    }

    public static SphinxState State(string evt, SphinxProgress progress)
    {
        return new SphinxState
        {
            @event = evt,
            hp = VoyageState.Started ? VoyageState.Health : 100,
            max_hp = VoyageState.Started ? VoyageState.MaxHealth : 100,
            deaths = Deaths,
            last_event = LastEvent,
            riddle_index = progress.RiddleIndex,
            correct_count = progress.CorrectCount,
        };
    }
}
