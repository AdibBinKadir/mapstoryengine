using NUnit.Framework;

public class DropoutGraceTests
{
    [Test] public void FalseStartStaysFalse() { Assert.IsFalse(new DropoutGrace(0.1f).Step(false, 0.016f)); }
    [Test] public void ShortGapIsBridged() { var g = new DropoutGrace(0.1f); g.Step(true, 0.016f); Assert.IsTrue(g.Step(false, 0.05f)); Assert.IsTrue(g.Step(false, 0.04f)); }
    [Test] public void LongGapEnds() { var g = new DropoutGrace(0.1f); g.Step(true, 0.016f); g.Step(false, 0.06f); Assert.IsFalse(g.Step(false, 0.05f)); }
    [Test] public void ForcedFalseEndsImmediately() { var g = new DropoutGrace(0.1f); g.Step(true, false, 0.016f); Assert.IsFalse(g.Step(false, true, 0.016f)); Assert.IsFalse(g.Step(false, false, 0.016f)); }
    [Test] public void ForcedFalseWinsOverRaw() { var g = new DropoutGrace(0.1f); Assert.IsFalse(g.Step(true, true, 0.016f)); }
    [Test] public void TrueResetsTheGap() { var g = new DropoutGrace(0.1f); g.Step(true, 0.016f); g.Step(false, 0.08f); g.Step(true, 0.016f); Assert.IsTrue(g.Step(false, 0.08f)); }
}
