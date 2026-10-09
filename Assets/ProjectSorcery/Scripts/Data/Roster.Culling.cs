using UnityEngine;

namespace ProjectSorcery
{
    public static partial class Roster
    {
        // ===================================================================== CULLING GAME & SHINJUKU
        static void AddCullingGame()
        {
            // ---------------------------------------------------------- The gambler
            var d = C("gambler", "Daiki Bando", "The Gambler", "Jackpot Express", Era.Manga, Stance.Brawler, Hair.Afro, FaceMark.None, Weapon.None, "#ff4fb8", "#ffe14f");
            d.Hp = 1050; d.Ce = 110; d.CeRegen = 7.5f; d.Power = 1.1f; d.Style = AIStyle.Rushdown; d.Tier = 1; d.Domain = DomJackpot; d.PreferredRange = 1.5f;
            d.Bio = "Lives for the fever. His domain is a pachinko machine; hit the jackpot and he becomes functionally immortal for a while.";
            d.Kit = () => new Ability[]
            {
                new ProjectileAb { Name = "Shutter Doors", Desc = "Slams sliding train doors into the enemy.", Cost = 14, Cooldown = 3f, Pose = FPose.Palm, Color = H("#ffe14f"),
                    Proj = new ProjDef { Vis = ProjVis.Card, Color = H("#ffe14f"), Damage = 38, Speed = 20f, Size = 0.9f, Life = 0.6f, Type = DamageType.Blunt, Knockback = new Vector2(8f, 3f), Launch = Sfx.Whoosh, Impact = Sfx.PunchHeavy } },
                new StrikeAb { Name = "Rough Energy", Desc = "Fists coated in raw, abrasive cursed energy.", Cost = 18, Cooldown = 4.5f, Color = H("#ff4fb8"),
                    Strike = new StrikeDef { Duration = 0.3f, Velocity = new Vector2(12f, 0f), Hits = 3, Damage = 85, Pose = FPose.Jab, Knockback = new Vector2(8f, 4f), Color = H("#ff4fb8") } },
                new ProjectileAb { Name = "Ball Rush", Desc = "A spray of pachinko balls.", Cost = 25, Cooldown = 7f, Count = 7, Spread = 8f, Interval = 0.03f, Pose = FPose.Palm, Color = H("#e0e0e0"),
                    Proj = new ProjDef { Vis = ProjVis.Orb, Color = H("#e0e0e0"), Core = Color.white, Damage = 12, Speed = 24f, Size = 0.25f, Type = DamageType.Blunt, Knockback = new Vector2(2f, 1f), Launch = Sfx.Bell, Impact = Sfx.Punch } },
                DomainUlt(DomJackpot, "Spin the reels. Jackpot: infinite cursed energy and automatic healing.", "#ff4fb8"),
            };

            // ---------------------------------------------------------- Constellation
            d = C("stars", "Seira Hoshimi", "Constellation", "Star Rendezvous", Era.Manga, Stance.Caster, Hair.Bob, FaceMark.None, Weapon.None, "#ff9af0", "#ffe0f8");
            d.Hp = 1020; d.Ce = 115; d.CeRegen = 8.5f; d.Style = AIStyle.Zoner; d.PreferredRange = 6f;
            d.Bio = "Marks people with stars. Until you follow the constellation in order, you simply cannot reach her.";
            d.Kit = () => new Ability[]
            {
                new ProjectileAb { Name = "Star Mark", Desc = "Brands the target with a star.", Cost = 12, Cooldown = 2.5f, Pose = FPose.Point, Color = H("#ff9af0"),
                    Proj = new ProjDef { Vis = ProjVis.Star, Color = H("#ff9af0"), Damage = 40, Speed = 20f, Size = 0.6f, Type = DamageType.Light, Status = StatusType.Mark, StatusTime = 6f, StatusMag = 1f, Launch = Sfx.Bell } },
                new CustomAb { Name = "Constellation Lock", Desc = "Marked enemies are repelled whenever they approach you.", Cost = 30, Cooldown = 12f, CastTime = 0.2f, Pose = FPose.Raise, Use = AIUse.Zone, RangeMax = 30f, Color = H("#ff9af0"),
                    Fn = (f, p) => f.M.SpawnZone(f, new ZoneDef { Width = 5f, Height = 3.5f, Life = 6f, TickDamage = 8f, TickRate = 0.2f, Status = StatusType.Slow, StatusMag = 0.75f, StatusTime = 0.35f, FollowOwner = true, BlocksProjectiles = true, Vis = ZoneVis.Stars, Color = H("#ff9af0"), Type = DamageType.Light }, f.Pos, p) },
                new BurstAb { Name = "Star Repulsion", Desc = "Pushes everything away in a burst of starlight.", Cost = 25, Cooldown = 7f, Radius = 2.6f, Damage = 55, Knockback = new Vector2(14f, 5f), Vis = BurstVis.Stars, Color = H("#ff9af0"), Pose = FPose.Raise, Use = AIUse.Escape, Sound = Sfx.Bell },
                new ProjectileAb { Name = "Southern Cross", Desc = "A barrage of homing stars.", Cost = 65, Cooldown = 22f, Count = 8, Interval = 0.06f, Spread = 22f, Pose = FPose.Raise, Use = AIUse.Finisher, Color = H("#ff9af0"),
                    Proj = new ProjDef { Vis = ProjVis.Star, Color = H("#ff9af0"), Damage = 30, Speed = 14f, Size = 0.55f, Homing = 2.4f, Life = 3f, Type = DamageType.Light, Launch = Sfx.Bell } },
            };

            // ---------------------------------------------------------- The judge
            d = C("judge", "Hiro Sabaki", "The Judge", "Final Verdict", Era.Manga, Stance.Martial, Hair.Short, FaceMark.None, Weapon.Gavel, "#ffd36a", "#2a1a08");
            d.Hp = 1050; d.Ce = 120; d.CeRegen = 8f; d.Tier = 1; d.Domain = DomCourt; d.PreferredRange = 1.8f;
            d.Bio = "A lawyer who gave up on justice, until his technique put him on the bench. In his court, the verdict is absolute.";
            d.Kit = () => new Ability[]
            {
                new StrikeAb { Name = "Gavel Strike", Desc = "The gavel grows mid-swing.", Cost = 15, Cooldown = 4f, Color = H("#ffd36a"),
                    Strike = new StrikeDef { Duration = 0.2f, Velocity = new Vector2(12f, 0f), Offset = new Vector2(1.2f, 1.2f), Radius = 1.0f, Damage = 65, Flags = HitFlags.Heavy, Pose = FPose.Slam, Knockback = new Vector2(9f, 5f), Color = H("#ffd36a"), Sound = Sfx.Gavel } },
                new CounterAb { Name = "Objection", Desc = "Catches an attack and answers with the gavel.", Cost = 15, Cooldown = 7f, Window = 0.6f, Damage = 95, Type = DamageType.Blunt, Color = H("#ffd36a") },
                new SummonAb { Name = "Judgeman", Desc = "The shikigami judge hovers nearby, striking anyone who approaches.", Cost = 35, Cooldown = 14f, Count = 1, MaxActive = 1, Color = H("#2a2a2a"),
                    Minion = new MinionDef { Name = "Judgeman", Shape = MinionShape.Judge, Brain = MinionBrain.Guard, Color = H("#3a3a3a"), Hp = 300, Damage = 30, Range = 1.5f, AttackRate = 1f, Size = 1.4f, Life = 12f, Speed = 5f, Invulnerable = false } },
                DomainUlt(DomCourt, "Put the enemy on trial. Guilty: confiscation of their technique, or your blade becomes an executioner's sword.", "#ffd36a"),
            };

            // ---------------------------------------------------------- Thunder god
            d = C("thunder", "Gen Ikazuchi", "Thunder God", "Amber Lightning", Era.Manga, Stance.Agile, Hair.Wild, FaceMark.None, Weapon.Staff, "#7af0ff", "#ffffff");
            d.Hp = 1000; d.Ce = 130; d.CeRegen = 8f; d.Speed = 1.12f; d.Power = 1.05f; d.Tier = 1; d.PreferredRange = 2f; d.Style = AIStyle.Rushdown;
            d.Bio = "A four-hundred-year-old sorcerer whose cursed energy behaves like electricity. Saves his only technique for one final, fatal show.";
            d.Kit = () => new Ability[]
            {
                new ProjectileAb { Name = "Charge Bolt", Desc = "A lightning bolt that leaps to the target.", Cost = 14, Cooldown = 2.8f, Pose = FPose.Point, Color = H("#7af0ff"),
                    Proj = new ProjDef { Vis = ProjVis.Lightning, Color = H("#7af0ff"), Damage = 40, Speed = 34f, Size = 0.5f, Type = DamageType.Electric, Status = StatusType.Stun, StatusTime = 0.15f, Launch = Sfx.Electric, Impact = Sfx.Electric } },
                new StrikeAb { Name = "Static Strike", Desc = "Staff strike that charges the enemy; lightning follows.", Cost = 18, Cooldown = 5f, Color = H("#7af0ff"),
                    Strike = new StrikeDef { Duration = 0.2f, Velocity = new Vector2(16f, 0f), Damage = 55, Type = DamageType.Electric, Pose = FPose.Stab, Knockback = new Vector2(7f, 3f), DelayedDamage = 40, DelayedTime = 0.3f, Color = H("#7af0ff"), Sound = Sfx.Electric } },
                new StrikeAb { Name = "Lightning Rod Rush", Desc = "A blitz of electrified strikes.", Cost = 35, Cooldown = 13f, Color = H("#c0faff"), RangeMax = 4f,
                    Strike = new StrikeDef { Duration = 0.5f, Velocity = new Vector2(12f, 0f), Hits = 6, Damage = 115, Type = DamageType.Electric, Pose = FPose.Jab, Radius = 1.1f, Color = H("#c0faff"), Afterimages = true, Sound = Sfx.Electric } },
                new CustomAb { Name = "Amber Beast Ascension", Desc = "His body becomes pure lightning: massive power and speed. When it ends, the body pays.", Cost = 90, Cooldown = 40f, CastTime = 0.5f, Pose = FPose.Raise, Use = AIUse.Buff, Color = H("#7af0ff"), Armored = true,
                    Fn = (f, p) =>
                    {
                        f.AddStatus(StatusType.PowerUp, 10f, 0.4f, f); f.AddStatus(StatusType.Haste, 10f, 0.35f, f); f.AddStatus(StatusType.Armor, 6f, 1f, f);
                        f.M.SpawnBeam(f, new BeamDef { FromSky = true, Length = 20f, Width = 2.2f, Duration = 0.4f, TickDamage = 30f, TickRate = 0.08f, Color = H("#7af0ff"), Core = Color.white, Type = DamageType.Electric, Charge = 0.1f, Sound = Sfx.Electric }, new Vector2(f.Pos.x, f.M.Arena.Ceiling + 4f), Vector2.down, p);
                        VFX.ImpactFrame(ImpactKind.Domain, f.Center);
                        var self = f;
                        f.M.Schedule(10f, () => { if (!self.Dead) { self.TakeSelfDamage(self.MaxHp * 0.3f); VFX.WorldText(self.HeadPos + Vector2.up, "BODY COLLAPSING", H("#7af0ff"), 1f); } });
                    } },
            };

            // ---------------------------------------------------------- Heaven's stair
            d = C("seraph", "Seraphine", "Heaven's Stair", "Technique Extinguishment", Era.Manga, Stance.Floaty, Hair.Long, FaceMark.Halo | FaceMark.Wings, Weapon.None, "#fff4c0", "#ffffff", "#fffbf0");
            d.Hp = 1020; d.Ce = 130; d.CeRegen = 9f; d.Passives = Passive.Flight; d.Tier = 1; d.Style = AIStyle.Zoner; d.PreferredRange = 6f;
            d.Bio = "An angel sharing a girl's body. Her light extinguishes cursed techniques - even the barriers of a domain.";
            d.Kit = () => new Ability[]
            {
                new ProjectileAb { Name = "Purifying Light", Desc = "A ray of light that strips buffs off whoever it touches.", Cost = 15, Cooldown = 3f, Pose = FPose.Point, Color = H("#fff4c0"),
                    Proj = new ProjDef { Vis = ProjVis.Bolt, Color = H("#fff4c0"), Core = Color.white, Damage = 46, Speed = 28f, Size = 0.5f, Type = DamageType.Light, Flags = HitFlags.Technique | HitFlags.PierceInfinity, Launch = Sfx.Bell, Impact = Sfx.ImpactEnergy } },
                new CustomAb { Name = "Extinguish", Desc = "Erases the target's active buffs and seals their techniques.", Cost = 30, Cooldown = 12f, CastTime = 0.25f, Pose = FPose.Palm, Use = AIUse.Utility, RangeMax = 9f, Color = H("#fff4c0"),
                    Fn = (f, p) =>
                    {
                        var t = f.Target; if (t == null) return;
                        t.RemoveStatus(StatusType.PowerUp); t.RemoveStatus(StatusType.Haste); t.RemoveStatus(StatusType.Armor); t.RemoveStatus(StatusType.Jackpot);
                        t.RemoveStatus(StatusType.Overtime); t.RemoveStatus(StatusType.Shielded); t.RemoveStatus(StatusType.DefenseUp); t.RemoveStatus(StatusType.Zone);
                        t.AddStatus(StatusType.Sealed, 3f, 0f, f);
                        f.M.KillMinionsOf(t);
                        VFX.Burst(BurstVis.Light, t.Center, 2f, H("#fff4c0"));
                        VFX.WorldText(t.HeadPos + Vector2.up * 0.7f, "EXTINGUISHED", H("#fff4c0"), 1f);
                    } },
                new BuffAb { Name = "Wing Guard", Desc = "Folds her wings into a shield.", Cost = 25, Cooldown = 12f, Status = StatusType.Shielded, Mag = 0.5f, Duration = 5f, Color = H("#fff4c0") },
                new CustomAb { Name = "Celestial Stair", Desc = "A pillar of heavenly light that dissolves domains and burns curses.", Cost = 85, Cooldown = 30f, CastTime = 0.5f, Pose = FPose.Raise, Use = AIUse.Finisher, RangeMax = 30f, Color = H("#fff4c0"), Armored = true,
                    Fn = (f, p) =>
                    {
                        var t = f.Target;
                        float x = t != null ? t.Pos.x : f.Pos.x + f.Facing * 4f;
                        f.M.SpawnBeam(f, new BeamDef { FromSky = true, Length = 24f, Width = 3f, Duration = 1.2f, TickDamage = 22f, TickRate = 0.08f, Color = H("#fff4c0"), Core = Color.white, Type = DamageType.Light, Flags = HitFlags.Technique | HitFlags.PierceInfinity | HitFlags.Unblockable, Charge = 0.4f, Sound = Sfx.Beam }, new Vector2(x, f.M.Arena.Ceiling + 4f), Vector2.down, p);
                        f.M.Domains.ForceCollapseEnemies(f);
                    } },
            };

            // ---------------------------------------------------------- Comedian
            d = C("comedian", "Taka Warai", "The Comedian", "Punchline Reality", Era.Manga, Stance.Brawler, Hair.Messy, FaceMark.None, Weapon.None, "#ffe04a", "#ff6a3a");
            d.Hp = 1000; d.Ce = 120; d.CeRegen = 8f; d.Style = AIStyle.Trickster; d.PreferredRange = 2f;
            d.Bio = "A middle-aged comedian whose technique makes whatever he finds funny come true. As long as he believes the joke, he's untouchable.";
            d.Kit = () => new Ability[]
            {
                new CustomAb { Name = "Tsukkomi Slap", Desc = "A comedic straight-man slap with an unpredictable punchline.", Cost = 12, Cooldown = 3f, CastTime = 0.08f, Pose = FPose.Palm, Use = AIUse.Attack, RangeMax = 2.5f, Color = H("#ffe04a"),
                    Fn = (f, p) =>
                    {
                        int roll = f.M.Rng.Range(0, 3);
                        var s = new StrikeDef { Duration = 0.16f, Velocity = new Vector2(14f, 0f), Damage = 45, Pose = FPose.Palm, Color = H("#ffe04a"), Knockback = new Vector2(8f, 3f) };
                        if (roll == 1) { s.Knockback = new Vector2(2f, 16f); s.Flags = HitFlags.Launch | HitFlags.Heavy; }
                        if (roll == 2) { s.Damage = 75; s.Knockback = new Vector2(16f, 6f); s.Flags = HitFlags.Heavy; }
                        f.BeginStrike(s, p);
                        VFX.WorldText(f.HeadPos + Vector2.up * 0.7f, roll == 0 ? "\"OI!\"" : roll == 1 ? "\"WHY YOU-\"" : "\"WHAT ARE YOU DOING?!\"", H("#ffe04a"), 0.8f);
                    } },
                new CustomAb { Name = "Rimshot", Desc = "Something falls from the sky. Could be anything.", Cost = 18, Cooldown = 5f, CastTime = 0.2f, Pose = FPose.Raise, Use = AIUse.Projectile, RangeMax = 12f, Color = H("#ffe04a"),
                    Fn = (f, p) =>
                    {
                        var vis = f.M.Rng.Chance(0.5f) ? ProjVis.Rock : ProjVis.Card;
                        var proj = new ProjDef { Vis = vis, Color = H("#ffe04a"), Damage = 30 + f.M.Rng.Range(0, 50), Speed = 18f, Size = 0.5f + f.M.Rng.Range(0f, 1.2f), Type = DamageType.Blunt, Knockback = new Vector2(4f, -4f), ExplodeRadius = 1.2f, ExplodeDamage = 15f, Launch = Sfx.Whoosh, Impact = Sfx.Bell };
                        var t = f.Target;
                        float x = t != null ? t.Pos.x : f.Pos.x + f.Facing * 4f;
                        f.M.Shoot(f, proj, new Vector2(x, f.M.Arena.Ceiling + 1f), Vector2.down, p);
                    } },
                new BuffAb { Name = "Pratfall", Desc = "Takes the fall on purpose: briefly untouchable.", Cost = 22, Cooldown = 10f, Status = StatusType.Comedy, Mag = 1f, Duration = 1.6f, Color = H("#ffe04a"), Use = AIUse.Escape, CastTime = 0.05f },
                new CustomAb { Name = "Punchline Reality", Desc = "Total belief in the joke: immune to everything, and the gags hit for real.", Cost = 80, Cooldown = 32f, CastTime = 0.3f, Pose = FPose.Taunt, Use = AIUse.Buff, Color = H("#ffe04a"),
                    Fn = (f, p) =>
                    {
                        f.AddStatus(StatusType.Comedy, 7f, 1f, f);
                        f.AddStatus(StatusType.PowerUp, 7f, 0.4f, f);
                        VFX.WorldText(f.HeadPos + Vector2.up, "IT'S SHOWTIME!", H("#ffe04a"), 1.4f);
                        Audio.Play(Sfx.Jackpot, f.Center, 0.8f, 1.3f);
                        for (int i = 0; i < 4; i++)
                        {
                            float delay = 0.6f + i * 1.2f;
                            f.M.Schedule(delay, () =>
                            {
                                if (f.Dead) return;
                                var t = f.Target; if (t == null) return;
                                var h = HitInfo.Make(f, HitSource.Other, 45f * p, new Vector2(0f, -6f), 0.5f, HitFlags.Technique | HitFlags.Unblockable, DamageType.Blunt, t.Center, H("#ffe04a"));
                                f.M.Hit(t, h);
                                VFX.Burst(BurstVis.Stars, t.HeadPos, 1.2f, H("#ffe04a"));
                                VFX.WorldText(t.HeadPos + Vector2.up * 0.5f, "*BONK*", H("#ffe04a"), 1f);
                                Audio.Play(Sfx.Gavel, t.Center, 1f, 1.4f);
                            });
                        }
                    } },
            };

            // ---------------------------------------------------------- Granite cannon
            d = C("granite", "Ryu Iwagami", "Granite Cannon", "Granite Howitzer", Era.Manga, Stance.Brute, Hair.Afro, FaceMark.None, Weapon.None, "#a0e0ff", "#5a5a6a");
            d.Hp = 1150; d.Ce = 150; d.CeRegen = 9f; d.Size = 1.1f; d.Weight = 1.2f; d.Speed = 0.92f; d.Style = AIStyle.Zoner; d.PreferredRange = 6f;
            d.Bio = "Possesses the greatest cursed energy output of any reincarnated sorcerer. Points, and the world in front of him disappears.";
            d.Kit = () => new Ability[]
            {
                new ProjectileAb { Name = "Granite Shot", Desc = "A dense bolt of raw output.", Cost = 16, Cooldown = 2.8f, Pose = FPose.Point, Color = H("#a0e0ff"),
                    Proj = new ProjDef { Vis = ProjVis.Bolt, Color = H("#a0e0ff"), Core = Color.white, Damage = 48, Speed = 28f, Size = 0.6f, Type = DamageType.Energy, Knockback = new Vector2(7f, 3f), Launch = Sfx.Shoot, Impact = Sfx.ImpactEnergy } },
                new BurstAb { Name = "Point Blank", Desc = "Detonates cursed energy right in front of him.", Cost = 25, Cooldown = 6f, Radius = 2.2f, Offset = new Vector2(1.2f, 1.2f), Damage = 75, Knockback = new Vector2(13f, 5f), Vis = BurstVis.Explosion, Pose = FPose.Palm, Color = H("#a0e0ff") },
                new BuffAb { Name = "Output Overload", Desc = "Cranks his output even higher.", Cost = 30, Cooldown = 16f, Status = StatusType.PowerUp, Mag = 0.4f, Duration = 8f, CeRestore = 20f, Color = H("#a0e0ff") },
                new BeamAb { Name = "Granite Howitzer", Desc = "The highest-output beam in the game.", Cost = 85, Cooldown = 26f, Chantable = true, MaxChant = 1, Chant = new[] { "Full output!" }, Pose = FPose.Point, Use = AIUse.Finisher, Color = H("#a0e0ff"), Armored = true,
                    Beam = new BeamDef { Length = 24f, Width = 2.2f, Duration = 0.9f, TickDamage = 22f, TickRate = 0.07f, Color = H("#a0e0ff"), Core = Color.white, Charge = 0.45f, Knockback = new Vector2(8f, 2f) } },
            };

            // ---------------------------------------------------------- Sky weaver
            d = C("sky", "Ura Sorami", "Sky Weaver", "Sky Manipulation", Era.Manga, Stance.Elegant, Hair.Long, FaceMark.None, Weapon.None, "#9ad0ff", "#ffffff");
            d.Hp = 1000; d.Ce = 120; d.CeRegen = 8f; d.Style = AIStyle.Zoner; d.PreferredRange = 5f;
            d.Bio = "Treats the sky itself as a surface - folding it, flipping it, and bouncing attacks right back.";
            d.Kit = () => new Ability[]
            {
                new ProjectileAb { Name = "Sky Fold", Desc = "Folds space into a cutting wave.", Cost = 14, Cooldown = 2.8f, Pose = FPose.Palm, Color = H("#9ad0ff"),
                    Proj = new ProjDef { Vis = ProjVis.Disc, Color = H("#9ad0ff"), Damage = 38, Speed = 18f, Size = 0.9f, Type = DamageType.Energy, Pierce = 1, Knockback = new Vector2(5f, 3f), Launch = Sfx.Wind } },
                new CounterAb { Name = "Thin Ice Breaker", Desc = "Flips the surface: attacks bounce back.", Cost = 20, Cooldown = 8f, Window = 0.7f, Damage = 90, Type = DamageType.Energy, TechniqueOnly = false, Color = H("#9ad0ff") },
                new CustomAb { Name = "Sky Slide", Desc = "Slides along the sky to a new position behind the enemy.", Cost = 15, Cooldown = 6f, CastTime = 0.05f, Pose = FPose.Dash, Use = AIUse.Escape, RangeMax = 12f, Color = H("#9ad0ff"),
                    Fn = (f, p) => { var t = f.Target; if (t == null) return; f.Teleport(new Vector2(t.Pos.x - t.Facing * 3f, 2.5f)); f.Facing = t.Pos.x > f.Pos.x ? 1 : -1; } },
                new CustomAb { Name = "Heaven Flip", Desc = "Peels the sky down onto the enemy like a slab.", Cost = 70, Cooldown = 24f, CastTime = 0.4f, Pose = FPose.Slam, Use = AIUse.Finisher, RangeMax = 30f, Color = H("#9ad0ff"),
                    Fn = (f, p) =>
                    {
                        var t = f.Target;
                        float x = t != null ? t.Pos.x : f.Pos.x + f.Facing * 4f;
                        var h = HitInfo.Make(f, HitSource.Other, 170f * p, new Vector2(4f, -14f), 0.8f, HitFlags.Technique | HitFlags.Heavy | HitFlags.Spike, DamageType.Gravity, new Vector2(x, 2f), H("#9ad0ff"));
                        f.M.Area(f, new Vector2(x, 1.2f), 3.2f, h, true);
                        VFX.Burst(BurstVis.Gravity, new Vector2(x, 1.2f), 3.2f, H("#9ad0ff"));
                        VFX.ImpactFrame(ImpactKind.Heavy, new Vector2(x, 1.2f));
                        CameraRig.Shake(0.7f);
                        Audio.Play(Sfx.Explosion, new Vector2(x, 1f));
                    } },
            };

            // ---------------------------------------------------------- Receipt recreation
            d = C("receipt", "Roy Stern", "Receipt Recreation", "Contract Recreation", Era.Manga, Stance.Caster, Hair.Slick, FaceMark.Sunglasses, Weapon.Receipts, "#ffffff", "#ffd36a");
            d.Hp = 1000; d.Ce = 110; d.Style = AIStyle.Trickster; d.PreferredRange = 5f;
            d.Bio = "Anything he's ever bought, he can bring back - as long as he still has the receipt.";
            d.Kit = () => new Ability[]
            {
                new ProjectileAb { Name = "Receipt: Blades", Desc = "Recreates a pack of kitchen knives mid-air.", Cost = 14, Cooldown = 2.8f, Count = 3, Spread = 5f, Pose = FPose.Throw, Color = H("#e0e0e0"),
                    Proj = new ProjDef { Vis = ProjVis.Needle, Color = H("#e0e0e0"), Damage = 18, Speed = 26f, Size = 0.35f, Type = DamageType.Slash, Launch = Sfx.WhooshLight, Impact = Sfx.Slash } },
                new SummonAb { Name = "Receipt: Car", Desc = "Recreates a car and floors it into the enemy.", Cost = 30, Cooldown = 10f, Count = 1, MaxActive = 1, Color = H("#ff6a3a"),
                    Minion = new MinionDef { Name = "Car", Shape = MinionShape.Car, Brain = MinionBrain.Kamikaze, Color = H("#ff6a3a"), Hp = 150, Damage = 95, Speed = 16f, Size = 1.4f, Life = 4f, Range = 0.7f, Knockback = new Vector2(14f, 7f), Hitstun = 0.7f, Type = DamageType.Blunt } },
                new BurstAb { Name = "Receipt: Fireworks", Desc = "Recreates a box of fireworks, already lit.", Cost = 25, Cooldown = 7f, AtTarget = true, Radius = 1.8f, Damage = 60, Delay = 0.4f, Vis = BurstVis.Explosion, Color = H("#ffb04a"), Pose = FPose.Throw, RangeMax = 10f },
                new ProjectileAb { Name = "Receipt Barrage", Desc = "Every receipt at once.", Cost = 70, Cooldown = 24f, Count = 10, Interval = 0.05f, Spread = 4f, Pose = FPose.Throw, Use = AIUse.Finisher, Color = H("#ffffff"),
                    Proj = new ProjDef { Vis = ProjVis.Card, Color = H("#ffffff"), Damage = 20, Speed = 24f, Size = 0.5f, Type = DamageType.Blunt, ExplodeRadius = 0.9f, ExplodeDamage = 8f, Launch = Sfx.WhooshLight } },
            };

            // ---------------------------------------------------------- Future pen
            d = C("pen", "Carl Benoit", "Future Pen", "Foresight Panels", Era.Manga, Stance.Swordsman, Hair.Messy, FaceMark.None, Weapon.Axe, "#ff7ad0", "#ffffff");
            d.Hp = 1000; d.Ce = 110; d.PreferredRange = 1.8f;
            d.Bio = "A manga artist who sees the future of anyone whose blood touches his weapon, drawn out panel by panel.";
            d.Kit = () => new Ability[]
            {
                new StrikeAb { Name = "Panel Slash", Desc = "A slash that draws blood - and the next panel of the future.", Cost = 14, Cooldown = 3.5f, Color = H("#ff7ad0"),
                    Strike = new StrikeDef { Duration = 0.18f, Velocity = new Vector2(15f, 0f), Damage = 55, Type = DamageType.Slash, Pose = FPose.Slash, Status = StatusType.Mark, StatusTime = 6f, StatusMag = 1f, Color = H("#ff7ad0"), Sound = Sfx.Slash } },
                new CounterAb { Name = "Foresight", Desc = "Already saw it coming.", Cost = 15, Cooldown = 6f, Window = 0.8f, Damage = 80, Type = DamageType.Slash, Teleport = true, Color = H("#ff7ad0") },
                new ProjectileAb { Name = "Ink Spray", Desc = "A fan of ink that slows.", Cost = 18, Cooldown = 5f, Count = 4, Spread = 8f, Pose = FPose.Throw, Color = H("#20202a"),
                    Proj = new ProjDef { Vis = ProjVis.Blood, Color = H("#20202a"), Damage = 15, Speed = 18f, Size = 0.4f, Status = StatusType.Slow, StatusTime = 2f, StatusMag = 0.3f, Type = DamageType.Blunt } },
                new BuffAb { Name = "Final Page", Desc = "He's read the ending: dodges nearly everything for a while.", Cost = 70, Cooldown = 28f, Status = StatusType.Shielded, Mag = 0.75f, Status2 = StatusType.PowerUp, Mag2 = 0.3f, Duration = 6f, Color = H("#ff7ad0") },
            };

            // ---------------------------------------------------------- Trail shikigami
            d = C("trails", "Davin Lakshman", "Trail Tamer", "Territory Shikigami", Era.Manga, Stance.Caster, Hair.Short, FaceMark.None, Weapon.None, "#ffb04a", "#3a2a1a");
            d.Hp = 1000; d.Ce = 120; d.Style = AIStyle.Summoner; d.PreferredRange = 5f;
            d.Bio = "His two shikigami leave trails behind them; cross a trail and you're in their territory.";
            d.Kit = () => new Ability[]
            {
                new SummonAb { Name = "Trail Bird", Desc = "A flying shikigami that dives at intruders.", Cost = 22, Cooldown = 8.5f, Count = 1, MaxActive = 1, Color = H("#ffb04a"),
                    Minion = new MinionDef { Name = "Trail Bird", Shape = MinionShape.Bird, Brain = MinionBrain.Chase, Color = H("#ffb04a"), Fly = true, Hp = 90, Damage = 20, Speed = 10f, Life = 10f, Range = 0.8f } },
                new SummonAb { Name = "Trail Hound", Desc = "A ground shikigami that hunts.", Cost = 22, Cooldown = 8.5f, Count = 1, MaxActive = 1, Color = H("#ff8a3a"),
                    Minion = new MinionDef { Name = "Trail Hound", Shape = MinionShape.Serpent, Brain = MinionBrain.Chase, Color = H("#ff8a3a"), Hp = 120, Damage = 24, Speed = 8f, Life = 10f, Range = 1f } },
                new ZoneAb { Name = "Territory Trail", Desc = "Leaves a burning trail on the ground.", Cost = 25, Cooldown = 8f, Color = H("#ffb04a"),
                    Zone = new ZoneDef { Width = 6f, Height = 1.2f, Life = 4f, TickDamage = 9f, TickRate = 0.25f, Status = StatusType.Slow, StatusMag = 0.3f, StatusTime = 0.5f, Vis = ZoneVis.Fire, Color = H("#ffb04a"), Type = DamageType.Fire } },
                new CustomAb { Name = "Territory Lines", Desc = "Floods the arena with trails and calls both shikigami.", Cost = 70, Cooldown = 26f, CastTime = 0.3f, Pose = FPose.Raise, Use = AIUse.Zone, RangeMax = 30f, Color = H("#ffb04a"),
                    Fn = (f, p) =>
                    {
                        f.M.SpawnZone(f, new ZoneDef { Width = 22f, Height = 1.2f, Life = 5f, TickDamage = 10f, TickRate = 0.25f, Status = StatusType.Slow, StatusMag = 0.35f, StatusTime = 0.5f, Vis = ZoneVis.Fire, Color = H("#ffb04a"), Type = DamageType.Fire }, new Vector2(0f, 0f), p);
                        f.M.SpawnMinion(f, new MinionDef { Name = "Trail Bird", Shape = MinionShape.Bird, Brain = MinionBrain.Chase, Color = H("#ffb04a"), Fly = true, Hp = 90, Damage = 20, Speed = 10f, Life = 8f, Range = 0.8f }, f.Front(1f, 2.5f), p);
                    } },
            };

            // ---------------------------------------------------------- Inverted might
            d = C("inverted", "Taro Sakawa", "Inverted Might", "Weakness Reversal", Era.Manga, Stance.Wrestler, Hair.Short, FaceMark.Scar, Weapon.None, "#c0a0ff", "#2a1a3a");
            d.Hp = 1100; d.Power = 1.05f; d.Ce = 100; d.Passives = Passive.WeaknessInverted; d.Style = AIStyle.Grappler; d.PreferredRange = 1.4f;
            d.Bio = "Strong blows aimed at him weaken; weak ones grow. Fight him with big moves and you'll lose.";
            d.Kit = () => new Ability[]
            {
                new StrikeAb { Name = "Pressure Jab", Desc = "Quick body blows.", Cost = 12, Cooldown = 3f, Color = H("#c0a0ff"),
                    Strike = new StrikeDef { Duration = 0.3f, Velocity = new Vector2(12f, 0f), Hits = 3, Damage = 50, Pose = FPose.Jab, Color = H("#c0a0ff") } },
                new CounterAb { Name = "Inversion Guard", Desc = "Catches a strong blow and inverts it back.", Cost = 18, Cooldown = 7f, Window = 0.7f, Damage = 100, Type = DamageType.Blunt, Color = H("#c0a0ff") },
                new StrikeAb { Name = "Shoulder Charge", Desc = "Barrels through.", Cost = 22, Cooldown = 6f, Color = H("#c0a0ff"),
                    Strike = new StrikeDef { Duration = 0.3f, Velocity = new Vector2(17f, 0f), Damage = 75, Flags = HitFlags.Heavy, Pose = FPose.Dash, Knockback = new Vector2(12f, 5f), Color = H("#c0a0ff") } },
                new BuffAb { Name = "Total Inversion", Desc = "Heavy armor and defense: let them hit as hard as they want.", Cost = 60, Cooldown = 26f, Status = StatusType.Armor, Mag = 1f, Status2 = StatusType.DefenseUp, Mag2 = 0.3f, Duration = 6f, Color = H("#c0a0ff") },
            };

            // ---------------------------------------------------------- Liquid metal
            d = C("metal", "Yorune", "Liquid Metal", "Forge", Era.Manga, Stance.Elegant, Hair.Long, FaceMark.None, Weapon.None, "#d6dce8", "#ff9ad0", "#e8ecf4");
            d.Hp = 1000; d.Ce = 140; d.CeRegen = 8.5f; d.Tier = 1; d.Domain = DomSpheres; d.Style = AIStyle.Zoner; d.PreferredRange = 5f;
            d.Bio = "Constructs anything from liquid metal, including armor shaped like an insect. Obsessed with a love that can crush.";
            d.Kit = () => new Ability[]
            {
                new ProjectileAb { Name = "Forged Lance", Desc = "A metal lance formed in mid-air.", Cost = 15, Cooldown = 3f, Pose = FPose.Point, Color = H("#d6dce8"),
                    Proj = new ProjDef { Vis = ProjVis.Needle, Color = H("#d6dce8"), Damage = 45, Speed = 26f, Size = 0.6f, Pierce = 1, Type = DamageType.Pierce, Knockback = new Vector2(6f, 2f), Launch = Sfx.Slash, Impact = Sfx.Nail } },
                new BuffAb { Name = "Insect Armor", Desc = "Wraps herself in jointed metal armor.", Cost = 30, Cooldown = 15f, Status = StatusType.DefenseUp, Mag = 0.4f, Status2 = StatusType.Haste, Mag2 = 0.2f, Duration = 8f, Color = H("#d6dce8") },
                new ProjectileAb { Name = "Metal Rain", Desc = "Blades rain from overhead.", Cost = 35, Cooldown = 10f, Count = 5, Interval = 0.07f, FromSky = true, Pose = FPose.Raise, Color = H("#d6dce8"),
                    Proj = new ProjDef { Vis = ProjVis.Metal, Color = H("#d6dce8"), Damage = 28, Speed = 24f, Size = 0.5f, Type = DamageType.Slash, Knockback = new Vector2(2f, -4f), Launch = Sfx.Slash } },
                DomainUlt(DomSpheres, "A perfect sphere of mass crushes each enemy once - nothing stops it.", "#d6dce8"),
            };

            // ---------------------------------------------------------- Black rope dancer
            d = C("rope", "Marcos", "Black Rope Dancer", "Black Rope", Era.Manga, Stance.Agile, Hair.Afro, FaceMark.None, Weapon.Rope, "#4a4a5a", "#ffb04a", "#5a4030");
            d.Hp = 1100; d.Speed = 1.15f; d.Power = 1.1f; d.Ce = 110; d.Passives = Passive.SimpleDomainMaster; d.PreferredRange = 2.5f;
            d.Bio = "Wields a cursed rope that unravels techniques, and a dance that keeps up with the strongest.";
            d.Kit = () => new Ability[]
            {
                new ProjectileAb { Name = "Rope Lash", Desc = "Snares and yanks the enemy in.", Cost = 15, Cooldown = 4f, Pose = FPose.Throw, Color = H("#4a4a5a"),
                    Proj = new ProjDef { Vis = ProjVis.Needle, Color = H("#2a2a3a"), Damage = 30, Speed = 26f, Size = 0.45f, Life = 0.6f, PullOnHit = true, Type = DamageType.Blunt, Flags = HitFlags.Technique | HitFlags.PierceInfinity, Launch = Sfx.WhooshLight } },
                new StrikeAb { Name = "Rope Dance", Desc = "A whirling dance of rope strikes that unravels defenses.", Cost = 25, Cooldown = 7f, Color = H("#ffb04a"), RangeMax = 3.5f,
                    Strike = new StrikeDef { Duration = 0.55f, Velocity = new Vector2(8f, 0f), Hits = 6, Damage = 130, Flags = HitFlags.PierceInfinity, Pose = FPose.Spin, Radius = 1.3f, Color = H("#ffb04a") } },
                new CustomAb { Name = "Unravel", Desc = "The black rope drains the target's cursed energy and strips their barriers.", Cost = 30, Cooldown = 12f, CastTime = 0.2f, Pose = FPose.Throw, Use = AIUse.Utility, RangeMax = 8f, Color = H("#4a4a5a"),
                    Fn = (f, p) =>
                    {
                        var t = f.Target; if (t == null) return;
                        t.Ce = Mathf.Max(0f, t.Ce - 45f);
                        t.RemoveStatus(StatusType.Shielded); t.RemoveStatus(StatusType.DefenseUp);
                        f.M.KillMinionsOf(t);
                        VFX.Tongue(f.HandPosOrCenter(), t.Center, new Color(0.1f, 0.1f, 0.12f));
                        VFX.WorldText(t.HeadPos + Vector2.up * 0.6f, "UNRAVELED", H("#ffb04a"), 0.9f);
                    } },
                new CustomAb { Name = "Black Rope Storm", Desc = "A storm of ropes that tears down even a domain's barrier.", Cost = 80, Cooldown = 28f, CastTime = 0.3f, Pose = FPose.Spin, Use = AIUse.Finisher, RangeMax = 6f, Color = H("#ffb04a"),
                    Fn = (f, p) =>
                    {
                        f.M.Domains.ForceCollapseEnemies(f);
                        f.BeginStrike(new StrikeDef { Duration = 0.8f, Velocity = new Vector2(6f, 0f), Hits = 9, HitInterval = 0.08f, Damage = 190, Flags = HitFlags.PierceInfinity | HitFlags.Unblockable, Pose = FPose.Spin, Radius = 1.8f, Color = H("#ffb04a") }, p);
                    } },
            };

            // ---------------------------------------------------------- Heart catcher
            d = C("heart", "Laurent", "Heart Catcher", "Heart Catch", Era.Manga, Stance.Elegant, Hair.Slick, FaceMark.None, Weapon.None, "#ff6a9a", "#ffd0e0");
            d.Hp = 1000; d.Ce = 110; d.Style = AIStyle.Trickster; d.PreferredRange = 4f;
            d.Bio = "Catches the hostility aimed at him and turns it around. Hearts, quite literally, in his hands.";
            d.Kit = () => new Ability[]
            {
                new ProjectileAb { Name = "Heart Catch", Desc = "A heart that scrambles the target's sense of direction.", Cost = 18, Cooldown = 5f, Pose = FPose.Point, Color = H("#ff6a9a"),
                    Proj = new ProjDef { Vis = ProjVis.Star, Color = H("#ff6a9a"), Damage = 30, Speed = 18f, Size = 0.6f, Type = DamageType.Soul, Status = StatusType.Confused, StatusTime = 2.5f, Launch = Sfx.Bell } },
                new CounterAb { Name = "Redirect", Desc = "Catches the hostility and throws it back.", Cost = 15, Cooldown = 6f, Window = 0.7f, Damage = 85, Type = DamageType.Soul, Color = H("#ff6a9a") },
                new StrikeAb { Name = "Heartbreaker", Desc = "An elegant kick.", Cost = 15, Cooldown = 4f, Color = H("#ff6a9a"),
                    Strike = new StrikeDef { Duration = 0.2f, Velocity = new Vector2(15f, 0f), Damage = 60, Pose = FPose.Kick, Knockback = new Vector2(9f, 4f), Color = H("#ff6a9a") } },
                new CustomAb { Name = "Captured Hearts", Desc = "Every enemy's heart is caught: confused, slowed and hurt.", Cost = 65, Cooldown = 24f, CastTime = 0.3f, Pose = FPose.Raise, Use = AIUse.Finisher, RangeMax = 30f, Color = H("#ff6a9a"),
                    Fn = (f, p) =>
                    {
                        foreach (var e in f.M.Fighters)
                        {
                            if (!f.M.IsEnemy(f, e) || e.Dead) continue;
                            e.AddStatus(StatusType.Confused, 4f, 0f, f); e.AddStatus(StatusType.Slow, 4f, 0.3f, f);
                            var h = HitInfo.Make(f, HitSource.Other, 80f * p, Vector2.zero, 0.3f, HitFlags.Technique | HitFlags.Unblockable | HitFlags.Soul, DamageType.Soul, e.Center, H("#ff6a9a"));
                            f.M.Hit(e, h);
                            VFX.Burst(BurstVis.Stars, e.HeadPos, 1.2f, H("#ff6a9a"));
                        }
                    } },
            };
        }
    }
}
