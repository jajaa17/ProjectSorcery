using UnityEngine;

namespace ProjectSorcery
{
    /// <summary>
    /// The animation brain of a fighter. Picks a keyframed target every frame (moves, gait, reactions, casts),
    /// drives every channel toward it with a spring-damper (overshoot, settle, overlap), then solves the skeleton:
    /// hips from the legs, hands by two-bone IK (fists travel in straight lines), weapon from the wrist, then the
    /// whole-body rotation and the 2D "turn". Pure math: no rendering, so the headless simulator can preview it.
    /// </summary>
    public sealed class RigSolver
    {
        public readonly Fighter F;
        public readonly MoveSet Set;
        public readonly Persona Persona;
        readonly Stance stance;
        readonly Kf[] gait;
        readonly Kf idleK, guardK, victoryK, pocketK, carryK;
        readonly float omegaBase, zetaBase, bounce, bounceRate, sway, landSquash;
        readonly bool hover, armed, bladeCarry;
        readonly float weaponLen, gripOffset;
        readonly System.Random rnd;

        const int CH = Kf.Count;
        readonly float[] x = new float[CH], v = new float[CH];
        static readonly float[] chMul = { 1f, 0.85f, 0.85f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1.15f, 0.5f, 1f, 1.1f, 1f, 1.25f };

        float runPhase, idleTime, airTime, flip, tumble, prevPhase = -1f, gripBlend;
        bool wasGrounded = true, hurtHeavy;
        int prevAirJumps = 1, lastFacing, srcId = int.MinValue, prevClip = -1;

        // ---------------- outputs
        public Vector2 RenderPos, Hip, Neck, HeadC, HandF, HandB, ElbowF, ElbowB, KneeF, KneeB, FootF, FootB, WeaponBase, WeaponTip;
        public float FaceSign = 1f;            // -1 while turned around mid-spin
        public float BodyWidth = 1f;           // |cos(turn)|, how "side-on" the figure currently reads
        public Clip Clip;                      // clip playing this frame (null = locomotion / reactions)
        public float Phase;                    // 0..3 within the clip
        public bool Striking;                  // in the snap/contact part of a move (smears, trails)
        public bool Contact;                   // the frame the strike reaches its target pose
        public bool Charging;
        public bool Turned => FaceSign < 0f;

        public RigSolver(Fighter f)
        {
            F = f;
            var d = f.Def;
            stance = d.Stance;
            Set = MoveLib.For(d);
            Persona = Persona.For(d);
            rnd = new System.Random(d.Id.GetHashCode() ^ f.Slot * 7919);
            armed = Set.Style == MoveStyle.Blade || Set.Style == MoveStyle.Staff || Set.Style == MoveStyle.HeavyWeapon;
            bladeCarry = Set.Style == MoveStyle.Blade;

            var idle = Persona.Idle ?? MoveLib.StyleIdle(stance);
            idleK = idle.ToKf();
            Pose guard;
            if (armed) guard = Pose.P(-4, 40, 150, 60, 140, -26, -40, 30, 2, 0, -2, 0.08f, 20f);
            else
                switch (stance)
                {
                    case Stance.Brawler: guard = Pose.P(-2, 40, 165, 55, 160, -26, -40, 28, 2, 0, 4, 0.1f); break;
                    case Stance.Martial: guard = Pose.P(0, 40, 150, 80, 110, -26, -38, 30, 0, 0, 0, 0.08f); break;
                    case Stance.Elegant: guard = Pose.P(-4, -12, 12, 70, 150, -14, -18, 16, 2, 0, -4); break;
                    default: guard = Poses.Block; break;
                }
            guardK = guard.ToKf();
            var vict = Persona.Victory ?? (stance == Stance.Elegant ? Pose.P(-8, -12, 12, -8, 18, -10, -10, 12, 2, 0, -15)
                                          : stance == Stance.Feral ? Pose.P(-10, 140, 120, 150, 130, -20, -30, 20, 0, 0, -20) : Poses.Victory);
            victoryK = vict.ToKf();
            pocketK = Persona.PocketArm.ToKf();
            carryK = (bladeCarry ? Pose.P(0, 0, 0, -25, -50, 0, 0, 0, 0, 0, 0, 0, -25f) : Pose.P(0, 0, 0, 15, 55, 0, 0, 0, 0, 0, 0, 0, 55f)).ToKf();

            gait = Anim.GaitKeys(Persona.Casual ? Stance.Elegant : stance);

            switch (stance)
            {
                case Stance.Brawler: omegaBase = 30f; zetaBase = 0.55f; bounce = 0.06f; bounceRate = 2.2f; break;
                case Stance.Elegant: omegaBase = 26f; zetaBase = 0.68f; bounce = 0.015f; bounceRate = 0.6f; break;
                case Stance.Swordsman: omegaBase = 30f; zetaBase = 0.55f; bounce = 0.02f; bounceRate = 0.9f; break;
                case Stance.Brute: omegaBase = 21f; zetaBase = 0.68f; bounce = 0.03f; bounceRate = 0.7f; landSquash = 1.8f; break;
                case Stance.Feral: omegaBase = 32f; zetaBase = 0.48f; bounce = 0.05f; bounceRate = 1.8f; break;
                case Stance.Caster: omegaBase = 26f; zetaBase = 0.65f; bounce = 0.02f; bounceRate = 0.8f; break;
                case Stance.Agile: omegaBase = 34f; zetaBase = 0.52f; bounce = 0.07f; bounceRate = 2.8f; break;
                case Stance.Floaty: omegaBase = 18f; zetaBase = 0.75f; bounce = 0.01f; bounceRate = 0.5f; hover = true; break;
                case Stance.Mechanical: omegaBase = 40f; zetaBase = 0.95f; bounce = 0f; bounceRate = 1f; break;
                case Stance.Wrestler: omegaBase = 23f; zetaBase = 0.62f; bounce = 0.05f; bounceRate = 1.4f; landSquash = 1.6f; break;
                default: omegaBase = 28f; zetaBase = 0.6f; bounce = 0.03f; bounceRate = 1f; break;
            }
            if (landSquash <= 0f) landSquash = 1f;
            sway = Persona.Casual ? 0.8f : 1.5f;
            if (Persona.Bouncy) { bounce = 0.09f; bounceRate = 2.4f; }
            if (Persona.Casual) { bounce = Mathf.Min(bounce, 0.02f); bounceRate = Mathf.Min(bounceRate, 0.8f); }

            weaponLen = WeaponLength(d.Look.Weapon);
            gripOffset = GripOffset(d.Look.Weapon);

            for (int i = 0; i < CH; i++) x[i] = idleK[i];
            lastFacing = f.Facing;
        }

        static float WeaponLength(Weapon w)
        {
            switch (w)
            {
                case Weapon.Katana: return 1.15f; case Weapon.Sword: return 1.6f; case Weapon.Spear: return 1.3f; case Weapon.Polearm: return 1.6f;
                case Weapon.Staff: return 0.9f; case Weapon.Broom: return 0.6f; case Weapon.Cloud: return 1.5f; case Weapon.Hammer: return 0.55f;
                case Weapon.Gavel: return 0.45f; case Weapon.Cleaver: return 0.85f; case Weapon.Axe: return 0.85f; case Weapon.Guitar: return 0.7f;
                case Weapon.Claws: return 0.45f; case Weapon.Fan: return 0.5f; default: return 0.3f;
            }
        }

        static float GripOffset(Weapon w)
        {
            switch (w)
            {
                case Weapon.Staff: case Weapon.Polearm: case Weapon.Spear: return 0.5f;
                case Weapon.Broom: return 0.4f; case Weapon.Sword: return 0.22f; case Weapon.Katana: return 0.16f;
                case Weapon.Hammer: return 0.25f; case Weapon.Guitar: return 0.35f; case Weapon.Axe: return 0.2f;
                default: return 0f;
            }
        }

        float Rand(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

        // =====================================================================
        public void Step(float dt, float alpha)
        {
            var f = F;
            idleTime += dt;
            RenderPos = Vector2.LerpUnclamped(f.PrevPos, f.Pos, alpha);

            // ---- bookkeeping
            if (f.State == FState.Run) runPhase += Mathf.Abs(f.Vel.x) * dt * 0.36f * Anim.Gait(stance).Cadence / Mathf.Max(0.7f, f.Size);
            if (f.Grounded) airTime = 0f; else airTime += dt;
            if (!wasGrounded && f.Grounded && f.State != FState.Hitstun && f.State != FState.Knockdown && f.State != FState.Dead)
            {
                v[13] += 3.2f * landSquash;   // landing squash
                v[0] += 70f;
            }
            wasGrounded = f.Grounded;
            if (!f.Grounded && f.AirJumps < prevAirJumps) flip = 0.42f;
            prevAirJumps = f.AirJumps;
            if (flip > 0f) flip -= dt;

            // turning around reads as a quick pivot rather than a pop
            if (f.Facing != lastFacing)
            {
                var s = f.State;
                if (s == FState.Idle || s == FState.Run || s == FState.Air || s == FState.Block || s == FState.Attack || s == FState.Cast || s == FState.GetUp)
                { x[16] = Mathf.Round(x[16]) + 0.5f; v[16] = 0f; }
                lastFacing = f.Facing;
            }

            var t = Target(out float omega, out float zeta, out int id);
            if (id != srcId)
            {
                x[11] += 360f * Mathf.Round((t.Rot - x[11]) / 360f);
                x[14] += 360f * Mathf.Round((t.Wr - x[14]) / 360f);
                x[16] += Mathf.Round(t.Tn - x[16]);
                srcId = id;
            }

            // ---- hit recoil: bigger hits whip the body harder
            if (f.HitImpulse > 0f)
            {
                float imp = f.HitImpulse;
                hurtHeavy = imp > 1.15f || (!f.Grounded && f.Vel.y > 7f);
                tumble = 0f;
                v[0] -= 260f * imp;
                v[12] -= 420f * imp;
                for (int i = 1; i <= 5; i++) if (i != 3) v[i] += Rand(-1f, 1f) * 2.2f * imp;
                v[13] += 1.1f * imp;
                f.HitImpulse = 0f;
            }
            if (f.GuardImpulse > 0f)
            {
                v[0] -= 140f * f.GuardImpulse;
                v[13] += 0.7f * f.GuardImpulse;
                f.GuardImpulse = 0f;
            }

            Simulate(t, dt, omega, zeta);
            Solve();
        }

        // =====================================================================
        //  target selection
        // =====================================================================
        Kf Target(out float omega, out float zeta, out int id)
        {
            var f = F;
            omega = omegaBase * Set.Tempo; zeta = zetaBase;
            Clip = null; Striking = false; Contact = false; Charging = false;
            Kf k;

            // ---- scripted technique strikes (rushes, flurries, lunges)
            if (f.InStrike)
            {
                var s = f.CurStrike;
                Clip c; float ph;
                if (s.Hits > 1 && s.Duration > 0.22f && (s.Pose == FPose.Jab || s.Pose == FPose.Dash || s.Pose == FPose.Kick || s.Pose == FPose.Hook))
                {
                    // flurry: alternate hands, each blow its own snap
                    float per = Mathf.Max(0.05f, s.HitInterval);
                    int n = (int)(f.StrikeTime / per);
                    c = (n & 1) == 0 ? Set.Light[0].Clip : Set.Light[Mathf.Min(1, Set.Light.Length - 1)].Clip;
                    ph = 0.75f + Mathf.Repeat(f.StrikeTime / per, 1f) * 1.3f;
                }
                else
                {
                    c = IsMeleePose(s.Pose) ? Set.ForPose(s.Pose) : Anim.Cast(s.Pose);
                    ph = 1f + Mathf.Clamp01(f.StrikeTime / Mathf.Max(0.05f, s.Duration)) * 0.9f;
                }
                return Play(c, ph, ref omega, ref zeta, out id);
            }

            switch (f.State)
            {
                case FState.Attack:
                {
                    var a = f.CurAttack;
                    if (a == null || a.Clip == null) break;
                    float t = f.StateTime, ph;
                    if (f.ChargeTime > 0f && t < a.Startup)
                    {
                        Charging = true;
                        ph = 0.8f + Mathf.Sin(idleTime * 48f) * 0.012f;
                    }
                    else if (t < a.Startup) ph = t / Mathf.Max(0.001f, a.Startup);
                    else if (t < a.Startup + a.Active) ph = 1f + (t - a.Startup) / Mathf.Max(0.001f, a.Active);
                    else ph = 2f + Mathf.Min(1f, (t - a.Startup - a.Active) / Mathf.Max(0.001f, a.Recovery * 0.85f));
                    k = Play(a.Clip, ph, ref omega, ref zeta, out id);
                    return k;
                }
                case FState.Cast:
                {
                    var ab = f.Casting;
                    if (ab == null) break;
                    if (ab is DomainAb || (f.ChantLevel > 0 && !f.CastFired))
                    {
                        var c = ab is DomainAb ? Anim.Cast(FPose.HandSign) : Anim.Cast(FPose.Chant);
                        k = Play(c, 1.2f, ref omega, ref zeta, out id);
                        k.Drop += Mathf.Sin(idleTime * 3f) * 0.01f;
                        omega *= 0.7f;
                        return k;
                    }
                    var clip = IsMeleePose(ab.Pose) ? Set.ForPose(ab.Pose) : Anim.Cast(ab.Pose);
                    float ph;
                    float clock = f.CastClock;
                    if (!f.CastFired) ph = Mathf.Clamp01(clock / Mathf.Max(0.05f, ab.CastTime)) * 0.97f;
                    else
                    {
                        float after = clock - ab.CastTime;
                        ph = after < 0.07f ? 1f + after / 0.07f : 2f + Mathf.Clamp01((after - 0.07f) / Mathf.Max(0.08f, ab.Recovery));
                    }
                    return Play(clip, ph, ref omega, ref zeta, out id);
                }
            }

            id = -100 - (int)f.State;
            switch (f.State)
            {
                case FState.Idle:
                {
                    k = idleK;
                    float br = Mathf.Sin(idleTime * bounceRate * Mathf.PI * 2f);
                    k.Lean += br * sway;
                    k.HFy += br * 0.012f; k.HBy -= br * 0.01f;
                    k.Drop += (br * 0.5f + 0.5f) * bounce;
                    k.Head += Mathf.Sin(idleTime * 0.9f) * 2f;
                    Hold(ref k, true);
                    return k;
                }
                case FState.Run:
                {
                    float sp = Mathf.Clamp01(Mathf.Abs(f.Vel.x) / Mathf.Max(1f, 7.2f * f.Def.Speed));
                    var run = Anim.SampleGait(gait, runPhase);
                    float amp = Mathf.Lerp(0.45f, 1f, sp) * (Persona.Casual ? 0.85f : 1f);
                    k = Kf.Lerp(idleK, run, amp);
                    k.Lean += Mathf.Clamp(f.Vel.x * f.Facing * 0.9f, -5f, 7f);
                    Hold(ref k, false);
                    omega *= 1.1f;
                    return k;
                }
                case FState.Air:
                {
                    float vy = f.Vel.y;
                    if (flip > 0f)
                    {
                        k = Anim.Apex.ToKf();
                        k.Rot = 360f * (1f - flip / 0.42f);
                        omega = 40f; zeta = 0.8f;
                        id = -50;
                    }
                    else if (airTime < 0.09f && vy > 0f) k = Anim.Takeoff.ToKf();
                    else
                    {
                        float u = Mathf.InverseLerp(9f, -9f, vy);
                        k = u < 0.5f ? Kf.Lerp(Anim.Rise.ToKf(), Anim.Apex.ToKf(), u * 2f) : Kf.Lerp(Anim.Apex.ToKf(), Anim.Fall.ToKf(), (u - 0.5f) * 2f);
                    }
                    Hold(ref k, false);
                    return k;
                }
                case FState.Dash:
                {
                    bool back = f.Vel.x * f.Facing < 0f;
                    k = (back ? Anim.DashBack : Anim.DashFwd).ToKf();
                    omega *= 1.6f; Striking = true;
                    Hold(ref k, false);
                    return k;
                }
                case FState.Block: k = guardK; if (Persona.Pocket) Pocket(ref k); return k;
                case FState.SimpleDomain: k = Pose.P(20, 46, 156, 52, 150, -55, -110, 70, -30, 0, 0, 0.25f).ToKf(); return k;
                case FState.RCT: k = Anim.Cast(FPose.Channel).Sample(1.2f); k.Lean += Mathf.Sin(idleTime * 3f) * 2f; return k;
                case FState.Hitstun:
                    omega = 28f; zeta = 0.4f;
                    if (f.Grounded) return (hurtHeavy ? Anim.HurtGut : Anim.HurtHigh).ToKf();
                    k = Anim.HurtAir.ToKf();
                    if (hurtHeavy)
                    {
                        tumble += Time0(f) * 620f;
                        k.Rot = -tumble;
                        id = -60;
                        omega = 34f;
                    }
                    else k.Rot = Mathf.Clamp(-f.Vel.y * 2.5f - 20f, -70f, 30f);
                    return k;
                case FState.Knockdown:
                case FState.Dead:
                    omega = 16f; zeta = 0.5f;
                    k = Anim.Knocked.ToKf();
                    if (!f.Grounded) k.Rot = -40f - Mathf.Clamp(-f.Vel.y * 3f, -40f, 40f);
                    return k;
                case FState.GetUp:
                {
                    float u = Mathf.Clamp01(f.StateTime / 0.32f);
                    bool kip = stance == Stance.Agile || stance == Stance.Martial || stance == Stance.Feral || stance == Stance.Elegant || stance == Stance.Brawler;
                    var mid = (kip ? Anim.KipCoil : Anim.Kneel).ToKf();
                    k = u < 0.45f ? Kf.Lerp(Anim.Knocked.ToKf(), mid, Anim.Apply(Ease.Out, u / 0.45f)) : Kf.Lerp(mid, idleK, Anim.Apply(Ease.InOut, (u - 0.45f) / 0.55f));
                    omega = 30f; zeta = 0.6f;
                    return k;
                }
                case FState.Victory:
                    k = victoryK; k.Drop += Mathf.Sin(idleTime * 2f) * 0.01f;
                    if (Persona.Pocket) Pocket(ref k);
                    return k;
                case FState.Locked:
                    if (f.M.Domains.Clashing) return Anim.Cast(FPose.HandSign).Sample(1.2f);
                    k = idleK; Hold(ref k, true); return k;
                default:
                    k = idleK; Hold(ref k, true); return k;
            }
        }

        float Time0(Fighter f) => Mathf.Min(0.05f, lastDt);
        float lastDt = 1f / 60f;

        static bool IsMeleePose(FPose p) => p == FPose.Jab || p == FPose.Cross || p == FPose.Hook || p == FPose.Kick || p == FPose.Uppercut ||
                                            p == FPose.Sweep || p == FPose.HeavyPunch || p == FPose.AirKick || p == FPose.AirSlash || p == FPose.DiveKick;

        /// <summary>Samples a clip, applying per-move spring feel and contact detection.</summary>
        Kf Play(Clip c, float ph, ref float omega, ref float zeta, out int id)
        {
            Clip = c; Phase = ph;
            id = c.Id;
            if (prevClip != c.Id) { prevClip = c.Id; prevPhase = ph; }
            Contact = prevPhase < 1f && ph >= 1f;
            prevPhase = ph;
            var k = c.Sample(ph);
            // the snap: very stiff into contact, a touch looser through the follow-through (overshoot = weight)
            if (ph < 0.8f) { omega = Mathf.Max(omega * 1.5f, 36f) * c.Stiff; zeta = 0.7f; }
            else if (ph < 2f) { omega = Mathf.Max(omega * 2.4f, 58f) * c.Stiff; zeta = 0.5f; Striking = ph > 0.86f; }
            else { omega = Mathf.Max(omega * 1.2f, 30f) * c.Stiff; zeta = 0.55f; Striking = ph < 2.3f; }
            omega *= Set.Tempo;
            if (Persona.Pocket && c.TwoHand != 1 && c.Smear != Limb.HandB && c.Smear != Limb.Hands && !ReadsBackArm(c)) Pocket(ref k);
            return k;
        }

        static bool ReadsBackArm(Clip c) => c.Name == "Cross" || c.Name == "Haymaker" || c.Name == "GiantHay";

        void Pocket(ref Kf k) { k.HBx = pocketK.HBx; k.HBy = pocketK.HBy; k.BB = pocketK.BB; }

        /// <summary>Hands while not attacking: pocket, weapon carry.</summary>
        void Hold(ref Kf k, bool idle)
        {
            if (Persona.Pocket) Pocket(ref k);
            if (armed && !idle) { k.HFx = carryK.HFx; k.HFy = carryK.HFy; k.BF = carryK.BF; k.Wr = carryK.Wr; }
        }

        // =====================================================================
        void Simulate(in Kf target, float dt, float omega, float zeta)
        {
            lastDt = dt;
            if (dt <= 0f) return;
            int steps = Mathf.Clamp(Mathf.CeilToInt(dt / (1f / 180f)), 1, 8);
            float h = dt / steps;
            for (int s = 0; s < steps; s++)
                for (int i = 0; i < CH; i++)
                {
                    float w = omega * chMul[i];
                    float a = w * w * (target[i] - x[i]) - 2f * zeta * w * v[i];
                    v[i] += a * h;
                    x[i] += v[i] * h;
                }
            // bend sides are signs: keep them from drifting past +-1
            x[3] = Mathf.Clamp(x[3], -1f, 1f); x[6] = Mathf.Clamp(x[6], -1f, 1f);
        }

        // =====================================================================
        void Solve()
        {
            var f = F;
            float S = f.Size;
            int fc = f.Facing;
            float TH = 0.5f * S, SH = 0.5f * S, T = 0.85f * S, HR = 0.25f * S;
            float drop = Mathf.Clamp(x[13], -0.5f, 0.6f);
            float thB = x[7] + drop * 35f, shB = x[8] - drop * 55f, thF = x[9] + drop * 35f, shF = x[10] - drop * 55f;

            Vector2 L(float deg) { float r = deg * Mathf.Deg2Rad; return new Vector2(fc * Mathf.Sin(r), -Mathf.Cos(r)); }

            float lean = x[0] * Mathf.Deg2Rad;
            Vector2 up = new Vector2(fc * Mathf.Sin(lean), Mathf.Cos(lean));
            float dropB = TH * Mathf.Cos(thB * Mathf.Deg2Rad) + SH * Mathf.Cos(shB * Mathf.Deg2Rad);
            float dropF = TH * Mathf.Cos(thF * Mathf.Deg2Rad) + SH * Mathf.Cos(shF * Mathf.Deg2Rad);
            float hipH = Mathf.Max(0.22f * S, Mathf.Max(dropB, dropF));
            if (drop < 0f) hipH += -drop * S;                     // negative drop = hop / rise
            bool lying = Mathf.Abs(x[11]) > 50f && (f.State == FState.Knockdown || f.State == FState.Dead) && f.Grounded;
            if (lying) hipH = 0.28f * S;
            float hover = 0f;
            if (this.hover && f.Grounded && f.State != FState.Knockdown && f.State != FState.Dead) hover = 0.18f + Mathf.Sin(idleTime * 2.2f) * 0.08f;
            Vector2 shiver = f.Hitstop > 0f ? new Vector2(Rand(-0.05f, 0.05f), Rand(-0.025f, 0.025f)) : Vector2.zero;

            Hip = RenderPos + shiver + new Vector2(fc * x[15] * S, hipH + hover);
            Neck = Hip + up * T;
            float ht = (x[0] + x[12] * 0.6f) * Mathf.Deg2Rad;
            HeadC = Neck + new Vector2(fc * Mathf.Sin(ht), Mathf.Cos(ht)) * (HR * 1.25f);

            KneeB = Hip + L(thB) * TH; FootB = KneeB + L(shB) * SH;
            KneeF = Hip + L(thF) * TH; FootF = KneeF + L(shF) * SH;

            // arms: two-bone IK toward the keyed hand positions (straight-line punches)
            Arm(Neck, new Vector2(fc * x[4], x[5]) * S, x[6] * fc, S, out ElbowF, out HandF);
            Arm(Neck, new Vector2(fc * x[1], x[2]) * S, x[3] * fc, S, out ElbowB, out HandB);

            // weapon: blade angle = forearm angle + wrist
            Vector2 fore = HandF - ElbowF;
            float foreDeg = Mathf.Atan2(fore.x * fc, -fore.y) * Mathf.Rad2Deg;
            Vector2 wd = L(foreDeg + x[14]);
            WeaponBase = HandF;
            WeaponTip = HandF + wd * weaponLen * S;

            // two-handed grip: the back hand rides the shaft
            float wantGrip = 0f;
            if (gripOffset > 0f)
            {
                int th = Clip != null ? Clip.TwoHand : -1;
                bool two = th == 1 || (th == -1 && Set.TwoHand && (Clip != null || F.State == FState.Block || F.State == FState.Idle));
                if (F.State == FState.Hitstun || F.State == FState.Knockdown || F.State == FState.Dead || Persona.Pocket) two = false;
                wantGrip = two ? 1f : 0f;
            }
            gripBlend = Mathf.MoveTowards(gripBlend, wantGrip, lastDt * 8f);
            if (gripBlend > 0.01f)
            {
                Vector2 grip = HandF - wd * gripOffset * S;
                Arm(Neck, grip - Neck, x[3] * fc, S, out var eb, out var hb);
                ElbowB = Vector2.Lerp(ElbowB, eb, gripBlend); HandB = Vector2.Lerp(HandB, hb, gripBlend);
            }

            // whole-body rotation (flips, tumbles, lying down)
            float rot = -x[11] * fc;
            if (Mathf.Abs(rot) > 0.01f)
            {
                Vector2 p = Hip;
                Neck = Rot(Neck, p, rot); HeadC = Rot(HeadC, p, rot);
                ElbowB = Rot(ElbowB, p, rot); HandB = Rot(HandB, p, rot); ElbowF = Rot(ElbowF, p, rot); HandF = Rot(HandF, p, rot);
                KneeB = Rot(KneeB, p, rot); FootB = Rot(FootB, p, rot); KneeF = Rot(KneeF, p, rot); FootF = Rot(FootF, p, rot);
                WeaponBase = Rot(WeaponBase, p, rot); WeaponTip = Rot(WeaponTip, p, rot);
            }

            // 2D turn: squash through side-on and mirror (spinning kicks, pivots)
            float sx = Mathf.Cos(x[16] * Mathf.PI * 2f);
            BodyWidth = Mathf.Abs(sx);
            if (Mathf.Abs(sx) > 0.02f) FaceSign = Mathf.Sign(sx);
            if (Mathf.Abs(sx - 1f) > 0.001f)
            {
                float hx = Hip.x;
                Neck.x = hx + (Neck.x - hx) * sx; HeadC.x = hx + (HeadC.x - hx) * sx;
                ElbowB.x = hx + (ElbowB.x - hx) * sx; HandB.x = hx + (HandB.x - hx) * sx; ElbowF.x = hx + (ElbowF.x - hx) * sx; HandF.x = hx + (HandF.x - hx) * sx;
                KneeB.x = hx + (KneeB.x - hx) * sx; FootB.x = hx + (FootB.x - hx) * sx; KneeF.x = hx + (KneeF.x - hx) * sx; FootF.x = hx + (FootF.x - hx) * sx;
                WeaponBase.x = hx + (WeaponBase.x - hx) * sx; WeaponTip.x = hx + (WeaponTip.x - hx) * sx;
            }
        }

        /// <summary>Two-bone IK. bendSide picks which side of the shoulder->hand line the elbow sits on.</summary>
        static void Arm(Vector2 shoulder, Vector2 rel, float bendSide, float S, out Vector2 elbow, out Vector2 hand)
        {
            float l1 = Kf.Upper * S, l2 = Kf.Fore * S;
            float d = rel.magnitude;
            float max = (l1 + l2) * 1.06f;                         // a little rubber-hose stretch on full extension
            Vector2 dir = d > 1e-4f ? rel / d : Vector2.down;
            if (d > max) d = max;
            d = Mathf.Max(d, 0.05f * S);
            float reach = Mathf.Min(d, (l1 + l2) * 0.999f);
            float cosA = Mathf.Clamp((l1 * l1 + reach * reach - l2 * l2) / (2f * l1 * reach), -1f, 1f);
            float a = Mathf.Acos(cosA) * (bendSide >= 0f ? 1f : -1f);
            float cs = Mathf.Cos(a), sn = Mathf.Sin(a);
            Vector2 ed = new Vector2(dir.x * cs - dir.y * sn, dir.x * sn + dir.y * cs);
            elbow = shoulder + ed * l1;
            hand = shoulder + dir * d;
        }

        static Vector2 Rot(Vector2 p, Vector2 c, float deg)
        {
            float r = deg * Mathf.Deg2Rad, cs = Mathf.Cos(r), sn = Mathf.Sin(r);
            Vector2 d = p - c;
            return c + new Vector2(d.x * cs - d.y * sn, d.x * sn + d.y * cs);
        }

        /// <summary>The swept segment for a smear on the given limb.</summary>
        public void SmearSegment(Limb l, bool second, out Vector2 a, out Vector2 b)
        {
            switch (l)
            {
                case Limb.Weapon: a = Vector2.Lerp(WeaponBase, WeaponTip, 0.25f); b = WeaponTip; return;
                case Limb.HandB: a = Vector2.Lerp(ElbowB, HandB, 0.35f); b = HandB + (HandB - ElbowB).normalized * 0.12f; return;
                case Limb.FootF: a = Vector2.Lerp(KneeF, FootF, 0.3f); b = FootF + (FootF - KneeF).normalized * 0.1f; return;
                case Limb.FootB: a = Vector2.Lerp(KneeB, FootB, 0.3f); b = FootB + (FootB - KneeB).normalized * 0.1f; return;
                case Limb.Hands:
                    if (second) { a = Vector2.Lerp(ElbowB, HandB, 0.35f); b = HandB + (HandB - ElbowB).normalized * 0.12f; }
                    else { a = Vector2.Lerp(ElbowF, HandF, 0.35f); b = HandF + (HandF - ElbowF).normalized * 0.12f; }
                    return;
                default: a = Vector2.Lerp(ElbowF, HandF, 0.35f); b = HandF + (HandF - ElbowF).normalized * 0.12f; return;
            }
        }
    }
}
