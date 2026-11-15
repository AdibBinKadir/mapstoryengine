using UnityEngine;

// Medusa hijacks the screen. Keep your eyes off it; her clue flashes for 2 s at a random moment.
// Talks to the game only through GameEvents and Services.
public class MedusaEncounter : MonoBehaviour, IEncounter
{
    const string FallbackClue = "Ithaca hides from those who dare to look.";
    const float ClueAnswerTimeoutSeconds = 0.2f;

    public Texture medusaImage;

    MedusaTimeline timeline;
    MedusaScreen screen;
    MedusaPhase lastPhase;
    bool streaming;
    bool clueRequested;
    bool waitingForClue;
    bool clueShown;
    float clueRequestedAt;
    bool warnedNoFace;

    public EncounterType Type { get { return EncounterType.Medusa; } }
    public bool IsRunning { get; private set; }

    public void Begin()
    {
        if (IsRunning)
        {
            Debug.LogWarning("[Medusa] Begin() ignored: already running.");
            return;
        }

        IsRunning = true;
        streaming = false;
        clueRequested = false;
        waitingForClue = false;
        clueShown = false;
        warnedNoFace = false;
        timeline = new MedusaTimeline(Random.Range(MedusaTimeline.ClueEarliestSeconds, MedusaTimeline.ClueLatestSeconds));
        lastPhase = timeline.Phase;
        screen = MedusaScreen.Create(medusaImage);
        screen.Show(MedusaPhase.Hijack);

        GameEvents.RaiseEncounterStarted(EncounterType.Medusa);
        if (Services.CameraMode != null) Services.CameraMode.EnterFrontCameraMode();
    }

    public void Abort()
    {
        if (!IsRunning) return;
        timeline.Abort();
        Finish(false);
    }

    void Update()
    {
        if (!IsRunning) return;

        MedusaPhase phase = timeline.Tick(Time.unscaledDeltaTime, IsLooking());
        screen.UpdateEffects(Time.unscaledTime);

        if (phase != lastPhase)
        {
            lastPhase = phase;
            screen.Show(phase);
            OnPhaseEntered(phase);
            if (!IsRunning) return;
        }

        if (waitingForClue && !clueShown && Time.unscaledTime - clueRequestedAt >= ClueAnswerTimeoutSeconds)
            ShowClue(FallbackClue);

        if (phase == MedusaPhase.Stoned && timeline.IsOver)
        {
            Finish(false);
            KillPlayer();
        }
    }

    void OnPhaseEntered(MedusaPhase phase)
    {
        // A frame hitch can jump straight from Coming to Stoned, so streaming starts on any dangerous phase.
        if (!streaming && (phase == MedusaPhase.Danger || phase == MedusaPhase.Clue || phase == MedusaPhase.Stoned))
        {
            streaming = true;
            if (Services.Face != null) Services.Face.StartStreaming();
        }

        // A frame hitch can pass straight through Clue (to Won or Stoned); the clue is still earned.
        if (timeline.ClueReached && !clueRequested) RequestClue();

        if (phase == MedusaPhase.Won) Finish(true);
    }

    void RequestClue()
    {
        clueRequested = true;
        waitingForClue = true;
        clueRequestedAt = Time.unscaledTime;
        GameEvents.ClueUnlocked += OnClueUnlocked;
        GameEvents.RequestClue(ClueTier.Good);
    }

    void OnClueUnlocked(string text)
    {
        if (!IsRunning || !waitingForClue || clueShown) return;
        ShowClue(string.IsNullOrEmpty(text) ? FallbackClue : text);
    }

    void ShowClue(string text)
    {
        clueShown = true;
        screen.SetClue(text);
    }

    bool IsLooking()
    {
        if (Services.Face == null)
        {
            if (!warnedNoFace)
            {
                warnedNoFace = true;
                Debug.LogWarning("[Medusa] Services.Face is not set; treating the player as not looking.");
            }
            return false;
        }
        return Services.Face.LookingAtScreen;
    }

    void Finish(bool won)
    {
        IsRunning = false;   // first, so a re-entrant Abort() from an event handler is ignored
        GameEvents.ClueUnlocked -= OnClueUnlocked;
        waitingForClue = false;
        if (streaming && Services.Face != null) Services.Face.StopStreaming();
        streaming = false;
        if (Services.CameraMode != null) Services.CameraMode.ExitFrontCameraMode();
        if (screen != null) screen.Destroy();
        screen = null;
        GameEvents.RaiseEncounterEnded(EncounterType.Medusa, won);
    }

    // Contract gap: no GameEvents.RequestKill yet. CONTRACTS.md lets Medusa kill instantly with "stoned".
    // Swap this one line for GameEvents.RequestKill("stoned") once the team adds it.
    static void KillPlayer()
    {
        GameEvents.RaisePlayerDied("stoned");
    }

    void OnDisable()
    {
        Abort();
    }
}
