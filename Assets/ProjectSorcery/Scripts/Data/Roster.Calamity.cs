using UnityEngine;

namespace ProjectSorcery
{
    public static partial class Roster
    {
        // ===================================================================== THE KING OF CALAMITY
        static void AddCalamity()
        {
            // ---------------------------------------------------------- Calamity king (vessel)
            var d = C("calamity", "Omaga", "King of Calamity", "Shrine", Era.Anime, Stance.Feral, Hair.Spiky, FaceMark.Marks | FaceMark.ExtraEyes, Weapon.None, "#ff2a3a", "#ff6a6a");
            d.Hp = 1000; d.Ce = 150; d.CeRegen = 9f; d.Power = 1.2f; d.Speed = 1.12f; d.RctRate = 35f; d.Tier = 1; d.PreferredRange = 3f; d.Domain = DomShrine;
            d.Passives = Passive.AutoRCT | Passive.RCT | Passive.BlackFlashAffinity; d.Style = AIStyle.Balanced;
            d.Bio = "The undisputed king of curses, wearing a borrowed body. Invisible slashes, a flame arrow, and a shrine that cuts everything within its reach.";
            d.Kit = () => new Ability[] { SunderAb(1), CarveAb(), DivineArrowAb(), DomainUlt(DomShrine, "No barrier: invisible slashes shred everything inside its vast reach.", "#ff2a3a") };

            // ---------------------------------------------------------- Calamity king (shadow vessel)
            d = C("calamity_shadow", "Omaga", "Shadow-Stealing King", "Shrine & Ten Umbras", Era.Manga, Stance.Feral, Hair.Messy, FaceMark.Marks | FaceMark.ExtraEyes, Weapon.None, "#ff3a5a", "#8a6bff");
            d.Hp = 1000; d.Ce = 150; d.CeRegen = 9f; d.Power = 1.15f; d.Speed = 1.1f; d.RctRate = 25f; d.Tier = 1; d.PreferredRange = 3f; d.Domain = DomShrine;
            d.Passives = Passive.AutoRCT | Passive.RCT | Passive.BlackFlashAffinity; d.Style = AIStyle.Balanced;
            d.Bio = "Having stolen the shadow-tamer's body, the king commands both his own slashes and the strongest shikigami - and learned to cut the world itself.";
            d.Kit = () => new Ability[]
            {
                SunderAb(1),
                new SummonAb { Name = "Wheel General", Desc = "Summons the adapting divine general.", Cost = 60, Cooldown = 32f, Count = 1, MaxActive = 1, Color = H("#fff1c4"), Minion = WheelGeneral, Offset = new Vector2(1.5f, 0f) },
                WorldSeverAb(80f),
                DomainUlt(DomShrine, "No barrier: invisible slashes shred everything inside its vast reach.", "#ff2a3a"),
            };

            // ---------------------------------------------------------- Calamity king (true form, playable)
            d = C("calamity_heian", "Omaga", "Heian Calamity", "Shrine (True Form)", Era.Manga, Stance.Feral, Hair.Wild, FaceMark.Marks | FaceMark.ExtraEyes, Weapon.None, "#ff1a2a", "#ff6a6a", "#f0dcd8");
            d.Look.FourArms = true;
            d.Hp = 1050; d.Ce = 170; d.CeRegen = 10f; d.Power = 1.2f; d.Speed = 1.1f; d.Size = 1.12f; d.RctRate = 25f; d.Tier = 1; d.PreferredRange = 3f; d.Domain = DomShrine;
            d.Passives = Passive.AutoRCT | Passive.RCT | Passive.BlackFlashAffinity | Passive.ToughBody; d.Style = AIStyle.Balanced;
            d.Bio = "The king in his true four-armed form, as he was when sorcerers of a golden age failed to stop him.";
            d.Kit = () => new Ability[] { SunderAb(3), DivineArrowAb(), WorldSeverAb(80f), DomainUlt(DomShrine, "No barrier: invisible slashes shred everything inside its vast reach.", "#ff2a3a") };

            // ---------------------------------------------------------- RAID BOSS
            d = C("calamity_boss", "Omaga", "Calamity of the Golden Age", "Shrine (Unbound)", Era.Boss, Stance.Feral, Hair.Wild, FaceMark.Marks | FaceMark.ExtraEyes | FaceMark.Crown, Weapon.None, "#ff1020", "#ffb030", "#f4dcd4");
            d.Look.FourArms = true;
            d.BossOnly = true;
            d.Hp = 1600; d.Ce = 300; d.CeRegen = 16f; d.Power = 1.3f; d.Speed = 1.15f; d.Size = 1.25f; d.Weight = 1.5f; d.RctRate = 30f; d.Tier = 1; d.PreferredRange = 3.5f;
            d.Passives = Passive.AutoRCT | Passive.RCT | Passive.BlackFlashAffinity | Passive.ToughBody; d.Style = AIStyle.Balanced; d.Domain = DomShrineBoss;
            d.Bio = "Raid boss. Unshackled, at the height of his power, with no intention of holding back.";
            d.Kit = () => new Ability[]
            {
                SunderAb(5),
                new BurstAb { Name = "Carve Field", Desc = "Cuts everything around him to ribbons.", Cost = 30, Cooldown = 6f, Radius = 3.4f, Damage = 95, Knockback = new Vector2(10f, 6f), Vis = BurstVis.Slashes, Delay = 0.25f, Type = DamageType.Slash, Pose = FPose.Spin, Color = H("#ff1020"), Sound = Sfx.Slash, Shake = 0.5f },
                WorldSeverAb(90f),
                DomainUlt(DomShrineBoss, "The shrine at full power.", "#ff1020"),
            };
        }

        public static readonly DomainDef DomShrineBoss = new DomainDef
        {
            Name = "Shrine of Ruin (Unbound)", Theme = DomainTheme.Shrine, Effect = SureHit.Slashes, Primary = Art.Hex("#ff1020"), Secondary = Art.Hex("#2a0005"),
            Refinement = 10.4f, Open = true, Duration = 9f, Power = 1.1f, Chant = new[] { "Kneel.", "Know your place, fools." }
        };

        static ProjectileAb SunderAb(int count) => new ProjectileAb
        {
            Name = count > 1 ? "Sunder Storm" : "Sunder", Desc = count > 1 ? "A volley of invisible slashes." : "An invisible slash that cuts straight through.",
            Cost = 15 + 5 * (count - 1), Cooldown = 2.5f + 0.6f * (count - 1), Count = count, Spread = 7f, Interval = 0.05f, Pose = FPose.Slash, Color = Art.Hex("#ff2a3a"),
            Proj = WithPierce(Slash("#ff2a3a", 50f, 30f, 0.9f), 1)
        };

        static CustomAb CarveAb() => new CustomAb
        {
            Name = "Carve", Desc = "A close slash that adjusts to the target's toughness: the stronger they are, the deeper it cuts.", Cost = 25, Cooldown = 6f, CastTime = 0.1f,
            Pose = FPose.Slash, Use = AIUse.Attack, RangeMax = 3f, Color = Art.Hex("#ff2a3a"),
            Fn = (f, p) =>
            {
                var t = f.Target;
                if (t == null || Mathf.Abs(t.Pos.x - f.Pos.x) > 3.2f || Mathf.Abs(t.Pos.y - f.Pos.y) > 2.5f) { VFX.Burst(BurstVis.Slashes, f.Front(1.4f, 1.2f), 1.4f, Art.Hex("#ff2a3a")); return; }
                float dmg = (40f + t.MaxHp * 0.09f) * p;
                var h = HitInfo.Make(f, HitSource.Other, dmg, new Vector2(8f * f.Facing, 4f), 0.6f, HitFlags.Technique | HitFlags.Heavy, DamageType.Slash, t.Center, Art.Hex("#ff2a3a"));
                h.Hitstop = 0.12f;
                f.M.Hit(t, h);
                VFX.Burst(BurstVis.Slashes, t.Center, 1.6f, Art.Hex("#ff2a3a"));
                Audio.Play(Sfx.Slash, t.Center, 1f, 0.7f);
            }
        };

        static ProjectileAb DivineArrowAb() => new ProjectileAb
        {
            Name = "Ignite: Divine Arrow", Desc = "Draws a bow of flame and looses an arrow that detonates. Hold to say the word.", Cost = 60, Cooldown = 18f, Chantable = true, MaxChant = 1,
            Chant = new[] { "Open." }, Pose = FPose.Bow, Use = AIUse.Finisher, CastTime = 0.4f, Armored = true, Color = Art.Hex("#ff8a2a"),
            Proj = new ProjDef
            {
                Vis = ProjVis.Arrow, Color = Art.Hex("#ff8a2a"), Core = Art.Hex("#fff0c0"), Size = 1.2f, Speed = 26f, Damage = 90f, ExplodeRadius = 3.5f, ExplodeDamage = 150f,
                ExplodeKb = new Vector2(12f, 9f), Unerasable = true, Flags = HitFlags.Technique | HitFlags.Heavy, Type = DamageType.Fire,
                Status = StatusType.Burn, StatusTime = 4f, StatusMag = 12f, Launch = Sfx.Fire, Impact = Sfx.Explosion
            }
        };

        static BeamAb WorldSeverAb(float cost) => new BeamAb
        {
            Name = "World Sever", Desc = "A slash that cuts space itself. Nothing - not even boundless space - stops it. Hold to chant.", Cost = cost, Cooldown = 22f,
            Chantable = true, MaxChant = 2, Chant = new[] { "Dragon scales. Recoil.", "Twin meteors." }, Pose = FPose.Slash, Use = AIUse.Finisher, CastTime = 0.35f, Armored = true,
            Color = Art.Hex("#ffffff"),
            Beam = new BeamDef
            {
                Length = 30f, Width = 0.25f, Duration = 0.18f, TickDamage = 80f, TickRate = 0.09f, Color = Art.Hex("#ff2a3a"), Core = Color.white, Type = DamageType.Slash,
                Flags = HitFlags.Technique | HitFlags.PierceInfinity | HitFlags.Unblockable | HitFlags.Heavy, Charge = 0.3f, Knockback = new Vector2(9f, 5f), Hitstun = 0.6f, Sound = Sfx.Slash
            }
        };
    }
}
