using UnityEngine;

namespace Boreatek.Audio
{
    internal enum SoundCue
    {
        Click, Confirm, StepSnow, StepPlanks, StepIce, Jump, Land, Swing, Wood, Metal, Stone, Ice, Crystal,
        Break, Blocked, Deploy, Blast, Eat, Drink, Hurt, Death, Servo, GolemStep, RobotReady, GolemReady,
        RobotStrike, GolemStrike, RobotOff, GolemOff, Reboot, StepStone
    }

    internal enum SoundBed { Foundry, Tundra, Candy, Wrecks, Tunnel, Cavern, Geode, Abyss, Title, Storm }

    internal static class BoreatekSoundBank
    {
        private const int SampleRate = 24000;
        private const float Tau = 2f * Mathf.PI;
        internal const int FootfallVariants = 4;

        private readonly struct Voice
        {
            internal readonly float Time, Start, End, Ring, Grit, Grain, Color;
            internal Voice(float time, float start, float end, float ring, float grit, float grain, float color)
            {
                Time = time;
                Start = start;
                End = end;
                Ring = ring;
                Grit = grit;
                Grain = grain;
                Color = color;
            }
        }

        internal static bool IsFootfall(SoundCue cue) => cue is SoundCue.StepSnow or SoundCue.StepPlanks
            or SoundCue.StepIce or SoundCue.StepStone or SoundCue.Jump or SoundCue.Land or SoundCue.Servo or SoundCue.GolemStep;

        internal static AudioClip MakeFootfall(SoundCue cue, int variant)
        {
            (float duration, float impact, float scuff, float texture, float resonance, float ring) = cue switch
            {
                SoundCue.StepPlanks => (0.17f, 0.72f, 0.40f, 0.17f, 138f, 0.12f),
                SoundCue.StepStone  => (0.16f, 0.67f, 0.46f, 0.30f, 102f, 0.04f),
                SoundCue.StepIce    => (0.19f, 0.51f, 0.72f, 0.34f,  92f, 0.03f),
                SoundCue.Jump       => (0.14f, 0.42f, 0.57f, 0.16f,  78f, 0.03f),
                SoundCue.Land       => (0.21f, 0.94f, 0.52f, 0.19f,  69f, 0.09f),
                SoundCue.Servo      => (0.18f, 0.70f, 0.48f, 0.33f, 216f, 0.13f),
                SoundCue.GolemStep  => (0.26f, 1.12f, 0.69f, 0.19f,  54f, 0.16f),
                _                   => (0.22f, 0.54f, 0.84f, 0.21f,  81f, 0.03f)
            };
            uint rng = 0x50495341u + (uint)cue * 4679u + (uint)variant * 7919u;
            duration *= 1f + Noise(ref rng) * 0.08f;
            resonance *= 1f + Noise(ref rng) * 0.12f;
            float toeOnset = duration * (0.29f + Noise(ref rng) * 0.075f);
            float heelLength = duration * (0.36f + Noise(ref rng) * 0.055f);
            var grainStarts = new float[6];
            var grainStrengths = new float[grainStarts.Length];
            for (int grain = 0; grain < grainStarts.Length; grain++)
            {
                grainStarts[grain] = duration * (0.21f + (grain + Noise(ref rng) * 0.55f) * 0.099f);
                grainStrengths[grain] = 0.48f + Noise(ref rng) * 0.26f;
            }
            var samples = new float[Mathf.RoundToInt(duration * SampleRate)];
            float low = 0f, detail = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)SampleRate;
                float noise = Noise(ref rng);
                low += (noise - low) * 0.042f;
                detail += (noise - detail) * texture;
                float heel = Contact(t, heelLength, 0.12f);
                float toe = Contact(t - toeOnset, duration * 0.40f, 0.25f);
                float grains = 0f;
                for (int grain = 0; grain < grainStarts.Length; grain++)
                    grains += Contact(t - grainStarts[grain], duration * 0.13f, 0.22f) * grainStrengths[grain];
                float pressure = heel + 0.51f * toe;
                float sole = low * impact * pressure;
                float surface = (detail - low) * scuff * (0.30f * heel + 0.42f * toe + grains);
                float thud = Mathf.Sin(Tau * t * resonance) * ring * heel * heel;
                samples[i] = (sole + surface + thud) * 0.76f;
            }
            return Clip($"{cue} {variant + 1}", samples);
        }

        private static float Contact(float time, float duration, float rise)
        {
            float phase = time / duration;
            if (phase <= 0f || phase >= 1f) return 0f;
            float envelope = phase < rise ? phase / rise : (1f - phase) / (1f - rise);
            return envelope * envelope * (3f - 2f * envelope);
        }

        internal static AudioClip MakeCue(SoundCue cue)
        {
            Voice voice = cue switch
            {
                SoundCue.Click       => new Voice(0.089f, 940f, 660f, 0.30f, 0.15f, 0f, 0.32f),
                SoundCue.Confirm     => new Voice(0.40f, 495f, 742f, 0.35f, 0.05f, 3f, 0.38f),
                SoundCue.Swing       => new Voice(0.23f, 251f, 126f, 0.05f, 0.52f, 9f, 0.15f),
                SoundCue.Wood        => new Voice(0.25f, 253f, 112f, 0.40f, 0.60f, 25f, 0.18f),
                SoundCue.Metal       => new Voice(0.38f, 769f, 532f, 0.44f, 0.44f, 16f, 0.58f),
                SoundCue.Stone       => new Voice(0.27f, 150f, 71f, 0.24f, 0.78f, 28f, 0.24f),
                SoundCue.Ice         => new Voice(0.30f, 962f, 445f, 0.25f, 0.64f, 41f, 0.62f),
                SoundCue.Crystal     => new Voice(0.49f, 1047f, 787f, 0.42f, 0.34f, 35f, 0.49f),
                SoundCue.Break       => new Voice(0.42f, 208f, 64f, 0.31f, 0.83f, 49f, 0.34f),
                SoundCue.Blocked     => new Voice(0.25f, 581f, 245f, 0.48f, 0.49f, 9f, 0.51f),
                SoundCue.Deploy      => new Voice(0.34f, 213f, 524f, 0.34f, 0.38f, 8f, 0.30f),
                SoundCue.Blast       => new Voice(0.77f, 86f, 29f, 0.64f, 0.88f, 14f, 0.14f),
                SoundCue.Eat         => new Voice(0.46f, 184f, 136f, 0.08f, 0.74f, 9f, 0.28f),
                SoundCue.Drink       => new Voice(0.46f, 203f, 153f, 0.27f, 0.51f, 7f, 0.065f),
                SoundCue.Hurt        => new Voice(0.31f, 123f, 67f, 0.48f, 0.57f, 15f, 0.13f),
                SoundCue.Death       => new Voice(1.18f, 282f, 73f, 0.40f, 0.24f, 4f, 0.10f),
                SoundCue.RobotReady  => new Voice(0.34f, 137f, 395f, 0.36f, 0.39f, 19f, 0.25f),
                SoundCue.GolemReady  => new Voice(0.50f, 55f, 112f, 0.47f, 0.65f, 28f, 0.15f),
                SoundCue.RobotStrike => new Voice(0.26f, 363f, 88f, 0.34f, 0.76f, 18f, 0.29f),
                SoundCue.GolemStrike => new Voice(0.63f, 95f, 30f, 0.61f, 0.80f, 27f, 0.18f),
                SoundCue.RobotOff    => new Voice(0.76f, 332f, 46f, 0.42f, 0.61f, 16f, 0.37f),
                SoundCue.GolemOff    => new Voice(0.88f, 179f, 51f, 0.42f, 0.77f, 38f, 0.21f),
                _                    => new Voice(0.59f, 112f, 467f, 0.38f, 0.33f, 12f, 0.20f)
            };
            var samples = new float[Mathf.RoundToInt(voice.Time * SampleRate)];
            uint rng = 0x426f7265u + (uint)cue * 7723u;
            float smooth = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)SampleRate;
                float progress = i / (float)(samples.Length - 1);
                float fadeIn = Mathf.Clamp01(t * (cue == SoundCue.Swing ? 19f : 420f));
                float decay = 1f - progress;
                float grain = voice.Grain > 0f ? 0.67f + 0.33f * Mathf.Cos(Tau * t * voice.Grain) : 1f;
                smooth += (Noise(ref rng) - smooth) * voice.Color;
                float phase = Tau * t * (voice.Start + (voice.End - voice.Start) * progress * 0.5f);
                float harmonics = Mathf.Sin(phase) * 0.62f + Mathf.Sin(phase * 2.29f) * 0.23f +
                    Mathf.Sin(phase * 3.71f) * 0.15f;
                samples[i] = (voice.Ring * harmonics * decay + voice.Grit * smooth * grain * decay * decay)
                    * decay * fadeIn * 0.75f;
            }
            return Clip(cue.ToString(), samples);
        }

        internal static AudioClip MakeBed(SoundBed bed)
        {
            if (bed == SoundBed.Title) return MakeTitle();
            (int bass, int overtone, float air, float flutter, float grit) = bed switch
            {
                SoundBed.Foundry => (54, 109, 0.15f, 6f, 0.070f),
                SoundBed.Tundra  => (78, 114, 0.94f, 1f, 0.095f),
                SoundBed.Candy   => (110, 165, 0.34f, 3f, 0.085f),
                SoundBed.Wrecks  => (45, 183, 0.65f, 5f, 0.109f),
                SoundBed.Tunnel  => (61, 122, 0.28f, 8f, 0.070f),
                SoundBed.Cavern  => (63, 141, 0.31f, 2f, 0.100f),
                SoundBed.Geode   => (83, 208, 0.22f, 4f, 0.082f),
                SoundBed.Abyss   => (34, 77, 0.59f, 3f, 0.140f),
                _                => (43, 204, 1.28f, 5f, 0.130f)
            };
            const int duration = 8;
            var samples = new float[duration * SampleRate];
            uint rng = 0x504f4c41u + (uint)bed * 7351u;
            float smooth = 0f, gust = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)SampleRate;
                float cycle = Tau * t / duration;
                float noise = Noise(ref rng);
                smooth += (noise - smooth) * grit;
                gust += (smooth - gust) * 0.018f;
                float gustWeight = 0.63f + 0.22f * Mathf.Cos(cycle * 2f) + 0.15f * Mathf.Sin(cycle * flutter);
                float seam = Mathf.Clamp01(Mathf.Min(t, duration - t) * 10f);
                float texture = (gust * 0.92f + smooth * 0.41f) * air * gustWeight * seam;
                float drone = Mathf.Sin(Tau * t * bass) * (0.028f + (1f - Mathf.Min(air, 1f)) * 0.034f);
                float overtonePulse = Mathf.Pow(Mathf.Max(0f, Mathf.Cos(cycle * flutter + 0.85f)), 8f);
                float detail = bed switch
                {
                    SoundBed.Candy => Mathf.Sin(Tau * t * 661f) * overtonePulse * 0.027f,
                    SoundBed.Geode => Mathf.Sin(Tau * t * 936f) * overtonePulse * 0.028f,
                    SoundBed.Abyss => Mathf.Sin(Tau * t * 47f) * Mathf.Pow(Mathf.Max(0f, Mathf.Cos(cycle)), 2f) * 0.055f,
                    SoundBed.Foundry => Mathf.Sin(Tau * t * 144f) * overtonePulse * 0.040f,
                    _ => 0f
                };
                samples[i] = (texture + drone + detail + Mathf.Sin(Tau * t * overtone) * 0.030f * overtonePulse) * 0.88f;
            }
            return Clip(bed.ToString(), samples);
        }

        private static AudioClip MakeTitle()
        {
            const float beat = 0.55f, duration = 32f * beat;
            var samples = new float[Mathf.RoundToInt(duration * SampleRate)];
            int[] melody = { 62, 69, 65, 74, 62, 69, 65, 72, 58, 65, 62, 70, 58, 65, 67, 65,
                55, 62, 58, 69, 55, 62, 58, 67, 57, 64, 61, 72, 57, 64, 61, 69 };
            int[] roots = { 38, 34, 43, 45 };
            for (int note = 0; note < melody.Length; note++)
            {
                int start = Mathf.RoundToInt(note * beat * SampleRate);
                float frequency = Note(melody[note] + 12);
                int trail = (int)(1.65f * SampleRate);
                for (int i = 0; i < trail; i++)
                {
                    float t = i / (float)SampleRate;
                    float phase = Tau * t * frequency;
                    float bell = Mathf.Sin(phase) * 0.58f + Mathf.Sin(2.006f * phase) * 0.25f +
                        Mathf.Sin(3.99f * phase) * 0.11f;
                    float envelope = Mathf.Clamp01(t * 150f) * Mathf.Exp(-t * 4.4f) *
                        Mathf.Clamp01((1.65f - t) * 13f);
                    samples[(start + i) % samples.Length] += bell * envelope * (note % 4 == 0 ? 0.20f : 0.125f);
                }
            }
            for (int chord = 0; chord < roots.Length; chord++)
            {
                int start = Mathf.RoundToInt(chord * beat * 8f * SampleRate);
                int count = Mathf.RoundToInt(beat * 9f * SampleRate);
                float root = Note(roots[chord]);
                for (int i = 0; i < count; i++)
                {
                    float t = i / (float)SampleRate;
                    float swell = Mathf.SmoothStep(0f, 1f, Mathf.Min(t, count / (float)SampleRate - t) * 1.65f);
                    float phase = Tau * t * root;
                    samples[(start + i) % samples.Length] += swell * (0.070f * Mathf.Sin(phase) +
                        0.023f * Mathf.Sin(phase * 2f) + 0.027f * Mathf.Sin(phase * 1.501f));
                }
            }
            return Clip("BOREATEK - Forja Boreal", samples);
        }

        private static float Note(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

        private static float Noise(ref uint state)
        {
            state = unchecked(state * 1664525u + 1013904223u);
            return (state >> 8) / 8388608f - 1f;
        }

        private static AudioClip Clip(string name, float[] samples)
        {
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
