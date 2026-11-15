using System;

public enum MedusaPhase { Hijack, Coming, Danger, Clue, Won, Stoned, Aborted }

// Pure Medusa rules: phases, timers and the "looked too long" check. No UnityEngine.
public sealed class MedusaTimeline
{
    public const float HijackSeconds = 1f, ComingSeconds = 3f, ClueSeconds = 2f;
    public const float StoneAfterLookingSeconds = 0.5f, StonedScreenSeconds = 2f;
    public const float ClueEarliestSeconds = 5f, ClueLatestSeconds = 15f;

    const float Epsilon = 1e-5f;

    readonly float clueDelaySeconds;
    float phaseTime;
    float lookingTime;

    public MedusaPhase Phase { get; private set; }
    public bool IsOver { get; private set; }
    public bool ClueReached { get; private set; }   // true once Clue was entered, even if a hitch passed straight through it

    public MedusaTimeline(float clueDelaySeconds)
    {
        this.clueDelaySeconds = Math.Max(ClueEarliestSeconds, Math.Min(ClueLatestSeconds, clueDelaySeconds));
        Phase = MedusaPhase.Hijack;
    }

    // Consumes deltaSeconds across as many phases as it spans, so a frame hitch can't skip the rules.
    public MedusaPhase Tick(float deltaSeconds, bool lookingAtScreen)
    {
        float remaining = Math.Max(0f, deltaSeconds);
        while (!IsOver)
        {
            bool dangerous = Phase == MedusaPhase.Danger || Phase == MedusaPhase.Clue;
            float step = Math.Min(remaining, Math.Max(0f, Duration(Phase) - phaseTime));
            if (dangerous && lookingAtScreen)
                step = Math.Min(step, Math.Max(0f, StoneAfterLookingSeconds - lookingTime));

            phaseTime += step;
            remaining -= step;

            if (dangerous)
            {
                if (lookingAtScreen) lookingTime += step;
                else lookingTime = 0f;

                if (lookingAtScreen && lookingTime >= StoneAfterLookingSeconds - Epsilon)
                {
                    Enter(MedusaPhase.Stoned);
                    continue;
                }
            }

            if (phaseTime >= Duration(Phase) - Epsilon)
            {
                Advance();
                continue;
            }

            if (remaining <= 0f) break;
        }
        return Phase;
    }

    public void Abort()
    {
        Phase = MedusaPhase.Aborted;
        IsOver = true;
    }

    float Duration(MedusaPhase phase)
    {
        switch (phase)
        {
            case MedusaPhase.Hijack: return HijackSeconds;
            case MedusaPhase.Coming: return ComingSeconds;
            case MedusaPhase.Danger: return clueDelaySeconds;
            case MedusaPhase.Clue: return ClueSeconds;
            case MedusaPhase.Stoned: return StonedScreenSeconds;
            default: return 0f;
        }
    }

    void Advance()
    {
        switch (Phase)
        {
            case MedusaPhase.Hijack: Enter(MedusaPhase.Coming); break;
            case MedusaPhase.Coming: Enter(MedusaPhase.Danger); break;
            case MedusaPhase.Danger: Enter(MedusaPhase.Clue); break;   // lookingTime carries over
            case MedusaPhase.Clue: Enter(MedusaPhase.Won); IsOver = true; break;
            case MedusaPhase.Stoned: IsOver = true; break;
        }
    }

    void Enter(MedusaPhase phase)
    {
        Phase = phase;
        phaseTime = 0f;
        if (phase == MedusaPhase.Clue) ClueReached = true;
    }
}
