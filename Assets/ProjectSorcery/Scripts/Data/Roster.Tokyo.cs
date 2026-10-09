using UnityEngine;

namespace ProjectSorcery
{
    public static partial class Roster
    {
        // ===================================================================== TOKYO STUDENTS
        static void AddTokyo()
        {
            // ---------------------------------------------------------- The Vessel
            var d = C("vessel", "Haru Kanzaki", "The Vessel", "Divergent Fist", Era.Anime, Stance.Brawler, Hair.Spiky, FaceMark.None, Weapon.None, "#ff5a7a", "#ff9db0");
            d.Hp = 1050; d.Speed = 1.12f; d.Power = 1.1f; d.Ce = 90; d.CeRegen = 6.5f; d.Style = AIStyle.Rushdown; d.PreferredRange = 1.3f;
            d.Passives = Passive.BlackFlashAffinity | Passive.SoulResist; d.BlackFlashBonus = 0.03f;
            d.Bio = "A freakishly strong boy who swallowed a calamity. His cursed energy lags behind his fists, landing every punch twice.";
            d.Kit = () => new Ability[]
            {
                new StrikeAb { Name = "Divergent Fist", Desc = "Dash punch; cursed energy lands a beat later for a second impact.", Cost = 15, Cooldown = 3.5f, Color = H("#ff5a7a"),
                    Strike = new StrikeDef { Duration = 0.2f, Velocity = new Vector2(18f, 0f), Damage = 55, Knockback = new Vector2(7f, 3f), Hitstun = 0.5f, DelayedDamage = 35, DelayedTime = 0.22f, Color = H("#ff5a7a"), Pose = FPose.HeavyPunch, StopOnHit = true } },
                new StrikeAb { Name = "Manji Kick", Desc = "Rising spin kick that launches the enemy skyward.", Cost = 20, Cooldown = 6f, Color = H("#ff9db0"), RangeMax = 3f,
                    Strike = new StrikeDef { Duration = 0.25f, Velocity = new Vector2(9f, 9f), Damage = 70, Knockback = new Vector2(3f, 15f), Hitstun = 0.85f, Flags = HitFlags.Heavy | HitFlags.Launch, Pose = FPose.Kick, Color = H("#ff9db0") } },
                new StrikeAb { Name = "Divergent Barrage", Desc = "Six-hit flurry capped by a delayed cursed burst.", Cost = 40, Cooldown = 12f, Color = H("#ff5a7a"), RangeMax = 3.5f,
                    Strike = new StrikeDef { Duration = 0.55f, Velocity = new Vector2(9f, 0f), Hits = 6, HitInterval = 0.07f, Damage = 130, Knockback = new Vector2(9f, 5f), DelayedDamage = 60, Pose = FPose.Jab, Radius = 0.95f, Color = H("#ff5a7a") } },
                new CustomAb { Name = "In The Zone", Desc = "Your next three heavy hits are guaranteed Black Flashes.", Cost = 70, Cooldown = 28f, CastTime = 0.35f, Pose = FPose.Channel, Use = AIUse.Buff, Color = H("#ff1a33"),
                    Fn = (f, p) => { f.ForcedBlackFlashes = 3; f.AddStatus(StatusType.Zone, 12f, 1f, f); VFX.Aura(f.Center, new Color(1f, 0.1f, 0.15f), 1.6f); VFX.WorldText(f.HeadPos + Vector2.up, "IN THE ZONE", new Color(1f, 0.15f, 0.2f), 1.3f); Audio.Play(Sfx.Charge, f.Center, 1f, 0.6f); } },
            };

            // ---------------------------------------------------------- Shinjuku vessel
            d = C("vessel_shinjuku", "Haru Kanzaki", "Soul Executioner", "Blood & Soul Striking", Era.Manga, Stance.Brawler, Hair.Spiky, FaceMark.None, Weapon.None, "#ff2848", "#ffd0a0");
            d.Hp = 1050; d.Speed = 1.15f; d.Power = 1.15f; d.Ce = 115; d.CeRegen = 7f; d.Style = AIStyle.Rushdown; d.Tier = 1; d.PreferredRange = 1.5f;
            d.Passives = Passive.BlackFlashAffinity | Passive.SoulResist; d.BlackFlashBonus = 0.05f; d.Domain = DomHometown;
            d.Bio = "Grown into a full sorcerer: piercing blood from his brothers, the calamity's own slashes, and fists that strike the soul itself.";
            d.Kit = () => new Ability[]
            {
                new BeamAb { Name = "Crimson Lance", Desc = "A pressurized jet of blood that pierces in a line.", Cost = 20, Cooldown = 4f, Color = H("#ff2040"), Pose = FPose.Palm,
                    Beam = new BeamDef { Length = 12f, Width = 0.3f, Duration = 0.25f, TickDamage = 13f, TickRate = 0.05f, Color = H("#ff2040"), Core = H("#ffd0d0"), Type = DamageType.Blood, Charge = 0.12f, Knockback = new Vector2(3f, 0.5f), Sound = Sfx.Blood } },
                new ProjectileAb { Name = "Sunder", Desc = "A borrowed invisible slash that cuts through the first target.", Cost = 25, Cooldown = 6f, Color = H("#ff3040"), Pose = FPose.Slash, Proj = WithPierce(Slash("#ff3040", 70f, 26f, 0.9f), 1) },
                new StrikeAb { Name = "Soul Rend", Desc = "Three blows aimed at the boundary of the soul. Ignores boundless defenses.", Cost = 40, Cooldown = 11f, Color = H("#ffe0a0"),
                    Strike = new StrikeDef { Duration = 0.3f, Velocity = new Vector2(20f, 0f), Hits = 3, Damage = 120, Flags = HitFlags.Heavy | HitFlags.Soul | HitFlags.PierceInfinity, Type = DamageType.Soul, Color = H("#ffe0a0"), Pose = FPose.Cross, Knockback = new Vector2(10f, 4f) } },
                DomainUlt(DomHometown, "Soul-striking empowerment: your blows pierce every defense.", "#ffb070"),
            };

            // ---------------------------------------------------------- Shadow Tamer
            d = C("shadow", "Kage Tsubaki", "Shadow Tamer", "Ten Umbra Rites", Era.Anime, Stance.Caster, Hair.Messy, FaceMark.None, Weapon.None, "#7a5cff", "#2a1f55");
            d.Hp = 980; d.Ce = 115; d.CeRegen = 7.5f; d.Style = AIStyle.Summoner; d.PreferredRange = 4f; d.Domain = DomShadow;
            d.Bio = "Weaves hand signs to call shikigami out of his shadow. Still incomplete, but his potential frightens a king.";
            d.Kit = () => new Ability[]
            {
                new SummonAb { Name = "Twin Wolves", Desc = "Two shadow wolves hunt the nearest enemy.", Cost = 22, Cooldown = 7f, Count = 2, MaxActive = 2, Color = H("#c8c0ff"),
                    Minion = WolfDef },
                new SummonAb { Name = "Thunder Owl", Desc = "A winged shikigami dives in and electrocutes on impact.", Cost = 25, Cooldown = 8f, Count = 1, MaxActive = 1, Color = H("#ffe066"),
                    Minion = OwlDef },
                new SummonAb { Name = "Deluge Elephant", Desc = "Drops a colossal elephant that floods the ground.", Cost = 45, Cooldown = 14f, AtTarget = true, MaxActive = 1, Color = H("#7fd0ff"),
                    Minion = ElephantDef },
                DomainUlt(DomShadow, "Shadows swallow the floor; enemies sink and are struck from below.", "#8a6bff"),
            };

            // ---------------------------------------------------------- Nailstrike Witch
            d = C("nail", "Rin Hoshizaki", "Nailstrike Witch", "Straw Effigy", Era.Anime, Stance.Caster, Hair.Bob, FaceMark.None, Weapon.Hammer, "#ff8a3d", "#b8643a");
            d.Hp = 950; d.Ce = 100; d.CeRegen = 6.5f; d.Style = AIStyle.Zoner; d.PreferredRange = 6f;
            d.Bio = "Hammers cursed nails through anything. Whatever her nails touch, her straw doll can hurt from anywhere.";
            d.Kit = () => new Ability[]
            {
                new ProjectileAb { Name = "Nail Volley", Desc = "Three cursed nails. Each hit marks the target for Resonance.", Cost = 12, Cooldown = 2.5f, Count = 3, Interval = 0.07f, Spread = 4f, Pose = FPose.Throw, Color = H("#ffb070"), Proj = NailDef(22f) },
                new ProjectileAb { Name = "Hairpin", Desc = "Nails embed, then detonate a moment later.", Cost = 22, Cooldown = 6f, Count = 2, Interval = 0.1f, Pose = FPose.Throw, Color = H("#ff8a3d"), Proj = HairpinDef() },
                new CustomAb { Name = "Resonance", Desc = "Strike the straw doll: every marked enemy takes soul damage wherever they are.", Cost = 35, Cooldown = 10f, CastTime = 0.3f, Pose = FPose.Slam, Use = AIUse.Finisher, RangeMax = 30f, Color = H("#ff8a3d"),
                    Can = f => AnyMarked(f), Fn = (f, p) => Resonate(f, 95f * p, 0.5f, false) },
                new CustomAb { Name = "Effigy Resonance", Desc = "Mark every enemy, then hammer the doll: heavy unblockable soul damage.", Cost = 70, Cooldown = 26f, CastTime = 0.4f, Pose = FPose.Slam, Use = AIUse.Finisher, RangeMax = 30f, Color = H("#ff6a1a"),
                    Fn = (f, p) =>
                    {
                        foreach (var e in f.M.Fighters) if (f.M.IsEnemy(f, e) && !e.Dead) { e.AddStatus(StatusType.Mark, 3f, 1f, f); VFX.Ring(e.Center, H("#ff8a3d"), 1.3f, 0.5f, 0.08f); }
                        f.M.Schedule(0.55f, () => Resonate(f, 115f * p, 0.8f, true));
                    } },
            };

            // ---------------------------------------------------------- Unbound Blade (early)
            d = C("blade", "Kaede Mibu", "Unbound Blade", "Heavenly Restriction", Era.Anime, Stance.Swordsman, Hair.Ponytail, FaceMark.Glasses, Weapon.Polearm, "#9cff6d", "#2a3a2a");
            Restricted(d, 1100f, 1.2f, 1.15f);
            d.Style = AIStyle.Rushdown; d.PreferredRange = 1.8f;
            d.Bio = "Born with almost no cursed energy, she sees curses through her glasses and fights with an arsenal of cursed tools.";
            d.Kit = () => new Ability[]
            {
                new BurstAb { Name = "Sweeping Arc", Desc = "Wide polearm sweep in front.", Physical = true, Cost = 0, Cooldown = 3.5f, Radius = 1.8f, Offset = new Vector2(1.1f, 1.0f), Damage = 60, Type = DamageType.Slash, Vis = BurstVis.Slashes, Knockback = new Vector2(6f, 4f), Pose = FPose.Slash, Flags = HitFlags.Heavy, Color = H("#c8ffb0"), Sound = Sfx.Slash, RangeMax = 2.6f },
                new CounterAb { Name = "Dragon-Bone", Desc = "The blade drinks the impact and releases it back.", Physical = true, Cost = 0, Cooldown = 8f, Window = 0.6f, Damage = 110, Type = DamageType.Blunt, Color = H("#e0ffd0") },
                new ProjectileAb { Name = "Tool Barrage", Desc = "Hurls four cursed tools.", Physical = true, Cost = 0, Cooldown = 9f, Count = 4, Interval = 0.08f, Spread = 6f, Pose = FPose.Throw, Color = H("#c0c8d0"),
                    Proj = new ProjDef { Vis = ProjVis.Metal, Color = H("#c0c8d0"), Core = Color.white, Damage = 30, Speed = 22, Size = 0.4f, Type = DamageType.Pierce, Flags = HitFlags.None, Launch = Sfx.WhooshLight, Impact = Sfx.Nail } },
                new BuffAb { Name = "Restriction Unleashed", Desc = "Pushes her body past its limit: faster and stronger.", Physical = true, Cost = 0, Cooldown = 30f, Status = StatusType.Haste, Mag = 0.35f, Status2 = StatusType.PowerUp, Mag2 = 0.35f, Duration = 10f, Color = H("#9cff6d") },
            };

            // ---------------------------------------------------------- Unbound Blade (awakened)
            d = C("blade_awakened", "Kaede Mibu", "Awakened Restriction", "Total Heavenly Restriction", Era.Manga, Stance.Swordsman, Hair.Ponytail, FaceMark.Scar, Weapon.Katana, "#e8ffe0", "#1a2a1a");
            Restricted(d, 1200f, 1.4f, 1.35f);
            d.Style = AIStyle.Rushdown; d.Tier = 1; d.PreferredRange = 1.6f;
            d.Bio = "Her restriction made complete. No cursed energy at all: domains cannot see her, and nothing can keep up.";
            d.Kit = () => new Ability[]
            {
                new StrikeAb { Name = "Soul Splitter", Desc = "A blade that cuts the soul, ignoring the body's hardness.", Physical = true, Cost = 0, Cooldown = 4f, Color = H("#e8ffe0"),
                    Strike = new StrikeDef { Duration = 0.18f, Velocity = new Vector2(24f, 0f), Damage = 70, Type = DamageType.Soul, Flags = HitFlags.Heavy | HitFlags.Soul | HitFlags.PierceInfinity, Pose = FPose.Slash, Knockback = new Vector2(8f, 3f), Color = H("#e8ffe0"), Sound = Sfx.Slash } },
                new StrikeAb { Name = "Afterimage Step", Desc = "Vanish and cut from behind.", Physical = true, Cost = 0, Cooldown = 6f, Color = H("#e8ffe0"), RangeMax = 9f,
                    Strike = new StrikeDef { TeleportBehind = true, Duration = 0.15f, Velocity = new Vector2(6f, 0f), Damage = 55, Pose = FPose.Slash, Type = DamageType.Slash, Color = H("#e8ffe0"), Sound = Sfx.Slash } },
                new StrikeAb { Name = "Massacre", Desc = "Eight cuts in the blink of an eye.", Physical = true, Cost = 0, Cooldown = 12f, Color = H("#ffffff"), RangeMax = 3f,
                    Strike = new StrikeDef { Duration = 0.6f, Velocity = new Vector2(10f, 0f), Hits = 8, HitInterval = 0.06f, Damage = 160, Type = DamageType.Slash, Pose = FPose.Slash, Radius = 1.0f, Color = H("#ffffff"), Sound = Sfx.Slash } },
                new CustomAb { Name = "Heavenly Rampage", Desc = "Unleash everything: a blinding rush followed by a surge of speed and power.", Physical = true, Cost = 0, Cooldown = 30f, CastTime = 0.15f, Pose = FPose.Slash, Use = AIUse.Finisher, RangeMax = 8f, Color = H("#ffffff"),
                    Fn = (f, p) =>
                    {
                        f.AddStatus(StatusType.Haste, 8f, 0.5f, f); f.AddStatus(StatusType.PowerUp, 8f, 0.3f, f);
                        f.BeginStrike(new StrikeDef { TeleportBehind = true, Duration = 0.5f, Velocity = new Vector2(8f, 0f), Hits = 5, HitInterval = 0.07f, Damage = 170, Type = DamageType.Slash, Pose = FPose.Slash, Radius = 1.1f, Color = Color.white }, p);
                        VFX.SpeedBurst(f.Center, Color.white, 30, 1.5f, 8f);
                    } },
            };

            // ---------------------------------------------------------- Cursed Corpse
            d = C("corpse", "Bao", "Cursed Corpse", "Core Swap", Era.Anime, Stance.Brute, Hair.Ears, FaceMark.None, Weapon.None, "#f0f0f0", "#1a1a1a", "#f4f4f4");
            d.Hp = 1150; d.Size = 1.15f; d.Weight = 1.3f; d.Power = 1.1f; d.Speed = 0.95f; d.Style = AIStyle.Grappler; d.PreferredRange = 1.3f;
            d.Passives = Passive.ToughBody;
            d.Bio = "An abrupt mutated cursed corpse with three cores. Swapping to the brutal core turns every punch into a drumbeat.";
            d.Kit = () => new Ability[]
            {
                new StrikeAb { Name = "Drumming Beat", Desc = "A shockwave punch that passes straight through a guard.", Cost = 18, Cooldown = 4.5f, Color = H("#ffffff"),
                    Strike = new StrikeDef { Duration = 0.18f, Velocity = new Vector2(14f, 0f), Damage = 70, Flags = HitFlags.Heavy | HitFlags.Unblockable, Pose = FPose.HeavyPunch, Knockback = new Vector2(9f, 4f), Hitstun = 0.55f, StopOnHit = true, Color = Color.white } },
                new BuffAb { Name = "Brute Core", Desc = "Swap to the brute core: super armor and heavier hits.", Cost = 25, Cooldown = 14f, Status = StatusType.Armor, Mag = 1f, Status2 = StatusType.PowerUp, Mag2 = 0.25f, Duration = 6f, Color = H("#ff6a6a") },
                new StrikeAb { Name = "Triple Core Crush", Desc = "Five pounding blows, then the cores resonate.", Cost = 40, Cooldown = 12f, Color = H("#dddddd"), RangeMax = 3f,
                    Strike = new StrikeDef { Duration = 0.5f, Velocity = new Vector2(8f, 0f), Hits = 5, Damage = 140, Pose = FPose.Hook, DelayedDamage = 40, Color = Color.white, Radius = 1.0f } },
                new BurstAb { Name = "Final Drum", Desc = "Slams the ground so hard everything nearby is stunned.", Cost = 70, Cooldown = 25f, Radius = 3.2f, Damage = 160, Knockback = new Vector2(12f, 7f), Status = StatusType.Stun, StatusTime = 1f, Vis = BurstVis.Shockwave, Pose = FPose.Slam, Delay = 0.25f, Color = Color.white, Shake = 0.7f, Use = AIUse.Finisher },
            };

            // ---------------------------------------------------------- Word Binder
            d = C("speech", "Sora Inaba", "Word Binder", "Spellvoice", Era.Anime, Stance.Caster, Hair.Short, FaceMark.Collar, Weapon.None, "#c6a0ff", "#e8e0ff");
            d.Hp = 950; d.Ce = 110; d.CeRegen = 7f; d.Style = AIStyle.Zoner; d.PreferredRange = 5f;
            d.Bio = "His every word is a curse, so he only speaks in rice-ball fillings. When he does command, the world obeys - and his throat pays.";
            d.Kit = () => new Ability[]
            {
                new CommandAb { Name = "\"Don't Move\"", Word = "DON'T MOVE", Desc = "Freezes enemies in front for a moment. Costs a little HP.", Cost = 18, Cooldown = 7f, Status = StatusType.Stun, StatusTime = 1.0f, Damage = 30, SelfDamage = 12, Range = 9f, Color = H("#c6a0ff") },
                new CommandAb { Name = "\"Blast Away\"", Word = "BLAST AWAY", Desc = "Hurls enemies away with a single word.", Cost = 22, Cooldown = 6f, Status = StatusType.Count, Damage = 75, Knockback = new Vector2(13f, 6f), SelfDamage = 15, Range = 8f, Color = H("#e0c8ff"), Use = AIUse.Attack },
                new CommandAb { Name = "\"Get Crushed\"", Word = "GET CRUSHED", Desc = "Crushes enemies into the ground.", Cost = 40, Cooldown = 12f, Status = StatusType.Gravity, StatusTime = 1.5f, StatusMag = 22f, Damage = 135, Knockback = new Vector2(0f, -6f), SelfDamage = 28, Range = 8f, Color = H("#a070ff"), Use = AIUse.Attack },
                new CommandAb { Name = "\"Explode\"", Word = "EXPLODE", Desc = "A command that reaches every enemy. Shreds your own throat.", Cost = 80, Cooldown = 30f, Global = true, Damage = 230, Knockback = new Vector2(10f, 9f), Status = StatusType.Stun, StatusTime = 0.6f, SelfDamage = 70, Color = H("#ff70c0"), Use = AIUse.Finisher, RangeMax = 30f },
            };

            // ---------------------------------------------------------- Bound by Love
            d = C("love", "Ren Okada", "Bound by Love", "Mimicry", Era.Anime, Stance.Swordsman, Hair.Messy, FaceMark.None, Weapon.Katana, "#ff8fb0", "#1c1c2a");
            d.Hp = 1000; d.Ce = 170; d.CeRegen = 9f; d.Passives = Passive.RCT; d.RctRate = 40f; d.Tier = 1; d.PreferredRange = 2.5f; d.Domain = DomSwords;
            d.Bio = "Haunted by the queen of curses who loves him. A bottomless reservoir of cursed energy and the ability to copy techniques.";
            d.Kit = () => new Ability[]
            {
                RikaSummon(),
                MimicryAb(),
                new BeamAb { Name = "Pure Love", Desc = "A torrent of raw cursed energy. Hold to declare your love for more power.", Cost = 55, Cooldown = 15f, Chantable = true, MaxChant = 2,
                    Chant = new[] { "I love you...", "...so lend me your strength." }, Color = H("#ff9fc8"),
                    Beam = new BeamDef { Length = 18f, Width = 1.3f, Duration = 1.0f, TickDamage = 16f, TickRate = 0.07f, Color = H("#ff9fc8"), Core = Color.white, Charge = 0.35f, Knockback = new Vector2(5f, 1f) } },
                DomainUlt(DomSwords, "A field of copied blades: you hit harder, faster, and your techniques recharge rapidly.", "#ff8fb0"),
            };

            // ---------------------------------------------------------- Borrowed Vessel
            d = C("love_borrowed", "Ren Okada", "Borrowed Vessel", "Boundless (Borrowed)", Era.Manga, Stance.Elegant, Hair.Spiky, FaceMark.Stitches, Weapon.Katana, "#9fd0ff", "#ff9fc8");
            d.Hp = 900; d.Ce = 130; d.CeRegen = 8f; d.Passives = Passive.SixEyes | Passive.RCT; d.RctRate = 25f; d.Tier = 1; d.PreferredRange = 4f; d.Domain = DomSwords;
            d.Bio = "Wearing the strongest's body for one last fight, carrying both boundless space and his queen.";
            d.Kit = () => new Ability[] { BlueAb(), RikaSummon(), PurpleAb(), DomainUlt(DomSwords, "A field of copied blades: you hit harder, faster, and your techniques recharge rapidly.", "#ff8fb0") };
        }

        // ---------------------------------------------------------------- shared pieces
        static ProjDef WithPierce(ProjDef p, int n) { p.Pierce = n; return p; }

        static readonly MinionDef WolfDef = new MinionDef { Name = "Shadow Wolf", Shape = MinionShape.Dog, Brain = MinionBrain.Chase, Color = Art.Hex("#d8d0ff"), Hp = 120, Damage = 22, Speed = 9f, Life = 12f, Range = 1f, AttackRate = 0.7f, Type = DamageType.Slash };
        static readonly MinionDef OwlDef = new MinionDef { Name = "Thunder Owl", Shape = MinionShape.Bird, Brain = MinionBrain.Kamikaze, Color = Art.Hex("#ffe066"), Fly = true, Damage = 65, Type = DamageType.Electric, Status = StatusType.Stun, StatusTime = 0.4f, Range = 0.6f, Speed = 12f, Size = 1.4f, Hp = 60, Life = 4f };
        static readonly MinionDef ElephantDef = new MinionDef { Name = "Deluge Elephant", Shape = MinionShape.Elephant, Brain = MinionBrain.Flood, Color = Art.Hex("#7fd0ff"), Damage = 90, Size = 2f, Hp = 300, Life = 2f, Type = DamageType.Water };

        static ProjDef NailDef(float dmg) => new ProjDef
        {
            Vis = ProjVis.Nail, Color = Art.Hex("#ffb070"), Core = Color.white, Damage = dmg, Speed = 24f, Size = 0.3f, Type = DamageType.Pierce,
            Status = StatusType.Mark, StatusTime = 8f, StatusMag = 1f, Launch = Sfx.Nail, Impact = Sfx.Nail, Knockback = new Vector2(2f, 1f), Hitstun = 0.25f
        };

        static ProjDef HairpinDef()
        {
            var p = NailDef(20f);
            p.StickAndDetonate = true; p.DetonateDelay = 0.7f; p.ExplodeRadius = 1.6f; p.ExplodeDamage = 45f; p.ExplodeKb = new Vector2(6f, 5f);
            p.Color = Art.Hex("#ff8a3d");
            return p;
        }

        static bool AnyMarked(Fighter f)
        {
            foreach (var e in f.M.Fighters) if (!e.Dead && f.M.IsEnemy(f, e) && e.Has(StatusType.Mark)) return true;
            return false;
        }

        static void Resonate(Fighter f, float dmg, float stun, bool percent)
        {
            bool any = false;
            foreach (var e in f.M.Fighters)
            {
                if (e.Dead || !f.M.IsEnemy(f, e) || !e.Has(StatusType.Mark)) continue;
                any = true;
                float total = dmg + (percent ? e.MaxHp * 0.08f : 0f);
                var h = HitInfo.Make(f, HitSource.Other, total, new Vector2(0f, 4f), stun, HitFlags.Unblockable | HitFlags.Soul | HitFlags.Technique | HitFlags.Heavy | HitFlags.NoCombo, DamageType.Soul, e.Center, Art.Hex("#ff8a3d"));
                h.Hitstop = 0.12f;
                f.M.Hit(e, h);
                e.RemoveStatus(StatusType.Mark);
                VFX.Burst(BurstVis.Lightning, e.Center, 1.8f, Art.Hex("#ff8a3d"));
                VFX.SpeedBurst(e.Center, Color.black, 14, 1f, 4f, true);
                VFX.WorldText(e.HeadPos + Vector2.up * 0.6f, "RESONANCE", Art.Hex("#ff8a3d"), 1f);
            }
            if (any) { Audio.Play(Sfx.Nail, f.Center, 1f, 0.6f); CameraRig.Punch(0.3f); VFX.ImpactFrame(ImpactKind.Heavy, f.Center); }
        }

        static SummonAb RikaSummon() => new SummonAb
        {
            Name = "Lady Marrow", Desc = "Manifests the queen of curses to guard you and maul anything nearby.", Cost = 35, Cooldown = 16f, MaxActive = 1, Count = 1,
            Color = Art.Hex("#ffc0d0"), Offset = new Vector2(-1.2f, 0f),
            Minion = RikaDef
        };

        static readonly MinionDef RikaDef = new MinionDef
        {
            Name = "Lady Marrow", Shape = MinionShape.Giant, Brain = MinionBrain.Guard, Color = Art.Hex("#ffc0d0"), Hp = 500, Damage = 55, Range = 1.6f,
            AttackRate = 0.9f, Size = 2.2f, Life = 14f, Speed = 7f, Knockback = new Vector2(8f, 5f), Hitstun = 0.5f, Flags = HitFlags.Technique | HitFlags.Minion | HitFlags.Heavy
        };

        static CustomAb MimicryAb() => new CustomAb
        {
            Name = "Mimicry", Desc = "Copies a random technique from another sorcerer and uses it.", Cost = 25, Cooldown = 7f, CastTime = 0.25f, Pose = FPose.Point,
            Use = AIUse.Projectile, RangeMin = 1f, RangeMax = 10f, Color = Art.Hex("#ff8fb0"),
            Fn = (f, p) =>
            {
                var pool = All;
                Ability ab = null;
                for (int tries = 0; tries < 30 && ab == null; tries++)
                {
                    var src = pool[f.M.Rng.Range(0, pool.Count)];
                    if (src.BossOnly || src == f.Def || src.Kit == null) continue;
                    var cand = src.Kit()[0];
                    if (cand == null || cand is CustomAb || cand is CounterAb || cand is DomainAb || cand is BuffAb) continue;
                    ab = cand;
                }
                if (ab == null) return;
                VFX.WorldText(f.HeadPos + Vector2.up * 0.9f, "COPY: " + ab.Name.ToUpperInvariant(), Art.Hex("#ff8fb0"), 0.9f);
                ab.Execute(f, p);
            }
        };
    }
}
