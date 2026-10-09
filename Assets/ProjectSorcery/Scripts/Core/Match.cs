using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectSorcery
{
    public interface ITickDriver
    {
        /// <summary>Advance the simulation for this real frame.</summary>
        void Update(Match m, float realDt);
        /// <summary>Interpolation factor for rendering between the last two ticks.</summary>
        float Alpha { get; }
        void Dispose();
    }

    /// <summary>
    /// Owns one match: fighters, entities, domains and the mode rules. The simulation runs at a fixed 60 Hz
    /// and is a pure function of the config + seed + per-tick input bits, which makes online lockstep possible.
    /// </summary>
    public sealed class Match : MonoBehaviour
    {
        public static Match I;
        /// <summary>Headless mode (automated tests): no rendering objects are created.</summary>
        public static bool Headless;

        public const float TickDt = 1f / 60f;

        public MatchConfig Config;
        public Rng Rng;
        public float Time, Dt = TickDt;
        public int TickCount;
        public float TimeScale = 1f;
        float slowTimer;
        public float FreezeTimer;
        public bool Paused;           // local pause (offline only)
        public bool Ended;

        public Arena Arena;
        public readonly List<Fighter> Fighters = new List<Fighter>();
        public readonly List<Minion> Minions = new List<Minion>();
        public readonly List<Projectile> Projectiles = new List<Projectile>();
        public readonly List<Beam> Beams = new List<Beam>();
        public readonly List<Zone> Zones = new List<Zone>();
        public DomainSystem Domains;
        public readonly MatchEvents Events = new MatchEvents();
        public ModeController Mode;
        public ITickDriver Driver;

        struct Sched { public float At; public Action A; }
        readonly List<Sched> scheduled = new List<Sched>();
        readonly List<Sched> due = new List<Sched>();
        ushort[] bitsBuffer = new ushort[8];

        ArenaView arenaView;

        // ------------------------------------------------------------------ setup
        public static Match Create(MatchConfig cfg, ITickDriver driver)
        {
            var go = new GameObject("Match");
            var m = go.AddComponent<Match>();
            m.Setup(cfg, driver);
            return m;
        }

        internal void Setup(MatchConfig cfg, ITickDriver driver)
        {
            I = this;
            Config = cfg;
            Driver = driver;
            Rng = new Rng(cfg.Seed);
            Arena = new Arena { Left = -13f, Right = 13f, Ceiling = 11f, Theme = cfg.Arena };
            Domains = new DomainSystem(this);
            Mode = ModeController.Create(this, cfg);
            Arena = Mode.ConfigureArena(Arena);

            if (!Headless) arenaView = new ArenaView(transform, Arenas.Get(Arena.Theme), Arena);

            bool sharedKeyboard = false;
            int kb2 = 0, kb1 = 0;
            foreach (var s in cfg.Slots) { if (s.Control == SlotControl.Local && s.Device == 0) kb1++; if (s.Control == SlotControl.Local && s.Device == 1) kb2++; }
            sharedKeyboard = kb1 > 0 && kb2 > 0;
            InputHub.SetSharedKeyboard(sharedKeyboard);

            int localNum = 1;
            for (int i = 0; i < cfg.Slots.Count; i++)
            {
                var s = cfg.Slots[i];
                if (s.Control == SlotControl.None) continue;
                var def = Roster.Get(s.Character);
                var f = new Fighter();
                f.IsCpu = s.Control == SlotControl.Cpu;
                f.IsDummy = f.IsCpu && s.Diff == Difficulty.Dummy;
                f.IsBoss = Mode.IsBossSlot(i);
                f.Init(this, def, i, s.Team);
                if (f.IsCpu) { f.Brain = new AIBrain(f, s.Diff); f.Tag = f.IsBoss ? "BOSS" : "CPU"; }
                else if (s.Control == SlotControl.Remote || cfg.Online) f.Tag = string.IsNullOrEmpty(s.Name) ? "P" + (i + 1) : s.Name;
                else f.Tag = "P" + (localNum++);
                f.TeamColor = TeamColor(s.Team);
                Mode.PrepareFighter(f);
                Fighters.Add(f);
                if (!Headless) f.Rig = new StickRig(transform, f);
            }
            if (bitsBuffer.Length < cfg.Slots.Count) bitsBuffer = new ushort[cfg.Slots.Count];
            Mode.Begin();
            CameraRig.I?.SnapTo(this);
        }

        public static Color TeamColor(int team)
        {
            switch (team)
            {
                case 0: return new Color(0.32f, 0.78f, 1f);
                case 1: return new Color(1f, 0.33f, 0.42f);
                case 2: return new Color(0.45f, 1f, 0.55f);
                default: return new Color(1f, 0.85f, 0.3f);
            }
        }

        void OnDestroy()
        {
            Driver?.Dispose();
            foreach (var f in Fighters) f.Rig?.Destroy();
            Views.ReleaseAll();
            arenaView?.Destroy();
            if (I == this) I = null;
        }

        // ------------------------------------------------------------------ frame loop
        void Update()
        {
            if (Driver == null) return;
            Driver.Update(this, UnityEngine.Time.unscaledDeltaTime);
        }

        void LateUpdate()
        {
            float alpha = Driver != null ? Driver.Alpha : 1f;
            for (int i = 0; i < Fighters.Count; i++) Fighters[i].Rig?.Render(alpha);
            Views.Sync(this, alpha);
            arenaView?.Update(this);
            VFX.SimSpeed = Paused ? 0f : TimeScale;
        }

        /// <summary>Gather local inputs for one tick (offline / host-side local players).</summary>
        public ushort[] GatherLocalBits()
        {
            for (int i = 0; i < Config.Slots.Count; i++)
            {
                var s = Config.Slots[i];
                ushort b = 0;
                if (s.Control == SlotControl.Local)
                {
                    var d = InputHub.Get(s.Device);
                    if (d != null) b = d.TakeTick();
                }
                bitsBuffer[i] = b;
            }
            return bitsBuffer;
        }

        // ------------------------------------------------------------------ simulation
        public void SimTick(ushort[] slotBits)
        {
            TickCount++;
            if (slowTimer > 0f) { slowTimer -= TickDt; if (slowTimer <= 0f) TimeScale = 1f; }
            Dt = TickDt * TimeScale;
            Time += Dt;

            Mode.Tick(Dt);
            if (Ended) return;

            if (FreezeTimer > 0f)
            {
                // cut-in freeze: only inputs (for domain clash mashing) and the domain system advance
                FreezeTimer -= TickDt;
                for (int i = 0; i < Fighters.Count; i++)
                {
                    var f = Fighters[i];
                    ushort b = f.IsCpu ? f.Brain.ThinkFrozen() : SafeBits(slotBits, f.Slot);
                    f.In.Push(b);
                }
                Domains.Tick(TickDt, true);
                return;
            }

            for (int i = 0; i < Fighters.Count; i++)
            {
                var f = Fighters[i];
                ushort b = f.IsCpu ? f.Brain.Think(Dt) : SafeBits(slotBits, f.Slot);
                if (!Mode.InputEnabled) b = 0;
                f.Tick(Dt, b);
            }
            for (int i = 0; i < Fighters.Count; i++)
                for (int j = i + 1; j < Fighters.Count; j++)
                    Fighters[i].Separate(Fighters[j], Dt);

            for (int i = 0; i < Minions.Count; i++) if (!Minions[i].Dead) Minions[i].Tick(this, Dt);
            for (int i = 0; i < Projectiles.Count; i++) if (!Projectiles[i].Dead) Projectiles[i].Tick(this, Dt);
            ProjectileClashes();
            for (int i = 0; i < Beams.Count; i++) if (!Beams[i].Dead) Beams[i].Tick(this, Dt);
            for (int i = 0; i < Zones.Count; i++) if (!Zones[i].Dead) Zones[i].Tick(this, Dt);

            RunScheduled();
            Domains.Tick(Dt, false);
            Cleanup();
        }

        static ushort SafeBits(ushort[] bits, int slot) => bits != null && slot < bits.Length ? bits[slot] : (ushort)0;

        void RunScheduled()
        {
            if (scheduled.Count == 0) return;
            due.Clear();
            for (int i = 0; i < scheduled.Count; i++)
                if (scheduled[i].At <= Time) due.Add(scheduled[i]);
            if (due.Count == 0) return;
            scheduled.RemoveAll(s => s.At <= Time);
            for (int i = 0; i < due.Count; i++) due[i].A();
        }

        void ProjectileClashes()
        {
            int n = Projectiles.Count;
            for (int i = 0; i < n; i++)
            {
                var a = Projectiles[i];
                if (a.Dead || a.Stuck != null) continue;
                for (int j = i + 1; j < n; j++)
                {
                    var b = Projectiles[j];
                    if (b.Dead || b.Stuck != null || !IsEnemyTeam(a.Team, b.Team)) continue;
                    float r = (a.Size + b.Size) * 0.5f;
                    if ((a.Pos - b.Pos).sqrMagnitude > r * r) continue;
                    if (a.D.Erase && !b.D.Unerasable) { b.Dead = true; VFX.ProjectilePop(b.Pos, b.D.Color, b.Size); continue; }
                    if (b.D.Erase && !a.D.Unerasable) { a.Dead = true; VFX.ProjectilePop(a.Pos, a.D.Color, a.Size); break; }
                    if (a.D.Unerasable || b.D.Unerasable) continue;
                    // collision: the stronger technique survives
                    float sa = a.Strength, sb = b.Strength;
                    Vector2 mid = (a.Pos + b.Pos) * 0.5f;
                    VFX.Burst(BurstVis.Shockwave, mid, 1.2f, Color.Lerp(a.D.Color, b.D.Color, 0.5f));
                    Audio.Play(Sfx.ImpactEnergy, mid, 0.7f);
                    if (sa >= sb * 1.5f) b.Finish(this);
                    else if (sb >= sa * 1.5f) { a.Finish(this); break; }
                    else { a.Finish(this); b.Finish(this); break; }
                }
            }
        }

        void Cleanup()
        {
            for (int i = Projectiles.Count - 1; i >= 0; i--) if (Projectiles[i].Dead) { Views.Release(Projectiles[i]); Projectiles.RemoveAt(i); }
            for (int i = Beams.Count - 1; i >= 0; i--) if (Beams[i].Dead) { Views.Release(Beams[i]); Beams.RemoveAt(i); }
            for (int i = Minions.Count - 1; i >= 0; i--) if (Minions[i].Dead) { Views.Release(Minions[i]); Minions.RemoveAt(i); }
            for (int i = Zones.Count - 1; i >= 0; i--) if (Zones[i].Dead) { Views.Release(Zones[i]); Zones.RemoveAt(i); }
        }

        public void ClearEntities()
        {
            foreach (var p in Projectiles) Views.Release(p);
            foreach (var b in Beams) Views.Release(b);
            foreach (var mn in Minions) Views.Release(mn);
            foreach (var z in Zones) Views.Release(z);
            Projectiles.Clear(); Beams.Clear(); Minions.Clear(); Zones.Clear();
            scheduled.Clear();
            Domains.Reset();
        }

        // ------------------------------------------------------------------ API used by abilities
        public void Schedule(float delay, Action a) => scheduled.Add(new Sched { At = Time + delay, A = a });

        public void SlowMo(float scale, float duration)
        {
            if (scale < TimeScale || slowTimer <= 0f) TimeScale = scale;
            slowTimer = Mathf.Max(slowTimer, duration);
        }

        public void Freeze(float seconds) => FreezeTimer = Mathf.Max(FreezeTimer, seconds);

        public HitResult Hit(Fighter victim, HitInfo h)
        {
            if (victim == null) return HitResult.Miss;
            return victim.ReceiveHit(ref h);
        }

        public Projectile Shoot(Fighter owner, ProjDef d, Vector2 pos, Vector2 dir, float power)
        {
            var p = new Projectile();
            p.Init(this, owner, d, pos, dir, power);
            Projectiles.Add(p);
            Views.Attach(p);
            if (d.Launch != Sfx.None) Audio.Play(d.Launch, pos, 0.6f);
            return p;
        }

        public Beam SpawnBeam(Fighter owner, BeamDef d, Vector2 origin, Vector2 dir, float power)
        {
            var b = new Beam();
            b.Init(owner, d, origin, dir, power);
            Beams.Add(b);
            Views.Attach(b);
            return b;
        }

        public Minion SpawnMinion(Fighter owner, MinionDef d, Vector2 pos, float power)
        {
            var mn = new Minion();
            pos.x = Mathf.Clamp(pos.x, Arena.Left + 0.5f, Arena.Right - 0.5f);
            mn.Init(owner, d, pos, power);
            Minions.Add(mn);
            Views.Attach(mn);
            return mn;
        }

        public Zone SpawnZone(Fighter owner, ZoneDef d, Vector2 pos, float power)
        {
            var z = new Zone();
            z.Init(owner, d, pos, power);
            Zones.Add(z);
            Views.Attach(z);
            return z;
        }

        /// <summary>Adds a CPU fighter mid-match (survival waves, raid summons).</summary>
        public Fighter AddFighter(CharacterDef def, int team, Difficulty diff, float hpMul, Vector2 pos, int facing)
        {
            var f = new Fighter { IsCpu = true, HpMul = hpMul, Tag = "CPU" };
            f.Init(this, def, Fighters.Count, team);
            f.Brain = new AIBrain(f, diff);
            f.TeamColor = TeamColor(team);
            f.ResetForRound(pos, facing);
            Fighters.Add(f);
            if (!Headless) f.Rig = new StickRig(transform, f);
            if (bitsBuffer.Length < Fighters.Count) System.Array.Resize(ref bitsBuffer, Fighters.Count + 4);
            VFX.Summon(f.Center, def.Look.Aura);
            return f;
        }

        public void RemoveFighter(Fighter f)
        {
            KillMinionsOf(f);
            f.Rig?.Destroy();
            Fighters.Remove(f);
        }

        public int CountMinions(Fighter owner, MinionDef d)
        {
            int c = 0;
            for (int i = 0; i < Minions.Count; i++) if (!Minions[i].Dead && Minions[i].Owner == owner && Minions[i].D == d) c++;
            return c;
        }

        public void KillMinionsOf(Fighter owner)
        {
            for (int i = 0; i < Minions.Count; i++) if (Minions[i].Owner == owner) Minions[i].Kill(this, false);
        }

        /// <summary>Hit every enemy of the attacker inside a circle. Knockback.x is outward magnitude.</summary>
        public int Area(Fighter attacker, Vector2 center, float radius, HitInfo template, bool includeMinions)
        {
            int count = 0;
            for (int i = 0; i < Fighters.Count; i++)
            {
                var f = Fighters[i];
                if (f.Dead || attacker == null || !IsEnemy(attacker, f)) continue;
                if (!f.Overlaps(center, radius)) continue;
                var h = template;
                float dx = f.Pos.x - center.x;
                float sign = Mathf.Abs(dx) < 0.2f ? attacker.Facing : Mathf.Sign(dx);
                h.Knockback = new Vector2(Mathf.Abs(template.Knockback.x) * sign, template.Knockback.y);
                h.Point = f.Center;
                Hit(f, h);
                count++;
            }
            if (includeMinions && attacker != null)
            {
                for (int i = 0; i < Minions.Count; i++)
                {
                    var mn = Minions[i];
                    if (mn.Dead || mn.Owner == null || !IsEnemyTeam(attacker.Team, mn.Owner.Team)) continue;
                    float rr = radius + mn.Radius;
                    if ((mn.Pos - center).sqrMagnitude < rr * rr) mn.TakeDamage(template.Damage, attacker, (mn.Pos - center).normalized * 6f);
                }
            }
            return count;
        }

        public bool IsEnemy(Fighter a, Fighter b) => a != null && b != null && a != b && a.Team != b.Team;
        public bool IsEnemyTeam(int a, int b) => a != b;

        public Fighter NearestEnemy(Fighter f)
        {
            Fighter best = null;
            float bd = float.MaxValue;
            for (int i = 0; i < Fighters.Count; i++)
            {
                var o = Fighters[i];
                if (o == f || o.Dead || o.Team == f.Team || o.State == FState.Respawning) continue;
                float d = Mathf.Abs(o.Pos.x - f.Pos.x) + Mathf.Abs(o.Pos.y - f.Pos.y) * 0.5f;
                // focus wounded targets a little
                d -= (1f - o.HpFrac) * 1.5f;
                if (d < bd) { bd = d; best = o; }
            }
            return best;
        }

        public Fighter NearestEnemyTo(Vector2 p, int team)
        {
            Fighter best = null;
            float bd = float.MaxValue;
            for (int i = 0; i < Fighters.Count; i++)
            {
                var o = Fighters[i];
                if (o.Dead || o.Team == team || o.State == FState.Respawning) continue;
                float d = (o.Center - p).sqrMagnitude;
                if (d < bd) { bd = d; best = o; }
            }
            return best;
        }

        public int AliveOnTeam(int team)
        {
            int c = 0;
            for (int i = 0; i < Fighters.Count; i++) if (!Fighters[i].Dead && Fighters[i].Team == team) c++;
            return c;
        }

        public Fighter FirstHuman()
        {
            for (int i = 0; i < Fighters.Count; i++) if (!Fighters[i].IsCpu) return Fighters[i];
            return null;
        }

        /// <summary>Lightweight checksum for desync detection in online play.</summary>
        public uint Checksum()
        {
            uint h = 2166136261u;
            for (int i = 0; i < Fighters.Count; i++)
            {
                var f = Fighters[i];
                h = (h ^ (uint)Mathf.RoundToInt(f.Pos.x * 100f)) * 16777619u;
                h = (h ^ (uint)Mathf.RoundToInt(f.Pos.y * 100f)) * 16777619u;
                h = (h ^ (uint)Mathf.RoundToInt(f.Hp)) * 16777619u;
                h = (h ^ (uint)f.State) * 16777619u;
            }
            h = (h ^ Rng.State) * 16777619u;
            return h;
        }
    }

    /// <summary>Offline driver: fixed-step accumulator fed by local devices.</summary>
    public sealed class LocalDriver : ITickDriver
    {
        float acc;
        public float Alpha => Mathf.Clamp01(acc / Match.TickDt);

        public void Update(Match m, float realDt)
        {
            if (m.Paused || m.Ended)
            {
                acc = 0f;
                for (int i = 0; i < InputHub.Devices.Count; i++) InputHub.Devices[i].ClearLatch();
                return;
            }
            acc += Mathf.Min(realDt, 0.1f);
            int steps = 0;
            while (acc >= Match.TickDt && steps < 5)
            {
                m.SimTick(m.GatherLocalBits());
                acc -= Match.TickDt;
                steps++;
                if (m.Ended) break;
            }
            if (steps == 5) acc = 0f;
        }

        public void Dispose() { }
    }
}
