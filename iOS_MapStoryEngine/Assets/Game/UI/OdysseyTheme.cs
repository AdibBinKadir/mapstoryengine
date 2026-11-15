using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Odyssey
{
    internal static class OdysseyTheme
    {
        internal static readonly Color Ink = new Color32(16, 34, 37, 255);
        internal static readonly Color DeepSea = new Color32(24, 52, 55, 255);
        internal static readonly Color Sea = new Color32(94, 150, 145, 255);
        internal static readonly Color Gold = new Color32(209, 175, 105, 255);
        internal static readonly Color Bronze = new Color32(138, 108, 65, 255);
        internal static readonly Color Ivory = new Color32(243, 232, 199, 255);
        internal static readonly Color Muted = new Color32(201, 200, 172, 255);
        internal static readonly Color Red = new Color32(212, 101, 88, 255);
        private static Font font;
        private static bool triedFont;

        internal static Font Font
        {
            get
            {
                if (!triedFont || font == null)
                {
                    triedFont = true;
                    font = Resources.Load<Font>("Odyssey/CaesarDressing-Regular");
                    if (font == null)
                    {
                        Debug.LogWarning("Caesar Dressing Regular could not be loaded from Resources/Odyssey/CaesarDressing-Regular.");
                        font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    }
                }
                return font;
            }
        }

        internal static void StyleLegacyCanvas(Transform root)
        {
            foreach (UnityEngine.UI.Text text in root.GetComponentsInChildren<UnityEngine.UI.Text>(true))
            {
                text.font = Font;
                text.fontStyle = FontStyle.Normal;
            }
            foreach (var button in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                if (button.targetGraphic == null)
                    continue;
                button.targetGraphic.color = DeepSea;
                if (button.targetGraphic is UnityEngine.UI.Image face)
                    face.sprite = null;
                foreach (var label in button.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                    label.color = Ivory;
                Frame(button.transform, Bronze, 1);
            }
        }

        internal static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        internal static RectTransform Inset(Transform parent, string name, float size)
        {
            return Rect(parent, name, Vector2.zero, Vector2.one, Vector2.one * size, Vector2.one * -size);
        }

        internal static UnityEngine.UI.Image Tint(RectTransform rect, Color color, bool raycast = false)
        {
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        internal static UnityEngine.UI.Text Label(RectTransform rect, string value, int size, Color color,
            TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var text = rect.gameObject.AddComponent<UnityEngine.UI.Text>();
            text.font = Font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.text = value;
            text.raycastTarget = false;
            return text;
        }

        internal static UnityEngine.UI.Button Button(RectTransform rect, UnityAction click, bool primary = false)
        {
            var face = Tint(rect, primary ? Gold : DeepSea, true);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = face;
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            var colors = button.colors;
            colors.highlightedColor = new Color(0.91f, 0.94f, 0.91f);
            colors.pressedColor = new Color(0.69f, 0.75f, 0.71f);
            colors.fadeDuration = 0.12f;
            button.colors = colors;
            button.onClick.AddListener(click);
            Frame(rect, primary ? Ink : Bronze, 1);
            return button;
        }

        internal static void Frame(Transform parent, Color color, float inset)
        {
            var frame = Inset(parent, "Border", inset);
            Tint(Rect(frame, "Top", Vector2.up, Vector2.one, new Vector2(0, -1), Vector2.zero), color);
            Tint(Rect(frame, "Bottom", Vector2.zero, Vector2.right, Vector2.zero, Vector2.up), color);
            Tint(Rect(frame, "Left", Vector2.zero, Vector2.up, Vector2.zero, Vector2.right), color);
            Tint(Rect(frame, "Right", Vector2.right, Vector2.one, -Vector2.right, Vector2.zero), color);
        }

        internal static void FitSafeArea(RectTransform panel)
        {
            if (Screen.width <= 0 || Screen.height <= 0)
                return;
            Rect area = Screen.safeArea;
            panel.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            panel.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            panel.offsetMin = panel.offsetMax = Vector2.zero;
        }
    }
}
