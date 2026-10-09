using UnityEngine;

namespace ProjectSorcery
{
    /// <summary>
    /// CPU opponent. Produces the same 12-bit input a human would, so it obeys every rule a player does.
    /// Fully deterministic (only uses the match RNG) so CPU fighters stay in sync during online play.
    /// </summary>
    public sealed class AIBrain
    {
        struct Profile
        {
            public float Reaction;        // seconds between decisions
            public float Block;           // chance to block a threatening attack
            public float Dodge;           // chance to dodge an incoming projectile / beam
            public float ComboSkill;      // 0..1: length and quality of combos
            public float BlackFlash;      // chance to hit the black flash rhythm
            public float Specials;        // how eagerly specials are used
            public float DomainIQ;        // 0..1: smart domain usage
            public float Mash;            // presses per second during a domain clash
            public float Mistakes;        // chance of a wasted decision
            public float Aggression;      // 0..1
            public int BurstAfter;        // juggle hits before bursting (99 = never)
            public float SimpleDomain;    // chance to defend inside an enemy domain
            public float Chant;           // max chant levels used
        }

        readonly Fighter f;
        public readonly Difficulty Diff;
        Profile P;
        Match M => f.M;

        ushort hold, pulse, lastOut;
        float decide;
        int moveDir;
        float moveTimer;
        int holdSlot = -1;
        int chantTarget;
        float idleTimer;
        float comboTimer;
        int comboRoute, comboStep;
        float mashClock;
        bool wantRct, wantSimple;

        public AIBrain(Fighter fighter, Difficulty d)
        {
            f = fighter; Diff = d;
            P = Make(d);
        }

        static Profile Make(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Dummy: return new Profile { Reaction = 99f, Mash = 0f, BurstAfter = 99 };
                case Difficulty.Easy:
                    return new Profile { Reaction = 0.42f, Block = 0.15f, Dodge = 0.1f, ComboSkill = 0.15f, BlackFlash = 0.01f, Specials = 0.45f, DomainIQ = 0.1f, Mash = 3.5f, Mistakes = 0.25f, Aggression = 0.45f, BurstAfter = 99, SimpleDomain = 0.1f, Chant = 0 };
                case Difficulty.Medium:
                    return new Profile { Reaction = 0.24f, Block = 0.45f, Dodge = 0.4f, ComboSkill = 0.55f, BlackFlash = 0.06f, Specials = 0.75f, DomainIQ = 0.55f, Mash = 7f, Mistakes = 0.08f, Aggression = 0.6f, BurstAfter = 7, SimpleDomain = 0.45f, Chant = 1 };
                case Difficulty.Hard:
                    return new Profile { Reaction = 0.12f, Block = 0.78f, Dodge = 0.75f, ComboSkill = 0.95f, BlackFlash = 0.14f, Specials = 1f, DomainIQ = 1f, Mash = 11f, Mistakes = 0.02f, Aggression = 0.75f, BurstAfter = 4, SimpleDomain = 0.85f, Chant = 2 };
                default: // Nightmare (raid boss)
                    return new Profile { Reaction = 0.1f, Block = 0.55f, Dodge = 0.6f, ComboSkill = 1f, BlackFlash = 0.16f, Specials = 1.2f, DomainIQ = 1f, Mash = 12f, Mistakes = 0f, Aggression = 0.9f, BurstAfter = 3, SimpleDomain = 0.9f, Chant = 2 };
            }
        }

        public void Reset()
        {
            hold = pulse = lastOut = 0; decide = 0.4f; holdSlot = -1; idleTimer = 0f; comboStep = 0; mashClock = 0f; wantRct = wantSimple = false;
        }

        ushort Output()
        {
            // a pulse only registers as a press if the button was up last tick
            ushort ready = (ushort)(pulse & ~lastOut);
            ushort deferred = (ushort)(pulse & lastOut);
            ushort o = (ushort)(hold | ready);
            lastOut = o;
            pulse = deferred;
            return o;
        }

        void Press(ushort b) { pulse |= b; }

        // =====================================================================
        public ushort ThinkFrozen()
        {
            hold = 0;
            if (f.Dead || Diff == Difficulty.Dummy) { pulse = 0; return Output(); }
            var dom = M.Domains;
            if (dom.Clashing && dom.ClashFighters.Contains(f) && dom.ClashTime > DomainSystem.ClashCutIn)
            {
                mashClock += Match.TickDt * P.Mash * M.Rng.Range(0.8f, 1.2f);
                while (mashClock >= 1f) { mashClock -= 1f; Press((M.TickCount & 1) == 0 ? IB.Light : IB.Heavy); }
            }
            return Output();
        }

        public ushort Think(float dt)
        {
            if (f.Dead || Diff == Difficulty.Dummy || f.State == FState.Locked || f.State == FState.Victory)
            {
                hold = 0; pulse = 0;
                if (Diff == Difficulty.Dummy && f.M.Config.Mode == GameMode.Training && TrainingDummy.Block) hold = IB.Block;
                return Output();
            }

            var t = f.Target;
            if (t == null) { hold = 0; return Output(); }

            float dx = t.Pos.x - f.Pos.x;
            float dist = Mathf.Abs(dx);
            int toward = dx >= 0 ? 1 : -1;

            // ---------------- held actions continue (chant, RCT, simple domain)
            if (holdSlot >= 0)
            {
                if (f.State == FState.Cast && f.ChantLevel < chantTarget && !f.CastFired) { hold = (ushort)(IB.Slot(holdSlot)); return Output(); }
                holdSlot = -1;
                hold = 0;
            }

            // ---------------- burst out of long combos
            if (f.State == FState.Hitstun)
            {
                hold = 0;
                if (f.Juggle >= P.BurstAfter && f.BurstCd <= 0f && (f.IsRestricted || f.Ce >= 30f)) { hold = IB.Block; Press(IB.Dash); }
                return Output();
            }
            if (f.State == FState.Knockdown || f.State == FState.GetUp) { hold = 0; return Output(); }

            // ---------------- sustained defensive channels
            if (wantSimple)
            {
                if (M.Domains.EnemyDomainActiveFor(f) && f.Ce > 8f) { hold = (ushort)(IB.Block | IB.Down); return Output(); }
                wantSimple = false;
            }
            if (wantRct)
            {
                if (f.Hp < f.MaxHp * 0.85f && f.Ce > 15f && dist > 3.2f && !M.Domains.EnemyDomainActiveFor(f)) { hold = (ushort)(IB.Block | IB.Down); return Output(); }
                wantRct = false;
            }

            // keep moving between decisions
            decide -= dt;
            comboTimer -= dt;
            if (idleTimer > 0f) { idleTimer -= dt; hold = 0; return Output(); }

            // ---------------- combo continuation is reflexive (doesn't wait for a decision)
            if (f.State == FState.Attack && f.CurAttack != null)
            {
                ContinueCombo(t, dist);
                return Output();
            }

            if (decide > 0f)
            {
                ApplyMove();
                return Output();
            }
            decide = P.Reaction * M.Rng.Range(0.75f, 1.3f);

            if (M.Rng.Chance(P.Mistakes)) { idleTimer = M.Rng.Range(0.2f, 0.6f); hold = 0; return Output(); }

            hold = 0;

            // ---------------- 1. domain logic
            if (TryDomain(t)) return Output();

            // ---------------- 2. inside an enemy domain: defend
            if (M.Domains.EnemyDomainActiveFor(f) && !f.IsRestricted && f.Ce > 15f && M.Rng.Chance(P.SimpleDomain))
            {
                wantSimple = true;
                hold = (ushort)(IB.Block | IB.Down);
                return Output();
            }

            // ---------------- 3. threats: projectiles, beams, incoming strikes
            if (ReactToThreats(t, dist, toward)) return Output();

            // ---------------- 4. recover: RCT when safe and hurt
            if (f.Def.Has(Passive.RCT) && f.HpFrac < 0.45f && dist > 4.5f && f.Ce > 30f && M.Rng.Chance(0.5f * P.Specials))
            {
                wantRct = true;
                hold = (ushort)(IB.Block | IB.Down);
                return Output();
            }

            // ---------------- 5. specials
            if (TrySpecial(t, dist)) return Output();

            // ---------------- 6. footsies / melee
            Neutral(t, dist, toward);
            return Output();
        }

        // =====================================================================
        bool TryDomain(Fighter t)
        {
            var ab = f.Kit[3] as DomainAb;
            if (ab == null || !f.CanCast(3)) return false;
            var dom = M.Domains;
            bool enemyDomain = dom.EnemyDomainActiveFor(f);
            bool enemyCasting = dom.EnemyCasting(f);
            bool targetRestricted = t.IsRestricted;
            if (f.IsBoss && M.Mode != null && M.Mode.BossPhase < 2) return false;

            bool go = false;
            if (enemyDomain || enemyCasting) go = M.Rng.Chance(0.25f + 0.75f * P.DomainIQ); // clash back
            else if (P.DomainIQ < 0.3f) go = M.Rng.Chance(0.25f);
            else
            {
                bool good = t.Vulnerable || t.Has(StatusType.Burnout) || t.HpFrac < 0.6f || f.IsBoss;
                if (targetRestricted && P.DomainIQ > 0.5f) good = false;   // sure-hit can't see them
                if (t.Def.Has(Passive.SimpleDomainMaster) && P.DomainIQ > 0.8f && !t.Vulnerable) good = M.Rng.Chance(0.2f);
                go = good && M.Rng.Chance(0.35f + 0.5f * P.DomainIQ);
            }
            if (!go) return false;

            Press(IB.Ult);
            float dist = Mathf.Abs(t.Pos.x - f.Pos.x);
            bool safe = enemyDomain || enemyCasting ? false : dist > 4f || t.Vulnerable || t.HardCC;
            chantTarget = safe ? Mathf.Min(ab.MaxChant, (int)P.Chant) : 0;
            if (chantTarget > 0) { holdSlot = 3; hold = IB.Ult; }
            return true;
        }

        bool ReactToThreats(Fighter t, float dist, int toward)
        {
            // incoming projectiles
            var ps = M.Projectiles;
            for (int i = 0; i < ps.Count; i++)
            {
                var p = ps[i];
                if (p.Dead || p.Owner == null || !M.IsEnemyTeam(f.Team, p.Team) || p.Stuck != null) continue;
                Vector2 to = f.Center - p.Pos;
                float closing = Vector2.Dot(p.Vel, to.normalized);
                if (closing <= 2f) continue;
                float tti = to.magnitude / closing;
                if (tti > 0.45f || Mathf.Abs(to.y) > 2.2f) continue;
                if (!M.Rng.Chance(P.Dodge)) return false;
                bool unblock = (p.D.Flags & (HitFlags.Unblockable | HitFlags.SureHit)) != 0 || p.D.Erase;
                if (p.D.Ground || (!unblock && p.Size < 1.2f && M.Rng.Chance(0.4f))) { if (p.D.Ground) { Press(IB.Jump); return true; } }
                if (!unblock && M.Rng.Chance(0.55f)) { hold = IB.Block; return true; }
                if (f.DashCdReady) { hold = (ushort)(toward > 0 ? IB.Right : IB.Left); Press(IB.Dash); return true; }
                Press(IB.Jump);
                return true;
            }
            // charging beams aimed at us
            var bs = M.Beams;
            for (int i = 0; i < bs.Count; i++)
            {
                var b = bs[i];
                if (b.Dead || b.Owner == null || !M.IsEnemy(b.Owner, f) || b.Firing) continue;
                if (!f.OverlapsSegment(b.Origin, b.End, b.Width * 0.5f + 0.4f)) continue;
                if (!M.Rng.Chance(P.Dodge)) return false;
                if (b.D.FromSky) { hold = (ushort)(M.Rng.Chance(0.5f) ? IB.Left : IB.Right); Press(IB.Dash); }
                else Press(IB.Jump);
                return true;
            }
            // enemy melee coming
            bool threat = dist < 2.6f && (t.State == FState.Attack || t.InStrike || (t.State == FState.Cast && t.Casting != null && t.Casting.Use == AIUse.Attack));
            if (threat && f.Grounded)
            {
                // counter stance if available
                for (int s = 0; s < 3; s++)
                    if (f.Kit[s] is CounterAb && f.CanCast(s) && M.Rng.Chance(P.Block * 0.6f)) { Press(IB.Slot(s)); return true; }
                if (M.Rng.Chance(P.Block)) { hold = IB.Block; return true; }
                if (M.Rng.Chance(P.Dodge * 0.3f)) { hold = (ushort)(toward > 0 ? IB.Left : IB.Right); Press(IB.Dash); return true; }
            }
            return false;
        }

        bool TrySpecial(Fighter t, float dist)
        {
            if (f.State != FState.Idle && f.State != FState.Run && f.State != FState.Air) return false;
            int best = -1;
            float bestScore = 0.35f;
            bool vulnerable = t.Vulnerable || t.HardCC || t.Has(StatusType.Bind);
            for (int s = 0; s < 4; s++)
            {
                var ab = f.Kit[s];
                if (ab == null || ab is DomainAb || !f.CanCast(s)) continue;
                if (f.IsBoss && s == 3 && M.Mode != null && M.Mode.BossPhase < 2) continue;
                float score = Score(ab, s, t, dist, vulnerable);
                score *= P.Specials * M.Rng.Range(0.7f, 1.3f);
                // keep cursed energy for the domain on smart difficulties
                var ult = f.Kit[3] as DomainAb;
                if (ult != null && P.DomainIQ > 0.5f && s < 3 && f.Ce - f.CostOf(ab) < f.CostOf(ult) && f.Cd[3] <= 3f) score *= 0.5f;
                if (score > bestScore) { bestScore = score; best = s; }
            }
            if (best < 0) return false;
            var chosen = f.Kit[best];
            // face the target before casting
            hold = (ushort)(t.Pos.x > f.Pos.x ? IB.Right : IB.Left);
            Press(IB.Slot(best));
            if (chosen.Chantable && P.Chant > 0)
            {
                bool safe = dist > 5f || vulnerable;
                chantTarget = safe ? Mathf.Min(chosen.MaxChant, (int)P.Chant) : 0;
                if (chantTarget > 0) { holdSlot = best; hold = IB.Slot(best); }
            }
            return true;
        }

        float Score(Ability ab, int slot, Fighter t, float dist, bool vulnerable)
        {
            bool inRange = dist >= ab.RangeMin && dist <= ab.RangeMax;
            switch (ab.Use)
            {
                case AIUse.Projectile: return inRange ? (vulnerable ? 0.9f : 0.6f) : 0.1f;
                case AIUse.Gapclose: return dist > 2.2f && dist <= ab.RangeMax ? 0.65f : (dist < 2f ? 0.45f : 0.1f);
                case AIUse.Attack: return inRange ? (vulnerable ? 0.85f : 0.6f) : 0.05f;
                case AIUse.Area: return dist < ab.RangeMax ? 0.7f : 0.05f;
                case AIUse.Finisher:
                    if (!inRange && ab.RangeMax < 20f) return 0.1f;
                    return t.HpFrac < 0.35f || vulnerable ? 0.95f : (f.IsBoss ? 0.55f : 0.3f);
                case AIUse.Buff:
                    return Busy(ab) ? 0f : (dist > 3f ? 0.55f : 0.4f);
                case AIUse.Heal:
                    {
                        float worst = f.HpFrac;
                        foreach (var a in M.Fighters) if (a.Team == f.Team && !a.Dead) worst = Mathf.Min(worst, a.HpFrac);
                        return worst < 0.55f ? 0.85f : 0.05f;
                    }
                case AIUse.Summon: return ab.CanUse(f) ? 0.6f : 0f;
                case AIUse.Escape: return (f.HpFrac < 0.35f && dist < 2.5f) || (dist < 1.5f && t.State == FState.Attack) ? 0.75f : 0.1f;
                case AIUse.Zone: return dist > 1.5f && dist < 9f ? 0.5f : 0.1f;
                case AIUse.Counter: return dist < 2.5f && (t.State == FState.Attack || t.InStrike) ? 0.8f : 0.05f;
                case AIUse.Utility: return dist <= ab.RangeMax ? (ab.CanUse(f) ? 0.5f : 0f) : 0.1f;
                default: return 0.3f;
            }
        }

        bool Busy(Ability ab)
        {
            var b = ab as BuffAb;
            if (b == null) return false;
            return b.Status != StatusType.Count && f.Has(b.Status);
        }

        void Neutral(Fighter t, float dist, int toward)
        {
            float want = f.Def.PreferredRange;
            bool zoner = f.Def.Style == AIStyle.Zoner || f.Def.Style == AIStyle.Summoner || f.Def.Style == AIStyle.Support;
            float reach = 1.35f + (f.Def.Reach ? 0.45f : f.Def.Bladed ? 0.25f : 0f);

            // anti-air
            if (t.Pos.y > f.Pos.y + 1.2f && dist < 1.6f && f.Grounded && M.Rng.Chance(0.4f + 0.4f * P.ComboSkill))
            {
                hold = IB.Down; Press(IB.Heavy);
                return;
            }

            if (dist <= reach)
            {
                // in range: attack (or punish a vulnerable target)
                FaceToward(toward);
                if (t.Vulnerable || M.Rng.Chance(P.Aggression))
                {
                    comboRoute = PickRoute();
                    comboStep = 0;
                    if (comboRoute == 3 && f.Grounded) { hold = IB.Down; Press(IB.Light); return; } // sweep opener
                    Press(IB.Light);
                    return;
                }
                if (zoner && !t.Vulnerable) { MoveAway(toward); if (M.Rng.Chance(0.25f)) Press(IB.Dash); return; }
                hold = IB.Block;
                return;
            }

            if (zoner && dist < want - 1f)
            {
                MoveAway(toward);
                if (M.Rng.Chance(0.15f + 0.2f * P.ComboSkill) && f.Grounded) Press(IB.Jump);
                return;
            }

            if (dist > want + 0.6f || !zoner)
            {
                // approach: run, dash in from far, jump-in occasionally
                moveDir = toward; moveTimer = 0.4f;
                hold = (ushort)(toward > 0 ? IB.Right : IB.Left);
                if (dist > 5f && M.Rng.Chance(0.25f * P.Aggression)) Press(IB.Dash);
                else if (dist > 2.5f && dist < 4.5f && M.Rng.Chance(0.12f * P.ComboSkill) && f.Grounded) Press(IB.Jump);
                return;
            }

            // spacing dance
            if (M.Rng.Chance(0.5f)) { moveDir = M.Rng.Chance(0.5f) ? 1 : -1; moveTimer = M.Rng.Range(0.15f, 0.4f); ApplyMove(); }
        }

        int PickRoute()
        {
            float r = M.Rng.Value();
            if (P.ComboSkill < 0.3f) return r < 0.7f ? 0 : 1;
            if (r < 0.3f) return 0;                       // L L L L
            if (r < 0.55f) return 1;                      // L L Heavy (black flash attempt)
            if (r < 0.8f + 0.1f * P.ComboSkill) return 2; // L L Launcher -> air combo
            return 3;                                      // sweep opener
        }

        void ContinueCombo(Fighter t, float dist)
        {
            var a = f.CurAttack;
            bool recovering = f.StateTime >= a.Startup + a.Active;
            if (!recovering) return;
            bool hitting = t.Vulnerable;
            if (!hitting) { comboStep = 0; return; }   // whiffed or blocked: stop

            // black flash rhythm: the window opens just after a light lands
            if (f.BlackFlashWindowOpen && (comboRoute == 1 || M.Rng.Chance(0.15f)) && M.Rng.Chance(P.BlackFlash))
            {
                Press(IB.Heavy);
                comboStep = 99;
                return;
            }

            // heavy strings: keep swinging while the heavies land
            int hs = System.Array.IndexOf(f.Set.HeavyChain, a);
            if (hs >= 0 && hs < f.Set.HeavyChain.Length - 1 && f.Grounded)
            {
                if (M.Rng.Chance(0.25f + 0.5f * P.ComboSkill)) Press(IB.Heavy);
                return;
            }

            int maxLights = P.ComboSkill < 0.3f ? 2 : P.ComboSkill < 0.7f ? 3 : 4;
            if (!f.Grounded)
            {
                // air combo: two airs then a smash
                if (comboStep < 2) { Press(IB.Light); comboStep++; }
                else if (comboStep < 4) { Press(IB.Heavy); comboStep = 99; }
                return;
            }
            switch (comboRoute)
            {
                case 1:
                    if (comboStep < 2) { Press(IB.Light); comboStep++; }
                    else if (comboStep < 90) { Press(IB.Heavy); comboStep = 99; }
                    break;
                case 2:
                    if (comboStep < 2) { Press(IB.Light); comboStep++; }
                    else if (comboStep == 2) { hold = IB.Down; Press(IB.Heavy); comboStep = 3; }
                    else if (comboStep == 3 && (a.Flags & HitFlags.Launch) != 0 && P.ComboSkill > 0.6f) { Press(IB.Jump); comboStep = 0; }
                    else if (comboStep < 90 && M.Rng.Chance(P.Specials * 0.5f)) { TryComboSpecial(t, dist); comboStep = 99; }
                    break;
                default:
                    if (comboStep < maxLights - 1) { Press(IB.Light); comboStep++; }
                    else if (comboStep < 90)
                    {
                        // special cancel after the last hit
                        if (M.Rng.Chance(0.6f * P.Specials * P.ComboSkill)) TryComboSpecial(t, dist);
                        comboStep = 99;
                    }
                    break;
            }
        }

        void TryComboSpecial(Fighter t, float dist)
        {
            for (int s = 0; s < 3; s++)
            {
                var ab = f.Kit[s];
                if (ab == null || !f.CanCast(s)) continue;
                if ((ab.Use == AIUse.Attack || ab.Use == AIUse.Gapclose || ab.Use == AIUse.Area || ab.Use == AIUse.Finisher) && dist <= ab.RangeMax + 0.5f)
                {
                    Press(IB.Slot(s));
                    return;
                }
            }
        }

        void FaceToward(int dir) { hold |= (ushort)0; if (f.Facing != dir) hold = (ushort)(dir > 0 ? IB.Right : IB.Left); }
        void MoveAway(int toward)
        {
            moveDir = -toward; moveTimer = 0.35f;
            hold = (ushort)(toward > 0 ? IB.Left : IB.Right);
            // don't back into a corner forever
            if ((f.Pos.x < M.Arena.Left + 1.5f && toward > 0) || (f.Pos.x > M.Arena.Right - 1.5f && toward < 0))
            {
                hold = 0;
                if (f.Grounded && M.Rng.Chance(0.5f)) { Press(IB.Jump); hold = (ushort)(toward > 0 ? IB.Right : IB.Left); }
            }
        }

        void ApplyMove()
        {
            if (moveTimer > 0f)
            {
                moveTimer -= M.Dt;
                hold = (ushort)((hold & ~(IB.Left | IB.Right)) | (moveDir > 0 ? IB.Right : IB.Left));
            }
        }
    }

    /// <summary>Training-mode dummy settings (changed from the pause menu).</summary>
    public static class TrainingDummy
    {
        public static bool Block;
    }
}
