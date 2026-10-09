using UnityEngine;

namespace ProjectSorcery
{
    public static partial class Roster
    {
        // ===================================================================== MODULO (decades later)
        static void AddModulo()
        {
            // ---------------------------------------------------------- Ageless fist
            var d = C("vessel_modulo", "Haru Kanzaki", "The Ageless Fist", "Black Flash Mastery", Era.Modulo, Stance.Brawler, Hair.Short, FaceMark.Scar, Weapon.None, "#ff2848", "#ffffff");
            d.Hp = 1100; d.Power = 1.2f; d.Speed = 1.1f; d.Ce = 120; d.CeRegen = 8f; d.Tier = 1; d.PreferredRange = 1.5f; d.Style = AIStyle.Rushdown;
            d.Passives = Passive.BlackFlashAffinity | Passive.SoulResist; d.BlackFlashBonus = 0.08f;
            d.Bio = "Decades on, a veteran who catches blades without looking, lands black flashes at will, and speaks a single word to cut a city in two.";
            d.Kit = () => new Ability[]
            {
                new BurstAb { Name = "Blood Burst", Desc = "Clenches a fist and blood explodes outward.", Cost = 18, Cooldown = 5f, Radius = 1.8f, Offset = new Vector2(0.4f, 1.1f), Damage = 55, Type = DamageType.Blood, Vis = BurstVis.Blood,
                    Knockback = new Vector2(6f, 4f), Status = StatusType.Bleed, StatusTime = 3f, StatusMag = 6f, Pose = FPose.Raise, Color = H("#ff2848"), Sound = Sfx.Blood },
                new CounterAb { Name = "Without Looking", Desc = "Catches the incoming blow mid-air and answers.", Cost = 15, Cooldown = 7f, Window = 0.5f, Damage = 90, Type = DamageType.Blunt, Color = H("#ffffff") },
                new StrikeAb { Name = "Black Flash Barrage", Desc = "Four blows, the last one a guaranteed Black Flash.", Cost = 45, Cooldown = 14f, Color = H("#ff1a33"), RangeMax = 4f,
                    Strike = new StrikeDef { Duration = 0.45f, Velocity = new Vector2(14f, 0f), Hits = 4, Damage = 110, BlackFlash = true, Pose = FPose.HeavyPunch, Radius = 1f, Color = H("#ff1a33") } },
                new CustomAb { Name = "Sunder: One Word", Desc = "Points and says one word. The slash tears across everything and leaves twin explosions behind.", Cost = 85, Cooldown = 30f, CastTime = 0.55f, Pose = FPose.Point, Use = AIUse.Finisher, RangeMax = 30f, Color = H("#ff2848"), Armored = true,
                    Fn = (f, p) =>
                    {
                        VFX.WorldText(f.HeadPos + Vector2.up, "\"SUNDER.\"", H("#ff2848"), 1.4f);
                        Audio.Play(Sfx.Voice, f.Center, 1f, 0.8f);
                        var slash = new ProjDef { Vis = ProjVis.Slash, Color = H("#ff2848"), Core = Color.white, Damage = 150f, Speed = 34f, Size = 2.6f, Pierce = 3, Erase = true, Unerasable = true, DestroyOnWall = false,
                            Type = DamageType.Slash, Flags = HitFlags.Technique | HitFlags.Heavy | HitFlags.PierceInfinity, Knockback = new Vector2(12f, 5f), Launch = Sfx.Slash, Impact = Sfx.Slash };
                        f.M.Shoot(f, slash, f.Front(1f, 1.3f), new Vector2(f.Facing, 0f), p);
                        var t = f.Target;
                        if (t != null)
                        {
                            Vector2 a = t.Center + new Vector2(-1.4f, 0f), b = t.Center + new Vector2(1.4f, 0.6f);
                            f.M.Schedule(0.5f, () =>
                            {
                                var h = HitInfo.Make(f, HitSource.Other, 70f * p, new Vector2(8f, 8f), 0.6f, HitFlags.Technique | HitFlags.Heavy, DamageType.Energy, a, H("#ff2848"));
                                f.M.Area(f, a, 2.6f, h, true); f.M.Area(f, b, 2.6f, h, true);
                                VFX.Explosion(a, 2.6f, H("#ff2848"), ProjVis.Orb); VFX.Explosion(b, 2.6f, H("#ff2848"), ProjVis.Orb);
                                Audio.Play(Sfx.Explosion, a, 1f, 0.8f); CameraRig.Shake(0.8f);
                            });
                        }
                    } },
            };

            // ---------------------------------------------------------- Restricted heir
            d = C("heir", "Ken Odagiri", "Restricted Heir", "Heavenly Restriction", Era.Modulo, Stance.Swordsman, Hair.Short, FaceMark.None, Weapon.Katana, "#c8ffd8", "#2a3a3a");
            Restricted(d, 1180f, 1.4f, 1.3f);
            d.Tier = 1; d.Style = AIStyle.Rushdown; d.PreferredRange = 1.7f;
            d.Bio = "A grandson born with zero cursed energy and a body that makes up for it. Pulls stored cursed tools out of the shadows.";
            d.Kit = () => new Ability[]
            {
                new ProjectileAb { Name = "Shadow Arsenal", Desc = "Pulls tools from storage and hurls them.", Physical = true, Cost = 0, Cooldown = 4f, Count = 3, Spread = 8f, Interval = 0.06f, Pose = FPose.Throw, Color = H("#c0c8d0"),
                    Proj = new ProjDef { Vis = ProjVis.Metal, Color = H("#c0c8d0"), Damage = 26, Speed = 24f, Size = 0.45f, Type = DamageType.Pierce, Flags = HitFlags.None, Launch = Sfx.WhooshLight, Impact = Sfx.Nail } },
                new StrikeAb { Name = "Soul Splitter", Desc = "A soul-cutting blade drawn from the shadows.", Physical = true, Cost = 0, Cooldown = 5f, Color = H("#c8ffd8"),
                    Strike = new StrikeDef { Duration = 0.18f, Velocity = new Vector2(24f, 0f), Damage = 72, Type = DamageType.Soul, Flags = HitFlags.Heavy | HitFlags.Soul | HitFlags.PierceInfinity, Pose = FPose.Slash, Color = H("#c8ffd8"), Sound = Sfx.Slash } },
                new BuffAb { Name = "Bond Fusion", Desc = "Fuses with an inherited curse: armor and raw power.", Physical = true, Cost = 0, Cooldown = 22f, Status = StatusType.Armor, Mag = 1f, Status2 = StatusType.PowerUp, Mag2 = 0.35f, Duration = 7f, Color = H("#ffc0d0") },
                new StrikeAb { Name = "Heir's Massacre", Desc = "A storm of cuts at superhuman speed.", Physical = true, Cost = 0, Cooldown = 28f, Use = AIUse.Finisher, Color = H("#ffffff"), RangeMax = 8f,
                    Strike = new StrikeDef { TeleportBehind = true, Duration = 0.7f, Velocity = new Vector2(9f, 0f), Hits = 8, HitInterval = 0.07f, Damage = 220, Type = DamageType.Slash, Pose = FPose.Slash, Radius = 1.1f, Color = Color.white, Sound = Sfx.Slash } },
            };

            // ---------------------------------------------------------- Blade sister
            d = C("sister", "Yume Odagiri", "Blade Sister", "Inherited Arts", Era.Modulo, Stance.Swordsman, Hair.Ponytail, FaceMark.None, Weapon.Polearm, "#ffa0c8", "#3a2a3a");
            d.Hp = 1050; d.Speed = 1.15f; d.Power = 1.12f; d.Ce = 100; d.CeRegen = 7f; d.PreferredRange = 1.9f; d.Style = AIStyle.Rushdown;
            d.Bio = "A granddaughter who fights with her grandmother's polearm style - plus cursed energy her grandmother never had.";
            d.Kit = () => new Ability[]
            {
                new BurstAb { Name = "Polearm Spin", Desc = "A wide spinning sweep.", Cost = 14, Cooldown = 3.5f, Radius = 2f, Damage = 55, Type = DamageType.Slash, Vis = BurstVis.Slashes, Knockback = new Vector2(7f, 4f), Pose = FPose.Spin, Color = H("#ffa0c8"), Sound = Sfx.Slash, RangeMax = 2.5f },
                new StrikeAb { Name = "Cursed Thrust", Desc = "A cursed-energy-charged lunge.", Cost = 18, Cooldown = 4.5f, Color = H("#ffa0c8"),
                    Strike = new StrikeDef { Duration = 0.2f, Velocity = new Vector2(20f, 0f), Damage = 65, Type = DamageType.Pierce, Pose = FPose.Stab, Knockback = new Vector2(9f, 3f), DelayedDamage = 25, Color = H("#ffa0c8") } },
                new StrikeAb { Name = "Twin Step", Desc = "Blink behind and strike twice.", Cost = 20, Cooldown = 6f, Color = H("#ffa0c8"), RangeMax = 9f,
                    Strike = new StrikeDef { TeleportBehind = true, Duration = 0.25f, Velocity = new Vector2(7f, 0f), Hits = 2, Damage = 70, Pose = FPose.Slash, Type = DamageType.Slash, Color = H("#ffa0c8") } },
                new StrikeAb { Name = "Inherited Rampage", Desc = "Her family's full style, unleashed.", Cost = 70, Cooldown = 24f, Use = AIUse.Finisher, Color = H("#ffa0c8"), RangeMax = 4f,
                    Strike = new StrikeDef { Duration = 0.7f, Velocity = new Vector2(10f, 0f), Hits = 7, HitInterval = 0.08f, Damage = 200, Type = DamageType.Slash, Pose = FPose.Spin, Radius = 1.4f, Color = H("#ffa0c8"), Sound = Sfx.Slash } },
            };

            // ---------------------------------------------------------- Star visitor
            d = C("visitor", "Zelo", "Star Visitor", "Skyfolk Sorcery", Era.Modulo, Stance.Floaty, Hair.Horns, FaceMark.ExtraEyes, Weapon.None, "#7affb0", "#c0a0ff", "#e0fff0");
            d.Hp = 1000; d.Ce = 140; d.CeRegen = 9f; d.Passives = Passive.Flight; d.Style = AIStyle.Zoner; d.PreferredRange = 5.5f;
            d.Bio = "A visitor from beyond the stars, of a people who also wield cursed energy - in ways no human textbook covers.";
            d.Kit = () => new Ability[]
            {
                new ProjectileAb { Name = "Star Bolt", Desc = "A spiraling bolt of alien cursed energy.", Cost = 14, Cooldown = 2.6f, Pose = FPose.Point, Color = H("#7affb0"),
                    Proj = new ProjDef { Vis = ProjVis.Star, Color = H("#7affb0"), Damage = 40, Speed = 22f, Size = 0.6f, Homing = 1f, Type = DamageType.Energy, Launch = Sfx.Shoot } },
                new ProjectileAb { Name = "Gravity Ring", Desc = "A slow ring that drags enemies toward it.", Cost = 22, Cooldown = 7f, Pose = FPose.Palm, Color = H("#c0a0ff"),
                    Proj = new ProjDef { Vis = ProjVis.Disc, Color = H("#c0a0ff"), Damage = 8, TickInterval = 0.2f, Speed = 7f, Size = 1.2f, Life = 2f, PullRadius = 3f, PullForce = 7f, Type = DamageType.Gravity, ExplodeRadius = 1.8f, ExplodeDamage = 35f } },
                new BurstAb { Name = "Levitate Slam", Desc = "Lifts the enemy and drops them.", Cost = 30, Cooldown = 9f, AtTarget = true, Radius = 1.6f, Damage = 80, Knockback = new Vector2(0f, -14f), Flags = HitFlags.Technique | HitFlags.Heavy | HitFlags.Spike, Vis = BurstVis.Gravity, Delay = 0.3f, Color = H("#c0a0ff"), Pose = FPose.Raise, RangeMax = 12f },
                new BeamAb { Name = "Skyfolk Overdrive", Desc = "Unleashes a torrent of alien energy.", Cost = 75, Cooldown = 24f, Pose = FPose.TwoPalm, Use = AIUse.Finisher, Color = H("#7affb0"), Armored = true,
                    Beam = new BeamDef { Length = 20f, Width = 1.5f, Duration = 0.9f, TickDamage = 18f, TickRate = 0.07f, Sweep = 30f, Color = H("#7affb0"), Core = Color.white, Charge = 0.35f, Knockback = new Vector2(6f, 2f) } },
            };
        }
    }
}
