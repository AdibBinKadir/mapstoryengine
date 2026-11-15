using UnityEngine;

// Sandbox controls for Medusa. B = begin, A = abort, K = toggle abort-on-death.
[RequireComponent(typeof(MedusaEncounter))]
public class MedusaSandbox : MonoBehaviour
{
    public bool abortOnKill = true;

    MedusaEncounter encounter;
    KeyboardFaceSignals keyboard;

    void Awake()
    {
        encounter = GetComponent<MedusaEncounter>();
    }

    // Start, not Awake: on the phone FaceTrackingServices registers the real face source first.
    void Start()
    {
        if (Application.isEditor && Services.Face == null)
        {
            keyboard = gameObject.AddComponent<KeyboardFaceSignals>();
            Services.Face = keyboard;
        }
    }

    void OnEnable()
    {
        GameEvents.PlayerDied += OnPlayerDied;
        GameEvents.EncounterStarted += OnEncounterStarted;
        GameEvents.EncounterEnded += OnEncounterEnded;
    }

    void OnDisable()
    {
        GameEvents.PlayerDied -= OnPlayerDied;
        GameEvents.EncounterStarted -= OnEncounterStarted;
        GameEvents.EncounterEnded -= OnEncounterEnded;
    }

    void OnDestroy()
    {
        if (keyboard != null && ReferenceEquals(Services.Face, keyboard)) Services.Face = null;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B)) encounter.Begin();
        if (Input.GetKeyDown(KeyCode.A)) encounter.Abort();
        if (Input.GetKeyDown(KeyCode.K)) abortOnKill = !abortOnKill;
    }

    void OnPlayerDied(string cause)
    {
        Debug.Log("[Sandbox] killed: " + cause);
        if (abortOnKill) encounter.Abort();   // GameState may do this; Medusa must ignore it
    }

    void OnEncounterStarted(EncounterType type)
    {
        Debug.Log("[Sandbox] EncounterStarted " + type);
    }

    void OnEncounterEnded(EncounterType type, bool won)
    {
        Debug.Log("[Sandbox] EncounterEnded " + type + " won=" + won);
    }

    // IMGUI draws above Medusa's overlay canvas, so the buttons work mid-encounter (on the phone too).
    void OnGUI()
    {
        int fontSize = Application.isMobilePlatform ? 32 : 14;
        GUI.skin.label.fontSize = fontSize;
        GUI.skin.button.fontSize = fontSize;

        IFaceSignals f = Services.Face;
        string face = f == null ? "face source: none" :
            "face=" + f.FaceFound + "  eyesClosed=" + f.EyesClosed + "  looking=" + f.LookingAtScreen;
        var arFace = f as ARKitFaceSignals;
        if (arFace != null) face += "  gaze=" + arFace.GazeAngle.ToString("0") + "°";
        GUI.Label(new Rect(10, 10, Screen.width - 20, fontSize * 5),
            face + "\nabortOnKill(K)=" + abortOnKill +
            "  running=" + encounter.IsRunning + "\nB begin, A abort, F/E/L/M face keys");

        float w = Application.isMobilePlatform ? 220f : 120f;
        float h = Application.isMobilePlatform ? 110f : 40f;
        float y = Screen.height - h - 20f;
        if (GUI.Button(new Rect(10, y, w, h), "Begin")) encounter.Begin();
        if (GUI.Button(new Rect(20 + w, y, w, h), "Abort")) encounter.Abort();
        if (GUI.Button(new Rect(30 + 2 * w, y, w, h), "Abort on kill: " + (abortOnKill ? "on" : "off"))) abortOnKill = !abortOnKill;
    }
}
