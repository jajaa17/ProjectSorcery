namespace ProjectSorcery
{
    public enum Track { None, Menu, Battle, Domain, Raid }

    /// <summary>Procedurally composed, seamlessly looping music. Runs on a worker thread at boot.</summary>
    public static class MusicGen
    {
        public const int Rate = 22050;

        public static float[] Make(Track t)
        {
            switch (t)
            {
                case Track.Menu: return Menu();
                case Track.Battle: return Battle(150f, false);
                case Track.Raid: return Battle(168f, true);
                case Track.Domain: return Domain();
                default: return new float[Rate];
            }
        }

        static int N(int note) => note;

        static float[] Battle(float bpm, bool raid)
        {
            float beat = 60f / bpm, bar = beat * 4f;
            int bars = 8;
            float loop = bar * bars;
            var s = new Synth(loop + 2.5f, Rate, raid ? 777u : 555u);
            // D minor / D harmonic minor progressions (root midi notes)
            int[] roots = raid ? new[] { 38, 38, 34, 34, 31, 31, 33, 33 } : new[] { 38, 38, 34, 36, 38, 38, 31, 33 };
            bool[] minor = raid ? new[] { true, true, false, false, true, true, false, false } : new[] { true, true, false, false, true, true, true, false };
            int[] penta = { 62, 65, 67, 69, 72, 74, 77, 79 };

            for (int b = 0; b < bars; b++)
            {
                float t0 = b * bar;
                int root = roots[b];
                // drums
                for (int q = 0; q < 8; q++)
                {
                    float tt = t0 + q * beat * 0.5f;
                    bool kick = q == 0 || q == 3 || q == 4 || (raid && (q == 6 || q == 7));
                    if (kick) s.Drum(tt, 150f, 45f, raid ? 1.0f : 0.85f, 0.07f, 0.35f);
                    if (q == 2 || q == 6) s.Snare(tt, raid ? 0.55f : 0.45f);
                    s.Hat(tt, q % 2 == 0 ? 0.16f : 0.1f);
                    if (raid) s.Hat(tt + beat * 0.25f, 0.06f, 0.02f);
                }
                if (b == bars - 1)
                    for (int k = 0; k < 6; k++) s.Drum(t0 + bar * 0.5f + k * beat * 0.25f, 110f, 60f, 0.5f + k * 0.08f, 0.09f, 0.2f);
                if (b % 4 == 0) s.Drum(t0, 70f, 38f, 0.9f, 0.18f, 0.1f); // taiko accent

                // bass: driving eighths with octave jumps
                for (int q = 0; q < 8; q++)
                {
                    int note = root - 12 + ((q == 3 || q == 7) ? 12 : 0);
                    s.Saw(t0 + q * beat * 0.5f, beat * 0.4f, Synth.Midi(note), raid ? 0.5f : 0.42f, 0.004f, 0.05f, raid ? 900f : 650f, 0.01f, 0.5f);
                }
                // chord stabs on the off-beats
                int third = minor[b] ? 3 : 4;
                float[] chord = { Synth.Midi(root + 12), Synth.Midi(root + 12 + third), Synth.Midi(root + 19) };
                foreach (var f in chord)
                {
                    s.Saw(t0 + beat * 1.5f, beat * 0.3f, f, 0.12f, 0.005f, 0.12f, 2600f, 0.007f, 0.3f);
                    s.Saw(t0 + beat * 3.5f, beat * 0.3f, f, 0.1f, 0.005f, 0.12f, 2600f, 0.007f, 0.3f);
                }
                // koto ostinato on odd bars
                if (b % 2 == 1 || raid)
                    for (int k = 0; k < 8; k++)
                    {
                        int pn = penta[(k * 3 + b) % penta.Length];
                        s.Pluck(t0 + k * beat * 0.5f, Synth.Midi(pn), 0.18f, 0.994f, 0.7f);
                    }
            }
            s.Reverb(0.18f, 1.1f);
            var outp = s.FoldLoop((int)(loop * Rate));
            Normalize(outp, 0.85f);
            return outp;
        }

        static float[] Menu()
        {
            float bpm = 72f, beat = 60f / bpm, bar = beat * 4f;
            int bars = 8;
            float loop = bar * bars;
            var s = new Synth(loop + 3f, Rate, 321u);
            int[] roots = { 50, 50, 46, 46, 41, 41, 48, 45 };
            bool[] minor = { true, true, false, false, false, false, false, false };
            int[] penta = { 62, 65, 67, 69, 72, 74, 77, 81 };
            for (int b = 0; b < bars; b++)
            {
                float t0 = b * bar;
                int r = roots[b];
                int third = minor[b] ? 3 : 4;
                if (b % 2 == 0)
                {
                    float[] chord = { Synth.Midi(r - 12), Synth.Midi(r), Synth.Midi(r + third), Synth.Midi(r + 7) };
                    foreach (var f in chord) s.Saw(t0, bar * 2f - 0.2f, f, 0.12f, 0.8f, 1.2f, 900f, 0.008f, 0.2f, 0.003f);
                }
                if (b == 0 || b == 4) { s.Drum(t0, 70f, 40f, 0.8f, 0.25f, 0.1f); s.Drum(t0 + beat * 2.5f, 80f, 42f, 0.5f, 0.2f, 0.1f); }
                for (int k = 0; k < 6; k++)
                {
                    if (((k + b) % 3) == 2) continue;
                    s.Pluck(t0 + k * beat * 0.66f, Synth.Midi(penta[(k * 2 + b * 3) % penta.Length]), 0.16f, 0.996f, 1.4f);
                }
                if (b == 3 || b == 7) s.Ring(t0 + beat * 2f, 2.5f, Synth.Midi(86), new[] { 1f, 2.76f, 5.4f }, 0.08f, 0.9f, 0.3f);
                s.Whisper(t0, bar, 0.04f, 0.6f, 900f);
            }
            s.Reverb(0.45f, 1.6f, 0.25f);
            var outp = s.FoldLoop((int)(loop * Rate));
            Normalize(outp, 0.75f);
            return outp;
        }

        static float[] Domain()
        {
            float bpm = 60f, beat = 1f, bar = 4f * beat;
            int bars = 8;
            float loop = bar * bars;
            var s = new Synth(loop + 3f, Rate, 999u);
            for (int b = 0; b < bars; b++)
            {
                float t0 = b * bar;
                // heartbeat
                s.Drum(t0, 70f, 38f, 0.8f, 0.12f, 0.05f);
                s.Drum(t0 + 0.28f, 65f, 36f, 0.55f, 0.12f, 0.05f);
                s.Drum(t0 + 2f, 70f, 38f, 0.7f, 0.12f, 0.05f);
                s.Drum(t0 + 2.28f, 65f, 36f, 0.5f, 0.12f, 0.05f);
                if (b % 2 == 0)
                {
                    s.Saw(t0, bar * 2f, Synth.Midi(26), 0.25f, 1.5f, 1.5f, 260f, 0.01f, 0.4f);
                    s.Saw(t0, bar * 2f, Synth.Midi(33), 0.18f, 1.5f, 1.5f, 320f, 0.01f, 0.4f);
                    // choir-like formant pad
                    s.Saw(t0, bar * 2f, Synth.Midi(62), 0.07f, 1.2f, 1.5f, 1100f, 0.012f, 0.9f, 0.004f);
                    s.Saw(t0, bar * 2f, Synth.Midi(65 + (b % 4 == 2 ? -1 : 0)), 0.06f, 1.2f, 1.5f, 1100f, 0.012f, 0.9f, 0.004f);
                    s.Ring(t0 + bar, 3f, Synth.Midi(74), new[] { 1f, 2.76f, 5.4f, 8.9f }, 0.1f, 1.2f, 0.6f);
                }
                s.Whisper(t0 + 1f, 2f, 0.05f, 3f, 1300f);
            }
            s.Reverb(0.5f, 1.8f, 0.2f);
            var outp = s.FoldLoop((int)(loop * Rate));
            Normalize(outp, 0.8f);
            return outp;
        }

        static void Normalize(float[] a, float peak)
        {
            float m = 0f;
            for (int i = 0; i < a.Length; i++) { float v = a[i] < 0 ? -a[i] : a[i]; if (v > m) m = v; }
            if (m < 1e-6f) return;
            float k = peak / m;
            for (int i = 0; i < a.Length; i++) a[i] *= k;
        }
    }
}
