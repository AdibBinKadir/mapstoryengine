using UnityEngine;
using UnityEngine.XR.ARFoundation;

// The Sphinx: an AR boss you argue with by voice. Two correct riddle answers and she "reveals" the clue
// she was guarding: "Not here." Talks to the game only through GameEvents; to the laptop server through
// SphinxClient. Spec: docs/superpowers/specs/2026-11-15-sphinx-design.md
public class SphinxEncounter : MonoBehaviour, IEncounter
{
    public const string ClueText = "Not here.";
    const string ModelResource = "SphinxModel";   // optional prefab in Sphinx/Resources
    const string OfflineLine = "The Sphinx ignores you. (No answer from the oracle. Try again.)";
    const float MinRecordingSeconds = 0.3f;
    const float RevealPauseSeconds = 1f;

    public float distance = 3.5f;      // metres from the camera to her centre (she is ~2.5 m long)
    public float belowCamera = 1.2f;   // metres below the camera (roughly the floor for a standing player)

    SphinxClient client;
    SphinxMic mic;
    SphinxScreen screen;
    SphinxProgress progress;
    AudioSource voice;
    GameObject model;
    Vector3 modelScale = Vector3.one;
    readonly float[] loudness = new float[256];
    float pulse;
    bool practiceRun;
    bool busy;
    int run;                 // bumped on every Begin/Finish; late server replies from an old run are dropped
    float revealEndsAt = -1f;

    public EncounterType Type { get { return EncounterType.Sphinx; } }
    public bool IsRunning { get; private set; }

    void Awake()
    {
        client = GetComponent<SphinxClient>();
        if (client == null) client = gameObject.AddComponent<SphinxClient>();
        voice = gameObject.AddComponent<AudioSource>();
        voice.playOnAwake = false;
        voice.spatialBlend = 0f;
    }

    public void Begin() { Begin(false); }

    // For the voyage's practice button: no game events, no clue.
    public void BeginPractice() { Begin(true); }

    void Begin(bool practice)
    {
        if (IsRunning)
        {
            Debug.LogWarning("[Sphinx] Begin() ignored: already running.");
            return;
        }

        practiceRun = practice;
        IsRunning = true;
        run++;
        busy = false;
        revealEndsAt = -1f;
        progress = new SphinxProgress();
        mic = new SphinxMic();
        PlaceModel();
        screen = SphinxScreen.Create(OnSpeakDown, OnSpeakUp, OnTyped, Abort);
        screen.SetStatus("The Sphinx stirs...");
        screen.SetPips(0);

        if (!practiceRun) GameEvents.RaiseEncounterStarted(EncounterType.Sphinx);

        busy = true;
        int myRun = run;
        client.Speak(SphinxMemory.State("intro", progress),
            reply => { if (myRun == run) OnReply(reply); },
            error => { if (myRun == run) OnFailed(error); });
    }

    public void Abort()
    {
        if (!IsRunning) return;
        Finish(false);
    }

    void OnSpeakDown()
    {
        if (!IsRunning || busy || progress.Won) return;
        voice.Stop();
        if (!mic.StartRecording())
        {
            screen.SetStatus(mic.Problem);
            screen.ShowTyping(true);
            return;
        }
        screen.SetListening(true);
        screen.SetStatus("");
    }

    void OnSpeakUp()
    {
        if (!IsRunning || !mic.IsRecording) return;
        screen.SetListening(false);
        int rate;
        float[] samples = mic.StopRecording(out rate);
        if (samples == null || samples.Length < rate * MinRecordingSeconds)
        {
            screen.SetStatus("Hold Speak while you talk.");
            return;
        }
        Send(Wav.Encode(samples, rate), null);
    }

    void OnTyped(string text)
    {
        if (!IsRunning || busy || progress.Won) return;
        Send(null, text);
    }

    void Send(byte[] wav, string text)
    {
        busy = true;
        screen.SetStatus("...");
        int myRun = run;
        client.Talk(SphinxMemory.State("intro", progress), wav, text,
            reply => { if (myRun == run) OnReply(reply); },
            error => { if (myRun == run) OnFailed(error); });
    }

    void OnReply(SphinxReply reply)
    {
        busy = false;
        progress.Apply(reply.riddle_index, reply.verdict, reply.reveal);
        screen.SetHeard(reply.heard);
        screen.SetLine(reply.line, reply.riddle);
        screen.SetPips(progress.CorrectCount);
        screen.SetStatus("");

        float speakingFor = 3f;
        AudioClip clip = SphinxClient.ToClip(reply.audio);
        if (clip != null)
        {
            if (voice.clip != null) Destroy(voice.clip);
            voice.clip = clip;
            voice.Play();
            speakingFor = clip.length;
        }

        if (progress.Won)
        {
            screen.SetSpeakEnabled(false);
            revealEndsAt = Time.unscaledTime + speakingFor + RevealPauseSeconds;
        }
    }

    void OnFailed(string error)
    {
        busy = false;
        Debug.LogWarning("[Sphinx] Server: " + error);
        screen.SetLine(OfflineLine, null);
        screen.SetStatus("Try again.");
    }

    void Update()
    {
        if (!IsRunning) return;
        screen.Refresh();
        if (mic.ReachedLimit) OnSpeakUp();
        Pulse();
        if (revealEndsAt > 0f && Time.unscaledTime >= revealEndsAt)
        {
            if (!practiceRun) GameEvents.RaiseClueUnlocked(ClueText);
            Finish(true);
        }
    }

    // She swells slightly with the loudness of her own voice.
    void Pulse()
    {
        if (model == null) return;
        float target = 0f;
        if (voice.isPlaying)
        {
            voice.GetOutputData(loudness, 0);
            float sum = 0f;
            for (int i = 0; i < loudness.Length; i++) sum += loudness[i] * loudness[i];
            target = Mathf.Clamp01(Mathf.Sqrt(sum / loudness.Length) * 4f);
        }
        pulse = Mathf.Lerp(pulse, target, Time.unscaledDeltaTime * 12f);
        model.transform.localScale = modelScale * (1f + 0.08f * pulse);
    }

    void PlaceModel()
    {
        Camera cam = null;
        var origin = FindObjectOfType<ARSessionOrigin>();
        if (origin != null) cam = origin.camera;
        if (cam == null) cam = Camera.main;
        // Without a listener Unity plays no sound at all (the P2 sandbox's AR camera has none).
        if (FindObjectOfType<AudioListener>() == null)
            (cam != null ? cam.gameObject : gameObject).AddComponent<AudioListener>();
        Vector3 eye = cam != null ? cam.transform.position : Vector3.zero;
        Vector3 forward = cam != null ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up) : Vector3.forward;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        forward.Normalize();

        var prefab = Resources.Load<GameObject>(ModelResource);
        model = prefab != null ? Instantiate(prefab) : Placeholder();
        model.name = "Sphinx";
        model.transform.position = eye + forward * distance + Vector3.down * belowCamera;
        model.transform.rotation = Quaternion.LookRotation(-forward, Vector3.up);
        modelScale = model.transform.localScale;
    }

    // A gold stand-in until the real model is in Sphinx/Resources/SphinxModel.
    static GameObject Placeholder()
    {
        var root = new GameObject("Sphinx placeholder");
        Part(root, PrimitiveType.Cube, new Vector3(0, 0.35f, 0.1f), new Vector3(0.7f, 0.7f, 1.4f));      // body
        Part(root, PrimitiveType.Cube, new Vector3(0, 0.85f, -0.45f), new Vector3(0.6f, 0.7f, 0.5f));    // chest
        Part(root, PrimitiveType.Sphere, new Vector3(0, 1.4f, -0.5f), new Vector3(0.45f, 0.5f, 0.45f));  // head
        return root;
    }

    static void Part(GameObject parent, PrimitiveType type, Vector3 position, Vector3 scale)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        Destroy(part.GetComponent<Collider>());
        part.transform.SetParent(parent.transform, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().material.color = new Color32(209, 175, 105, 255);
    }

    void Finish(bool won)
    {
        IsRunning = false;   // first, so a re-entrant Abort() from an event handler is ignored
        run++;
        busy = false;
        revealEndsAt = -1f;
        if (mic != null) mic.Cancel();
        voice.Stop();
        if (voice.clip != null) Destroy(voice.clip);
        voice.clip = null;
        if (screen != null) screen.Destroy();
        screen = null;
        if (model != null) Destroy(model);
        model = null;
        GameEvents.RaiseEncounterEnded(EncounterType.Sphinx, won);
    }

    void OnDisable()
    {
        Abort();
    }
}
