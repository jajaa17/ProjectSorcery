using System;
using UnityEngine;

namespace ProjectSorcery
{
    public enum FState { Idle, Run, Air, Attack, Cast, Block, Hitstun, Knockdown, GetUp, Dash, Dead, Victory, Locked, RCT, SimpleDomain, Respawning }

    public enum DamageType { Blunt, Slash, Pierce, Fire, Ice, Electric, Water, Blood, Soul, Energy, Gravity, Poison, Light, Sound, Count }

    [Flags]
    public enum HitFlags
    {
        None = 0,
        Heavy = 1 << 0,
        GuardBreak = 1 << 1,
        Unblockable = 1 << 2,
        PierceInfinity = 1 << 3,
        SureHit = 1 << 4,
        Launch = 1 << 5,
        Spike = 1 << 6,
        NoHitstop = 1 << 7,
        BlackFlash = 1 << 8,
        Soul = 1 << 9,
        Projectile = 1 << 10,
        NoKnockdown = 1 << 11,
        Technique = 1 << 12,  // cursed technique (cancelled by technique-nullifiers)
        Finisher = 1 << 13,
        Light = 1 << 14,      // light attack (combo chain, black flash window opener)
        Minion = 1 << 15,
        IgnoreArmor = 1 << 16,
        NoCombo = 1 << 17,
    }

    public enum StatusType
    {
        Stun, Slow, Burn, Bind, Freeze, Mark, Sealed, Executioner, Jackpot, Zone, Burnout, Overtime, Haste,
        Weaken, Poison, Paralysis, Sink, Gravity, Amplification, Armor, Invuln, Comedy, Shielded, PowerUp,
        DefenseUp, Bleed, Confused, Rot, Lucky, Count
    }

    public struct Status
    {
        public StatusType Type;
        public float Time, Mag;
        public Fighter Src;
    }

    public enum HitResult { Miss, Hit, Blocked, Nullified, Dodged, Countered, Immune }

    public enum HitSource { Melee, Projectile, Beam, Domain, Minion, Zone, Strike, Other }

    /// <summary>Everything about one hit, built by the attacker and resolved by the victim.</summary>
    public struct HitInfo
    {
        public Fighter Attacker;
        public HitSource Source;
        public float Damage;
        public Vector2 Knockback;     // world-space
        public float Hitstun, Hitstop;
        public HitFlags Flags;
        public DamageType Type;
        public StatusType Status;     // StatusType.Count = none
        public float StatusTime, StatusMag;
        public Vector2 Point;
        public Color Color;
        public float CeGain;          // CE the attacker gains on hit

        public bool Has(HitFlags f) => (Flags & f) != 0;

        public static HitInfo Make(Fighter attacker, HitSource src, float dmg, Vector2 kb, float hitstun, HitFlags flags, DamageType type, Vector2 point, Color color)
        {
            return new HitInfo
            {
                Attacker = attacker, Source = src, Damage = dmg, Knockback = kb, Hitstun = hitstun,
                Hitstop = 0.06f, Flags = flags, Type = type, Status = StatusType.Count, Point = point, Color = color,
                CeGain = dmg * 0.06f
            };
        }

        public HitInfo WithStatus(StatusType s, float time, float mag)
        {
            Status = s; StatusTime = time; StatusMag = mag;
            return this;
        }
    }

    [Flags]
    public enum Passive
    {
        None = 0,
        Infinity = 1 << 0,            // nullifies most incoming hits unless pierced (costs CE per hit)
        SixEyes = 1 << 1,             // halves technique cost, faster domain recovery, clash bonus
        AutoRCT = 1 << 2,             // passive reverse cursed technique regeneration
        RCT = 1 << 3,                 // can channel RCT (Down + Block)
        BlackFlashAffinity = 1 << 4,  // wider black flash window, higher random chance
        HeavenlyRestriction = 1 << 5, // zero cursed energy: invisible to sure-hit, superhuman body, no techniques/RCT
        Adaptation = 1 << 6,          // adapts to damage types after being hit by them
        SoulResist = 1 << 7,          // soul-based attacks deal far less damage
        SimpleDomainMaster = 1 << 8,  // simple domain fully negates sure-hit and allows walking
        CurseBody = 1 << 9,           // cursed spirit: heals from own CE, weak to Light/RCT-type damage
        ToughBody = 1 << 10,          // heavy attacks have super armor
        Flight = 1 << 11,             // slow fall / hover when holding jump
        LuckStored = 1 << 12,         // survives one lethal hit
        PerfectBody = 1 << 13,        // reshapes self: non-soul damage reduced
        Restricted = 1 << 14,         // reverse heavenly restriction: huge CE output, fragile body
        Support = 1 << 15,            // heals/buffs nearby allies passively
        RotBlood = 1 << 16,           // attacks inflict rot
        WeaknessInverted = 1 << 17,   // strong hits against this fighter are weakened, weak hits strengthened
        Counter = 1 << 18,            // no CE so domains can't see them (Simple domain-like innate)
    }

    public enum Era { Anime, Manga, Modulo, Boss }
    public enum AIStyle { Balanced, Rushdown, Zoner, Grappler, Summoner, Trickster, Support }

    // Visual look of a stickman
    public enum Hair { None, Spiky, Messy, Slick, Ponytail, Bun, Long, Short, Wild, Twin, TopKnot, Horns, Volcano, Ears, Hood, Bob, Afro, Braid, Bald }
    [Flags]
    public enum FaceMark
    {
        None = 0, Blindfold = 1, Glasses = 2, Sunglasses = 4, Marks = 8, Stitches = 16, Patches = 32, Collar = 64,
        Scar = 128, Goggles = 256, Halo = 512, Wheel = 1024, ExtraEyes = 2048, Mask = 4096, Wings = 8192, Crown = 16384, Beard = 32768
    }
    public enum Weapon { None, Katana, Spear, Cloud, Polearm, Hammer, Cleaver, Gavel, Rope, Staff, Bow, Broom, Revolver, Guitar, Fan, Sword, Claws, Chain, Axe, Pen, Receipts, Phone }

    public struct Look
    {
        public Hair Hair;
        public FaceMark Face;
        public Weapon Weapon;
        public bool FourArms;
        public Color Body, Aura, Accent;
    }
}
