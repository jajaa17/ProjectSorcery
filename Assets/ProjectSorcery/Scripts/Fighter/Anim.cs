using System.Collections.Generic;
using UnityEngine;

namespace ProjectSorcery
{
    public enum Ease : byte { Linear, In, Out, InOut, Snap, Back, Hold }

    /// <summary>Which body part leaves a smear arc during a strike.</summary>
    public enum Limb : byte { None, HandF, HandB, FootF, FootB, Weapon, Hands }

    /// <summary>
    /// A keyframed move. Time is in phase units: 0..1 = wind-up (startup), 1..2 = strike (active frames),
    /// 2..3 = follow-through (recovery). Keys are joined with Catmull-Rom splines, each segment shaped by an ease,
    /// so motion flows through the keys instead of popping between them.
    /// </summary>
    public sealed class Clip
    {
        public string Name;
        public float[] T;
        public Kf[] K;
        public Ease[] E;
        public Limb Smear = Limb.HandF;
        public int TwoHand = -1;          // -1 = style default, 0 = one hand, 1 = both hands on the weapon
        public bool Ghosts;               // afterimages while striking
        public float Impact = 1f;         // camera/hit feedback scale at contact
        public bool GroundSlam;           // dust and cracks at contact
        public float Stiff = 1f;          // spring stiffness multiplier
        public bool Aura;                 // cursed-energy shockwave bursts out at contact
        static int counter;
        public readonly int Id = ++counter;

        public Kf Sample(float t)
        {
            int n = T.Length;
            if (t <= T[0]) return K[0];
            if (t >= T[n - 1]) return K[n - 1];
            int i = 0;
            while (i < n - 2 && t >= T[i + 1]) i++;
            float u = (t - T[i]) / Mathf.Max(0.0001f, T[i + 1] - T[i]);
            var e = E[i + 1];
            if (e == Ease.Hold) return K[i];
            u = Anim.Apply(e, u);
            return Kf.Spline(K[Mathf.Max(0, i - 1)], K[i], K[i + 1], K[Mathf.Min(n - 1, i + 2)], u);
        }

        public static Clip Keys(string name, Limb smear, params (float t, Pose p, Ease e)[] keys)
        {
            var c = new Clip { Name = name, Smear = smear, T = new float[keys.Length], K = new Kf[keys.Length], E = new Ease[keys.Length] };
            for (int i = 0; i < keys.Length; i++) { c.T[i] = keys[i].t; c.K[i] = keys[i].p.ToKf(); c.E[i] = keys[i].e; }
            return c;
        }

        /// <summary>
        /// The standard strike: settle into the anticipation, coil a little deeper, SNAP to contact, hang on the
        /// impact through the active frames, then let the body carry through and recover.
        /// </summary>
        public static Clip Strike(string name, Pose antic, Pose contact, Pose follow, Limb smear)
        {
            var a = antic.ToKf(); var c = contact.ToKf(); var f = follow.ToKf();
            var deep = Kf.Lerp(c, a, 1.12f);
            var hold = Kf.Lerp(c, f, 0.25f);
            return new Clip
            {
                Name = name, Smear = smear,
                T = new[] { 0f, 0.8f, 1f, 1.85f, 2.55f, 3f },
                K = new[] { a, deep, c, hold, f, f },
                E = new[] { Ease.Linear, Ease.InOut, Ease.Snap, Ease.Out, Ease.Out, Ease.Linear },
            };
        }

        /// <summary>A strike whose path bends through an extra pose just before contact (big arcs, overheads, spins).</summary>
        public static Clip Arc(string name, Pose antic, Pose mid, Pose contact, Pose follow, Limb smear)
        {
            var a = antic.ToKf(); var m = mid.ToKf(); var c = contact.ToKf(); var f = follow.ToKf();
            var deep = Kf.Lerp(m, a, 1.08f);
            var hold = Kf.Lerp(c, f, 0.25f);
            return new Clip
            {
                Name = name, Smear = smear,
                T = new[] { 0f, 0.78f, 0.93f, 1f, 1.85f, 2.55f, 3f },
                K = new[] { a, deep, m, c, hold, f, f },
                E = new[] { Ease.Linear, Ease.InOut, Ease.In, Ease.Out, Ease.Out, Ease.Out, Ease.Linear },
            };
        }

        public Clip With(int twoHand = -2, bool? ghosts = null, float impact = -1f, bool? slam = null, float stiff = -1f)
        {
            if (twoHand != -2) TwoHand = twoHand;
            if (ghosts.HasValue) Ghosts = ghosts.Value;
            if (impact >= 0f) Impact = impact;
            if (slam.HasValue) GroundSlam = slam.Value;
            if (stiff > 0f) Stiff = stiff;
            return this;
        }
    }

    public static class Anim
    {
        public static float Apply(Ease e, float u)
        {
            switch (e)
            {
                case Ease.In: return u * u * u;
                case Ease.Out: { float v = 1f - u; return 1f - v * v * v; }
                case Ease.InOut: return u * u * (3f - 2f * u);
                case Ease.Snap: { float v = 1f - u; return 1f - v * v * v * v * v; }
                case Ease.Back: { const float s = 1.9f; float v = u - 1f; return 1f + v * v * ((s + 1f) * v + s); }
                default: return u;
            }
        }

        static Pose P(float lean, float uaB, float faB, float uaF, float faF, float thB, float shB, float thF, float shF,
                      float rot = 0f, float head = 0f, float drop = 0f, float wr = 0f, float hx = 0f, float tn = 0f)
            => Pose.P(lean, uaB, faB, uaF, faF, thB, shB, thF, shF, rot, head, drop, wr, hx, tn);

        // =====================================================================
        //  Cast / technique clips (shared by every fighter's abilities)
        // =====================================================================
        static readonly Dictionary<FPose, Clip> castClips = new Dictionary<FPose, Clip>();

        public static Clip Cast(FPose p)
        {
            if (castClips.TryGetValue(p, out var c)) return c;
            c = BuildCast(p);
            castClips[p] = c;
            return c;
        }

        static Clip BuildCast(FPose p)
        {
            switch (p)
            {
                case FPose.Point:   // draw the hand back to the ear, then thrust the technique out
                    return Clip.Strike("Point", P(-6, 20, 135, -10, 150, -20, -32, 26, -2, 0, -4), P(16, 15, 120, 92, 90, -32, -50, 38, 6, 0, 0, 0, 0, 0.12f), P(12, 18, 125, 86, 96, -30, -46, 34, 4), Limb.HandF);
                case FPose.Palm:
                    return Clip.Strike("Palm", P(-4, 30, 140, -40, 40, -22, -34, 30, -4, 0, 0, 0.08f), P(22, 20, 130, 90, 78, -40, -60, 42, 8, 0, 0, 0.12f, 0, 0.18f), P(18, 20, 130, 86, 84, -36, -54, 40, 6, 0, 0, 0.1f), Limb.HandF);
                case FPose.TwoPalm: // gather at the hip, then push both palms out
                    return Clip.Strike("TwoPalm", P(-12, -55, 30, -45, 40, -26, -40, 36, -6, 0, 6, 0.16f), P(18, 88, 92, 92, 88, -42, -62, 44, 8, 0, 0, 0.12f, 0, 0.12f), P(22, 86, 94, 90, 92, -44, -64, 44, 8, 0, 0, 0.12f), Limb.Hands);
                case FPose.HandSign:
                    return Clip.Strike("HandSign", P(6, 40, 160, 46, 158, -20, -30, 22, 0, 0, 14, 0.05f), P(0, 46, 156, 52, 150, -22, -30, 24, 0, 0, -6), P(2, 46, 156, 52, 150, -22, -30, 24, 0), Limb.None);
                case FPose.Raise:   // crouch, gather, then fling both arms to the sky
                    return Clip.Strike("Raise", P(26, 10, 40, 20, 50, -55, -110, 70, -30, 0, 14, 0.3f), P(-8, 172, 178, 168, 180, -18, -24, 18, -2, 0, -14, -0.06f), P(-4, 164, 176, 168, 178, -20, -28, 20, 0, 0, -8), Limb.Hands);
                case FPose.Throw:   // overhand: arm behind the head, whip it through, follow down
                    return Clip.Arc("Throw", P(-16, 50, 130, -120, 180, -16, -26, 30, 0, 0, -6), P(4, 40, 120, 150, 140, -26, -40, 34, 2), P(28, -20, 30, 82, 62, -38, -56, 40, 8, 0, 0, 0.05f, 0, 0.15f), P(34, -30, 10, 30, 0, -40, -60, 42, 8, 0, 0, 0.08f), Limb.HandF);
                case FPose.Slash:   // hand-blade: high and behind, rip diagonally down through the target
                    return Clip.Arc("Slash", P(-10, 30, 120, 175, 215, -18, -28, 26, 0, 0, -6), P(6, 20, 110, 130, 140, -26, -40, 32, 4), P(26, -10, 40, 70, 45, -42, -60, 44, 10, 0, 0, 0.06f, 0, 0.15f), P(32, -20, 30, 15, -25, -44, -62, 46, 10, 0, 0, 0.1f), Limb.HandF);
                case FPose.Stab:
                    return Clip.Strike("Stab", P(-4, 30, 120, -25, 70, -20, -30, 26, 0, 0, 0, 0.05f, 0, -0.1f), P(30, -30, 10, 94, 92, -50, -62, 56, 14, 0, 0, 0.05f, 0, 0.3f), P(26, -20, 20, 90, 95, -46, -58, 52, 12), Limb.HandF);
                case FPose.Spin:
                    return Clip.Strike("Spin", P(6, 40, 150, 50, 150, -24, -36, 28, 0, 0, 0, 0.12f, 0, 0, 0f), P(8, -95, -90, 95, 90, -24, -32, 30, 0, 0, 0, 0.05f, 0, 0, 0.5f), P(6, 30, 140, 60, 140, -22, -30, 26, 0, 0, 0, 0.05f, 0, 0, 1f), Limb.Hands);
                case FPose.Slam:    // up on the toes with arms overhead, then crash down into the ground
                    return Clip.Arc("Slam", P(-12, 190, 205, 185, 200, -14, -18, 18, -2, 0, -10, -0.08f), P(10, 150, 160, 145, 155, -30, -50, 40, 0, 0, 0, 0.05f), P(46, 70, 25, 72, 20, -52, -110, 66, -24, 0, 20, 0.36f, 0, 0.1f), P(42, 60, 20, 62, 15, -52, -110, 66, -24, 0, 18, 0.34f), Limb.Hands).With(slam: true, impact: 1.3f);
                case FPose.Clap:    // arms flung wide, then SMACK
                    return Clip.Strike("Clap", P(-6, -110, -95, 115, 105, -20, -28, 22, 0, 0, -6), P(8, 66, 152, 72, 150, -22, -30, 24, 0, 0, 4), P(6, 68, 150, 72, 152, -20, -28, 22, 0), Limb.Hands);
                case FPose.Grab:
                    return Clip.Strike("Grab", P(-4, 20, 120, -10, 100, -22, -32, 28, 0), P(26, 40, 90, 96, 96, -40, -56, 44, 8, 0, 0, 0.05f, 0, 0.25f), P(18, 30, 80, 70, 60, -34, -50, 40, 6), Limb.HandF);
                case FPose.Bow:     // nock beside the front hand, draw to the cheek, release
                    return Clip.Strike("Bow", P(2, 86, 96, 88, 90, -22, -32, 24, 0), P(4, 82, 176, 90, 90, -24, -34, 26, 0), P(0, -50, 30, 92, 92, -24, -34, 26, 0, 0, -4), Limb.None);
                case FPose.Shoot:   // raise, fire, recoil up
                    return Clip.Strike("Shoot", P(4, 20, 120, 40, 60, -20, -30, 24, 0), P(6, 84, 100, 94, 94, -22, -30, 24, 0), P(-4, 84, 100, 112, 128, -20, -28, 22, 0, 0, -6), Limb.None);
                case FPose.Chant:
                    return Clip.Strike("Chant", P(4, 15, 110, 40, 170, -18, -26, 20, 0, 0, 12), P(-6, 15, 100, 40, 162, -18, -26, 20, 0, 0, 8), P(-6, 15, 100, 40, 162, -18, -26, 20, 0, 0, 8), Limb.None);
                case FPose.Channel:
                    return Clip.Strike("Channel", P(10, 20, 60, 30, 70, -22, -30, 24, 0, 0, 10, 0.1f), P(-4, -50, -80, 50, 80, -20, -26, 20, 0, 0, -8), P(0, -40, -70, 40, 70, -18, -25, 18, 0), Limb.None);
                case FPose.Crouch:
                    return Clip.Strike("Crouch", Poses.Idle, Poses.Crouch, Poses.Crouch, Limb.None);
                case FPose.Block:
                    return Clip.Strike("Guard", Poses.Idle, Poses.Block, Poses.Block, Limb.None);
                case FPose.Victory:
                    return Clip.Strike("Victory", Poses.Idle, Poses.Victory, Poses.Victory, Limb.None);
                case FPose.Taunt:
                    return Clip.Strike("Taunt", Poses.Idle, Poses.Taunt, Poses.Taunt, Limb.None);
                case FPose.Dash:    // technique lunge: coil low, explode forward, arm leading
                    return Clip.Strike("Rush", P(18, 10, 120, 30, 150, -40, -80, 56, -24, 0, 6, 0.2f), P(44, -60, -30, 95, 92, -78, -88, 40, 26, 0, 6, 0.05f, 0, 0.3f), P(36, -40, -10, 90, 96, -60, -70, 40, 10, 0, 0, 0.05f), Limb.HandF).With(ghosts: true);
                default:
                    return MoveLib.Brawler.ForPose(p);
            }
        }

        // =====================================================================
        //  Locomotion
        // =====================================================================
        public enum ArmMode { Pump, Back, Sword, Wide, Low, Stiff, Glide, Relaxed }

        public struct GaitSpec
        {
            public float Lean, Stride, KneeLift, Kick, Bob, ArmAmp, ArmBend, Cadence;
            public ArmMode Arms;
        }

        public static GaitSpec Gait(Stance s)
        {
            var g = new GaitSpec { Lean = 9f, Stride = 46f, KneeLift = 66f, Kick = 46f, Bob = 0.07f, ArmAmp = 45f, ArmBend = 95f, Arms = ArmMode.Pump, Cadence = 1f };
            switch (s)
            {
                case Stance.Brawler: g.Lean = 10f; g.ArmAmp = 40f; g.ArmBend = 110f; break;
                case Stance.Martial: g.Lean = 9f; g.Stride = 50f; break;
                case Stance.Agile: g.Lean = 24f; g.Stride = 54f; g.KneeLift = 78f; g.Kick = 70f; g.Arms = ArmMode.Back; g.Cadence = 1.12f; break;
                case Stance.Brute: g.Lean = 8f; g.Stride = 38f; g.KneeLift = 50f; g.Kick = 45f; g.Bob = 0.12f; g.ArmAmp = 34f; g.ArmBend = 45f; g.Arms = ArmMode.Wide; g.Cadence = 0.85f; break;
                case Stance.Wrestler: g.Lean = 12f; g.Stride = 40f; g.KneeLift = 55f; g.Bob = 0.1f; g.ArmAmp = 30f; g.ArmBend = 70f; g.Arms = ArmMode.Wide; g.Cadence = 0.9f; break;
                case Stance.Feral: g.Lean = 26f; g.Stride = 56f; g.KneeLift = 80f; g.Kick = 65f; g.Bob = 0.09f; g.Arms = ArmMode.Low; g.Cadence = 1.1f; break;
                case Stance.Elegant: g.Lean = 4f; g.Stride = 40f; g.KneeLift = 55f; g.Kick = 50f; g.Bob = 0.04f; g.ArmAmp = 22f; g.ArmBend = 40f; g.Arms = ArmMode.Relaxed; break;
                case Stance.Caster: g.Lean = 7f; g.Stride = 42f; g.ArmAmp = 32f; g.ArmBend = 70f; break;
                case Stance.Swordsman: g.Lean = 14f; g.Stride = 50f; g.Arms = ArmMode.Sword; g.Cadence = 1.05f; break;
                case Stance.Mechanical: g.Lean = 6f; g.Stride = 36f; g.KneeLift = 50f; g.Kick = 35f; g.Bob = 0.03f; g.ArmAmp = 25f; g.ArmBend = 90f; g.Arms = ArmMode.Stiff; g.Cadence = 0.95f; break;
                case Stance.Floaty: g.Lean = 20f; g.Stride = 10f; g.KneeLift = 20f; g.Kick = 30f; g.Bob = 0.02f; g.Arms = ArmMode.Glide; g.Cadence = 0.5f; break;
            }
            return g;
        }

        static readonly Dictionary<Stance, Kf[]> gaitCache = new Dictionary<Stance, Kf[]>();

        /// <summary>Eight-key run cycle (contact, down, passing, up, then the mirrored half).</summary>
        public static Kf[] GaitKeys(Stance s)
        {
            if (gaitCache.TryGetValue(s, out var keys)) return keys;
            var g = Gait(s);
            float st = g.Stride, kl = g.KneeLift, kk = g.Kick, bob = g.Bob;
            var legs = new Pose[4];
            legs[0] = P(0, 0, 0, 0, 0, -st * 0.8f, -st * 0.8f - kk * 0.35f, st, st - 8f, 0, 0, 0f);
            legs[1] = P(0, 0, 0, 0, 0, -st * 0.55f, -st * 0.55f - kk, st * 0.45f, st * 0.45f - 30f, 0, 0, bob);
            legs[2] = P(0, 0, 0, 0, 0, kl * 0.55f, kl * 0.55f - kk * 1.1f, -st * 0.1f, -st * 0.15f, 0, 0, bob * 0.3f);
            legs[3] = P(0, 0, 0, 0, 0, kl, kl - 62f, -st * 0.65f, -st * 0.75f, 0, 0, -bob * 0.6f);
            float[] swing = { -1f, -0.6f, 0f, 0.7f, 1f, 0.6f, 0f, -0.7f };
            keys = new Kf[8];
            for (int i = 0; i < 8; i++)
            {
                var p = i < 4 ? legs[i] : legs[i - 4].SwapLegs();
                p.Lean = g.Lean + (i % 4 == 1 ? 3f : 0f);
                p.Head = -g.Lean * 0.6f;
                float sw = swing[i];
                switch (g.Arms)
                {
                    case ArmMode.Back: p.UaF = -72f + sw * 6f; p.FaF = -48f + sw * 6f; p.UaB = -76f - sw * 6f; p.FaB = -52f - sw * 6f; break;
                    case ArmMode.Sword: p.UaF = -30f + sw * 8f; p.FaF = -55f + sw * 8f; p.Wr = -25f; p.UaB = -sw * g.ArmAmp; p.FaB = p.UaB + g.ArmBend; break;
                    case ArmMode.Low: p.UaF = 35f + sw * 35f; p.FaF = 15f + sw * 40f; p.UaB = 35f - sw * 35f; p.FaB = 15f - sw * 40f; break;
                    case ArmMode.Glide:
                        p = P(g.Lean, -70, -50, -60, -40, -30, -60, -12, -40, 0, -6, -0.05f);
                        p.UaF += sw * 4f; p.UaB -= sw * 4f; p.ThF += sw * 4f; p.ThB -= sw * 4f;
                        break;
                    default: p.UaF = sw * g.ArmAmp; p.FaF = p.UaF + g.ArmBend; p.UaB = -sw * g.ArmAmp; p.FaB = p.UaB + g.ArmBend; break;
                }
                keys[i] = p.ToKf();
            }
            gaitCache[s] = keys;
            return keys;
        }

        /// <summary>Samples the looping cycle (phase in cycles) with a smooth spline.</summary>
        public static Kf SampleGait(Kf[] keys, float phase)
        {
            float t = Mathf.Repeat(phase, 1f) * 8f;
            int i = (int)t; float u = t - i;
            return Kf.Spline(keys[(i + 7) & 7], keys[i & 7], keys[(i + 1) & 7], keys[(i + 2) & 7], u);
        }

        // ---- air
        public static readonly Pose Takeoff = P(4, -30, 10, 150, 175, -14, -24, -6, -16, 0, -10, -0.06f);
        public static readonly Pose Rise = P(10, -50, -10, 140, 120, 25, -55, 78, 8, 0, -4);
        public static readonly Pose Apex = P(14, -30, 40, 110, 150, 55, -40, 92, -18, 0, 0, -0.05f);
        public static readonly Pose Fall = P(-4, 115, 150, 125, 160, -8, -32, 30, -4, 0, -8);
        public static readonly Pose LandSquat = P(24, 40, 100, 60, 120, -50, -105, 66, -26, 0, 10, 0.3f);

        // ---- reactions
        public static readonly Pose HurtHigh = P(-24, -70, -40, -55, -20, -26, -40, 14, -8, 0, -34);
        public static readonly Pose HurtGut = P(34, 30, 10, 40, 20, -20, -30, 24, -6, 0, 22, 0.14f);
        public static readonly Pose HurtAir = P(-30, 150, 170, 130, 150, 30, -10, 60, 30, 0, -30);
        public static readonly Pose Knocked = P(0, 160, 175, 30, 60, -5, -5, 10, 0, -88);
        public static readonly Pose KipCoil = P(0, 160, 200, 165, 200, 120, 160, 140, 170, -70, 0, 0.1f);
        public static readonly Pose Kneel = P(20, 20, 60, 50, 90, -60, -120, 80, -10, 0, 10, 0.32f);
        public static readonly Pose DashFwd = P(42, -75, -50, -60, -35, -55, -90, 55, 25, 0, 8, 0.06f);
        public static readonly Pose DashBack = P(-20, 60, 140, 70, 150, -20, -40, 50, 20, 0, -6, 0.06f);
    }
}
