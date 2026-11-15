using UnityEngine;

namespace Odyssey
{
    // One voyage per application session. Scene and encounter lifetimes do not own the player's HP.
    internal static class VoyageState
    {
        internal static bool Started { get; private set; }
        internal static bool Won { get; private set; }
        internal static int MaxHealth { get; private set; } = 100;
        internal static int Health { get; private set; } = 100;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            End();
            GameEvents.DamageRequested -= Damage;
            GameEvents.DamageRequested += Damage;
            GameEvents.FullHealRequested -= Heal;
            GameEvents.FullHealRequested += Heal;
            GameEvents.PlayerRespawned -= Heal;
            GameEvents.PlayerRespawned += Heal;
            GameEvents.PlayerDied -= MarkDead;
            GameEvents.PlayerDied += MarkDead;
            GameEvents.EncounterStarted -= EncounterJournal.Begin;
            GameEvents.EncounterStarted += EncounterJournal.Begin;
            GameEvents.EncounterEnded -= EncounterJournal.Complete;
            GameEvents.EncounterEnded += EncounterJournal.Complete;
        }

        internal static void Begin()
        {
            if (Started)
                return;

            Started = true;
            MaxHealth = Mathf.Max(1, Services.Config != null ? Services.Config.maxHp : 100);
            Health = MaxHealth;
        }

        internal static void End()
        {
            Started = false;
            Won = false;
            Health = MaxHealth = 100;
            EncounterJournal.Reset();
        }

        internal static void Complete()
        {
            if (!Started || Won || Health == 0)
                return;

            Won = true;
            GameEvents.RaiseGameWon();
        }

        private static void Damage(int amount)
        {
            if (amount > 0)
                Health = Mathf.Max(0, Health - amount);
        }

        private static void Heal()
        {
            Health = MaxHealth;
        }

        private static void MarkDead(string cause)
        {
            Health = 0;
        }
    }
}
