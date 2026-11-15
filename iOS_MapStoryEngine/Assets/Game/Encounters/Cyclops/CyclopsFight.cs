using UnityEngine;

namespace Odyssey.Cyclops
{
    internal sealed class CyclopsFight
    {
        internal enum Phase { Windup, Warning, Recovery, Won, Lost }

        internal const int PlayerMax = 100;
        internal const int BossMax = 60;

        internal Phase Current { get; private set; }
        internal int PlayerHp { get; private set; } = PlayerMax;
        internal int BossHp { get; private set; } = BossMax;
        internal bool LastHitBlocked { get; private set; }
        internal bool Finished => Current == Phase.Won || Current == Phase.Lost;

        private float remaining = 2f;
        private float cooldown;

        internal void Tick(float seconds, bool shieldRaised)
        {
            if (Finished)
                return;

            cooldown = Mathf.Max(0, cooldown - seconds);
            remaining -= seconds;
            if (remaining > 0)
                return;

            switch (Current)
            {
                case Phase.Windup:
                    Current = Phase.Warning;
                    remaining = 1.5f;
                    break;
                case Phase.Warning:
                    LastHitBlocked = shieldRaised;
                    if (!shieldRaised)
                        PlayerHp = Mathf.Max(0, PlayerHp - 25);
                    Current = PlayerHp == 0 ? Phase.Lost : Phase.Recovery;
                    remaining = 1.25f;
                    break;
                case Phase.Recovery:
                    Current = Phase.Windup;
                    remaining = 2f;
                    break;
            }
        }

        internal void TryHit()
        {
            if (Current != Phase.Recovery || cooldown > 0)
                return;

            cooldown = 0.5f;
            BossHp = Mathf.Max(0, BossHp - 10);
            if (BossHp == 0)
                Current = Phase.Won;
        }
    }
}
