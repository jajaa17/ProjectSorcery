using System.Collections.Generic;
using UnityEngine;

namespace ProjectSorcery
{
    public enum MoveStyle { Brawler, Martial, Agile, Brute, Wrestler, Feral, Elegant, Caster, Blade, Staff, HeavyWeapon }

    /// <summary>A fighter's normal attacks: light string, heavy smash, launcher, sweep, dash strike and aerials.</summary>
    public sealed class MoveSet
    {
        public MoveStyle Style;
        public AttackDef[] Light;
        public AttackDef Heavy, Launcher, Sweep, Dash;
        public AttackDef[] Air;
        public AttackDef AirHeavy, Dive;
        public bool TwoHand;           // weapon held with both hands by default
        public float Tempo = 1f;       // visual spring stiffness personality

        /// <summary>Clip for a scripted technique strike that names a melee pose.</summary>
        public Clip ForPose(FPose p)
        {
            switch (p)
            {
                case FPose.Jab: return Light[0].Clip;
                case FPose.Cross: return Light[Mathf.Min(1, Light.Length - 1)].Clip;
                case FPose.Hook: return Light[Mathf.Min(2, Light.Length - 1)].Clip;
                case FPose.Kick: return Light[Light.Length - 1].Clip;
                case FPose.Uppercut: return Launcher.Clip;
                case FPose.Sweep: return Sweep.Clip;
                case FPose.HeavyPunch: return Heavy.Clip;
                case FPose.AirKick: return Air[0].Clip;
                case FPose.AirSlash: return Air[Mathf.Min(1, Air.Length - 1)].Clip;
                case FPose.DiveKick: return Dive.Clip;
                case FPose.Dash: return Dash.Clip;
                default: return Light[0].Clip;
            }
        }
    }

    /// <summary>
    /// Hand-keyed choreography for every fighting style. Poses are (lean, back arm upper/fore, front arm upper/fore,
    /// back leg thigh/shin, front leg thigh/shin, rot, head, drop, weapon wrist, hip shift, turn); see <see cref="Pose"/>.
    /// Each move is anticipation -> strike -> follow-through.
    /// </summary>
    public static class MoveLib
    {
        static Pose P(float lean, float uaB, float faB, float uaF, float faF, float thB, float shB, float thF, float shF,
                      float rot = 0f, float head = 0f, float drop = 0f, float wr = 0f, float hx = 0f, float tn = 0f)
            => Pose.P(lean, uaB, faB, uaF, faF, thB, shB, thF, shF, rot, head, drop, wr, hx, tn);

        static Clip S(string n, Pose a, Pose c, Pose f, Limb l) => Clip.Strike(n, a, c, f, l);
        static Clip A(string n, Pose a, Pose m, Pose c, Pose f, Limb l) => Clip.Arc(n, a, m, c, f, l);

        // ------------------------------------------------------------------ gameplay templates
        enum Slot { L1, L2, L3, L4, Heavy, Launch, Sweep, Dash, Air1, Air2, AirHeavy, Dive }

        struct StyleStats { public float Speed, Damage, Reach, Radius; }

        static StyleStats Stats(MoveStyle s)
        {
            switch (s)
            {
                case MoveStyle.Agile: return new StyleStats { Speed = 0.9f, Damage = 0.92f };
                case MoveStyle.Brute: return new StyleStats { Speed = 1.14f, Damage = 1.12f, Radius = 0.05f };
                case MoveStyle.Wrestler: return new StyleStats { Speed = 1.08f, Damage = 1.08f, Radius = 0.04f };
                case MoveStyle.Feral: return new StyleStats { Speed = 0.94f, Damage = 0.96f, Reach = 0.05f };
                case MoveStyle.Elegant: return new StyleStats { Speed = 0.97f, Damage = 0.97f, Reach = 0.08f };
                case MoveStyle.Caster: return new StyleStats { Speed = 1f, Damage = 0.95f };
                case MoveStyle.Blade: return new StyleStats { Speed = 1.04f, Damage = 1.03f };
                case MoveStyle.Staff: return new StyleStats { Speed = 1.06f, Damage = 1.02f, Radius = 0.06f };
                case MoveStyle.HeavyWeapon: return new StyleStats { Speed = 1.16f, Damage = 1.14f, Radius = 0.08f };
                default: return new StyleStats { Speed = 1f, Damage = 1f };
            }
        }

        static AttackDef Make(Slot slot, string name, Clip clip, MoveStyle style, Vector2? offset = null, float radius = -1f)
        {
            var st = Stats(style);
            AttackDef a;
            switch (slot)
            {
                case Slot.L1: a = new AttackDef { Startup = 0.075f, Active = 0.06f, Recovery = 0.13f, Damage = 26f, Knockback = new Vector2(1.6f, 0.3f), Hitstun = 0.34f, Lunge = new Vector2(3.5f, 0f) }; break;
                case Slot.L2: a = new AttackDef { Startup = 0.08f, Active = 0.06f, Recovery = 0.14f, Damage = 28f, Knockback = new Vector2(1.9f, 0.3f), Hitstun = 0.36f, Lunge = new Vector2(3.5f, 0f) }; break;
                case Slot.L3: a = new AttackDef { Startup = 0.09f, Active = 0.07f, Recovery = 0.16f, Damage = 31f, Knockback = new Vector2(2.2f, 1f), Hitstun = 0.38f, Lunge = new Vector2(4f, 0f) }; break;
                case Slot.L4: a = new AttackDef { Startup = 0.13f, Active = 0.08f, Recovery = 0.27f, Damage = 46f, Knockback = new Vector2(7.5f, 4f), Hitstun = 0.5f, Hitstop = 0.09f, Offset = new Vector2(0.95f, 1.15f), Radius = 0.62f, Lunge = new Vector2(5f, 0f) }; break;
                case Slot.Heavy: a = new AttackDef { Startup = 0.24f, Active = 0.09f, Recovery = 0.32f, Damage = 72f, Knockback = new Vector2(10.5f, 4.5f), Hitstun = 0.62f, Hitstop = 0.13f, Flags = HitFlags.Heavy, Offset = new Vector2(0.92f, 1.3f), Radius = 0.66f, Lunge = new Vector2(7f, 0f), CeGain = 6f, Chargeable = true }; break;
                case Slot.Launch: a = new AttackDef { Startup = 0.15f, Active = 0.09f, Recovery = 0.3f, Damage = 55f, Knockback = new Vector2(1.4f, 15.5f), Hitstun = 0.75f, Hitstop = 0.1f, Flags = HitFlags.Heavy | HitFlags.Launch, Offset = new Vector2(0.7f, 1.7f), Radius = 0.62f, Lunge = new Vector2(3f, 0f), CeGain = 6f }; break;
                case Slot.Sweep: a = new AttackDef { Startup = 0.11f, Active = 0.08f, Recovery = 0.26f, Damage = 34f, Knockback = new Vector2(2f, 5.5f), Hitstun = 0.55f, Offset = new Vector2(1f, 0.3f), Radius = 0.6f, Lunge = new Vector2(4f, 0f) }; break;
                case Slot.Dash: a = new AttackDef { Startup = 0.1f, Active = 0.1f, Recovery = 0.28f, Damage = 44f, Knockback = new Vector2(8f, 3f), Hitstun = 0.5f, Hitstop = 0.09f, Offset = new Vector2(0.9f, 1.2f), Radius = 0.66f, Lunge = new Vector2(11f, 0f), DashStrike = true }; break;
                case Slot.Air1: a = new AttackDef { Startup = 0.06f, Active = 0.08f, Recovery = 0.12f, Damage = 24f, Knockback = new Vector2(2f, 3.2f), Hitstun = 0.4f, Air = true, Lunge = Vector2.zero, Offset = new Vector2(0.8f, 0.9f) }; break;
                case Slot.Air2: a = new AttackDef { Startup = 0.07f, Active = 0.08f, Recovery = 0.14f, Damage = 28f, Knockback = new Vector2(2.4f, 3.4f), Hitstun = 0.42f, Air = true, Lunge = Vector2.zero }; break;
                case Slot.AirHeavy: a = new AttackDef { Startup = 0.14f, Active = 0.08f, Recovery = 0.22f, Damage = 55f, Knockback = new Vector2(9f, 2f), Hitstun = 0.55f, Hitstop = 0.11f, Flags = HitFlags.Heavy, Air = true, Lunge = new Vector2(2f, 0f), CeGain = 5f }; break;
                default: a = new AttackDef { Startup = 0.1f, Active = 0.12f, Recovery = 0.2f, Damage = 50f, Knockback = new Vector2(3f, -16f), Hitstun = 0.6f, Hitstop = 0.11f, Flags = HitFlags.Heavy | HitFlags.Spike, Air = true, Offset = new Vector2(0.5f, 0.2f), Radius = 0.7f, Lunge = new Vector2(4f, -10f), CeGain = 5f }; break;
            }
            a.Name = name;
            a.Clip = clip;
            a.Startup *= st.Speed; a.Recovery *= st.Speed;
            a.Damage *= st.Damage;
            if (offset.HasValue) a.Offset = offset.Value;
            a.Offset.x += st.Reach;
            if (radius > 0f) a.Radius = radius;
            a.Radius += st.Radius;
            return a;
        }

        static readonly Vector2 KickHigh = new Vector2(0.95f, 1.2f), KickMid = new Vector2(0.95f, 1.0f), Low = new Vector2(1.0f, 0.3f), Body = new Vector2(0.75f, 1.05f);

        // ------------------------------------------------------------------ cache
        static readonly Dictionary<MoveStyle, MoveSet> bases = new Dictionary<MoveStyle, MoveSet>();
        static readonly Dictionary<string, MoveSet> perFighter = new Dictionary<string, MoveSet>();

        public static MoveSet Brawler => Base(MoveStyle.Brawler);

        public static MoveStyle StyleOf(CharacterDef d)
        {
            var persona = Persona.For(d);
            if (persona.Style.HasValue) return persona.Style.Value;
            switch (d.Look.Weapon)
            {
                case Weapon.Katana: case Weapon.Sword: case Weapon.Cleaver: return MoveStyle.Blade;
                case Weapon.Staff: case Weapon.Polearm: case Weapon.Spear: case Weapon.Broom: case Weapon.Cloud: return MoveStyle.Staff;
                case Weapon.Hammer: case Weapon.Gavel: case Weapon.Guitar: case Weapon.Axe: return MoveStyle.HeavyWeapon;
                case Weapon.Claws: return MoveStyle.Feral;
                case Weapon.Fan: return MoveStyle.Elegant;
            }
            switch (d.Stance)
            {
                case Stance.Martial: return MoveStyle.Martial;
                case Stance.Agile: return MoveStyle.Agile;
                case Stance.Brute: return MoveStyle.Brute;
                case Stance.Wrestler: return MoveStyle.Wrestler;
                case Stance.Feral: return MoveStyle.Feral;
                case Stance.Elegant: return MoveStyle.Elegant;
                case Stance.Caster: case Stance.Floaty: return MoveStyle.Caster;
                case Stance.Swordsman: return MoveStyle.Martial;
                default: return MoveStyle.Brawler;
            }
        }

        static bool TwoHanded(Weapon w) => w == Weapon.Staff || w == Weapon.Polearm || w == Weapon.Spear || w == Weapon.Broom ||
                                           w == Weapon.Sword || w == Weapon.Katana || w == Weapon.Hammer || w == Weapon.Guitar;

        /// <summary>
        /// The fighter's own moveset: their style's choreography with a personal twist (lead hand, exaggeration,
        /// tempo, finisher) derived from their id, so no two fighters move quite alike.
        /// </summary>
        public static MoveSet For(CharacterDef d)
        {
            if (perFighter.TryGetValue(d.Id, out var set)) return set;
            var b = Base(StyleOf(d));
            uint h = 2166136261u;
            foreach (char c in d.Id) { h ^= c; h *= 16777619u; }
            float r1 = (h & 1023) / 1023f, r2 = ((h >> 10) & 1023) / 1023f, r3 = ((h >> 20) & 1023) / 1023f;
            bool armed = b.Style == MoveStyle.Blade || b.Style == MoveStyle.Staff || b.Style == MoveStyle.HeavyWeapon;
            bool southpaw = !armed && r1 < 0.3f;
            float exag = 0.92f + r2 * 0.26f;
            if (d.Stance == Stance.Brute || d.Stance == Stance.Wrestler) exag += 0.06f;
            if (d.Stance == Stance.Elegant || d.Stance == Stance.Mechanical) exag -= 0.06f;
            var idle = StyleIdle(d.Stance).ToKf();

            set = new MoveSet
            {
                Style = b.Style,
                TwoHand = TwoHanded(d.Look.Weapon),
                Tempo = (d.Stance == Stance.Mechanical ? 1.25f : 0.92f + r3 * 0.22f) * (d.Stance == Stance.Brute ? 0.9f : 1f),
            };
            AttackDef F(AttackDef a) => Flavor(a, idle, exag, southpaw);
            set.Light = new AttackDef[b.Light.Length];
            for (int i = 0; i < b.Light.Length; i++) set.Light[i] = F(b.Light[i]);
            // a third of fighters finish their string with their style's alternate finisher (same frame data)
            if (r3 > 0.66f && AltFinisher.TryGetValue(b.Style, out var alt))
            {
                var last = set.Light[set.Light.Length - 1].Copy();
                last.Clip = FlavorClip(alt, idle, exag, southpaw);
                set.Light[set.Light.Length - 1] = last;
            }
            set.Heavy = F(b.Heavy); set.Launcher = F(b.Launcher); set.Sweep = F(b.Sweep); set.Dash = F(b.Dash);
            set.Air = new[] { F(b.Air[0]), F(b.Air[1]) };
            set.AirHeavy = F(b.AirHeavy); set.Dive = F(b.Dive);
            perFighter[d.Id] = set;
            return set;
        }

        static AttackDef Flavor(AttackDef a, in Kf idle, float exag, bool southpaw)
        {
            var c = a.Copy();
            c.Clip = FlavorClip(a.Clip, idle, exag, southpaw);
            return c;
        }

        static Clip FlavorClip(Clip src, in Kf idle, float exag, bool southpaw)
        {
            var c = new Clip { Name = src.Name, T = src.T, E = src.E, Smear = src.Smear, TwoHand = src.TwoHand, Ghosts = src.Ghosts, Impact = src.Impact, GroundSlam = src.GroundSlam, Stiff = src.Stiff, Aura = src.Aura, K = new Kf[src.K.Length] };
            for (int i = 0; i < src.K.Length; i++)
            {
                var k = src.K[i];
                if (southpaw) k = k.SwapArms();
                // exaggerate the silhouette (not the spin/turn/wrist channels)
                for (int ch = 0; ch < Kf.Count; ch++)
                {
                    if (ch == 3 || ch == 6 || ch == 11 || ch == 14 || ch == 16) continue;
                    k[ch] = idle[ch] + (k[ch] - idle[ch]) * exag;
                }
                c.K[i] = k;
            }
            if (southpaw)
            {
                if (c.Smear == Limb.HandF) c.Smear = Limb.HandB;
                else if (c.Smear == Limb.HandB) c.Smear = Limb.HandF;
            }
            return c;
        }

        public static Pose StyleIdle(Stance s)
        {
            switch (s)
            {
                case Stance.Elegant: return P(-2, -12, 12, -8, 18, -10, -12, 12, 2, 0, -4);
                case Stance.Swordsman: return P(6, 30, 110, 55, 82, -30, -32, 32, 26, 0, -2, 0.02f, 38f);
                case Stance.Brute: return P(6, 15, 50, 25, 60, -26, -30, 28, 0, 0, -2);
                case Stance.Feral: return P(14, 5, 40, 20, 50, -30, -60, 36, -14, 0, -8, 0.05f);
                case Stance.Caster: return P(0, 10, 60, 60, 112, -16, -22, 18, 0);
                case Stance.Agile: return P(10, -30, 30, 50, 120, -32, -36, 36, 10, 0, -4, 0.04f);
                case Stance.Floaty: return P(-2, -60, -70, 70, 80, -6, -20, 10, -12);
                case Stance.Mechanical: return P(0, 20, 90, 30, 90, -14, -14, 14, 0);
                case Stance.Wrestler: return P(8, 60, 120, 70, 110, -28, -30, 30, 14);
                case Stance.Martial: return P(2, 40, 150, 70, 100, -26, -28, 30, 22);
                default: return P(2, 25, 150, 60, 125, -26, -28, 28, 20);
            }
        }

        static readonly Dictionary<MoveStyle, Clip> AltFinisher = new Dictionary<MoveStyle, Clip>();

        // ------------------------------------------------------------------ styles
        static MoveSet Base(MoveStyle s)
        {
            if (bases.TryGetValue(s, out var set)) return set;
            switch (s)
            {
                case MoveStyle.Martial: set = Martial(); break;
                case MoveStyle.Agile: set = Agile(); break;
                case MoveStyle.Brute: set = Brute(); break;
                case MoveStyle.Wrestler: set = Wrestler(); break;
                case MoveStyle.Feral: set = Feral(); break;
                case MoveStyle.Elegant: set = Elegant(); break;
                case MoveStyle.Caster: set = Caster(); break;
                case MoveStyle.Blade: set = Blade(); break;
                case MoveStyle.Staff: set = Staff(); break;
                case MoveStyle.HeavyWeapon: set = HeavyWeapon(); break;
                default: set = BrawlerSet(); break;
            }
            set.Style = s;
            bases[s] = set;
            return set;
        }

        // ================= shared unarmed aerials
        static void UnarmedAir(MoveSet m, MoveStyle s)
        {
            m.Air = new[]
            {
                Make(Slot.Air1, "Air Knee", S("AirKnee", P(10, -30, 20, 120, 150, 40, -40, 80, -10), P(-8, -40, 0, 110, 150, 30, -40, 98, 94), P(0, -30, 10, 110, 150, 30, -40, 72, 40), Limb.FootF), s),
                Make(Slot.Air2, "Air Spin Kick", S("AirSpin", P(6, 60, 140, 80, 150, 20, -40, 60, -20, 0, 0, 0, 0, 0, 0.06f), P(-16, 60, 140, 80, 150, -106, -112, 40, -20, 0, -6, 0, 0, 0, 0.5f), P(-6, 60, 140, 80, 150, 0, -40, 50, -10, 0, 0, 0, 0, 0, 1f), Limb.FootB), s, KickMid),
            };
            m.AirHeavy = Make(Slot.AirHeavy, "Hammer Drop", A("AirHammer", P(-16, 195, 212, 190, 205, 40, -30, 70, 0, 0, -10), P(8, 150, 150, 150, 150, 40, -30, 70, 0), P(36, 60, 25, 62, 22, 20, -40, 60, -10, 0, 14, 0, 0, 0.1f), P(40, 50, 15, 52, 12, 20, -40, 60, -10), Limb.Hands).With(impact: 1.3f), s);
            m.Dive = Make(Slot.Dive, "Meteor Heel", S("Dive", P(-12, 140, 170, 120, 150, 60, -20, 92, -10, 0, -6, -0.05f), P(32, 140, 170, 120, 150, 80, -10, 30, 38), P(30, 140, 170, 120, 150, 80, -10, 30, 38), Limb.FootF).With(ghosts: true, impact: 1.3f), s);
        }

        // ================= BRAWLER: boxing and street fighting
        static MoveSet BrawlerSet()
        {
            var s = MoveStyle.Brawler;
            var m = new MoveSet();
            m.Light = new[]
            {
                Make(Slot.L1, "Jab", S("Jab", P(-2, 25, 150, 40, 150, -28, -46, 30, 4, 0, 0, 0.04f), P(16, 25, 150, 92, 90, -38, -42, 42, 4, 0, 0, 0.08f, 0, 0.14f), P(18, 25, 150, 95, 88, -40, -44, 44, 4, 0, 0, 0.09f, 0, 0.16f), Limb.HandF), s),
                Make(Slot.L2, "Cross", S("Cross", P(-10, -40, 150, 70, 110, -30, -62, 34, 24, 0, 0, 0.06f, 0, -0.06f), P(26, 94, 91, 25, 150, -52, -54, 52, 6, 0, 4, 0.16f, 0, 0.22f), P(30, 98, 96, 20, 140, -54, -56, 54, 8, 0, 4, 0.18f, 0, 0.26f), Limb.HandB), s),
                Make(Slot.L3, "Body Hook", S("Hook", P(8, 22, 130, -25, 80, -24, -40, 28, -4, 0, 0, 0.06f), P(26, 30, 140, 72, 118, -36, -56, 38, 0, 0, 0, 0.1f, 0, 0.1f), P(30, 30, 140, 50, 140, -36, -56, 38, 0, 0, 0, 0.1f), Limb.HandF), s, Body),
                Make(Slot.L4, "Roundhouse", S("Roundhouse", P(-6, 30, 140, 40, 150, -14, -22, 72, -24, 0, 0, 0.02f), P(-26, 55, 150, -35, -5, -6, -10, 104, 100, 0, -6, 0, 0, 0.08f), P(-14, 45, 145, -10, 30, -10, -14, 70, 20), Limb.FootF).With(impact: 1.2f), s, KickHigh),
            };
            AltFinisher[s] = S("Spinning Backfist", P(6, 22, 118, 42, 142, -22, -36, 26, -2, 0, 0, 0.05f, 0, 0, 0.06f), P(14, -96, -88, 40, 140, -30, -42, 32, 0, 0, 0, 0, 0, 0.12f, 0.5f), P(8, 20, 110, 45, 140, -26, -38, 28, 0, 0, 0, 0, 0, 0, 1f), Limb.HandB);
            m.Heavy = Make(Slot.Heavy, "Haymaker", A("Haymaker", P(-16, -60, 160, 75, 115, -34, -70, 38, 30, 0, -4, 0.08f, 0, -0.12f), P(8, 10, 120, 50, 130, -42, -58, 46, 10, 0, 0, 0.12f), P(36, 98, 94, -10, 120, -62, -64, 62, 4, 0, 6, 0.22f, 0, 0.42f), P(42, 102, 98, -20, 110, -62, -64, 62, 4, 0, 8, 0.24f, 0, 0.46f), Limb.HandB).With(impact: 1.6f), s);
            m.Launcher = Make(Slot.Launch, "Rising Uppercut", S("Uppercut", P(30, 30, 140, 0, 60, -46, -92, 62, -26, 0, 10, 0.32f), P(-8, 20, 130, 168, 182, -18, -22, 12, -4, 0, -16, -0.06f), P(-12, 20, 130, 162, 178, -16, -20, 14, 0, 0, -10, -0.02f), Limb.HandF).With(impact: 1.3f), s);
            m.Sweep = Make(Slot.Sweep, "Low Sweep", S("Sweep", P(30, 10, 60, 60, 110, -58, -118, 70, -30, 0, 0, 0.3f), P(38, -10, 20, 40, 70, -64, -126, 96, 93, 0, 0, 0.38f, 0, 0.05f), P(32, 0, 30, 50, 100, -60, -120, 80, 60, 0, 0, 0.35f), Limb.FootF), s, Low);
            m.Dash = Make(Slot.Dash, "Superman Punch", S("Superman", P(12, -40, 90, 50, 140, -30, -60, 62, -40, 0, 0, 0.08f), P(44, 96, 92, -25, 35, -82, -90, 46, 30, 0, 6, 0, 0, 0.3f), P(38, 90, 95, -10, 50, -62, -72, 42, 12), Limb.HandB).With(ghosts: true, impact: 1.3f), s);
            UnarmedAir(m, s);
            return m;
        }

        // ================= MARTIAL: kung-fu palms, spinning strikes, kicks
        static MoveSet Martial()
        {
            var s = MoveStyle.Martial;
            var m = new MoveSet();
            m.Light = new[]
            {
                Make(Slot.L1, "Palm Strike", S("Palm", P(2, 40, 150, 20, 160, -22, -34, 26, -2), P(18, 38, 148, 90, 78, -34, -50, 38, 6, 0, 0, 0, 0, 0.12f), P(14, 40, 150, 86, 84, -30, -46, 34, 4), Limb.HandF), s),
                Make(Slot.L2, "Spinning Backfist", S("Backfist", P(6, 40, 150, 60, 120, -24, -36, 28, -2, 0, 0, 0, 0, 0, 0.06f), P(12, -96, -90, 40, 140, -28, -40, 30, 0, 0, 0, 0, 0, 0.1f, 0.5f), P(8, 30, 120, 50, 130, -26, -36, 28, 0, 0, 0, 0, 0, 0, 1f), Limb.HandB), s),
                Make(Slot.L3, "Snap Kick", S("SnapKick", P(2, 40, 150, 70, 110, -20, -28, 88, -24), P(-12, 40, 150, 60, 120, -16, -20, 96, 94, 0, 0, 0, 0, 0.1f), P(-6, 40, 150, 70, 110, -18, -24, 70, 10), Limb.FootF), s, KickMid),
                Make(Slot.L4, "Spinning Hook Kick", S("SpinHook", P(10, 40, 150, 70, 100, -24, -36, 28, -2, 0, 0, 0.04f, 0, 0, 0.08f), P(-18, 60, 150, 80, 150, -108, -118, 10, -10, 0, -6, 0, 0, 0.06f, 0.5f), P(-6, 40, 150, 70, 110, -30, -40, 20, -10, 0, 0, 0, 0, 0, 1f), Limb.FootB).With(impact: 1.2f), s, KickHigh),
            };
            AltFinisher[s] = S("Tornado Kick", P(10, 40, 150, 70, 100, -40, -80, 50, -30, 0, 0, 0.2f, 0, 0, 0.1f), P(-20, 150, 170, 140, 160, -100, -110, 60, -20, 0, -8, -0.15f, 0, 0.1f, 0.5f), P(-6, 40, 150, 70, 110, -26, -40, 24, -8, 0, 0, 0, 0, 0, 1f), Limb.FootB);
            m.Heavy = Make(Slot.Heavy, "Twin Dragon Palms", S("TwinPalm", P(-12, -60, -20, -50, -10, -30, -40, 40, 0, 0, 6, 0.18f), P(26, 92, 86, 95, 88, -50, -72, 52, 10, 0, 0, 0.12f, 0, 0.32f), P(28, 90, 90, 92, 92, -50, -72, 52, 10, 0, 0, 0.12f), Limb.Hands).With(impact: 1.4f, slam: false), s);
            m.Heavy.Clip.Aura = true;
            m.Launcher = Make(Slot.Launch, "Crescent Rise", S("Crescent", P(18, 40, 150, 70, 110, -40, -80, 56, -24, 0, 0, 0.22f), P(-24, 30, 130, 50, 140, -10, -14, 165, 170, 0, -10), P(-14, 40, 150, 70, 120, -14, -20, 110, 80), Limb.FootF).With(impact: 1.2f), s);
            m.Sweep = Make(Slot.Sweep, "Dragon Tail", S("TailSweep", P(30, 10, 60, 60, 110, -58, -118, 70, -30, 0, 0, 0.3f, 0, 0, 0.05f), P(36, 20, 60, 40, 60, -96, -94, 70, -40, 0, 0, 0.4f, 0, 0, 0.5f), P(30, 10, 60, 60, 110, -58, -118, 70, -30, 0, 0, 0.32f, 0, 0, 1f), Limb.FootB), s, Low);
            m.Dash = Make(Slot.Dash, "Flying Side Kick", S("FlyingKick", P(10, 40, 150, 70, 110, -30, -60, 60, -30, 0, 0, 0.12f), P(-30, 70, 140, -40, 0, 40, -30, 96, 93, 0, -8, -0.1f, 0, 0.25f), P(-20, 60, 140, 0, 60, 20, -40, 80, 60), Limb.FootF).With(ghosts: true, impact: 1.3f), s, KickMid);
            UnarmedAir(m, s);
            return m;
        }

        // ================= AGILE: fast hands, knees, flips
        static MoveSet Agile()
        {
            var s = MoveStyle.Agile;
            var m = new MoveSet();
            m.Light = new[]
            {
                Make(Slot.L1, "Quick Jab", S("QuickJab", P(14, -20, 40, 30, 160, -40, -80, 50, -20, 0, 0, 0.1f), P(26, -30, 30, 94, 90, -44, -76, 52, -4, 0, 0, 0.08f, 0, 0.12f), P(22, -30, 30, 84, 100, -42, -78, 52, -12, 0, 0, 0.1f), Limb.HandF), s),
                Make(Slot.L2, "Rising Knee", S("Knee", P(16, -40, 30, 20, 150, -30, -60, 40, -20, 0, 0, 0.05f), P(-4, 70, 20, 82, 25, -12, -18, 100, 8, 0, 4, 0, 0, 0.12f), P(4, 50, 40, 60, 60, -16, -24, 70, 0), Limb.FootF), s, Body),
                Make(Slot.L3, "Head Kick", S("HeadKick", P(10, -30, 30, 50, 120, -30, -60, 70, -30, 0, 0, 0.05f), P(-32, -50, -20, -40, 0, -6, -10, 128, 132, 0, -10), P(-20, -40, 0, -20, 30, -10, -16, 90, 60), Limb.FootF), s, KickHigh),
                Make(Slot.L4, "Axe Kick", A("AxeKick", P(-14, -40, -10, -30, 10, -10, -16, 172, 178, 0, -12), P(-4, -40, -10, -20, 20, -12, -18, 140, 150), P(26, -50, -20, -40, 0, -30, -50, 55, 40, 0, 8, 0.12f, 0, 0.1f), P(28, -40, -10, -30, 10, -34, -56, 45, 20, 0, 6, 0.14f), Limb.FootF).With(impact: 1.3f), s, KickMid),
            };
            AltFinisher[s] = S("Cartwheel Kick", P(20, -30, 30, 50, 120, -40, -80, 52, -26, 0, 0, 0.15f), P(10, 120, 160, 160, 190, -110, -120, 40, -20, -60, 0, -0.1f, 0, 0.1f, 0.5f), P(20, -30, 30, 50, 120, -42, -90, 52, -26, 0, 0, 0.12f, 0, 0, 1f), Limb.FootB);
            m.Heavy = Make(Slot.Heavy, "Flying Knee", S("FlyingKnee", P(26, -80, -60, -70, -50, -50, -100, 60, -30, 0, 0, 0.3f), P(4, -70, -40, -80, -50, -40, -80, 112, 10, 0, 6, -0.25f, 0, 0.45f), P(10, -50, -20, -40, -10, -30, -60, 80, 0, 0, 0, -0.05f), Limb.FootF).With(ghosts: true, impact: 1.5f), s, Body);
            m.Launcher = Make(Slot.Launch, "Backflip Kick", Clip.Keys("Backflip", Limb.FootF,
                (0f, P(20, -40, 30, 50, 120, -40, -80, 52, -26, 0, 0, 0.28f), Ease.Linear),
                (0.85f, P(26, -50, 20, 40, 110, -46, -96, 56, -30, 0, 0, 0.32f), Ease.InOut),
                (1f, P(-10, 150, 180, 160, 190, -20, -30, 150, 165, -110, -10, -0.1f), Ease.Snap),
                (1.9f, P(-6, 140, 170, 150, 180, 0, -30, 120, 120, -220, 0, -0.1f), Ease.Out),
                (2.6f, P(10, 120, 150, 140, 170, 20, -30, 60, 10, -360, 0, -0.05f), Ease.Out),
                (3f, P(16, -30, 30, 50, 120, -40, -80, 52, -26, -360, 0, 0.12f), Ease.InOut)).With(impact: 1.3f), s);
            m.Sweep = Make(Slot.Sweep, "Breaker Sweep", S("BreakSweep", P(34, 20, 40, 40, 60, -60, -120, 70, -34, 0, 0, 0.36f, 0, 0, 0.04f), P(40, 60, 20, 30, 20, -98, -96, 60, -60, 0, 0, 0.44f, 0, 0, 0.5f), P(34, 20, 40, 40, 60, -60, -120, 70, -34, 0, 0, 0.36f, 0, 0, 1f), Limb.FootB), s, Low);
            m.Dash = Make(Slot.Dash, "Dropkick", S("Dropkick", P(16, -40, 30, 50, 120, -40, -80, 52, -26, 0, 0, 0.2f), P(-42, -80, -40, -70, -30, 88, 86, 96, 94, -18, -10, -0.2f, 0, 0.35f), P(-24, -60, -20, -50, -10, 40, 20, 60, 40, 0, 0, -0.05f), Limb.FootF).With(ghosts: true, impact: 1.4f), s, KickMid);
            UnarmedAir(m, s);
            return m;
        }

        // ================= BRUTE: wide, heavy, slow and devastating
        static MoveSet Brute()
        {
            var s = MoveStyle.Brute;
            var m = new MoveSet();
            m.Light = new[]
            {
                Make(Slot.L1, "Wide Hook", S("WideHook", P(4, 15, 50, -60, 40, -28, -34, 32, 0), P(28, 20, 60, 82, 128, -40, -56, 40, 4, 0, 0, 0.04f, 0, 0.12f), P(32, 20, 60, 40, 150, -40, -56, 40, 4), Limb.HandF).With(impact: 1.1f), s),
                Make(Slot.L2, "Hammer Fist", A("HammerFist", P(-10, 195, 210, 30, 70, -24, -30, 30, 0, 0, -8), P(6, 150, 150, 30, 70, -30, -40, 34, 2), P(30, 70, 25, 30, 70, -42, -60, 44, 6, 0, 6, 0.14f, 0, 0.1f), P(34, 40, 0, 30, 70, -42, -60, 44, 6, 0, 6, 0.16f), Limb.HandB).With(impact: 1.15f), s),
                Make(Slot.L3, "Shoulder Ram", S("Ram", P(-8, 20, 60, 20, 70, -24, -30, 30, 0, 0, -6, 0.1f, 0, -0.1f), P(42, -20, 30, 10, 60, -62, -84, 52, 20, 0, 20, 0.04f, 0, 0.35f), P(36, -10, 40, 20, 70, -50, -70, 46, 12, 0, 12), Limb.None).With(impact: 1.2f), s, Body),
                Make(Slot.L4, "Double Axe Handle", A("AxeHandle", P(-22, 200, 215, 195, 210, -30, -34, 34, 0, 0, -10), P(0, 160, 165, 158, 165, -34, -40, 36, 0), P(42, 58, 40, 62, 44, -52, -96, 62, -22, 0, 16, 0.3f, 0, 0.12f), P(44, 50, 30, 52, 32, -52, -96, 62, -22, 0, 16, 0.3f), Limb.Hands).With(impact: 1.5f, slam: true), s, Body),
            };
            AltFinisher[s] = S("Clothesline", P(-6, 15, 50, -85, -80, -30, -40, 34, 0), P(32, 20, 50, 96, 96, -52, -76, 54, 14, 0, 4, 0.04f, 0, 0.32f), P(36, 20, 50, 120, 130, -50, -72, 52, 12), Limb.HandF);
            m.Heavy = Make(Slot.Heavy, "Giant Haymaker", A("GiantHay", P(-26, -135, -85, 60, 110, -40, -50, 50, 10, 0, -8, 0.18f, 0, -0.22f), P(6, -40, 40, 30, 100, -46, -62, 52, 10, 0, 0, 0.2f), P(44, 98, 95, -40, 10, -62, -84, 64, 16, 0, 6, 0.2f, 0, 0.48f), P(48, 90, 85, -50, 0, -60, -82, 62, 14, 0, 6, 0.2f), Limb.HandB).With(impact: 1.8f), s);
            m.Launcher = Make(Slot.Launch, "Giant Scoop", S("Scoop", P(36, 10, 20, 15, 25, -50, -100, 62, -22, 0, 10, 0.36f), P(-12, 160, 175, 165, 178, -18, -22, 16, -2, 0, -14, -0.05f), P(-14, 150, 170, 155, 172, -16, -20, 16, 0, 0, -10), Limb.Hands).With(impact: 1.4f), s);
            m.Sweep = Make(Slot.Sweep, "Quake Stomp", S("Stomp", P(-4, 15, 50, 25, 60, -24, -30, 88, 40, 0, -6, -0.05f), P(24, 15, 50, 25, 60, -40, -70, 48, 8, 0, 10, 0.2f), P(22, 15, 50, 25, 60, -40, -70, 48, 8, 0, 8, 0.2f), Limb.FootF).With(impact: 1.3f, slam: true), s, new Vector2(1.1f, 0.3f), 0.85f);
            m.Dash = Make(Slot.Dash, "Bull Charge", S("BullCharge", P(-6, 20, 60, 20, 70, -24, -30, 30, 0, 0, -6, 0.15f, 0, -0.1f), P(52, -10, 40, 0, 50, -76, -90, 50, 24, 0, 24, 0.05f, 0, 0.4f), P(44, -10, 40, 10, 60, -60, -80, 46, 16, 0, 16), Limb.None).With(ghosts: true, impact: 1.5f), s, Body, 0.75f);
            UnarmedAir(m, s);
            return m;
        }

        // ================= WRESTLER: chops, clinches, lariats, slams
        static MoveSet Wrestler()
        {
            var s = MoveStyle.Wrestler;
            var m = new MoveSet();
            m.Light = new[]
            {
                Make(Slot.L1, "Knife Chop", A("Chop", P(-6, 60, 120, 165, 205, -26, -36, 32, 0), P(8, 60, 120, 130, 150, -30, -44, 34, 2), P(24, 60, 120, 72, 40, -38, -56, 40, 6, 0, 0, 0, 0, 0.1f), P(28, 60, 120, 40, 10, -38, -56, 40, 6), Limb.HandF), s),
                Make(Slot.L2, "Clinch Knee", S("ClinchKnee", P(14, 80, 90, 85, 95, -30, -44, 36, 0), P(10, 80, 40, 85, 40, -14, -20, 100, 8, 0, 6, 0, 0, 0.1f), P(14, 80, 60, 85, 60, -20, -30, 60, 0), Limb.FootF), s, Body),
                Make(Slot.L3, "Headbutt", S("Headbutt", P(-14, 60, 120, 70, 110, -30, -40, 34, 0, 0, -20, 0, 0, -0.1f), P(40, 60, 100, 70, 100, -54, -74, 50, 12, 0, 34, 0, 0, 0.3f), P(32, 60, 110, 70, 105, -46, -66, 46, 8, 0, 20), Limb.None).With(impact: 1.2f), s),
                Make(Slot.L4, "Lariat", S("Lariat", P(-6, 40, 90, -85, -80, -30, -40, 34, 0, 0, 0, 0, 0, -0.05f), P(30, 30, 60, 96, 96, -50, -74, 52, 14, 0, 4, 0, 0, 0.32f), P(34, 20, 40, 120, 130, -48, -70, 50, 12), Limb.HandF).With(impact: 1.3f), s),
            };
            AltFinisher[s] = S("Elbow Drop", P(-10, 60, 120, 175, 210, -24, -30, 30, 0, 0, 0, -0.1f), P(40, 60, 120, 60, 180, -50, -100, 60, -20, 0, 14, 0.3f, 0, 0.1f), P(40, 60, 120, 60, 180, -50, -100, 60, -20, 0, 14, 0.3f), Limb.HandF);
            m.Heavy = Make(Slot.Heavy, "Body Slam", A("BodySlam", P(-14, 175, 190, 180, 195, -24, -30, 30, 0, 0, -10, -0.12f), P(20, 120, 110, 120, 110, -40, -60, 44, 0), P(56, 60, 20, 62, 18, -56, -110, 70, -20, 0, 24, 0.38f, 0, 0.2f), P(52, 55, 15, 58, 12, -56, -110, 70, -20, 0, 20, 0.36f), Limb.Hands).With(impact: 1.7f, slam: true), s, Body, 0.8f);
            m.Launcher = Make(Slot.Launch, "Suplex Toss", S("Suplex", P(30, 40, 60, 45, 60, -50, -90, 58, -20, 0, 6, 0.3f), P(-34, 175, 190, 178, 192, -10, -20, 30, 10, -30, -24, -0.08f), P(-24, 160, 180, 160, 180, -14, -24, 26, 6, -10, -16), Limb.Hands).With(impact: 1.4f), s);
            m.Sweep = Make(Slot.Sweep, "Ankle Pick", S("AnklePick", P(20, 60, 120, 70, 110, -40, -70, 50, -20, 0, 0, 0.2f), P(52, 50, 30, 60, 20, -70, -120, 70, -30, 0, 20, 0.36f, 0, 0.25f), P(44, 50, 40, 60, 30, -66, -118, 68, -30, 0, 16, 0.34f), Limb.HandF), s, Low);
            m.Dash = Make(Slot.Dash, "Spear Tackle", S("Spear", P(30, 60, 120, 70, 110, -50, -100, 60, -24, 0, 0, 0.25f), P(60, 70, 50, 72, 48, -80, -100, 50, 20, 0, 20, 0.15f, 0, 0.4f), P(50, 70, 60, 72, 60, -66, -90, 50, 16, 0, 14, 0.12f), Limb.None).With(ghosts: true, impact: 1.5f), s, Body, 0.75f);
            UnarmedAir(m, s);
            return m;
        }

        // ================= FERAL: claws, pounces, spins
        static MoveSet Feral()
        {
            var s = MoveStyle.Feral;
            var m = new MoveSet();
            var legs = (-32f, -72f, 42f, -22f);
            m.Light = new[]
            {
                Make(Slot.L1, "Claw Rake", A("Rake", P(4, 5, 30, 168, 205, -32, -60, 40, -16, 0, -14), P(16, 5, 30, 130, 150, -36, -66, 42, -18), P(36, 5, 30, 66, 35, -44, -76, 48, -10, 0, -10, 0.05f, 0, 0.12f), P(40, 5, 30, 20, -20, -46, -78, 48, -10, 0, -10, 0.06f), Limb.HandF), s),
                Make(Slot.L2, "Rising Swipe", S("RisingSwipe", P(36, -30, -50, 20, 40, -40, -90, 50, -30, 0, -16, 0.12f), P(14, 140, 165, 20, 40, -34, -60, 42, -10, 0, -20, 0, 0, 0.1f), P(10, 165, 190, 20, 40, -32, -58, 40, -10, 0, -20), Limb.HandB), s),
                Make(Slot.L3, "Pounce Bite", S("Pounce", P(20, 20, 20, 25, 25, -50, -100, 60, -30, 0, -10, 0.3f), P(56, 80, 40, 85, 45, -80, -90, 46, 20, 0, 30, 0.1f, 0, 0.35f), P(48, 60, 30, 65, 35, -66, -86, 46, 10, 0, 20, 0.1f), Limb.Hands), s, Body),
                Make(Slot.L4, "Whirling Claws", S("WhirlClaw", P(30, 10, 40, 20, 50, -40, -80, 46, -24, 0, -10, 0.12f, 0, 0, 0.06f), P(30, -100, -110, 100, 110, -36, -72, 44, -20, 0, -14, 0.06f, 0, 0.1f, 0.5f), P(30, 5, 30, 20, 40, legs.Item1, legs.Item2, legs.Item3, legs.Item4, 0, -20, 0.05f, 0, 0, 1f), Limb.Hands).With(impact: 1.2f), s),
            };
            AltFinisher[s] = S("Tail Lash", P(30, 5, 30, 20, 40, -40, -90, 50, -30, 0, -14, 0.2f, 0, 0, 0.08f), P(10, 40, 60, 50, 70, -112, -116, 20, -20, 0, -10, 0.1f, 0, 0.06f, 0.5f), P(30, 5, 30, 20, 40, -32, -72, 42, -22, 0, -20, 0.05f, 0, 0, 1f), Limb.FootB);
            m.Heavy = Make(Slot.Heavy, "Lunge Rake", A("LungeRake", P(14, -70, -40, -60, -30, -60, -118, 66, -32, 0, -6, 0.35f), P(40, 120, 140, 125, 145, -60, -90, 56, 0, 0, 0, 0.1f), P(52, 70, 30, 75, 35, -78, -94, 52, 22, 0, 20, 0.12f, 0, 0.45f), P(56, 40, 0, 45, 5, -76, -92, 52, 20, 0, 20, 0.12f), Limb.Hands).With(impact: 1.5f, ghosts: true), s);
            m.Launcher = Make(Slot.Launch, "Rising Claws", S("RisingClaws", P(40, 10, 10, 15, 15, -56, -110, 64, -28, 0, -10, 0.36f), P(-10, 160, 185, 165, 190, -20, -26, 16, -4, 0, -16, -0.06f), P(-12, 150, 175, 155, 180, -18, -24, 16, -2, 0, -12), Limb.Hands).With(impact: 1.3f), s);
            m.Sweep = Make(Slot.Sweep, "Tail Sweep", S("FeralSweep", P(36, 10, 30, 20, 40, -58, -118, 70, -30, 0, -10, 0.32f, 0, 0, 0.05f), P(44, 40, 20, 40, 20, -96, -94, 70, -40, 0, -6, 0.42f, 0, 0, 0.5f), P(36, 10, 30, 20, 40, -58, -118, 70, -30, 0, -10, 0.32f, 0, 0, 1f), Limb.FootB), s, Low);
            m.Dash = Make(Slot.Dash, "Predator Pounce", S("PredPounce", P(30, 10, 30, 20, 40, -56, -112, 64, -30, 0, -10, 0.34f), P(60, 90, 70, 92, 72, -84, -92, 40, 30, 0, 26, -0.1f, 0, 0.4f), P(50, 70, 40, 74, 44, -66, -88, 44, 16, 0, 16, 0.05f), Limb.Hands).With(ghosts: true, impact: 1.4f), s);
            UnarmedAir(m, s);
            return m;
        }

        // ================= ELEGANT: slaps, high kicks, spinning heels
        static MoveSet Elegant()
        {
            var s = MoveStyle.Elegant;
            var m = new MoveSet();
            m.Light = new[]
            {
                Make(Slot.L1, "Backhand", S("Backhand", P(-6, -12, 12, 30, 175, -10, -14, 12, 2, 0, -6), P(8, -12, 12, 92, 110, -18, -26, 22, 4, 0, -4, 0, 0, 0.08f), P(4, -12, 12, 100, 140, -16, -22, 18, 4, 0, -4), Limb.HandF), s),
                Make(Slot.L2, "High Kick", S("HighKick", P(-6, -20, 10, 0, 40, -10, -14, 60, -30, 0, -6), P(-36, -40, -10, -30, 10, -4, -8, 130, 134, 0, -8), P(-24, -30, 0, -20, 20, -6, -10, 90, 70, 0, -6), Limb.FootF), s, KickHigh),
                Make(Slot.L3, "Spinning Heel", S("SpinHeel", P(0, -12, 12, -8, 18, -10, -14, 12, 2, 0, -6, 0, 0, 0, 0.06f), P(-24, 30, 80, 40, 80, -112, -118, 8, -8, 0, -6, 0, 0, 0.06f, 0.5f), P(-6, -12, 12, -8, 18, -12, -16, 12, 2, 0, -6, 0, 0, 0, 1f), Limb.FootB), s, KickHigh),
                Make(Slot.L4, "Palm Thrust", S("PalmThrust", P(-8, -12, 12, -30, 60, -14, -20, 16, 0, 0, -6, 0, 0, -0.05f), P(22, -30, 10, 92, 95, -40, -58, 42, 6, 0, 0, 0.1f, 0, 0.28f), P(18, -30, 10, 88, 98, -36, -54, 40, 4, 0, 0, 0.08f), Limb.HandF).With(impact: 1.2f), s),
            };
            AltFinisher[s] = S("Butterfly Kick", P(10, -12, 12, -8, 18, -30, -60, 40, -20, 0, -6, 0.2f, 0, 0, 0.06f), P(-10, 120, 140, 130, 150, -100, -106, 100, 100, -40, -8, -0.15f, 0, 0.1f, 0.5f), P(-4, -12, 12, -8, 18, -10, -12, 12, 2, 0, -6, 0, 0, 0, 1f), Limb.FootB);
            m.Heavy = Make(Slot.Heavy, "Crescent Axe", A("CrescentAxe", P(-18, -40, 0, -20, 30, -8, -12, 176, 182, 0, -10), P(-6, -40, 0, -20, 30, -10, -14, 140, 150), P(24, -50, -10, -30, 10, -30, -52, 52, 38, 0, 6, 0.12f, 0, 0.12f), P(26, -40, 0, -20, 20, -32, -54, 46, 24, 0, 6, 0.14f), Limb.FootF).With(impact: 1.5f), s, KickMid);
            m.Launcher = Make(Slot.Launch, "Rising Pirouette", S("Pirouette", P(10, -12, 12, -8, 18, -40, -80, 50, -24, 0, -6, 0.25f, 0, 0, 0.04f), P(-30, 30, 100, 60, 130, -14, -20, 170, 175, 0, -10, 0, 0, 0, 0.5f), P(-16, -12, 12, -8, 18, -12, -16, 110, 90, 0, -6, 0, 0, 0, 1f), Limb.FootF).With(impact: 1.2f), s);
            m.Sweep = Make(Slot.Sweep, "Low Crescent", S("LowCrescent", P(28, -12, 12, 30, 60, -58, -118, 70, -30, 0, -6, 0.3f, 0, 0, 0.05f), P(34, 20, 60, 40, 60, -96, -94, 70, -40, 0, -6, 0.4f, 0, 0, 0.5f), P(28, -12, 12, 30, 60, -58, -118, 70, -30, 0, -6, 0.32f, 0, 0, 1f), Limb.FootB), s, Low);
            m.Dash = Make(Slot.Dash, "Gliding Palm", S("GlidePalm", P(-6, -12, 12, -30, 60, -16, -22, 18, 0, 0, -6, 0.05f), P(30, -40, -20, 94, 92, -60, -80, 30, 10, 0, -4, 0, 0, 0.4f), P(24, -30, -10, 90, 96, -50, -70, 34, 8, 0, -4), Limb.HandF).With(ghosts: true, impact: 1.3f), s);
            UnarmedAir(m, s);
            return m;
        }

        // ================= CASTER: palms, elbows and cursed-energy bursts
        static MoveSet Caster()
        {
            var s = MoveStyle.Caster;
            var m = new MoveSet();
            m.Light = new[]
            {
                Make(Slot.L1, "Energy Palm", S("EPalm", P(-2, 10, 60, -20, 70, -18, -26, 20, 0), P(16, 10, 60, 92, 82, -30, -44, 32, 4, 0, 0, 0, 0, 0.1f), P(12, 10, 60, 86, 90, -26, -40, 28, 2), Limb.HandF), s),
                Make(Slot.L2, "Elbow", S("Elbow", P(-4, -50, 40, 60, 112, -20, -28, 22, 0), P(24, 85, 160, 40, 120, -36, -52, 38, 4, 0, 4, 0, 0, 0.15f), P(20, 75, 150, 45, 120, -32, -48, 34, 2), Limb.HandB), s, Body),
                Make(Slot.L3, "Push Kick", S("PushKick", P(0, 10, 60, 60, 112, -18, -24, 86, -20), P(-12, 10, 60, 60, 112, -14, -18, 94, 92, 0, 0, 0, 0, 0.1f), P(-6, 10, 60, 60, 112, -16, -22, 66, 10), Limb.FootF), s, KickMid),
                Make(Slot.L4, "Burst Palm", S("BurstPalm", P(-10, -50, 30, -40, 40, -24, -36, 34, -4, 0, 4, 0.12f), P(20, 88, 90, 92, 88, -40, -60, 44, 8, 0, 0, 0.1f, 0, 0.18f), P(22, 86, 92, 90, 92, -42, -62, 44, 8, 0, 0, 0.1f), Limb.Hands).With(impact: 1.2f), s),
            };
            m.Light[3].Clip.Aura = true;
            AltFinisher[s] = S("Spinning Elbow", P(4, 10, 60, 60, 112, -20, -28, 22, 0, 0, 0, 0, 0, 0, 0.06f), P(16, -60, -150, 40, 120, -32, -48, 34, 2, 0, 0, 0, 0, 0.12f, 0.5f), P(8, 10, 60, 60, 112, -20, -28, 22, 0, 0, 0, 0, 0, 0, 1f), Limb.HandB);
            m.Heavy = Make(Slot.Heavy, "Aura Blast", S("AuraBlast", P(-12, -55, 30, -45, 40, -26, -40, 36, -6, 0, 6, 0.16f), P(22, 90, 92, 92, 90, -44, -64, 46, 8, 0, 0, 0.12f, 0, 0.2f), P(26, 88, 94, 90, 94, -46, -66, 46, 8, 0, 0, 0.12f), Limb.Hands).With(impact: 1.5f), s);
            m.Heavy.Clip.Aura = true;
            m.Launcher = Make(Slot.Launch, "Rising Palm", S("RisingPalm", P(26, 10, 40, 10, 50, -48, -96, 60, -26, 0, 8, 0.3f), P(-10, 165, 180, 168, 182, -18, -22, 14, -2, 0, -14, -0.05f), P(-12, 158, 176, 160, 178, -16, -20, 14, 0, 0, -10), Limb.Hands).With(impact: 1.2f), s);
            m.Sweep = Make(Slot.Sweep, "Low Kick", S("LowKick", P(20, 10, 60, 60, 112, -50, -96, 60, -26, 0, 0, 0.26f), P(30, 10, 60, 60, 112, -60, -120, 92, 90, 0, 0, 0.34f, 0, 0.05f), P(26, 10, 60, 60, 112, -56, -112, 70, 40, 0, 0, 0.3f), Limb.FootF), s, Low);
            m.Dash = Make(Slot.Dash, "Phantom Palm", S("PhantomPalm", P(-6, 10, 60, -30, 60, -18, -26, 20, 0, 0, 0, 0.05f), P(34, -40, -20, 94, 90, -66, -84, 36, 14, 0, 0, 0, 0, 0.4f), P(28, -30, -10, 90, 94, -54, -74, 36, 10), Limb.HandF).With(ghosts: true, impact: 1.3f), s);
            UnarmedAir(m, s);
            return m;
        }

        // ================= BLADE: katana and sword arcs (blade angle = forearm + wrist)
        static MoveSet Blade()
        {
            var s = MoveStyle.Blade;
            var m = new MoveSet();
            m.Light = new[]
            {
                Make(Slot.L1, "Falling Cut", A("FallingCut", P(-8, 30, 110, 170, 205, -20, -30, 30, 0, 0, -6, 0, 35), P(8, 30, 110, 140, 150, -28, -44, 36, 4, 0, 0, 0, 30), P(26, 30, 110, 75, 55, -44, -64, 46, 10, 0, 0, 0.04f, 20, 0.15f), P(32, 30, 110, 30, 10, -46, -66, 48, 10, 0, 0, 0.08f, 0), Limb.Weapon), s),
                Make(Slot.L2, "Rising Cut", A("RisingCut", P(24, 30, 110, -20, -40, -46, -80, 52, -10, 0, 0, 0.14f, -25), P(20, 30, 110, 30, 30, -40, -60, 46, 0, 0, 0, 0.08f, -10), P(-6, 30, 110, 125, 150, -22, -30, 28, 0, 0, -6, 0, 25, 0.1f), P(-12, 30, 110, 150, 175, -18, -26, 24, 0, 0, -8, 0, 30), Limb.Weapon), s),
                Make(Slot.L3, "Piercing Thrust", S("Thrust", P(-4, 30, 110, 15, 40, -20, -30, 26, 0, 0, 0, 0, 50, -0.1f), P(30, 20, 100, 92, 90, -52, -68, 56, 14, 0, 0, 0.04f, 0, 0.32f), P(26, 20, 100, 88, 92, -48, -64, 52, 12, 0, 0, 0.04f, 2), Limb.Weapon).With(twoHand: 0), s, new Vector2(1.05f, 1.3f)),
                Make(Slot.L4, "Whirlwind Cut", S("WhirlCut", P(10, 30, 110, 40, 60, -30, -50, 36, 0, 0, 0, 0.12f, 30, 0, 0.05f), P(20, 30, 110, -92, -90, -40, -60, 44, 6, 0, 0, 0.1f, 0, 0.1f, 0.5f), P(14, 30, 110, 60, 80, -34, -56, 40, 0, 0, 0, 0.08f, 30, 0, 1f), Limb.Weapon).With(twoHand: 0, impact: 1.3f), s),
            };
            AltFinisher[s] = A("Cross Cut", P(-10, 200, 230, 170, 205, -22, -30, 30, 0, 0, -8, 0, 30), P(6, 150, 160, 140, 150, -30, -44, 36, 2, 0, 0, 0, 25), P(30, 40, 30, 70, 50, -48, -68, 50, 10, 0, 0, 0.1f, 15, 0.18f), P(34, 20, 0, 30, 10, -48, -68, 50, 10, 0, 0, 0.12f, 0), Limb.Weapon);
            m.Heavy = Make(Slot.Heavy, "Heaven Splitter", A("Splitter", P(-20, 30, 110, 195, 225, -22, -30, 34, 0, 0, -10, 0.04f, 35), P(4, 30, 110, 160, 175, -30, -44, 38, 4, 0, 0, 0, 20), P(40, 30, 110, 70, 45, -56, -80, 60, 14, 0, 6, 0.22f, 15, 0.4f), P(44, 30, 110, 40, 10, -56, -80, 60, 14, 0, 6, 0.24f, 5), Limb.Weapon).With(twoHand: 1, impact: 1.7f), s);
            m.Launcher = Make(Slot.Launch, "Moon Rise", A("MoonRise", P(34, 30, 110, -30, -60, -56, -110, 66, -28, 0, 6, 0.34f, -20), P(20, 30, 110, 40, 40, -40, -70, 50, -10, 0, 0, 0.15f, -5), P(-14, 30, 110, 160, 185, -16, -22, 14, -4, 0, -14, -0.06f, 15), P(-16, 30, 110, 170, 195, -14, -20, 14, 0, 0, -12, 0, 20), Limb.Weapon).With(twoHand: 1, impact: 1.3f), s);
            m.Sweep = Make(Slot.Sweep, "Ground Cut", S("GroundCut", P(26, 30, 110, -20, -50, -58, -118, 70, -30, 0, 0, 0.32f, -20), P(40, 30, 110, 70, 85, -64, -126, 80, -30, 0, 0, 0.4f, -10, 0.1f), P(40, 30, 110, 110, 130, -62, -122, 80, -30, 0, 0, 0.38f, 0), Limb.Weapon), s, Low);
            m.Dash = Make(Slot.Dash, "Flash Draw", S("FlashDraw", P(20, 30, 110, 0, -30, -40, -70, 50, -20, 0, 0, 0.15f, -50), P(46, 30, 110, 96, 100, -80, -92, 44, 26, 0, 6, 0, -5, 0.45f), P(40, 30, 110, 120, 150, -60, -76, 44, 14, 0, 0, 0, 20), Limb.Weapon).With(ghosts: true, twoHand: 0, impact: 1.5f), s, new Vector2(1.0f, 1.25f));
            m.Air = new[]
            {
                Make(Slot.Air1, "Air Cut", A("AirCut", P(-8, 30, 110, 170, 200, 40, -40, 70, 0, 0, -6, 0, 30), P(6, 30, 110, 140, 150, 40, -40, 70, 0, 0, 0, 0, 25), P(24, 30, 110, 72, 50, 30, -40, 60, -10, 0, 0, 0, 15), P(28, 30, 110, 30, 10, 30, -40, 60, -10, 0, 0, 0, 0), Limb.Weapon), s),
                Make(Slot.Air2, "Air Rising Cut", A("AirRise", P(20, 30, 110, -20, -40, 40, -40, 70, 0, 0, 0, 0, -25), P(14, 30, 110, 40, 40, 40, -40, 70, 0, 0, 0, 0, -10), P(-10, 30, 110, 130, 160, 30, -40, 60, -10, 0, -6, 0, 25), P(-14, 30, 110, 150, 175, 30, -40, 60, -10, 0, -8, 0, 30), Limb.Weapon), s),
            };
            m.AirHeavy = Make(Slot.AirHeavy, "Somersault Slash", Clip.Keys("SomerSlash", Limb.Weapon,
                (0f, P(-10, 30, 110, 180, 210, 40, -40, 70, 0, 0, -6, 0, 30), Ease.Linear),
                (0.85f, P(-14, 30, 110, 185, 215, 50, -30, 80, 0, 0, -8, 0, 32), Ease.InOut),
                (1f, P(20, 30, 110, 70, 45, 50, -30, 80, 10, 200, 0, 0, 15), Ease.Snap),
                (1.9f, P(20, 30, 110, 60, 35, 50, -30, 80, 10, 300, 0, 0, 10), Ease.Out),
                (2.6f, P(10, 30, 110, 60, 80, 40, -40, 70, 0, 360, 0, 0, 30), Ease.Out),
                (3f, P(10, 30, 110, 60, 80, 40, -40, 70, 0, 360, 0, 0, 30), Ease.Linear)).With(twoHand: 1, impact: 1.4f, ghosts: true), s);
            m.Dive = Make(Slot.Dive, "Plunging Blade", S("Plunge", P(-10, 30, 110, 170, 195, 50, -20, 80, 0, 0, -6, -0.05f, 0), P(20, 30, 110, 20, 0, 40, -20, 60, 20, 0, 10, 0, 0), P(20, 30, 110, 20, 0, 40, -20, 60, 20, 0, 10, 0, 0), Limb.Weapon).With(twoHand: 1, ghosts: true, impact: 1.4f), s);
            return m;
        }

        // ================= STAFF: monkey-king twirls, vaults and earth-shaking slams
        static MoveSet Staff()
        {
            var s = MoveStyle.Staff;
            var m = new MoveSet();
            m.Light = new[]
            {
                Make(Slot.L1, "Staff Thrust", S("StaffThrust", P(-4, 30, 110, 20, 40, -22, -34, 28, 0, 0, 0, 0, 50, -0.1f), P(24, 30, 110, 92, 90, -46, -64, 50, 10, 0, 0, 0.04f, 0, 0.28f), P(20, 30, 110, 88, 92, -42, -60, 48, 8, 0, 0, 0.04f, 4), Limb.Weapon), s, new Vector2(1.1f, 1.3f)),
                Make(Slot.L2, "Overhead Whack", A("Whack", P(-12, 30, 110, 170, 195, -22, -30, 30, 0, 0, -8, 0, 20), P(6, 30, 110, 140, 150, -30, -44, 36, 2, 0, 0, 0, 20), P(28, 30, 110, 75, 60, -40, -60, 46, 8, 0, 4, 0.1f, 10, 0.12f), P(32, 30, 110, 50, 30, -42, -62, 46, 8, 0, 4, 0.12f, 0), Limb.Weapon).With(impact: 1.15f), s),
                Make(Slot.L3, "Wheel Twirl", Clip.Keys("Twirl", Limb.Weapon,
                    (0f, P(6, 30, 110, 70, 85, -26, -40, 30, 0, 0, 0, 0.04f, 0), Ease.Linear),
                    (0.8f, P(4, 30, 110, 75, 90, -26, -40, 30, 0, 0, 0, 0.04f, -40), Ease.InOut),
                    (1f, P(14, 30, 110, 85, 95, -34, -50, 36, 4, 0, 0, 0.06f, 200, 0.1f), Ease.Out),
                    (1.85f, P(16, 30, 110, 88, 95, -36, -52, 38, 4, 0, 0, 0.06f, 560, 0.1f), Ease.Linear),
                    (2.6f, P(10, 30, 110, 60, 80, -30, -46, 34, 2, 0, 0, 0.05f, 750), Ease.Out),
                    (3f, P(10, 30, 110, 50, 80, -30, -50, 36, -6, 0, 0, 0.08f, 750), Ease.Linear)).With(twoHand: 0, impact: 1.1f), s, new Vector2(0.95f, 1.3f), 0.75f),
                Make(Slot.L4, "Pole Vault Kick", S("VaultKick", P(14, 30, 110, 60, 40, -40, -80, 50, -20, 0, 0, 0.25f, -60), P(-40, 60, 40, 40, 20, 90, 88, 98, 95, -25, -6, -0.35f, -30, 0.2f), P(-20, 40, 60, 50, 40, 30, -10, 60, 20, 0, 0, -0.1f, -20), Limb.FootF).With(twoHand: 1, impact: 1.4f), s, KickMid),
            };
            AltFinisher[s] = S("Sweeping Staff", P(10, 30, 110, 40, 60, -30, -50, 36, 0, 0, 0, 0.12f, 30, 0, 0.05f), P(18, 30, 110, -90, -92, -40, -60, 44, 6, 0, 0, 0.12f, 0, 0.1f, 0.5f), P(12, 30, 110, 50, 80, -30, -50, 36, -6, 0, 0, 0.08f, 30, 0, 1f), Limb.Weapon);
            m.Heavy = Make(Slot.Heavy, "Mountain Breaker", Clip.Keys("Breaker", Limb.Weapon,
                (0f, P(30, 30, 110, 0, -30, -56, -112, 68, -30, 0, 6, 0.32f, -40), Ease.Linear),
                (0.6f, P(-14, 30, 110, 175, 200, -10, -60, 70, -20, 0, -10, -0.45f, 20), Ease.Out),
                (0.9f, P(6, 30, 110, 150, 165, -30, -70, 70, -10, 0, 0, -0.3f, 20), Ease.InOut),
                (1f, P(46, 30, 110, 72, 40, -58, -112, 68, -26, 0, 14, 0.34f, 25, 0.35f), Ease.Snap),
                (1.85f, P(46, 30, 110, 68, 36, -58, -112, 68, -26, 0, 14, 0.34f, 22, 0.35f), Ease.Out),
                (2.55f, P(40, 30, 110, 60, 30, -54, -108, 66, -26, 0, 10, 0.3f, 20, 0.3f), Ease.Out),
                (3f, P(30, 30, 110, 55, 40, -48, -96, 60, -22, 0, 6, 0.24f, 30, 0.2f), Ease.Linear)).With(twoHand: 1, impact: 1.9f, slam: true), s, new Vector2(1.1f, 1.0f), 0.85f);
            m.Launcher = Make(Slot.Launch, "Sky Flip", A("SkyFlip", P(30, 30, 110, 30, 10, -50, -100, 60, -24, 0, 6, 0.3f, -50), P(20, 30, 110, 60, 50, -40, -70, 50, -10, 0, 0, 0.15f, -20), P(-10, 30, 110, 140, 170, -18, -24, 16, -2, 0, -14, -0.04f, 20), P(-12, 30, 110, 150, 180, -16, -22, 16, 0, 0, -12, 0, 30), Limb.Weapon).With(twoHand: 1, impact: 1.3f), s);
            m.Sweep = Make(Slot.Sweep, "Staff Sweep", S("StaffSweep", P(30, 30, 110, 40, 60, -58, -118, 70, -30, 0, 0, 0.32f, 30, 0, 0.04f), P(34, 30, 110, -88, -90, -60, -118, 70, -30, 0, 0, 0.4f, 0, 0, 0.5f), P(30, 30, 110, 40, 60, -58, -118, 70, -30, 0, 0, 0.34f, 30, 0, 1f), Limb.Weapon).With(twoHand: 0), s, new Vector2(1.15f, 0.35f), 0.7f);
            m.Dash = Make(Slot.Dash, "Ruyi Lunge", S("RuyiLunge", P(10, 30, 110, 10, 30, -30, -56, 48, -20, 0, 0, 0.1f, 60), P(42, 30, 110, 95, 92, -82, -90, 46, 28, 0, 4, 0, 0, 0.5f), P(36, 30, 110, 90, 94, -66, -80, 44, 16, 0, 0, 0, 2), Limb.Weapon).With(ghosts: true, impact: 1.4f), s, new Vector2(1.2f, 1.25f));
            m.Air = new[]
            {
                Make(Slot.Air1, "Air Thrust", S("AirThrust", P(-4, 30, 110, 20, 40, 40, -40, 70, 0, 0, 0, 0, 50), P(16, 30, 110, 80, 70, 30, -40, 60, -10, 0, 0, 0, 0, 0.1f), P(12, 30, 110, 76, 72, 30, -40, 60, -10, 0, 0, 0, 4), Limb.Weapon), s),
                Make(Slot.Air2, "Air Twirl", Clip.Keys("AirTwirl", Limb.Weapon,
                    (0f, P(4, 30, 110, 70, 85, 40, -40, 70, 0, 0, 0, 0, 0), Ease.Linear),
                    (0.8f, P(4, 30, 110, 75, 90, 40, -40, 70, 0, 0, 0, 0, -40), Ease.InOut),
                    (1f, P(10, 30, 110, 85, 95, 40, -40, 70, 0, 0, 0, 0, 200), Ease.Out),
                    (1.85f, P(10, 30, 110, 88, 95, 40, -40, 70, 0, 0, 0, 0, 560), Ease.Linear),
                    (3f, P(6, 30, 110, 60, 80, 40, -40, 70, 0, 0, 0, 0, 720), Ease.Out)).With(twoHand: 0), s, null, 0.75f),
            };
            m.AirHeavy = Make(Slot.AirHeavy, "Falling Mountain", A("AirSlam", P(-16, 30, 110, 175, 200, 40, -30, 70, 0, 0, -10, 0, 20), P(6, 30, 110, 145, 155, 40, -30, 70, 0, 0, 0, 0, 20), P(36, 30, 110, 70, 40, 20, -40, 60, -10, 0, 12, 0, 20, 0.1f), P(40, 30, 110, 60, 30, 20, -40, 60, -10, 0, 12, 0, 15), Limb.Weapon).With(twoHand: 1, impact: 1.5f), s);
            m.Dive = Make(Slot.Dive, "Pillar Drop", S("PillarDrop", P(-10, 30, 110, 170, 195, 50, -20, 80, 0, 0, -6, -0.05f, 0), P(20, 30, 110, 20, 0, 40, -20, 60, 20, 0, 10, 0, 0), P(20, 30, 110, 20, 0, 40, -20, 60, 20, 0, 10, 0, 0), Limb.Weapon).With(twoHand: 1, ghosts: true, impact: 1.5f), s);
            return m;
        }

        // ================= HEAVY WEAPON: hammers, gavels, guitars, axes
        static MoveSet HeavyWeapon()
        {
            var s = MoveStyle.HeavyWeapon;
            var m = new MoveSet();
            m.Light = new[]
            {
                Make(Slot.L1, "Side Swing", A("SideSwing", P(-8, 30, 110, -60, -30, -24, -32, 30, 0, 0, -4, 0, -40), P(10, 30, 110, 30, 40, -32, -48, 38, 2, 0, 0, 0, 0), P(28, 30, 110, 85, 100, -44, -64, 46, 8, 0, 0, 0.04f, 10, 0.12f), P(32, 30, 110, 110, 150, -44, -64, 46, 8, 0, 0, 0.04f, 20), Limb.Weapon).With(impact: 1.15f), s),
                Make(Slot.L2, "Backswing", A("Backswing", P(24, 30, 110, 120, 160, -36, -56, 40, 4, 0, 0, 0, 20), P(20, 30, 110, 100, 110, -36, -56, 40, 4, 0, 0, 0, 10), P(18, 30, 110, 70, 40, -34, -50, 38, 2, 0, 0, 0.06f, 0, 0.08f), P(8, 30, 110, 0, -40, -30, -40, 34, 0, 0, 0, 0.06f, -20), Limb.Weapon), s),
                Make(Slot.L3, "Overhead Smash", A("OverSmash", P(-18, 30, 110, 190, 215, -22, -30, 30, 0, 0, -10, 0, 25), P(4, 30, 110, 150, 160, -30, -44, 36, 2, 0, 0, 0, 20), P(40, 30, 110, 68, 35, -50, -90, 58, -14, 0, 10, 0.26f, 20, 0.14f), P(40, 30, 110, 62, 30, -50, -90, 58, -14, 0, 10, 0.26f, 18), Limb.Weapon).With(impact: 1.4f, slam: true), s),
                Make(Slot.L4, "Spinning Swing", S("SpinSwing", P(10, 30, 110, 40, 60, -30, -50, 36, 0, 0, 0, 0.12f, 30, 0, 0.05f), P(22, 30, 110, -92, -90, -42, -62, 46, 6, 0, 0, 0.12f, 0, 0.12f, 0.5f), P(14, 30, 110, 40, 70, -34, -56, 40, 0, 0, 0, 0.08f, 60, 0, 1f), Limb.Weapon).With(impact: 1.4f), s),
            };
            AltFinisher[s] = A("Uppercut Swing", P(30, 30, 110, -10, -40, -50, -100, 60, -24, 0, 6, 0.3f, -40), P(20, 30, 110, 50, 50, -40, -70, 50, -10, 0, 0, 0.15f, -10), P(-14, 30, 110, 150, 180, -18, -24, 16, -2, 0, -14, -0.04f, 20), P(-16, 30, 110, 160, 190, -16, -22, 16, 0, 0, -12, 0, 25), Limb.Weapon);
            m.Heavy = Make(Slot.Heavy, "Earthshaker", Clip.Keys("Earthshaker", Limb.Weapon,
                (0f, P(30, 30, 110, 0, -30, -56, -112, 68, -30, 0, 6, 0.32f, -40), Ease.Linear),
                (0.6f, P(-16, 30, 110, 180, 210, -10, -60, 70, -20, 0, -10, -0.42f, 25), Ease.Out),
                (0.9f, P(6, 30, 110, 150, 165, -30, -70, 70, -10, 0, 0, -0.28f, 20), Ease.InOut),
                (1f, P(48, 30, 110, 70, 35, -58, -112, 68, -26, 0, 16, 0.36f, 20, 0.3f), Ease.Snap),
                (1.85f, P(48, 30, 110, 66, 32, -58, -112, 68, -26, 0, 16, 0.36f, 18, 0.3f), Ease.Out),
                (2.55f, P(42, 30, 110, 60, 30, -54, -108, 66, -26, 0, 10, 0.3f, 16, 0.25f), Ease.Out),
                (3f, P(30, 30, 110, 40, 60, -46, -90, 58, -20, 0, 6, 0.22f, 50, 0.15f), Ease.Linear)).With(impact: 2f, slam: true), s, new Vector2(1.05f, 1.0f), 0.9f);
            m.Launcher = Make(Slot.Launch, "Golf Swing", A("GolfSwing", P(30, 30, 110, -10, -40, -50, -100, 60, -24, 0, 6, 0.3f, -40), P(20, 30, 110, 50, 50, -40, -70, 50, -10, 0, 0, 0.15f, -10), P(-14, 30, 110, 150, 180, -18, -24, 16, -2, 0, -14, -0.04f, 20), P(-16, 30, 110, 160, 190, -16, -22, 16, 0, 0, -12, 0, 25), Limb.Weapon).With(impact: 1.4f), s);
            m.Sweep = Make(Slot.Sweep, "Ankle Breaker", S("LowSwing", P(26, 30, 110, -20, -50, -58, -118, 70, -30, 0, 0, 0.32f, -30), P(40, 30, 110, 70, 85, -64, -126, 80, -30, 0, 0, 0.4f, 0, 0.1f), P(40, 30, 110, 110, 130, -62, -122, 80, -30, 0, 0, 0.38f, 10), Limb.Weapon), s, Low);
            m.Dash = Make(Slot.Dash, "Charging Swing", A("ChargeSwing", P(14, 30, 110, -50, -30, -36, -66, 50, -20, 0, 0, 0.12f, -40), P(30, 30, 110, 30, 40, -60, -80, 46, 10, 0, 0, 0.05f, 0, 0.2f), P(44, 30, 110, 88, 100, -80, -92, 44, 26, 0, 6, 0, 10, 0.45f), P(40, 30, 110, 110, 150, -64, -80, 44, 16, 0, 0, 0, 20), Limb.Weapon).With(ghosts: true, impact: 1.5f), s);
            m.Air = new[]
            {
                Make(Slot.Air1, "Air Swing", A("AirSwing", P(-8, 30, 110, -60, -30, 40, -40, 70, 0, 0, 0, 0, -40), P(8, 30, 110, 30, 40, 40, -40, 70, 0, 0, 0, 0, 0), P(24, 30, 110, 85, 100, 30, -40, 60, -10, 0, 0, 0, 10), P(28, 30, 110, 110, 150, 30, -40, 60, -10, 0, 0, 0, 20), Limb.Weapon), s),
                Make(Slot.Air2, "Air Backswing", A("AirBack", P(20, 30, 110, 120, 160, 40, -40, 70, 0, 0, 0, 0, 20), P(16, 30, 110, 100, 110, 40, -40, 70, 0, 0, 0, 0, 10), P(14, 30, 110, 70, 40, 30, -40, 60, -10, 0, 0, 0, 0), P(6, 30, 110, 0, -40, 30, -40, 60, -10, 0, 0, 0, -20), Limb.Weapon), s),
            };
            m.AirHeavy = Make(Slot.AirHeavy, "Meteor Hammer", A("AirSmash", P(-16, 30, 110, 180, 205, 40, -30, 70, 0, 0, -10, 0, 25), P(6, 30, 110, 145, 155, 40, -30, 70, 0, 0, 0, 0, 20), P(36, 30, 110, 70, 40, 20, -40, 60, -10, 0, 12, 0, 20, 0.1f), P(40, 30, 110, 60, 30, 20, -40, 60, -10, 0, 12, 0, 15), Limb.Weapon).With(impact: 1.6f), s);
            m.Dive = Make(Slot.Dive, "Crater Drop", S("CraterDrop", P(-10, 30, 110, 170, 195, 50, -20, 80, 0, 0, -6, -0.05f, 20), P(20, 30, 110, 40, 10, 40, -20, 60, 20, 0, 10, 0, 10), P(20, 30, 110, 40, 10, 40, -20, 60, 20, 0, 10, 0, 10), Limb.Weapon).With(ghosts: true, impact: 1.6f), s);
            return m;
        }
    }
}
