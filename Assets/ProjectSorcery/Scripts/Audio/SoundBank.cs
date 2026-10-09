using System;

namespace ProjectSorcery
{
    public enum Sfx
    {
        None, Punch, PunchHeavy, Kick, Slash, Whoosh, WhooshLight, Dash, Jump, Land, Block, GuardBreak, Hurt, KO,
        BlackFlash, Shoot, ImpactEnergy, Explosion, Beam, Fire, Water, Electric, Ice, Blood, Crow, Insect, Nail,
        Clap, Gavel, Jackpot, Chant, DomainOpenBright, DomainOpenDark, DomainOpenDeep, DomainShatter, ClashStart, ClashHit,
        Heal, Summon, Teleport, Gong, Bell, UiMove, UiConfirm, UiBack, Countdown, Voice, Charge, Rumble, Infinity, Gun, Wind,
        Count
    }

    /// <summary>Recipes for every synthesized sound effect.</summary>
    public static class SoundBank
    {
        public const int Rate = 44100;

        public static float[] Make(Sfx id)
        {
            switch (id)
            {
                case Sfx.Punch: return Punch(false);
                case Sfx.PunchHeavy: return Punch(true);
                case Sfx.Kick: return Kick();
                case Sfx.Slash: return Slash();
                case Sfx.Whoosh: return Whoosh(0.26f, 300f, 2200f, 0.8f);
                case Sfx.WhooshLight: return Whoosh(0.14f, 700f, 3500f, 0.6f);
                case Sfx.Dash: return Dash();
                case Sfx.Jump: return Whoosh(0.18f, 500f, 1800f, 0.45f);
                case Sfx.Land: return Land();
                case Sfx.Block: return Block();
                case Sfx.GuardBreak: return GuardBreak();
                case Sfx.Hurt: return Hurt();
                case Sfx.KO: return Ko();
                case Sfx.BlackFlash: return BlackFlash();
                case Sfx.Shoot: return Shoot();
                case Sfx.ImpactEnergy: return Zap();
                case Sfx.Explosion: return Explosion();
                case Sfx.Beam: return BeamS();
                case Sfx.Fire: return Fire();
                case Sfx.Water: return Water();
                case Sfx.Electric: return Electric();
                case Sfx.Ice: return Ice();
                case Sfx.Blood: return Blood();
                case Sfx.Crow: return Crow();
                case Sfx.Insect: return Insect();
                case Sfx.Nail: return Nail();
                case Sfx.Clap: return Clap();
                case Sfx.Gavel: return Gavel();
                case Sfx.Jackpot: return Jackpot();
                case Sfx.Chant: return Chant();
                case Sfx.DomainOpenBright: return DomainOpen(0);
                case Sfx.DomainOpenDark: return DomainOpen(1);
                case Sfx.DomainOpenDeep: return DomainOpen(2);
                case Sfx.DomainShatter: return Shatter();
                case Sfx.ClashStart: return ClashStart();
                case Sfx.ClashHit: return ClashHit();
                case Sfx.Heal: return Heal();
                case Sfx.Summon: return Summon();
                case Sfx.Teleport: return Teleport();
                case Sfx.Gong: return Gong(98f, 3.2f);
                case Sfx.Bell: return Gong(523f, 1.6f);
                case Sfx.UiMove: return Ui(1250f, 1250f, 0.04f, 0.25f);
                case Sfx.UiConfirm: return UiConfirm();
                case Sfx.UiBack: return Ui(620f, 380f, 0.09f, 0.35f);
                case Sfx.Countdown: return Ui(900f, 900f, 0.08f, 0.4f);
                case Sfx.Voice: return Voice();
                case Sfx.Charge: return Charge();
                case Sfx.Rumble: return Rumble();
                case Sfx.Infinity: return InfinityS();
                case Sfx.Gun: return Gun();
                case Sfx.Wind: return WindS();
                default: return new float[64];
            }
        }

        static float[] Done(Synth s, float peak = 0.9f) { s.Normalize(peak); return s.Buf; }

        static float[] Punch(bool heavy)
        {
            var s = new Synth(heavy ? 0.7f : 0.25f, Rate, heavy ? 11u : 7u);
            if (heavy)
            {
                s.Sweep(0f, 0.6f, 110f, 34f, 1f, 0.16f, 0.001f, 3f);
                s.NoiseBurst(0f, 0.12f, 0.9f, 0.0005f, 0.035f, 5000f, 900f);
                s.NoiseBurst(0f, 0.012f, 0.8f, 0.0002f, 0.003f, 12000f, 12000f, 2000f, 2000f);
                s.Crackle(0.01f, 0.25f, 900f, 0.25f, 6000f, 1500f, 0.06f);
                s.Reverb(0.18f, 0.6f);
            }
            else
            {
                s.Sweep(0f, 0.2f, 160f, 60f, 0.9f, 0.05f, 0.001f, 2f);
                s.NoiseBurst(0f, 0.07f, 0.8f, 0.0005f, 0.02f, 3500f, 1200f);
                s.NoiseBurst(0f, 0.008f, 0.6f, 0.0002f, 0.002f, 10000f, 10000f, 2500f, 2500f);
            }
            return Done(s);
        }

        static float[] Kick()
        {
            var s = new Synth(0.35f, Rate, 3);
            s.NoiseBurst(0f, 0.2f, 0.5f, 0.02f, 0.06f, 1800f, 600f, 200f, 200f);
            s.Sweep(0.04f, 0.25f, 130f, 50f, 0.9f, 0.07f, 0.001f, 2f);
            return Done(s);
        }

        static float[] Slash()
        {
            var s = new Synth(0.45f, Rate, 21);
            s.NoiseBurst(0f, 0.18f, 0.8f, 0.004f, 0.05f, 12000f, 4000f, 2500f, 1800f);
            s.Ring(0.005f, 0.4f, 2100f, new[] { 1f, 1.58f, 2.37f }, 0.35f, 0.09f, 7f);
            s.Sweep(0f, 0.08f, 4000f, 900f, 0.25f, 0.02f);
            return Done(s, 0.8f);
        }

        static float[] Whoosh(float dur, float f0, float f1, float amp)
        {
            var s = new Synth(dur + 0.05f, Rate, 5);
            s.NoiseBurst(0f, dur, amp, dur * 0.45f, dur * 0.25f, f0, f1, f0 * 0.5f, f1 * 0.4f);
            return Done(s, 0.7f);
        }

        static float[] Dash()
        {
            var s = new Synth(0.3f, Rate, 9);
            s.NoiseBurst(0f, 0.25f, 0.8f, 0.02f, 0.08f, 6000f, 1500f, 900f, 300f);
            s.Sweep(0f, 0.15f, 900f, 300f, 0.15f, 0.05f);
            return Done(s, 0.7f);
        }

        static float[] Land()
        {
            var s = new Synth(0.25f, Rate, 4);
            s.Sweep(0f, 0.2f, 90f, 45f, 0.8f, 0.05f, 0.001f, 1.5f);
            s.NoiseBurst(0f, 0.12f, 0.4f, 0.002f, 0.03f, 1200f, 400f);
            return Done(s, 0.6f);
        }

        static float[] Block()
        {
            var s = new Synth(0.4f, Rate, 6);
            s.Ring(0f, 0.35f, 820f, new[] { 1f, 1.74f, 2.69f, 3.9f }, 0.6f, 0.08f, 5f);
            s.NoiseBurst(0f, 0.04f, 0.7f, 0.0005f, 0.01f, 8000f, 4000f, 1500f, 1500f);
            return Done(s, 0.75f);
        }

        static float[] GuardBreak()
        {
            var s = new Synth(0.9f, Rate, 8);
            s.NoiseBurst(0f, 0.5f, 0.9f, 0.001f, 0.12f, 9000f, 1500f, 1000f, 200f);
            for (int i = 0; i < 9; i++) s.Ring(0.01f + i * 0.025f, 0.4f, 1800f + i * 430f, new[] { 1f, 2.3f }, 0.2f, 0.06f);
            s.Sweep(0f, 0.5f, 140f, 40f, 0.7f, 0.15f, 0.001f, 2f);
            s.Reverb(0.25f);
            return Done(s);
        }

        static float[] Hurt()
        {
            var s = new Synth(0.22f, Rate, 12);
            s.Saw(0f, 0.12f, 170f, 0.6f, 0.005f, 0.06f, 900f, 0.01f, 0.6f);
            s.NoiseBurst(0f, 0.1f, 0.2f, 0.005f, 0.03f, 2500f, 800f, 300f, 300f);
            return Done(s, 0.5f);
        }

        static float[] Ko()
        {
            var s = new Synth(2.6f, Rate, 13);
            s.Sweep(0f, 2f, 80f, 26f, 1f, 0.6f, 0.001f, 3f);
            s.NoiseBurst(0f, 0.5f, 0.9f, 0.0005f, 0.12f, 8000f, 400f);
            s.Ring(0.02f, 2.4f, 196f, new[] { 1f, 2.76f, 5.4f }, 0.3f, 0.9f, 0.6f);
            s.Reverb(0.45f, 1.4f);
            s.FadeOut(0.3f);
            return Done(s);
        }

        /// <summary>The signature sound: a crack of distorted space, a sub-bass drop and a cursed metallic ring.</summary>
        static float[] BlackFlash()
        {
            var s = new Synth(3.0f, Rate, 99);
            // 1) instant transient crack
            s.NoiseBurst(0f, 0.02f, 1.4f, 0.0001f, 0.004f, 16000f, 16000f, 3000f, 3000f);
            // 2) massive distorted sub drop
            s.Sweep(0f, 0.9f, 75f, 24f, 1.4f, 0.32f, 0.0005f, 5f);
            // 3) body: distorted noise boom with a closing filter
            s.NoiseBurst(0.002f, 0.8f, 1.0f, 0.001f, 0.18f, 7000f, 250f, 40f, 40f);
            // 4) black lightning crackle that thins out
            s.Crackle(0f, 0.6f, 2600f, 0.6f, 7000f, 2500f, 0.16f);
            s.Crackle(0.08f, 0.9f, 500f, 0.35f, 5000f, 1500f, 0.35f);
            // 5) cursed metallic ring with beating partials
            s.Ring(0.015f, 2.9f, 233f, new[] { 1f, 2.76f, 5.40f, 8.93f }, 0.42f, 1.15f, 1.1f);
            // 6) glassy high shimmer
            s.NoiseBurst(0.01f, 1.5f, 0.25f, 0.01f, 0.45f, 14000f, 9000f, 7000f, 6000f);
            s.Distort(2.2f, 0f, 0.25f);
            s.Reverb(0.5f, 1.6f, 0.25f);
            s.FadeOut(0.5f);
            return Done(s, 0.98f);
        }

        static float[] Shoot()
        {
            var s = new Synth(0.35f, Rate, 14);
            s.Sweep(0f, 0.3f, 260f, 980f, 0.5f, 0.12f, 0.01f);
            s.NoiseBurst(0f, 0.3f, 0.5f, 0.03f, 0.08f, 5000f, 2500f, 800f, 1200f);
            return Done(s, 0.6f);
        }

        static float[] Zap()
        {
            var s = new Synth(0.35f, Rate, 15);
            s.Sweep(0f, 0.3f, 720f, 120f, 0.7f, 0.08f, 0.001f, 1.5f);
            s.NoiseBurst(0f, 0.15f, 0.6f, 0.001f, 0.04f, 6000f, 1500f, 500f, 300f);
            s.Crackle(0f, 0.2f, 1500f, 0.3f, 6000f, 2000f, 0.06f);
            return Done(s, 0.75f);
        }

        static float[] Explosion()
        {
            var s = new Synth(1.6f, Rate, 16);
            s.NoiseBurst(0f, 1.3f, 1f, 0.002f, 0.3f, 3000f, 150f);
            s.Sweep(0f, 0.9f, 70f, 28f, 1f, 0.3f, 0.001f, 3f);
            s.Crackle(0.05f, 1.0f, 300f, 0.3f, 4000f, 800f, 0.4f);
            s.Reverb(0.3f, 1.2f);
            s.FadeOut(0.2f);
            return Done(s);
        }

        static float[] BeamS()
        {
            var s = new Synth(1.3f, Rate, 17);
            s.Saw(0f, 1.0f, 110f, 0.6f, 0.02f, 0.25f, 2200f, 0.012f, 0.4f, 0.01f);
            s.Saw(0f, 1.0f, 165f, 0.4f, 0.02f, 0.25f, 3000f, 0.008f, 0.3f);
            s.NoiseBurst(0f, 1.2f, 0.5f, 0.02f, 0.6f, 9000f, 4000f, 1500f, 1500f);
            s.Sweep(0f, 0.2f, 1500f, 300f, 0.5f, 0.05f);
            s.Reverb(0.2f);
            s.FadeOut(0.15f);
            return Done(s, 0.85f);
        }

        static float[] Fire()
        {
            var s = new Synth(0.8f, Rate, 18);
            s.NoiseBurst(0f, 0.7f, 0.8f, 0.03f, 0.25f, 2500f, 700f, 150f, 150f);
            s.Crackle(0f, 0.7f, 1200f, 0.5f, 6000f, 1500f, 0.3f);
            s.Sweep(0f, 0.4f, 120f, 60f, 0.4f, 0.15f);
            return Done(s, 0.8f);
        }

        static float[] Water()
        {
            var s = new Synth(0.8f, Rate, 19);
            s.NoiseBurst(0f, 0.6f, 0.7f, 0.01f, 0.2f, 2200f, 500f, 120f, 120f);
            for (int i = 0; i < 12; i++) s.Sweep(0.02f + i * 0.04f + s.Rand01() * 0.02f, 0.06f, 300f + s.Rand01() * 400f, 900f + s.Rand01() * 800f, 0.12f, 0.02f);
            return Done(s, 0.75f);
        }

        static float[] Electric()
        {
            var s = new Synth(0.6f, Rate, 20);
            s.Crackle(0f, 0.55f, 4000f, 0.8f, 9000f, 2500f, 0.2f);
            s.Saw(0f, 0.4f, 58f, 0.35f, 0.005f, 0.1f, 2500f, 0.03f, 0.7f);
            s.Sweep(0f, 0.1f, 2600f, 400f, 0.3f, 0.03f);
            return Done(s, 0.8f);
        }

        static float[] Ice()
        {
            var s = new Synth(0.8f, Rate, 22);
            for (int i = 0; i < 10; i++) s.Ring(s.Rand01() * 0.25f, 0.5f, 2800f + s.Rand01() * 3000f, new[] { 1f, 2.1f }, 0.15f, 0.12f);
            s.NoiseBurst(0f, 0.15f, 0.6f, 0.001f, 0.03f, 9000f, 3000f, 2000f, 2000f);
            s.Reverb(0.25f);
            return Done(s, 0.75f);
        }

        static float[] Blood()
        {
            var s = new Synth(0.5f, Rate, 23);
            s.NoiseBurst(0f, 0.35f, 0.7f, 0.005f, 0.1f, 900f, 300f, 60f, 60f);
            s.Sweep(0f, 0.3f, 300f, 90f, 0.4f, 0.08f, 0.002f, 1.5f);
            s.Sweep(0.03f, 0.25f, 3000f, 700f, 0.15f, 0.04f);
            return Done(s, 0.75f);
        }

        static float[] Crow()
        {
            var s = new Synth(0.6f, Rate, 24);
            s.Saw(0f, 0.16f, 820f, 0.6f, 0.01f, 0.05f, 2200f, 0.02f, 0.85f, 0.04f);
            s.Saw(0.24f, 0.14f, 760f, 0.5f, 0.01f, 0.05f, 2000f, 0.02f, 0.85f, 0.04f);
            s.NoiseBurst(0f, 0.4f, 0.15f, 0.01f, 0.1f, 4000f, 2000f, 1000f, 1000f);
            return Done(s, 0.65f);
        }

        static float[] Insect()
        {
            var s = new Synth(0.6f, Rate, 25);
            int n = s.Len;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)s.Rate;
                float am = 0.5f + 0.5f * (float)Math.Sin(2 * Math.PI * 38 * t);
                float saw = (float)((t * 230.0) % 1.0) * 2f - 1f;
                s.Buf[i] += saw * am * Synth.Bell(t, 0f, 0.1f, 0.6f) * 0.5f;
            }
            s.LowpassAll(3000f);
            return Done(s, 0.55f);
        }

        static float[] Nail()
        {
            var s = new Synth(0.5f, Rate, 26);
            s.Ring(0f, 0.45f, 1820f, new[] { 1f, 1.43f, 2.62f }, 0.6f, 0.11f, 3f);
            s.Sweep(0f, 0.12f, 160f, 80f, 0.6f, 0.03f);
            s.NoiseBurst(0f, 0.03f, 0.7f, 0.0005f, 0.006f, 9000f, 6000f, 2000f, 2000f);
            return Done(s, 0.8f);
        }

        static float[] Clap()
        {
            var s = new Synth(0.7f, Rate, 27);
            for (int i = 0; i < 3; i++) s.NoiseBurst(i * 0.008f, 0.05f, 0.9f, 0.0005f, 0.008f, 4500f, 2500f, 900f, 900f);
            s.NoiseBurst(0.024f, 0.25f, 0.6f, 0.001f, 0.06f, 3800f, 1800f, 800f, 800f);
            s.Reverb(0.35f, 1.1f);
            return Done(s, 0.85f);
        }

        static float[] Gavel()
        {
            var s = new Synth(0.9f, Rate, 28);
            s.Sweep(0f, 0.25f, 210f, 160f, 0.8f, 0.06f, 0.001f, 1.3f);
            s.Ring(0f, 0.25f, 540f, new[] { 1f, 2.4f }, 0.4f, 0.05f);
            s.NoiseBurst(0f, 0.04f, 0.6f, 0.0005f, 0.008f, 5000f, 2000f, 400f, 400f);
            s.Reverb(0.45f, 1.5f);
            return Done(s, 0.85f);
        }

        static float[] Jackpot()
        {
            var s = new Synth(1.8f, Rate, 29);
            int[] notes = { 72, 76, 79, 84, 88, 91, 96 };
            for (int i = 0; i < notes.Length; i++)
            {
                float f = Synth.Midi(notes[i]);
                s.Ring(i * 0.08f, 0.9f, f, new[] { 1f, 2f, 3f }, 0.35f, 0.25f, 0.5f);
            }
            for (int i = 0; i < 14; i++) s.Ring(0.5f + i * 0.06f, 0.4f, 2400f + (i % 3) * 600f, new[] { 1f, 2.7f }, 0.12f, 0.08f);
            s.Reverb(0.25f);
            return Done(s, 0.8f);
        }

        static float[] Chant()
        {
            var s = new Synth(1.6f, Rate, 30);
            s.Whisper(0f, 1.4f, 0.8f, 5.5f, 1400f);
            s.Saw(0f, 1.2f, 55f, 0.35f, 0.3f, 0.3f, 300f, 0.01f, 0.5f);
            s.Saw(0f, 1.2f, 82.4f, 0.2f, 0.3f, 0.3f, 400f, 0.01f, 0.5f);
            s.Reverb(0.55f, 1.6f);
            s.FadeOut(0.2f);
            return Done(s, 0.75f);
        }

        /// <summary>Domain expansion: rising swell into a deep boom and a spreading chord.</summary>
        static float[] DomainOpen(int flavor)
        {
            var s = new Synth(4.2f, Rate, (uint)(31 + flavor));
            float root = flavor == 0 ? 146.8f : flavor == 1 ? 69.3f : 55f;
            float[] chord = flavor == 0 ? new[] { 1f, 1.26f, 1.5f, 1.89f, 2.52f }
                          : flavor == 1 ? new[] { 1f, 1.19f, 1.41f, 2f }
                          : new[] { 1f, 1.5f, 2f, 2.38f };
            // inhale swell
            s.NoiseBurst(0f, 1.0f, 0.6f, 0.85f, 0.08f, 400f, 6000f, 100f, 1500f);
            s.Sweep(0f, 1.0f, root * 0.5f, root, 0.35f, 2f, 0.9f);
            // the hand sign clap
            s.NoiseBurst(0.95f, 0.05f, 0.8f, 0.0005f, 0.01f, 6000f, 3000f, 900f, 900f);
            // BOOM
            s.Sweep(1.0f, 2f, 90f, 22f, 1.3f, 0.7f, 0.001f, 4f);
            s.NoiseBurst(1.0f, 1.2f, 0.8f, 0.001f, 0.35f, 4000f, 120f);
            // spreading chord pad
            for (int i = 0; i < chord.Length; i++) s.Saw(1.05f, 2.4f, root * chord[i], 0.22f, 0.4f, 0.8f, 1800f + i * 300f, 0.006f, 0.3f);
            s.Ring(1.02f, 3.0f, root * 4f, new[] { 1f, 2.76f, 5.4f }, 0.18f, 1.4f, 0.4f);
            s.Reverb(0.6f, 1.8f, 0.2f);
            s.FadeOut(0.4f);
            return Done(s, 0.95f);
        }

        static float[] Shatter()
        {
            var s = new Synth(1.4f, Rate, 34);
            s.NoiseBurst(0f, 0.6f, 0.8f, 0.001f, 0.15f, 12000f, 3000f, 2500f, 1500f);
            for (int i = 0; i < 26; i++) s.Ring(s.Rand01() * 0.5f, 0.5f, 2000f + s.Rand01() * 6000f, new[] { 1f, 2.3f }, 0.1f, 0.1f + s.Rand01() * 0.1f);
            s.Sweep(0f, 0.6f, 100f, 40f, 0.6f, 0.2f, 0.001f, 2f);
            s.Reverb(0.4f, 1.3f);
            return Done(s, 0.85f);
        }

        static float[] ClashStart()
        {
            var s = new Synth(2.4f, Rate, 35);
            s.Sweep(0f, 1.2f, 85f, 30f, 1.1f, 0.4f, 0.001f, 3f);
            s.Sweep(0.18f, 1.2f, 95f, 28f, 1f, 0.4f, 0.001f, 3f);
            s.NoiseBurst(0f, 0.6f, 0.9f, 0.001f, 0.15f, 6000f, 300f);
            s.NoiseBurst(0.18f, 0.6f, 0.8f, 0.001f, 0.15f, 6000f, 300f);
            s.Crackle(0f, 2f, 800f, 0.4f, 7000f, 2000f, 0.8f);
            s.NoiseBurst(0.4f, 1.8f, 0.4f, 1.2f, 0.2f, 500f, 8000f, 200f, 3000f);
            s.Reverb(0.4f, 1.5f);
            s.FadeOut(0.3f);
            return Done(s);
        }

        static float[] ClashHit()
        {
            var s = new Synth(0.35f, Rate, 36);
            s.Crackle(0f, 0.3f, 3000f, 0.7f, 8000f, 2500f, 0.08f);
            s.Sweep(0f, 0.25f, 120f, 50f, 0.6f, 0.06f, 0.001f, 2f);
            return Done(s, 0.7f);
        }

        static float[] Heal()
        {
            var s = new Synth(0.9f, Rate, 37);
            int[] notes = { 79, 83, 86, 91 };
            for (int i = 0; i < notes.Length; i++) s.Ring(i * 0.07f, 0.7f, Synth.Midi(notes[i]), new[] { 1f, 2f }, 0.25f, 0.25f, 0.4f);
            s.NoiseBurst(0f, 0.8f, 0.1f, 0.2f, 0.3f, 12000f, 8000f, 6000f, 6000f);
            s.Reverb(0.4f);
            return Done(s, 0.55f);
        }

        static float[] Summon()
        {
            var s = new Synth(0.9f, Rate, 38);
            s.NoiseBurst(0f, 0.5f, 0.6f, 0.4f, 0.05f, 300f, 2500f, 80f, 400f);
            s.Sweep(0.45f, 0.4f, 70f, 35f, 0.9f, 0.12f, 0.001f, 2.5f);
            s.Reverb(0.35f, 1.2f);
            return Done(s, 0.8f);
        }

        static float[] Teleport()
        {
            var s = new Synth(0.25f, Rate, 39);
            s.Sweep(0f, 0.15f, 2400f, 180f, 0.5f, 0.05f, 0.001f);
            s.NoiseBurst(0f, 0.15f, 0.4f, 0.001f, 0.04f, 9000f, 2000f, 1500f, 500f);
            return Done(s, 0.6f);
        }

        static float[] Gong(float f, float dur)
        {
            var s = new Synth(dur, Rate, 40);
            s.Ring(0f, dur, f, new[] { 1f, 1.48f, 2.31f, 3.17f, 4.1f }, 0.7f, dur * 0.4f, 0.7f);
            s.Sweep(0f, 0.3f, f * 0.5f, f * 0.45f, 0.4f, 0.12f);
            s.NoiseBurst(0f, 0.02f, 0.4f, 0.0005f, 0.004f, 5000f, 3000f);
            s.Reverb(0.3f, 1.4f);
            s.FadeOut(0.3f);
            return Done(s, 0.75f);
        }

        static float[] Ui(float f0, float f1, float dur, float amp)
        {
            var s = new Synth(dur + 0.05f, Rate, 41);
            s.Sweep(0f, dur, f0, f1, amp, dur * 0.4f, 0.001f);
            return Done(s, 0.4f);
        }

        static float[] UiConfirm()
        {
            var s = new Synth(0.35f, Rate, 42);
            s.Ring(0f, 0.25f, 880f, new[] { 1f, 2f }, 0.4f, 0.07f);
            s.Ring(0.06f, 0.25f, 1318f, new[] { 1f, 2f }, 0.4f, 0.09f);
            s.NoiseBurst(0f, 0.2f, 0.2f, 0.01f, 0.06f, 8000f, 4000f, 2000f, 2000f);
            return Done(s, 0.45f);
        }

        static float[] Voice()
        {
            var s = new Synth(1.0f, Rate, 43);
            s.Saw(0f, 0.45f, 92f, 1f, 0.01f, 0.25f, 650f, 0.015f, 0.85f, 0.02f);
            s.Saw(0f, 0.45f, 138f, 0.5f, 0.01f, 0.25f, 1100f, 0.015f, 0.85f);
            s.Distort(3f);
            s.Sweep(0f, 0.6f, 70f, 35f, 0.7f, 0.2f, 0.001f, 2f);
            s.Reverb(0.5f, 1.5f);
            return Done(s, 0.9f);
        }

        static float[] Charge()
        {
            var s = new Synth(0.6f, Rate, 44);
            s.Sweep(0f, 0.55f, 110f, 440f, 0.4f, 1f, 0.3f);
            s.NoiseBurst(0f, 0.55f, 0.4f, 0.45f, 0.05f, 1500f, 7000f, 300f, 2000f);
            return Done(s, 0.5f);
        }

        static float[] Rumble()
        {
            var s = new Synth(2.4f, Rate, 45);
            s.NoiseBurst(0f, 2.3f, 1f, 0.6f, 0.9f, 180f, 120f, 25f, 25f);
            s.Sweep(0f, 2.2f, 45f, 38f, 0.6f, 1.4f, 0.4f, 1.5f);
            s.FadeOut(0.4f);
            return Done(s, 0.85f);
        }

        static float[] InfinityS()
        {
            var s = new Synth(0.4f, Rate, 46);
            s.Sweep(0f, 0.35f, 520f, 260f, 0.4f, 0.12f, 0.02f);
            s.Sweep(0f, 0.35f, 780f, 390f, 0.2f, 0.1f, 0.02f);
            s.Reverb(0.4f);
            return Done(s, 0.4f);
        }

        static float[] Gun()
        {
            var s = new Synth(0.9f, Rate, 47);
            s.NoiseBurst(0f, 0.2f, 1f, 0.0005f, 0.03f, 9000f, 1500f);
            s.Sweep(0f, 0.3f, 150f, 50f, 0.8f, 0.07f, 0.001f, 3f);
            s.Reverb(0.4f, 1.4f);
            return Done(s, 0.85f);
        }

        static float[] WindS()
        {
            var s = new Synth(1.0f, Rate, 48);
            s.NoiseBurst(0f, 1f, 0.7f, 0.3f, 0.4f, 900f, 2500f, 300f, 700f);
            return Done(s, 0.6f);
        }
    }
}
