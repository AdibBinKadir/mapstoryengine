using Odyssey;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public sealed class MedusaScreen
{
    private GameObject root;
    private RawImage portrait;
    private RectTransform safeArea;
    private RectTransform countdownPanel;
    private Text countdown;
    private RectTransform cluePanel;
    private Text clue;
    private RectTransform stonedPanel;
    private int shownCount;
    private MedusaPhase phase;
    private AspectRatioFitter fitter;
    private VideoPlayer video;
    private RenderTexture videoTexture;
    private bool showingVideo;
    private bool videoFailed;
    private bool playRequested;

    private MedusaScreen() { }

    public static MedusaScreen Create(Texture medusaImage, VideoClip medusaVideo)
    {
        var screen = new MedusaScreen();
        screen.root = new GameObject("Medusa", typeof(RectTransform));
        var canvas = screen.root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = screen.root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(390, 844);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        screen.root.AddComponent<GraphicRaycaster>();
        Transform root = screen.root.transform;

        OdysseyTheme.Tint(OdysseyTheme.Inset(root, "Backdrop", 0), OdysseyTheme.Ink, true);
        RectTransform holder = OdysseyTheme.Inset(root, "Portrait crop", 0);
        holder.gameObject.AddComponent<RectMask2D>();
        screen.portrait = OdysseyTheme.Inset(holder, "Medusa portrait", 0).gameObject.AddComponent<RawImage>();
        screen.portrait.texture = medusaImage;
        screen.portrait.raycastTarget = false;
        var fitter = screen.fitter = screen.portrait.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = medusaImage != null ? (float)medusaImage.width / medusaImage.height : 474f / 1024f;

        screen.safeArea = OdysseyTheme.Inset(root, "Safe area", 0);
        OdysseyTheme.Frame(screen.safeArea, OdysseyTheme.Bronze, 12);
        var center = new Vector2(0.5f, 0.5f);
        screen.countdownPanel = OdysseyTheme.Rect(screen.safeArea, "Countdown", center, center,
            new Vector2(-158, -124), new Vector2(158, 124));
        OdysseyTheme.Tint(screen.countdownPanel, OdysseyTheme.Ink);
        OdysseyTheme.Frame(screen.countdownPanel, OdysseyTheme.Bronze, 0);
        OdysseyTheme.Label(OdysseyTheme.Rect(screen.countdownPanel, "Heading", Vector2.up, Vector2.one,
            new Vector2(12, -62), new Vector2(-12, -16)), "Medusa is coming", 30, OdysseyTheme.Ivory);
        screen.countdown = OdysseyTheme.Label(OdysseyTheme.Rect(screen.countdownPanel, "Count", center, center,
            new Vector2(-90, -46), new Vector2(90, 54)), "", 76, OdysseyTheme.Gold);
        OdysseyTheme.Label(OdysseyTheme.Rect(screen.countdownPanel, "Instruction", Vector2.zero, Vector2.right,
            new Vector2(14, 18), new Vector2(-14, 64)), "Look away or close your eyes.", 19, OdysseyTheme.Ivory);

        screen.cluePanel = OdysseyTheme.Rect(screen.safeArea, "Clue", Vector2.zero, Vector2.right,
            new Vector2(24, 34), new Vector2(-24, 134));
        OdysseyTheme.Tint(screen.cluePanel, OdysseyTheme.Ink);
        OdysseyTheme.Frame(screen.cluePanel, OdysseyTheme.Bronze, 0);
        screen.clue = OdysseyTheme.Label(OdysseyTheme.Inset(screen.cluePanel, "Clue text", 14),
            "", 19, OdysseyTheme.Ivory);

        screen.stonedPanel = OdysseyTheme.Rect(screen.safeArea, "Stoned", center, center,
            new Vector2(-158, -86), new Vector2(158, 86));
        OdysseyTheme.Tint(screen.stonedPanel, OdysseyTheme.Ink);
        OdysseyTheme.Frame(screen.stonedPanel, OdysseyTheme.Bronze, 0);
        OdysseyTheme.Label(OdysseyTheme.Rect(screen.stonedPanel, "Heading", Vector2.up, Vector2.one,
            new Vector2(12, -93), new Vector2(-12, -21)), "Stoned", 44, OdysseyTheme.Gold);
        OdysseyTheme.Label(OdysseyTheme.Rect(screen.stonedPanel, "Explanation", Vector2.zero, Vector2.right,
            new Vector2(14, 18), new Vector2(-14, 72)), "You looked into Medusa's eyes.", 19, OdysseyTheme.Ivory);

        OdysseyTheme.FitSafeArea(screen.safeArea);
        if (medusaVideo != null) screen.AddVideo(medusaVideo);
        screen.Show(MedusaPhase.Hijack);
        return screen;
    }

    // Plays into a RenderTexture; the portrait keeps the still image until the first video frame arrives.
    private void AddVideo(VideoClip clip)
    {
        videoTexture = new RenderTexture((int)clip.width, (int)clip.height, 0);
        video = root.AddComponent<VideoPlayer>();
        video.playOnAwake = false;
        video.isLooping = true;
        video.source = VideoSource.VideoClip;
        video.clip = clip;
        video.renderMode = VideoRenderMode.RenderTexture;
        video.targetTexture = videoTexture;
        video.audioOutputMode = VideoAudioOutputMode.Direct;
        video.EnableAudioTrack(0, true);
        video.errorReceived += OnVideoError;
        video.Prepare();
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        if (videoFailed) return;
        videoFailed = true;
        Debug.LogWarning("[Medusa] Video failed, showing the still image: " + message);
    }

    public void Show(MedusaPhase newPhase)
    {
        phase = newPhase;
        root.SetActive(phase != MedusaPhase.Won && phase != MedusaPhase.Aborted);
        portrait.enabled = phase != MedusaPhase.Coming;
        countdownPanel.gameObject.SetActive(phase == MedusaPhase.Coming);
        cluePanel.gameObject.SetActive(phase == MedusaPhase.Clue && !string.IsNullOrEmpty(clue.text));
        stonedPanel.gameObject.SetActive(phase == MedusaPhase.Stoned);

        if (video == null || videoFailed) return;
        if (MedusaVideoRules.Plays(phase))
        {
            if (!playRequested) video.Play();
            playRequested = true;
        }
        else if (MedusaVideoRules.ShouldPause(phase, playRequested))
        {
            video.Pause();   // Stoned: her last frame stays, the sound stops (also cancels a Play() still preparing)
            playRequested = false;
        }
    }

    public void SetCountdown(int count)
    {
        if (shownCount == count)
            return;

        shownCount = count;
        countdown.text = count.ToString();
    }

    public void SetClue(string text)
    {
        clue.text = text;
        cluePanel.gameObject.SetActive(phase == MedusaPhase.Clue && !string.IsNullOrEmpty(text));
    }

    public void UpdateEffects(float time)
    {
        OdysseyTheme.FitSafeArea(safeArea);
        if (!showingVideo && video != null && !videoFailed && video.isPlaying && video.frame > 0)
        {
            showingVideo = true;
            portrait.texture = videoTexture;
            fitter.aspectRatio = (float)videoTexture.width / videoTexture.height;
        }
        Color tint = Color.white;
        if (phase == MedusaPhase.Hijack)
            tint.a = 0.4f + 0.6f * Mathf.Abs(Mathf.Sin(time * 40f));
        portrait.color = tint;
    }

    public void Destroy()
    {
        if (video != null)
        {
            video.errorReceived -= OnVideoError;
            video.Stop();
        }
        video = null;
        if (videoTexture != null)
        {
            videoTexture.Release();
            Object.Destroy(videoTexture);
        }
        videoTexture = null;
        if (root != null) Object.Destroy(root);
        root = null;
    }
}
