using UnityEngine;

namespace Odyssey.Cyclops
{
    internal sealed class CyclopsFight
    {
        internal enum Phase { Windup, Warning, Recovery, Won, Lost }
        internal enum Attack { Slam, Swipe }
        internal enum Impact { None, Hit, Block, PerfectBlock }

        internal const int PlayerMax = 100;
        internal const int BossMax = 60;
        private const float WindupSeconds = 1.15f;
        private const float PerfectSeconds = 0.28f;
        private const int MaxCounters = 3;

        internal Phase Current { get; private set; }
        internal Attack CurrentAttack { get; private set; }
        internal Impact LastImpact { get; private set; }
        internal int PlayerHp { get; private set; } = PlayerMax;
        internal int BossHp { get; private set; } = BossMax;
        internal bool Finished => Current == Phase.Won || Current == Phase.Lost;
        internal bool PerfectWindow => Current == Phase.Warning && remaining <= PerfectSeconds;
        internal float Progress => 1 - Mathf.Clamp01(remaining / duration);
        internal int CountersLeft => MaxCounters - counters;

        private float duration = WindupSeconds;
        private float remaining = WindupSeconds;
        private float cooldown;
        private int counters;
        private bool shieldWasRaised;
        private bool timedRaise;

        internal void Tick(float seconds, bool shieldRaised)
        {
            if (Finished)
                return;

            cooldown = Mathf.Max(0, cooldown - seconds);
            if (!shieldRaised)
                timedRaise = false;
            else if (!shieldWasRaised && Current == Phase.Warning)
                timedRaise = remaining - seconds <= PerfectSeconds;
            shieldWasRaised = shieldRaised;

            remaining -= seconds;
            if (remaining > 0)
                return;

            switch (Current)
            {
                case Phase.Windup:
                    timedRaise = false;
                    SetPhase(Phase.Warning, CurrentAttack == Attack.Slam ? 1.5f : 1.05f);
                    break;
                case Phase.Warning:
                    LastImpact = !shieldRaised ? Impact.Hit : timedRaise ? Impact.PerfectBlock : Impact.Block;
                    if (LastImpact == Impact.Hit)
                        PlayerHp = Mathf.Max(0, PlayerHp - 25);

                    counters = 0;
                    SetPhase(PlayerHp == 0 ? Phase.Lost : Phase.Recovery,
                        LastImpact == Impact.PerfectBlock ? 2.1f : 1.35f);
                    break;
                case Phase.Recovery:
                    CurrentAttack = CurrentAttack == Attack.Slam ? Attack.Swipe : Attack.Slam;
                    SetPhase(Phase.Windup, WindupSeconds);
                    break;
            }
        }

        internal int TryHit()
        {
            if (Current != Phase.Recovery || cooldown > 0 || counters == MaxCounters)
                return 0;

            int damage = Mathf.Min(BossHp, counters == 0 && LastImpact == Impact.PerfectBlock ? 20 : 10);
            counters++;
            cooldown = 0.3f;
            BossHp -= damage;
            if (BossHp == 0)
                SetPhase(Phase.Won, 1f);

            return damage;
        }

        private void SetPhase(Phase phase, float seconds)
        {
            Current = phase;
            duration = remaining = seconds;
        }
    }
}
