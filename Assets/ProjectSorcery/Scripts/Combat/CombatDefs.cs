using System;
using UnityEngine;

namespace ProjectSorcery
{
    public enum FPose
    {
        Idle, Run, Jump, Fall, Block, Hurt, Down, GetUp, Dash, Jab, Cross, Hook, Kick, Uppercut, Sweep, HeavyPunch,
        AirKick, DiveKick, AirSlash, Point, Palm, TwoPalm, HandSign, Raise, Throw, Slash, Stab, Spin, Slam, Chant,
        Victory, Dead, Channel, Clap, Crouch, Grab, Bow, Shoot, Taunt
    }

    /// <summary>A basic melee attack (lights, heavies, aerials).</summary>
    public sealed class AttackDef
    {
        public string Name;
        public FPose Pose;
        public float Startup = 0.07f, Active = 0.07f, Recovery = 0.14f;
        public Vector2 Offset = new Vector2(0.75f, 1.3f);
        public float Radius = 0.55f;
        public float Damage = 30f;
        public Vector2 Knockback = new Vector2(2.5f, 1.5f);
        public float Hitstun = 0.32f, Hitstop = 0.055f;
        public HitFlags Flags = HitFlags.Light;
        public DamageType Type = DamageType.Blunt;
        public Vector2 Lunge = new Vector2(3f, 0f);
        public bool Air;
        public float CeGain = 3f;
        public bool Chargeable;          // hold the button to charge (heavy smash)
        public bool DashStrike;          // performed out of a dash
        public Clip Clip;                // visual choreography (never read by the simulation)
        public float Total => Startup + Active + Recovery;
        public AttackDef Copy() => (AttackDef)MemberwiseClone();
    }

    public enum ProjVis { Orb, Bolt, Slash, Arrow, Nail, Fire, Blood, Rock, Bird, Insect, Lightning, Ice, Water, Star, Sphere, Disc, Wave, Needle, Bullet, Skull, Sound, Wood, Card, Metal }

    public sealed class ProjDef
    {
        public ProjVis Vis = ProjVis.Orb;
        public Color Color = Color.white, Core = Color.white;
        public float Size = 0.35f;
        public float Speed = 14f, Life = 2.2f, Gravity, Homing, Accel, Drag;
        public float Damage = 50f, Hitstun = 0.35f, Hitstop = 0.06f;
        public Vector2 Knockback = new Vector2(5f, 3f);
        public HitFlags Flags = HitFlags.Technique;
        public DamageType Type = DamageType.Energy;
        public int Pierce;                     // extra fighters it can pass through
        public float PullRadius, PullForce;    // positive attracts, negative repels
        public float ExplodeRadius, ExplodeDamage;
        public Vector2 ExplodeKb = new Vector2(7f, 5f);
        public StatusType Status = StatusType.Count;
        public float StatusTime, StatusMag;
        public bool Erase;                     // annihilates other projectiles it touches
        public bool Unerasable;
        public float GrowRate;                 // size growth per second
        public float TickInterval;             // >0: multi-hit while overlapping
        public bool PullOnHit;                 // yanks the victim to the owner (tongues, chains)
        public bool StickAndDetonate;          // embeds, detonates later (nails)
        public float DetonateDelay = 0.8f;
        public bool Ground;                    // hugs the floor
        public bool Returns;                   // boomerang back to owner
        public bool HitsMinions = true;
        public bool DestroyOnWall = true;
        public Sfx Launch = Sfx.Shoot, Impact = Sfx.ImpactEnergy;
        public float TrailTime = 0.18f;
        public ProjDef Clone() => (ProjDef)MemberwiseClone();
    }

    public sealed class BeamDef
    {
        public float Length = 16f, Width = 0.6f, Duration = 0.6f;
        public float TickDamage = 18f, TickRate = 0.08f;
        public Vector2 Knockback = new Vector2(3f, 1f);
        public float Hitstun = 0.2f;
        public Color Color = Color.white, Core = Color.white;
        public DamageType Type = DamageType.Energy;
        public HitFlags Flags = HitFlags.Technique;
        public StatusType Status = StatusType.Count;
        public float StatusTime, StatusMag;
        public bool FromSky;        // vertical pillar from above the target point
        public bool Track;          // follows the caster's hand
        public float Sweep;         // degrees swept over the duration
        public float Charge = 0.15f;
        public Sfx Sound = Sfx.Beam;
        public BeamDef Clone() => (BeamDef)MemberwiseClone();
    }

    public enum MinionShape { Dog, Bird, Toad, Elephant, Humanoid, Blob, Fish, Giant, Crow, Insect, Jellyfish, Serpent, Rabbit, Ox, Deer, Tiger, Dragon, Puppet, Corpse, Cockroach, Judge, Wheel, Garuda, Mech, Car }
    public enum MinionBrain { Chase, Guard, Kamikaze, Static, Bomber, Grabber, Flood, Healer, Shooter, Swarm }

    public sealed class MinionDef
    {
        public string Name = "Shikigami";
        public MinionShape Shape = MinionShape.Dog;
        public MinionBrain Brain = MinionBrain.Chase;
        public float Hp = 120f, Speed = 6f, Damage = 25f, Range = 1.0f, AttackRate = 0.8f, Size = 1f, Life = 10f;
        public bool Fly;
        public Color Color = Color.white;
        public Vector2 Knockback = new Vector2(4f, 3f);
        public float Hitstun = 0.3f;
        public HitFlags Flags = HitFlags.Technique | HitFlags.Minion;
        public DamageType Type = DamageType.Blunt;
        public StatusType Status = StatusType.Count;
        public float StatusTime, StatusMag;
        public ProjDef Shot;          // for Shooter brain
        public float HealRate;        // for Healer brain (heals owner)
        public bool Invulnerable;
        public bool Adapts;           // wheel guardian
        public MinionDef Clone() => (MinionDef)MemberwiseClone();
    }

    public enum ZoneVis { Flowers, Fire, Ice, Water, Gravity, Lightning, Shadow, Spikes, Blood, Stars, Rot, Smoke, Insects, Sound }

    public sealed class ZoneDef
    {
        public float Width = 4f, Height = 2.5f, Life = 4f;
        public float TickDamage = 8f, TickRate = 0.3f;
        public StatusType Status = StatusType.Count;
        public float StatusTime = 0.5f, StatusMag;
        public ZoneVis Vis = ZoneVis.Fire;
        public Color Color = Color.white;
        public bool FollowOwner;
        public bool HealsAllies;
        public DamageType Type = DamageType.Energy;
        public bool BlocksProjectiles;
    }

    /// <summary>A scripted lunge / rush / multi-hit move driven by an ability.</summary>
    public sealed class StrikeDef
    {
        public float Duration = 0.25f;
        public Vector2 Velocity = new Vector2(16f, 0f);   // relative to facing
        public bool Gravity;
        public Vector2 Offset = new Vector2(0.6f, 1.1f);
        public float Radius = 0.8f;
        public float Damage = 60f;
        public Vector2 Knockback = new Vector2(8f, 4f);
        public float Hitstun = 0.45f, Hitstop = 0.08f;
        public HitFlags Flags = HitFlags.Heavy;
        public DamageType Type = DamageType.Blunt;
        public int Hits = 1;
        public float HitInterval = 0.07f;
        public StatusType Status = StatusType.Count;
        public float StatusTime, StatusMag;
        public Color Color = Color.white;
        public FPose Pose = FPose.Dash;
        public bool Afterimages = true;
        public bool Invuln;
        public bool StopOnHit;
        public bool TeleportBehind;     // blink behind the target first
        public bool BlackFlash;         // final hit is a guaranteed black flash
        public float DelayedDamage, DelayedTime = 0.2f;  // second impact (divergent strikes)
        public Action<Fighter, Fighter, float> OnHit;     // (attacker, victim, power)
        public Sfx Sound = Sfx.Whoosh;
    }

    public enum BurstVis { Shockwave, Explosion, Slashes, Pillar, Sound, Lightning, Ice, Bloom, Gravity, Blood, Water, Light, Dark, Stars, Fire }
}
