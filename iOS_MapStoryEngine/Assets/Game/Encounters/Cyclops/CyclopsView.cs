using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Odyssey.Cyclops
{
    internal static class CyclopsView
    {
        private static readonly Color PanelColor = OdysseyTheme.DeepSea;

        internal static Canvas Overlay(GameObject owner)
        {
            var canvas = owner.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            var scaler = owner.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390, 844);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            owner.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        internal static RectTransform Place(Transform parent, string label, Vector2 anchor,
            Vector2 pivot, Vector2 position, Vector2 size)
        {
            var child = new GameObject(label, typeof(RectTransform));
            var rect = (RectTransform)child.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        internal static void FitSafeArea(RectTransform panel)
        {
            OdysseyTheme.FitSafeArea(panel);
        }

        internal static Text Label(RectTransform rect, string value, int size = 16)
        {
            var text = rect.gameObject.AddComponent<Text>();
            text.font = OdysseyTheme.Font;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = OdysseyTheme.Ivory;
            text.text = value;
            text.raycastTarget = false;
            return text;
        }

        internal static Button Button(RectTransform rect, string value, UnityAction click)
        {
            var button = OdysseyTheme.Button(rect, click);
            Label(Place(rect, "Label", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, rect.sizeDelta), value);
            return button;
        }

        internal static Image Background(RectTransform rect, Color tint)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = tint;
            image.raycastTarget = false;
            return image;
        }

        internal static Image Bar(RectTransform rect, Color color)
        {
            Background(rect, PanelColor);
            RectTransform fill = Place(rect, "Fill", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            fill.anchorMax = Vector2.one;
            return Background(fill, color);
        }

        internal static void SetBar(Image bar, float remaining)
        {
            bar.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(remaining), 1);
        }
    }
}
