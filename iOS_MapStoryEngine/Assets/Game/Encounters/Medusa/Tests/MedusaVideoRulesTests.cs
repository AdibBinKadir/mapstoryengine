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
}
