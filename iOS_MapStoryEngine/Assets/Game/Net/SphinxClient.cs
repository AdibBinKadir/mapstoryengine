using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Talks to the laptop server's /sphinx and /sphinx/talk (docs/superpowers/specs/2026-11-15-sphinx-design.md).
public class SphinxClient : MonoBehaviour
{
    public const string UrlPrefKey = "odyssey.serverUrl";
    public const string DefaultUrl = "http://192.168.0.2:8000";
    public const int TimeoutSeconds = 9;   // the server answers within 7.5 s

    // GameConfig's serverUrl wins when it's set; otherwise the value typed into the sandbox / debug UI.
    public static string ServerUrl
    {
        get
        {
            string url = Services.Config != null ? Services.Config.serverUrl : null;
            if (string.IsNullOrEmpty(url))
                url = PlayerPrefs.GetString(UrlPrefKey, Application.isEditor ? "http://127.0.0.1:8000" : DefaultUrl);
            return url.Trim().TrimEnd('/');
        }
        set
        {
            PlayerPrefs.SetString(UrlPrefKey, (value ?? "").Trim());
            PlayerPrefs.Save();
        }
    }

    public Coroutine Speak(SphinxState state, Action<SphinxReply> onReply, Action<string> onError)
    {
        var request = new UnityWebRequest(ServerUrl + "/sphinx", "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(state)));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        return StartCoroutine(Send(request, onReply, onError));
    }

    // Sends the recording (wav) or, for the typed fallback, text. One of them must be set.
    public Coroutine Talk(SphinxState state, byte[] wav, string text, Action<SphinxReply> onReply, Action<string> onError)
    {
        var form = new WWWForm();
        form.AddField("state", JsonUtility.ToJson(state));
        if (wav != null) form.AddBinaryData("audio", wav, "speech.wav", "audio/wav");
        if (text != null) form.AddField("text", text);
        return StartCoroutine(Send(UnityWebRequest.Post(ServerUrl + "/sphinx/talk", form), onReply, onError));
    }

    IEnumerator Send(UnityWebRequest request, Action<SphinxReply> onReply, Action<string> onError)
    {
        using (request)
        {
            request.timeout = TimeoutSeconds;
            yield return request.SendWebRequest();
            if (request.isNetworkError || request.isHttpError)
            {
                onError(request.error + " (" + request.url + ")");
                yield break;
            }

            SphinxReply reply = null;
            try { reply = JsonUtility.FromJson<SphinxReply>(request.downloadHandler.text); }
            catch (ArgumentException e) { onError("bad reply: " + e.Message); yield break; }
            if (reply == null || string.IsNullOrEmpty(reply.line)) { onError("empty reply"); yield break; }
            onReply(reply);
        }
    }

    // Her voice as an AudioClip, or null if there's none or it can't be read.
    public static AudioClip ToClip(string base64Wav)
    {
        if (string.IsNullOrEmpty(base64Wav)) return null;
        try
        {
            int rate, channels;
            float[] samples = Wav.Decode(Convert.FromBase64String(base64Wav), out rate, out channels);
            if (samples.Length == 0) return null;
            AudioClip clip = AudioClip.Create("SphinxVoice", samples.Length / channels, channels, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
        catch (FormatException e)
        {
            Debug.LogWarning("[Sphinx] Can't read her voice: " + e.Message);
            return null;
        }
    }
}
