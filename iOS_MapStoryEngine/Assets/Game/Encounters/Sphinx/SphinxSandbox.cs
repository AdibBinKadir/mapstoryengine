using UnityEngine;

// Sandbox controls for P2_Sphinx.unity: Begin / Practice / Abort, the server URL, and a log of what the
// encounter tells the game. No hotkeys: they would fire while typing an answer.
[RequireComponent(typeof(SphinxEncounter))]
public class SphinxSandbox : MonoBehaviour
{
    const string UrlPrefKey = "odyssey.serverUrl";
    SphinxEncounter encounter;
    string url;
    string lastEvent = "";

    void Awake()
    {
        encounter = GetComponent<SphinxEncounter>();
        url = PlayerPrefs.GetString(UrlPrefKey, SphinxClient.ServerUrl);
    }

    void Start() { GetComponent<SphinxClient>().ServerUrlOverride = url; }

    void OnEnable()
    {
        GameEvents.ClueUnlocked += OnClue;
        GameEvents.EncounterStarted += OnStarted;
        GameEvents.EncounterEnded += OnEnded;
    }

    void OnDisable()
    {
        GameEvents.ClueUnlocked -= OnClue;
        GameEvents.EncounterStarted -= OnStarted;
        GameEvents.EncounterEnded -= OnEnded;
    }

    void OnClue(string text) { Log("clue unlocked: " + text); }
    void OnStarted(EncounterType type) { Log("encounter started: " + type); }
    void OnEnded(EncounterType type, bool won) { Log("encounter ended: " + type + " won=" + won); }

    void Log(string message)
    {
        lastEvent = message;
        Debug.Log("[SphinxSandbox] " + message);
    }

    void OnGUI()
    {
        float scale = Mathf.Max(1f, Screen.dpi / 160f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float width = Screen.width / scale;
        GUILayout.BeginArea(new Rect(width - 330, 70, 320, 260), GUI.skin.box);
        GUILayout.Label("Sphinx sandbox");
        GUILayout.BeginHorizontal();
        GUILayout.Label("Server", GUILayout.Width(50));
        url = GUILayout.TextField(url ?? "");
        if (GUILayout.Button("Save", GUILayout.Width(50)))
        {
            GetComponent<SphinxClient>().ServerUrlOverride = url;
            PlayerPrefs.SetString(UrlPrefKey, url.Trim());
            PlayerPrefs.Save();
        }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Begin")) encounter.Begin();
        if (GUILayout.Button("Practice")) encounter.BeginPractice();
        if (GUILayout.Button("Abort")) encounter.Abort();
        GUILayout.EndHorizontal();
        GUILayout.Label("running: " + encounter.IsRunning);
        GUILayout.Label(lastEvent);
        GUILayout.EndArea();
    }
}
