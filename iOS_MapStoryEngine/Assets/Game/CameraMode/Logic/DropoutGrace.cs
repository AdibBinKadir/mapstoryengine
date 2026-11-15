// Keeps a true signal alive through short dropouts (one lost tracking frame shouldn't reset Medusa's count).
public sealed class DropoutGrace
{
    readonly float graceSeconds;
    bool output;
    float gap;

    public DropoutGrace(float graceSeconds)
    {
        this.graceSeconds = graceSeconds;
    }

    // forcedFalse (e.g. eyes seen closed) ends the signal at once: only tracking gaps are bridged.
    public bool Step(bool raw, bool forcedFalse, float deltaSeconds)
    {
        if (forcedFalse)
        {
            Reset();
            return false;
        }
        return Step(raw, deltaSeconds);
    }

    public bool Step(bool raw, float deltaSeconds)
    {
        if (raw)
        {
            gap = 0f;
            output = true;
        }
        else if (output)
        {
            gap += deltaSeconds;
            output = gap < graceSeconds;
        }
        return output;
    }

    public void Reset()
    {
        output = false;
        gap = 0f;
    }
}
