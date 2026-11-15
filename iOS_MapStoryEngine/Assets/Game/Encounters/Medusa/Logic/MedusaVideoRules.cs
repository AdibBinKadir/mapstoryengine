// When Medusa's video loop plays: only while she hunts. Stoned pauses it (frozen frame, no sound).
public static class MedusaVideoRules
{
    public static bool Plays(MedusaPhase phase)
    {
        return phase == MedusaPhase.Danger || phase == MedusaPhase.Clue;
    }

    // Uses our own "play requested" flag: while the clip is still preparing, isPlaying is false but Play() is pending.
    public static bool ShouldPause(MedusaPhase phase, bool playRequested)
    {
        return playRequested && !Plays(phase);
    }
}
