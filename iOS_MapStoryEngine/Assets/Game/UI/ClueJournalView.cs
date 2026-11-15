using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Odyssey
{
    internal sealed class ClueJournalView
    {
        private static readonly EncounterType[] Encounters =
            { EncounterType.Cyclops, EncounterType.Sphinx, EncounterType.Medusa };

        private readonly Dictionary<EncounterType, Text> bodies = new Dictionary<EncounterType, Text>();
        private readonly Text empty;
        internal RectTransform Content { get; }

        internal ClueJournalView(RectTransform viewport)
        {
            Content = OdysseyTheme.Rect(viewport, "Encounter journal", Vector2.up, Vector2.one,
                Vector2.zero, Vector2.zero);
            Content.pivot = new Vector2(0.5f, 1f);
            var stack = Content.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.spacing = 10;
            stack.childControlWidth = stack.childControlHeight = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;
            var size = Content.gameObject.AddComponent<ContentSizeFitter>();
            size.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            empty = OdysseyTheme.Label(OdysseyTheme.Inset(Content, "Empty journal", 0), "No clues discovered yet.",
                18, OdysseyTheme.Ivory, TextAnchor.UpperLeft);
            foreach (var encounter in Encounters)
            {
                var card = OdysseyTheme.Inset(Content, encounter.ToString(), 0);
                OdysseyTheme.Tint(card, new Color32(236, 218, 174, 255));
                OdysseyTheme.Frame(card, OdysseyTheme.Bronze, 2);
                card.Find("Border").gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(15, 15, 10, 13);
                layout.spacing = 5;
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                OdysseyTheme.Label(OdysseyTheme.Inset(card, "Encounter", 0), encounter.ToString(),
                    23, OdysseyTheme.Ink, TextAnchor.MiddleLeft);
                bodies.Add(encounter, OdysseyTheme.Label(OdysseyTheme.Inset(card, "Discovery", 0), "",
                    18, OdysseyTheme.Ink, TextAnchor.UpperLeft));
                card.gameObject.SetActive(false);
            }
        }

        internal void Refresh()
        {
            foreach (var entry in bodies)
            {
                string clue = EncounterJournal.EarnedClue(entry.Key);
                entry.Value.transform.parent.gameObject.SetActive(clue != null);
                string text = clue ?? "";
                if (entry.Value.text != text)
                    entry.Value.text = text;
            }

            var earned = EncounterJournal.EarnedEncounters;
            empty.gameObject.SetActive(earned.Count == 0);
            for (int i = 0; i < earned.Count; i++)
                bodies[earned[i]].transform.parent.SetSiblingIndex(i);
        }
    }
}
