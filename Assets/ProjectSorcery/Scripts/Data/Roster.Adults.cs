using UnityEngine;

namespace ProjectSorcery
{
    public static partial class Roster
    {
        // ===================================================================== TEACHERS & PRO SORCERERS
        static void AddAdults()
        {
            // ---------------------------------------------------------- The Strongest
            var d = C("strongest", "Shiro Amagiri", "The Strongest", "Boundless & Prism Sight", Era.Anime, Stance.Elegant, Hair.Spiky, FaceMark.Blindfold, Weapon.None, "#7fc8ff", "#f4f4ff", "#f8fbff");
            d.Hp = 900; d.Ce = 150; d.CeRegen = 9f; d.Speed = 1.1f; d.Power = 1.1f; d.RctRate = 45f; d.Tier = 1; d.PreferredRange = 4f;
            d.Passives = Passive.Infinity | Passive.SixEyes | Passive.AutoRCT | Passive.RCT; d.Domain = DomVoid; d.Style = AIStyle.Balanced;
            d.Bio = "Boundless space wraps him like a second skin: nothing touches him unless he allows it. His eyes see every flow of cursed energy.";
            d.Kit = () => new Ability[]
            {
                BlueAb(),
                RedAb(),
                PurpleAb(),
                DomainUlt(DomVoid, "Floods every enemy with infinite information: they cannot act at all.", "#7fc8ff"),
            };

            // ---------------------------------------------------------- Overtime man
            d = C("overtime", "Masaru Hino", "The Overtime Man", "Sevenfold Fraction", Era.Anime, Stance.Martial, Hair.Slick, FaceMark.Goggles, Weapon.Cleaver, "#ffd36a", "#c8b070");
            d.Hp = 1080; d.Power = 1.12f; d.Ce = 100; d.Style = AIStyle.Balanced; d.PreferredRange = 1.7f;
            d.Bio = "Divides any target into seven parts; the point at the 7:3 line takes a critical hit. Works harder once the clock runs out.";
            d.Kit = () => new Ability[]
            {
                new StrikeAb { Name = "Sevenfold Fraction", Desc = "Strike the 7:3 point. Crits against stunned or unlucky targets.", Cost = 15, Cooldown = 4f, Color = H("#ffd36a"),
                    Strike = new StrikeDef { Duration = 0.18f, Velocity = new Vector2(14f, 0f), Damage = 60, Type = DamageType.Slash, Pose = FPose.Slash, Knockback = new Vector2(7f, 3f), StopOnHit = true, Color = H("#ffd36a"),
                        OnHit = (a, v, p) => { if (v.State == FState.Hitstun || a.M.Rng.Chance(0.3f)) { v.TakeTrueDamage(45f * p, a); VFX.WorldText(v.HeadPos + Vector2.up * 0.6f, "7 : 3 CRITICAL", new Color(1f, 0.85f, 0.4f), 1f); VFX.ImpactFrame(ImpactKind.Heavy, v.Center); } } } },
                new BurstAb { Name = "Structural Fault", Desc = "Strikes the weak point of the ground itself; it collapses upward.", Cost = 28, Cooldown = 8f, Offset = new Vector2(1.6f, 0.3f), Radius = 2.6f, Damage = 75, Vis = BurstVis.Explosion, Type = DamageType.Blunt, Knockback = new Vector2(6f, 9f), Pose = FPose.Slam, Color = H("#c8a070"), Sound = Sfx.Explosion },
                new StrikeAb { Name = "Clocking Out", Desc = "Four efficient strikes. Nothing wasted.", Cost = 35, Cooldown = 11f, Color = H("#ffe9a0"), RangeMax = 3f,
                    Strike = new StrikeDef { Duration = 0.4f, Velocity = new Vector2(8f, 0f), Hits = 4, Damage = 125, Type = DamageType.Slash, Pose = FPose.Slash, Radius = 1f, Color = H("#ffe9a0"), Sound = Sfx.Slash } },
                new BuffAb { Name = "Overtime", Desc = "After hours, the restraints come off: huge power and speed.", Cost = 60, Cooldown = 28f, Status = StatusType.Overtime, Mag = 0.5f, Status2 = StatusType.Haste, Mag2 = 0.2f, Duration = 12f, Color = H("#ffb030") },
            };

            // ---------------------------------------------------------- Crow mistress
            d = C("crows", "Madam Karasu", "Murder of Crows", "Black Bird Command", Era.Anime, Stance.Caster, Hair.Braid, FaceMark.None, Weapon.Axe, "#4a4a6a", "#ffd0a0");
            d.Hp = 1000; d.Ce = 110; d.Style = AIStyle.Zoner; d.PreferredRange = 5f;
            d.Bio = "A mercenary who commands crows and swings a giant axe. Will make her birds give their lives - for a price.";
            d.Kit = () => new Ability[]
            {
                new ProjectileAb { Name = "Crow Swarm", Desc = "Three crows dive at the target.", Cost = 14, Cooldown = 3f, Count = 3, Interval = 0.08f, Spread = 8f, Pose = FPose.Point, Color = H("#4a4a6a"),
                    Proj = new ProjDef { Vis = ProjVis.Bird, Color = H("#20202a"), Damage = 22, Speed = 16f, Size = 0.6f, Homing = 1.5f, Type = DamageType.Pierce, Launch = Sfx.Crow, Impact = Sfx.Punch, Knockback = new Vector2(3f, 2f) } },
                new StrikeAb { Name = "Axe Cleave", Desc = "A heavy axe swing.", Cost = 18, Cooldown = 5f, Color = H("#ffd0a0"),
                    Strike = new StrikeDef { Duration = 0.2f, Velocity = new Vector2(14f, 0f), Damage = 75, Type = DamageType.Slash, Pose = FPose.Slash, Flags = HitFlags.Heavy, Knockback = new Vector2(9f, 4f), Color = H("#ffd0a0"), Sound = Sfx.Slash } },
                new SummonAb { Name = "Murder", Desc = "A flock that harasses enemies.", Cost = 28, Cooldown = 12f, Count = 3, MaxActive = 3, Color = H("#4a4a6a"),
                    Minion = new MinionDef { Name = "Crow", Shape = MinionShape.Crow, Brain = MinionBrain.Chase, Color = H("#20202a"), Fly = true, Hp = 40, Damage = 14, Speed = 10f, Life = 8f, Size = 0.8f, Range = 0.7f, AttackRate = 0.6f, Type = DamageType.Pierce } },
                new SummonAb { Name = "Bird Strike", Desc = "A crow sacrifices itself at impossible speed. Pierces any defense.", Cost = 70, Cooldown = 26f, Count = 1, MaxActive = 1, Use = AIUse.Finisher, Color = H("#ffffff"),
                    Minion = new MinionDef { Name = "Bird Strike", Shape = MinionShape.Crow, Brain = MinionBrain.Kamikaze, Color = H("#ffffff"), Fly = true, Hp = 9999, Invulnerable = true, Damage = 230, Speed = 22f, Size = 1.5f, Life = 4f, Range = 0.5f, Knockback = new Vector2(12f, 6f), Hitstun = 0.8f,
                        Flags = HitFlags.Technique | HitFlags.Minion | HitFlags.Unblockable | HitFlags.PierceInfinity | HitFlags.Heavy } },
            };

            // ---------------------------------------------------------- Simple edge
            d = C("simpleedge", "Tetsuo Kurosawa", "Simple Edge", "New Moon Style", Era.Anime, Stance.Swordsman, Hair.Short, FaceMark.Beard, Weapon.Katana, "#a0d0ff", "#3a4a5a");
            d.Hp = 1100; d.Ce = 90; d.CeRegen = 6.5f; d.Passives = Passive.SimpleDomainMaster; d.Tier = 3; d.PreferredRange = 1.8f;
            d.Bio = "The strongest sorcerer without a cursed technique. Inside his simple domain, nothing gets past the blade.";
            d.Kit = () => new Ability[]
            {
                new CounterAb { Name = "Simple Domain: Draw", Desc = "Auto-counter anything that enters your circle.", Cost = 15, Cooldown = 6f, Window = 0.9f, Damage = 100, Type = DamageType.Slash, Color = H("#a0d0ff") },
                new StrikeAb { Name = "Batto Flash", Desc = "Draw-cut at full speed.", Cost = 15, Cooldown = 4f, Color = H("#a0d0ff"),
                    Strike = new StrikeDef { Duration = 0.14f, Velocity = new Vector2(25f, 0f), Damage = 68, Type = DamageType.Slash, Pose = FPose.Slash, Knockback = new Vector2(7f, 3f), Color = H("#a0d0ff"), Sound = Sfx.Slash } },
                new ProjectileAb { Name = "Evening Moon", Desc = "A thrown crescent of cursed energy.", Cost = 25, Cooldown = 7f, Pose = FPose.Slash, Color = H("#cfe4ff"), Proj = Slash("#cfe4ff", 70f, 24f, 1.0f) },
                new CustomAb { Name = "Simple Domain: Full Moon", Desc = "Expand the simple domain: block projectiles, harden yourself, cut intruders.", Cost = 60, Cooldown = 24f, CastTime = 0.2f, Pose = FPose.Block, Use = AIUse.Counter, RangeMax = 4f, Color = H("#a0d0ff"),
                    Fn = (f, p) =>
                    {
                        f.AddStatus(StatusType.DefenseUp, 6f, 0.4f, f);
                        f.M.SpawnZone(f, new ZoneDef { Width = 4.4f, Height = 3f, Life = 6f, TickDamage = 14f, TickRate = 0.25f, Status = StatusType.Slow, StatusMag = 0.4f, StatusTime = 0.4f, FollowOwner = true, BlocksProjectiles = true, Vis = ZoneVis.Sound, Color = H("#a0d0ff"), Type = DamageType.Slash }, f.Pos, p);
                        VFX.Ring(f.Center, H("#a0d0ff"), 2.4f, 0.6f, 0.1f);
                    } },
            };

            // ---------------------------------------------------------- Doll maker
            d = C("dollmaker", "Tetsuya Mogami", "Doll Maker", "Cursed Corpse Craft", Era.Anime, Stance.Brute, Hair.Short, FaceMark.Sunglasses | FaceMark.Beard, Weapon.None, "#ffb0a0", "#2a2a2a");
            d.Hp = 1100; d.Size = 1.1f; d.Weight = 1.2f; d.Speed = 0.92f; d.Ce = 120; d.Style = AIStyle.Summoner; d.PreferredRange = 4.5f;
            d.Bio = "A principal who sews cursed corpses into living dolls. Gentle hands, terrifying creations.";
            d.Kit = () => new Ability[]
            {
                new SummonAb { Name = "Cursed Doll", Desc = "A stitched doll that brawls for you.", Cost = 20, Cooldown = 7f, Count = 1, MaxActive = 1, Color = H("#ffb0a0"),
                    Minion = new MinionDef { Name = "Cursed Doll", Shape = MinionShape.Puppet, Brain = MinionBrain.Chase, Color = H("#ffb0a0"), Hp = 130, Damage = 22, Speed = 6.5f, Life = 14f, Range = 1f } },
                new SummonAb { Name = "Doll Charge", Desc = "A doll that sprints in and bursts.", Cost = 22, Cooldown = 7f, Count = 1, MaxActive = 1, Color = H("#ff8070"),
                    Minion = new MinionDef { Name = "Bomb Doll", Shape = MinionShape.Puppet, Brain = MinionBrain.Kamikaze, Color = H("#ff8070"), Hp = 60, Damage = 50, Speed = 11f, Life = 5f, Range = 0.6f, Knockback = new Vector2(8f, 6f) } },
                new SummonAb { Name = "Prototype Beast", Desc = "A large experimental corpse with three cores.", Cost = 45, Cooldown = 16f, Count = 1, MaxActive = 1, Color = H("#f4f4f4"),
                    Minion = new MinionDef { Name = "Prototype", Shape = MinionShape.Humanoid, Brain = MinionBrain.Chase, Color = H("#f4f4f4"), Hp = 240, Damage = 40, Speed = 5.5f, Size = 1.6f, Life = 15f, Range = 1.3f, Knockback = new Vector2(8f, 5f), Hitstun = 0.5f } },
                new SummonAb { Name = "Puppet Parade", Desc = "Unleash every doll in the workshop.", Cost = 75, Cooldown = 30f, Count = 2, MaxActive = 2, Use = AIUse.Summon, Color = H("#ffd0c0"),
                    Minion = new MinionDef { Name = "Parade Doll", Shape = MinionShape.Puppet, Brain = MinionBrain.Chase, Color = H("#ffd0c0"), Hp = 160, Damage = 28, Speed = 7f, Life = 14f, Range = 1f } },
            };

            // ---------------------------------------------------------- Field medic (support)
            d = C("medic", "Dr. Rei Kanase", "Field Medic", "Reverse Output", Era.Anime, Stance.Elegant, Hair.Long, FaceMark.None, Weapon.Pen, "#a0ffc0", "#3a3a3a");
            d.Hp = 950; d.Ce = 120; d.CeRegen = 8.5f; d.Passives = Passive.RCT | Passive.Support; d.RctRate = 60f; d.Style = AIStyle.Support; d.Tier = 3; d.PreferredRange = 5f;
            d.Bio = "One of the rare sorcerers who can heal others with reverse cursed energy. Tired, chain-smoking, irreplaceable.";
            d.Kit = () => new Ability[]
            {
                new ProjectileAb { Name = "Scalpel Toss", Desc = "Precise thrown scalpels.", Physical = true, Cost = 0, Cooldown = 1.8f, Count = 2, Interval = 0.06f, Pose = FPose.Throw, Color = H("#e0f0ff"),
                    Proj = new ProjDef { Vis = ProjVis.Needle, Color = H("#e0f0ff"), Damage = 26, Speed = 28f, Size = 0.25f, Type = DamageType.Pierce, Pierce = 1, Flags = HitFlags.None, Launch = Sfx.WhooshLight, Impact = Sfx.Nail } },
                new BuffAb { Name = "Reverse Output", Desc = "Heals you and every ally.", Cost = 30, Cooldown = 11f, Allies = true, Status = StatusType.Count, Heal = 140f, Color = H("#a0ffc0"), Use = AIUse.Heal },
                new ZoneAb { Name = "Smoke Break", Desc = "A cloud of smoke that slows and chokes.", Cost = 20, Cooldown = 10f, Color = H("#b0b0b0"),
                    Zone = new ZoneDef { Width = 3.5f, Height = 3f, Life = 4f, TickDamage = 11f, TickRate = 0.3f, Status = StatusType.Slow, StatusMag = 0.4f, Vis = ZoneVis.Smoke, Color = H("#b0b0b0"), Type = DamageType.Poison } },
                new BuffAb { Name = "Emergency Surgery", Desc = "Mass healing, cleanse and protection for the whole team.", Cost = 80, Cooldown = 30f, Allies = true, Heal = 300f, Cleanse = true, Status = StatusType.DefenseUp, Mag = 0.3f, Duration = 6f, CastTime = 0.6f, Color = H("#a0ffc0"), Use = AIUse.Heal },
            };

            // ---------------------------------------------------------- Warp page
            d = C("warp", "Kaiko", "Warp Page", "Spatial Transfer", Era.Manga, Stance.Agile, Hair.Bob, FaceMark.None, Weapon.None, "#c0a0ff", "#ffffff");
            d.Hp = 900; d.Speed = 1.15f; d.Ce = 110; d.CeRegen = 8f; d.Style = AIStyle.Trickster; d.Tier = 3; d.PreferredRange = 3f;
            d.Bio = "A loyal attendant who can teleport anyone she touches. Never where you expect her.";
            d.Kit = () => new Ability[]
            {
                new StrikeAb { Name = "Warp Strike", Desc = "Teleport behind the enemy and strike.", Cost = 15, Cooldown = 4f, Color = H("#c0a0ff"), RangeMax = 10f,
                    Strike = new StrikeDef { TeleportBehind = true, Duration = 0.15f, Velocity = new Vector2(6f, 0f), Damage = 50, Pose = FPose.Kick, Color = H("#c0a0ff") } },
                new CustomAb { Name = "Escape Gate", Desc = "Warp to the far side of the arena.", Cost = 15, Cooldown = 7f, CastTime = 0.05f, Use = AIUse.Escape, Pose = FPose.Point, Color = H("#c0a0ff"),
                    Fn = (f, p) => { var t = f.Target; float x = t != null && t.Pos.x > 0 ? f.M.Arena.Left + 1.5f : f.M.Arena.Right - 1.5f; f.Teleport(new Vector2(x, 0f)); f.Invuln = Mathf.Max(f.Invuln, 0.3f); } },
                new ProjectileAb { Name = "Portal Drop", Desc = "Opens portals overhead and drops debris.", Cost = 25, Cooldown = 8f, Count = 3, Interval = 0.12f, FromSky = true, Pose = FPose.Raise, Color = H("#c0a0ff"),
                    Proj = new ProjDef { Vis = ProjVis.Rock, Color = H("#8a8090"), Damage = 35, Speed = 20f, Size = 0.7f, Gravity = 8f, Type = DamageType.Blunt, Flags = HitFlags.Technique, Knockback = new Vector2(3f, -4f), Launch = Sfx.Teleport, Impact = Sfx.PunchHeavy } },
                new CustomAb { Name = "Mass Displacement", Desc = "Hurls every enemy through space: scattered, dazed and hurt.", Cost = 70, Cooldown = 26f, CastTime = 0.3f, Pose = FPose.Raise, Use = AIUse.Finisher, RangeMax = 30f, Color = H("#c0a0ff"),
                    Fn = (f, p) =>
                    {
                        foreach (var e in f.M.Fighters)
                        {
                            if (!f.M.IsEnemy(f, e) || e.Dead || e.IsBoss) continue;
                            float x = f.M.Rng.Range(f.M.Arena.Left + 1f, f.M.Arena.Right - 1f);
                            e.Teleport(new Vector2(x, 4f));
                            var h = HitInfo.Make(f, HitSource.Other, 60f * p, new Vector2(0f, -8f), 0.5f, HitFlags.Technique | HitFlags.Unblockable, DamageType.Energy, e.Center, H("#c0a0ff"));
                            f.M.Hit(e, h.WithStatus(StatusType.Stun, 0.8f, 0f));
                        }
                    } },
            };

            // ---------------------------------------------------------- Auspicious masks
            d = C("masks", "Kai Tomoe", "Auspicious Masks", "Four Sacred Beasts", Era.Manga, Stance.Martial, Hair.Short, FaceMark.Mask, Weapon.None, "#6ae0ff", "#f0f0f0");
            d.Hp = 1000; d.Ce = 105; d.PreferredRange = 3.5f;
            d.Bio = "Channels four auspicious beasts by covering his eyes. Each mask brings a different beast and a different trick.";
            d.Kit = () => new Ability[]
            {
                new ProjectileAb { Name = "Horned Mask", Desc = "A spiraling horn that pierces.", Cost = 14, Cooldown = 3f, Pose = FPose.Point, Color = H("#ffffff"),
                    Proj = new ProjDef { Vis = ProjVis.Needle, Color = H("#ffffff"), Damage = 45, Speed = 26f, Size = 0.5f, Pierce = 1, Type = DamageType.Pierce, Knockback = new Vector2(5f, 2f) } },
                new ProjectileAb { Name = "Shell Mask", Desc = "A ground-hugging torrent of water that slows.", Cost = 20, Cooldown = 6f, Pose = FPose.Slam, Color = H("#6ae0ff"),
                    Proj = new ProjDef { Vis = ProjVis.Wave, Color = H("#6ae0ff"), Damage = 50, Speed = 14f, Size = 1.1f, Ground = true, Type = DamageType.Water, Status = StatusType.Slow, StatusTime = 2f, StatusMag = 0.4f, Life = 1.6f, Launch = Sfx.Water, Impact = Sfx.Water } },
                new BuffAb { Name = "Kirin Mask", Desc = "Floods the brain with chemicals: no pain, more speed.", Cost = 30, Cooldown = 15f, Status = StatusType.Armor, Mag = 1f, Status2 = StatusType.Haste, Mag2 = 0.25f, Duration = 6f, Color = H("#ffe680") },
                new BeamAb { Name = "Dragon Mask", Desc = "A dragon of water roars across the arena.", Cost = 70, Cooldown = 24f, Pose = FPose.TwoPalm, Use = AIUse.Finisher, Color = H("#6ae0ff"),
                    Beam = new BeamDef { Length = 18f, Width = 1.4f, Duration = 0.9f, TickDamage = 16f, TickRate = 0.08f, Color = H("#6ae0ff"), Core = Color.white, Type = DamageType.Water, Status = StatusType.Slow, StatusTime = 1f, StatusMag = 0.4f, Charge = 0.3f, Knockback = new Vector2(6f, 2f), Sound = Sfx.Water } },
            };

            // ---------------------------------------------------------- Injury stopper (support)
            d = C("stopper", "Kenji Arai", "Injury Stopper", "Wound Freeze", Era.Anime, Stance.Brawler, Hair.Short, FaceMark.None, Weapon.None, "#9affc0", "#2a3a2a");
            d.Hp = 1050; d.Ce = 100; d.CeRegen = 8f; d.Passives = Passive.Support; d.Style = AIStyle.Support; d.Tier = 3; d.PreferredRange = 2f;
            d.Bio = "Can't heal anyone - but he can stop a wound from getting worse. On the front line, that's everything.";
            d.Kit = () => new Ability[]
            {
                new StrikeAb { Name = "Field Kick", Desc = "A solid, practical kick.", Physical = true, Cost = 0, Cooldown = 3f, Color = H("#9affc0"),
                    Strike = new StrikeDef { Duration = 0.18f, Velocity = new Vector2(13f, 0f), Damage = 45, Pose = FPose.Kick, Knockback = new Vector2(8f, 4f), Color = H("#9affc0") } },
                new BuffAb { Name = "Stop the Bleeding", Desc = "Seals wounds: heal and cleanse yourself and allies.", Cost = 25, Cooldown = 12f, Allies = true, Heal = 80f, Cleanse = true, Status = StatusType.Count, Color = H("#9affc0"), Use = AIUse.Heal },
                new BuffAb { Name = "Hold the Line", Desc = "Allies take much less damage for a while.", Cost = 25, Cooldown = 14f, Allies = true, Status = StatusType.DefenseUp, Mag = 0.4f, Duration = 6f, Color = H("#c0ffd8") },
                new BuffAb { Name = "Triage", Desc = "Big team heal, and everyone survives the next lethal blow.", Cost = 70, Cooldown = 30f, Allies = true, Heal = 220f, Status = StatusType.Lucky, Mag = 1f, Duration = 8f, Color = H("#9affc0"), Use = AIUse.Heal },
            };

            // ---------------------------------------------------------- Star rage
            d = C("starrage", "Yuna Hoshikawa", "Star Rage", "Mass Surge", Era.Manga, Stance.Martial, Hair.Long, FaceMark.None, Weapon.None, "#ffd84f", "#ff9a3a");
            d.Hp = 1050; d.Power = 1.2f; d.Ce = 140; d.CeRegen = 8f; d.Tier = 1; d.PreferredRange = 1.8f;
            d.Bio = "Adds virtual mass to herself and anything she touches. Her punches hit like collapsing stars.";
            d.Kit = () => new Ability[]
            {
                new StrikeAb { Name = "Mass Surge", Desc = "A punch carrying impossible mass.", Cost = 18, Cooldown = 4f, Color = H("#ffd84f"),
                    Strike = new StrikeDef { Duration = 0.18f, Velocity = new Vector2(16f, 0f), Damage = 85, Knockback = new Vector2(14f, 6f), Flags = HitFlags.Heavy, Pose = FPose.HeavyPunch, StopOnHit = true, Color = H("#ffd84f"), Hitstop = 0.12f } },
                new SummonAb { Name = "Galura", Desc = "Her bird shikigami: grows heavier as it strikes.", Cost = 30, Cooldown = 14f, Count = 1, MaxActive = 1, Color = H("#ffd84f"),
                    Minion = new MinionDef { Name = "Galura", Shape = MinionShape.Garuda, Brain = MinionBrain.Chase, Color = H("#ffd84f"), Fly = true, Hp = 200, Damage = 38, Speed = 8f, Size = 1.6f, Life = 10f, Range = 1f, Knockback = new Vector2(8f, 4f) } },
                new BurstAb { Name = "Weighted Landing", Desc = "Gains mass and crashes down, cratering the ground.", Cost = 40, Cooldown = 12f, Radius = 2.6f, Damage = 120, Knockback = new Vector2(8f, 10f), Vis = BurstVis.Gravity, Delay = 0.2f, Pose = FPose.Slam, Color = H("#ffd84f"), Shake = 0.6f },
                new CustomAb { Name = "Singularity Fist", Desc = "Infinite mass: a black hole that swallows, then detonates.", Cost = 90, Cooldown = 32f, CastTime = 0.5f, Pose = FPose.HeavyPunch, Use = AIUse.Finisher, RangeMax = 10f, Color = H("#ffd84f"), Armored = true,
                    Fn = (f, p) =>
                    {
                        var hole = new ProjDef
                        {
                            Vis = ProjVis.Sphere, Color = H("#1a0a2a"), Core = H("#ffd84f"), Size = 1.6f, Speed = 2.5f, Life = 2.2f, Damage = 14f, TickInterval = 0.2f,
                            PullRadius = 6.5f, PullForce = 14f, Unerasable = true, Erase = true, ExplodeRadius = 3.6f, ExplodeDamage = 200f, ExplodeKb = new Vector2(14f, 9f),
                            Flags = HitFlags.Technique | HitFlags.Unblockable, Type = DamageType.Gravity, Hitstun = 0.2f, Knockback = Vector2.zero, Launch = Sfx.Rumble, Impact = Sfx.Explosion
                        };
                        f.M.Shoot(f, hole, f.Front(2f, 1.3f), new Vector2(f.Facing, 0f), p);
                        CameraRig.Shake(0.5f);
                    } },
            };
        }

        // ---------------------------------------------------------------- the strongest's techniques (shared with the borrowed vessel)
        static ProjectileAb BlueAb() => new ProjectileAb
        {
            Name = "Attraction: Azure", Desc = "A point of negative space that drags everything in and grinds it.", Cost = 20, Cooldown = 4.5f, Pose = FPose.Point, Color = Art.Hex("#3a7bff"),
            RangeMin = 1f, RangeMax = 9f,
            Proj = new ProjDef
            {
                Vis = ProjVis.Orb, Color = Art.Hex("#3a7bff"), Core = Color.white, Size = 0.8f, Speed = 9f, Life = 1.4f, PullRadius = 3.5f, PullForce = 9f,
                TickInterval = 0.15f, Damage = 9f, Hitstun = 0.18f, Knockback = new Vector2(0.5f, 0.5f), ExplodeRadius = 1.6f, ExplodeDamage = 30f,
                Type = DamageType.Gravity, Launch = Sfx.Infinity, Impact = Sfx.ImpactEnergy
            }
        };

        static ProjectileAb RedAb() => new ProjectileAb
        {
            Name = "Reversal: Scarlet", Desc = "Repelling force fired as a blast. Hold to chant for a bigger blast.", Cost = 30, Cooldown = 7f, Pose = FPose.Point, Color = Art.Hex("#ff3344"),
            Chantable = true, MaxChant = 1, Chant = new[] { "Phase. Twilight. Eyes of wisdom." },
            Proj = new ProjDef
            {
                Vis = ProjVis.Orb, Color = Art.Hex("#ff3344"), Core = Color.white, Size = 0.6f, Speed = 30f, Damage = 85f, Knockback = new Vector2(16f, 6f), Hitstun = 0.6f,
                PullRadius = 2f, PullForce = -12f, Flags = HitFlags.Technique | HitFlags.Heavy, ExplodeRadius = 1.8f, ExplodeDamage = 25f, Type = DamageType.Energy,
                Launch = Sfx.Shoot, Impact = Sfx.Explosion
            }
        };

        static ProjectileAb PurpleAb() => new ProjectileAb
        {
            Name = "Annihilation: Violet", Desc = "Attraction and repulsion collide into imaginary mass that erases everything in its path. Hold to chant.", Cost = 60, Cooldown = 16f,
            Pose = FPose.TwoPalm, Color = Art.Hex("#b04aff"), Chantable = true, MaxChant = 2, Use = AIUse.Finisher, CastTime = 0.45f, Armored = true,
            Chant = new[] { "Nine ropes. Polarized light. Crow and declaration.", "Between front and back... Annihilate." },
            Proj = new ProjDef
            {
                Vis = ProjVis.Sphere, Color = Art.Hex("#b04aff"), Core = Color.white, Size = 2.0f, Speed = 12f, Life = 3f, Damage = 180f, Erase = true, Unerasable = true,
                Pierce = 3, GrowRate = 0.2f, Knockback = new Vector2(12f, 6f), Hitstun = 0.8f, Hitstop = 0.14f, DestroyOnWall = false,
                Flags = HitFlags.Technique | HitFlags.Heavy | HitFlags.Unblockable | HitFlags.PierceInfinity, Type = DamageType.Energy, Launch = Sfx.Beam, Impact = Sfx.Explosion
            }
        };
    }
}
