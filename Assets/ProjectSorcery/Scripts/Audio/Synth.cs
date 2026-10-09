using System;

namespace ProjectSorcery
{
    /// <summary>Tiny DSP toolkit used to synthesize every sound effect and music track at runtime.</summary>
    public sealed class Synth
    {
        public readonly int Rate;
        public readonly float[] Buf;
        uint seed;

        public Synth(float seconds, int rate = 44100, uint seed = 1)
        {
            Rate = rate;
            Buf = new float[Math.Max(1, (int)(seconds * rate))];
            this.seed = seed == 0 ? 1u : seed;
        }

        public int Len => Buf.Length;
        public float Dur => Buf.Length / (float)Rate;

        // ---------------------------------------------------------- randomness
        public float Noise()
        {
            seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5;
            return (seed & 0xFFFFFF) / 8388608f - 1f;
        }
        public float Rand01() => (Noise() + 1f) * 0.5f;

        // ---------------------------------------------------------- envelopes
        public static float Exp(float t, float decay) => t < 0f ? 0f : (float)Math.Exp(-t / Math.Max(1e-4f, decay));
        public static float AD(float t, float attack, float decay)
        {
            if (t < 0f) return 0f;
            if (t < attack) return t / Math.Max(1e-4f, attack);
            return (float)Math.Exp(-(t - attack) / Math.Max(1e-4f, decay));
        }
        public static float Bell(float t, float start, float peak, float end)
        {
            if (t <= start || t >= end) return 0f;
            if (t < peak) return (t - start) / (peak - start);
            return 1f - (t - peak) / (end - peak);
        }

        // ---------------------------------------------------------- generators (add into buffer)
        /// <summary>Sine with exponential pitch glide from f0 to f1.</summary>
        public void Sweep(float start, float dur, float f0, float f1, float amp, float decay, float attack = 0.002f, float drive = 0f)
        {
            int s0 = (int)(start * Rate), n = (int)(dur * Rate);
            double ph = 0;
            for (int i = 0; i < n && s0 + i < Len; i++)
            {
                float t = i / (float)Rate;
                float k = n > 1 ? i / (float)(n - 1) : 0f;
                double f = f0 * Math.Pow(f1 / f0, k);
                ph += 2 * Math.PI * f / Rate;
                float v = (float)Math.Sin(ph) * AD(t, attack, decay) * amp;
                if (drive > 0f) v = (float)Math.Tanh(v * drive) / (float)Math.Tanh(drive);
                Buf[s0 + i] += v;
            }
        }

        /// <summary>Filtered noise burst. Cutoffs glide from lp0->lp1 and hp0->hp1.</summary>
        public void NoiseBurst(float start, float dur, float amp, float attack, float decay, float lp0, float lp1, float hp0 = 20f, float hp1 = 20f)
        {
            int s0 = (int)(start * Rate), n = (int)(dur * Rate);
            float lp = 0f, lp2 = 0f, hpLow = 0f;
            for (int i = 0; i < n && s0 + i < Len; i++)
            {
                float t = i / (float)Rate;
                float k = n > 1 ? i / (float)(n - 1) : 0f;
                float fl = lp0 + (lp1 - lp0) * k, fh = hp0 + (hp1 - hp0) * k;
                float a = Coef(fl), b = Coef(fh);
                float x = Noise();
                lp += a * (x - lp); lp2 += a * (lp - lp2);
                hpLow += b * (lp2 - hpLow);
                float y = lp2 - hpLow;
                Buf[s0 + i] += y * AD(t, attack, decay) * amp * 2.2f;
            }
        }

        /// <summary>Inharmonic metallic ring (bells, clanks, gongs).</summary>
        public void Ring(float start, float dur, float baseF, float[] ratios, float amp, float decay, float beat = 0.3f)
        {
            int s0 = (int)(start * Rate), n = (int)(dur * Rate);
            for (int p = 0; p < ratios.Length; p++)
            {
                double f = baseF * ratios[p];
                double f2 = f + beat * (p + 1);
                float pa = amp / (1f + p * 0.6f);
                float pd = decay / (1f + p * 0.35f);
                for (int i = 0; i < n && s0 + i < Len; i++)
                {
                    float t = i / (float)Rate;
                    float v = (float)(Math.Sin(2 * Math.PI * f * t) + 0.5 * Math.Sin(2 * Math.PI * f2 * t)) * 0.66f;
                    Buf[s0 + i] += v * AD(t, 0.001f, pd) * pa;
                }
            }
        }

        /// <summary>Band-limited-ish saw voice with a simple resonant lowpass (pads, bass, stabs).</summary>
        public void Saw(float start, float dur, float freq, float amp, float attack, float release, float cutoff, float detune = 0f, float res = 0.2f, float vibrato = 0f)
        {
            int s0 = (int)(start * Rate), n = (int)((dur + release) * Rate);
            double ph1 = 0, ph2 = 0.37;
            float lp = 0f, bp = 0f;
            for (int i = 0; i < n && s0 + i < Len; i++)
            {
                float t = i / (float)Rate;
                double f = freq * (1.0 + vibrato * Math.Sin(2 * Math.PI * 5.2 * t));
                ph1 += f / Rate; ph2 += f * (1.0 + detune) / Rate;
                float s = (float)((ph1 % 1.0) * 2.0 - 1.0 + (detune != 0f ? (ph2 % 1.0) * 2.0 - 1.0 : 0.0));
                // state variable lowpass
                float fc = (float)(2.0 * Math.Sin(Math.PI * Math.Min(cutoff, Rate * 0.2f) / Rate));
                lp += fc * bp;
                float hp = s - lp - res * bp;
                bp += fc * hp;
                float env = t < attack ? t / Math.Max(1e-4f, attack) : t < dur ? 1f : Math.Max(0f, 1f - (t - dur) / Math.Max(1e-4f, release));
                Buf[s0 + i] += lp * env * amp * 0.5f;
            }
        }

        /// <summary>Karplus-Strong plucked string (koto / shamisen flavour).</summary>
        public void Pluck(float start, float freq, float amp, float decay = 0.996f, float dur = 1.6f)
        {
            int s0 = (int)(start * Rate), n = (int)(dur * Rate);
            int period = Math.Max(2, (int)(Rate / freq));
            var line = new float[period];
            for (int i = 0; i < period; i++) line[i] = Noise();
            int idx = 0;
            float prev = 0f;
            for (int i = 0; i < n && s0 + i < Len; i++)
            {
                float cur = line[idx];
                float nv = (cur + prev) * 0.5f * decay;
                prev = cur;
                line[idx] = nv;
                idx = (idx + 1) % period;
                Buf[s0 + i] += cur * amp;
            }
        }

        /// <summary>Taiko / kick drum.</summary>
        public void Drum(float start, float f0, float f1, float amp, float decay, float click = 0.3f)
        {
            Sweep(start, decay * 6f, f0, f1, amp, decay, 0.001f, 1.5f);
            NoiseBurst(start, 0.03f, amp * click, 0.0005f, 0.008f, 4000f, 1500f);
        }

        public void Snare(float start, float amp)
        {
            NoiseBurst(start, 0.22f, amp, 0.001f, 0.06f, 7000f, 3000f, 800f, 1200f);
            Sweep(start, 0.12f, 240f, 160f, amp * 0.5f, 0.04f);
        }

        public void Hat(float start, float amp, float decay = 0.03f)
        {
            NoiseBurst(start, decay * 5f, amp, 0.0005f, decay, 16000f, 14000f, 7000f, 7000f);
        }

        /// <summary>Sparse random crackle impulses (electricity, fire).</summary>
        public void Crackle(float start, float dur, float density, float amp, float lp, float hp, float decayAll)
        {
            int s0 = (int)(start * Rate), n = (int)(dur * Rate);
            float l = 0f, h = 0f;
            float a = Coef(lp), b = Coef(hp);
            for (int i = 0; i < n && s0 + i < Len; i++)
            {
                float t = i / (float)Rate;
                float x = Rand01() < density / Rate ? Noise() * 4f : 0f;
                l += a * (x - l); h += b * (l - h);
                Buf[s0 + i] += (l - h) * amp * Exp(t, decayAll);
            }
        }

        /// <summary>Whisper-like formant noise with syllabic amplitude modulation.</summary>
        public void Whisper(float start, float dur, float amp, float syllableHz, float formant)
        {
            int s0 = (int)(start * Rate), n = (int)(dur * Rate);
            float l1 = 0f, h1 = 0f;
            for (int i = 0; i < n && s0 + i < Len; i++)
            {
                float t = i / (float)Rate;
                float form = formant * (1f + 0.35f * (float)Math.Sin(2 * Math.PI * 1.7 * t));
                float a = Coef(form * 1.8f), b = Coef(form * 0.6f);
                float x = Noise();
                l1 += a * (x - l1); h1 += b * (l1 - h1);
                float syl = 0.5f + 0.5f * (float)Math.Sin(2 * Math.PI * syllableHz * t + Math.Sin(t * 3.1) * 2.0);
                syl = syl * syl;
                Buf[s0 + i] += (l1 - h1) * syl * amp * Bell(t, 0f, dur * 0.15f, dur) * 2.5f;
            }
        }

        // ---------------------------------------------------------- processors
        public float Coef(float hz) => 1f - (float)Math.Exp(-2.0 * Math.PI * Math.Max(10f, hz) / Rate);

        public void Distort(float drive, float from = 0f, float to = 999f)
        {
            int a = (int)(from * Rate), b = Math.Min(Len, (int)(to * Rate));
            float norm = (float)Math.Tanh(drive);
            for (int i = Math.Max(0, a); i < b; i++) Buf[i] = (float)Math.Tanh(Buf[i] * drive) / norm;
        }

        public void LowpassAll(float hz)
        {
            float a = Coef(hz), y = 0f;
            for (int i = 0; i < Len; i++) { y += a * (Buf[i] - y); Buf[i] = y; }
        }

        /// <summary>Schroeder reverb: 4 parallel combs + 2 series allpasses. wet 0..1.</summary>
        public void Reverb(float wet, float size = 1f, float damp = 0.3f)
        {
            int[] combs = { 1557, 1617, 1491, 1422 };
            int[] alls = { 225, 556 };
            var outp = new float[Len];
            float scale = Rate / 44100f * size;
            for (int c = 0; c < combs.Length; c++)
            {
                int d = Math.Max(1, (int)(combs[c] * scale));
                var line = new float[d];
                int idx = 0; float filt = 0f;
                float fb = 0.84f;
                for (int i = 0; i < Len; i++)
                {
                    float o = line[idx];
                    filt = o * (1f - damp) + filt * damp;
                    line[idx] = Buf[i] + filt * fb;
                    idx = (idx + 1) % d;
                    outp[i] += o * 0.25f;
                }
            }
            for (int a = 0; a < alls.Length; a++)
            {
                int d = Math.Max(1, (int)(alls[a] * scale));
                var line = new float[d];
                int idx = 0;
                for (int i = 0; i < Len; i++)
                {
                    float bo = line[idx];
                    float x = outp[i];
                    float y = -x + bo;
                    line[idx] = x + bo * 0.5f;
                    idx = (idx + 1) % d;
                    outp[i] = y;
                }
            }
            for (int i = 0; i < Len; i++) Buf[i] = Buf[i] * (1f - wet * 0.5f) + outp[i] * wet;
        }

        public void FadeOut(float seconds)
        {
            int n = Math.Min(Len, (int)(seconds * Rate));
            for (int i = 0; i < n; i++) Buf[Len - 1 - i] *= i / (float)n;
        }

        public void FadeIn(float seconds)
        {
            int n = Math.Min(Len, (int)(seconds * Rate));
            for (int i = 0; i < n; i++) Buf[i] *= i / (float)n;
        }

        public void Normalize(float peak = 0.9f)
        {
            float m = 0f;
            for (int i = 0; i < Len; i++) { float a = Math.Abs(Buf[i]); if (a > m) m = a; }
            if (m < 1e-6f) return;
            float k = peak / m;
            for (int i = 0; i < Len; i++) Buf[i] *= k;
        }

        /// <summary>Copies a rendered loop tail back to the start so music loops seamlessly.</summary>
        public float[] FoldLoop(int loopSamples)
        {
            var o = new float[loopSamples];
            for (int i = 0; i < Len; i++) o[i % loopSamples] += Buf[i];
            return o;
        }

        public static float Midi(int note) => 440f * (float)Math.Pow(2.0, (note - 69) / 12.0);
    }
}
