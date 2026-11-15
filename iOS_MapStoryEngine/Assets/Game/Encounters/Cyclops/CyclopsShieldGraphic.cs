using UnityEngine;
using UnityEngine.UI;

namespace Odyssey.Cyclops
{
    public sealed class CyclopsShieldGraphic : RawImage
    {
        private const float RaisedTop = 0.52f;
        private const float ShieldHeight = 0.69f;
        private RectTransform viewport;
        private float lift;

        internal void Initialize(RectTransform viewport)
        {
            this.viewport = viewport;
            texture = Resources.Load<Texture2D>("Odyssey/AspisShield");
            raycastTarget = false;
            if (texture == null)
                Debug.LogError("The Cyclops shield texture is missing.", this);

            HideImmediately();
        }

        internal void SetRaised(bool raised, float deltaTime)
        {
            if (raised && !gameObject.activeSelf && texture != null)
            {
                lift = 0f;
                gameObject.SetActive(true);
            }

            if (!gameObject.activeSelf)
                return;

            float duration = raised ? 0.19f : 0.17f;
            lift = Mathf.MoveTowards(lift, raised ? 1f : 0f, deltaTime / duration);
            if (!raised && lift <= 0f)
            {
                HideImmediately();
                return;
            }

            Vector2 size = viewport.rect.size;
            float progress = Mathf.SmoothStep(0f, 1f, lift);
            float height = size.y * ShieldHeight;
            rectTransform.sizeDelta = new Vector2(height * texture.width / texture.height, height);
            rectTransform.anchoredPosition = new Vector2(
                Mathf.Lerp(-size.x * 0.055f, size.x * 0.025f, progress),
                size.y * Mathf.Lerp(-0.07f, RaisedTop, progress));
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-8f, -2f, progress));
            canvasRenderer.SetAlpha(Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(lift / 0.22f)));
        }

        internal void HideImmediately()
        {
            if (!gameObject.activeSelf)
                return;

            lift = 0f;
            canvasRenderer.SetAlpha(0f);
            gameObject.SetActive(false);
        }
    }
}
