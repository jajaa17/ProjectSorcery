using UnityEngine;

namespace ProjectSorcery
{
    public static partial class Roster
    {
        // ===================================================================== KYOTO SCHOOL
        static void AddKyoto()
        {
            // ---------------------------------------------------------- Best Friend
            var d = C("bestfriend", "Goro Tatsuta", "Best Friend", "Clap Shift", Era.Anime, Stance.Wrestler, Hair.TopKnot, FaceMark.Scar, Weapon.None, "#7affd8", "#3a2a1a");
            d.Hp = 1150; d.Size = 1.12f; d.Weight = 1.25f; d.Power = 1.15f; d.Ce = 100; d.Style = AIStyle.Grappler; d.PreferredRange = 1.6f;
            d.Passives = Passive.BlackFlashAffinity | Passive.ToughBody; d.BlackFlashBonus = 0.02f;
            d.Bio = "A towering brawler with a clap that swaps the places of anything with cursed energy. Is that a feint or a swap? You'll never know.";
            d.Kit = () => new Ability[]
            {
                new CustomAb { Name = "Clap Shift", Desc = "Clap: swap places with the nearest enemy, leaving them open.", Cost = 15, Cooldown = 4f, CastTime = 0.12f, Pose = FPose.Clap, Use = AIUse.Utility, RangeMax = 12f, Color = H("#7affd8"),
                    Fn = (f, p) => { Audio.Play(Sfx.Clap, f.Center, 1f); var t = f.Target; if (t != null) SwapPlaces(f, t, 0.35f); } },
                new CustomAb { Name = "Feint Clap", Desc = "Clap... but don't swap. Enemies flinch anyway, then you charge.", Cost = 10, Cooldown = 3f, CastTime = 0.12f, Pose = FPose.Clap, Use = AIUse.Gapclose, RangeMax = 6f, Color = H("#b0ffe8"),
                    Fn = (f, p) =>
                    {
                        Audio.Play(Sfx.Clap, f.Center, 1f, 1.1f);
                        VFX.Burst(BurstVis.Sound, f.Front(0.4f, 1.4f), 2f, H("#b0ffe8"));
                        f.BeginStrike(new StrikeDef { Duration = 0.2f, Velocity = new Vector2(19f, 0f), Damage = 60, Knockback = new Vector2(8f, 4f), Pose = FPose.HeavyPunch, StopOnHit = true, Color = H("#7affd8") }, p);
                    } },
                new ProjectileAb { Name = "Boulder Toss", Desc = "Hurls a chunk of the arena.", Cost = 25, Cooldown = 7f, Pose = FPose.Throw, Arc = 12f, Color = H("#8a8070"),
                    Proj = new ProjDef { Vis = ProjVis.Rock, Color = H("#8a8070"), Damage = 75, Speed = 17f, Size = 0.9f, Gravity = 9f, Type = DamageType.Blunt, Flags = HitFlags.Heavy, Knockback = new Vector2(9f, 6f), ExplodeRadius = 1.2f, ExplodeDamage = 20f, Launch = Sfx.Whoosh, Impact = Sfx.PunchHeavy } },
                new CustomAb { Name = "Brotherly Rush", Desc = "Swap in behind your enemy and finish with a guaranteed Black Flash.", Cost = 70, Cooldown = 25f, CastTime = 0.1f, Pose = FPose.Clap, Use = AIUse.Finisher, RangeMax = 12f, Color = H("#ff2040"),
                    Fn = (f, p) =>
                    {
                        Audio.Play(Sfx.Clap, f.Center, 1f, 0.9f);
                        var t = f.Target;
                        if (t != null) SwapPlaces(f, t, 0.5f);
                        f.BeginStrike(new StrikeDef { Duration = 0.45f, Velocity = new Vector2(7f, 0f), Hits = 4, HitInterval = 0.08f, Damage = 120, BlackFlash = true, Pose = FPose.HeavyPunch, Radius = 1.0f, Color = H("#ff2040") }, p);
                    } },
            };

            // ---------------------------------------------------------- Simple-domain swordswoman
            d = C("broke", "Mizuki Wada", "Broke Swordswoman", "New Moon Style", Era.Anime, Stance.Swordsman, Hair.Long, FaceMark.None, Weapon.Katana, "#7fb8ff", "#2a4a8a");
            d.Hp = 1080; d.Ce = 95; d.CeRegen = 7f; d.Speed = 1.08f; d.Passives = Passive.SimpleDomainMaster; d.Tier = 3; d.PreferredRange = 1.7f;
            d.Bio = "No flashy technique - just a simple domain and a fast draw. Underestimated by everyone, which is exactly the point.";
            d.Kit = () => new Ability[]
            {
                new CounterAb { Name = "Simple Domain: Draw", Desc = "Anything entering your small domain is cut down automatically.", Cost = 15, Cooldown = 6f, Window = 0.8f, Damage = 95, Type = DamageType.Slash, Color = H("#9fd0ff") },
                new StrikeAb { Name = "Quick Draw", Desc = "A blindingly fast iaido dash.", Cost = 15, Cooldown = 4f, Color = H("#9fd0ff"),
                    Strike = new StrikeDef { Duration = 0.14f, Velocity = new Vector2(26f, 0f), Damage = 65, Type = DamageType.Slash, Pose = FPose.Slash, Knockback = new Vector2(7f, 3f), Color = H("#9fd0ff"), Sound = Sfx.Slash } },
                new StrikeAb { Name = "Desperate Flurry", Desc = "Everything she has, in five cuts.", Cost = 35, Cooldown = 11f, Color = H("#cfe4ff"), RangeMax = 3f,
                    Strike = new StrikeDef { Duration = 0.45f, Velocity = new Vector2(8f, 0f), Hits = 5, Damage = 135, Type = DamageType.Slash, Pose = FPose.Slash, Radius = 1.0f, Color = H("#cfe4ff"), Sound = Sfx.Slash } },
                new ProjectileAb { Name = "Moonlit Arc", Desc = "A crescent of cursed energy launched from the sheath.", Cost = 60, Cooldown = 20f, Pose = FPose.Slash, Use = AIUse.Finisher, Color = H("#9fd0ff"),
                    Proj = new ProjDef { Vis = ProjVis.Slash, Color = H("#9fd0ff"), Core = Color.white, Damage = 170, Speed = 28f, Size = 1.6f, Type = DamageType.Slash, Pierce = 2, Flags = HitFlags.Technique | HitFlags.Heavy, Knockback = new Vector2(10f, 4f), Launch = Sfx.Slash, Impact = Sfx.Slash } },
            };

            // ---------------------------------------------------------- Broom rider
            d = C("broom", "Mimi Kazeno", "Broom Rider", "Wind Broom", Era.Anime, Stance.Floaty, Hair.Twin, FaceMark.None, Weapon.Broom, "#b0ffe0", "#ffd080");
            d.Hp = 920; d.Ce = 100; d.CeRegen = 7f; d.Speed = 1.15f; d.Jump = 1.2f; d.Passives = Passive.Flight; d.Style = AIStyle.Zoner; d.Tier = 3; d.PreferredRange = 6f;
            d.Bio = "Rides her broom over the battlefield, raining wind blades from out of reach.";
            d.Kit = () => new Ability[]
            {
                new ProjectileAb { Name = "Gale Slice", Desc = "A pair of wind blades.", Cost = 12, Cooldown = 2.5f, Count = 2, Spread = 10f, Pose = FPose.Point, Color = H("#b0ffe0"),
                    Proj = new ProjDef { Vis = ProjVis.Slash, Color = H("#b0ffe0"), Damage = 28, Speed = 20f, Size = 0.6f, Type = DamageType.Slash, Knockback = new Vector2(4f, 2f), Launch = Sfx.Wind } },
                new StrikeAb { Name = "Sky Ride", Desc = "Swoops through the air, ramming anything in the way.", Cost = 18, Cooldown = 5f, Use = AIUse.Escape, Color = H("#b0ffe0"),
                    Strike = new StrikeDef { Duration = 0.35f, Velocity = new Vector2(16f, 5f), Damage = 45, Pose = FPose.Dash, Invuln = true, Color = H("#b0ffe0"), Sound = Sfx.Wind } },
                new ZoneAb { Name = "Whirlwind", Desc = "A cyclone that slows and shreds.", Cost = 30, Cooldown = 10f, Color = H("#b0ffe0"),
                    Zone = new ZoneDef { Width = 3.5f, Height = 4f, Life = 3.5f, TickDamage = 9f, TickRate = 0.25f, Status = StatusType.Slow, StatusMag = 0.35f, Vis = ZoneVis.Smoke, Color = H("#b0ffe0"), Type = DamageType.Slash } },
                new ProjectileAb { Name = "Tempest", Desc = "A storm of wind blades from above.", Cost = 70, Cooldown = 24f, Count = 7, Interval = 0.08f, FromSky = true, Pose = FPose.Raise, Use = AIUse.Finisher, Color = H("#b0ffe0"),
                    Proj = new ProjDef { Vis = ProjVis.Slash, Color = H("#b0ffe0"), Damage = 32, Speed = 22f, Size = 0.8f, Type = DamageType.Slash, Knockback = new Vector2(3f, -3f), Launch = Sfx.Wind } },
            };

            // ---------------------------------------------------------- Single bullet
            d = C("bullet", "Sayo Kuroba", "Last Bullet", "Construction", Era.Anime, Stance.Caster, Hair.Bob, FaceMark.None, Weapon.Revolver, "#ffd36a", "#2a2a2a");
            d.Hp = 950; d.Ce = 90; d.CeRegen = 6f; d.Style = AIStyle.Zoner; d.Tier = 3; d.PreferredRange = 7f;
            d.Bio = "A revolver and a technique that can create one thing from nothing - once a day.";
            d.Kit = () => new Ability[]
            {
                new ProjectileAb { Name = "Revolver Shot", Desc = "A fast, precise shot.", Physical = true, Cost = 0, Cooldown = 1.6f, Pose = FPose.Shoot, Color = H("#ffd36a"),
                    Proj = new ProjDef { Vis = ProjVis.Bullet, Color = H("#ffd36a"), Damage = 30, Speed = 40f, Size = 0.22f, Type = DamageType.Pierce, Flags = HitFlags.None, Knockback = new Vector2(3f, 1f), Launch = Sfx.Gun, Impact = Sfx.Punch } },
                new ProjectileAb { Name = "Fan the Hammer", Desc = "Empties the cylinder in a spread.", Cost = 15, Cooldown = 6f, Count = 5, Spread = 6f, Interval = 0.05f, Pose = FPose.Shoot, Color = H("#ffd36a"),
                    Proj = new ProjDef { Vis = ProjVis.Bullet, Color = H("#ffd36a"), Damage = 18, Speed = 38f, Size = 0.2f, Type = DamageType.Pierce, Knockback = new Vector2(2f, 1f), Launch = Sfx.Gun } },
                new ZoneAb { Name = "Smoke Cover", Desc = "Pops smoke that slows anyone chasing you.", Cost = 20, Cooldown = 10f, AtTarget = false, Use = AIUse.Escape, Color = H("#888888"),
                    Zone = new ZoneDef { Width = 4f, Height = 3f, Life = 3f, TickDamage = 0f, TickRate = 0.3f, Status = StatusType.Slow, StatusMag = 0.45f, Vis = ZoneVis.Smoke, Color = H("#9a9a9a") } },
                new ProjectileAb { Name = "Construction: Last Bullet", Desc = "A bullet made from her own life. Pierces everything.", Cost = 85, Cooldown = 30f, Pose = FPose.Shoot, Use = AIUse.Finisher, Color = H("#ffe9a0"),
                    Proj = new ProjDef { Vis = ProjVis.Bolt, Color = H("#ffe9a0"), Core = Color.white, Damage = 260, Speed = 50f, Size = 0.5f, Type = DamageType.Pierce, Pierce = 3, Unerasable = true, Flags = HitFlags.Technique | HitFlags.Heavy | HitFlags.Unblockable, Knockback = new Vector2(12f, 4f), Launch = Sfx.Gun, Impact = Sfx.Explosion } },
            };

            // ---------------------------------------------------------- Blood heir
            d = C("bloodheir", "Toshi Akaba", "Blood Heir", "Blood Arts", Era.Anime, Stance.Martial, Hair.Short, FaceMark.None, Weapon.Bow, "#ff4a5a", "#3a0a10");
            d.Hp = 1080; d.Ce = 110; d.CeRegen = 8f; d.Style = AIStyle.Zoner; d.PreferredRange = 6f;
            d.Bio = "Heir of an old blood-manipulating clan, firing arrows of his own blood that bend through the air.";
            d.Kit = () => new Ability[]
            {
                new ProjectileAb { Name = "Blood Arrow", Desc = "A homing arrow of blood.", Cost = 14, Cooldown = 2.2f, Pose = FPose.Bow, Color = H("#ff4a5a"),
                    Proj = new ProjDef { Vis = ProjVis.Arrow, Color = H("#ff3040"), Damage = 56, Speed = 24f, Size = 0.4f, Homing = 2.2f, Type = DamageType.Blood, Knockback = new Vector2(4f, 2f), Launch = Sfx.WhooshLight, Impact = Sfx.Blood } },
                new ProjectileAb { Name = "Crimson Binding", Desc = "A blood rope that binds on contact.", Cost = 22, Cooldown = 7f, Pose = FPose.Throw, Color = H("#c01828"),
                    Proj = new ProjDef { Vis = ProjVis.Blood, Color = H("#c01828"), Damage = 40, Speed = 20f, Size = 0.5f, Type = DamageType.Blood, Status = StatusType.Bind, StatusTime = 1.2f, Knockback = Vector2.zero, Hitstun = 0.2f, Launch = Sfx.Blood, Impact = Sfx.Blood } },
                new BuffAb { Name = "Red Scale Surge", Desc = "Boils the blood: faster and stronger.", Cost = 30, Cooldown = 15f, Status = StatusType.Haste, Mag = 0.25f, Status2 = StatusType.PowerUp, Mag2 = 0.25f, Duration = 8f, Color = H("#ff2040") },
                new BeamAb { Name = "Convergence Lance", Desc = "All the blood he has, compressed into a single piercing line.", Cost = 75, Cooldown = 24f, Chantable = true, MaxChant = 1, Chant = new[] { "Compress..." }, Pose = FPose.Bow, Use = AIUse.Finisher, Color = H("#ff2040"),
                    Beam = new BeamDef { Length = 20f, Width = 0.6f, Duration = 0.5f, TickDamage = 34f, TickRate = 0.05f, Color = H("#ff2040"), Core = H("#ffd0d0"), Type = DamageType.Blood, Charge = 0.3f, Knockback = new Vector2(5f, 1f), Sound = Sfx.Blood } },
            };

            // ---------------------------------------------------------- Puppet mech (reverse restriction)
            d = C("mech", "Unit Zero", "Puppet Mech", "Puppet Link", Era.Anime, Stance.Mechanical, Hair.None, FaceMark.Goggles, Weapon.None, "#5ad0ff", "#8a8f9a", "#b8c0cc");
            d.Hp = 800; d.Ce = 170; d.CeRegen = 10f; d.Size = 1.1f; d.Passives = Passive.Restricted; d.Style = AIStyle.Zoner; d.PreferredRange = 6f;
            d.Bio = "A broken body traded for enormous cursed energy range, piloting a puppet stocked with ten years of saved output.";
            d.Kit = () => new Ability[]
            {
                new StrikeAb { Name = "Sword Arm", Desc = "Arm unfolds into a blade and lunges.", Cost = 15, Cooldown = 4f, Color = H("#5ad0ff"),
                    Strike = new StrikeDef { Duration = 0.2f, Velocity = new Vector2(16f, 0f), Damage = 55, Type = DamageType.Slash, Pose = FPose.Stab, Knockback = new Vector2(7f, 3f), Color = H("#5ad0ff") } },
                new BeamAb { Name = "Arm Cannon", Desc = "A sustained cursed energy beam.", Cost = 25, Cooldown = 6f, Pose = FPose.Point, Color = H("#5ad0ff"),
                    Beam = new BeamDef { Length = 14f, Width = 0.5f, Duration = 0.6f, TickDamage = 11f, TickRate = 0.07f, Color = H("#5ad0ff"), Core = Color.white, Charge = 0.2f } },
                new BuffAb { Name = "Absolute Mode", Desc = "Unlocks stored output: armor and power.", Cost = 40, Cooldown = 18f, Status = StatusType.DefenseUp, Mag = 0.3f, Status2 = StatusType.PowerUp, Mag2 = 0.3f, Duration = 9f, Color = H("#5ad0ff") },
                new BeamAb { Name = "Ultra Cannon", Desc = "Fires a decade of stored cursed energy at once.", Cost = 100, Cooldown = 30f, Chantable = true, MaxChant = 1, Chant = new[] { "Release all reserves." }, Pose = FPose.TwoPalm, Use = AIUse.Finisher, Color = H("#9fe8ff"), Armored = true,
                    Beam = new BeamDef { Length = 22f, Width = 2f, Duration = 1.1f, TickDamage = 17f, TickRate = 0.07f, Color = H("#5ad0ff"), Core = Color.white, Charge = 0.5f, Knockback = new Vector2(6f, 2f) } },
            };

            // ---------------------------------------------------------- Solo choir (support)
            d = C("choir", "Kiyo Utagawa", "Solo Choir", "Forbidden Solo", Era.Anime, Stance.Caster, Hair.Long, FaceMark.Scar, Weapon.Fan, "#ffd0f0", "#a03050");
            d.Hp = 1080; d.Ce = 120; d.CeRegen = 9f; d.Passives = Passive.Support; d.Style = AIStyle.Support; d.Tier = 3; d.PreferredRange = 5f;
            d.Bio = "A ritual dancer whose performance amplifies the cursed energy of every ally around her.";
            d.Kit = () => new Ability[]
            {
                new ProjectileAb { Name = "Cursed Note", Desc = "A pair of ringing notes of cursed energy.", Cost = 10, Cooldown = 1.8f, Count = 2, Interval = 0.08f, Spread = 8f, Pose = FPose.Point, Color = H("#ffd0f0"),
                    Proj = new ProjDef { Vis = ProjVis.Sound, Color = H("#ffd0f0"), Damage = 42, Speed = 18f, Size = 0.7f, Type = DamageType.Sound, Pierce = 1, Launch = Sfx.Bell, Impact = Sfx.ImpactEnergy } },
                new BuffAb { Name = "Ritual Step", Desc = "A dance that strengthens you and your allies.", Cost = 25, Cooldown = 12f, Allies = true, Status = StatusType.PowerUp, Mag = 0.25f, Duration = 8f, Color = H("#ffd0f0") },
                new BuffAb { Name = "Bell Ward", Desc = "Hardens you and your allies against harm.", Cost = 25, Cooldown = 14f, Allies = true, Status = StatusType.DefenseUp, Mag = 0.3f, Duration = 7f, Cleanse = true, Color = H("#fff0a0") },
                new BuffAb { Name = "Solo Forbidden Area", Desc = "The full performance: huge output boost and CE restored for the whole team.", Cost = 70, Cooldown = 28f, Allies = true, Status = StatusType.PowerUp, Mag = 0.55f, Status2 = StatusType.Haste, Mag2 = 0.2f, Duration = 10f, CeRestore = 40f, CastTime = 0.6f, Color = H("#ff9ff0"), Use = AIUse.Buff },
            };

            // ---------------------------------------------------------- Rock principal
            d = C("principal", "Gakuro Fujiwara", "Rock Principal", "Sonic Resonance", Era.Anime, Stance.Brute, Hair.Bald, FaceMark.Beard, Weapon.Guitar, "#ff6a3a", "#ffcf6a");
            d.Hp = 1100; d.Power = 1.05f; d.Speed = 0.9f; d.Ce = 110; d.Style = AIStyle.Zoner; d.PreferredRange = 5f;
            d.Bio = "An old principal who plays electric guitar to turn cursed energy into crushing sound.";
            d.Kit = () => new Ability[]
            {
                new ProjectileAb { Name = "Power Riff", Desc = "A sound wave that pierces.", Cost = 14, Cooldown = 2.5f, Pose = FPose.Point, Color = H("#ff6a3a"),
                    Proj = new ProjDef { Vis = ProjVis.Sound, Color = H("#ff6a3a"), Damage = 48, Speed = 20f, Size = 0.9f, Type = DamageType.Sound, Pierce = 2, Launch = Sfx.Voice, Impact = Sfx.ImpactEnergy } },
                new BurstAb { Name = "Amp Blast", Desc = "Point-blank speaker blast.", Cost = 22, Cooldown = 6f, Radius = 2.6f, Damage = 85, Vis = BurstVis.Sound, Knockback = new Vector2(11f, 5f), Type = DamageType.Sound, Color = H("#ff6a3a"), Pose = FPose.Point, Sound = Sfx.Voice },
                new ZoneAb { Name = "Feedback Wall", Desc = "A wall of feedback that damages and blocks projectiles.", Cost = 30, Cooldown = 11f, AtTarget = false, Use = AIUse.Zone, Color = H("#ffcf6a"),
                    Zone = new ZoneDef { Width = 2f, Height = 4f, Life = 4f, TickDamage = 12f, TickRate = 0.3f, Vis = ZoneVis.Sound, Color = H("#ffcf6a"), BlocksProjectiles = true, Type = DamageType.Sound } },
                new BeamAb { Name = "Encore", Desc = "A wall of pure sound that tears across the stage.", Cost = 75, Cooldown = 24f, Pose = FPose.TwoPalm, Use = AIUse.Finisher, Color = H("#ff6a3a"),
                    Beam = new BeamDef { Length = 20f, Width = 1.8f, Duration = 1.0f, TickDamage = 15f, TickRate = 0.08f, Color = H("#ff6a3a"), Core = H("#ffe0a0"), Type = DamageType.Sound, Charge = 0.3f, Knockback = new Vector2(7f, 2f), Sound = Sfx.Voice } },
            };
        }

        /// <summary>Swap positions with another fighter (Clap Shift).</summary>
        static void SwapPlaces(Fighter a, Fighter b, float stun)
        {
            Vector2 pa = a.Pos, pb = b.Pos;
            VFX.Teleport(a.Center, a.Def.Look.Aura); VFX.Teleport(b.Center, a.Def.Look.Aura);
            a.Pos = a.PrevPos = pb; b.Pos = b.PrevPos = pa;
            a.Facing = b.Pos.x > a.Pos.x ? 1 : -1;
            b.Facing = a.Pos.x > b.Pos.x ? 1 : -1;
            b.Vel = Vector2.zero;
            if (stun > 0f && !b.IsBoss) b.AddStatus(StatusType.Stun, stun, 0f, a);
            VFX.WorldText(a.HeadPos + Vector2.up * 0.6f, "SWAP", a.Def.Look.Aura, 0.8f);
            CameraRig.Shake(0.2f);
        }
    }
}
