using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectSorcery
{
    /// <summary>
    /// Every fighter in the game. All names and technique names are original; kits are inspired by
    /// how classic cursed techniques behave. Split across partial files by story arc.
    /// Balance guide (per fighter): S1 ~15-25 CE / 3-5s / 50-80 dmg, S2 ~25-35 CE / 6-9s,
    /// S3 ~40-60 CE / 10-16s / big payoff, ULT = domain (100 CE) or a 70-90 CE super.
    /// </summary>
    public static partial class Roster
    {
        static List<CharacterDef> all;
        public static List<CharacterDef> All { get { Build(); return all; } }
        public static int Count => All.Count;
        public static int BossIndex { get; private set; } = -1;

        public static CharacterDef Get(int i)
        {
            var a = All;
            return a[Mathf.Clamp(i, 0, a.Count - 1)];
        }

        public static int IndexOf(string id)
        {
            var a = All;
            for (int i = 0; i < a.Count; i++) if (a[i].Id == id) return i;
            return 0;
        }

        public static CharacterDef RandomPlayable(Rng rng)
        {
            var a = All;
            for (int guard = 0; guard < 100; guard++)
            {
                var d = a[rng.Range(0, a.Count)];
                if (!d.BossOnly) return d;
            }
            return a[0];
        }

        public static CharacterDef RandomTier(Rng rng, int tier)
        {
            var pool = new List<CharacterDef>();
            foreach (var d in All) if (!d.BossOnly && d.Tier == tier) pool.Add(d);
            if (pool.Count == 0) return RandomPlayable(rng);
            return pool[rng.Range(0, pool.Count)];
        }

        public static int RandomPlayableIndex(System.Random r)
        {
            var a = All;
            for (int guard = 0; guard < 100; guard++)
            {
                int i = r.Next(a.Count);
                if (!a[i].BossOnly) return i;
            }
            return 0;
        }

        static void Build()
        {
            if (all != null) return;
            all = new List<CharacterDef>();
            AddTokyo();
            AddKyoto();
            AddAdults();
            AddCurses();
            AddCurseUsers();
            AddCullingGame();
            AddClans();
            AddCalamity();
            AddModulo();
            for (int i = 0; i < all.Count; i++)
            {
                all[i].Index = i;
                if (all[i].BossOnly) BossIndex = i;
                if (ProjectSorcery.Balance.Tuning.TryGetValue(all[i].Id, out var t)) { all[i].DamageMul = t.dmg; all[i].ToughMul = t.tough; }
            }
        }

        // ---------------------------------------------------------------- helpers
        static Color H(string hex) => Art.Hex(hex);

        static CharacterDef C(string id, string name, string title, string technique, Era era, Stance stance,
                              Hair hair, FaceMark face, Weapon weapon, string aura, string accent = null, string body = null)
        {
            var d = new CharacterDef
            {
                Id = id, Name = name, Title = title, Technique = technique, Era = era, Stance = stance,
                Look = new Look
                {
                    Hair = hair, Face = face, Weapon = weapon,
                    Aura = H(aura),
                    Accent = accent != null ? H(accent) : new Color(0, 0, 0, 0),
                    Body = body != null ? H(body) : new Color(0.93f, 0.93f, 0.96f)
                }
            };
            all.Add(d);
            return d;
        }

        /// <summary>Heavenly restriction: no cursed energy, superhuman body, invisible to sure-hit.</summary>
        static void Restricted(CharacterDef d, float hp = 1150f, float speed = 1.25f, float power = 1.2f)
        {
            d.Passives |= Passive.HeavenlyRestriction;
            d.Hp = hp; d.Speed = speed; d.Power = power; d.Jump = 1.15f; d.Ce = 0f; d.CeRegen = 0f;
        }

        static ProjDef Orb(string color, float dmg, float speed = 15f, float size = 0.45f, DamageType t = DamageType.Energy)
            => new ProjDef { Vis = ProjVis.Orb, Color = H(color), Core = Color.white, Damage = dmg, Speed = speed, Size = size, Type = t };

        static ProjDef Slash(string color, float dmg, float speed = 22f, float size = 0.8f)
            => new ProjDef { Vis = ProjVis.Slash, Color = H(color), Core = Color.white, Damage = dmg, Speed = speed, Size = size, Type = DamageType.Slash, Launch = Sfx.Slash, Impact = Sfx.Slash, Knockback = new Vector2(5f, 2f) };

        static MinionDef Mn(string name, MinionShape shape, MinionBrain brain, string color, float hp, float dmg, float speed = 6f, float size = 1f, float life = 10f)
            => new MinionDef { Name = name, Shape = shape, Brain = brain, Color = H(color), Hp = hp, Damage = dmg, Speed = speed, Size = size, Life = life };

        static DomainDef Dom(string name, DomainTheme theme, SureHit effect, string primary, string secondary, float refinement, params string[] chant)
            => new DomainDef { Name = name, Theme = theme, Effect = effect, Primary = H(primary), Secondary = H(secondary), Refinement = refinement, Chant = chant };

        static DomainAb DomainUlt(DomainDef d, string desc, string color)
            => new DomainAb { Name = "Domain Expansion", Domain = d, Desc = desc, Color = H(color), Chant = d.Chant };

        // shared shikigami
        public static readonly MinionDef WheelGeneral = new MinionDef
        {
            Name = "Wheel General", Shape = MinionShape.Wheel, Brain = MinionBrain.Chase, Color = Art.Hex("#fff1c4"),
            Hp = 900f, Damage = 70f, Range = 1.5f, AttackRate = 1.1f, Size = 2.1f, Life = 22f, Speed = 5.2f,
            Knockback = new Vector2(9f, 6f), Hitstun = 0.6f, Type = DamageType.Light, Adapts = true,
            Flags = HitFlags.Technique | HitFlags.Minion | HitFlags.Heavy
        };

        // ---------------------------------------------------------------- shared domains
        public static readonly DomainDef DomVoid = Dom("Infinite Null Expanse", DomainTheme.Void, SureHit.Paralyze, "#7fc8ff", "#ffffff", 10f,
            "Throughout heaven and earth...", "...I alone am the honored one.");
        public static readonly DomainDef DomShrine = new DomainDef
        {
            Name = "Shrine of Ruin", Theme = DomainTheme.Shrine, Effect = SureHit.Slashes, Primary = Art.Hex("#ff2a3a"), Secondary = Art.Hex("#2a0005"),
            Refinement = 9.6f, Open = true, Duration = 9f, Chant = new[] { "Kneel.", "Know your place." }
        };
        public static readonly DomainDef DomShadow = Dom("Abyssal Shadow Garden", DomainTheme.Shadow, SureHit.Sink, "#8a6bff", "#160b2b", 4.5f, "With this treasure...", "...I summon.");
        public static readonly DomainDef DomSwords = Dom("True Mutual Devotion", DomainTheme.Swords, SureHit.Swords, "#ff8fb0", "#ffffff", 8.6f, "Come forth...", "...my love.");
        public static readonly DomainDef DomHometown = Dom("Hometown Crossing", DomainTheme.Hometown, SureHit.SoulStrike, "#ffb070", "#fff2c0", 8.2f, "This is where I come from.", "No more running.");
        public static readonly DomainDef DomPalms = Dom("Palm of Absolute Form", DomainTheme.Palms, SureHit.SoulTouch, "#6ad6c8", "#0b2b2a", 7.4f, "Let me touch your soul.", "Become something new.");
        public static readonly DomainDef DomCaldera = Dom("Iron Caldera", DomainTheme.Caldera, SureHit.Burn, "#ff6a1a", "#3b0a00", 6.2f, "Burn.", "Everything turns to ash.");
        public static readonly DomainDef DomGarden = Dom("Garden of Blinding Bloom", DomainTheme.Garden, SureHit.Bloom, "#9dffb0", "#ffd0e8", 5.6f, "The earth remembers.", "Return to the soil.");
        public static readonly DomainDef DomShore = Dom("Endless Tideshore", DomainTheme.Shore, SureHit.Swarm, "#4fd7ff", "#fff3b0", 5.2f, "The tide comes in.", "No one escapes the sea.");
        public static readonly DomainDef DomWomb = Dom("Cradle of Countless Births", DomainTheme.Womb, SureHit.Gravity, "#ff4a6a", "#2a0008", 9.1f, "A thousand years of plans...", "...born again in this cradle.");
        public static readonly DomainDef DomMoon = Dom("Lunar Cell Palace", DomainTheme.Moon, SureHit.MoveCut, "#c9d4ff", "#1a1f3a", 6.4f, "Twenty-four frames.", "Move, and you break.");
        public static readonly DomainDef DomCourt = Dom("Court of Final Verdict", DomainTheme.Court, SureHit.Verdict, "#ffd36a", "#2a1a08", 7.2f, "Court is in session.", "The accused will rise.");
        public static readonly DomainDef DomJackpot = Dom("Jackpot Express", DomainTheme.Jackpot, SureHit.Jackpot, "#ff4fb8", "#ffe14f", 7.0f, "All aboard.", "Feel the fever!");
        public static readonly DomainDef DomSpheres = Dom("Triple Torment", DomainTheme.Spheres, SureHit.TrueSphere, "#d6dce8", "#6a7080", 6.6f, "Love is a perfect sphere.", "Be crushed by it.");
    }
}
