using NUnit.Framework;

public class MedusaTimelineTests
{
    static MedusaTimeline AtDanger(float clueDelay)
    {
        var t = new MedusaTimeline(clueDelay);
        t.Tick(4f, false);
        return t;
    }

    [Test] public void StartsInHijack() { Assert.AreEqual(MedusaPhase.Hijack, new MedusaTimeline(10f).Phase); }
    [Test] public void HijackLastsOneSecond() { var t = new MedusaTimeline(10f); Assert.AreEqual(MedusaPhase.Hijack, t.Tick(0.99f, false)); Assert.AreEqual(MedusaPhase.Coming, t.Tick(0.02f, false)); }
    [Test] public void ComingLastsThreeSeconds() { var t = new MedusaTimeline(10f); t.Tick(1f, false); Assert.AreEqual(MedusaPhase.Coming, t.Tick(2.99f, false)); Assert.AreEqual(MedusaPhase.Danger, t.Tick(0.02f, false)); }
    [Test] public void LookingIsSafeBeforeDanger() { var t = new MedusaTimeline(10f); t.Tick(0.9f, true); Assert.AreEqual(MedusaPhase.Coming, t.Tick(3f, true)); }
    [Test] public void StonedAfterHalfSecondOfLooking() { var t = AtDanger(10f); Assert.AreEqual(MedusaPhase.Danger, t.Tick(0.3f, true)); Assert.AreEqual(MedusaPhase.Stoned, t.Tick(0.25f, true)); }
    [Test] public void LookingMustBeContinuous() { var t = AtDanger(10f); t.Tick(0.3f, true); t.Tick(0.1f, false); Assert.AreEqual(MedusaPhase.Danger, t.Tick(0.3f, true)); }
    [Test] public void EyesClosedNeverStones() { var t = AtDanger(15f); Assert.AreEqual(MedusaPhase.Danger, t.Tick(14f, false)); }
    [Test] public void ClueStartsAtTheDelay() { var t = AtDanger(5f); Assert.AreEqual(MedusaPhase.Danger, t.Tick(4.99f, false)); Assert.AreEqual(MedusaPhase.Clue, t.Tick(0.02f, false)); }
    [Test] public void WonAfterClueLastsTwoSeconds() { var t = AtDanger(5f); t.Tick(5f, false); Assert.AreEqual(MedusaPhase.Clue, t.Tick(1.99f, false)); Assert.AreEqual(MedusaPhase.Won, t.Tick(0.02f, false)); Assert.IsTrue(t.IsOver); }
    [Test] public void LookingDuringClueStones() { var t = AtDanger(5f); t.Tick(5f, false); Assert.AreEqual(MedusaPhase.Stoned, t.Tick(0.5f, true)); }
    [Test] public void LookingCarriesFromDangerIntoClue() { var t = AtDanger(5f); t.Tick(4.8f, false); t.Tick(0.3f, true); Assert.AreEqual(MedusaPhase.Stoned, t.Tick(0.25f, true)); }
    [Test] public void StonedScreenLastsTwoSeconds() { var t = AtDanger(10f); t.Tick(0.5f, true); Assert.IsFalse(t.IsOver); t.Tick(1.9f, false); Assert.IsFalse(t.IsOver); t.Tick(0.2f, false); Assert.IsTrue(t.IsOver); Assert.AreEqual(MedusaPhase.Stoned, t.Phase); }
    [Test] public void ClueDelayIsClamped() { var early = AtDanger(1f); Assert.AreEqual(MedusaPhase.Danger, early.Tick(4.9f, false)); Assert.AreEqual(MedusaPhase.Clue, early.Tick(0.2f, false)); var late = AtDanger(30f); Assert.AreEqual(MedusaPhase.Danger, late.Tick(14.9f, false)); Assert.AreEqual(MedusaPhase.Clue, late.Tick(0.2f, false)); }
    [Test] public void BigStepCrossesPhasesAndOnlyCountsDangerTime() { Assert.AreEqual(MedusaPhase.Danger, new MedusaTimeline(10f).Tick(4f, true)); Assert.AreEqual(MedusaPhase.Stoned, new MedusaTimeline(10f).Tick(4.6f, true)); }
    [Test] public void AbortEndsEverything() { var t = AtDanger(10f); t.Abort(); Assert.IsTrue(t.IsOver); Assert.AreEqual(MedusaPhase.Aborted, t.Tick(10f, true)); }
    [Test] public void ClueNotReachedBeforeTheDelay() { var t = AtDanger(10f); t.Tick(9f, false); Assert.IsFalse(t.ClueReached); }
    [Test] public void ClueReachedEvenWhenAHitchSkipsStraightToWon() { var t = AtDanger(5f); Assert.AreEqual(MedusaPhase.Won, t.Tick(30f, false)); Assert.IsTrue(t.ClueReached); }
    [Test] public void ClueReachedWhenAHitchGoesThroughClueToStoned() { var t = AtDanger(5f); t.Tick(4.9f, false); Assert.AreEqual(MedusaPhase.Stoned, t.Tick(0.6f, true)); Assert.IsTrue(t.ClueReached); }
    [Test] public void NothingChangesAfterWon() { var t = AtDanger(5f); t.Tick(7.1f, false); Assert.AreEqual(MedusaPhase.Won, t.Tick(5f, true)); }
}
