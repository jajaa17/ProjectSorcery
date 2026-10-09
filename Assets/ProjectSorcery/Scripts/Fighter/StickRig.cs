using UnityEngine;

namespace ProjectSorcery
{
    /// <summary>
    /// Renders a fighter as a glowing stick figure. Every joint is driven by a spring-damper toward the
    /// target pose, so motion overshoots and settles (anticipation, snap, follow-through) instead of
    /// snapping robotically. Purely visual: reads the simulation, never writes gameplay state.
    /// </summary>
    public sealed class StickRig
    {
        readonly Fighter f;
        readonly GameObject root;
        readonly LineRenderer legs, torso, arms, head, legsGlow, armsGlow, torsoGlow, headGlow, hair, face, eye, weapon, extraArms, crown, marker;
        readonly TrailRenderer handTrail, footTrail;
        readonly StyleProfile style;

        // spring channels: Lean, UaB, FaB, UaF, FaF, ThB, ShB, ThF, ShF, Rot, Head, Drop
        const int CH = 12;
        readonly float[] x = new float[CH], v = new float[CH], tgt = new float[CH];
        float runPhase, idleTime, hover;
        bool wasGrounded = true;
        Vector2 tailPos, tailVel;
        bool tailInit;
        float rainbow;
        int prevAirJumps = 1;
        float flip;

        public Vector2 RenderPos;
        public Vector2 Hip, Neck, HeadC, HandF, HandB, ElbowF, ElbowB, KneeF, KneeB, FootF, FootB;

        public StickRig(Transform parent, Fighter fighter)
        {
            f = fighter;
            style = StyleProfile.For(f.Def.Stance);
            root = new GameObject("Rig_" + f.Def.Id);
            root.transform.SetParent(parent, false);
            var t = root.transform;
            float w = 0.11f * f.Size;
            int o = Art.OrderFighter;
            legsGlow = Art.NewLine(t, "LegsGlow", Art.AddLine, Art.OrderFighterGlow, w * 3.2f, 5);
            torsoGlow = Art.NewLine(t, "TorsoGlow", Art.AddLine, Art.OrderFighterGlow, w * 3.4f, 2);
            armsGlow = Art.NewLine(t, "ArmsGlow", Art.AddLine, Art.OrderFighterGlow, w * 3.2f, 5);
            headGlow = Art.NewLine(t, "HeadGlow", Art.AddLine, Art.OrderFighterGlow, w * 3f, 20);
            legs = Art.NewLine(t, "Legs", Art.Line, o, w, 5);
            torso = Art.NewLine(t, "Torso", Art.Line, o, w * 1.15f, 2);
            arms = Art.NewLine(t, "Arms", Art.Line, o, w * 0.95f, 5);
            head = Art.NewLine(t, "Head", Art.Line, o, w * 0.9f, 20);
            head.loop = true; headGlow.loop = true;
            hair = Art.NewLine(t, "Hair", Art.Line, o, w * 0.85f, 2);
            face = Art.NewLine(t, "Face", Art.Line, o + 1, w * 0.7f, 2);
            eye = Art.NewLine(t, "Eye", Art.AddLine, o + 1, w * 0.55f, 2);
            weapon = Art.NewLine(t, "Weapon", Art.Line, o + 1, w * 0.75f, 2);
            extraArms = Art.NewLine(t, "ExtraArms", Art.Line, o, w * 0.85f, 5);
            crown = Art.NewLine(t, "Crown", Art.AddLine, o + 1, w * 0.6f, 2);
            marker = Art.NewLine(t, "Marker", Art.Line, o + 2, 0.06f, 3);
            hair.numCapVertices = 2; face.numCapVertices = 2;
            extraArms.enabled = f.Def.Look.FourArms;

            handTrail = MakeTrail(t, "HandTrail");
            footTrail = MakeTrail(t, "FootTrail");

            var idle = style.Idle;
            Load(idle, tgt);
            Load(idle, x);
        }

        TrailRenderer MakeTrail(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tr = go.AddComponent<TrailRenderer>();
            tr.sharedMaterial = Art.AddLine;
            tr.time = 0.11f;
            tr.minVertexDistance = 0.04f;
            tr.widthMultiplier = 0.32f * f.Size;
            tr.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            tr.sortingOrder = Art.OrderFighterGlow;
            tr.emitting = false;
            tr.numCapVertices = 2;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var c = f.Def.Look.Aura;
            tr.startColor = c.WithA(0.75f);
            tr.endColor = c.WithA(0f);
            return tr;
        }

        public LineRenderer Line(int i) => i == 0 ? legs : i == 1 ? torso : i == 2 ? arms : i == 3 ? head : null;

        public void Destroy()
        {
            if (root != null) Object.Destroy(root);
        }

        // =====================================================================
        static void Load(in Pose p, float[] a)
        {
            a[0] = p.Lean; a[1] = p.UaB; a[2] = p.FaB; a[3] = p.UaF; a[4] = p.FaF;
            a[5] = p.ThB; a[6] = p.ShB; a[7] = p.ThF; a[8] = p.ShF; a[9] = p.Rot; a[10] = p.Head; a[11] = p.Drop;
        }

        static float Ease(float t) => 1f - (1f - t) * (1f - t);

        /// <summary>Chooses the target pose and spring feel for this frame.</summary>
        Pose Target(out float omega, out float zeta, out bool trails)
        {
            omega = style.Omega; zeta = style.Zeta; trails = false;
            var st = f.State;
            Pose p;

            if (f.InStrike)
            {
                Poses.Frames(f.StrikePose, out var w, out var s);
                p = s;
                omega = style.Omega * 1.7f; zeta = 0.5f; trails = true;
                return p;
            }

            switch (st)
            {
                case FState.Idle:
                {
                    p = style.Idle;
                    float br = Mathf.Sin(idleTime * style.BounceRate * Mathf.PI * 2f);
                    p.Lean += br * style.Sway;
                    p.UaF += br * 4f; p.FaF -= br * 5f; p.UaB -= br * 3f;
                    p.Drop += (br * 0.5f + 0.5f) * style.Bounce;
                    return p;
                }
                case FState.Run:
                {
                    p = Poses.Run(runPhase);
                    p.Lean = p.Lean * style.RunLean + style.Idle.Lean * 0.35f;
                    if (style.SwordRun) { p.UaF = 30f; p.FaF = 60f; }
                    if (style.ArmsBack) { p.UaF = -60f; p.FaF = -40f; p.UaB = -70f; p.FaB = -50f; }
                    return p;
                }
                case FState.Air:
                    p = f.Vel.y > 1.5f ? Poses.Jump : Poses.Fall;
                    if (flip > 0f) { p = Poses.Jump; p.Rot = -360f * (1f - flip / 0.42f); omega = 40f; zeta = 0.8f; }
                    return p;
                case FState.Attack:
                {
                    var a = f.CurAttack;
                    if (a == null) return style.Idle;
                    var pose = style.MapStrike(a.Pose);
                    Poses.Frames(pose, out var wind, out var strike);
                    strike.Lean *= style.StrikeLean;
                    float t = f.StateTime;
                    if (t < a.Startup)
                    {
                        float k = Ease(t / Mathf.Max(0.001f, a.Startup));
                        p = Pose.Lerp(style.Idle, wind, Mathf.Min(1f, k * 1.4f));
                        omega = style.Omega * 1.3f;
                    }
                    else if (t < a.Startup + a.Active + a.Recovery * 0.35f)
                    {
                        p = strike;
                        omega = style.Omega * 2.1f; zeta = 0.42f;   // the snap, with overshoot for follow-through
                        trails = true;
                    }
                    else
                    {
                        p = Pose.Lerp(strike, style.Idle, 0.55f);
                        omega = style.Omega * 0.9f;
                    }
                    return p;
                }
                case FState.Cast:
                {
                    var ab = f.Casting;
                    if (ab == null) return style.Idle;
                    if (f.ChantLevel > 0 && !f.CastFired) { p = ab is DomainAb ? Poses.HandSign : Poses.Chant; omega *= 0.8f; return p; }
                    Poses.Frames(ab.Pose, out var wind, out var strike);
                    float k = f.CastProgress;
                    if (!f.CastFired) { p = Pose.Lerp(wind, strike, Ease(k) * 0.35f); if (ab is DomainAb) p = Poses.HandSign; }
                    else { p = strike; omega = style.Omega * 1.9f; zeta = 0.45f; trails = ab.Pose == FPose.Slash || ab.Pose == FPose.Stab || ab.Pose == FPose.Slam || ab.Pose == FPose.Throw; }
                    return p;
                }
                case FState.Block: return style.Guard;
                case FState.SimpleDomain: { p = Poses.Crouch; p.UaF = 50f; p.FaF = 150f; p.UaB = 45f; p.FaB = 155f; return p; }
                case FState.RCT: { p = Poses.Channel; p.Lean += Mathf.Sin(idleTime * 3f) * 2f; return p; }
                case FState.Hitstun:
                    p = Poses.Hurt;
                    if (!f.Grounded) { p.Rot = Mathf.Clamp(-f.Vel.y * 2.5f - 20f, -70f, 30f); }
                    omega = 28f; zeta = 0.38f;
                    return p;
                case FState.Knockdown:
                case FState.Dead:
                    p = Poses.Down; omega = 16f; zeta = 0.5f;
                    if (!f.Grounded) p.Rot = -40f - Mathf.Clamp(-f.Vel.y * 3f, -40f, 40f);
                    return p;
                case FState.GetUp: return Pose.Lerp(Poses.Crouch, style.Idle, f.StateTime / 0.32f);
                case FState.Dash: trails = true; omega = style.Omega * 1.6f; return Poses.Dash;
                case FState.Victory: return style.Victory;
                case FState.Locked: return f.M.Domains.Clashing ? Poses.HandSign : style.Idle;
                default: return style.Idle;
            }
        }

        void Simulate(float dt, float omega, float zeta)
        {
            // substep for stability with stiff springs
            int steps = Mathf.Clamp(Mathf.CeilToInt(dt / (1f / 150f)), 1, 6);
            float h = dt / steps;
            float k = omega * omega, c = 2f * zeta * omega;
            for (int s = 0; s < steps; s++)
                for (int i = 0; i < CH; i++)
                {
                    float target = tgt[i];
                    float diff = (i >= 1 && i <= 4) || i == 9 ? Mathf.DeltaAngle(x[i], target) : target - x[i];
                    float a = k * diff - c * v[i];
                    v[i] += a * h;
                    x[i] += v[i] * h;
                }
        }

        // =====================================================================
        public void Render(float alpha)
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f) * Mathf.Max(0.15f, VFX.SimSpeed);
            if (f.Hitstop > 0f) dt *= 0.15f;   // freeze on hitstop, a little shiver remains
            idleTime += dt;
            RenderPos = Vector2.LerpUnclamped(f.PrevPos, f.Pos, alpha);

            bool visible = f.State != FState.Respawning;
            root.SetActive(visible);
            if (!visible) return;

            // ---- locomotion bookkeeping
            if (f.State == FState.Run) runPhase += Mathf.Abs(f.Vel.x) * 1.85f * dt / Mathf.Max(0.7f, f.Size);
            if (!wasGrounded && f.Grounded && f.State != FState.Hitstun) v[11] += 3.2f * style.LandSquash;   // landing squash
            wasGrounded = f.Grounded;
            if (!f.Grounded && f.AirJumps < prevAirJumps) flip = 0.42f;
            prevAirJumps = f.AirJumps;
            if (flip > 0f) flip -= dt;

            var pose = Target(out float omega, out float zeta, out bool trails);
            // velocity lean (weight shifts into motion)
            if (f.Grounded && (f.State == FState.Run || f.State == FState.Idle))
                pose.Lean += Mathf.Clamp(f.Vel.x * f.Facing * 1.4f, -8f, 12f);
            Load(pose, tgt);

            // ---- hit recoil impulse: bigger hits whip the body harder
            if (f.HitImpulse > 0f)
            {
                float imp = f.HitImpulse;
                v[0] -= 260f * imp;
                v[10] -= 380f * imp;
                v[1] += Random.Range(-1f, 1f) * 500f * imp; v[3] += Random.Range(-1f, 1f) * 500f * imp;
                v[2] += Random.Range(-1f, 1f) * 400f * imp; v[4] += Random.Range(-1f, 1f) * 400f * imp;
                v[11] += 1.2f * imp;
                f.HitImpulse = 0f;
            }

            Simulate(dt, omega, zeta);

            // hitstop shiver
            Vector2 shiver = f.Hitstop > 0f ? new Vector2(Random.Range(-0.06f, 0.06f), Random.Range(-0.03f, 0.03f)) : Vector2.zero;
            if (style.Hover) { hover = 0.18f + Mathf.Sin(idleTime * 2.2f) * 0.08f; } else hover = 0f;
            Solve(RenderPos + shiver + Vector2.up * (f.Grounded && f.State != FState.Knockdown && f.State != FState.Dead ? hover : 0f));
            Draw(trails);
        }

        void Solve(Vector2 basePos)
        {
            float S = f.Size;
            int fc = f.Facing;
            float TH = 0.5f * S, SH = 0.5f * S, T = 0.85f * S, UA = 0.42f * S, FA = 0.42f * S, HR = 0.25f * S;
            float drop = Mathf.Clamp(x[11], -0.2f, 0.6f);
            float thB = x[5] + drop * 35f, shB = x[6] - drop * 55f, thF = x[7] + drop * 35f, shF = x[8] - drop * 55f;

            Vector2 Limb(float deg) { float r = deg * Mathf.Deg2Rad; return new Vector2(fc * Mathf.Sin(r), -Mathf.Cos(r)); }

            float lean = x[0] * Mathf.Deg2Rad;
            Vector2 up = new Vector2(fc * Mathf.Sin(lean), Mathf.Cos(lean));
            float dropB = TH * Mathf.Cos(thB * Mathf.Deg2Rad) + SH * Mathf.Cos(shB * Mathf.Deg2Rad);
            float dropF = TH * Mathf.Cos(thF * Mathf.Deg2Rad) + SH * Mathf.Cos(shF * Mathf.Deg2Rad);
            float hipH = Mathf.Max(0.22f * S, Mathf.Max(dropB, dropF));
            bool lying = Mathf.Abs(x[9]) > 50f && (f.State == FState.Knockdown || f.State == FState.Dead) && f.Grounded;
            if (lying) hipH = 0.28f * S;

            Hip = basePos + new Vector2(0f, hipH);
            Neck = Hip + up * T;
            float ht = (x[0] + x[10] * 0.6f) * Mathf.Deg2Rad;
            HeadC = Neck + new Vector2(fc * Mathf.Sin(ht), Mathf.Cos(ht)) * (HR * 1.25f);
            ElbowB = Neck + Limb(x[1]) * UA; HandB = ElbowB + Limb(x[2]) * FA;
            ElbowF = Neck + Limb(x[3]) * UA; HandF = ElbowF + Limb(x[4]) * FA;
            KneeB = Hip + Limb(thB) * TH; FootB = KneeB + Limb(shB) * SH;
            KneeF = Hip + Limb(thF) * TH; FootF = KneeF + Limb(shF) * SH;

            float rot = -x[9] * fc;
            if (Mathf.Abs(rot) > 0.01f)
            {
                Vector2 pivot = Hip;
                Neck = RotAround(Neck, pivot, rot); HeadC = RotAround(HeadC, pivot, rot);
                ElbowB = RotAround(ElbowB, pivot, rot); HandB = RotAround(HandB, pivot, rot);
                ElbowF = RotAround(ElbowF, pivot, rot); HandF = RotAround(HandF, pivot, rot);
                KneeB = RotAround(KneeB, pivot, rot); FootB = RotAround(FootB, pivot, rot);
                KneeF = RotAround(KneeF, pivot, rot); FootF = RotAround(FootF, pivot, rot);
            }
        }

        static Vector2 RotAround(Vector2 p, Vector2 c, float deg)
        {
            float r = deg * Mathf.Deg2Rad, cs = Mathf.Cos(r), sn = Mathf.Sin(r);
            Vector2 d = p - c;
            return c + new Vector2(d.x * cs - d.y * sn, d.x * sn + d.y * cs);
        }

        // =====================================================================
        void Draw(bool trails)
        {
            var look = f.Def.Look;
            Color body = look.Body.a <= 0f ? new Color(0.94f, 0.94f, 0.97f) : look.Body;
            Color glow = look.Aura;
            float glowA = 0.22f;

            // status tints
            if (f.Has(StatusType.Burn)) { body = Color.Lerp(body, new Color(1f, 0.55f, 0.2f), 0.5f + 0.3f * Mathf.Sin(idleTime * 30f)); VFX.AuraTick(RenderPos, 2f * f.Size, new Color(1f, 0.5f, 0.1f), 0.5f); }
            if (f.Has(StatusType.Freeze)) body = new Color(0.65f, 0.9f, 1f);
            if (f.Has(StatusType.Paralysis) || f.Has(StatusType.Stun)) { if (Random.value < 0.15f) VFX.Info(HeadC); }
            if (f.Has(StatusType.Sealed) || f.Has(StatusType.Burnout)) { glowA *= 0.3f; if (Random.value < 0.04f) VFX.Puff(HeadC + Vector2.up * 0.3f, new Color(0.3f, 0.3f, 0.3f), 0.4f); }
            if (f.Has(StatusType.Jackpot)) { rainbow += Time.unscaledDeltaTime * 0.8f; glow = Color.HSVToRGB(Mathf.Repeat(rainbow, 1f), 0.8f, 1f); glowA = 0.45f; VFX.AuraTick(RenderPos, 2.2f, glow, 0.6f); }
            if (f.Has(StatusType.Zone)) { glow = Color.Lerp(new Color(0.9f, 0.05f, 0.1f), Color.black, Mathf.PingPong(idleTime * 3f, 1f)); glowA = 0.4f; VFX.AuraTick(RenderPos, 2f, new Color(1f, 0.1f, 0.15f), 0.35f); }
            if (f.Has(StatusType.Gravity) || f.Has(StatusType.Sink)) body = Color.Lerp(body, new Color(0.3f, 0.2f, 0.45f), 0.4f);
            if (f.Casting != null || f.State == FState.RCT) { glowA = 0.4f; VFX.AuraTick(RenderPos, 2.1f * f.Size, f.Casting != null ? f.Casting.Color : new Color(0.6f, 1f, 0.75f), f.ChantLevel > 0 ? 1f : 0.55f); }
            if (f.HpFrac < 0.25f && !f.Dead) glowA += 0.12f * Mathf.Sin(idleTime * 8f);
            if (f.FlashTimer > 0f) { body = Color.white; glow = Color.white; glowA = 0.6f; }
            bool blink = (f.Invuln > 0.2f || f.Has(StatusType.Invuln)) && Mathf.Repeat(idleTime * 14f, 1f) > 0.5f;
            if (blink) body.a *= 0.45f;

            // impact frame ink
            var ink = ImpactFrames.Ink;
            if (ink == ImpactFrames.FighterInk.White) { body = Color.white; glowA = 0f; }
            else if (ink == ImpactFrames.FighterInk.Black) { body = Color.black; glowA = 0f; }

            Color g = glow.WithA(glowA);

            SetPts(legs, 5, FootB, KneeB, Hip, KneeF, FootF); Col(legs, body);
            SetPts(torso, 2, Hip, Neck); Col(torso, body);
            SetPts(arms, 5, HandB, ElbowB, Neck, ElbowF, HandF); Col(arms, body);
            Circle(head, HeadC, 0.25f * f.Size, 20); Col(head, body);

            legsGlow.enabled = torsoGlow.enabled = armsGlow.enabled = headGlow.enabled = glowA > 0.01f && Settings.VfxQuality > 0;
            if (legsGlow.enabled)
            {
                SetPts(legsGlow, 5, FootB, KneeB, Hip, KneeF, FootF); Col(legsGlow, g);
                SetPts(torsoGlow, 2, Hip, Neck); Col(torsoGlow, g);
                SetPts(armsGlow, 5, HandB, ElbowB, Neck, ElbowF, HandF); Col(armsGlow, g);
                Circle(headGlow, HeadC, 0.25f * f.Size, 20); Col(headGlow, g);
            }

            if (extraArms.enabled)
            {
                Vector2 sh = Vector2.Lerp(Neck, Hip, 0.3f);
                float S = f.Size;
                Vector2 eb = sh + Dir(x[1] + 40f) * 0.38f * S, hb = eb + Dir(x[2] + 30f) * 0.38f * S;
                Vector2 ef = sh + Dir(x[3] - 30f) * 0.38f * S, hf = ef + Dir(x[4] - 20f) * 0.38f * S;
                SetPts(extraArms, 5, hb, eb, sh, ef, hf); Col(extraArms, body);
            }

            DrawHair(body);
            DrawFace(body, ink);
            DrawWeapon(body, ink);
            DrawCrown(ink);
            DrawMarker();

            // smear trails on strikes/dashes
            handTrail.transform.position = HandF;
            footTrail.transform.position = FootF;
            bool kicking = f.CurAttack != null && (f.CurAttack.Pose == FPose.Kick || f.CurAttack.Pose == FPose.AirKick || f.CurAttack.Pose == FPose.Sweep || f.CurAttack.Pose == FPose.DiveKick);
            handTrail.emitting = trails && !kicking && Settings.VfxQuality > 0;
            footTrail.emitting = trails && (kicking || f.State == FState.Dash) && Settings.VfxQuality > 0;
            var tc = f.Has(StatusType.Zone) ? new Color(1f, 0.1f, 0.15f) : look.Aura;
            handTrail.startColor = tc.WithA(0.8f); footTrail.startColor = tc.WithA(0.8f);

            // black flash rhythm cue: the fist crackles red-black while the window is open
            if (f.BlackFlashWindowOpen && Random.value < 0.8f) VFX.Sparks(HandF, Random.value < 0.5f ? Color.black : new Color(1f, 0.1f, 0.15f), 1, 2f, 0.1f);
        }

        Vector2 Dir(float deg) { float r = deg * Mathf.Deg2Rad; return new Vector2(f.Facing * Mathf.Sin(r), -Mathf.Cos(r)); }

        static readonly Vector2[] pts = new Vector2[12];
        /// <summary>Allocation-free polyline setter (up to 11 points).</summary>
        static void SetPts(LineRenderer lr, int n, Vector2 a, Vector2 b, Vector2 c = default, Vector2 d = default, Vector2 e = default, Vector2 f = default,
                           Vector2 g = default, Vector2 h = default, Vector2 i = default, Vector2 j = default, Vector2 k = default)
        {
            pts[0] = a; pts[1] = b; pts[2] = c; pts[3] = d; pts[4] = e; pts[5] = f; pts[6] = g; pts[7] = h; pts[8] = i; pts[9] = j; pts[10] = k;
            if (lr.positionCount != n) lr.positionCount = n;
            for (int q = 0; q < n; q++) lr.SetPosition(q, pts[q]);
        }

        static void Circle(LineRenderer lr, Vector2 c, float r, int n)
        {
            if (lr.positionCount != n) lr.positionCount = n;
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(c.x + Mathf.Cos(a) * r, c.y + Mathf.Sin(a) * r, 0f));
            }
        }

        static void Col(LineRenderer lr, Color c) { lr.startColor = c; lr.endColor = c; }

        // ---------------------------------------------------------------- hair (with secondary motion)
        void DrawHair(Color body)
        {
            var look = f.Def.Look;
            float r = 0.25f * f.Size;
            int fc = f.Facing;
            Vector2 c = HeadC;
            Vector2 P(float deg, float rr) { float a = deg * Mathf.Deg2Rad; return c + new Vector2(fc * Mathf.Cos(a) * rr, Mathf.Sin(a) * rr); }

            // physics tail (ponytails, long hair, braids)
            Vector2 anchor = P(150f, r * 1.05f);
            if (!tailInit) { tailPos = anchor + Vector2.down * r * 2f; tailInit = true; }
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            Vector2 want = anchor + new Vector2(-fc * r * 1.4f, -r * 1.6f) - (f.Vel * 0.03f);
            tailVel += ((want - tailPos) * 140f - tailVel * 11f) * dt;
            tailPos += tailVel * dt;

            Color hc = Color.Lerp(body, look.Accent.a > 0f ? look.Accent : body, 0.6f);
            hair.enabled = look.Hair != Hair.None;
            switch (look.Hair)
            {
                case Hair.Spiky: SetPts(hair, 9, P(200, r), P(175, r * 1.7f), P(160, r * 1.05f), P(135, r * 1.85f), P(118, r * 1.05f), P(95, r * 1.9f), P(78, r * 1.05f), P(55, r * 1.7f), P(35, r)); break;
                case Hair.Messy: SetPts(hair, 11, P(210, r), P(195, r * 1.8f), P(170, r * 1.1f), P(150, r * 2f), P(128, r * 1.1f), P(108, r * 2.1f), P(88, r * 1.1f), P(68, r * 1.9f), P(48, r * 1.05f), P(30, r * 1.55f), P(20, r)); break;
                case Hair.Wild: SetPts(hair, 9, P(220, r), P(205, r * 2.1f), P(180, r * 1.1f), P(160, r * 2.3f), P(130, r * 1.1f), P(100, r * 2.4f), P(75, r * 1.1f), P(45, r * 1.9f), P(25, r)); break;
                case Hair.Slick: SetPts(hair, 5, P(30, r * 1.05f), P(70, r * 1.25f), P(110, r * 1.3f), P(150, r * 1.2f), P(185, r * 1.05f)); break;
                case Hair.Short: SetPts(hair, 4, P(25, r * 1.05f), P(80, r * 1.2f), P(140, r * 1.15f), P(190, r * 1.02f)); break;
                case Hair.Bob: SetPts(hair, 6, P(-20, r * 1.15f), P(30, r * 1.2f), P(90, r * 1.25f), P(150, r * 1.25f), P(210, r * 1.2f), P(250, r * 1.15f)); break;
                case Hair.Ponytail: case Hair.Braid:
                    SetPts(hair, 6, P(30, r * 1.05f), P(90, r * 1.2f), P(150, r * 1.1f), anchor, Vector2.Lerp(anchor, tailPos, 0.5f) + Vector2.down * 0.05f, tailPos); break;
                case Hair.Long:
                    SetPts(hair, 6, P(20, r * 1.1f), P(90, r * 1.25f), P(160, r * 1.15f), P(210, r * 1.1f), Vector2.Lerp(anchor, tailPos, 0.6f), tailPos + Vector2.down * r); break;
                case Hair.Twin:
                    SetPts(hair, 5, tailPos, P(160, r * 1.1f), P(90, r * 1.2f), P(20, r * 1.1f), c + new Vector2(fc * r * 1.2f, -r * 1.8f)); break;
                case Hair.Bun:
                    SetPts(hair, 8, P(30, r * 1.05f), P(90, r * 1.15f), P(140, r * 1.15f), P(120, r * 1.55f), P(150, r * 1.8f), P(175, r * 1.5f), P(150, r * 1.15f), P(190, r * 1.05f)); break;
                case Hair.TopKnot:
                    SetPts(hair, 6, P(40, r * 1.05f), P(90, r * 1.1f), P(95, r * 1.6f), P(80, r * 1.8f), P(100, r * 1.9f), P(140, r * 1.1f)); break;
                case Hair.Horns:
                    SetPts(hair, 7, P(150, r * 0.95f), P(125, r * 1.9f), P(115, r * 2.4f), P(100, r * 1.05f), P(70, r * 1.9f), P(55, r * 2.5f), P(60, r)); break;
                case Hair.Volcano:
                    SetPts(hair, 6, P(160, r), P(125, r * 2.1f), P(105, r * 2.3f), P(75, r * 2.3f), P(55, r * 2.1f), P(20, r)); break;
                case Hair.Ears:
                    SetPts(hair, 8, P(150, r), P(140, r * 1.45f), P(118, r * 1.5f), P(110, r * 1.05f), P(70, r * 1.05f), P(62, r * 1.5f), P(40, r * 1.45f), P(30, r)); break;
                case Hair.Hood:
                    SetPts(hair, 5, P(-40, r * 1.35f), P(20, r * 1.4f), P(90, r * 1.45f), P(160, r * 1.4f), P(230, r * 1.35f)); break;
                case Hair.Afro:
                    SetPts(hair, 7, P(10, r * 1.2f), P(45, r * 2f), P(70, r * 2.4f), P(100, r * 2.5f), P(130, r * 2.2f), P(160, r * 1.6f), P(185, r * 1.1f)); break;
                default: hair.enabled = false; break;
            }
            Col(hair, ImpactFrames.Ink == ImpactFrames.FighterInk.Black ? Color.black : ImpactFrames.Ink == ImpactFrames.FighterInk.White ? Color.white : hc);
        }

        // ---------------------------------------------------------------- face (eye, marks)
        void DrawFace(Color body, ImpactFrames.FighterInk ink)
        {
            var look = f.Def.Look;
            float r = 0.25f * f.Size;
            int fc = f.Facing;
            Vector2 c = HeadC;
            Vector2 P(float deg, float rr) { float a = deg * Mathf.Deg2Rad; return c + new Vector2(fc * Mathf.Cos(a) * rr, Mathf.Sin(a) * rr); }

            // glowing eye slit (hidden under a blindfold)
            bool blind = (look.Face & FaceMark.Blindfold) != 0;
            eye.enabled = !blind || f.State == FState.Locked || (f.Casting is DomainAb);
            Color ec = ink == ImpactFrames.FighterInk.Normal ? look.Aura : ink == ImpactFrames.FighterInk.White ? Color.black : Color.white;
            if (f.Dead) ec = new Color(0.4f, 0.4f, 0.4f);
            SetPts(eye, 2, P(12f, r * 0.45f), P(5f, r * 0.85f));
            Col(eye, ec);

            face.enabled = true;
            var fm = look.Face;
            if ((fm & FaceMark.Blindfold) != 0) SetPts(face, 4, P(180f, r * 1.02f), P(170f, r * 1.05f), P(15f, r * 1.05f), P(0f, r * 1.12f));
            else if ((fm & FaceMark.Goggles) != 0) SetPts(face, 5, P(175f, r), P(20f, r * 1.02f), P(20f, r * 1.25f), P(-5f, r * 1.25f), P(-5f, r));
            else if ((fm & FaceMark.Sunglasses) != 0) SetPts(face, 4, P(160f, r), P(15f, r * 1.05f), P(5f, r * 1.2f), P(-8f, r * 1.05f));
            else if ((fm & FaceMark.Glasses) != 0) SetPts(face, 5, P(170f, r), P(18f, r * 1.02f), P(18f, r * 1.2f), P(-8f, r * 1.2f), P(-8f, r * 1.02f));
            else if ((fm & FaceMark.Marks) != 0) SetPts(face, 4, P(-5f, r * 0.55f), P(-15f, r * 0.95f), P(-25f, r * 0.6f), P(-40f, r * 0.9f));
            else if ((fm & FaceMark.Stitches) != 0) SetPts(face, 7, P(150f, r * 0.75f), P(110f, r * 0.62f), P(105f, r * 0.85f), P(90f, r * 0.62f), P(85f, r * 0.85f), P(70f, r * 0.62f), P(40f, r * 0.75f));
            else if ((fm & FaceMark.Patches) != 0) SetPts(face, 4, P(80f, r), P(60f, r * 0.4f), P(-30f, r * 0.5f), P(-70f, r));
            else if ((fm & FaceMark.Collar) != 0) SetPts(face, 4, Neck + new Vector2(-fc * 0.15f, -0.05f), P(-60f, r * 1.15f), P(-20f, r * 1.2f), P(5f, r * 1.0f));
            else if ((fm & FaceMark.Mask) != 0) SetPts(face, 3, P(-80f, r * 1.05f), P(-30f, r * 1.1f), P(-5f, r * 1.05f));
            else if ((fm & FaceMark.Scar) != 0) SetPts(face, 2, P(-20f, r * 0.75f), P(-45f, r * 1.0f));
            else if ((fm & FaceMark.Beard) != 0) SetPts(face, 4, P(-10f, r), P(-60f, r * 1.4f), P(-100f, r * 1.3f), P(-140f, r));
            else face.enabled = false;
            Color fcCol = (fm & (FaceMark.Marks | FaceMark.Stitches | FaceMark.Patches)) != 0 ? look.Accent : body;
            if ((fm & FaceMark.Blindfold) != 0) fcCol = look.Accent.a > 0f ? look.Accent : Color.white;
            if (ink != ImpactFrames.FighterInk.Normal) fcCol = ink == ImpactFrames.FighterInk.White ? Color.white : Color.black;
            Col(face, fcCol);

            if ((fm & FaceMark.ExtraEyes) != 0 && eye.enabled)
            {
                // second set of eyes beneath the first
                if (eye.positionCount != 5) eye.positionCount = 5;
                eye.SetPosition(0, P(12f, r * 0.45f)); eye.SetPosition(1, P(5f, r * 0.85f));
                eye.SetPosition(2, P(-5f, r * 0.6f));
                eye.SetPosition(3, P(-14f, r * 0.45f)); eye.SetPosition(4, P(-18f, r * 0.85f));
            }
            else if (eye.positionCount != 2) eye.positionCount = 2;
        }

        // ---------------------------------------------------------------- weapons
        void DrawWeapon(Color body, ImpactFrames.FighterInk ink)
        {
            var look = f.Def.Look;
            var wpn = look.Weapon;
            if (f.Has(StatusType.Executioner)) wpn = Weapon.Sword;
            Vector2 h = HandF;
            Vector2 d = (HandF - ElbowF).normalized;
            if (d.sqrMagnitude < 0.01f) d = new Vector2(f.Facing, 0f);
            Vector2 n = new Vector2(-d.y, d.x);
            float S = f.Size;
            weapon.widthMultiplier = 0.08f * S;
            weapon.enabled = true;
            Color wc = look.Accent.a > 0f ? Color.Lerp(body, look.Accent, 0.5f) : body;
            switch (wpn)
            {
                case Weapon.Katana: SetPts(weapon, 5, h - d * 0.18f * S, h + n * 0.1f * S, h - n * 0.1f * S, h, h + d * 1.15f * S); wc = new Color(0.85f, 0.88f, 0.95f); break;
                case Weapon.Sword: SetPts(weapon, 5, h - d * 0.2f * S, h + n * 0.14f * S, h - n * 0.14f * S, h, h + d * 1.6f * S); weapon.widthMultiplier = 0.14f * S; wc = f.Has(StatusType.Executioner) ? new Color(1f, 0.85f, 0.4f) : new Color(0.85f, 0.88f, 0.95f); break;
                case Weapon.Spear: SetPts(weapon, 5, h - d * 0.9f * S, h + d * 1.3f * S, h + d * 1.15f * S + n * 0.12f * S, h + d * 1.3f * S, h + d * 1.15f * S - n * 0.12f * S); break;
                case Weapon.Polearm: SetPts(weapon, 4, h - d * 1.0f * S, h + d * 1.2f * S, h + d * 1.25f * S + n * 0.25f * S, h + d * 1.6f * S); break;
                case Weapon.Cloud:
                {
                    float sw = Mathf.Sin(idleTime * 9f) * 0.25f;
                    Vector2 a = h + d * 0.55f * S, b = a + ProjectileAb.Rotate(d, 25f + sw * 60f) * 0.55f * S, c = b + ProjectileAb.Rotate(d, 50f + sw * 90f) * 0.55f * S;
                    SetPts(weapon, 4, h - d * 0.4f * S, a, b, c); weapon.widthMultiplier = 0.11f * S; wc = new Color(0.75f, 0.6f, 0.4f); break;
                }
                case Weapon.Staff: SetPts(weapon, 2, h - d * 0.8f * S, h + d * 0.9f * S); weapon.widthMultiplier = 0.09f * S; break;
                case Weapon.Hammer: SetPts(weapon, 4, h - d * 0.1f * S, h + d * 0.4f * S, h + d * 0.4f * S + n * 0.18f * S, h + d * 0.4f * S - n * 0.12f * S); weapon.widthMultiplier = 0.1f * S; break;
                case Weapon.Gavel: SetPts(weapon, 4, h, h + d * 0.35f * S, h + d * 0.35f * S + n * 0.14f * S, h + d * 0.35f * S - n * 0.14f * S); weapon.widthMultiplier = 0.12f * S; wc = new Color(0.65f, 0.45f, 0.3f); break;
                case Weapon.Cleaver: SetPts(weapon, 2, h - d * 0.15f * S, h + d * 0.85f * S); weapon.widthMultiplier = 0.17f * S; wc = new Color(0.9f, 0.88f, 0.8f); break;
                case Weapon.Rope: case Weapon.Chain:
                {
                    int segs = 7;
                    if (weapon.positionCount != segs) weapon.positionCount = segs;
                    for (int i = 0; i < segs; i++)
                    {
                        float t = i / (float)(segs - 1);
                        Vector2 p = h + d * t * 1.2f * S + n * Mathf.Sin(t * 9f + idleTime * 10f) * 0.12f * S * t;
                        weapon.SetPosition(i, p);
                    }
                    wc = wpn == Weapon.Rope ? new Color(0.08f, 0.08f, 0.1f) : new Color(0.7f, 0.7f, 0.75f);
                    break;
                }
                case Weapon.Bow: SetPts(weapon, 3, h + n * 0.55f * S, h + d * 0.18f * S, h - n * 0.55f * S); break;
                case Weapon.Broom: SetPts(weapon, 5, h + d * 0.6f * S, h - d * 0.9f * S, h - d * 1.05f * S + n * 0.15f * S, h - d * 0.9f * S, h - d * 1.05f * S - n * 0.15f * S); wc = new Color(0.7f, 0.55f, 0.35f); break;
                case Weapon.Revolver: SetPts(weapon, 3, h - n * 0.12f * S, h, h + d * 0.35f * S); weapon.widthMultiplier = 0.09f * S; wc = new Color(0.4f, 0.4f, 0.45f); break;
                case Weapon.Guitar: SetPts(weapon, 6, h + d * 0.7f * S, h - d * 0.1f * S, h - d * 0.25f * S + n * 0.2f * S, h - d * 0.45f * S, h - d * 0.25f * S - n * 0.2f * S, h - d * 0.1f * S); wc = new Color(0.8f, 0.3f, 0.25f); break;
                case Weapon.Fan: SetPts(weapon, 5, h, h + ProjectileAb.Rotate(d, 35f) * 0.45f * S, h + d * 0.5f * S, h + ProjectileAb.Rotate(d, -35f) * 0.45f * S, h); break;
                case Weapon.Claws: SetPts(weapon, 5, h + ProjectileAb.Rotate(d, 18f) * 0.4f * S, h, h + d * 0.45f * S, h, h + ProjectileAb.Rotate(d, -18f) * 0.4f * S); weapon.widthMultiplier = 0.05f * S; wc = look.Aura; break;
                case Weapon.Axe: SetPts(weapon, 5, h - d * 0.2f * S, h + d * 0.8f * S, h + d * 0.7f * S + n * 0.3f * S, h + d * 0.5f * S + n * 0.28f * S, h + d * 0.55f * S); weapon.widthMultiplier = 0.09f * S; break;
                case Weapon.Pen: SetPts(weapon, 2, h, h + d * 0.3f * S); weapon.widthMultiplier = 0.05f * S; break;
                case Weapon.Receipts: SetPts(weapon, 5, h, h + n * 0.15f * S, h + n * 0.15f * S + d * 0.22f * S, h + d * 0.22f * S, h); weapon.widthMultiplier = 0.04f * S; wc = Color.white; break;
                case Weapon.Phone: SetPts(weapon, 2, h, h + n * 0.2f * S); weapon.widthMultiplier = 0.11f * S; wc = new Color(0.3f, 0.35f, 0.45f); break;
                default:
                    if ((look.Face & FaceMark.Wings) != 0)
                    {
                        Vector2 back = Vector2.Lerp(Neck, Hip, 0.2f) + new Vector2(-f.Facing * 0.1f, 0f);
                        float flap = Mathf.Sin(idleTime * 5f) * 0.25f;
                        SetPts(weapon, 5, back + new Vector2(-f.Facing * 0.9f * S, (0.9f + flap) * S), back, back + new Vector2(-f.Facing * 1.1f * S, (0.2f + flap) * S), back, back + new Vector2(-f.Facing * 0.8f * S, (-0.4f + flap * 0.5f) * S));
                        weapon.widthMultiplier = 0.06f * S;
                        wc = Color.Lerp(Color.white, look.Aura, 0.3f);
                    }
                    else weapon.enabled = false;
                    break;
            }
            if (ink != ImpactFrames.FighterInk.Normal) wc = ink == ImpactFrames.FighterInk.White ? Color.white : Color.black;
            Col(weapon, wc);
        }

        // ---------------------------------------------------------------- halo / wheel / crown
        void DrawCrown(ImpactFrames.FighterInk ink)
        {
            var fm = f.Def.Look.Face;
            float r = 0.25f * f.Size;
            Vector2 top = HeadC + Vector2.up * r * 2.2f;
            crown.enabled = true;
            if ((fm & FaceMark.Wheel) != 0)
            {
                // the dharma wheel: rim plus eight spokes; turns when adapting
                float spin = idleTime * 20f + f.Adapt[0] * 400f + f.Adapt[(int)DamageType.Slash] * 400f + f.Adapt[(int)DamageType.Energy] * 400f;
                int n = 8;
                int count = n * 3 + 1;
                if (crown.positionCount != count) crown.positionCount = count;
                float R = r * 1.6f;
                int k = 0;
                for (int i = 0; i < n; i++)
                {
                    float a0 = (spin + i * 45f) * Mathf.Deg2Rad, a1 = (spin + (i + 1) * 45f) * Mathf.Deg2Rad;
                    crown.SetPosition(k++, top + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0) * 0.45f) * R);
                    crown.SetPosition(k++, top);
                    crown.SetPosition(k++, top + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1) * 0.45f) * R);
                }
                crown.SetPosition(k, top + new Vector2(Mathf.Cos(spin * Mathf.Deg2Rad), Mathf.Sin(spin * Mathf.Deg2Rad) * 0.45f) * R);
                Col(crown, new Color(1f, 0.92f, 0.65f));
            }
            else if ((fm & FaceMark.Halo) != 0)
            {
                int n = 16;
                if (crown.positionCount != n + 1) crown.positionCount = n + 1;
                for (int i = 0; i <= n; i++)
                {
                    float a = i / (float)n * Mathf.PI * 2f;
                    crown.SetPosition(i, top + new Vector2(Mathf.Cos(a) * r * 1.1f, Mathf.Sin(a) * r * 0.35f));
                }
                Col(crown, new Color(1f, 0.95f, 0.7f));
            }
            else if ((fm & FaceMark.Crown) != 0)
            {
                SetPts(crown, 6, top + new Vector2(-r, -r * 0.6f), top + new Vector2(-r * 0.6f, r * 0.3f), top + new Vector2(-r * 0.2f, -r * 0.3f), top + new Vector2(r * 0.2f, r * 0.4f), top + new Vector2(r * 0.6f, -r * 0.3f), top + new Vector2(r, r * 0.3f));
                Col(crown, f.Def.Look.Aura);
            }
            else crown.enabled = false;
            if (ink != ImpactFrames.FighterInk.Normal && crown.enabled) Col(crown, ink == ImpactFrames.FighterInk.White ? Color.white : Color.black);
        }

        void DrawMarker()
        {
            bool show = f.M != null && !f.M.Config.Demo && !f.Dead && f.M.Fighters.Count > 1;
            marker.enabled = show;
            if (!show) return;
            Vector2 top = HeadC + Vector2.up * (0.25f * f.Size * 2.2f + ((f.Def.Look.Face & (FaceMark.Wheel | FaceMark.Halo | FaceMark.Crown)) != 0 ? 0.5f : 0f) + (f.Def.Look.Hair == Hair.Horns || f.Def.Look.Hair == Hair.Volcano || f.Def.Look.Hair == Hair.Afro ? 0.35f : 0.15f));
            SetPts(marker, 3, top + new Vector2(-0.16f, 0.2f), top, top + new Vector2(0.16f, 0.2f));
            Col(marker, f.TeamColor);
        }
    }

    /// <summary>Per-stance animation personality.</summary>
    public struct StyleProfile
    {
        public Pose Idle, Guard, Victory;
        public float Omega, Zeta, Bounce, BounceRate, Sway, RunLean, StrikeLean, LandSquash;
        public bool SwordRun, ArmsBack, Hover;
        public Stance Stance;

        public FPose MapStrike(FPose p)
        {
            switch (Stance)
            {
                case Stance.Swordsman:
                    if (p == FPose.Jab || p == FPose.Hook) return FPose.Slash;
                    if (p == FPose.Cross) return FPose.Stab;
                    if (p == FPose.HeavyPunch) return FPose.Slash;
                    if (p == FPose.AirKick) return FPose.AirSlash;
                    return p;
                case Stance.Feral:
                    if (p == FPose.Jab || p == FPose.Cross || p == FPose.Hook) return FPose.Slash;
                    return p;
                case Stance.Elegant:
                    if (p == FPose.Jab) return FPose.Palm;
                    if (p == FPose.Hook) return FPose.Kick;
                    return p;
                case Stance.Martial:
                    if (p == FPose.Hook) return FPose.Palm;
                    return p;
                case Stance.Wrestler:
                    if (p == FPose.Cross) return FPose.Grab;
                    if (p == FPose.HeavyPunch) return FPose.Slam;
                    return p;
                case Stance.Caster:
                    if (p == FPose.Cross) return FPose.Palm;
                    return p;
                default: return p;
            }
        }

        public static StyleProfile For(Stance s)
        {
            var p = new StyleProfile
            {
                Stance = s, Idle = Poses.Idle, Guard = Poses.Block, Victory = Poses.Victory,
                Omega = 26f, Zeta = 0.6f, Bounce = 0.04f, BounceRate = 1.1f, Sway = 1.5f, RunLean = 1f, StrikeLean = 1f, LandSquash = 1f
            };
            switch (s)
            {
                case Stance.Brawler:
                    p.Bounce = 0.09f; p.BounceRate = 2.4f; p.Omega = 28f; p.Zeta = 0.55f;
                    break;
                case Stance.Elegant:
                    p.Idle = Pose.P(-4, -12, 12, -8, 18, -8, -10, 10, 2, 0, -6);
                    p.Bounce = 0.02f; p.BounceRate = 0.6f; p.Sway = 2.5f; p.Zeta = 0.68f; p.RunLean = 0.6f; p.ArmsBack = false;
                    p.Victory = Pose.P(-8, -12, 12, -8, 18, -10, -10, 12, 2, 0, -15);
                    break;
                case Stance.Swordsman:
                    p.Idle = Pose.P(14, 30, 110, 55, 82, -36, -62, 40, -8, 0, 0, 0.1f);
                    p.Guard = Pose.P(4, 40, 150, 70, 150, -32, -50, 34, 0, 0, 0, 0.1f);
                    p.Bounce = 0.03f; p.BounceRate = 0.9f; p.SwordRun = true; p.StrikeLean = 1.15f; p.Omega = 30f; p.Zeta = 0.5f;
                    break;
                case Stance.Brute:
                    p.Idle = Pose.P(16, 15, 50, 25, 60, -28, -30, 30, 0);
                    p.Omega = 19f; p.Zeta = 0.7f; p.Bounce = 0.05f; p.BounceRate = 0.7f; p.StrikeLean = 1.4f; p.LandSquash = 1.8f; p.RunLean = 0.8f;
                    break;
                case Stance.Feral:
                    p.Idle = Pose.P(30, 5, 30, 20, 40, -32, -72, 42, -22, 0, -22, 0.05f);
                    p.Omega = 32f; p.Zeta = 0.45f; p.Bounce = 0.06f; p.BounceRate = 1.8f; p.Sway = 3f; p.RunLean = 1.4f; p.StrikeLean = 1.3f;
                    p.Victory = Pose.P(-10, 140, 120, 150, 130, -20, -30, 20, 0, 0, -20);
                    break;
                case Stance.Caster:
                    p.Idle = Pose.P(2, 10, 60, 60, 112, -16, -22, 18, 0);
                    p.Bounce = 0.03f; p.BounceRate = 0.8f; p.Zeta = 0.65f;
                    break;
                case Stance.Agile:
                    p.Idle = Pose.P(24, -30, 30, 50, 120, -42, -92, 52, -26, 0, -10, 0.12f);
                    p.Omega = 34f; p.Zeta = 0.52f; p.Bounce = 0.1f; p.BounceRate = 3f; p.RunLean = 1.6f; p.ArmsBack = true;
                    break;
                case Stance.Floaty:
                    p.Idle = Pose.P(0, -60, -70, 70, 80, -6, -20, 10, -12);
                    p.Hover = true; p.Omega = 18f; p.Zeta = 0.75f; p.Sway = 4f; p.BounceRate = 0.5f;
                    break;
                case Stance.Mechanical:
                    p.Idle = Pose.P(0, 20, 90, 30, 90, -14, -14, 14, 0);
                    p.Omega = 40f; p.Zeta = 1f; p.Bounce = 0f; p.Sway = 0f; p.LandSquash = 0.4f;
                    break;
                case Stance.Wrestler:
                    p.Idle = Pose.P(20, 60, 120, 70, 110, -34, -46, 38, -4);
                    p.Omega = 22f; p.Zeta = 0.62f; p.Bounce = 0.06f; p.BounceRate = 1.4f; p.StrikeLean = 1.3f; p.LandSquash = 1.6f;
                    break;
                case Stance.Martial:
                    p.Idle = Pose.P(6, 40, 150, 70, 100, -24, -36, 28, -2);
                    p.Omega = 27f; p.Zeta = 0.62f; p.Bounce = 0.03f; p.BounceRate = 1f;
                    break;
            }
            return p;
        }
    }
}
