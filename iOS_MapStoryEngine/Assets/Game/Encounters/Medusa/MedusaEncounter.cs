using Odyssey;
using UnityEngine;

// Medusa hijacks the screen. Keep your eyes off it until the danger has passed.
// Talks to the game only through GameEvents and Services.
public class MedusaEncounter : MonoBehaviour, IEncounter
{
    public Texture medusaImage;

    MedusaTimeline timeline;
    MedusaScreen screen;
    MedusaPhase lastPhase;
    bool streaming;
    bool warnedNoFace;
    bool practiceRun;
    AudioSource cues;

    public EncounterType Type { get { return EncounterType.Medusa; } }
    public bool IsRunning { get; private set; }

    void Awake()
    {
        cues = OdysseyEncounterAudio.CreateSource(gameObject);
    }

    public void Begin()
    {
        Begin(false);
    }

    internal void BeginPractice()
    {
        Begin(true);
    }

    private void Begin(bool practice)
    {
        if (IsRunning)
        {
            Debug.LogWarning("[Medusa] Begin() ignored: already running.");
            return;
        }

        practiceRun = practice;
        IsRunning = true;
        streaming = false;
        warnedNoFace = false;
        timeline = new MedusaTimeline(Random.Range(MedusaTimeline.ClueEarliestSeconds, MedusaTimeline.ClueLatestSeconds));
        lastPhase = timeline.Phase;
        screen = MedusaScreen.Create(medusaImage);
        screen.Show(MedusaPhase.Hijack);
        cues.PlayOneShot(OdysseyEncounterAudio.MedusaReveal, 0.56f);

        if (!practiceRun) GameEvents.RaiseEncounterStarted(EncounterType.Medusa);
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
        if (!IsRunning || !Application.isFocused) return;

        MedusaPhase phase = timeline.Tick(Time.unscaledDeltaTime, IsLooking());
        screen.UpdateEffects(Time.unscaledTime);

        if (phase != lastPhase)
        {
            lastPhase = phase;
            screen.Show(phase);
            OnPhaseEntered(phase);
            if (!IsRunning) return;
        }

        if (phase == MedusaPhase.Coming && screen.SetCountdown(timeline.Countdown))
            cues.PlayOneShot(OdysseyEncounterAudio.Countdown, 0.45f);

        if (phase == MedusaPhase.Stoned && timeline.IsOver)
        {
            Finish(false);
            if (!practiceRun) KillPlayer();
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

        if (phase == MedusaPhase.Stoned)
            cues.PlayOneShot(OdysseyEncounterAudio.Stoned, 0.58f);
        if (phase == MedusaPhase.Won)
        {
            cues.PlayOneShot(OdysseyEncounterAudio.Victory, 0.55f);
            Finish(true);
        }
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
