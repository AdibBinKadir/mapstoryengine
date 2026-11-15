using UnityEngine;

// Hold-to-talk recording: up to 8 s, mono, asked at 16 kHz (the clip's real rate is reported back).
public sealed class SphinxMic
{
    public const int Rate = 16000;
    public const int MaxSeconds = 8;

    AudioClip clip;

    public bool IsRecording { get; private set; }
    public string Problem { get; private set; }

    // Recording stopped by itself at MaxSeconds: the caller should treat it as a release.
    public bool ReachedLimit { get { return IsRecording && !Microphone.IsRecording(null); } }

    public bool StartRecording()
    {
        Problem = null;
        if (IsRecording) return true;
        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            Application.RequestUserAuthorization(UserAuthorization.Microphone);
            Problem = "Allow the microphone, then hold Speak again.";
            return false;
        }
        if (Microphone.devices.Length == 0)
        {
            Problem = "No microphone found. Type instead.";
            return false;
        }
        clip = Microphone.Start(null, false, MaxSeconds, Rate);
        if (clip == null)
        {
            Problem = "The microphone didn't start. Type instead.";
            return false;
        }
        IsRecording = true;
        return true;
    }

    // The recorded samples (trimmed), or null if nothing was recording.
    public float[] StopRecording(out int rate)
    {
        rate = Rate;
        if (!IsRecording) return null;
        bool stillRecording = Microphone.IsRecording(null);
        int length = stillRecording ? Microphone.GetPosition(null) : clip.samples;
        Microphone.End(null);
        IsRecording = false;

        rate = clip.frequency;
        var data = new float[Mathf.Max(0, length) * clip.channels];
        if (data.Length > 0) clip.GetData(data, 0);
        Object.Destroy(clip);
        clip = null;
        return data;
    }

    public void Cancel()
    {
        if (IsRecording) Microphone.End(null);
        IsRecording = false;
        if (clip != null) Object.Destroy(clip);
        clip = null;
    }
}
