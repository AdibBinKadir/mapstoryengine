using UnityEngine;

namespace Odyssey
{
    internal sealed class VoyageHealthBar
    {
        private readonly UnityEngine.UI.Text amount;
        private readonly UnityEngine.UI.Image fill;
        private float displayed = -1;
        private int previousHealth = -1;
        private int previousMaximum = -1;

        internal VoyageHealthBar(RectTransform safeArea)
        {
            var panel = OdysseyTheme.Rect(safeArea, "Player health", Vector2.up, Vector2.one,
                new Vector2(16, -66), new Vector2(-16, -12));
            OdysseyTheme.Tint(panel, OdysseyTheme.Ink);
            OdysseyTheme.Frame(panel, OdysseyTheme.Bronze, 0);
            OdysseyTheme.Label(OdysseyTheme.Rect(panel, "Health label", Vector2.up, Vector2.one,
                new Vector2(13, -30), new Vector2(-13, -6)), "Odysseus", 16, OdysseyTheme.Ivory,
                TextAnchor.MiddleLeft);
            amount = OdysseyTheme.Label(OdysseyTheme.Rect(panel, "Hit points", Vector2.up, Vector2.one,
                new Vector2(13, -30), new Vector2(-13, -6)), "", 15, OdysseyTheme.Ivory,
                TextAnchor.MiddleRight);

            var track = OdysseyTheme.Rect(panel, "Health track", Vector2.zero, Vector2.right,
                new Vector2(13, 11), new Vector2(-13, 19));
            OdysseyTheme.Tint(track, new Color32(64, 69, 58, 255));
            fill = OdysseyTheme.Tint(OdysseyTheme.Inset(track, "Health fill", 0), OdysseyTheme.Sea);
        }

        internal void Render(int health, int maximum)
        {
            if (health != previousHealth || maximum != previousMaximum)
            {
                previousHealth = health;
                previousMaximum = maximum;
                amount.text = health + " / " + maximum + " HP";
            }

            float target = (float)health / maximum;
            displayed = displayed < 0 ? target : Mathf.MoveTowards(displayed, target, Time.unscaledDeltaTime * 1.65f);
            fill.rectTransform.anchorMax = new Vector2(displayed, 1);
            fill.color = target <= 0.25f ? OdysseyTheme.Red : target <= 0.6f ? OdysseyTheme.Gold : OdysseyTheme.Sea;
        }
    }
}
