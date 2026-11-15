// Each encounter prefab's root script implements this.
public interface IEncounter
{
    EncounterType Type { get; }
    void Begin();   // raise GameEvents.EncounterEnded when done
    void Abort();   // the player died or walked away
}
