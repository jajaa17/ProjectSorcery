using UnityEngine;

namespace ProjectSorcery
{
    public static partial class Roster
    {
        // ===================================================================== THE OLD CLAN
        static void AddClans()
        {
            // ---------------------------------------------------------- 24-frame rush
            var d = C("frames", "Kaito Senba", "24-Frame Rush", "Frame Projection", Era.Manga, Stance.Agile, Hair.Slick, FaceMark.None, Weapon.None, "#c9d4ff", "#ffd36a");
            d.Hp = 980; d.Speed = 1.3f; d.Ce = 115; d.CeRegen = 8f; d.Style = AIStyle.Rushdown; d.Domain = DomMoon; d.PreferredRange = 2f;
            d.Bio = "Divides one second into twenty-four frames and moves through them. Touch him off-rhythm and you freeze in a frame.";
            d.Kit = () => new Ability[]
            {
                new StrikeAb { Name = "Frame Dash", Desc = "Moves through the frames and strikes from behind.", Cost = 15, Cooldown = 3.5f, Color = H("#c9d4ff"), RangeMax = 10f,
                    Strike = new StrikeDef { TeleportBehind = true, Duration = 0.12f, Velocity = new Vector2(8f, 0f), Damage = 45, Pose = FPose.Kick, Knockback = new Vector2(7f, 3f), Color = H("#c9d4ff") } },
                new ProjectileAb { Name = "Frame Freeze", Desc = "Anyone it touches gets trapped in a single frame.", Cost = 22, Cooldown = 7f, Pose = FPose.Palm, Color = H("#c9d4ff"),
                    Proj = new ProjDef { Vis = ProjVis.Card, Color = H("#c9d4ff"), Damage = 25, Speed = 24f, Size = 0.8f, Type = DamageType.Energy, Status = StatusType.Freeze, StatusTime = 0.9f, Launch = Sfx.Teleport, Impact = Sfx.Ice } },
                new StrikeAb { Name = "Mach Rush", Desc = "Accelerates past the sound barrier and strikes again and again.", Cost = 35, Cooldown = 11f, Color = H("#ffffff"), RangeMax = 6f,
                    Strike = new StrikeDef { Duration = 0.5f, Velocity = new Vector2(20f, 0f), Hits = 6, Damage = 135, Pose = FPose.Jab, Radius = 1f, Color = H("#ffffff"), Sound = Sfx.Dash } },
                DomainUlt(DomMoon, "Inside the palace every movement cuts you - the more you move, the more you bleed.", "#c9d4ff"),
            };

            // ---------------------------------------------------------- Fastest elder
            d = C("elder", "Naohiko Senba", "Fastest Elder", "Frame Projection", Era.Anime, Stance.Agile, Hair.Short, FaceMark.Beard, Weapon.None, "#ffe0a0", "#5a4a2a");
            d.Hp = 950; d.Speed = 1.35f; d.Ce = 110; d.CeRegen = 8f; d.Style = AIStyle.Rushdown; d.PreferredRange = 2f;
            d.Bio = "An elderly clan head who is, frame for frame, among the fastest sorcerers alive.";
            d.Kit = () => new Ability[]
            {
                new StrikeAb { Name = "Frame Step", Desc = "Instant repositioning strike.", Cost = 14, Cooldown = 3.5f, Color = H("#ffe0a0"), RangeMax = 10f,
                    Strike = new StrikeDef { TeleportBehind = true, Duration = 0.12f, Velocity = new Vector2(8f, 0f), Damage = 45, Pose = FPose.Jab, Color = H("#ffe0a0") } },
                new ProjectileAb { Name = "Frame Snare", Desc = "Projects a frame onto the enemy, freezing them.", Cost = 22, Cooldown = 8f, Pose = FPose.Palm, Color = H("#ffe0a0"),
                    Proj = new ProjDef { Vis = ProjVis.Card, Color = H("#ffe0a0"), Damage = 20, Speed = 24f, Size = 0.8f, Status = StatusType.Freeze, StatusTime = 0.8f, Type = DamageType.Energy } },
                new StrikeAb { Name = "Supersonic Rush", Desc = "A blur of strikes.", Cost = 35, Cooldown = 11f, Color = H("#ffffff"), RangeMax = 6f,
                    Strike = new StrikeDef { Duration = 0.5f, Velocity = new Vector2(22f, 0f), Hits = 6, Damage = 130, Pose = FPose.Jab, Radius = 1f, Color = H("#ffffff") } },
                new StrikeAb { Name = "Breaking the Sound", Desc = "A single strike at supersonic speed.", Cost = 70, Cooldown = 24f, Use = AIUse.Finisher, Color = H("#ffe0a0"), RangeMax = 12f,
                    Strike = new StrikeDef { Duration = 0.3f, Velocity = new Vector2(40f, 0f), Damage = 220, Flags = HitFlags.Heavy | HitFlags.GuardBreak, Pose = FPose.HeavyPunch, Knockback = new Vector2(18f, 6f), Hitstun = 0.9f, StopOnHit = true, Color = H("#ffe0a0"), Hitstop = 0.18f } },
            };

            // ---------------------------------------------------------- Flame blade
            d = C("flameblade", "Ogai Senba", "Flame Blade", "Blazing Edge", Era.Manga, Stance.Swordsman, Hair.Short, FaceMark.Beard, Weapon.Sword, "#ff7a2a", "#3a1a0a");
            d.Hp = 1050; d.Power = 1.1f; d.Ce = 105; d.PreferredRange = 1.9f;
            d.Bio = "A clan elder whose sword is wreathed in cursed fire.";
            d.Kit = () => new Ability[]
            {
                new StrikeAb { Name = "Blazing Edge", Desc = "A burning slash.", Cost = 15, Cooldown = 3.5f, Color = H("#ff7a2a"),
                    Strike = new StrikeDef { Duration = 0.18f, Velocity = new Vector2(16f, 0f), Damage = 55, Type = DamageType.Fire, Pose = FPose.Slash, Status = StatusType.Burn, StatusTime = 2f, StatusMag = 8f, Color = H("#ff7a2a"), Sound = Sfx.Fire } },
                new ProjectileAb { Name = "Fire Arc", Desc = "Hurls a crescent of flame.", Cost = 20, Cooldown = 5f, Pose = FPose.Slash, Color = H("#ff7a2a"),
                    Proj = new ProjDef { Vis = ProjVis.Slash, Color = H("#ff7a2a"), Damage = 50, Speed = 20f, Size = 1.0f, Type = DamageType.Fire, Status = StatusType.Burn, StatusTime = 2f, StatusMag = 8f, Launch = Sfx.Fire, Impact = Sfx.Fire } },
                new CounterAb { Name = "Burning Draw", Desc = "A counter-cut that ignites.", Cost = 18, Cooldown = 7f, Window = 0.6f, Damage = 95, Type = DamageType.Fire, Color = H("#ff7a2a") },
                new BurstAb { Name = "Inferno Cleave", Desc = "Splits the ground with a pillar of fire.", Cost = 70, Cooldown = 24f, Radius = 3f, Offset = new Vector2(2f, 1f), Damage = 190, Knockback = new Vector2(10f, 9f), Vis = BurstVis.Pillar, Status = StatusType.Burn, StatusTime = 4f, StatusMag = 12f, Delay = 0.25f, Pose = FPose.Slash, Color = H("#ff7a2a"), Type = DamageType.Fire, Use = AIUse.Finisher, Sound = Sfx.Fire, Shake = 0.6f },
            };

            // ---------------------------------------------------------- Giant palms
            d = C("palms", "Jinsuke Senba", "Giant Palms", "Great Hands", Era.Manga, Stance.Brute, Hair.Short, FaceMark.Beard, Weapon.None, "#ffb07a", "#5a3a2a");
            d.Hp = 1100; d.Size = 1.08f; d.Ce = 110; d.Speed = 0.92f; d.Style = AIStyle.Zoner; d.PreferredRange = 4.5f;
            d.Bio = "Conjures enormous hands of cursed energy that slap, crush and clap from a distance.";
            d.Kit = () => new Ability[]
            {
                new BurstAb { Name = "Hand Slam", Desc = "A giant palm slams down on the enemy.", Cost = 16, Cooldown = 3.5f, AtTarget = true, Radius = 1.5f, Damage = 50, Delay = 0.3f, Knockback = new Vector2(2f, -6f), Vis = BurstVis.Shockwave, Pose = FPose.Slam, Color = H("#ffb07a"), RangeMax = 10f },
                new CustomAb { Name = "Clap Crush", Desc = "Two giant hands clap together on the enemy.", Cost = 28, Cooldown = 7f, CastTime = 0.3f, Pose = FPose.Clap, Use = AIUse.Attack, RangeMax = 10f, Color = H("#ffb07a"),
                    Fn = (f, p) =>
                    {
                        var t = f.Target; if (t == null) return;
                        var h = HitInfo.Make(f, HitSource.Other, 85f * p, new Vector2(0f, 4f), 0.7f, HitFlags.Technique | HitFlags.Heavy, DamageType.Blunt, t.Center, H("#ffb07a"));
                        f.M.Hit(t, h.WithStatus(StatusType.Stun, 0.4f, 0f));
                        VFX.Burst(BurstVis.Sound, t.Center, 2f, H("#ffb07a"));
                        Audio.Play(Sfx.Clap, t.Center, 1f, 0.6f);
                    } },
                new ZoneAb { Name = "Palm Wall", Desc = "A wall of hands that blocks projectiles.", Cost = 25, Cooldown = 10f, AtTarget = false, Use = AIUse.Zone, Color = H("#ffb07a"),
                    Zone = new ZoneDef { Width = 1.6f, Height = 4.5f, Life = 4f, TickDamage = 8f, TickRate = 0.3f, BlocksProjectiles = true, Vis = ZoneVis.Spikes, Color = H("#ffb07a"), Type = DamageType.Blunt } },
                new CustomAb { Name = "Thousand Palms", Desc = "Hands rain down across the whole arena.", Cost = 75, Cooldown = 26f, CastTime = 0.4f, Pose = FPose.Raise, Use = AIUse.Finisher, RangeMax = 30f, Color = H("#ffb07a"),
                    Fn = (f, p) =>
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            float x = Mathf.Lerp(f.M.Arena.Left + 2f, f.M.Arena.Right - 2f, i / 5f);
                            f.M.Schedule(0.12f * i, () =>
                            {
                                var h = HitInfo.Make(f, HitSource.Other, 55f * p, new Vector2(2f, -8f), 0.5f, HitFlags.Technique | HitFlags.Heavy, DamageType.Blunt, new Vector2(x, 1f), H("#ffb07a"));
                                f.M.Area(f, new Vector2(x, 1f), 2.4f, h, true);
                                VFX.Burst(BurstVis.Shockwave, new Vector2(x, 0.8f), 2.4f, H("#ffb07a"));
                                Audio.Play(Sfx.PunchHeavy, new Vector2(x, 1f), 0.8f, 0.7f);
                            });
                        }
                    } },
            };

            // ---------------------------------------------------------- Binding gaze
            d = C("gaze", "Ranmaru", "Binding Gaze", "Eye Lock", Era.Anime, Stance.Caster, Hair.Long, FaceMark.ExtraEyes, Weapon.None, "#ff4ad0", "#2a0a2a");
            d.Hp = 1100; d.Ce = 115; d.CeRegen = 8.5f; d.Style = AIStyle.Zoner; d.Tier = 3; d.PreferredRange = 6f;
            d.Bio = "Whatever falls under his unblinking gaze stops moving.";
            d.Kit = () => new Ability[]
            {
                new ProjectileAb { Name = "Glare", Desc = "A piercing look that slows.", Cost = 12, Cooldown = 2.5f, Pose = FPose.Point, Color = H("#ff4ad0"),
                    Proj = new ProjDef { Vis = ProjVis.Bolt, Color = H("#ff4ad0"), Damage = 40, Speed = 30f, Size = 0.4f, Type = DamageType.Soul, Status = StatusType.Slow, StatusTime = 1.5f, StatusMag = 0.35f } },
                new ProjectileAb { Name = "Eye Lock", Desc = "Pins the target in place.", Cost = 25, Cooldown = 8f, Pose = FPose.Point, Color = H("#ff4ad0"),
                    Proj = new ProjDef { Vis = ProjVis.Orb, Color = H("#ff4ad0"), Damage = 35, Speed = 26f, Size = 0.5f, Type = DamageType.Soul, Status = StatusType.Bind, StatusTime = 1.6f } },
                new BurstAb { Name = "Flash Glare", Desc = "Stuns everything nearby.", Cost = 30, Cooldown = 11f, Radius = 2.6f, Damage = 65, Knockback = new Vector2(6f, 3f), Status = StatusType.Stun, StatusTime = 0.7f, Vis = BurstVis.Light, Color = H("#ff4ad0"), Pose = FPose.Raise, Use = AIUse.Escape },
                new CustomAb { Name = "Unblinking", Desc = "Locks every enemy in sight.", Cost = 70, Cooldown = 26f, CastTime = 0.4f, Pose = FPose.Point, Use = AIUse.Utility, RangeMax = 30f, Color = H("#ff4ad0"),
                    Fn = (f, p) =>
                    {
                        foreach (var e in f.M.Fighters)
                        {
                            if (!f.M.IsEnemy(f, e) || e.Dead) continue;
                            var h = HitInfo.Make(f, HitSource.Other, 90f * p, Vector2.zero, 0.2f, HitFlags.Technique | HitFlags.Unblockable | HitFlags.NoCombo, DamageType.Soul, e.Center, H("#ff4ad0"));
                            f.M.Hit(e, h.WithStatus(StatusType.Bind, 2.5f, 0f));
                            VFX.Ring(e.Center, H("#ff4ad0"), 1.4f, 0.6f, 0.08f);
                        }
                    } },
            };
        }
    }
}
