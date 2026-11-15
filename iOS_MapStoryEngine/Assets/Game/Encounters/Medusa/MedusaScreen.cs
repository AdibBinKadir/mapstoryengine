using Odyssey;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public sealed class MedusaScreen
{
    private GameObject root;
    private RawImage portrait;
    private AspectRatioFitter portraitFitter;
    private Texture stillImage;
    private VideoPlayer video;
    private RectTransform safeArea;
    private RectTransform countdownPanel;
    private Text countdown;
    private RectTransform cluePanel;
    private Text clue;
    private RectTransform stonedPanel;
    private int shownCount;
    private MedusaPhase phase;

    private bool InHunt => phase == MedusaPhase.Danger || phase == MedusaPhase.Clue;

    private MedusaScreen() { }

    public static MedusaScreen Create(Texture medusaImage)
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
        screen.portrait.raycastTarget = false;
        screen.portraitFitter = screen.portrait.gameObject.AddComponent<AspectRatioFitter>();
        screen.portraitFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        screen.stillImage = medusaImage;
        screen.SetPortrait(medusaImage);

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
        screen.Show(MedusaPhase.Hijack);
        screen.PrepareVideo();
        return screen;
    }

    private void SetPortrait(Texture texture)
    {
        portrait.texture = texture;
        portraitFitter.aspectRatio = texture != null ? (float)texture.width / texture.height : 474f / 1024f;
    }

    private void PrepareVideo()
    {
        var clip = Resources.Load<VideoClip>("MedusaLoop");
        if (clip == null)
            return;

        video = root.AddComponent<VideoPlayer>();
        video.playOnAwake = false;
        video.isLooping = true;
        video.clip = clip;
        video.renderMode = VideoRenderMode.APIOnly;
        video.audioOutputMode = VideoAudioOutputMode.Direct;
        if (clip.audioTrackCount > 0)
        {
            video.controlledAudioTrackCount = 1;
            video.EnableAudioTrack(0, true);
            if (video.canSetDirectAudioVolume)
                video.SetDirectAudioVolume(0, 0.65f);
        }
        video.prepareCompleted += OnVideoPrepared;
        video.errorReceived += OnVideoError;
        video.Prepare();
    }

    private void OnVideoPrepared(VideoPlayer player)
    {
        if (InHunt)
            player.Play();
    }

    private void OnVideoError(VideoPlayer player, string message)
    {
        Debug.LogWarning("[Medusa] Video failed, showing the still image: " + message);
        StopVideo();
        SetPortrait(stillImage);
    }

    public void Show(MedusaPhase newPhase)
    {
        bool wasInHunt = InHunt;
        phase = newPhase;
        if (video != null && video.isPrepared && wasInHunt != InHunt)
        {
            if (InHunt)
                video.Play();
            else
                video.Pause();
        }
        root.SetActive(phase != MedusaPhase.Won && phase != MedusaPhase.Aborted);
        portrait.enabled = phase != MedusaPhase.Coming;
        countdownPanel.gameObject.SetActive(phase == MedusaPhase.Coming);
        cluePanel.gameObject.SetActive(phase == MedusaPhase.Clue && !string.IsNullOrEmpty(clue.text));
        stonedPanel.gameObject.SetActive(phase == MedusaPhase.Stoned);
    }

    public bool SetCountdown(int count)
    {
        if (shownCount == count)
            return false;

        shownCount = count;
        countdown.text = count.ToString();
        return true;
    }

    public void SetClue(string text)
    {
        clue.text = text;
        cluePanel.gameObject.SetActive(phase == MedusaPhase.Clue && !string.IsNullOrEmpty(text));
    }

    public void UpdateEffects(float time)
    {
        OdysseyTheme.FitSafeArea(safeArea);
        if (InHunt && video != null && video.frame >= 0)
        {
            Texture frame = video.texture;
            if (frame != null && portrait.texture != frame)
                SetPortrait(frame);
        }
        Color tint = Color.white;
        if (phase == MedusaPhase.Hijack)
            tint.a = 0.4f + 0.6f * Mathf.Abs(Mathf.Sin(time * 40f));
        portrait.color = tint;
    }

    public void Destroy()
    {
        StopVideo();
        if (root != null) Object.Destroy(root);
        root = null;
    }

    private void StopVideo()
    {
        if (video == null)
            return;

        video.prepareCompleted -= OnVideoPrepared;
        video.errorReceived -= OnVideoError;
        video.Stop();
        video = null;
    }
}
