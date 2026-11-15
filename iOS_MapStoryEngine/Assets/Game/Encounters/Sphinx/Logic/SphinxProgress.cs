// Counts the player's correct answers. The phone owns this count; the server only words the replies.
// Two correct answers, the second arriving with the server's reveal, win the encounter.
public sealed class SphinxProgress
{
    public const int CorrectNeeded = 2;

    public int RiddleIndex { get; private set; } = -1;
    public int CorrectCount { get; private set; }
    public bool Won { get; private set; }

    // riddleIndex < 0 keeps the current riddle. A reveal only counts as the last of the correct answers.
    public void Apply(int riddleIndex, string verdict, bool reveal)
    {
        if (Won) return;
        if (riddleIndex >= 0) RiddleIndex = riddleIndex;
        if (verdict != "correct") return;

        CorrectCount++;
        if (reveal && CorrectCount >= CorrectNeeded) Won = true;
    }
}
