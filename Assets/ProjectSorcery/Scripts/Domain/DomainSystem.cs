using System.Collections.Generic;
using UnityEngine;

namespace ProjectSorcery
{
    /// <summary>
    /// Domain expansions: casting, manifestation, sure-hit effects, barrier breaks, burnout and
    /// N-way domain clashes (2v2 and 3-way free-for-all clashes are resolved together).
    /// </summary>
    public sealed class DomainSystem
    {
        public sealed class Active
        {
            public Fighter Owner;
            public DomainDef Def;
            public float Time, Power, Duration, TickA, TickB;
            public bool Fired;
            public Fighter Defendant;
            public bool Collapsing;
        }

        sealed class Pending { public Fighter F; public DomainAb Ab; public float Start; }

        readonly Match M;
        public readonly List<Active> Actives = new List<Active>();
        readonly List<Pending> pending = new List<Pending>();

        // clash state
        public bool Clashing;
        public float ClashTime;
        public readonly List<Fighter> ClashFighters = new List<Fighter>();
        public readonly List<DomainDef> ClashDefs = new List<DomainDef>();
        public readonly List<float> ClashScore = new List<float>();
        readonly List<float> clashPower = new List<float>();
        public const float ClashCutIn = 1.7f, ClashPush = 3.4f;

        readonly List<Fighter> victims = new List<Fighter>(4);
        readonly HashSet<Fighter> undetectedShown = new HashSet<Fighter>();

        public DomainSystem(Match m) { M = m; }

        public void Reset()
        {
            foreach (var a in Actives) DomainFX.Hide(a.Def);
            Actives.Clear(); pending.Clear();
            Clashing = false; ClashFighters.Clear(); ClashDefs.Clear(); ClashScore.Clear(); clashPower.Clear();
            undetectedShown.Clear();
            DomainFX.HideAll();
        }

        // ------------------------------------------------------------ queries
        public bool IsOwnerActive(Fighter f)
        {
            for (int i = 0; i < Actives.Count; i++) if (Actives[i].Owner == f) return true;
            return false;
        }

        public bool AllyDomainActive(Fighter f)
        {
            for (int i = 0; i < Actives.Count; i++) if (Actives[i].Owner != f && Actives[i].Owner.Team == f.Team) return true;
            return false;
        }

        public SureHit OwnerEffect(Fighter f)
        {
            for (int i = 0; i < Actives.Count; i++) if (Actives[i].Owner == f) return Actives[i].Def.Effect;
            return (SureHit)(-1);
        }

        public bool EnemyDomainActiveFor(Fighter f)
        {
            for (int i = 0; i < Actives.Count; i++) if (M.IsEnemy(Actives[i].Owner, f)) return true;
            return false;
        }

        public bool AnyActive => Actives.Count > 0;
        public Active Current => Actives.Count > 0 ? Actives[Actives.Count - 1] : null;

        public bool IsCasting(Fighter f)
        {
            for (int i = 0; i < pending.Count; i++) if (pending[i].F == f) return true;
            return false;
        }

        public bool EnemyCasting(Fighter f)
        {
            for (int i = 0; i < pending.Count; i++) if (M.IsEnemy(pending[i].F, f)) return true;
            return false;
        }

        /// <summary>Technique-extinguishing effects tear down every enemy barrier.</summary>
        public void ForceCollapseEnemies(Fighter f)
        {
            for (int i = Actives.Count - 1; i >= 0; i--)
            {
                if (!M.IsEnemy(Actives[i].Owner, f)) continue;
                Collapse(Actives[i], true);
                Actives.RemoveAt(i);
            }
        }

        // ------------------------------------------------------------ casting
        public void BeginCast(Fighter f, DomainAb ab)
        {
            pending.Add(new Pending { F = f, Ab = ab, Start = M.Time });
            VFX.HandSign(f, ab.Domain.Primary);
            VFX.WorldText(f.HeadPos + Vector2.up * 1.1f, "DOMAIN EXPANSION", ab.Domain.Primary, 1.2f);
            Audio.Play(Sfx.Rumble, f.Center, 1f);
            Audio.Play(Sfx.Clap, f.Center, 0.9f);
            M.SlowMo(0.55f, 0.5f);
            CameraRig.Focus(f, 0.9f);
        }

        public void CancelCast(Fighter f)
        {
            for (int i = pending.Count - 1; i >= 0; i--) if (pending[i].F == f) pending.RemoveAt(i);
        }

        public void FinishCast(Fighter f, DomainAb ab, float power)
        {
            CancelCast(f);
            var parts = new List<Fighter> { f };
            var defs = new List<DomainDef> { ab.Domain };
            var pows = new List<float> { power };

            // enemies who already have a domain up are dragged into the clash
            for (int i = 0; i < Actives.Count; i++)
            {
                var a = Actives[i];
                if (!M.IsEnemy(a.Owner, f) || parts.Contains(a.Owner)) continue;
                parts.Add(a.Owner); defs.Add(a.Def); pows.Add(a.Power);
            }
            // enemies mid-cast complete instantly and join
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                var p = pending[i];
                if (!M.IsEnemy(p.F, f) || parts.Contains(p.F)) continue;
                parts.Add(p.F); defs.Add(p.Ab.Domain); pows.Add((1f + 0.35f * p.F.ChantLevel) * p.F.PowerNoBase());
                p.F.EndCastIfDomain();
                pending.RemoveAt(i);
            }

            if (parts.Count >= 2) StartClash(parts, defs, pows);
            else Manifest(f, ab.Domain, power, true);
        }

        // ------------------------------------------------------------ manifestation
        void Manifest(Fighter f, DomainDef def, float power, bool cinematic)
        {
            // an ally's barrier is replaced
            for (int i = Actives.Count - 1; i >= 0; i--) if (Actives[i].Owner.Team == f.Team) { DomainFX.Hide(Actives[i].Def); Actives.RemoveAt(i); }

            var a = new Active
            {
                Owner = f, Def = def, Power = power, Time = 0f,
                Duration = def.Duration * (1f + 0.12f * f.ChantLevel) * (M.Mode != null ? M.Mode.DomainDurationMul(f) : 1f)
            };
            Actives.Add(a);
            f.StatDomainWins++;
            f.DamageTakenWindow = 0f;
            undetectedShown.Clear();
            DomainFX.Show(def, f);
            M.Events.DomainExpanded(f, def);
            Audio.DomainOpen(def.Theme);
            CameraRig.Shake(0.6f);
            if (cinematic) M.Freeze(1.5f);

            switch (def.Effect)
            {
                case SureHit.Verdict:
                    a.Defendant = M.NearestEnemy(f);
                    a.Duration = 4f;
                    M.Freeze(2.6f);
                    M.Events.Announce("TRIAL", a.Defendant != null ? "Defendant: " + a.Defendant.Def.Name : "", def.Primary, 2.4f);
                    Audio.Play(Sfx.Gavel, f.Center);
                    break;
                case SureHit.Jackpot:
                    a.Duration = 3f;
                    M.Freeze(2.4f);
                    DomainFX.JackpotSpin();
                    break;
            }
        }

        // ------------------------------------------------------------ clash
        void StartClash(List<Fighter> parts, List<DomainDef> defs, List<float> pows)
        {
            Clashing = true;
            ClashTime = 0f;
            ClashFighters.Clear(); ClashDefs.Clear(); ClashScore.Clear(); clashPower.Clear();
            for (int i = 0; i < parts.Count; i++)
            {
                var f = parts[i];
                var d = defs[i];
                ClashFighters.Add(f);
                ClashDefs.Add(d);
                clashPower.Add(pows[i]);
                float s = d.Refinement * 10f + (pows[i] - 1f) * 18f + f.CeFrac * 12f;
                if (d.Open) s += 18f;   // a barrierless domain overwhelms barriers from outside
                if (f.Def.Has(Passive.SixEyes)) s += 6f;
                s += M.Rng.Range(0f, 4f);
                ClashScore.Add(s);
                f.Lock(true);
            }
            // existing domains pause while the barriers fight
            for (int i = Actives.Count - 1; i >= 0; i--) if (ClashFighters.Contains(Actives[i].Owner)) { DomainFX.Hide(Actives[i].Def); Actives.RemoveAt(i); }

            M.Freeze(ClashCutIn + ClashPush + 0.3f);
            M.Events.ClashStart(new List<Fighter>(ClashFighters));
            DomainFX.ShowClash(ClashDefs, ClashFighters);
            Audio.Play(Sfx.ClashStart, Vector2.zero, 1f);
            CameraRig.Shake(0.8f);
        }

        void TickClash(float dt)
        {
            ClashTime += dt;
            if (ClashTime > ClashCutIn && ClashTime < ClashCutIn + ClashPush)
            {
                for (int i = 0; i < ClashFighters.Count; i++)
                {
                    var f = ClashFighters[i];
                    int mash = f.In.MashCount;
                    if (mash > 0)
                    {
                        ClashScore[i] += mash * 2.2f;
                        if (!f.IsRestricted) f.Ce = Mathf.Max(0f, f.Ce - 0.4f * mash);
                    }
                    ClashScore[i] += f.CeFrac * 2.5f * dt; // raw output slowly pushes
                }
                if ((int)(ClashTime * 6f) != (int)((ClashTime - dt) * 6f)) { Audio.Play(Sfx.ClashHit, Vector2.zero, 0.6f, Random.Range(0.85f, 1.15f)); CameraRig.Shake(0.2f); }
            }
            DomainFX.UpdateClash(Shares());

            if (ClashTime >= ClashCutIn + ClashPush)
            {
                int win = 0;
                for (int i = 1; i < ClashScore.Count; i++)
                    if (ClashScore[i] > ClashScore[win] + 0.001f || (Mathf.Abs(ClashScore[i] - ClashScore[win]) <= 0.001f && ClashFighters[i].Slot < ClashFighters[win].Slot)) win = i;
                var winner = ClashFighters[win];
                for (int i = 0; i < ClashFighters.Count; i++)
                {
                    var f = ClashFighters[i];
                    f.Lock(false);
                    if (i == win) continue;
                    f.AddStatus(StatusType.Burnout, f.Def.Has(Passive.SixEyes) ? 5f : 9f, 0f, winner);
                    f.Ce = Mathf.Max(0f, f.Ce - 25f);
                    f.Knockdown(0.6f, new Vector2((f.Pos.x < winner.Pos.x ? -1f : 1f) * 6f, 5f));
                    VFX.Shatter(f.Center, ClashDefs[i].Primary);
                }
                Clashing = false;
                DomainFX.EndClash();
                M.FreezeTimer = 0f;
                M.Events.ClashEnd(winner);
                Audio.Play(Sfx.DomainShatter, winner.Center);
                VFX.ImpactFrame(ImpactKind.Domain, winner.Center);
                var def = ClashDefs[win];
                float pw = clashPower[win] * 1.1f;
                ClashFighters.Clear(); ClashDefs.Clear(); ClashScore.Clear(); clashPower.Clear();
                if (!winner.Dead) Manifest(winner, def, pw, false);
            }
        }

        readonly List<float> shares = new List<float>(4);
        public List<float> Shares()
        {
            shares.Clear();
            float total = 0f;
            for (int i = 0; i < ClashScore.Count; i++) total += Mathf.Max(1f, ClashScore[i]);
            for (int i = 0; i < ClashScore.Count; i++) shares.Add(Mathf.Max(1f, ClashScore[i]) / Mathf.Max(1f, total));
            return shares;
        }

        // ------------------------------------------------------------ tick
        public void Tick(float dt, bool frozen)
        {
            if (Clashing) { TickClash(dt); return; }
            if (frozen) return;

            for (int i = Actives.Count - 1; i >= 0; i--)
            {
                var a = Actives[i];
                a.Time += dt;
                bool broken = !a.Def.Open && a.Owner.DamageTakenWindow > a.Owner.MaxHp * 0.24f;
                if (a.Owner.Dead || a.Time >= a.Duration || broken)
                {
                    Collapse(a, broken);
                    Actives.RemoveAt(i);
                    continue;
                }
                ApplyEffect(a, dt);
            }
        }

        void Collapse(Active a, bool broken)
        {
            DomainFX.Hide(a.Def);
            VFX.Shatter(a.Owner.Center, a.Def.Primary);
            Audio.Play(Sfx.DomainShatter, a.Owner.Center);
            CameraRig.Shake(0.4f);
            if (!a.Owner.Dead)
            {
                float burn = a.Owner.Def.Has(Passive.SixEyes) ? 4f : 8f;
                if (broken) { burn += 3f; M.Events.Announce("BARRIER BROKEN", a.Owner.Def.Name, a.Def.Primary, 1.4f); }
                a.Owner.AddStatus(StatusType.Burnout, burn, 0f, a.Owner);
            }
            M.Events.DomainEnded(a.Owner, a.Def);
        }

        void CollectVictims(Active a)
        {
            victims.Clear();
            var fs = M.Fighters;
            for (int i = 0; i < fs.Count; i++)
            {
                var f = fs[i];
                if (f.Dead || !M.IsEnemy(a.Owner, f) || f.State == FState.Respawning) continue;
                if (f.IsRestricted)
                {
                    // no cursed energy: the sure-hit cannot register them
                    if (!undetectedShown.Contains(f)) { undetectedShown.Add(f); VFX.WorldText(f.HeadPos + Vector2.up * 0.8f, "UNDETECTED", Color.white, 1.2f); }
                    continue;
                }
                victims.Add(f);
            }
        }

        static float Guard(Fighter v)
        {
            if (v.State != FState.SimpleDomain) return 1f;
            return v.Def.Has(Passive.SimpleDomainMaster) ? 0.05f : 0.3f;
        }

        void SureHitDamage(Active a, Fighter v, float dmg, DamageType type, HitFlags extra, Vector2 kb, float stun)
        {
            var h = HitInfo.Make(a.Owner, HitSource.Domain, dmg * Guard(v), kb, stun, HitFlags.SureHit | HitFlags.Unblockable | HitFlags.PierceInfinity | HitFlags.NoCombo | HitFlags.Technique | extra,
                                 type, v.Center, a.Def.Primary);
            h.Hitstop = 0.03f;
            h.CeGain = 0f;
            M.Hit(v, h);
        }

        void ApplyEffect(Active a, float dt)
        {
            CollectVictims(a);
            a.TickA -= dt; a.TickB -= dt;
            float p = a.Power * a.Def.Power;
            var owner = a.Owner;

            switch (a.Def.Effect)
            {
                case SureHit.Paralyze:
                {
                    float cap = Mathf.Min(5f, 3.2f * p);
                    foreach (var v in victims)
                    {
                        if (a.Time < cap && Guard(v) > 0.5f) { v.AddStatus(StatusType.Paralysis, 0.25f, 0f, owner); if (a.TickA <= 0f) VFX.Info(v.HeadPos); }
                        else v.AddStatus(StatusType.Slow, 0.3f, 0.35f, owner);
                    }
                    if (a.TickA <= 0f) a.TickA = 0.3f;
                    owner.AddStatus(StatusType.PowerUp, 0.3f, 0.2f, owner);
                    break;
                }
                case SureHit.Slashes:
                    if (a.TickA <= 0f)
                    {
                        a.TickA = 0.17f;
                        foreach (var v in victims)
                        {
                            SureHitDamage(a, v, 7f * p, DamageType.Slash, HitFlags.None, Vector2.zero, 0.05f);
                            VFX.SlashMark(v.Center, a.Def.Primary);
                        }
                        // everything inside is minced: summons and constructs too
                        foreach (var mn in M.Minions) if (!mn.Dead && M.IsEnemyTeam(owner.Team, mn.Owner.Team)) mn.TakeDamage(25f * p, owner, Vector2.zero);
                        foreach (var pr in M.Projectiles) if (!pr.Dead && !pr.D.Unerasable && M.IsEnemyTeam(owner.Team, pr.Team)) pr.Finish(M);
                        Audio.Play(Sfx.Slash, owner.Center, 0.35f, Random.Range(0.8f, 1.3f));
                    }
                    break;
                case SureHit.Burn:
                    foreach (var v in victims) v.AddStatus(StatusType.Burn, 0.6f, 14f * p * Guard(v), owner);
                    if (a.TickA <= 0f)
                    {
                        a.TickA = 1.5f;
                        foreach (var v in victims)
                        {
                            SureHitDamage(a, v, 40f * p, DamageType.Fire, HitFlags.Heavy, new Vector2(0f, 9f), 0.45f);
                            VFX.Burst(BurstVis.Pillar, new Vector2(v.Pos.x, 0f), 1.6f, a.Def.Primary);
                        }
                        Audio.Play(Sfx.Fire, owner.Center, 0.8f, 0.7f);
                        CameraRig.Shake(0.3f);
                    }
                    break;
                case SureHit.Swarm:
                    if (a.TickA <= 0f && victims.Count > 0)
                    {
                        a.TickA = 0.2f;
                        var v = victims[M.Rng.Range(0, victims.Count)];
                        float side = M.Rng.Chance(0.5f) ? M.Arena.Left - 1f : M.Arena.Right + 1f;
                        var d = DomainProj(ProjVis.Water, a.Def.Primary, 9f * p, DamageType.Water);
                        d.Homing = 3f; d.Speed = 15f; d.DestroyOnWall = false;
                        M.Shoot(owner, d, new Vector2(side, M.Rng.Range(0.5f, 6f)), v.Center - new Vector2(side, 3f), 1f);
                    }
                    foreach (var v in victims) v.AddStatus(StatusType.Slow, 0.3f, 0.25f, owner);
                    break;
                case SureHit.Sink:
                    foreach (var v in victims) v.AddStatus(StatusType.Sink, 0.3f, 0f, owner);
                    owner.AddStatus(StatusType.Haste, 0.3f, 0.3f, owner);
                    if (a.TickA <= 0f)
                    {
                        a.TickA = 1.1f;
                        foreach (var v in victims)
                        {
                            SureHitDamage(a, v, 22f * p, DamageType.Blunt, HitFlags.None, new Vector2(0f, 2f), 0.3f);
                            v.AddStatus(StatusType.Bind, 0.45f * Guard(v), 0f, owner);
                            VFX.Tendrils(new Vector2(v.Pos.x, 0f), a.Def.Primary);
                        }
                    }
                    break;
                case SureHit.Swords:
                    owner.AddStatus(StatusType.PowerUp, 0.3f, 0.35f, owner);
                    owner.AddStatus(StatusType.Haste, 0.3f, 0.2f, owner);
                    if (a.TickA <= 0f && victims.Count > 0)
                    {
                        a.TickA = 0.5f;
                        var v = victims[M.Rng.Range(0, victims.Count)];
                        var d = DomainProj(ProjVis.Slash, a.Def.Primary, 30f * p, DamageType.Slash);
                        d.Speed = 26f; d.Size = 0.9f;
                        M.Shoot(owner, d, new Vector2(v.Pos.x + M.Rng.Range(-2f, 2f), M.Arena.Ceiling + 1f), v.Center - new Vector2(v.Pos.x, M.Arena.Ceiling + 1f), 1f);
                    }
                    break;
                case SureHit.Verdict:
                    if (!a.Fired)
                    {
                        a.Fired = true;
                        var dfd = a.Defendant;
                        if (dfd == null || dfd.Dead || dfd.IsRestricted) { M.Events.Announce("CASE DISMISSED", "", a.Def.Primary, 1.5f); break; }
                        float guiltyChance = 0.5f + Mathf.Min(0.35f, dfd.StatDamage / 2600f);
                        if (dfd.State == FState.SimpleDomain) guiltyChance *= 0.4f;
                        if (M.Rng.Chance(guiltyChance))
                        {
                            if (M.Rng.Chance(0.55f))
                            {
                                dfd.AddStatus(StatusType.Sealed, 14f, 0f, owner);
                                M.Events.Announce("GUILTY", "CONFISCATION - techniques sealed", new Color(1f, 0.85f, 0.3f), 2f);
                            }
                            else
                            {
                                owner.AddStatus(StatusType.Executioner, 12f, 1.0f, owner);
                                M.Events.Announce("GUILTY", "DEATH PENALTY - executioner's blade", new Color(1f, 0.25f, 0.25f), 2f);
                            }
                            Audio.Play(Sfx.Gavel, owner.Center, 1f, 0.8f);
                        }
                        else
                        {
                            M.Events.Announce("NOT GUILTY", "", new Color(0.7f, 0.9f, 1f), 1.6f);
                            Audio.Play(Sfx.Gavel, owner.Center, 0.8f, 1.2f);
                        }
                    }
                    break;
                case SureHit.Jackpot:
                    if (!a.Fired)
                    {
                        a.Fired = true;
                        float chance = 0.34f + 0.12f * owner.ChantLevel;
                        bool hit = M.Rng.Chance(chance);
                        DomainFX.JackpotResult(hit);
                        if (hit)
                        {
                            owner.AddStatus(StatusType.Jackpot, 14f, 55f, owner);
                            M.Events.Announce("JACKPOT!!", "infinite cursed energy + auto reverse technique", new Color(1f, 0.85f, 0.2f), 2.2f);
                            Audio.Play(Sfx.Jackpot, owner.Center);
                        }
                        else
                        {
                            owner.GainCe(50f);
                            owner.AddStatus(StatusType.PowerUp, 8f, 0.15f, owner);
                            M.Events.Announce("MISS", "the reels didn't align... (CE refunded)", new Color(0.8f, 0.8f, 0.8f), 1.6f);
                        }
                    }
                    break;
                case SureHit.SoulTouch:
                    if (!a.Fired && a.Time > 1.1f)
                    {
                        a.Fired = true;
                        foreach (var v in victims)
                        {
                            float dmg = v.MaxHp * 0.36f * Mathf.Min(1.4f, p);
                            SureHitDamage(a, v, dmg, DamageType.Soul, HitFlags.Soul | HitFlags.Heavy, new Vector2(0f, 6f), 0.8f);
                            VFX.SoulTouch(v.Center, a.Def.Primary);
                        }
                        VFX.ImpactFrame(ImpactKind.Domain, owner.Center);
                        Audio.Play(Sfx.PunchHeavy, owner.Center, 1f, 0.5f);
                    }
                    owner.AddStatus(StatusType.PowerUp, 0.3f, 0.25f, owner);
                    break;
                case SureHit.Gravity:
                    foreach (var v in victims) v.AddStatus(StatusType.Gravity, 0.3f, 16f * p * Guard(v), owner);
                    break;
                case SureHit.MoveCut:
                    owner.AddStatus(StatusType.Haste, 0.3f, 0.45f, owner);
                    foreach (var v in victims)
                    {
                        float moved = v.MovedDistance;
                        v.MovedDistance = 0f;
                        if (moved > 0.02f) v.TakeTrueDamage(moved * 13f * p * Guard(v), owner);
                        if (v.State == FState.Attack || v.State == FState.Cast)
                            if (a.TickB <= 0f) { v.TakeTrueDamage(8f * p * Guard(v), owner); VFX.SlashMark(v.Center, a.Def.Primary); }
                    }
                    if (a.TickB <= 0f) a.TickB = 0.25f;
                    break;
                case SureHit.SoulStrike:
                    owner.AddStatus(StatusType.PowerUp, 0.3f, 0.5f, owner);
                    foreach (var v in victims) v.AddStatus(StatusType.Slow, 0.3f, 0.25f, owner);
                    if (a.TickA <= 0f)
                    {
                        a.TickA = 1.8f;
                        foreach (var v in victims) { SureHitDamage(a, v, 30f * p, DamageType.Soul, HitFlags.Soul, Vector2.zero, 0.25f); VFX.SoulTouch(v.Center, a.Def.Primary); }
                    }
                    break;
                case SureHit.Bloom:
                    foreach (var v in victims)
                    {
                        float drain = 10f * dt * Guard(v);
                        if (v.Ce > 0f) { v.Ce = Mathf.Max(0f, v.Ce - drain); owner.GainCe(drain); }
                    }
                    if (a.TickA <= 0f)
                    {
                        a.TickA = 3.2f;
                        foreach (var v in victims) { v.AddStatus(StatusType.Bind, 1.1f * Guard(v), 0f, owner); SureHitDamage(a, v, 20f * p, DamageType.Blunt, HitFlags.None, Vector2.zero, 0.2f); VFX.Burst(BurstVis.Bloom, new Vector2(v.Pos.x, 0.3f), 1.2f, a.Def.Primary); }
                    }
                    break;
                case SureHit.TrueSphere:
                    if (!a.Fired && a.Time > 1.5f)
                    {
                        a.Fired = true;
                        foreach (var v in victims)
                        {
                            SureHitDamage(a, v, 210f * p, DamageType.Blunt, HitFlags.Heavy, new Vector2(0f, 12f), 0.9f);
                            VFX.Explosion(v.Center, 2.2f, a.Def.Primary, ProjVis.Sphere);
                        }
                        VFX.ImpactFrame(ImpactKind.Domain, owner.Center);
                        CameraRig.Shake(0.8f);
                        Audio.Play(Sfx.Explosion, owner.Center, 1f, 0.6f);
                    }
                    break;
            }
        }

        static ProjDef DomainProj(ProjVis vis, Color c, float dmg, DamageType type)
        {
            return new ProjDef
            {
                Vis = vis, Color = c, Core = Color.white, Size = 0.45f, Speed = 18f, Life = 3f, Damage = dmg, Hitstun = 0.25f,
                Knockback = new Vector2(2f, 2f), Flags = HitFlags.SureHit | HitFlags.Unblockable | HitFlags.PierceInfinity | HitFlags.Technique | HitFlags.NoCombo,
                Type = type, Unerasable = true, Launch = Sfx.None, HitsMinions = false
            };
        }
    }
}
