using NUnit.Framework;

public class SphinxProgressTests
{
    static SphinxProgress AfterIntro(int riddle)
    {
        var p = new SphinxProgress();
        p.Apply(riddle, "none", false);
        return p;
    }

    [Test] public void StartsAtNoRiddle() { var p = new SphinxProgress(); Assert.AreEqual(-1, p.RiddleIndex); Assert.AreEqual(0, p.CorrectCount); Assert.IsFalse(p.Won); }
    [Test] public void IntroSetsRiddle() { Assert.AreEqual(3, AfterIntro(3).RiddleIndex); }
    [Test] public void FirstCorrectCountsOneAndTakesTheNewRiddle() { var p = AfterIntro(3); p.Apply(5, "correct", false); Assert.AreEqual(1, p.CorrectCount); Assert.AreEqual(5, p.RiddleIndex); Assert.IsFalse(p.Won); }
    [Test] public void RevealOnSecondCorrectWins() { var p = AfterIntro(3); p.Apply(5, "correct", false); p.Apply(5, "correct", true); Assert.IsTrue(p.Won); Assert.AreEqual(2, p.CorrectCount); }
    [Test] public void RevealIgnoredBeforeFirstCorrect() { var p = AfterIntro(3); p.Apply(3, "correct", true); Assert.IsFalse(p.Won); Assert.AreEqual(1, p.CorrectCount); }
    [Test] public void RevealNeedsACorrectVerdict() { var p = AfterIntro(3); p.Apply(5, "correct", false); p.Apply(5, "wrong", true); Assert.IsFalse(p.Won); }
    [Test] public void WrongChangesNothing() { var p = AfterIntro(3); p.Apply(3, "wrong", false); Assert.AreEqual(0, p.CorrectCount); Assert.AreEqual(3, p.RiddleIndex); }
    [Test] public void NegativeIndexKeepsCurrent() { var p = AfterIntro(3); p.Apply(-1, "none", false); Assert.AreEqual(3, p.RiddleIndex); }
    [Test] public void NothingAfterWon() { var p = AfterIntro(3); p.Apply(5, "correct", false); p.Apply(5, "correct", true); p.Apply(7, "correct", false); Assert.AreEqual(2, p.CorrectCount); Assert.AreEqual(5, p.RiddleIndex); }
    [Test] public void UnknownVerdictIsNone() { var p = AfterIntro(3); p.Apply(3, null, false); Assert.AreEqual(0, p.CorrectCount); }
}
