using UnityEngine;

namespace ProjectSorcery
{
    /// <summary>
    /// Renders a fighter as a glowing stick figure. All motion comes from <see cref="RigSolver"/> (keyframed moves,
    /// springs, IK); this class only draws it: body lines, glow, hair, face, weapon, smear arcs, afterimages and
    /// the contact effects (dust, ground cracks, aura bursts). Purely visual: never writes gameplay state.
    /// </summary>
    public sealed class StickRig
    {
        readonly Fighter f;
        public readonly RigSolver S;
        readonly GameObject root;
        readonly LineRenderer legs, torso, arms, head, caRed, caCyan, legsGlow, armsGlow, torsoGlow, headGlow, hair, face, eye, weapon, extraArms, crown, marker;
        readonly Smear smearA, smearB;
        float idleTime, ghostClock, chargeFx;
        Vector2 tailPos, tailVel;
        bool tailInit;
        float rainbow;

        public Vector2 RenderPos => S.RenderPos;
        public Vector2 Hip => S.Hip;
        public Vector2 Neck => S.Neck;
        public Vector2 HeadC => S.HeadC;
        public Vector2 HandF => S.HandF;
        public Vector2 HandB => S.HandB;
        public Vector2 ElbowF => S.ElbowF;
        public Vector2 ElbowB => S.ElbowB;
        public Vector2 KneeF => S.KneeF;
        public Vector2 KneeB => S.KneeB;
        public Vector2 FootF => S.FootF;
        public Vector2 FootB => S.FootB;

        public StickRig(Transform parent, Fighter fighter)
        {
            f = fighter;
            S = new RigSolver(fighter);
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
            caRed = Art.NewLine(t, "AberrationR", Art.AddLine, Art.OrderFighterGlow, w * 0.9f, CaPts);
            caCyan = Art.NewLine(t, "AberrationC", Art.AddLine, Art.OrderFighterGlow, w * 0.9f, CaPts);
            hair.numCapVertices = 2; face.numCapVertices = 2;
            extraArms.enabled = f.Def.Look.FourArms;
            smearA = new Smear(t, "SmearA");
            smearB = new Smear(t, "SmearB");
        }

        public LineRenderer Line(int i) => i == 0 ? legs : i == 1 ? torso : i == 2 ? arms : i == 3 ? head : null;

        public void Destroy()
        {
            if (root != null) Object.Destroy(root);
        }

        // =====================================================================
        public void Render(float alpha)
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f) * Mathf.Max(0.15f, VFX.SimSpeed);
            if (f.Hitstop > 0f) dt *= 0.12f;   // impact freeze: everything hangs on the hit
            idleTime += dt;

            bool visible = f.State != FState.Respawning;
            root.SetActive(visible);
            if (!visible) return;

            S.Step(dt, alpha);
            S.Present(Mathf.Min(Time.unscaledDeltaTime, 0.1f), Settings.AnimFps);
            Effects(dt);
            Draw();
        }

        /// <summary>Contact dust/cracks, aura bursts, afterimages, charge aura and smear arcs.</summary>
        void Effects(float dt)
        {
            var clip = S.Clip;
            var look = f.Def.Look;
            Color aura = f.Has(StatusType.Zone) ? new Color(1f, 0.1f, 0.15f) : look.Aura;
            if (S.Contact && clip != null)
            {
                if (clip.GroundSlam) { VFX.GroundImpact(f.Pos + new Vector2(f.Facing * 0.9f * f.Size, 0f), aura); CameraRig.Shake(0.22f * clip.Impact); }
                else if (clip.Impact >= 1.3f) VFX.Dust(f.Pos, 4);
                if (clip.Aura) VFX.Burst(BurstVis.Shockwave, S.HandF, 1.3f * f.Size, aura);
            }
            if (clip != null && clip.Ghosts && S.Striking)
            {
                ghostClock -= dt;
                if (ghostClock <= 0f) { ghostClock = 0.035f; VFX.Afterimage(f, 0.2f); }
            }
            if (S.Charging)
            {
                chargeFx -= dt;
                bool full = f.ChargeTime >= Fighter.MaxCharge;
                if (chargeFx <= 0f)
                {
                    chargeFx = 0.05f;
                    VFX.AuraTick(S.RenderPos, (full ? 2.6f : 2f) * f.Size, aura, full ? 1f : 0.6f);
                    VFX.Sparks(S.HandF, aura, full ? 3 : 1, 3f, 0.1f);
                }
            }

            // smear arcs during the snap of a strike
            Limb limb = clip != null ? clip.Smear : (f.State == FState.Dash ? Limb.None : Limb.None);
            bool emit = S.Striking && limb != Limb.None && !f.Dead;
            Color core = Color.Lerp(Color.white, aura, 0.25f);
            smearA.Core = core; smearA.Edge = aura; smearB.Core = core; smearB.Edge = aura;
            S.SmearSegment(limb, false, out var a0, out var b0);
            smearA.Tick(dt, emit, a0, b0);
            bool two = limb == Limb.Hands;
            S.SmearSegment(limb, true, out var a1, out var b1);
            smearB.Tick(dt, emit && two, a1, b1);
        }

        // =====================================================================
        void Draw()
        {
            var look = f.Def.Look;
            Color body = look.Body.a <= 0f ? new Color(0.94f, 0.94f, 0.97f) : look.Body;
            Color glow = look.Aura;
            float glowA = 0.22f;

            // status tints
            if (f.Has(StatusType.Burn)) { body = Color.Lerp(body, new Color(1f, 0.55f, 0.2f), 0.5f + 0.3f * Mathf.Sin(idleTime * 30f)); VFX.AuraTick(S.RenderPos, 2f * f.Size, new Color(1f, 0.5f, 0.1f), 0.5f); }
            if (f.Has(StatusType.Freeze)) body = new Color(0.65f, 0.9f, 1f);
            if (f.Has(StatusType.Paralysis) || f.Has(StatusType.Stun)) { if (Random.value < 0.15f) VFX.Info(S.HeadC); }
            if (f.Has(StatusType.Sealed) || f.Has(StatusType.Burnout)) { glowA *= 0.3f; if (Random.value < 0.04f) VFX.Puff(S.HeadC + Vector2.up * 0.3f, new Color(0.3f, 0.3f, 0.3f), 0.4f); }
            if (f.Has(StatusType.Jackpot)) { rainbow += Time.unscaledDeltaTime * 0.8f; glow = Color.HSVToRGB(Mathf.Repeat(rainbow, 1f), 0.8f, 1f); glowA = 0.45f; VFX.AuraTick(S.RenderPos, 2.2f, glow, 0.6f); }
            if (f.Has(StatusType.Zone)) { glow = Color.Lerp(new Color(0.9f, 0.05f, 0.1f), Color.black, Mathf.PingPong(idleTime * 3f, 1f)); glowA = 0.4f; VFX.AuraTick(S.RenderPos, 2f, new Color(1f, 0.1f, 0.15f), 0.35f); }
            if (f.Has(StatusType.Gravity) || f.Has(StatusType.Sink)) body = Color.Lerp(body, new Color(0.3f, 0.2f, 0.45f), 0.4f);
            if (f.Casting != null || f.State == FState.RCT) { glowA = 0.4f; VFX.AuraTick(S.RenderPos, 2.1f * f.Size, f.Casting != null ? f.Casting.Color : new Color(0.6f, 1f, 0.75f), f.ChantLevel > 0 ? 1f : 0.55f); }
            if (S.Charging) glowA = 0.5f;
            if (f.HpFrac < 0.25f && !f.Dead) glowA += 0.12f * Mathf.Sin(idleTime * 8f);
            if (f.FlashTimer > 0f) { body = Color.white; glow = Color.white; glowA = 0.6f; }
            bool blink = (f.Invuln > 0.2f || f.Has(StatusType.Invuln)) && Mathf.Repeat(idleTime * 14f, 1f) > 0.5f;
            if (blink) body.a *= 0.45f;

            // impact frame ink
            var ink = ImpactFrames.Ink;
            if (ink == ImpactFrames.FighterInk.White) { body = Color.white; glowA = 0f; }
            else if (ink == ImpactFrames.FighterInk.Black) { body = Color.black; glowA = 0f; }

            Color g = glow.WithA(glowA);

            SetPts(legs, 5, S.FootB, S.KneeB, S.Hip, S.KneeF, S.FootF); Col(legs, body);
            SetPts(torso, 3, S.Hip, S.SpineMid, S.Neck); Col(torso, body);
            SetPts(arms, 7, S.HandB, S.ElbowB, S.ShoulderB, S.Neck, S.ShoulderF, S.ElbowF, S.HandF); Col(arms, body);
            Circle(head, S.HeadC, 0.25f * f.Size, 20); Col(head, body);

            legsGlow.enabled = torsoGlow.enabled = armsGlow.enabled = headGlow.enabled = glowA > 0.01f && Settings.VfxQuality > 0;
            if (legsGlow.enabled)
            {
                SetPts(legsGlow, 5, S.FootB, S.KneeB, S.Hip, S.KneeF, S.FootF); Col(legsGlow, g);
                SetPts(torsoGlow, 3, S.Hip, S.SpineMid, S.Neck); Col(torsoGlow, g);
                SetPts(armsGlow, 7, S.HandB, S.ElbowB, S.ShoulderB, S.Neck, S.ShoulderF, S.ElbowF, S.HandF); Col(armsGlow, g);
                Circle(headGlow, S.HeadC, 0.25f * f.Size, 20); Col(headGlow, g);
            }

            if (extraArms.enabled)
            {
                Vector2 sh = Vector2.Lerp(S.Neck, S.Hip, 0.3f);
                Vector2 off = new Vector2(0f, -0.3f * f.Size);
                Vector2 hb = S.HandB + off, hf = S.HandF + off;
                Vector2 eb = Vector2.Lerp(sh, hb, 0.5f) + new Vector2(-Fc * 0.15f, -0.12f) * f.Size;
                Vector2 ef = Vector2.Lerp(sh, hf, 0.5f) + new Vector2(Fc * 0.1f, -0.12f) * f.Size;
                SetPts(extraArms, 5, hb, eb, sh, ef, hf); Col(extraArms, body);
            }

            DrawAberration(ink);
            DrawHair(body);
            DrawFace(body, ink);
            DrawWeapon(body, ink);
            DrawCrown(ink);
            DrawMarker();

            // black flash rhythm cue: the fist crackles red-black while the window is open
            if (f.BlackFlashWindowOpen && Random.value < 0.8f) VFX.Sparks(S.HandF, Random.value < 0.5f ? Color.black : new Color(1f, 0.1f, 0.15f), 1, 2f, 0.1f);
        }

        const int CaPts = 18;
        static readonly Vector2[] caBuf = new Vector2[CaPts];

        /// <summary>
        /// Chromatic-aberration pulse: on huge hits the figure splits into red and cyan copies that slide apart along the
        /// camera's kick and snap back together (a lens-fringe look without a post stack, so it works in the built-in pipeline).
        /// </summary>
        void DrawAberration(ImpactFrames.FighterInk ink)
        {
            float ca = CameraRig.Aberration;
            bool on = ca > 0.02f && ink == ImpactFrames.FighterInk.Normal && Settings.VfxQuality > 0 && !f.Dead;
            caRed.enabled = caCyan.enabled = on;
            if (!on) return;
            // one continuous stroke over the whole skeleton (legs, spine, both arms), retracing where it has to
            caBuf[0] = S.FootB; caBuf[1] = S.KneeB; caBuf[2] = S.Hip; caBuf[3] = S.KneeF; caBuf[4] = S.FootF; caBuf[5] = S.KneeF;
            caBuf[6] = S.Hip; caBuf[7] = S.SpineMid; caBuf[8] = S.Neck; caBuf[9] = S.ShoulderB; caBuf[10] = S.ElbowB; caBuf[11] = S.HandB;
            caBuf[12] = S.ElbowB; caBuf[13] = S.ShoulderB; caBuf[14] = S.Neck; caBuf[15] = S.ShoulderF; caBuf[16] = S.ElbowF; caBuf[17] = S.HandF;
            Vector2 off = CameraRig.AberrationDir * (0.06f + 0.22f * ca) * f.Size;
            for (int i = 0; i < CaPts; i++)
            {
                caRed.SetPosition(i, caBuf[i] + off);
                caCyan.SetPosition(i, caBuf[i] - off);
            }
            float a = Mathf.Clamp01(ca * 1.4f) * 0.75f;
            Col(caRed, new Color(1f, 0.1f, 0.15f, a));
            Col(caCyan, new Color(0.1f, 0.9f, 1f, a));
        }

        /// <summary>Facing as drawn (flips while the body is turned around mid-spin).</summary>
        int Fc => f.Facing * (S.FaceSign < 0f ? -1 : 1);

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
            int fc = Fc;
            Vector2 c = S.HeadC;
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
            int fc = Fc;
            Vector2 c = S.HeadC;
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
            else if ((fm & FaceMark.Collar) != 0) SetPts(face, 4, S.Neck + new Vector2(-fc * 0.15f, -0.05f), P(-60f, r * 1.15f), P(-20f, r * 1.2f), P(5f, r * 1.0f));
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
            Vector2 h = S.WeaponBase;
            Vector2 d = (S.WeaponTip - S.WeaponBase).normalized;
            if (d.sqrMagnitude < 0.01f) d = new Vector2(Fc, 0f);
            Vector2 n = new Vector2(-d.y, d.x);
            float Sz = f.Size;
            weapon.widthMultiplier = 0.08f * Sz;
            weapon.enabled = true;
            Color wc = look.Accent.a > 0f ? Color.Lerp(body, look.Accent, 0.5f) : body;
            switch (wpn)
            {
                case Weapon.Katana: SetPts(weapon, 5, h - d * 0.18f * Sz, h + n * 0.1f * Sz, h - n * 0.1f * Sz, h, h + d * 1.15f * Sz); wc = new Color(0.85f, 0.88f, 0.95f); break;
                case Weapon.Sword: SetPts(weapon, 5, h - d * 0.2f * Sz, h + n * 0.14f * Sz, h - n * 0.14f * Sz, h, h + d * 1.6f * Sz); weapon.widthMultiplier = 0.14f * Sz; wc = f.Has(StatusType.Executioner) ? new Color(1f, 0.85f, 0.4f) : new Color(0.85f, 0.88f, 0.95f); break;
                case Weapon.Spear: SetPts(weapon, 5, h - d * 0.9f * Sz, h + d * 1.3f * Sz, h + d * 1.15f * Sz + n * 0.12f * Sz, h + d * 1.3f * Sz, h + d * 1.15f * Sz - n * 0.12f * Sz); break;
                case Weapon.Polearm: SetPts(weapon, 4, h - d * 1.0f * Sz, h + d * 1.2f * Sz, h + d * 1.25f * Sz + n * 0.25f * Sz, h + d * 1.6f * Sz); break;
                case Weapon.Cloud:
                {
                    float sw = Mathf.Sin(idleTime * 9f) * 0.25f;
                    Vector2 a = h + d * 0.55f * Sz, b = a + ProjectileAb.Rotate(d, 25f + sw * 60f) * 0.55f * Sz, c = b + ProjectileAb.Rotate(d, 50f + sw * 90f) * 0.55f * Sz;
                    SetPts(weapon, 4, h - d * 0.4f * Sz, a, b, c); weapon.widthMultiplier = 0.11f * Sz; wc = new Color(0.75f, 0.6f, 0.4f); break;
                }
                case Weapon.Staff: SetPts(weapon, 2, h - d * 0.8f * Sz, h + d * 0.9f * Sz); weapon.widthMultiplier = 0.09f * Sz; break;
                case Weapon.Hammer: SetPts(weapon, 4, h - d * 0.1f * Sz, h + d * 0.4f * Sz, h + d * 0.4f * Sz + n * 0.18f * Sz, h + d * 0.4f * Sz - n * 0.12f * Sz); weapon.widthMultiplier = 0.1f * Sz; break;
                case Weapon.Gavel: SetPts(weapon, 4, h, h + d * 0.35f * Sz, h + d * 0.35f * Sz + n * 0.14f * Sz, h + d * 0.35f * Sz - n * 0.14f * Sz); weapon.widthMultiplier = 0.12f * Sz; wc = new Color(0.65f, 0.45f, 0.3f); break;
                case Weapon.Cleaver: SetPts(weapon, 2, h - d * 0.15f * Sz, h + d * 0.85f * Sz); weapon.widthMultiplier = 0.17f * Sz; wc = new Color(0.9f, 0.88f, 0.8f); break;
                case Weapon.Rope: case Weapon.Chain:
                {
                    int segs = 7;
                    if (weapon.positionCount != segs) weapon.positionCount = segs;
                    for (int i = 0; i < segs; i++)
                    {
                        float t = i / (float)(segs - 1);
                        Vector2 p = h + d * t * 1.2f * Sz + n * Mathf.Sin(t * 9f + idleTime * 10f) * 0.12f * Sz * t;
                        weapon.SetPosition(i, p);
                    }
                    wc = wpn == Weapon.Rope ? new Color(0.08f, 0.08f, 0.1f) : new Color(0.7f, 0.7f, 0.75f);
                    break;
                }
                case Weapon.Bow: SetPts(weapon, 3, h + n * 0.55f * Sz, h + d * 0.18f * Sz, h - n * 0.55f * Sz); break;
                case Weapon.Broom: SetPts(weapon, 5, h + d * 0.6f * Sz, h - d * 0.9f * Sz, h - d * 1.05f * Sz + n * 0.15f * Sz, h - d * 0.9f * Sz, h - d * 1.05f * Sz - n * 0.15f * Sz); wc = new Color(0.7f, 0.55f, 0.35f); break;
                case Weapon.Revolver: SetPts(weapon, 3, h - n * 0.12f * Sz, h, h + d * 0.35f * Sz); weapon.widthMultiplier = 0.09f * Sz; wc = new Color(0.4f, 0.4f, 0.45f); break;
                case Weapon.Guitar: SetPts(weapon, 6, h + d * 0.7f * Sz, h - d * 0.1f * Sz, h - d * 0.25f * Sz + n * 0.2f * Sz, h - d * 0.45f * Sz, h - d * 0.25f * Sz - n * 0.2f * Sz, h - d * 0.1f * Sz); wc = new Color(0.8f, 0.3f, 0.25f); break;
                case Weapon.Fan: SetPts(weapon, 5, h, h + ProjectileAb.Rotate(d, 35f) * 0.45f * Sz, h + d * 0.5f * Sz, h + ProjectileAb.Rotate(d, -35f) * 0.45f * Sz, h); break;
                case Weapon.Claws: SetPts(weapon, 5, h + ProjectileAb.Rotate(d, 18f) * 0.4f * Sz, h, h + d * 0.45f * Sz, h, h + ProjectileAb.Rotate(d, -18f) * 0.4f * Sz); weapon.widthMultiplier = 0.05f * Sz; wc = look.Aura; break;
                case Weapon.Axe: SetPts(weapon, 5, h - d * 0.2f * Sz, h + d * 0.8f * Sz, h + d * 0.7f * Sz + n * 0.3f * Sz, h + d * 0.5f * Sz + n * 0.28f * Sz, h + d * 0.55f * Sz); weapon.widthMultiplier = 0.09f * Sz; break;
                case Weapon.Pen: SetPts(weapon, 2, h, h + d * 0.3f * Sz); weapon.widthMultiplier = 0.05f * Sz; break;
                case Weapon.Receipts: SetPts(weapon, 5, h, h + n * 0.15f * Sz, h + n * 0.15f * Sz + d * 0.22f * Sz, h + d * 0.22f * Sz, h); weapon.widthMultiplier = 0.04f * Sz; wc = Color.white; break;
                case Weapon.Phone: SetPts(weapon, 2, h, h + n * 0.2f * Sz); weapon.widthMultiplier = 0.11f * Sz; wc = new Color(0.3f, 0.35f, 0.45f); break;
                default:
                    if ((look.Face & FaceMark.Wings) != 0)
                    {
                        Vector2 back = Vector2.Lerp(S.Neck, S.Hip, 0.2f) + new Vector2(-Fc * 0.1f, 0f);
                        float flap = Mathf.Sin(idleTime * 5f) * 0.25f;
                        SetPts(weapon, 5, back + new Vector2(-Fc * 0.9f * Sz, (0.9f + flap) * Sz), back, back + new Vector2(-Fc * 1.1f * Sz, (0.2f + flap) * Sz), back, back + new Vector2(-Fc * 0.8f * Sz, (-0.4f + flap * 0.5f) * Sz));
                        weapon.widthMultiplier = 0.06f * Sz;
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
            Vector2 top = S.HeadC + Vector2.up * r * 2.2f;
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
            Vector2 top = S.HeadC + Vector2.up * (0.25f * f.Size * 2.2f + ((f.Def.Look.Face & (FaceMark.Wheel | FaceMark.Halo | FaceMark.Crown)) != 0 ? 0.5f : 0f) + (f.Def.Look.Hair == Hair.Horns || f.Def.Look.Hair == Hair.Volcano || f.Def.Look.Hair == Hair.Afro ? 0.35f : 0.15f));
            SetPts(marker, 3, top + new Vector2(-0.16f, 0.2f), top, top + new Vector2(0.16f, 0.2f));
            Col(marker, f.TeamColor);
        }
    }

}
