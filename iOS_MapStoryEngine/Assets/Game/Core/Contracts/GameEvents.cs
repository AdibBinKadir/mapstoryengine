using System;

// Sphinx and Medusa reward Good clues. Cyclops rewards a Specific clue. Hydra gives none.
public static class GameEvents
{
    public static event Action<EncounterType> EncounterStarted;
    public static event Action<EncounterType, bool> EncounterEnded;  // bool = playerWon
    public static event Action<int> DamageRequested;                 // encounters ask; GameState applies
    public static event Action FullHealRequested;
    public static event Action<string> PlayerDied;                   // "stoned", "poison", or "cyclops"
    public static event Action PlayerRespawned;
    public static event Action<ClueTier> ClueRequested;              // ClueBook unlocks the next clue
    public static event Action<string> ClueUnlocked;                  // clue text
    public static event Action GameWon;

    public static void RequestDamage(int amount)
    {
        DamageRequested?.Invoke(amount);
    }

    public static void RequestFullHeal()
    {
        FullHealRequested?.Invoke();
    }

    public static void RequestClue(ClueTier tier)
    {
        ClueRequested?.Invoke(tier);
    }

    public static void RaiseEncounterStarted(EncounterType t)
    {
        EncounterStarted?.Invoke(t);
    }

    public static void RaiseEncounterEnded(EncounterType t, bool won)
    {
        EncounterEnded?.Invoke(t, won);
    }

    public static void RaisePlayerDied(string cause)
    {
        PlayerDied?.Invoke(cause);
    }

    public static void RaisePlayerRespawned()
    {
        PlayerRespawned?.Invoke();
    }

    public static void RaiseClueUnlocked(string text)
    {
        ClueUnlocked?.Invoke(text);
    }

    public static void RaiseGameWon()
    {
        GameWon?.Invoke();
    }
}
