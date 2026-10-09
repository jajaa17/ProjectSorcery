using System.Collections.Generic;
using UnityEngine;

namespace ProjectSorcery
{
    /// <summary>
    /// Deterministic fighter simulation. Visual state (rig) reads from here but never writes back.
    /// Coordinates: Pos is the point between the feet; the floor is y = 0.
    /// </summary>
    public sealed partial class Fighter
    {
        // ---------- identity ----------
        public CharacterDef Def;
        public Match M;
        public int Slot, Team;
        public string Tag;                 // "P1", "CPU", player name online
        public Color TeamColor;
        public bool IsCpu;
        public AIBrain Brain;
        public bool IsBoss;
        public bool IsDummy;
        public float HpMul = 1f;

        // ---------- physics ----------
        public Vector2 Pos, Vel, PrevPos;
        public int Facing = 1;
        public bool Grounded = true;
        public int AirJumps, AirDashes;
        public float Size => Def.Size * SizeMul;
        public float SizeMul = 1f;

        // ---------- vitals ----------
        public float Hp, MaxHp, Ce, MaxCe, Guard = 100f;
        public bool Dead;
        public int Lives = 1;

        // ---------- state ----------
        public FState State = FState.Idle;
        public float StateTime;
        public InputFrame In;
        public AttackDef CurAttack;
        public int ComboStep, AirStep;
        float lastAttackEnd = -9f;
        const float ComboWindow = 0.45f;   // time after a light ends that the next press still continues the string
        public float Hitstun, Hitstop, Invuln, BurstCd, DashCd, KnockdownTime;
        readonly List<Fighter> hitThisAttack = new List<Fighter>(4);
        readonly List<Minion> minionsHitThisAttack = new List<Minion>(4);
        bool attackConnected;

        // input buffer
        enum Act { None, Light, Heavy, Jump, Dash, S1, S2, S3, Ult }
        Act buffered; float bufferTime; bool bufferedDown, bufferedFwd;
        public int HeavyStep;
        bool pendingStep;

        // ---------- abilities ----------
        public MoveSet Set;                // normal attacks (style + personal flavour)
        public Ability[] Kit;
        public readonly float[] Cd = new float[4];
        public Ability Casting;
        public int CastSlot = -1;
        public int ChantLevel;
        float chantTimer, castClock;
        bool castFired;
        public float CastPower = 1f;
        CounterAb counter; float counterTimer;

        // scripted strike
        StrikeDef strike; float strikeTime, strikePower, strikeHitClock;
        readonly List<Fighter> strikeHits = new List<Fighter>(4);
        int strikeHitCount;

        // ---------- statuses ----------
        public readonly List<Status> Statuses = new List<Status>(8);

        // ---------- combos & black flash ----------
        public int ComboHits; public float ComboTimer, ComboDamage;
        public int Juggle;
        float bfOpen, bfClose, bfReadyAt; bool bfPrimed;
        public float ChargeTime;           // heavy held: charge built so far
        bool chargeFull, chargeReleased;
        public const float MaxCharge = 0.6f;
        public int BfStreak; float bfStreakTimer;
        public int ForcedBlackFlashes;     // next N heavy hits are guaranteed black flashes
        public float StaggerMeter;
        public bool Launched;              // airborne from a real launch (lands in a knockdown)

        // ---------- misc ----------
        public float[] Adapt = new float[(int)DamageType.Count];
        public int InfinityHitsTaken;      // for wheel guardian adaptation to boundless defense
        public bool LuckUsed;
        public Fighter LastAttacker;
        public float DamageTakenWindow;    // for domain breaking
        public float MovedDistance;        // for moon-palace domain
        public float RespawnTimer;

        // ---------- stats ----------
        public float StatDamage, StatTaken;
        public int StatBlackFlashes, StatDomainWins, StatKOs, StatMaxCombo;

        // ---------- visuals ----------
        public StickRig Rig;
        public float FlashTimer;           // white hit flash
        public float HitImpulse;           // visual-only: recoil strength of the last hit (read by the rig)
        public float GuardImpulse;         // visual-only: blocked-hit flinch
        public int HitDir;                 // visual-only: direction the last hit pushed
        public string LastChant;

        public bool IsHuman => !IsCpu;
        public Vector2 Center => Pos + new Vector2(0f, 1.05f * Size);
        public Vector2 HeadPos => Pos + new Vector2(0f, 1.95f * Size);
        public Vector2 Front(float x, float y) => Pos + new Vector2(x * Facing, y) * Size;
        public bool Busy => State == FState.Attack || State == FState.Cast || State == FState.Dash || strike != null;
        public bool Vulnerable => State == FState.Hitstun || State == FState.Knockdown;
        public bool CanAct => !Dead && State != FState.Hitstun && State != FState.Knockdown && State != FState.Locked && !HardCC;
        public bool HardCC => Has(StatusType.Stun) || Has(StatusType.Paralysis) || Has(StatusType.Freeze);
        public bool InStrike => strike != null;
        public StrikeDef CurStrike => strike;
        public float HpFrac => MaxHp <= 0 ? 0 : Hp / MaxHp;
        public float CeFrac => MaxCe <= 0 ? 0 : Ce / MaxCe;
        public bool IsRestricted => Def.NoCE;
        public bool DashCdReady => DashCd <= 0f;

        Fighter cachedTarget; int targetTick = -1;
        public Fighter Target
        {
            get
            {
                if (targetTick == M.TickCount && (cachedTarget == null || !cachedTarget.Dead)) return cachedTarget;
                targetTick = M.TickCount;
                cachedTarget = M.NearestEnemy(this);
                return cachedTarget;
            }
        }

        public void Init(Match m, CharacterDef def, int slot, int team)
        {
            M = m; Def = def; Slot = slot; Team = team;
            Set = MoveLib.For(def);
            Kit = def.Kit != null ? def.Kit() : new Ability[4];
            if (Kit.Length < 4) System.Array.Resize(ref Kit, 4);
            ResetForRound(Vector2.zero, 1);
        }

        public void ResetForRound(Vector2 pos, int facing)
        {
            MaxHp = Def.Hp * HpMul * Balance.GlobalHp;
            Hp = MaxHp;
            MaxCe = Def.NoCE ? 0f : Def.Ce;
            Ce = MaxCe * 0.6f;
            Guard = 100f;
            Pos = PrevPos = pos; Vel = Vector2.zero; Facing = facing;
            Grounded = true; Dead = false;
            State = FState.Idle; StateTime = 0f;
            Statuses.Clear();
            for (int i = 0; i < 4; i++) Cd[i] = 0f;
            Casting = null; strike = null; counter = null;
            Hitstun = Hitstop = Invuln = 0f; BurstCd = 0f;
            ComboHits = 0; Juggle = 0; BfStreak = 0; StaggerMeter = 0; ForcedBlackFlashes = 0; bfReadyAt = 0f; ChargeTime = 0f;
            for (int i = 0; i < Adapt.Length; i++) Adapt[i] = 0f;
            InfinityHitsTaken = 0; LuckUsed = false; DamageTakenWindow = 0f;
            buffered = Act.None;
            Brain?.Reset();
        }

        // =====================================================================
        //  TICK
        // =====================================================================
        public void Tick(float dt, ushort bits)
        {
            PrevPos = Pos;
            if (Has(StatusType.Confused))
            {
                // swap left/right
                bool l = (bits & IB.Left) != 0, r = (bits & IB.Right) != 0;
                bits = (ushort)(bits & ~(IB.Left | IB.Right));
                if (l) bits |= IB.Right; if (r) bits |= IB.Left;
            }
            In.Push(bits);

            if (FlashTimer > 0f) FlashTimer -= dt;

            if (State == FState.Respawning)
            {
                RespawnTimer -= dt;
                return;
            }

            TickStatuses(dt);
            TickResources(dt);

            if (Dead)
            {
                Physics(dt, true);
                return;
            }

            if (State == FState.Locked) { StateTime += dt; return; }

            if (Hitstop > 0f)
            {
                Hitstop -= dt;
                BufferInput(false);   // presses during the impact freeze still count (responsive combos)
                return;
            }

            StateTime += dt;
            if (Invuln > 0f) Invuln -= dt;
            if (BurstCd > 0f) BurstCd -= dt;
            if (DashCd > 0f) DashCd -= dt;
            if (counterTimer > 0f) { counterTimer -= dt; if (counterTimer <= 0f) counter = null; }
            if (ComboTimer > 0f) { ComboTimer -= dt; if (ComboTimer <= 0f) { ComboHits = 0; ComboDamage = 0f; } }
            if (bfStreakTimer > 0f) { bfStreakTimer -= dt; if (bfStreakTimer <= 0f) BfStreak = 0; }
            if (DamageTakenWindow > 0f) DamageTakenWindow = Mathf.Max(0f, DamageTakenWindow - MaxHp * 0.03f * dt);

            BufferInput(true);

            if (HardCC && State != FState.Hitstun && State != FState.Knockdown)
            {
                // frozen in place: stunned, paralysed or frozen
                CancelCast();
                strike = null;
                if (State != FState.Air) SetState(FState.Idle);
                Vel.x = Mathf.MoveTowards(Vel.x, 0f, 40f * dt);
                Physics(dt, false);
                return;
            }

            switch (State)
            {
                case FState.Idle:
                case FState.Run: TickGround(dt); break;
                case FState.Air: TickAir(dt); break;
                case FState.Attack: TickAttack(dt); break;
                case FState.Cast: TickCast(dt); break;
                case FState.Block: TickBlock(dt); break;
                case FState.Hitstun: TickHitstun(dt); break;
                case FState.Knockdown: TickKnockdown(dt); break;
                case FState.GetUp: if (StateTime > 0.32f) SetState(FState.Idle); break;
                case FState.Dash: TickDash(dt); break;
                case FState.RCT: TickRct(dt); break;
                case FState.SimpleDomain: TickSimpleDomain(dt); break;
                case FState.Victory: Vel.x = 0; break;
            }

            if (strike != null) TickStrike(dt);
            Physics(dt, false);
        }

        void SetState(FState s)
        {
            if (State == s) return;
            if (State == FState.Cast && s != FState.Cast) { /* cast ended */ }
            State = s;
            StateTime = 0f;
        }

        // ---------------------------------------------------------------- buffer
        void BufferInput(bool decay)
        {
            Act a = Act.None;
            if (In.Pressed(IB.Ult)) a = Act.Ult;
            else if (In.Pressed(IB.S1)) a = Act.S1;
            else if (In.Pressed(IB.S2)) a = Act.S2;
            else if (In.Pressed(IB.S3)) a = Act.S3;
            else if (In.Pressed(IB.Heavy)) a = Act.Heavy;
            else if (In.Pressed(IB.Light)) a = Act.Light;
            else if (In.Pressed(IB.Dash)) a = Act.Dash;
            else if (In.Pressed(IB.Jump)) a = Act.Jump;
            if (a != Act.None)
            {
                buffered = a; bufferTime = 0.16f; bufferedDown = In.Down; bufferedFwd = In.X * TowardTarget > 0.5f;
                if (a == Act.Heavy && M.Time >= bfOpen && M.Time <= bfClose) bfPrimed = true;
            }
            else if (bufferTime > 0f && decay)
            {
                bufferTime -= M.Dt;
                if (bufferTime <= 0f) buffered = Act.None;
            }
        }

        /// <summary>+1/-1 toward the nearest enemy (falls back to facing): what "forward" means for command inputs.</summary>
        int TowardTarget { get { var t = Target; return t != null && Mathf.Abs(t.Pos.x - Pos.x) > 0.05f ? (t.Pos.x > Pos.x ? 1 : -1) : Facing; } }

        Act Consume()
        {
            var a = buffered;
            buffered = Act.None; bufferTime = 0f;
            return a;
        }

        /// <summary>Try to start whatever action is buffered. Returns true if something started.</summary>
        bool TryBuffered(bool grounded)
        {
            if (buffered == Act.None) return false;
            switch (buffered)
            {
                case Act.Light: Consume(); StartLight(grounded); return true;
                case Act.Heavy: Consume(); StartHeavy(grounded); return true;
                case Act.Jump:
                    if (grounded) { Consume(); DoJump(false); return true; }
                    if (AirJumps > 0) { Consume(); DoJump(true); return true; }
                    Consume(); return false;
                case Act.Dash:
                    if (DashCd <= 0f && (grounded || AirDashes > 0)) { Consume(); StartDash(); return true; }
                    return false;
                case Act.S1: Consume(); return TryCast(0);
                case Act.S2: Consume(); return TryCast(1);
                case Act.S3: Consume(); return TryCast(2);
                case Act.Ult: Consume(); return TryCast(3);
            }
            return false;
        }

        // ---------------------------------------------------------------- ground
        float MoveSpeed
        {
            get
            {
                float s = 7.2f * Def.Speed;
                if (Has(StatusType.Haste)) s *= 1f + StatusMag(StatusType.Haste);
                if (Has(StatusType.Slow)) s *= 1f - Mathf.Clamp(StatusMag(StatusType.Slow), 0f, 0.8f);
                if (Has(StatusType.Sink)) s *= 0.5f;
                if (Has(StatusType.Gravity)) s *= 0.3f;
                if (Has(StatusType.Bind)) s = 0f;
                return s;
            }
        }

        bool CanJump => !Has(StatusType.Sink) && !Has(StatusType.Gravity) && !Has(StatusType.Bind);

        void FaceTarget()
        {
            var t = Target;
            if (t != null && Mathf.Abs(t.Pos.x - Pos.x) > 0.15f) Facing = t.Pos.x > Pos.x ? 1 : -1;
        }

        void TickGround(float dt)
        {
            AirJumps = 1; AirDashes = 1;
            // RCT / simple domain channel: hold Down + Block
            if (In.Held(IB.Block))
            {
                if (In.Down)
                {
                    if (M.Domains.EnemyDomainActiveFor(this) && !IsRestricted) { SetState(FState.SimpleDomain); return; }
                    if (Def.Has(Passive.RCT) && Hp < MaxHp && Ce > 5f) { SetState(FState.RCT); return; }
                }
                SetState(FState.Block);
                return;
            }
            if (TryBuffered(true)) return;

            float x = In.X;
            float target = x * MoveSpeed;
            Vel.x = Mathf.MoveTowards(Vel.x, target, (Mathf.Abs(target) > Mathf.Abs(Vel.x) ? 90f : 60f) * dt);
            if (x != 0f) { Facing = x > 0 ? 1 : -1; if (State != FState.Run) SetState(FState.Run); }
            else { FaceTarget(); if (State != FState.Idle) SetState(FState.Idle); }
        }

        void DoJump(bool air)
        {
            if (!CanJump) return;
            float v = (air ? 13.2f : 15.5f) * Mathf.Sqrt(Def.Jump);
            Vel.y = v;
            if (air) { AirJumps--; VFX.Ring(Pos + Vector2.up * 0.2f, Def.Look.Aura, 0.9f, 0.25f, 0.05f); }
            else VFX.Dust(Pos, 6);
            if (In.X != 0f) Vel.x = In.X * MoveSpeed;
            Grounded = false;
            SetState(FState.Air);
            Audio.Play(Sfx.Jump, Pos, 0.5f, air ? 1.2f : 1f);
        }

        // ---------------------------------------------------------------- air
        void TickAir(float dt)
        {
            if (TryBuffered(false)) return;
            float x = In.X;
            Vel.x = Mathf.MoveTowards(Vel.x, x * MoveSpeed * 0.95f, 34f * dt);
            if (x != 0f) Facing = x > 0 ? 1 : -1;
            if (Def.Has(Passive.Flight) && In.Held(IB.Jump) && Vel.y < 0f) Vel.y = Mathf.Max(Vel.y, -2.5f);
        }

        // ---------------------------------------------------------------- attacks
        void StartLight(bool grounded)
        {
            AttackDef a;
            if (!grounded)
            {
                a = Set.Air[Mathf.Clamp(AirStep, 0, Set.Air.Length - 1)];
                AirStep = (AirStep + 1) % Set.Air.Length;
            }
            else if (bufferedDown || In.Down) { a = Set.Sweep; ComboStep = 0; }
            else
            {
                // the string keeps going as long as presses keep coming (hit or whiff); a pause resets it
                if (State != FState.Attack && M.Time - lastAttackEnd > ComboWindow) ComboStep = 0;
                a = Set.Light[Mathf.Clamp(ComboStep, 0, Set.Light.Length - 1)];
                ComboStep = (ComboStep + 1) % Set.Light.Length;
            }
            StartAttack(a);
        }

        void StartHeavy(bool grounded)
        {
            AttackDef a;
            bool down = bufferedDown || In.Down;
            bool chaining = (State == FState.Attack && HeavyStep > 0 && CurAttack != null && System.Array.IndexOf(Set.HeavyChain, CurAttack) >= 0)
                            || (State != FState.Attack && HeavyStep > 0 && M.Time - lastAttackEnd <= ComboWindow);
            if (!grounded) a = down ? Set.Dive : Set.AirHeavy;                       // aerial move list
            else if (down) a = Set.Launcher;                                         // Down + Heavy
            else if (bufferedFwd && !chaining) a = Set.Dash;                         // Forward + Heavy: lunging strike
            else
            {
                // Heavy -> Heavy -> Heavy: each swing's momentum winds the body into a bigger, spinning follow-up
                if (!chaining) HeavyStep = 0;
                a = Set.HeavyChain[Mathf.Clamp(HeavyStep, 0, Set.HeavyChain.Length - 1)];
                HeavyStep = (HeavyStep + 1) % Set.HeavyChain.Length;
            }
            ComboStep = 0;
            StartAttack(a);
        }

        void StartAttack(AttackDef a)
        {
            CurAttack = a;
            ChargeTime = 0f; chargeFull = false; chargeReleased = false;
            hitThisAttack.Clear();
            minionsHitThisAttack.Clear();
            attackConnected = false;
            SetState(FState.Attack);
            FaceTarget();
            float lungeMul = Def.Speed;
            // ground strikes step in ON the strike: a small shuffle now, the real drive late in the wind-up
            pendingStep = !a.Air && !a.DashStrike && Grounded && a.Lunge.x != 0f;
            if (a.Lunge.x != 0f) Vel.x = a.Lunge.x * Facing * lungeMul * (pendingStep ? 0.3f : 1f);
            if (a.Lunge.y != 0f) Vel.y = a.Lunge.y;
            if (!a.Air && Grounded && a.Lunge.x == 0f) Vel.x *= 0.3f;
            Audio.Play((a.Flags & HitFlags.Heavy) != 0 ? Sfx.Whoosh : Sfx.WhooshLight, Center, 0.45f, 0.9f + 0.05f * ComboStep);
        }

        void TickAttack(float dt)
        {
            var a = CurAttack;
            if (a == null) { SetState(Grounded ? FState.Idle : FState.Air); return; }
            float t = StateTime;
            if (pendingStep && t >= a.Startup * 0.62f && ChargeTime <= 0f) { pendingStep = false; Vel.x = a.Lunge.x * Facing * Def.Speed; }
            if (Grounded && !a.Air && !pendingStep) Vel.x = Mathf.MoveTowards(Vel.x, 0f, 28f * dt);
            // air heavy: hang in the air through the wind-up, then plunge with the strike
            if (a.Air && (a.Flags & HitFlags.Heavy) != 0 && (a.Flags & HitFlags.Spike) == 0 && !Grounded)
            {
                if (t < a.Startup) { Vel.y = Mathf.MoveTowards(Vel.y, 0.8f, 90f * dt); Vel.x *= 1f - 4f * dt; }
                else if (t - dt < a.Startup) Vel.y = -13f;
            }

            // hold Heavy to charge: the wind-up freezes, cursed energy gathers; a full charge bursts out as an aura blast
            if (a.Chargeable && !chargeReleased && t >= a.Startup * 0.8f)
            {
                bool canCharge = IsRestricted || Ce >= 2f;
                if (In.Held(IB.Heavy) && ChargeTime < MaxCharge && canCharge)
                {
                    ChargeTime += dt;
                    StateTime = a.Startup * 0.8f;
                    if (!IsRestricted) Ce = Mathf.Max(0f, Ce - 22f * dt);
                    if (ChargeTime >= MaxCharge && !chargeFull)
                    {
                        chargeFull = true;
                        VFX.Ring(Center, Def.Look.Aura, 1.6f * Size, 0.25f, 0.08f);
                        Audio.Play(Sfx.Charge, Center, 0.8f, 1.3f);
                    }
                    return;
                }
                chargeReleased = true;
            }
            if (t >= a.Startup && t < a.Startup + a.Active)
            {
                if (chargeFull && t - dt < a.Startup) AuraBlast(a);
                MeleeCheck(a);
            }

            bool recovering = t >= a.Startup + a.Active;
            // whiffed lights still flow into the next hit of the string, just a little later than on hit
            if (recovering && !attackConnected && buffered == Act.Light && !a.Air && (a.Flags & HitFlags.Light) != 0
                && t >= a.Startup + a.Active + a.Recovery * 0.55f)
            {
                if (TryBuffered(Grounded)) return;
            }
            // cancel windows: after a connected hit, chain into anything (never during the active frames)
            if (recovering && attackConnected && buffered != Act.None)
            {
                bool launchCancel = (a.Flags & HitFlags.Launch) != 0 && buffered == Act.Jump;
                if (buffered == Act.Light || buffered == Act.Heavy || buffered >= Act.S1 || buffered == Act.Dash || launchCancel)
                {
                    if (launchCancel) { Consume(); Grounded = true; DoJump(false); Vel.y *= 1.05f; return; }
                    if (TryBuffered(Grounded)) return;
                }
            }
            if (t >= a.Total * Mathf.Lerp(1f, 0.85f, Def.Speed - 1f))
            {
                CurAttack = null;
                lastAttackEnd = M.Time;
                SetState(Grounded ? FState.Idle : FState.Air);
                TryBuffered(Grounded);
            }
            if (a.Air && Grounded && t > a.Startup) { CurAttack = null; SetState(FState.Idle); Land(); }
        }

        /// <summary>Full-charge heavy: a short-range shockwave of raw cursed energy in front of the fist.</summary>
        void AuraBlast(AttackDef a)
        {
            Vector2 c = AttackCenter(a) + new Vector2(0.6f * Facing, 0f) * Size;
            var h = HitInfo.Make(this, HitSource.Other, 30f * PowerNoBase(), new Vector2(9f * Facing, 4f), 0.5f, HitFlags.Heavy | HitFlags.GuardBreak | HitFlags.NoCombo, IsRestricted ? DamageType.Blunt : DamageType.Energy, c, Def.Look.Aura);
            h.Hitstop = 0.1f;
            M.Area(this, c, 1.7f * Size, h, true);
            VFX.Burst(BurstVis.Shockwave, c, 1.8f * Size, Def.Look.Aura);
            CameraRig.Shake(0.35f);
            Audio.Play(Sfx.ImpactEnergy, c, 1f, 0.8f);
        }

        float ChargeMul => 1f + 0.7f * Mathf.Clamp01(ChargeTime / MaxCharge);

        Vector2 AttackCenter(AttackDef a)
        {
            float reach = Def.Reach ? 0.45f : Def.Bladed ? 0.25f : 0f;
            return Pos + new Vector2((a.Offset.x + reach) * Facing, a.Offset.y) * Size;
        }

        float AttackRadius(AttackDef a) => (a.Radius + (Def.Reach ? 0.2f : Def.Bladed ? 0.12f : 0f)) * Size;

        void MeleeCheck(AttackDef a)
        {
            Vector2 c = AttackCenter(a);
            float r = AttackRadius(a);
            var list = M.Fighters;
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e == this || e.Dead || !M.IsEnemy(this, e) || hitThisAttack.Contains(e)) continue;
                if (!e.Overlaps(c, r)) continue;
                hitThisAttack.Add(e);
                DoMeleeHit(e, a, c);
            }
            var mins = M.Minions;
            for (int i = 0; i < mins.Count; i++)
            {
                var mn = mins[i];
                if (mn.Dead || mn.Owner == null || !M.IsEnemyTeam(Team, mn.Owner.Team) || minionsHitThisAttack.Contains(mn)) continue;
                if ((mn.Pos - c).sqrMagnitude > (r + mn.Radius) * (r + mn.Radius)) continue;
                minionsHitThisAttack.Add(mn);
                mn.TakeDamage(a.Damage * Power, this, new Vector2(a.Knockback.x * Facing, a.Knockback.y));
                attackConnected = true;
                Hitstop = Mathf.Max(Hitstop, 0.04f);
                VFX.HitSpark(c, Facing, Def.Look.Aura, false);
                Audio.Play(Sfx.Punch, c, 0.6f);
            }
        }

        void DoMeleeHit(Fighter e, AttackDef a, Vector2 point)
        {
            bool heavy = (a.Flags & HitFlags.Heavy) != 0;
            var type = Def.Bladed ? DamageType.Slash : a.Type;
            float dmg = a.Damage * Power * (a.Chargeable ? ChargeMul : 1f);
            var h = HitInfo.Make(this, HitSource.Melee, dmg, new Vector2(a.Knockback.x * Facing, a.Knockback.y), a.Hitstun, a.Flags, type, point, Def.Look.Aura);
            h.Hitstop = a.Hitstop * (a.Chargeable ? Mathf.Lerp(1f, 1.5f, ChargeTime / MaxCharge) : 1f);
            h.CeGain = a.CeGain;
            if (chargeFull) { h.Flags |= HitFlags.GuardBreak; h.Knockback *= 1.3f; }
            if (Has(StatusType.Executioner)) { h.Damage *= 1f + StatusMag(StatusType.Executioner); h.Flags |= HitFlags.PierceInfinity | HitFlags.Unblockable; h.Type = DamageType.Slash; }
            if (Has(StatusType.Amplification)) h.Flags |= HitFlags.PierceInfinity;
            if (M.Domains.OwnerEffect(this) == SureHit.SoulStrike) h.Flags |= HitFlags.PierceInfinity | HitFlags.Soul;
            if (Def.Has(Passive.RotBlood)) h = h.WithStatus(StatusType.Rot, 3f, 6f);

            // ---- BLACK FLASH ----
            bool blackFlash = false;
            if (heavy && !IsRestricted && !Has(StatusType.Sealed))
            {
                // black flash is rare: the rhythm must be hit, and the spark needs time to come back
                float chance = (Def.Has(Passive.BlackFlashAffinity) ? 0.03f : 0f) + (Has(StatusType.Zone) ? 0.04f : 0f);
                bool ready = M.Time >= bfReadyAt;
                if (ForcedBlackFlashes > 0 || (ready && (bfPrimed || M.Rng.Chance(chance)))) blackFlash = true;
                if (blackFlash && ForcedBlackFlashes > 0) ForcedBlackFlashes--;
            }
            bfPrimed = false;
            if (blackFlash)
            {
                h.Damage *= 2.5f;
                h.Flags |= HitFlags.BlackFlash | HitFlags.GuardBreak | HitFlags.Unblockable;
                h.Knockback = new Vector2(16f * Facing, 7f);
                h.Hitstun = 0.9f;
                h.Hitstop = 0.24f;
                h.Type = DamageType.Energy;
            }

            var res = M.Hit(e, h);
            if (res == HitResult.Hit || res == HitResult.Blocked || res == HitResult.Nullified)
            {
                attackConnected = true;
                if ((h.Flags & HitFlags.NoHitstop) == 0) Hitstop = Mathf.Max(Hitstop, h.Hitstop * (res == HitResult.Blocked ? 0.7f : 1f));
                Vel.x *= 0.25f;   // the blow lands: momentum dies on contact instead of sliding through the target
                // corner pushback: when the defender is pinned on a wall, the attacker is the one pushed out
                float wallX = Facing > 0 ? M.Arena.Right : M.Arena.Left;
                if (Mathf.Abs(wallX - e.Pos.x) < 1.1f && Grounded)
                    Vel.x = -Facing * Mathf.Max(4.5f, Mathf.Abs(h.Knockback.x) * 0.9f);
            }
            if (res == HitResult.Hit && !blackFlash)
            {
                // weight: heavier blows punch the camera toward the impact
                float impact = (a.Clip != null ? a.Clip.Impact : 1f) * (heavy ? 1f : 0.45f) * (a.Chargeable ? ChargeMul : 1f);
                if (heavy || impact > 0.5f) { CameraRig.Shake(0.12f + 0.14f * impact); CameraRig.PunchAt(0.1f + 0.1f * impact, point); }
                if (chargeFull) M.SlowMo(0.35f, 0.18f);
            }
            if (res == HitResult.Hit)
            {
                if ((a.Flags & HitFlags.Light) != 0)
                {
                    // open the black flash window: the heavy must land in this rhythm after the light connects
                    if (M.Time >= bfReadyAt)
                    {
                        float window = 0.08f + Def.BlackFlashBonus + (Def.Has(Passive.BlackFlashAffinity) ? 0.04f : 0f) + (Has(StatusType.Zone) ? 0.03f : 0f);
                        bfOpen = M.Time + h.Hitstop + 0.03f;
                        bfClose = bfOpen + window;
                    }
                }
                if (blackFlash) OnBlackFlash(e, point);
            }
        }

        public bool BlackFlashWindowOpen => M != null && M.Time >= bfOpen && M.Time <= bfClose;

        void OnBlackFlash(Fighter e, Vector2 point)
        {
            BfStreak++;
            bfStreakTimer = 7f;
            StatBlackFlashes++;
            bfReadyAt = M.Time + (Has(StatusType.Zone) ? 1.5f : 5f);
            AddStatus(StatusType.Zone, 6f, 1f, this);
            GainCe(MaxCe * 0.3f);
            M.SlowMo(0.12f, 0.4f);
            e.Hitstop = Mathf.Max(e.Hitstop, 0.24f);
            Hitstop = Mathf.Max(Hitstop, 0.24f);
            VFX.BlackFlash(point, Facing, BfStreak);
            Audio.BlackFlash();
            M.Events.BlackFlash(this, e, BfStreak);
        }

        public bool Overlaps(Vector2 c, float r)
        {
            if (State == FState.Knockdown || State == FState.Dead)
            {
                Vector2 b = Pos + new Vector2(0f, 0.35f);
                float rr = 0.75f * Size + r;
                return (b - c).sqrMagnitude < rr * rr;
            }
            Vector2 body = Pos + new Vector2(0f, 1.0f * Size);
            float br = 0.52f * Size + r;
            if ((body - c).sqrMagnitude < br * br) return true;
            Vector2 head = HeadPos;
            float hr = 0.34f * Size + r;
            return (head - c).sqrMagnitude < hr * hr;
        }

        /// <summary>Distance from a segment to this fighter's body (for beams).</summary>
        public bool OverlapsSegment(Vector2 a, Vector2 b, float r)
        {
            Vector2 c = State == FState.Knockdown ? Pos + new Vector2(0, 0.35f) : Center;
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(c - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
            Vector2 p = a + ab * t;
            float rr = r + 0.6f * Size;
            return (c - p).sqrMagnitude < rr * rr;
        }

        // ---------------------------------------------------------------- block
        void TickBlock(float dt)
        {
            FaceTarget();
            Vel.x = Mathf.MoveTowards(Vel.x, 0f, 40f * dt);
            if (!In.Held(IB.Block)) { SetState(FState.Idle); return; }
            if (In.Down && StateTime > 0.05f)
            {
                if (M.Domains.EnemyDomainActiveFor(this) && !IsRestricted) { SetState(FState.SimpleDomain); return; }
                if (Def.Has(Passive.RCT) && Hp < MaxHp && Ce > 5f) { SetState(FState.RCT); return; }
            }
            if (buffered == Act.Dash && DashCd <= 0f) { Consume(); StartDash(); }
        }

        // ---------------------------------------------------------------- dash
        void StartDash()
        {
            int dir = In.X != 0f ? (In.X > 0 ? 1 : -1) : Facing;
            if (!Grounded) AirDashes--;
            SetState(FState.Dash);
            Vel = new Vector2(dir * 17.5f * Mathf.Lerp(1f, Def.Speed, 0.6f), Grounded ? 0f : 1.5f);
            Invuln = Mathf.Max(Invuln, 0.13f);
            DashCd = 0.42f;
            VFX.Afterimage(this, 0.25f);
            VFX.Dust(Pos, 4);
            Audio.Play(Sfx.Dash, Pos, 0.5f);
        }

        void TickDash(float dt)
        {
            if (StateTime % 0.05f < dt) VFX.Afterimage(this, 0.2f);
            if (StateTime > 0.19f)
            {
                Vel.x *= 0.35f;
                SetState(Grounded ? FState.Idle : FState.Air);
            }
            else if ((buffered == Act.Light || buffered == Act.Heavy) && Grounded && StateTime > 0.04f && Vel.x * Facing > 0f)
            {
                // attack out of a forward dash: the dash strike carries all that momentum
                Consume();
                ComboStep = 0;
                StartAttack(Set.Dash);
            }
            else if (buffered == Act.Light || buffered == Act.Heavy || buffered >= Act.S1)
            {
                if (StateTime > 0.08f) { Vel.x *= 0.6f; SetState(Grounded ? FState.Idle : FState.Air); TryBuffered(Grounded); }
            }
        }

        // ---------------------------------------------------------------- hitstun
        void TickHitstun(float dt)
        {
            Hitstun -= dt;
            // burst escape: Block + Dash while being comboed
            if (In.Held(IB.Block) && In.Pressed(IB.Dash) && BurstCd <= 0f && (IsRestricted || Ce >= 30f) && Juggle >= 2)
            {
                Burst();
                return;
            }
            if (Grounded && Vel.y <= 0f)
            {
                if (Hitstun <= 0f)
                {
                    Juggle = 0;
                    SetState(FState.Idle);
                }
                else Vel.x = Mathf.MoveTowards(Vel.x, 0f, 22f * dt);
            }
            else if (Hitstun <= -0.2f && !Grounded)
            {
                // air recovery after long juggles
                Juggle = 0;
                SetState(FState.Air);
            }
        }

        void Burst()
        {
            if (!IsRestricted) Ce -= 30f;
            BurstCd = 10f;
            Juggle = 0; Hitstun = 0f;
            Invuln = 0.35f;
            Vel = new Vector2(0f, 6f);
            SetState(FState.Air);
            var h = HitInfo.Make(this, HitSource.Other, 10f, new Vector2(9f, 5f), 0.35f, HitFlags.Unblockable | HitFlags.NoCombo, DamageType.Energy, Center, Def.Look.Aura);
            M.Area(this, Center, 2.4f, h, true);
            VFX.Burst(BurstVis.Shockwave, Center, 2.4f, Def.Look.Aura);
            VFX.WorldText(HeadPos + Vector2.up * 0.6f, "BURST", Def.Look.Aura, 0.9f);
            Audio.Play(Sfx.ImpactEnergy, Center, 0.9f, 0.8f);
            CameraRig.Shake(0.25f);
        }

        void TickKnockdown(float dt)
        {
            Vel.x = Mathf.MoveTowards(Vel.x, 0f, 20f * dt);
            if (StateTime > KnockdownTime)
            {
                Juggle = 0;
                SetState(FState.GetUp);
                Invuln = 0.45f;
            }
        }

        void Land()
        {
            VFX.Dust(Pos, 5);
            Audio.Play(Sfx.Land, Pos, 0.35f);
        }

        // ---------------------------------------------------------------- RCT / simple domain
        void TickRct(float dt)
        {
            Vel.x = Mathf.MoveTowards(Vel.x, 0f, 40f * dt);
            if (!In.Held(IB.Block) || !In.Down || Ce <= 0f || Hp >= MaxHp) { SetState(FState.Idle); return; }
            float rate = Def.RctRate * (Def.Has(Passive.SixEyes) ? 1.3f : 1f);
            float cost = rate * 0.32f * dt;
            if (Has(StatusType.Jackpot)) cost = 0f;
            Ce = Mathf.Max(0f, Ce - cost);
            Heal(rate * dt, false);
            // RCT repairs the burnt-out brain faster
            ReduceStatus(StatusType.Burnout, dt);
            if (StateTime % 0.12f < dt) VFX.Heal(Center, Def.Look.Aura);
        }

        void TickSimpleDomain(float dt)
        {
            if (!In.Held(IB.Block) || !In.Down || Ce <= 0f || !M.Domains.EnemyDomainActiveFor(this)) { SetState(FState.Idle); return; }
            bool master = Def.Has(Passive.SimpleDomainMaster);
            Ce = Mathf.Max(0f, Ce - (master ? 4f : 9f) * dt);
            if (master) Vel.x = In.X * MoveSpeed * 0.4f;
            else Vel.x = Mathf.MoveTowards(Vel.x, 0f, 40f * dt);
            if (StateTime % 0.25f < dt) VFX.Ring(Pos + Vector2.up * 0.05f, new Color(0.7f, 0.9f, 1f), 1.6f * Size, 0.35f, 0.05f);
        }

        // ---------------------------------------------------------------- casting
        public float CostOf(Ability ab)
        {
            if (ab == null) return 0f;
            if (ab.Physical || IsRestricted) return 0f;
            if (Has(StatusType.Jackpot)) return 0f;
            float c = ab.Cost;
            if (Def.Has(Passive.SixEyes) && !ab.IsDomain) c *= 0.6f;
            if (Def.Has(Passive.Restricted)) c *= 0.75f;
            return c;
        }

        public bool CanCast(int slot)
        {
            var ab = Kit[slot];
            if (ab == null || Cd[slot] > 0f || !CanAct) return false;
            if (!ab.Physical && !IsRestricted)
            {
                if (Has(StatusType.Sealed) || Has(StatusType.Amplification)) return false;
                if (Has(StatusType.Burnout)) return false;
            }
            if (Ce < CostOf(ab)) return false;
            if (!Grounded && !ab.UsableInAir) return false;
            return ab.CanUse(this);
        }

        public bool TryCast(int slot)
        {
            if (!CanCast(slot))
            {
                var ab0 = Kit[slot];
                if (ab0 != null && Ce < CostOf(ab0)) M.Events.LowCe(this);
                return false;
            }
            var ab = Kit[slot];
            Ce -= CostOf(ab);
            Cd[slot] = ab.Cooldown;
            Casting = ab; CastSlot = slot; ChantLevel = 0; chantTimer = 0f; castClock = 0f; castFired = false;
            CurAttack = null;
            SetState(FState.Cast);
            FaceTarget();
            if (Grounded) Vel.x *= 0.2f; else Vel *= 0.4f;
            if (ab.CastSound != Sfx.None) Audio.Play(ab.CastSound, Center, 0.5f);
            ab.OnCastStart(this);
            if (!(ab is DomainAb)) VFX.Charge(Front(0.7f, 1.3f), ab.Color, Mathf.Max(0.12f, ab.CastTime));
            return true;
        }

        void TickCast(float dt)
        {
            var ab = Casting;
            if (ab == null) { SetState(Grounded ? FState.Idle : FState.Air); return; }
            if (Grounded) Vel.x = Mathf.MoveTowards(Vel.x, 0f, 30f * dt);
            else Vel.y = Mathf.Max(Vel.y, -3f); // hang in the air while casting

            // chanting: keep holding the button to recite the incantation for extra power
            bool holding = In.Held(IB.Slot(CastSlot));
            if (!castFired && ab.Chantable && holding && ChantLevel < ab.MaxChant && castClock >= ab.CastTime * 0.5f)
            {
                chantTimer += dt;
                if (chantTimer >= 0.7f)
                {
                    chantTimer = 0f;
                    ChantLevel++;
                    string line = ab.Chant != null && ab.Chant.Length > 0 ? ab.Chant[Mathf.Min(ChantLevel - 1, ab.Chant.Length - 1)] : "...";
                    LastChant = line;
                    VFX.Chant(this, line, ab.Color);
                    Audio.Play(Sfx.Chant, Center, 0.8f, 1f - 0.08f * ChantLevel);
                    if (!IsRestricted) Ce = Mathf.Max(0f, Ce - 4f);
                    M.Events.Chant(this, ChantLevel);
                }
                return; // cast clock pauses while chanting
            }

            castClock += dt;
            if (!castFired && castClock >= ab.CastTime)
            {
                castFired = true;
                CastPower = (1f + 0.35f * ChantLevel) * PowerNoBase();
                ab.Execute(this, CastPower);
            }
            float recovery = ab is CounterAb ca ? ca.Window : ab.Recovery;
            if (castFired && castClock >= ab.CastTime + recovery)
            {
                Casting = null; CastSlot = -1;
                SetState(Grounded ? FState.Idle : FState.Air);
            }
        }

        public void CancelCast()
        {
            if (Casting != null)
            {
                if (Casting is DomainAb) M.Domains.CancelCast(this);
                Casting = null; CastSlot = -1;
            }
        }

        public void SetCounter(CounterAb c, float window) { counter = c; counterTimer = window; }

        /// <summary>Ability damage multiplier without the base character stat (abilities are pre-balanced per character).</summary>
        public float PowerNoBase()
        {
            float p = 1f;
            if (Has(StatusType.PowerUp)) p += StatusMag(StatusType.PowerUp);
            if (Has(StatusType.Overtime)) p += StatusMag(StatusType.Overtime);
            if (Has(StatusType.Zone)) p += 0.15f;
            if (Has(StatusType.Jackpot)) p += 0.2f;
            if (Has(StatusType.Executioner)) p += StatusMag(StatusType.Executioner);
            if (IsBoss) p *= M.Mode != null ? M.Mode.BossDamageMul : 1f;
            return p;
        }

        /// <summary>Damage multiplier for normal attacks.</summary>
        public float Power => Def.Power * PowerNoBase();

        // ---------------------------------------------------------------- strikes
        public void BeginStrike(StrikeDef s, float power)
        {
            strike = s; strikeTime = 0f; strikePower = power; strikeHitClock = 0f; strikeHitCount = 0;
            strikeHits.Clear();
            if (s.TeleportBehind)
            {
                var t = Target;
                if (t != null)
                {
                    VFX.Afterimage(this, 0.3f);
                    float nx = Mathf.Clamp(t.Pos.x - t.Facing * 1.1f, M.Arena.Left + 0.5f, M.Arena.Right - 0.5f);
                    Teleport(new Vector2(nx, t.Pos.y));
                    Facing = t.Pos.x > Pos.x ? 1 : -1;
                }
            }
            Vel = new Vector2(s.Velocity.x * Facing, s.Velocity.y);
            if (s.Invuln) Invuln = Mathf.Max(Invuln, s.Duration);
            if (s.Velocity.y > 0.1f) Grounded = false;
            Audio.Play(s.Sound, Center, 0.6f);
        }

        void TickStrike(float dt)
        {
            var s = strike;
            strikeTime += dt;
            if (!s.Gravity) Vel.y = s.Velocity.y;
            if (s.Velocity.x != 0f) Vel.x = s.Velocity.x * Facing;
            if (s.Afterimages && strikeTime % 0.04f < dt) VFX.Afterimage(this, 0.22f);

            strikeHitClock -= dt;
            if (strikeHitClock <= 0f && strikeHitCount < s.Hits)
            {
                Vector2 c = Pos + new Vector2(s.Offset.x * Facing, s.Offset.y) * Size;
                bool any = false;
                var list = M.Fighters;
                for (int i = 0; i < list.Count; i++)
                {
                    var e = list[i];
                    if (e == this || e.Dead || !M.IsEnemy(this, e)) continue;
                    if (s.Hits == 1 && strikeHits.Contains(e)) continue;
                    if (!e.Overlaps(c, s.Radius * Size)) continue;
                    any = true;
                    strikeHits.Add(e);
                    bool last = strikeHitCount >= s.Hits - 1;
                    Vector2 kb = last ? new Vector2(s.Knockback.x * Facing, s.Knockback.y) : new Vector2(1.5f * Facing, 0.8f + (e.Grounded ? 0f : 1.6f));
                    var h = HitInfo.Make(this, HitSource.Strike, s.Damage * strikePower / Mathf.Max(1, s.Hits) * (s.Hits > 1 ? 1.15f : 1f),
                                         kb, last ? s.Hitstun : 0.35f, s.Flags, s.Type, c, s.Color);
                    h.Hitstop = last ? s.Hitstop : 0.04f;
                    if (s.Status != StatusType.Count) h = h.WithStatus(s.Status, s.StatusTime, s.StatusMag);
                    if (Has(StatusType.Executioner)) { h.Damage *= 1f + StatusMag(StatusType.Executioner); h.Flags |= HitFlags.PierceInfinity; }
                    bool bf = s.BlackFlash && last && !IsRestricted;
                    if (bf)
                    {
                        h.Damage *= 2.5f;
                        h.Flags |= HitFlags.BlackFlash | HitFlags.GuardBreak | HitFlags.Unblockable;
                        h.Hitstop = 0.24f; h.Hitstun = Mathf.Max(h.Hitstun, 0.9f);
                        h.Knockback = new Vector2(16f * Facing, 7f);
                    }
                    var res = M.Hit(e, h);
                    if (res == HitResult.Hit)
                    {
                        if (bf) OnBlackFlash(e, c);
                        Hitstop = Mathf.Max(Hitstop, h.Hitstop);
                        s.OnHit?.Invoke(this, e, strikePower);
                        if (s.DelayedDamage > 0f)
                        {
                            var victim = e;
                            float dd = s.DelayedDamage * strikePower;
                            var col = s.Color;
                            int face = Facing;
                            M.Schedule(s.DelayedTime, () =>
                            {
                                if (victim.Dead) return;
                                var h2 = HitInfo.Make(this, HitSource.Other, dd, new Vector2(6f * face, 4f), 0.5f, HitFlags.Technique | HitFlags.Unblockable, DamageType.Energy, victim.Center, col);
                                h2.Hitstop = 0.1f;
                                M.Hit(victim, h2);
                                VFX.Burst(BurstVis.Shockwave, victim.Center, 1.4f, col);
                                VFX.WorldText(victim.HeadPos + Vector2.up * 0.5f, "DIVERGENT", col, 0.8f);
                                Audio.Play(Sfx.PunchHeavy, victim.Center, 0.8f, 0.7f);
                            });
                        }
                    }
                }
                if (any) { strikeHitCount++; strikeHitClock = s.HitInterval; if (s.StopOnHit) { Vel *= 0.15f; strikeTime = Mathf.Max(strikeTime, s.Duration - 0.06f); } }
            }
            if (strikeTime >= s.Duration)
            {
                strike = null;
                Vel.x *= 0.25f;
                if (Vel.y > 0f) Vel.y *= 0.5f;
            }
        }

        public void Teleport(Vector2 p)
        {
            VFX.Teleport(Center, Def.Look.Aura);
            Pos = new Vector2(Mathf.Clamp(p.x, M.Arena.Left + 0.3f, M.Arena.Right - 0.3f), Mathf.Max(0f, p.y));
            PrevPos = Pos;
            Grounded = Pos.y <= 0.001f;
            VFX.Teleport(Center, Def.Look.Aura);
            Audio.Play(Sfx.Teleport, Center, 0.6f);
        }

        // ---------------------------------------------------------------- physics
        void Physics(float dt, bool dead)
        {
            bool wasGrounded = Grounded;
            float g = 42f;
            if (State == FState.Hitstun) g *= 0.82f;
            if (State == FState.Cast) g *= 0.35f;
            if (State == FState.Attack && CurAttack != null && CurAttack.Air && (CurAttack.Flags & HitFlags.Heavy) == 0) g *= 0.5f;   // air strings hang
            if (strike != null && !strike.Gravity) g = 0f;
            if (State == FState.Dash && !Grounded) g = 0f;
            if (Has(StatusType.Gravity)) g *= 1.8f;
            if (!Grounded || Vel.y > 0f) Vel.y -= g * dt;
            if (Vel.y < -24f) Vel.y = -24f;

            Vector2 before = Pos;
            Pos += Vel * dt;
            MovedDistance += (Pos - before).magnitude;

            var A = M.Arena;
            if (Pos.y <= 0f)
            {
                Pos.y = 0f;
                if (Vel.y < 0f)
                {
                    // ground bounce from spikes / hard knockdowns
                    if (State == FState.Hitstun && Vel.y < -14f)
                    {
                        Vel.y = -Vel.y * 0.45f;
                        VFX.GroundImpact(Pos, Def.Look.Aura);
                        CameraRig.Shake(0.3f);
                        Audio.Play(Sfx.Land, Pos, 1f, 0.6f);
                    }
                    else Vel.y = 0f;
                }
                Grounded = Vel.y <= 0.01f;
            }
            else Grounded = false;

            // walls (wall splat when slammed)
            if (Pos.x < A.Left || Pos.x > A.Right)
            {
                Pos.x = Mathf.Clamp(Pos.x, A.Left, A.Right);
                if (State == FState.Hitstun && Mathf.Abs(Vel.x) > 11f)
                {
                    Vel.x = -Vel.x * 0.35f;
                    Vel.y = Mathf.Max(Vel.y, 4f);
                    VFX.WallImpact(new Vector2(Pos.x, Pos.y + 1.1f), Pos.x < 0 ? 1 : -1, Def.Look.Aura);
                    CameraRig.Shake(0.35f);
                    Audio.Play(Sfx.PunchHeavy, Pos, 0.8f, 0.6f);
                }
                else Vel.x = 0f;
            }
            if (Pos.y > A.Ceiling) { Pos.y = A.Ceiling; if (Vel.y > 0) Vel.y = 0; }

            if (dead) { if (Grounded) Vel.x = Mathf.MoveTowards(Vel.x, 0f, 15f * dt); return; }

            if (!wasGrounded && Grounded)
            {
                AirStep = 0;
                if (State == FState.Air || State == FState.Dash) { SetState(FState.Idle); Land(); }
                else if (State == FState.Hitstun)
                {
                    // only real launches (or very long juggles) end on the floor; light pops just stagger
                    bool hard = (Launched && (Juggle >= 2 || Hitstun > 0.15f)) || Juggle >= 6;
                    Launched = false;
                    if (hard && !Has(StatusType.Armor) && !IsBoss)
                    {
                        SetState(FState.Knockdown);
                        KnockdownTime = 0.55f;
                        VFX.Dust(Pos, 8);
                        Audio.Play(Sfx.Land, Pos, 0.7f, 0.8f);
                    }
                }
            }
            if (wasGrounded && !Grounded && (State == FState.Idle || State == FState.Run)) SetState(FState.Air);
        }

        /// <summary>Soft push so fighters don't stack inside each other.</summary>
        public void Separate(Fighter o, float dt)
        {
            if (Dead || o.Dead || State == FState.Respawning || o.State == FState.Respawning) return;
            float dx = o.Pos.x - Pos.x;
            float dy = Mathf.Abs(o.Pos.y - Pos.y);
            float min = 0.55f * (Size + o.Size) * 0.5f + 0.15f;
            if (dy > 1.4f || Mathf.Abs(dx) >= min) return;
            float push = (min - Mathf.Abs(dx)) * 0.5f;
            float s = dx >= 0 ? 1f : -1f;
            if (dx == 0f) s = Slot < o.Slot ? -1f : 1f;
            Pos.x -= push * s; o.Pos.x += push * s;
        }
    }
}
