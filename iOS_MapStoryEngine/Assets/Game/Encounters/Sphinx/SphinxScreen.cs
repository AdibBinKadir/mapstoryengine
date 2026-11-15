using System;
using Odyssey;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The Sphinx's overlay, built in code with the Odyssey theme: her subtitles, what she heard, two answer
// pips, a hold-to-talk Speak button, Leave, and a typed fallback.
public sealed class SphinxScreen
{
    GameObject root;
    RectTransform safeArea;
    Text line;
    Text riddle;
    Text heard;
    Text status;
    Image[] pips;
    SphinxHoldButton speak;
    Image speakFace;
    Text speakLabel;
    RectTransform typing;
    InputField input;

    SphinxScreen() { }

    public static SphinxScreen Create(Action onSpeakDown, Action onSpeakUp, Action<string> onTyped, Action onLeave)
    {
        var s = new SphinxScreen();
        s.root = new GameObject("Sphinx screen", typeof(RectTransform));
        var canvas = s.root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        var scaler = s.root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(390, 844);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        s.root.AddComponent<GraphicRaycaster>();
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)).transform.SetParent(s.root.transform);

        s.safeArea = OdysseyTheme.Inset(s.root.transform, "Safe area", 0);
        Transform area = s.safeArea;

        // Top: Leave + the two answer pips.
        RectTransform leave = OdysseyTheme.Rect(area, "Leave", Vector2.up, Vector2.up, new Vector2(16, -60), new Vector2(116, -16));
        OdysseyTheme.Button(leave, () => onLeave());
        OdysseyTheme.Label(OdysseyTheme.Inset(leave, "Label", 4), "Leave", 18, OdysseyTheme.Ivory);
        RectTransform pipRow = OdysseyTheme.Rect(area, "Answers", Vector2.one, Vector2.one, new Vector2(-150, -60), new Vector2(-16, -16));
        OdysseyTheme.Tint(pipRow, OdysseyTheme.Ink);
        OdysseyTheme.Frame(pipRow, OdysseyTheme.Bronze, 0);
        OdysseyTheme.Label(OdysseyTheme.Rect(pipRow, "Label", Vector2.zero, new Vector2(0.62f, 1), new Vector2(8, 0), Vector2.zero),
            "Riddles", 16, OdysseyTheme.Ivory);
        s.pips = new Image[SphinxProgress.CorrectNeeded];
        for (int i = 0; i < s.pips.Length; i++)
        {
            float x = 0.7f + i * 0.16f;
            s.pips[i] = OdysseyTheme.Tint(OdysseyTheme.Rect(pipRow, "Pip " + i, new Vector2(x, 0.5f), new Vector2(x, 0.5f),
                new Vector2(-8, -8), new Vector2(8, 8)), OdysseyTheme.DeepSea);
        }

        // Lower middle: her subtitles, then what she heard and the status.
        RectTransform panel = OdysseyTheme.Rect(area, "Subtitles", Vector2.zero, Vector2.right, new Vector2(16, 230), new Vector2(-16, 430));
        OdysseyTheme.Tint(panel, new Color(OdysseyTheme.Ink.r, OdysseyTheme.Ink.g, OdysseyTheme.Ink.b, 0.88f));
        OdysseyTheme.Frame(panel, OdysseyTheme.Bronze, 0);
        s.line = OdysseyTheme.Label(OdysseyTheme.Rect(panel, "Line", new Vector2(0, 0.45f), Vector2.one, new Vector2(14, 0), new Vector2(-14, -10)),
            "", 19, OdysseyTheme.Ivory);
        s.riddle = OdysseyTheme.Label(OdysseyTheme.Rect(panel, "Riddle", Vector2.zero, new Vector2(1, 0.45f), new Vector2(14, 10), new Vector2(-14, 0)),
            "", 17, OdysseyTheme.Gold);
        s.riddle.fontStyle = FontStyle.Italic;
        s.heard = OdysseyTheme.Label(OdysseyTheme.Rect(area, "Heard", Vector2.zero, Vector2.right, new Vector2(16, 196), new Vector2(-16, 226)),
            "", 15, OdysseyTheme.Muted);
        s.status = OdysseyTheme.Label(OdysseyTheme.Rect(area, "Status", Vector2.zero, Vector2.right, new Vector2(16, 166), new Vector2(-16, 196)),
            "", 16, OdysseyTheme.Gold);

        // Typed fallback: shown in the Editor, or after a mic problem.
        s.typing = OdysseyTheme.Rect(area, "Typing", Vector2.zero, Vector2.right, new Vector2(16, 122), new Vector2(-16, 162));
        RectTransform field = OdysseyTheme.Rect(s.typing, "Field", Vector2.zero, new Vector2(0.74f, 1), Vector2.zero, new Vector2(-6, 0));
        OdysseyTheme.Tint(field, OdysseyTheme.Ivory, true);
        s.input = field.gameObject.AddComponent<InputField>();
        Text typed = OdysseyTheme.Label(OdysseyTheme.Inset(field, "Text", 8), "", 17, OdysseyTheme.Ink, TextAnchor.MiddleLeft);
        typed.supportRichText = false;
        Text hint = OdysseyTheme.Label(OdysseyTheme.Inset(field, "Placeholder", 8), "Type to the Sphinx...", 17,
            OdysseyTheme.Bronze, TextAnchor.MiddleLeft);
        hint.fontStyle = FontStyle.Italic;
        s.input.textComponent = typed;
        s.input.placeholder = hint;
        s.input.lineType = InputField.LineType.SingleLine;
        RectTransform send = OdysseyTheme.Rect(s.typing, "Send", new Vector2(0.74f, 0), Vector2.one, Vector2.zero, Vector2.zero);
        InputField inputField = s.input;
        OdysseyTheme.Button(send, () =>
        {
            string text = inputField.text.Trim();
            if (text.Length == 0) return;
            inputField.text = "";
            onTyped(text);
        });
        OdysseyTheme.Label(OdysseyTheme.Inset(send, "Label", 4), "Send", 17, OdysseyTheme.Ivory);
        s.ShowTyping(Application.isEditor);

        // Bottom: hold to speak.
        RectTransform speakRect = OdysseyTheme.Rect(area, "Speak", new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(-110, 24), new Vector2(110, 108));
        s.speakFace = OdysseyTheme.Tint(speakRect, OdysseyTheme.Gold, true);
        OdysseyTheme.Frame(speakRect, OdysseyTheme.Ink, 1);
        s.speakLabel = OdysseyTheme.Label(OdysseyTheme.Inset(speakRect, "Label", 4), "Hold to speak", 22, OdysseyTheme.Ink);
        s.speak = speakRect.gameObject.AddComponent<SphinxHoldButton>();
        s.speak.Down = onSpeakDown;
        s.speak.Up = onSpeakUp;

        OdysseyTheme.FitSafeArea(s.safeArea);
        return s;
    }

    public void SetLine(string text, string riddleText)
    {
        line.text = text ?? "";
        riddle.text = riddleText ?? "";
    }

    public void SetHeard(string text) { heard.text = string.IsNullOrEmpty(text) ? "" : "You said: \"" + text + "\""; }
    public void SetStatus(string text) { status.text = text ?? ""; }
    public void ShowTyping(bool show) { typing.gameObject.SetActive(show); }

    public void SetPips(int correct)
    {
        for (int i = 0; i < pips.Length; i++) pips[i].color = i < correct ? OdysseyTheme.Gold : OdysseyTheme.DeepSea;
    }

    public void SetListening(bool listening)
    {
        speakFace.color = listening ? OdysseyTheme.Red : OdysseyTheme.Gold;
        speakLabel.text = listening ? "Listening..." : "Hold to speak";
    }

    public void SetSpeakEnabled(bool enabled)
    {
        speak.Interactable = enabled;
        speakFace.color = enabled ? OdysseyTheme.Gold : OdysseyTheme.Muted;
    }

    public void Refresh() { OdysseyTheme.FitSafeArea(safeArea); }

    public void Destroy()
    {
        if (root != null) UnityEngine.Object.Destroy(root);
        root = null;
    }
}
