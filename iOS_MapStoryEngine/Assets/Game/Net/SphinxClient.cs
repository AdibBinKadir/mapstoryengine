using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Talks to the shared Odyssey server's /sphinx and /sphinx/talk endpoints.
public class SphinxClient : MonoBehaviour
{
    public const int TimeoutSeconds = 9;   // the server answers within 7.5 s
    public string ServerUrlOverride { get; set; }

    public static string ServerUrl
    {
        get
        {
            string url = Services.Config?.serverUrl;
#if UNITY_EDITOR
            string editorUrl = Environment.GetEnvironmentVariable("ARDYSSEY_ORACLE_URL");
            if (!string.IsNullOrWhiteSpace(editorUrl))
                url = editorUrl;
#endif
            if (string.IsNullOrWhiteSpace(url))
                url = Resources.Load<TextAsset>("Odyssey/OracleEndpoint")?.text;
            if (string.IsNullOrWhiteSpace(url) && Application.isEditor)
                url = "http://127.0.0.1:8787";
            return url == null ? "" : url.Trim().TrimEnd('/');
        }
    }

    public Coroutine Speak(SphinxState state, Action<SphinxReply> onReply, Action<string> onError)
    {
        string endpoint = Endpoint("/sphinx");
        if (endpoint == null) { onError("Configure the Odyssey HTTPS server."); return null; }
        var request = new UnityWebRequest(endpoint, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(state)));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        return StartCoroutine(Send(request, onReply, onError));
    }

    // Sends the recording (wav) or, for the typed fallback, text. One of them must be set.
    public Coroutine Talk(SphinxState state, byte[] wav, string text, Action<SphinxReply> onReply, Action<string> onError)
    {
        string endpoint = Endpoint("/sphinx/talk");
        if (endpoint == null) { onError("Configure the Odyssey HTTPS server."); return null; }
        var form = new WWWForm();
        form.AddField("state", JsonUtility.ToJson(state));
        if (wav != null) form.AddBinaryData("audio", wav, "speech.wav", "audio/wav");
        if (text != null) form.AddField("text", text);
        return StartCoroutine(Send(UnityWebRequest.Post(endpoint, form), onReply, onError));
    }

    private string Endpoint(string path)
    {
        string url = string.IsNullOrWhiteSpace(ServerUrlOverride) ? ServerUrl : ServerUrlOverride.Trim().TrimEnd('/');
        if (!Uri.TryCreate(url + path, UriKind.Absolute, out Uri uri)
            || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            return null;
        if (uri.Scheme == Uri.UriSchemeHttps || (Application.isEditor && uri.IsLoopback && uri.Scheme == Uri.UriSchemeHttp))
            return uri.AbsoluteUri;
        return null;
    }

    IEnumerator Send(UnityWebRequest request, Action<SphinxReply> onReply, Action<string> onError)
    {
        using (request)
        {
            request.timeout = TimeoutSeconds;
            request.redirectLimit = 0;
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
        catch (Exception e)   // a bad WAV must never stop the encounter (e.g. the reveal timer)
        {
            Debug.LogWarning("[Sphinx] Can't read her voice: " + e.Message);
            return null;
        }
    }
}
