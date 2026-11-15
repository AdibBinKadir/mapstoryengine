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
