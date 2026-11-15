using System.Collections.Generic;
using UnityEngine;

namespace Odyssey
{
    internal static class EncounterJournal
    {
        private const string SavePrefix = "Odyssey.Journal.v1.";
        private static readonly Dictionary<EncounterType, string> Text = new Dictionary<EncounterType, string>
        {
            { EncounterType.Cyclops, "Building of Ithaca is a triangle." },
            { EncounterType.Sphinx, "This building is not Ithaca." },
            { EncounterType.Medusa, "The name of Ithaca in this world starts with P." }
        };
        private static readonly HashSet<EncounterType> active = new HashSet<EncounterType>();
        private static readonly HashSet<EncounterType> unlocked = new HashSet<EncounterType>();

        internal static void Restore()
        {
            active.Clear();
            unlocked.Clear();
            foreach (var entry in Text)
                if (PlayerPrefs.GetInt(SavePrefix + entry.Key, 0) == 1)
                    unlocked.Add(entry.Key);
        }

        internal static void ClearActiveEncounters() => active.Clear();

        internal static void Begin(EncounterType type)
        {
            if (VoyageState.Started && Text.ContainsKey(type))
                active.Add(type);
        }

        internal static void Complete(EncounterType type, bool won)
        {
            if (!active.Remove(type) || !won || !unlocked.Add(type))
                return;

            PlayerPrefs.SetInt(SavePrefix + type, 1);
            PlayerPrefs.Save();
            GameEvents.RaiseClueUnlocked(Text[type]);
        }

        internal static string EarnedClue(EncounterType type) => unlocked.Contains(type) ? Text[type] : null;
    }
}
