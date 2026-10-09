using UnityEngine;

namespace ProjectSorcery
{
    public static partial class Roster
    {
        // ===================================================================== CURSE USERS & DEATH PAINTINGS
        static void AddCurseUsers()
        {
            // ---------------------------------------------------------- Curse shepherd
            var d = C("shepherd", "Seiji Kagura", "Curse Shepherd", "Curse Herding", Era.Anime, Stance.Elegant, Hair.Bun, FaceMark.None, Weapon.Cloud, "#b080ff", "#2a1a3a");
            d.Hp = 1000; d.Ce = 150; d.CeRegen = 8.5f; d.Tier = 1; d.Style = AIStyle.Summoner; d.PreferredRange = 4f;
            d.Bio = "Swallows cursed spirits and commands them as his own. Thousands of curses in his stomach, and a dream of a world without non-sorcerers.";
            d.Kit = () => new Ability[]
            {
                new SummonAb { Name = "Curse Release", Desc = "Releases a captured curse to hunt.", Cost = 20, Cooldown = 6f, Count = 1, MaxActive = 3, Color = H("#b080ff"),
                    Minion = new MinionDef { Name = "Captured Curse", Shape = MinionShape.Blob, Brain = MinionBrain.Chase, Color = H("#9070c0"), Hp = 120, Damage = 24, Speed = 7f, Life = 12f, Range = 1f, Size = 1.2f } },
                new SummonAb { Name = "Rainbow Serpent", Desc = "A hardened dragon curse that rams through the enemy.", Cost = 30, Cooldown = 10f, Count = 1, MaxActive = 1, Color = H("#ffb0ff"),
                    Minion = new MinionDef { Name = "Rainbow Serpent", Shape = MinionShape.Dragon, Brain = MinionBrain.Kamikaze, Color = H("#ffb0ff"), Fly = true, Hp = 400, Damage = 90, Speed = 13f, Size = 1.6f, Life = 4f, Range = 0.8f, Knockback = new Vector2(11f, 6f) } },
                new CustomAb { Name = "Split-Mouth Curse", Desc = "A riddle-asking curse: the enemy freezes while it asks.", Cost = 25, Cooldown = 9f, CastTime = 0.25f, Pose = FPose.Point, Use = AIUse.Utility, RangeMax = 10f, Color = H("#ff8080"),
                    Fn = (f, p) =>
                    {
                        var t = f.Target; if (t == null) return;
                        VFX.WorldText(t.HeadPos + Vector2.up * 0.8f, "\"AM I PRETTY?\"", H("#ff8080"), 1f);
                        var h = HitInfo.Make(f, HitSource.Other, 30f * p, Vector2.zero, 0.2f, HitFlags.Technique | HitFlags.Unblockable | HitFlags.NoCombo, DamageType.Slash, t.Center, H("#ff8080"));
                        f.M.Hit(t, h.WithStatus(StatusType.Stun, 1.0f, 0f));
                        VFX.Burst(BurstVis.Slashes, t.Center, 1.2f, H("#ff8080"));
                    } },
                MaelstromAb(),
            };

            // ---------------------------------------------------------- The stitched mind
            d = C("stitched", "Tsugime", "The Stitched Mind", "Curse Herding & Gravity", Era.Manga, Stance.Elegant, Hair.Bun, FaceMark.Stitches, Weapon.None, "#ff4a6a", "#d8c0ff");
            d.Hp = 1000; d.Ce = 160; d.CeRegen = 9f; d.Tier = 1; d.Passives = Passive.SimpleDomainMaster | Passive.RCT; d.RctRate = 30f; d.Domain = DomWomb; d.Style = AIStyle.Zoner; d.PreferredRange = 5f;
            d.Bio = "A brain that has hopped from body to body for a thousand years, collecting techniques. The stitches give it away.";
            d.Kit = () => new Ability[]
            {
                new CustomAb { Name = "Weightless Reversal", Desc = "Flips gravity around the target: launched up, then slammed down.", Cost = 22, Cooldown = 6f, CastTime = 0.2f, Pose = FPose.Raise, Use = AIUse.Attack, RangeMax = 10f, Color = H("#ff4a6a"),
                    Fn = (f, p) =>
                    {
                        var t = f.Target; if (t == null) return;
                        var up = HitInfo.Make(f, HitSource.Other, 25f * p, new Vector2(0f, 16f), 0.9f, HitFlags.Technique | HitFlags.Launch, DamageType.Gravity, t.Center, H("#ff4a6a"));
                        if (f.M.Hit(t, up) == HitResult.Hit)
                        {
                            VFX.Burst(BurstVis.Gravity, t.Center, 1.6f, H("#ff4a6a"));
                            var victim = t;
                            f.M.Schedule(0.45f, () =>
                            {
                                if (victim.Dead) return;
                                var down = HitInfo.Make(f, HitSource.Other, 55f * p, new Vector2(0f, -22f), 0.7f, HitFlags.Technique | HitFlags.Spike | HitFlags.Unblockable, DamageType.Gravity, victim.Center, H("#ff4a6a"));
                                f.M.Hit(victim, down);
                                VFX.Burst(BurstVis.Gravity, victim.Center, 2f, H("#ff4a6a"));
                            });
                        }
                    } },
                new SummonAb { Name = "Curse Release", Desc = "Releases a stolen curse.", Cost = 20, Cooldown = 6f, Count = 1, MaxActive = 3, Color = H("#9070c0"),
                    Minion = new MinionDef { Name = "Stolen Curse", Shape = MinionShape.Blob, Brain = MinionBrain.Chase, Color = H("#9070c0"), Hp = 120, Damage = 24, Speed = 7f, Life = 12f, Range = 1f, Size = 1.2f } },
                MaelstromAb(),
                DomainUlt(DomWomb, "Crushing gravity pins every enemy to the ground and grinds them down.", "#ff4a6a"),
            };

            // ---------------------------------------------------------- Sorcerer killer
            d = C("killer", "Toru Rokkaku", "The Sorcerer Killer", "Total Heavenly Restriction", Era.Anime, Stance.Agile, Hair.Short, FaceMark.Scar, Weapon.Spear, "#c0c0c0", "#2a2a2a");
            Restricted(d, 1150f, 1.45f, 1.35f);
            d.Tier = 1; d.Style = AIStyle.Rushdown; d.PreferredRange = 1.6f;
            d.Bio = "Traded every drop of cursed energy for a body that outruns sorcery. Carries a weapon that nullifies techniques on contact.";
            d.Kit = () => new Ability[]
            {
                new StrikeAb { Name = "Null Spear", Desc = "A thrust that pierces any defense and seals the target's techniques briefly.", Physical = true, Cost = 0, Cooldown = 4.5f, Color = H("#c0c0c0"),
                    Strike = new StrikeDef { Duration = 0.17f, Velocity = new Vector2(24f, 0f), Damage = 62, Type = DamageType.Pierce, Flags = HitFlags.Heavy | HitFlags.PierceInfinity | HitFlags.Unblockable, Pose = FPose.Stab, Knockback = new Vector2(8f, 3f), StopOnHit = true,
                        Status = StatusType.Sealed, StatusTime = 2f, Color = H("#e0e0e0"), Sound = Sfx.Slash } },
                new StrikeAb { Name = "Wayward Cloud", Desc = "A three-section staff barrage of raw force.", Physical = true, Cost = 0, Cooldown = 6f, Color = H("#c8a070"), RangeMax = 3.5f,
                    Strike = new StrikeDef { Duration = 0.45f, Velocity = new Vector2(10f, 0f), Hits = 5, Damage = 125, Pose = FPose.Spin, Radius = 1.2f, Knockback = new Vector2(10f, 5f), Color = H("#c8a070") } },
                new ProjectileAb { Name = "Thousand-Mile Chain", Desc = "Whips a chain that drags the enemy in.", Physical = true, Cost = 0, Cooldown = 7f, Pose = FPose.Throw, Color = H("#c0c0c0"),
                    Proj = new ProjDef { Vis = ProjVis.Needle, Color = H("#c0c0c0"), Damage = 30, Speed = 30f, Size = 0.4f, Life = 0.6f, PullOnHit = true, Type = DamageType.Blunt, Flags = HitFlags.None, Launch = Sfx.WhooshLight, Impact = Sfx.Block } },
                new BuffAb { Name = "Heavenly Pact", Desc = "Pure physical dominance: speed, power and a body that won't stagger.", Physical = true, Cost = 0, Cooldown = 30f, Status = StatusType.Haste, Mag = 0.45f, Status2 = StatusType.PowerUp, Mag2 = 0.35f, Duration = 9f, Color = H("#ffffff") },
            };

            // ---------------------------------------------------------- Eldest brother
            d = C("eldest", "Akashi Chimura", "Eldest Brother", "Blood Arts", Era.Anime, Stance.Martial, Hair.Twin, FaceMark.Marks, Weapon.None, "#ff2848", "#5a0010");
            d.Hp = 1100; d.Ce = 110; d.CeRegen = 7.5f; d.Passives = Passive.CurseBody; d.PreferredRange = 4f;
            d.Bio = "Half human, half curse, and the eldest of nine brothers. His blood never clots, so he can fight forever.";
            d.Kit = () => new Ability[]
            {
                new BeamAb { Name = "Crimson Lance", Desc = "Compressed blood fired at supersonic speed.", Cost = 18, Cooldown = 3.5f, Pose = FPose.Clap, Color = H("#ff2848"),
                    Beam = new BeamDef { Length = 14f, Width = 0.35f, Duration = 0.25f, TickDamage = 14f, TickRate = 0.05f, Color = H("#ff2848"), Core = H("#ffd0d8"), Type = DamageType.Blood, Charge = 0.12f, Knockback = new Vector2(3f, 0.5f), Sound = Sfx.Blood } },
                new ProjectileAb { Name = "Blood Nova", Desc = "Blood orbs burst outward in a ring.", Cost = 25, Cooldown = 6f, Count = 6, Spread = 30f, Arc = 0f, Aim = false, Pose = FPose.Raise, Color = H("#ff2848"),
                    Proj = new ProjDef { Vis = ProjVis.Blood, Color = H("#c01028"), Damage = 22, Speed = 15f, Size = 0.45f, Life = 1.2f, Type = DamageType.Blood, Knockback = new Vector2(4f, 3f), Launch = Sfx.Blood, Impact = Sfx.Blood } },
                new BuffAb { Name = "Red Scale Surge", Desc = "Floods his body with boiling blood.", Cost = 30, Cooldown = 15f, Status = StatusType.Haste, Mag = 0.3f, Status2 = StatusType.PowerUp, Mag2 = 0.3f, Duration = 9f, Color = H("#ff2848") },
                new BeamAb { Name = "Maximum Convergence", Desc = "Every drop compressed into one beam. Hold to concentrate more.", Cost = 75, Cooldown = 24f, Chantable = true, MaxChant = 2, Chant = new[] { "Brothers...", "...lend me your blood." }, Pose = FPose.Clap, Use = AIUse.Finisher, Color = H("#ff2848"), Armored = true,
                    Beam = new BeamDef { Length = 22f, Width = 0.9f, Duration = 0.7f, TickDamage = 22f, TickRate = 0.06f, Color = H("#ff2848"), Core = Color.white, Type = DamageType.Blood, Charge = 0.3f, Knockback = new Vector2(6f, 1f), Sound = Sfx.Blood } },
            };

            // ---------------------------------------------------------- Rot brothers
            d = C("rot", "Eiso & Kezu", "Rot Brothers", "Rotting Blood", Era.Anime, Stance.Feral, Hair.Wild, FaceMark.ExtraEyes, Weapon.None, "#9aff40", "#3a5a10", "#b8c8a0");
            d.Hp = 1050; d.Ce = 110; d.Passives = Passive.RotBlood | Passive.CurseBody; d.Style = AIStyle.Trickster; d.PreferredRange = 3.5f;
            d.Bio = "Two cursed-womb brothers whose blood rots anything it touches. Where one is, the other is never far.";
            d.Kit = () => new Ability[]
            {
                new ProjectileAb { Name = "Rot Spit", Desc = "Corrosive blood that rots over time.", Cost = 12, Cooldown = 2.5f, Count = 2, Spread = 8f, Pose = FPose.Point, Color = H("#9aff40"),
                    Proj = new ProjDef { Vis = ProjVis.Blood, Color = H("#9aff40"), Damage = 15, Speed = 18f, Size = 0.4f, Type = DamageType.Poison, Status = StatusType.Rot, StatusTime = 4f, StatusMag = 5f, Launch = Sfx.Blood, Impact = Sfx.Blood } },
                new SummonAb { Name = "Little Brother", Desc = "The younger brother leaps in to brawl.", Cost = 25, Cooldown = 11f, Count = 1, MaxActive = 1, Color = H("#c8ff80"),
                    Minion = new MinionDef { Name = "Kezu", Shape = MinionShape.Humanoid, Brain = MinionBrain.Chase, Color = H("#c8ff80"), Hp = 180, Damage = 22, Speed = 7f, Life = 12f, Range = 1f, Status = StatusType.Rot, StatusTime = 3f, StatusMag = 4f, Type = DamageType.Poison } },
                new ZoneAb { Name = "Rot Field", Desc = "Spreads rotting blood on the ground.", Cost = 28, Cooldown = 10f, Color = H("#9aff40"),
                    Zone = new ZoneDef { Width = 4f, Height = 1.6f, Life = 4f, TickDamage = 8f, TickRate = 0.3f, Status = StatusType.Rot, StatusTime = 2f, StatusMag = 5f, Vis = ZoneVis.Rot, Color = H("#9aff40"), Type = DamageType.Poison } },
                new StrikeAb { Name = "Wing King", Desc = "Blood wings: a diving strike that rots everything it grazes.", Cost = 65, Cooldown = 22f, Use = AIUse.Finisher, Color = H("#9aff40"), RangeMax = 8f,
                    Strike = new StrikeDef { Duration = 0.45f, Velocity = new Vector2(16f, 3f), Hits = 4, Damage = 150, Type = DamageType.Poison, Status = StatusType.Rot, StatusTime = 5f, StatusMag = 8f, Pose = FPose.Dash, Radius = 1.3f, Color = H("#9aff40"), Invuln = true } },
            };

            // ---------------------------------------------------------- Moon jellyfish
            d = C("jelly", "Jun Yoshida", "Moon Jellyfish", "Lunar Medusa", Era.Anime, Stance.Caster, Hair.Long, FaceMark.None, Weapon.None, "#b0c8ff", "#5a6aa0");
            d.Hp = 920; d.Ce = 100; d.CeRegen = 7f; d.Style = AIStyle.Zoner; d.Tier = 3; d.PreferredRange = 5f;
            d.Bio = "A quiet boy and his jellyfish shikigami, whose sting carries a paralyzing poison.";
            d.Kit = () => new Ability[]
            {
                new ProjectileAb { Name = "Sting", Desc = "A poisoned tentacle lash.", Cost = 10, Cooldown = 2f, Pose = FPose.Point, Color = H("#b0c8ff"),
                    Proj = new ProjDef { Vis = ProjVis.Needle, Color = H("#b0c8ff"), Damage = 18, Speed = 20f, Size = 0.35f, Type = DamageType.Poison, Status = StatusType.Poison, StatusTime = 3f, StatusMag = 6f, Launch = Sfx.WhooshLight } },
                new SummonAb { Name = "Lunar Medusa", Desc = "A floating jellyfish that stings anything near it.", Cost = 22, Cooldown = 8f, Count = 1, MaxActive = 2, Color = H("#b0c8ff"),
                    Minion = new MinionDef { Name = "Medusa", Shape = MinionShape.Jellyfish, Brain = MinionBrain.Static, Color = H("#b0c8ff"), Fly = true, Hp = 120, Damage = 16, Range = 1.6f, AttackRate = 0.7f, Life = 10f, Size = 1.3f, Type = DamageType.Poison, Status = StatusType.Poison, StatusTime = 2f, StatusMag = 6f } },
                new ZoneAb { Name = "Poison Mist", Desc = "A cloud of toxin.", Cost = 28, Cooldown = 10f, Color = H("#a0b0ff"),
                    Zone = new ZoneDef { Width = 3.5f, Height = 3f, Life = 4f, TickDamage = 7f, TickRate = 0.3f, Status = StatusType.Poison, StatusTime = 2f, StatusMag = 6f, Vis = ZoneVis.Smoke, Color = H("#a0b0ff"), Type = DamageType.Poison } },
                new CustomAb { Name = "Moon Dregs", Desc = "The jellyfish envelops you: shield, and a stinging aura.", Cost = 60, Cooldown = 24f, CastTime = 0.3f, Pose = FPose.Channel, Use = AIUse.Buff, Color = H("#b0c8ff"),
                    Fn = (f, p) =>
                    {
                        f.AddStatus(StatusType.Shielded, 7f, 0.5f, f);
                        f.M.SpawnZone(f, new ZoneDef { Width = 3.2f, Height = 2.6f, Life = 7f, TickDamage = 10f, TickRate = 0.3f, Status = StatusType.Poison, StatusTime = 2f, StatusMag = 7f, FollowOwner = true, Vis = ZoneVis.Stars, Color = H("#b0c8ff"), Type = DamageType.Poison }, f.Pos, p);
                    } },
            };

            // ---------------------------------------------------------- Lucky survivor
            d = C("lucky", "Kenta Moriyama", "Lucky Survivor", "Miracle Stock", Era.Anime, Stance.Agile, Hair.Messy, FaceMark.None, Weapon.Pen, "#a0ffa0", "#2a4a2a");
            d.Hp = 900; d.Ce = 100; d.Speed = 1.1f; d.Passives = Passive.LuckStored; d.Tier = 3; d.Style = AIStyle.Trickster; d.PreferredRange = 4f;
            d.Bio = "Stockpiles small miracles in daily life, then spends them to survive certain death. Exactly once.";
            d.Kit = () => new Ability[]
            {
                new ProjectileAb { Name = "Knife Throw", Desc = "Two quick knives.", Physical = true, Cost = 0, Cooldown = 2f, Count = 2, Interval = 0.07f, Pose = FPose.Throw, Color = H("#e0e0e0"),
                    Proj = new ProjDef { Vis = ProjVis.Needle, Color = H("#e0e0e0"), Damage = 20, Speed = 26f, Size = 0.3f, Type = DamageType.Pierce, Flags = HitFlags.None } },
                new BuffAb { Name = "Lucky Break", Desc = "Spend a stored miracle: survive the next lethal hit.", Cost = 30, Cooldown = 20f, Status = StatusType.Lucky, Mag = 1f, Duration = 10f, Color = H("#a0ffa0") },
                new StrikeAb { Name = "Desperate Stab", Desc = "A frantic lunge.", Cost = 20, Cooldown = 5f, Color = H("#a0ffa0"),
                    Strike = new StrikeDef { Duration = 0.2f, Velocity = new Vector2(18f, 0f), Damage = 60, Type = DamageType.Pierce, Pose = FPose.Stab, Color = H("#a0ffa0") } },
                new BuffAb { Name = "Miracle Stock", Desc = "Cashes in everything: big heal and another miracle.", Cost = 60, Cooldown = 30f, Heal = 250f, Status = StatusType.Lucky, Mag = 1f, Duration = 12f, Status2 = StatusType.Haste, Mag2 = 0.2f, Color = H("#a0ffa0"), Use = AIUse.Heal },
            };

            // ---------------------------------------------------------- Frost retainer
            d = C("frost", "Hyomi", "Frost Retainer", "Ice Formation", Era.Manga, Stance.Elegant, Hair.Bob, FaceMark.None, Weapon.None, "#c8f0ff", "#ffffff", "#f4fbff");
            d.Hp = 1000; d.Ce = 130; d.CeRegen = 8f; d.Tier = 1; d.Style = AIStyle.Zoner; d.PreferredRange = 6f;
            d.Bio = "A thousand-year retainer of the calamity king, cold as the ice it commands.";
            d.Kit = () => new Ability[]
            {
                new ProjectileAb { Name = "Frost Shards", Desc = "Ice shards that can freeze on hit.", Cost = 14, Cooldown = 2.5f, Count = 3, Spread = 6f, Pose = FPose.Point, Color = H("#c8f0ff"),
                    Proj = new ProjDef { Vis = ProjVis.Ice, Color = H("#c8f0ff"), Damage = 20, Speed = 22f, Size = 0.45f, Type = DamageType.Ice, Status = StatusType.Slow, StatusTime = 1.5f, StatusMag = 0.3f, Launch = Sfx.Ice, Impact = Sfx.Ice } },
                new ZoneAb { Name = "Stilled Frost", Desc = "Freezing air that slows to a crawl.", Cost = 28, Cooldown = 9f, Color = H("#c8f0ff"),
                    Zone = new ZoneDef { Width = 4.5f, Height = 3f, Life = 3.5f, TickDamage = 8f, TickRate = 0.25f, Status = StatusType.Slow, StatusTime = 0.6f, StatusMag = 0.6f, Vis = ZoneVis.Ice, Color = H("#c8f0ff"), Type = DamageType.Ice } },
                new BurstAb { Name = "Ice Pillar", Desc = "A pillar of ice erupts under the enemy and freezes them.", Cost = 40, Cooldown = 12f, AtTarget = true, Radius = 1.5f, Damage = 85, Knockback = new Vector2(0f, 10f), Status = StatusType.Freeze, StatusTime = 1.0f, Vis = BurstVis.Ice, Delay = 0.35f, Color = H("#c8f0ff"), Pose = FPose.Raise, Sound = Sfx.Ice, RangeMax = 12f },
                new ProjectileAb { Name = "Glacial Collapse", Desc = "A storm of colossal icicles from above.", Cost = 80, Cooldown = 26f, Count = 6, Interval = 0.1f, FromSky = true, Pose = FPose.Raise, Use = AIUse.Finisher, CastTime = 0.4f, Color = H("#c8f0ff"),
                    Proj = new ProjDef { Vis = ProjVis.Ice, Color = H("#c8f0ff"), Damage = 40, Speed = 24f, Size = 1.0f, Type = DamageType.Ice, Status = StatusType.Freeze, StatusTime = 0.5f, ExplodeRadius = 1.4f, ExplodeDamage = 20f, Unerasable = true, Flags = HitFlags.Technique | HitFlags.Heavy, Launch = Sfx.Ice, Impact = Sfx.Ice } },
            };

            // ---------------------------------------------------------- Séance granny
            d = C("seance", "Granny Tamaki", "Seance", "Reincarnation Seance", Era.Anime, Stance.Caster, Hair.Bun, FaceMark.None, Weapon.None, "#d0c0ff", "#5a4a7a");
            d.Hp = 900; d.Ce = 120; d.CeRegen = 8f; d.Speed = 0.9f; d.Style = AIStyle.Summoner; d.Tier = 3; d.PreferredRange = 6f;
            d.Bio = "Channels the dead into a living vessel. Sometimes the spirit she calls is far stronger than she bargained for.";
            d.Kit = () => new Ability[]
            {
                new SummonAb { Name = "Possessed Vessel", Desc = "Channels a dead fighter into a puppet body.", Cost = 30, Cooldown = 10f, Count = 1, MaxActive = 1, Color = H("#d0c0ff"),
                    Minion = new MinionDef { Name = "Possessed", Shape = MinionShape.Humanoid, Brain = MinionBrain.Chase, Color = H("#d0c0ff"), Hp = 250, Damage = 32, Speed = 8.5f, Life = 14f, Range = 1.1f, Knockback = new Vector2(7f, 4f) } },
                new ProjectileAb { Name = "Spirit Wisp", Desc = "A wandering spirit that seeks the living.", Cost = 12, Cooldown = 2.5f, Pose = FPose.Point, Color = H("#d0c0ff"),
                    Proj = new ProjDef { Vis = ProjVis.Skull, Color = H("#d0c0ff"), Damage = 30, Speed = 10f, Size = 0.5f, Homing = 2.5f, Life = 3f, Type = DamageType.Soul, Launch = Sfx.Chant } },
                new ZoneAb { Name = "Ritual Circle", Desc = "Heals allies who stand inside, harms enemies.", Cost = 30, Cooldown = 12f, AtTarget = false, Use = AIUse.Heal, Color = H("#d0c0ff"),
                    Zone = new ZoneDef { Width = 4f, Height = 2.5f, Life = 5f, TickDamage = 12f, TickRate = 0.4f, HealsAllies = true, Vis = ZoneVis.Stars, Color = H("#d0c0ff"), Type = DamageType.Soul } },
                new SummonAb { Name = "Full Descent", Desc = "Calls down a legendary killer - who obeys no one but himself.", Cost = 85, Cooldown = 32f, Count = 1, MaxActive = 1, Use = AIUse.Summon, Color = H("#ffffff"),
                    Minion = new MinionDef { Name = "Descended Killer", Shape = MinionShape.Humanoid, Brain = MinionBrain.Chase, Color = H("#ffffff"), Hp = 500, Damage = 55, Speed = 11f, Size = 1.15f, Life = 14f, Range = 1.3f, AttackRate = 0.6f,
                        Knockback = new Vector2(10f, 5f), Hitstun = 0.5f, Flags = HitFlags.Minion | HitFlags.PierceInfinity | HitFlags.Heavy } },
            };

            // ---------------------------------------------------------- Twin acolytes
            d = C("twins", "Suzu & Kaya", "Twin Acolytes", "Shutter & Noose", Era.Anime, Stance.Caster, Hair.Long, FaceMark.None, Weapon.Phone, "#ff9ad0", "#2a2a3a");
            d.Hp = 950; d.Ce = 100; d.Style = AIStyle.Trickster; d.Tier = 3; d.PreferredRange = 5f;
            d.Bio = "Twin curse users devoted to their master. One curses through a camera lens, the other through a hanging doll.";
            d.Kit = () => new Ability[]
            {
                new ProjectileAb { Name = "Cursed Snapshot", Desc = "A camera flash that stuns briefly.", Cost = 15, Cooldown = 4f, Pose = FPose.Shoot, Color = H("#ffffff"),
                    Proj = new ProjDef { Vis = ProjVis.Card, Color = H("#ffffff"), Damage = 25, Speed = 30f, Size = 0.6f, Type = DamageType.Light, Status = StatusType.Stun, StatusTime = 0.5f, Launch = Sfx.Teleport } },
                new ProjectileAb { Name = "Noose Doll", Desc = "A cursed rope that binds and drags.", Cost = 22, Cooldown = 7f, Pose = FPose.Throw, Color = H("#ff9ad0"),
                    Proj = new ProjDef { Vis = ProjVis.Needle, Color = H("#ff9ad0"), Damage = 30, Speed = 20f, Size = 0.4f, PullOnHit = true, Status = StatusType.Bind, StatusTime = 0.8f, Type = DamageType.Blunt } },
                new SummonAb { Name = "Twin Assault", Desc = "Her sister joins the fight.", Cost = 28, Cooldown = 12f, Count = 1, MaxActive = 1, Color = H("#ff9ad0"),
                    Minion = new MinionDef { Name = "Twin", Shape = MinionShape.Humanoid, Brain = MinionBrain.Shooter, Color = H("#ff9ad0"), Hp = 150, Damage = 0, Speed = 6f, Life = 12f, AttackRate = 1.2f,
                        Shot = new ProjDef { Vis = ProjVis.Card, Color = H("#ff9ad0"), Damage = 18, Speed = 20f, Size = 0.4f, Type = DamageType.Light } } },
                new CustomAb { Name = "Hanging Ritual", Desc = "Every marked soul is strung up: heavy damage and a long bind.", Cost = 70, Cooldown = 26f, CastTime = 0.4f, Pose = FPose.Raise, Use = AIUse.Finisher, RangeMax = 30f, Color = H("#ff9ad0"),
                    Fn = (f, p) =>
                    {
                        foreach (var e in f.M.Fighters)
                        {
                            if (!f.M.IsEnemy(f, e) || e.Dead) continue;
                            var h = HitInfo.Make(f, HitSource.Other, 140f * p, new Vector2(0f, 6f), 0.8f, HitFlags.Technique | HitFlags.Unblockable, DamageType.Blunt, e.Center, H("#ff9ad0"));
                            f.M.Hit(e, h.WithStatus(StatusType.Bind, 1.5f, 0f));
                            VFX.Lightning(e.HeadPos + Vector2.up * 4f, e.HeadPos, H("#ff9ad0"), 0.08f, 0.6f, 6);
                        }
                    } },
            };
        }

        static ProjectileAb MaelstromAb() => new ProjectileAb
        {
            Name = "Maximum: Maelstrom", Desc = "Compresses every captured curse into a single spiraling sphere.", Cost = 80, Cooldown = 26f, Pose = FPose.TwoPalm, Use = AIUse.Finisher, CastTime = 0.5f,
            Chantable = true, MaxChant = 1, Chant = new[] { "Uncountable curses... become one." }, Color = Art.Hex("#7a4aff"), Armored = true,
            Proj = new ProjDef
            {
                Vis = ProjVis.Sphere, Color = Art.Hex("#4a2a8a"), Core = Art.Hex("#ff6aff"), Size = 1.8f, Speed = 10f, Life = 2.6f, Damage = 30f, TickInterval = 0.2f,
                PullRadius = 3f, PullForce = 6f, Unerasable = true, Erase = true, ExplodeRadius = 3f, ExplodeDamage = 150f, ExplodeKb = new Vector2(12f, 8f),
                Flags = HitFlags.Technique | HitFlags.Heavy, Type = DamageType.Energy, Hitstun = 0.3f, Knockback = new Vector2(2f, 1f), Launch = Sfx.Rumble, Impact = Sfx.Explosion
            }
        };
    }
}
