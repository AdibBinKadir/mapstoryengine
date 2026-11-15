using System.Collections.Generic;

namespace Odyssey
{
    internal static class EncounterJournal
    {
        private static readonly Dictionary<EncounterType, string> Text = new Dictionary<EncounterType, string>
        {
            { EncounterType.Cyclops, "Building of Ithaca is a triangle." },
            { EncounterType.Sphinx, "This building is not Ithaca." },
            { EncounterType.Medusa, "The name of Ithaca in this world starts with P." }
        };
        private static readonly HashSet<EncounterType> active = new HashSet<EncounterType>();
        private static readonly List<EncounterType> unlocked = new List<EncounterType>();
        internal static IReadOnlyList<EncounterType> EarnedEncounters => unlocked;

        internal static void Reset()
        {
            active.Clear();
            unlocked.Clear();
        }

        internal static void Begin(EncounterType type)
        {
            if (VoyageState.Started && Text.ContainsKey(type))
                active.Add(type);
        }

        internal static void Complete(EncounterType type, bool won)
        {
            if (!active.Remove(type) || !won || unlocked.Contains(type))
                return;

            unlocked.Add(type);
            GameEvents.RaiseClueUnlocked(Text[type]);
        }

        internal static string EarnedClue(EncounterType type) => unlocked.Contains(type) ? Text[type] : null;
    }
}
