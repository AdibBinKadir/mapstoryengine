using NUnit.Framework;

public class MedusaVideoRulesTests
{
    [TestCase(MedusaPhase.Danger, true)]
    [TestCase(MedusaPhase.Clue, true)]
    [TestCase(MedusaPhase.Hijack, false)]
    [TestCase(MedusaPhase.Coming, false)]
    [TestCase(MedusaPhase.Won, false)]
    [TestCase(MedusaPhase.Stoned, false)]
    [TestCase(MedusaPhase.Aborted, false)]
    public void PlaysOnlyWhileSheHunts(MedusaPhase phase, bool expected)
    {
        Assert.AreEqual(expected, MedusaVideoRules.Plays(phase));
    }

    // Play() may still be preparing (isPlaying false), so a requested play must still be paused on Stoned.
    [Test] public void StonedPausesARequestedPlay() { Assert.IsTrue(MedusaVideoRules.ShouldPause(MedusaPhase.Stoned, true)); }
    [Test] public void NothingToPauseIfNeverRequested() { Assert.IsFalse(MedusaVideoRules.ShouldPause(MedusaPhase.Stoned, false)); }
    [Test] public void NeverPausesWhileSheHunts() { Assert.IsFalse(MedusaVideoRules.ShouldPause(MedusaPhase.Clue, true)); }
}
