using UnityEngine;

namespace Odyssey
{
    internal static class OdysseyEncounterAudio
    {
        private const int SampleRate = 22050;
        private static AudioClip approach;
        private static AudioClip warning;
        private static AudioClip reveal;
        private static AudioClip countdown;
        private static AudioClip stone;
        private static AudioClip victory;

        internal static AudioClip CyclopsProximity => approach != null ? approach
            : approach = Resources.Load<AudioClip>("Odyssey/CyclopsProximity")
                ?? Compose("Cyclops proximity", 0.32f, true, 50, 57, 53, 50);
        internal static AudioClip CyclopsWarning => warning != null ? warning
            : warning = Compose("Cyclops war drum", 0.12f, true, 38, 45);
        internal static AudioClip MedusaReveal => reveal != null ? reveal
            : reveal = Compose("Medusa reveal", 0.12f, true, 65, 64, 57);
        internal static AudioClip MedusaProximity => Resources.Load<AudioClip>("Odyssey/MedusaProximity");
        internal static AudioClip VoyageBackground => Resources.Load<AudioClip>("Odyssey/VoyageBackground");
        internal static AudioClip Countdown => countdown != null ? countdown
            : countdown = Compose("Medusa countdown", 0.18f, false, 57);
        internal static AudioClip Stoned => stone != null ? stone
            : stone = Compose("Turned to stone", 0.15f, true, 53, 45, 38);
        internal static AudioClip Victory => victory != null ? victory
            : victory = Compose("Odyssey victory", 0.14f, false, 57, 60, 64, 69);

        internal static AudioSource CreateSource(GameObject owner)
        {
            var source = owner.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        internal static AudioSource CreateLoop(GameObject owner, AudioClip clip, float volume)
        {
            if (clip == null)
                return null;

            AudioSource source = CreateSource(owner);
            source.clip = clip;
            source.loop = true;
            source.volume = volume;
            return source;
        }

        // Short synthesized lyre-like plucks and a low frame-drum-inspired accent.
        private static AudioClip Compose(string name, float beat, bool drum, params int[] notes)
        {
            const float ring = 0.32f;
            var samples = new float[Mathf.CeilToInt(((notes.Length - 1) * beat + ring) * SampleRate)];
            for (int note = 0; note < notes.Length; note++)
            {
                int offset = Mathf.RoundToInt(note * beat * SampleRate);
                float frequency = 440f * Mathf.Pow(2f, (notes[note] - 69f) / 12f);
                for (int i = 0; i < Mathf.Min(ring * SampleRate, samples.Length - offset); i++)
                {
                    float t = i / (float)SampleRate;
                    float phase = 2f * Mathf.PI * frequency * t;
                    float envelope = Mathf.Clamp01(t * 190f) * Mathf.Exp(-12f * t)
                        * Mathf.Clamp01((ring - t) * 20f);
                    float stringTone = Mathf.Sin(phase) + 0.38f * Mathf.Sin(phase * 2f)
                        + 0.17f * Mathf.Sin(phase * 3.006f) * Mathf.Exp(-14f * t);
                    samples[offset + i] += stringTone * envelope * 0.29f;
                }
            }

            if (drum)
            {
                int length = Mathf.Min(samples.Length, Mathf.RoundToInt(0.19f * SampleRate));
                for (int i = 0; i < length; i++)
                {
                    float t = i / (float)SampleRate;
                    float pulse = Mathf.Sin(2f * Mathf.PI * (75f * t + 2f * (1f - Mathf.Exp(-35f * t))));
                    float skin = Mathf.Sin(2f * Mathf.PI * 217f * t) * Mathf.Exp(-43f * t);
                    samples[i] += (pulse + skin * 0.4f) * Mathf.Clamp01(t * 160f)
                        * Mathf.Exp(-32f * t) * 0.24f;
                }
            }

            var clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
