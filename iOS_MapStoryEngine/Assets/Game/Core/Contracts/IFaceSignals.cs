// KeyboardFaceSignals mock: L = toggle looking, F = toggle face, E = toggle eyes closed, M = toggle MirrorSeen.
public interface IFaceSignals
{
    bool FaceFound { get; }
    bool EyesClosed { get; }
    bool LookingAtScreen { get; }   // false whenever EyesClosed
    bool MirrorSeen { get; }   // Medusa: her face was recognised in a mirror held up to the phone
    void StartStreaming();
    void StopStreaming();
}
