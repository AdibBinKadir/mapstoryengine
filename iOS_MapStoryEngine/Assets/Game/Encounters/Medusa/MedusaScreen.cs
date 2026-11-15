using UnityEngine;
using UnityEngine.UI;

// Medusa's full-screen takeover, built by code so there's no prefab to keep in sync.
public sealed class MedusaScreen
{
    const string ComingText = "MEDUSA IS COMING";

    GameObject root;
    Image background;
    RawImage image;
    GameObject comingText;
    GameObject cluePanel;
    Text clueText;
    GameObject stonedOverlay;
    MedusaPhase phase;

    MedusaScreen() { }

    public static MedusaScreen Create(Texture medusaImage)
    {
        var s = new MedusaScreen();
        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        s.root = new GameObject("MedusaScreen");
        var canvas = s.root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = s.root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1179f, 2556f);
        scaler.matchWidthOrHeight = 0.5f;
        s.root.AddComponent<GraphicRaycaster>();   // swallows taps meant for the AR view underneath

        s.background = Child<Image>(s.root.transform, "Background");
        s.background.color = Color.black;

        // Image sits in a full-stretch holder; the fitter envelopes it so it fills without stretching.
        var holder = Child<RectMask2D>(s.root.transform, "ImageHolder");
        s.image = Child<RawImage>(holder.transform, "Image");
        s.image.texture = medusaImage;
        var fitter = s.image.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = medusaImage != null && medusaImage.height > 0
            ? (float)medusaImage.width / medusaImage.height
            : 474f / 1024f;

        var coming = Label(s.root.transform, "ComingText", font, 90, FontStyle.Bold);
        coming.text = ComingText;
        s.comingText = coming.gameObject;

        var panel = Child<Image>(s.root.transform, "CluePanel");
        panel.color = new Color(0f, 0f, 0f, 0.7f);
        var panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0.2f);
        s.cluePanel = panel.gameObject;
        s.clueText = Label(panel.transform, "ClueText", font, 54, FontStyle.Normal);
        s.clueText.rectTransform.offsetMin = new Vector2(48f, 24f);
        s.clueText.rectTransform.offsetMax = new Vector2(-48f, -24f);

        var stoned = Child<Image>(s.root.transform, "StonedOverlay");
        stoned.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        var stonedText = Label(stoned.transform, "StonedText", font, 60, FontStyle.Normal);
        stonedText.supportRichText = true;
        stonedText.text = "<size=140><b>STONED</b></size>\nYou looked into Medusa's eyes.";
        s.stonedOverlay = stoned.gameObject;

        s.Show(MedusaPhase.Hijack);
        return s;
    }

    public void Show(MedusaPhase newPhase)
    {
        phase = newPhase;
        bool showImage = newPhase == MedusaPhase.Hijack || newPhase == MedusaPhase.Danger
            || newPhase == MedusaPhase.Clue || newPhase == MedusaPhase.Stoned;
        bool visible = newPhase != MedusaPhase.Won && newPhase != MedusaPhase.Aborted;

        root.SetActive(visible);
        background.enabled = visible;
        image.enabled = showImage;
        comingText.SetActive(newPhase == MedusaPhase.Coming);
        cluePanel.SetActive(newPhase == MedusaPhase.Clue);
        stonedOverlay.SetActive(newPhase == MedusaPhase.Stoned);
    }

    public void SetClue(string text)
    {
        clueText.text = text;
    }

    public void UpdateEffects(float time)
    {
        float alpha = phase == MedusaPhase.Hijack ? 0.4f + 0.6f * Mathf.Abs(Mathf.Sin(time * 40f)) : 1f;
        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }

    public void Destroy()
    {
        if (root != null) Object.Destroy(root);
        root = null;
    }

    static T Child<T>(Transform parent, string name) where T : Component
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return go.AddComponent<T>();
    }

    static Text Label(Transform parent, string name, Font font, int size, FontStyle style)
    {
        var t = Child<Text>(parent, name);
        t.font = font;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }
}
