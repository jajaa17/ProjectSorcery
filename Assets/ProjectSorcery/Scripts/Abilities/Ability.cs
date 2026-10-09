using System;
using UnityEngine;

namespace ProjectSorcery
{
    public enum AIUse { Attack, Projectile, Gapclose, Escape, Buff, Heal, Summon, Area, Finisher, Counter, Domain, Zone, Utility }

    /// <summary>
    /// A special move. Abilities are stateless templates; per-fighter state (cooldown, chant) lives on the Fighter.
    /// Execute() runs inside the deterministic simulation: use f.M.Rng, never UnityEngine.Random.
    /// </summary>
    public abstract class Ability
    {
        public string Name = "Technique";
        public string Desc = "";
        public float Cost = 20f;
        public float Cooldown = 3f;
        public float CastTime = 0.18f;
        public float Recovery = 0.22f;
        public FPose Pose = FPose.Point;
        public bool Chantable;
        public string[] Chant;
        public int MaxChant = 2;
        public bool UsableInAir = true;
        public bool Physical;              // not a cursed technique (unaffected by burnout/seal, usable with no CE)
        public Color Color = Color.white;
        public AIUse Use = AIUse.Attack;
        public float RangeMin = 0f, RangeMax = 3f;
        public Sfx CastSound = Sfx.Charge;
        public bool Armored;               // super armor while casting

        public virtual bool IsDomain => false;
        public virtual bool CanUse(Fighter f) => true;
        public virtual void OnCastStart(Fighter f) { }
        public abstract void Execute(Fighter f, float power);

        /// <summary>Called by the move list UI.</summary>
        public virtual string Summary() => Desc;
    }

    public sealed class ProjectileAb : Ability
    {
        public ProjDef Proj;
        public int Count = 1;
        public float Spread;          // degrees between shots
        public float Interval;        // seconds between shots (0 = simultaneous)
        public bool Aim = true;       // aim at target
        public Vector2 Offset = new Vector2(0.8f, 1.35f);
        public float Arc;             // upward angle in degrees
        public bool FromSky;          // drop on target from above
        public bool AtTargetGround;   // erupt under target

        public ProjectileAb() { Use = AIUse.Projectile; RangeMin = 2.5f; RangeMax = 14f; Pose = FPose.Point; }

        public override void Execute(Fighter f, float power)
        {
            var m = f.M;
            for (int i = 0; i < Count; i++)
            {
                int idx = i;
                Action fire = () =>
                {
                    if (f.Dead) return;
                    Vector2 pos, dir;
                    var t = f.Target;
                    if (FromSky)
                    {
                        float tx = t != null ? t.Pos.x : f.Pos.x + f.Facing * 5f;
                        tx += (idx - (Count - 1) * 0.5f) * 1.6f + m.Rng.Range(-0.4f, 0.4f);
                        pos = new Vector2(tx - f.Facing * 3f, m.Arena.Ceiling + 2f);
                        dir = new Vector2(f.Facing * 0.45f, -1f).normalized;
                    }
                    else if (AtTargetGround)
                    {
                        float tx = t != null ? t.Pos.x : f.Pos.x + f.Facing * 5f;
                        tx += (idx - (Count - 1) * 0.5f) * 1.2f;
                        pos = new Vector2(tx, -0.6f);
                        dir = Vector2.up;
                    }
                    else
                    {
                        pos = f.Front(Offset.x, Offset.y);
                        dir = Aim && t != null ? (t.Center - pos).normalized : new Vector2(f.Facing, 0f);
                        if (!Aim || t == null || Vector2.Dot(dir, new Vector2(f.Facing, 0)) < 0.2f) dir = new Vector2(f.Facing, 0f);
                        float ang = Arc + (Count > 1 ? (idx - (Count - 1) * 0.5f) * Spread : 0f);
                        if (ang != 0f) dir = Rotate(dir, ang * f.Facing);
                    }
                    m.Shoot(f, Proj, pos, dir, power);
                };
                if (Interval > 0f && i > 0) m.Schedule(Interval * i, fire); else fire();
            }
        }

        public static Vector2 Rotate(Vector2 v, float deg)
        {
            float r = deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
    }

    public sealed class BeamAb : Ability
    {
        public BeamDef Beam;
        public bool Aim = true;
        public Vector2 Offset = new Vector2(0.8f, 1.35f);
        public BeamAb() { Use = AIUse.Projectile; RangeMin = 1.5f; RangeMax = 14f; Pose = FPose.TwoPalm; }

        public override void Execute(Fighter f, float power)
        {
            var t = f.Target;
            if (Beam.FromSky)
            {
                float x = t != null ? t.Pos.x : f.Pos.x + f.Facing * 4f;
                f.M.SpawnBeam(f, Beam, new Vector2(x, f.M.Arena.Ceiling + 4f), Vector2.down, power);
                return;
            }
            Vector2 o = f.Front(Offset.x, Offset.y);
            Vector2 d = new Vector2(f.Facing, 0f);
            if (Aim && t != null)
            {
                Vector2 to = (t.Center - o).normalized;
                if (Vector2.Dot(to, d) > 0.5f) d = to;
            }
            f.M.SpawnBeam(f, Beam, o, d, power);
        }
    }

    public sealed class StrikeAb : Ability
    {
        public StrikeDef Strike;
        public StrikeAb() { Use = AIUse.Gapclose; RangeMin = 0f; RangeMax = 6f; Pose = FPose.Dash; CastTime = 0.1f; Recovery = 0.1f; }
        public override void Execute(Fighter f, float power) => f.BeginStrike(Strike, power);
    }

    public sealed class BurstAb : Ability
    {
        public float Radius = 2.5f;
        public Vector2 Offset = new Vector2(0.5f, 1.0f);
        public float Delay;
        public float Damage = 70f;
        public Vector2 Knockback = new Vector2(8f, 6f);
        public float Hitstun = 0.5f;
        public HitFlags Flags = HitFlags.Technique | HitFlags.Heavy;
        public DamageType Type = DamageType.Energy;
        public StatusType Status = StatusType.Count;
        public float StatusTime, StatusMag;
        public BurstVis Vis = BurstVis.Shockwave;
        public bool AtTarget;
        public float Shake = 0.35f;
        public Sfx Sound = Sfx.Explosion;

        public BurstAb() { Use = AIUse.Area; RangeMin = 0f; RangeMax = 3f; Pose = FPose.Slam; }

        public override void Execute(Fighter f, float power)
        {
            Vector2 c;
            var t = f.Target;
            if (AtTarget && t != null) c = t.Center; else c = f.Front(Offset.x, Offset.y);
            Action go = () =>
            {
                if (f.Dead) return;
                var h = HitInfo.Make(f, HitSource.Other, Damage * power, Knockback, Hitstun, Flags, Type, c, Color);
                if (Status != StatusType.Count) h = h.WithStatus(Status, StatusTime, StatusMag);
                h.Hitstop = 0.09f;
                f.M.Area(f, c, Radius * Mathf.Lerp(1f, power, 0.4f), h, true);
                VFX.Burst(Vis, c, Radius, Color);
                Audio.Play(Sound, c);
                CameraRig.Shake(Shake);
            };
            if (Delay > 0f) { VFX.Charge(c, Color, Delay); f.M.Schedule(Delay, go); } else go();
        }
    }

    public sealed class SummonAb : Ability
    {
        public MinionDef Minion;
        public int Count = 1;
        public int MaxActive = 2;
        public Vector2 Offset = new Vector2(1.2f, 0f);
        public bool AtTarget;
        public SummonAb() { Use = AIUse.Summon; RangeMin = 0f; RangeMax = 20f; Pose = FPose.HandSign; CastSound = Sfx.Summon; }

        public override bool CanUse(Fighter f) => f.M.CountMinions(f, Minion) < MaxActive;

        public override void Execute(Fighter f, float power)
        {
            for (int i = 0; i < Count; i++)
            {
                if (f.M.CountMinions(f, Minion) >= MaxActive) break;
                Vector2 p;
                var t = f.Target;
                if (AtTarget && t != null) p = new Vector2(t.Pos.x - t.Facing * 1.5f, Minion.Fly ? 4f : 0f);
                else p = f.Front(Offset.x + i * 0.7f, Minion.Fly ? 2.5f : Offset.y);
                var mn = f.M.SpawnMinion(f, Minion, p, power);
                VFX.Summon(p + Vector2.up * 0.6f, Color);
            }
            Audio.Play(Sfx.Summon, f.Center);
        }
    }

    public sealed class BuffAb : Ability
    {
        public StatusType Status = StatusType.PowerUp;
        public float Duration = 8f, Mag = 0.3f;
        public StatusType Status2 = StatusType.Count;
        public float Mag2;
        public bool Allies;           // also applies to teammates
        public float Heal, CeRestore;
        public bool Cleanse;
        public BuffAb() { Use = AIUse.Buff; Pose = FPose.Channel; RangeMin = 0f; RangeMax = 30f; CastTime = 0.3f; CastSound = Sfx.Charge; }

        public override void Execute(Fighter f, float power)
        {
            Apply(f, power);
            if (Allies)
                foreach (var a in f.M.Fighters)
                    if (a != f && !a.Dead && a.Team == f.Team) Apply(a, power);
            VFX.Aura(f.Center, Color, 1.5f);
            Audio.Play(Sfx.Heal, f.Center, 0.7f);
        }

        void Apply(Fighter t, float power)
        {
            if (Status != StatusType.Count) t.AddStatus(Status, Duration, Mag * Mathf.Lerp(1f, power, 0.5f), t);
            if (Status2 != StatusType.Count) t.AddStatus(Status2, Duration, Mag2, t);
            if (Heal > 0f) t.Heal(Heal * power);
            if (CeRestore > 0f) t.GainCe(CeRestore);
            if (Cleanse) t.ClearDebuffs();
        }
    }

    public sealed class ZoneAb : Ability
    {
        public ZoneDef Zone;
        public bool AtTarget = true;
        public ZoneAb() { Use = AIUse.Zone; RangeMin = 0f; RangeMax = 10f; Pose = FPose.Slam; }
        public override void Execute(Fighter f, float power)
        {
            var t = f.Target;
            Vector2 p = AtTarget && t != null ? new Vector2(t.Pos.x, 0f) : (Zone.FollowOwner ? f.Pos : new Vector2(f.Pos.x + f.Facing * 2.5f, 0f));
            f.M.SpawnZone(f, Zone, p, power);
        }
    }

    /// <summary>Stance: if struck during the window, negates the hit and retaliates.</summary>
    public sealed class CounterAb : Ability
    {
        public float Window = 0.6f;
        public float Damage = 90f;
        public Vector2 Knockback = new Vector2(10f, 5f);
        public DamageType Type = DamageType.Slash;
        public bool Teleport;
        public bool TechniqueOnly;    // only counters cursed techniques (simple domain style)
        public CounterAb() { Use = AIUse.Counter; Pose = FPose.Block; CastTime = 0f; RangeMin = 0f; RangeMax = 2.5f; }
        public override void Execute(Fighter f, float power) { }
        public override void OnCastStart(Fighter f)
        {
            f.SetCounter(this, Window);
            VFX.Ring(f.Center, Color, 1.4f, 0.4f, 0.06f);
        }
    }

    /// <summary>Cursed speech style command: affects enemies in front, costs the user HP.</summary>
    public sealed class CommandAb : Ability
    {
        public string Word = "STOP";
        public float Range = 9f;
        public StatusType Status = StatusType.Stun;
        public float StatusTime = 1f, StatusMag;
        public float Damage;
        public Vector2 Knockback;
        public float SelfDamage = 25f;
        public bool Global;
        public CommandAb() { Use = AIUse.Utility; Pose = FPose.Chant; CastTime = 0.25f; RangeMin = 0f; RangeMax = 9f; CastSound = Sfx.None; }

        public override void Execute(Fighter f, float power)
        {
            VFX.WorldText(f.HeadPos + Vector2.up * 0.8f, Word, Color, 1.4f);
            VFX.Burst(BurstVis.Sound, f.Front(0.5f, 1.8f), Range * 0.6f, Color);
            Audio.Play(Sfx.Voice, f.Center);
            CameraRig.Shake(0.25f);
            foreach (var e in f.M.Fighters)
            {
                if (!f.M.IsEnemy(f, e) || e.Dead) continue;
                float dx = (e.Pos.x - f.Pos.x) * f.Facing;
                if (!Global && (dx < -0.5f || Mathf.Abs(e.Pos.x - f.Pos.x) > Range)) continue;
                // stronger targets resist (cursed speech backlash)
                float resist = e.Def.Tier == 1 ? 0.5f : 1f;
                var h = HitInfo.Make(f, HitSource.Other, Damage * power, new Vector2(Knockback.x * f.Facing, Knockback.y), Status == StatusType.Stun ? 0.1f : 0.4f,
                                     HitFlags.Technique | HitFlags.Unblockable | HitFlags.Soul | HitFlags.NoCombo, DamageType.Sound, e.Center, Color);
                if (Status != StatusType.Count) h = h.WithStatus(Status, StatusTime * resist * Mathf.Lerp(1f, power, 0.5f), StatusMag);
                f.M.Hit(e, h);
            }
            f.TakeSelfDamage(SelfDamage);
        }
    }

    /// <summary>Anything bespoke. The lambda runs inside the simulation.</summary>
    public sealed class CustomAb : Ability
    {
        public Action<Fighter, float> Fn;
        public Func<Fighter, bool> Can;
        public Action<Fighter> OnStart;
        public override bool CanUse(Fighter f) => Can == null || Can(f);
        public override void OnCastStart(Fighter f) { OnStart?.Invoke(f); }
        public override void Execute(Fighter f, float power) => Fn?.Invoke(f, power);
    }

    public sealed class DomainAb : Ability
    {
        public DomainDef Domain;
        public DomainAb()
        {
            Use = AIUse.Domain; Cost = 100f; Cooldown = 30f; CastTime = 0.9f; Recovery = 0.2f; Pose = FPose.HandSign;
            Chantable = true; MaxChant = 2; RangeMin = 0f; RangeMax = 40f; Armored = true; CastSound = Sfx.Rumble;
        }
        public override bool IsDomain => true;
        public override bool CanUse(Fighter f) => !f.M.Domains.IsOwnerActive(f) && !f.M.Domains.AllyDomainActive(f) && !f.M.Domains.Clashing;
        public override void OnCastStart(Fighter f) { f.M.Domains.BeginCast(f, this); }
        public override void Execute(Fighter f, float power) { f.M.Domains.FinishCast(f, this, power); }
        public override string Summary() => "Domain Expansion: " + Domain.Name + ". " + Desc;
    }
}
