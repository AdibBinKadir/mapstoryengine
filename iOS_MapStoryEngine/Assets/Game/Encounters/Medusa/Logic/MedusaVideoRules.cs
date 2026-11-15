// When Medusa's video loop plays: only while she hunts. Stoned pauses it (frozen frame, no sound).
public static class MedusaVideoRules
{
    public static bool Plays(MedusaPhase phase)
    {
        return phase == MedusaPhase.Danger || phase == MedusaPhase.Clue;
    }
}
