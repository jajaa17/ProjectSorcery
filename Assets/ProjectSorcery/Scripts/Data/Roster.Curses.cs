using UnityEngine;

namespace ProjectSorcery
{
    public static partial class Roster
    {
        // ===================================================================== CURSED SPIRITS
        static void AddCurses()
        {
            // ---------------------------------------------------------- Soul sculptor
            var d = C("sculptor", "Tsugi", "Soul Sculptor", "Soul Reshaping", Era.Anime, Stance.Feral, Hair.Long, FaceMark.Patches, Weapon.Claws, "#6ad6c8", "#2a6a66", "#cfe8e4");
            d.Hp = 1000; d.Ce = 120; d.CeRegen = 8f; d.Speed = 1.08f; d.Tier = 1; d.PreferredRange = 2f;
            d.Passives = Passive.PerfectBody | Passive.CurseBody; d.Domain = DomPalms; d.Style = AIStyle.Trickster;
            d.Bio = "A curse born from human hatred. Reshapes souls with a touch - and his own body most of all. Only blows to the soul truly hurt him.";
            d.Kit = () => new Ability[]
            {
                new StrikeAb { Name = "Reshaped Spike", Desc = "Arm stretches into a lance.", Cost = 15, Cooldown = 4.2f, Color = H("#6ad6c8"),
                    Strike = new StrikeDef { Duration = 0.16f, Velocity = new Vector2(10f, 0f), Offset = new Vector2(1.5f, 1.2f), Radius = 0.9f, Damage = 55, Type = DamageType.Pierce, Pose = FPose.Stab, Knockback = new Vector2(6f, 3f), Color = H("#6ad6c8") } },
                new SummonAb { Name = "Transfigured Horde", Desc = "Releases two reshaped humans who rush the enemy.", Cost = 28, Cooldown = 10f, Count = 2, MaxActive = 2, Color = H("#9ad6c0"),
                    Minion = new MinionDef { Name = "Transfigured", Shape = MinionShape.Blob, Brain = MinionBrain.Chase, Color = H("#9ad6c0"), Hp = 90, Damage = 24, Speed = 7.5f, Life = 10f, Range = 0.9f, Size = 1.1f } },
                new BurstAb { Name = "Polymorphic Isomer", Desc = "Explodes into a mass of reshaped bodies.", Cost = 45, Cooldown = 13f, Radius = 3f, Damage = 100, Knockback = new Vector2(10f, 7f), Vis = BurstVis.Dark, Delay = 0.2f, Pose = FPose.Raise, Color = H("#6ad6c8"), Type = DamageType.Soul },
                DomainUlt(DomPalms, "One touch of the soul: every enemy inside takes massive soul damage.", "#6ad6c8"),
            };

            // ---------------------------------------------------------- Living volcano
            d = C("volcano", "Kazan", "Living Volcano", "Disaster Flames", Era.Anime, Stance.Brute, Hair.Volcano, FaceMark.None, Weapon.None, "#ff6a1a", "#3a1a0a", "#e8d8c8");
            d.Hp = 1000; d.Ce = 130; d.CeRegen = 8f; d.Size = 0.95f; d.Passives = Passive.CurseBody; d.Domain = DomCaldera; d.Style = AIStyle.Zoner; d.PreferredRange = 6f;
            d.Bio = "A curse born from the fear of the earth. Short-tempered, and every bit as hot.";
            d.Kit = () => new Ability[]
            {
                new ProjectileAb { Name = "Ember Insects", Desc = "Burning insects that seek out the enemy and burst.", Cost = 14, Cooldown = 3f, Count = 3, Interval = 0.07f, Spread = 18f, Pose = FPose.Point, Color = H("#ff6a1a"),
                    Proj = new ProjDef { Vis = ProjVis.Fire, Color = H("#ff6a1a"), Damage = 16, Speed = 12f, Size = 0.35f, Homing = 2.6f, Life = 2.4f, Type = DamageType.Fire, ExplodeRadius = 0.9f, ExplodeDamage = 10f, Status = StatusType.Burn, StatusTime = 2f, StatusMag = 6f, Launch = Sfx.Fire, Impact = Sfx.Fire } },
                new ZoneAb { Name = "Eruption", Desc = "The ground beneath the enemy becomes magma.", Cost = 25, Cooldown = 7f, Color = H("#ff8a3a"),
                    Zone = new ZoneDef { Width = 3f, Height = 3f, Life = 2.5f, TickDamage = 14f, TickRate = 0.25f, Status = StatusType.Burn, StatusTime = 1.5f, StatusMag = 8f, Vis = ZoneVis.Fire, Color = H("#ff6a1a"), Type = DamageType.Fire } },
                new ProjectileAb { Name = "Maximum: Skyfall", Desc = "Calls down a meteor.", Cost = 55, Cooldown = 15f, FromSky = true, Pose = FPose.Raise, Use = AIUse.Finisher, CastTime = 0.4f, Color = H("#ff4a1a"),
                    Proj = new ProjDef { Vis = ProjVis.Fire, Color = H("#ff4a1a"), Core = H("#ffe0a0"), Damage = 60, Speed = 14f, Size = 2.4f, Type = DamageType.Fire, ExplodeRadius = 3.4f, ExplodeDamage = 140f, ExplodeKb = new Vector2(11f, 8f), Unerasable = true, Flags = HitFlags.Technique | HitFlags.Heavy, Status = StatusType.Burn, StatusTime = 3f, StatusMag = 10f, Launch = Sfx.Rumble, Impact = Sfx.Explosion } },
                DomainUlt(DomCaldera, "Searing heat and eruptions: everything inside burns.", "#ff6a1a"),
            };

            // ---------------------------------------------------------- Verdant curse
            d = C("verdant", "Mori", "Verdant Curse", "Disaster Plants", Era.Anime, Stance.Floaty, Hair.Horns, FaceMark.Mask, Weapon.None, "#9dffb0", "#3a5a2a", "#e8e0d0");
            d.Hp = 1150; d.Defense = 1.15f; d.Ce = 120; d.CeRegen = 8f; d.Size = 1.12f; d.Passives = Passive.CurseBody; d.Domain = DomGarden; d.Style = AIStyle.Zoner; d.PreferredRange = 5f;
            d.Bio = "A gentle curse born from the fear of the forest. Wants humans gone so the earth can breathe.";
            d.Kit = () => new Ability[]
            {
                new ProjectileAb { Name = "Seed Bullets", Desc = "Fires a spread of hard seeds.", Cost = 12, Cooldown = 2.5f, Count = 4, Spread = 7f, Pose = FPose.Palm, Color = H("#c8a070"),
                    Proj = new ProjDef { Vis = ProjVis.Wood, Color = H("#c8a070"), Damage = 15, Speed = 22f, Size = 0.35f, Type = DamageType.Blunt, Knockback = new Vector2(2f, 1f), Launch = Sfx.WhooshLight, Impact = Sfx.Punch } },
                new BurstAb { Name = "Root Snare", Desc = "Roots burst from under the enemy and bind them.", Cost = 25, Cooldown = 7f, AtTarget = true, Radius = 1.6f, Damage = 45, Knockback = new Vector2(1f, 6f), Status = StatusType.Bind, StatusTime = 1.3f, Vis = BurstVis.Bloom, Delay = 0.35f, Color = H("#9dffb0"), Pose = FPose.Slam, Sound = Sfx.Summon, RangeMax = 12f },
                new ZoneAb { Name = "Flower Field", Desc = "A soothing field that saps the will to fight.", Cost = 30, Cooldown = 12f, Color = H("#ffb0e0"),
                    Zone = new ZoneDef { Width = 5f, Height = 2.5f, Life = 5f, TickDamage = 6f, TickRate = 0.3f, Status = StatusType.Weaken, StatusMag = 0.25f, StatusTime = 0.6f, Vis = ZoneVis.Flowers, Color = H("#ffb0e0"), Type = DamageType.Light } },
                DomainUlt(DomGarden, "Roots bind and flowers drain cursed energy from everyone inside.", "#9dffb0"),
            };

            // ---------------------------------------------------------- Tidal curse
            d = C("tidal", "Umiboshi", "Tidal Curse", "Disaster Tides", Era.Anime, Stance.Floaty, Hair.None, FaceMark.ExtraEyes, Weapon.None, "#4fd7ff", "#1a3a5a", "#d0c0b0");
            d.Hp = 1150; d.Ce = 130; d.CeRegen = 8f; d.Size = 1.2f; d.Weight = 1.25f; d.Speed = 0.9f; d.Passives = Passive.CurseBody; d.Domain = DomShore; d.Style = AIStyle.Zoner; d.PreferredRange = 6f;
            d.Bio = "A curse born from the fear of the sea, hatched in the middle of a battle. Its domain is a beach where the fish never stop coming.";
            d.Kit = () => new Ability[]
            {
                new ProjectileAb { Name = "Death Swarm", Desc = "A school of shikigami fish hunts the target.", Cost = 16, Cooldown = 3f, Count = 4, Interval = 0.06f, Spread = 14f, Pose = FPose.Palm, Color = H("#4fd7ff"),
                    Proj = new ProjDef { Vis = ProjVis.Water, Color = H("#4fd7ff"), Damage = 14, Speed = 14f, Size = 0.4f, Homing = 2.5f, Life = 2.5f, Type = DamageType.Water, Knockback = new Vector2(2f, 1f), Launch = Sfx.Water, Impact = Sfx.Water } },
                new ProjectileAb { Name = "Tidal Wave", Desc = "A wave that rolls along the ground.", Cost = 22, Cooldown = 6f, Pose = FPose.Slam, Color = H("#4fd7ff"),
                    Proj = new ProjDef { Vis = ProjVis.Wave, Color = H("#4fd7ff"), Damage = 60, Speed = 13f, Size = 1.5f, Ground = true, Life = 1.8f, Pierce = 2, Type = DamageType.Water, Knockback = new Vector2(9f, 5f), Launch = Sfx.Water, Impact = Sfx.Water } },
                new SummonAb { Name = "Shark Shikigami", Desc = "Two sharks circle and bite.", Cost = 35, Cooldown = 12f, Count = 2, MaxActive = 2, Color = H("#4fa7d0"),
                    Minion = new MinionDef { Name = "Shark", Shape = MinionShape.Fish, Brain = MinionBrain.Chase, Color = H("#4fa7d0"), Fly = true, Hp = 90, Damage = 26, Speed = 9f, Size = 1.6f, Life = 9f, Range = 0.9f, Type = DamageType.Pierce } },
                DomainUlt(DomShore, "An endless shore: shikigami fish strike every enemy without missing.", "#4fd7ff"),
            };

            // ---------------------------------------------------------- Swarm king
            d = C("swarm", "Kurou", "Swarm King", "Plague Brood", Era.Manga, Stance.Feral, Hair.Horns, FaceMark.ExtraEyes, Weapon.Claws, "#a07a3a", "#2a1a0a", "#3a2a1a");
            d.Hp = 1050; d.Ce = 120; d.Size = 1.08f; d.Speed = 1.1f; d.Passives = Passive.CurseBody; d.Style = AIStyle.Summoner; d.PreferredRange = 3f;
            d.Bio = "A special grade curse born from humanity's disgust of insects. Breeds a swarm faster than you can kill it.";
            d.Kit = () => new Ability[]
            {
                new ProjectileAb { Name = "Roach Spray", Desc = "A burst of skittering insects.", Cost = 12, Cooldown = 2.5f, Count = 5, Spread = 12f, Pose = FPose.Palm, Color = H("#5a4020"),
                    Proj = new ProjDef { Vis = ProjVis.Insect, Color = H("#5a4020"), Damage = 10, Speed = 16f, Size = 0.3f, Homing = 1.5f, Type = DamageType.Poison, Status = StatusType.Poison, StatusTime = 2f, StatusMag = 4f, Launch = Sfx.Insect, Impact = Sfx.Insect } },
                new SummonAb { Name = "Egg Brood", Desc = "Hatches a crawling swarm.", Cost = 25, Cooldown = 10f, Count = 2, MaxActive = 3, Color = H("#5a4020"),
                    Minion = new MinionDef { Name = "Brood", Shape = MinionShape.Cockroach, Brain = MinionBrain.Swarm, Color = H("#5a4020"), Hp = 35, Damage = 9, Speed = 10f, Size = 0.9f, Life = 9f, Range = 0.6f, AttackRate = 0.4f, Type = DamageType.Poison, Status = StatusType.Poison, StatusTime = 1.5f, StatusMag = 4f } },
                new StrikeAb { Name = "Devour", Desc = "Lunges and gnaws, stealing life.", Cost = 30, Cooldown = 10f, Color = H("#a07a3a"),
                    Strike = new StrikeDef { Duration = 0.25f, Velocity = new Vector2(15f, 0f), Hits = 3, Damage = 75, Type = DamageType.Pierce, Pose = FPose.Grab, Color = H("#a07a3a"),
                        OnHit = (a, v, p) => a.Heal(15f * p) } },
                new ZoneAb { Name = "Plague Tide", Desc = "Floods the arena floor with insects.", Cost = 70, Cooldown = 24f, AtTarget = true, Use = AIUse.Zone, Color = H("#5a4020"),
                    Zone = new ZoneDef { Width = 10f, Height = 1.6f, Life = 4.5f, TickDamage = 9f, TickRate = 0.25f, Status = StatusType.Poison, StatusTime = 1.5f, StatusMag = 6f, Vis = ZoneVis.Insects, Color = H("#5a4020"), Type = DamageType.Poison } },
            };

            // ---------------------------------------------------------- Wheel General (playable shikigami)
            d = C("wheel", "The Wheel General", "Divine Adaptation", "Adaptation Wheel", Era.Anime, Stance.Brute, Hair.None, FaceMark.Wheel, Weapon.Sword, "#fff1c4", "#c8b070", "#f0eadc");
            d.Hp = 1250; d.Size = 1.45f; d.Weight = 1.8f; d.Speed = 0.88f; d.Power = 1.25f; d.Ce = 100; d.Tier = 1; d.PreferredRange = 2f; d.Style = AIStyle.Rushdown;
            d.Passives = Passive.Adaptation | Passive.ToughBody;
            d.Bio = "The strongest of the ten shadows. Every phenomenon that hurts it once, the wheel turns, and it never hurts the same way again.";
            d.Kit = () => new Ability[]
            {
                new StrikeAb { Name = "Extermination Blade", Desc = "A blade of positive energy - devastating against curses.", Cost = 18, Cooldown = 4.5f, Color = H("#fff1c4"),
                    Strike = new StrikeDef { Duration = 0.22f, Velocity = new Vector2(13f, 0f), Damage = 80, Type = DamageType.Light, Flags = HitFlags.Heavy, Pose = FPose.Slash, Knockback = new Vector2(10f, 4f), Radius = 1f, Color = H("#fff1c4"), Sound = Sfx.Slash } },
                new CustomAb { Name = "Wheel Turn", Desc = "The wheel turns: adapt hard to recent damage and mend.", Cost = 30, Cooldown = 12f, CastTime = 0.3f, Pose = FPose.Raise, Use = AIUse.Heal, Color = H("#fff1c4"),
                    Fn = (f, p) =>
                    {
                        for (int i = 0; i < f.Adapt.Length; i++) if (f.Adapt[i] > 0f) f.Adapt[i] = Mathf.Min(0.85f, f.Adapt[i] + 0.25f);
                        f.Heal(90f * p);
                        f.InfinityHitsTaken += 2;
                        Audio.Play(Sfx.Bell, f.Center); VFX.Ring(f.HeadPos + Vector2.up, H("#fff1c4"), 2f, 0.6f, 0.1f);
                    } },
                new BurstAb { Name = "Crushing Blow", Desc = "A downward smash that shatters the ground.", Cost = 40, Cooldown = 12f, Radius = 2.8f, Damage = 135, Offset = new Vector2(1.2f, 0.4f), Knockback = new Vector2(8f, 10f), Vis = BurstVis.Shockwave, Delay = 0.2f, Pose = FPose.Slam, Color = H("#fff1c4"), Shake = 0.7f },
                new BuffAb { Name = "Total Adaptation", Desc = "The wheel spins wildly: armor, resistance, and a blade that pierces boundless space.", Cost = 70, Cooldown = 28f, Status = StatusType.Armor, Mag = 1f, Status2 = StatusType.DefenseUp, Mag2 = 0.4f, Duration = 8f, Color = H("#fff1c4"), Use = AIUse.Buff },
            };
        }
    }
}
