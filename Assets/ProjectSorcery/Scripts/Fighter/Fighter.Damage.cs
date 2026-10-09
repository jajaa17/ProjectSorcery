using UnityEngine;

namespace ProjectSorcery
{
    public sealed partial class Fighter
    {
        // =====================================================================
        //  RECEIVING HITS
        // =====================================================================
        public HitResult ReceiveHit(ref HitInfo h)
        {
            if (Dead || State == FState.Respawning) return HitResult.Miss;
            var atk = h.Attacker;
            if (atk != null) h.Damage *= atk.Def.DamageMul;
            bool sure = h.Has(HitFlags.SureHit);

            if (Has(StatusType.Comedy) && !sure) { VFX.WorldText(HeadPos + Vector2.up * 0.6f, "NOT FUNNY", new Color(1f, 0.9f, 0.3f), 0.7f); return HitResult.Nullified; }
            if ((Invuln > 0f || Has(StatusType.Invuln)) && !sure) return HitResult.Dodged;

            // Counter stance
            if (counter != null && !sure && atk != null && (!counter.TechniqueOnly || h.Has(HitFlags.Technique)))
            {
                DoCounter(atk);
                return HitResult.Countered;
            }

            // Boundless (Infinity): everything slows to a stop before touching
            bool atkPierces = atk != null && (atk.Has(StatusType.Amplification) || atk.Has(StatusType.Executioner) ||
                                              (atk.Def.Has(Passive.Adaptation) && atk.InfinityHitsTaken >= 4));
            // Boundless is always on. The ways through: sure-hit domains, techniques that pierce it, soul strikes,
            // adaptation, sealing/burnout, or draining the cursed energy that sustains it below 20%.
            bool focusLost = Has(StatusType.Burnout) || Has(StatusType.Sealed) || (Casting is DomainAb);
            if (Def.Has(Passive.Infinity) && Ce > MaxCe * 0.2f && !sure && !h.Has(HitFlags.PierceInfinity) && !h.Has(HitFlags.Soul)
                && !atkPierces && !focusLost)
            {
                Ce = Mathf.Max(0f, Ce - (h.Has(HitFlags.Heavy) ? 15f : 8f));
                if (atk != null)
                {
                    atk.InfinityHitsTaken++;
                    if (atk.Def.Has(Passive.Adaptation) && atk.InfinityHitsTaken == 4)
                        VFX.WorldText(atk.HeadPos + Vector2.up * 0.8f, "ADAPTED", new Color(1f, 0.95f, 0.6f), 1f);
                }
                VFX.InfinityRipple(h.Point, Def.Look.Aura);
                Audio.Play(Sfx.Infinity, h.Point, 0.5f);
                if (atk != null && h.Source == HitSource.Melee) { atk.Vel.x = -atk.Facing * 3.5f; atk.Hitstop = Mathf.Max(atk.Hitstop, 0.05f); }
                if (Ce <= MaxCe * 0.2f) VFX.WorldText(HeadPos + Vector2.up * 0.6f, "BOUNDLESS DOWN", Def.Look.Aura, 0.9f);
                return HitResult.Nullified;
            }

            // Blocking
            bool facingAttacker = atk == null || (atk.Pos.x - Pos.x) * Facing >= -0.2f || h.Source == HitSource.Projectile && h.Knockback.x * Facing <= 0f;
            bool blocking = (State == FState.Block || State == FState.SimpleDomain && !sure) && facingAttacker;
            if (blocking && !h.Has(HitFlags.Unblockable) && !sure && !h.Has(HitFlags.GuardBreak))
            {
                float gdmg = h.Damage * (h.Has(HitFlags.Heavy) ? 0.9f : 0.5f);
                Guard -= gdmg * (IsBoss ? 0.3f : 1f);
                // cursed techniques still burn through a guard a little (chip damage)
                ApplyDamage(h.Damage * (h.Has(HitFlags.Technique) ? 0.22f : 0.1f), atk, h.Type);
                Vel.x = Mathf.Sign(h.Knockback.x == 0f ? -Facing : h.Knockback.x) * Mathf.Min(6f, Mathf.Abs(h.Knockback.x) * 0.5f + 1.5f);
                GuardImpulse = Mathf.Clamp(h.Damage / 45f, 0.3f, 1.5f);
                Hitstop = Mathf.Max(Hitstop, h.Hitstop * 0.7f);
                VFX.BlockSpark(h.Point, Facing, Def.Look.Aura);
                Audio.Play(Sfx.Block, h.Point, 0.7f);
                if (Guard <= 0f) GuardBreak();
                return HitResult.Blocked;
            }

            // ---------- damage modifiers ----------
            float dmg = h.Damage;
            if (h.Has(HitFlags.Soul))
            {
                if (Def.Has(Passive.SoulResist)) dmg *= 0.3f;
                if (Def.Has(Passive.PerfectBody)) dmg *= 1.35f;
            }
            else if (Def.Has(Passive.PerfectBody) && !h.Has(HitFlags.BlackFlash)) dmg *= 0.75f;
            if (Def.Has(Passive.CurseBody) && h.Type == DamageType.Light) dmg *= 1.5f;
            if (Def.Has(Passive.WeaknessInverted))
            {
                if (h.Damage >= 60f) dmg *= 0.55f; else if (h.Damage < 30f) dmg *= 1.4f;
            }
            if (Def.Has(Passive.Adaptation) && !h.Has(HitFlags.SureHit))
            {
                int ti = (int)h.Type;
                dmg *= 1f - Adapt[ti];
                if (Adapt[ti] < 0.7f)
                {
                    Adapt[ti] = Mathf.Min(0.7f, Adapt[ti] + 0.05f);
                    if (Adapt[ti] >= 0.5f && Adapt[ti] - 0.05f < 0.5f)
                    {
                        VFX.WorldText(HeadPos + Vector2.up * 0.8f, "ADAPTED", new Color(1f, 0.95f, 0.6f), 1f);
                        Audio.Play(Sfx.Bell, Center, 0.9f);
                    }
                }
            }
            dmg /= Def.Defense * Def.ToughMul;
            if (Has(StatusType.Weaken)) dmg *= 1f + StatusMag(StatusType.Weaken);
            if (Has(StatusType.DefenseUp)) dmg *= 1f - Mathf.Clamp(StatusMag(StatusType.DefenseUp), 0f, 0.8f);
            if (Has(StatusType.Shielded)) dmg *= 1f - Mathf.Clamp(StatusMag(StatusType.Shielded), 0f, 0.9f);
            if (State == FState.SimpleDomain && sure) dmg *= Def.Has(Passive.SimpleDomainMaster) ? 0.1f : 0.35f;

            // combo damage scaling & juggle decay (prevents infinites)
            bool inCombo = State == FState.Hitstun || State == FState.Knockdown;
            if (!h.Has(HitFlags.NoCombo) && !sure)
            {
                if (inCombo) Juggle++; else Juggle = 1;
                if (Juggle > 2) dmg *= Mathf.Max(0.35f, 1f - 0.06f * (Juggle - 2));
            }

            // resonance mark transfers extra damage
            if (Has(StatusType.Mark) && atk != null && StatusSrc(StatusType.Mark) == atk && h.Has(HitFlags.Technique)) dmg *= 1.25f;

            ApplyDamage(dmg, atk, h.Type);
            LastAttacker = atk;
            DamageTakenWindow += dmg;
            FlashTimer = 0.08f;
            HitImpulse = Mathf.Clamp(dmg / 40f, 0.35f, 3f) * (h.Has(HitFlags.BlackFlash) ? 1.8f : h.Has(HitFlags.Heavy) ? 1.3f : 1f);
            HitDir = h.Knockback.x >= 0f ? 1 : -1;

            if (atk != null)
            {
                atk.StatDamage += dmg;
                atk.GainCe(h.CeGain);
                if (!h.Has(HitFlags.NoCombo))
                {
                    atk.ComboHits = inCombo || atk.ComboTimer > 0f ? atk.ComboHits + 1 : 1;
                    atk.ComboDamage += dmg;
                    atk.ComboTimer = Mathf.Max(0.35f, h.Hitstun + 0.25f);
                    if (atk.ComboHits > atk.StatMaxCombo) atk.StatMaxCombo = atk.ComboHits;
                }
            }
            if (!IsRestricted) GainCe(dmg * 0.04f);

            // status
            if (h.Status != StatusType.Count && h.StatusTime > 0f)
            {
                bool cc = h.Status == StatusType.Stun || h.Status == StatusType.Freeze || h.Status == StatusType.Paralysis;
                float time = h.StatusTime;
                if (cc && IsBoss) time *= 0.4f;
                AddStatus(h.Status, time, h.StatusMag, atk);
            }

            // visuals & sound
            if (Settings.DamageNumbers && dmg >= 1f) VFX.DamageNumber(h.Point, dmg, h.Has(HitFlags.BlackFlash), h.Color);
            if (!h.Has(HitFlags.BlackFlash))
            {
                bool heavy = h.Has(HitFlags.Heavy) || dmg > 60f;
                VFX.HitSpark(h.Point, h.Knockback.x >= 0 ? 1 : -1, h.Color, heavy);
                if (heavy && h.Source != HitSource.Domain) { VFX.ImpactFrame(ImpactKind.Heavy, h.Point); CameraRig.Punch(0.18f); }
                Audio.Play(HitSound(h), h.Point, heavy ? 1f : 0.75f, Random.Range(0.92f, 1.08f));
            }

            if (Hp <= 0f)
            {
                if ((Def.Has(Passive.LuckStored) && !LuckUsed) || Has(StatusType.Lucky))
                {
                    LuckUsed = true;
                    RemoveStatus(StatusType.Lucky);
                    Hp = 1f;
                    Invuln = 1.5f;
                    VFX.WorldText(HeadPos + Vector2.up * 0.8f, "MIRACLE", new Color(0.6f, 1f, 0.6f), 1.2f);
                    Audio.Play(Sfx.Bell, Center);
                }
                else
                {
                    Die(h);
                    return HitResult.Hit;
                }
            }

            // ---------- reaction ----------
            bool armored = Has(StatusType.Armor) || (Casting != null && Casting.Armored) || (Def.Has(Passive.ToughBody) && CurAttack != null && (CurAttack.Flags & HitFlags.Heavy) != 0);
            if (IsBoss)
            {
                StaggerMeter += dmg * (h.Has(HitFlags.Heavy) ? 1.5f : 1f) * (h.Has(HitFlags.BlackFlash) ? 3f : 1f);
                armored = StaggerMeter < MaxHp * 0.06f;
                if (!armored) { StaggerMeter = 0f; VFX.WorldText(HeadPos + Vector2.up * 1f, "STAGGER", Color.white, 1.2f); }
            }
            if (armored && !h.Has(HitFlags.BlackFlash) && !h.Has(HitFlags.IgnoreArmor))
            {
                Hitstop = Mathf.Max(Hitstop, h.Hitstop * 0.5f);
                return HitResult.Hit;
            }

            CancelCast();
            strike = null;
            CurAttack = null;
            ComboStep = 0;
            if (State == FState.RCT || State == FState.SimpleDomain || State == FState.Block) SetState(FState.Idle);

            float weight = Def.Weight * (IsBoss ? 3f : 1f);
            Vector2 kb = h.Knockback / weight;
            if (inCombo && Juggle > 4) kb.y *= Mathf.Max(0.5f, 1f - 0.05f * (Juggle - 4));
            Vel = kb;
            Launched = Mathf.Abs(kb.y) > 5f || (Launched && inCombo && !Grounded);
            if (kb.y > 0.5f) Grounded = false;
            // hitstun decays through a combo so nothing loops forever; past a hard cap the victim breaks free
            float stun = h.Hitstun * (inCombo ? Mathf.Max(0.35f, 1f - 0.065f * Juggle) : 1f);
            Hitstun = Mathf.Max(stun, State == FState.Hitstun ? Hitstun * 0.4f : 0f);
            if (inCombo && Juggle >= 11 && !IsBoss && !sure)
            {
                Invuln = Mathf.Max(Invuln, 0.7f);
                Vel = new Vector2(Mathf.Sign(kb.x == 0f ? -Facing : kb.x) * 7f, 7f);
                Grounded = false;
                Juggle = 99; Launched = true;
                VFX.WorldText(HeadPos + Vector2.up * 0.7f, "BREAK", Color.white, 0.8f);
            }
            if ((h.Flags & HitFlags.NoHitstop) == 0) Hitstop = Mathf.Max(Hitstop, h.Hitstop);
            if (h.Knockback.x != 0f) Facing = h.Knockback.x > 0 ? -1 : 1;
            SetState(FState.Hitstun);
            StateTime = 0f;
            Audio.Play(Sfx.Hurt, Center, 0.35f, Random.Range(0.9f, 1.15f));
            return HitResult.Hit;
        }

        static Sfx HitSound(in HitInfo h)
        {
            switch (h.Type)
            {
                case DamageType.Slash: return Sfx.Slash;
                case DamageType.Fire: return Sfx.Fire;
                case DamageType.Electric: return Sfx.Electric;
                case DamageType.Ice: return Sfx.Ice;
                case DamageType.Water: return Sfx.Water;
                case DamageType.Blood: return Sfx.Blood;
                case DamageType.Energy: return h.Has(HitFlags.Heavy) ? Sfx.PunchHeavy : Sfx.ImpactEnergy;
                default: return h.Has(HitFlags.Heavy) ? Sfx.PunchHeavy : Sfx.Punch;
            }
        }

        void DoCounter(Fighter atk)
        {
            var c = counter;
            counter = null; counterTimer = 0f;
            Invuln = 0.4f;
            if (c.Teleport)
            {
                float nx = atk.Pos.x - atk.Facing * 1.1f;
                Teleport(new Vector2(nx, atk.Pos.y));
            }
            Facing = atk.Pos.x > Pos.x ? 1 : -1;
            var h = HitInfo.Make(this, HitSource.Other, c.Damage * PowerNoBase(), new Vector2(c.Knockback.x * Facing, c.Knockback.y), 0.6f,
                                 HitFlags.Heavy | HitFlags.Unblockable | HitFlags.PierceInfinity, c.Type, atk.Center, c.Color);
            h.Hitstop = 0.14f;
            Hitstop = 0.14f;
            M.Hit(atk, h);
            VFX.CounterFlash(Center, Facing, c.Color);
            VFX.WorldText(HeadPos + Vector2.up * 0.7f, "COUNTER", c.Color, 1f);
            Audio.Play(Sfx.Slash, Center, 1f, 0.8f);
            CameraRig.Punch(0.25f);
            Casting = null;
            SetState(FState.Idle);
        }

        void GuardBreak()
        {
            Guard = 45f;
            SetState(FState.Hitstun);
            Hitstun = IsBoss ? 0.4f : 0.9f;
            Vel = new Vector2(-Facing * 3f, 3f);
            Grounded = false;
            AddStatus(StatusType.Stun, IsBoss ? 0.3f : 0.6f, 0f, null);
            VFX.WorldText(HeadPos + Vector2.up * 0.6f, "GUARD BREAK", new Color(1f, 0.4f, 0.3f), 1f);
            VFX.Burst(BurstVis.Shockwave, Center, 1.6f, new Color(1f, 0.5f, 0.3f));
            Audio.Play(Sfx.GuardBreak, Center);
            CameraRig.Shake(0.3f);
        }

        void ApplyDamage(float dmg, Fighter atk, DamageType type)
        {
            if (dmg <= 0f) return;
            Hp -= dmg;
            StatTaken += dmg;
            M.Events.Damaged(this, atk, dmg);
        }

        /// <summary>Damage from own techniques (cursed speech backlash, amber body breakdown).</summary>
        public void TakeSelfDamage(float dmg)
        {
            if (Dead) return;
            Hp -= dmg;
            if (Hp < 1f) Hp = 1f;
            FlashTimer = 0.06f;
        }

        /// <summary>Unavoidable damage from statuses, zones and sure-hit (no hit reaction).</summary>
        public void TakeTrueDamage(float dmg, Fighter src)
        {
            if (Dead || dmg <= 0f || State == FState.Respawning) return;
            if (Has(StatusType.Comedy)) return;
            if (src != null) dmg *= src.Def.DamageMul;
            Hp -= dmg / (Def.Defense * Def.ToughMul);
            if (src != null) src.StatDamage += dmg;
            if (Hp <= 0f)
            {
                if ((Def.Has(Passive.LuckStored) && !LuckUsed) || Has(StatusType.Lucky))
                {
                    LuckUsed = true; RemoveStatus(StatusType.Lucky); Hp = 1f; Invuln = 1.5f;
                    VFX.WorldText(HeadPos + Vector2.up * 0.8f, "MIRACLE", new Color(0.6f, 1f, 0.6f), 1.2f);
                }
                else
                {
                    var h = HitInfo.Make(src, HitSource.Other, 0f, new Vector2(-Facing * 6f, 6f), 1f, HitFlags.None, DamageType.Energy, Center, Color.white);
                    LastAttacker = src;
                    Die(h);
                }
            }
        }

        void Die(HitInfo h)
        {
            Hp = 0f;
            Dead = true;
            CancelCast();
            strike = null;
            SetState(FState.Dead);
            Vel = new Vector2(Mathf.Sign(h.Knockback.x == 0 ? -Facing : h.Knockback.x) * 11f, 9f);
            Grounded = false;
            var killer = h.Attacker ?? LastAttacker;
            if (killer != null && killer != this) killer.StatKOs++;
            M.SlowMo(0.2f, 0.9f);
            VFX.ImpactFrame(ImpactKind.Ko, Center);
            VFX.KO(Center, Def.Look.Aura);
            Audio.Play(Sfx.KO, Center);
            CameraRig.Punch(0.4f);
            CameraRig.Shake(0.5f);
            M.Events.Killed(this, killer);
        }

        public void Revive(Vector2 pos, float hpFrac)
        {
            Dead = false;
            Hp = MaxHp * hpFrac;
            Ce = MaxCe * 0.5f;
            Pos = PrevPos = pos;
            Vel = Vector2.zero;
            Statuses.Clear();
            Invuln = 2f;
            SetState(FState.Idle);
            VFX.Summon(Center, Def.Look.Aura);
        }

        public void Heal(float amount, bool fx = true)
        {
            if (Dead || amount <= 0f) return;
            Hp = Mathf.Min(MaxHp, Hp + amount);
            if (fx) VFX.Heal(Center, new Color(0.6f, 1f, 0.7f));
        }

        public void GainCe(float amount)
        {
            if (IsRestricted || amount <= 0f) return;
            Ce = Mathf.Min(MaxCe, Ce + amount);
        }

        // =====================================================================
        //  STATUSES
        // =====================================================================
        public bool Has(StatusType t)
        {
            for (int i = 0; i < Statuses.Count; i++) if (Statuses[i].Type == t) return true;
            return false;
        }

        public float StatusMag(StatusType t)
        {
            for (int i = 0; i < Statuses.Count; i++) if (Statuses[i].Type == t) return Statuses[i].Mag;
            return 0f;
        }

        public float StatusTime(StatusType t)
        {
            for (int i = 0; i < Statuses.Count; i++) if (Statuses[i].Type == t) return Statuses[i].Time;
            return 0f;
        }

        public Fighter StatusSrc(StatusType t)
        {
            for (int i = 0; i < Statuses.Count; i++) if (Statuses[i].Type == t) return Statuses[i].Src;
            return null;
        }

        public void AddStatus(StatusType t, float time, float mag, Fighter src)
        {
            if (t == StatusType.Count || time <= 0f || Dead) return;
            // heavenly restriction bodies shrug off cursed binds faster
            if (IsRestricted && (t == StatusType.Bind || t == StatusType.Slow || t == StatusType.Sink)) time *= 0.6f;
            for (int i = 0; i < Statuses.Count; i++)
            {
                if (Statuses[i].Type != t) continue;
                var s = Statuses[i];
                s.Time = Mathf.Max(s.Time, time);
                s.Mag = Mathf.Max(s.Mag, mag);
                if (src != null) s.Src = src;
                Statuses[i] = s;
                return;
            }
            Statuses.Add(new Status { Type = t, Time = time, Mag = mag, Src = src });
            M.Events.StatusAdded(this, t);
        }

        public void RemoveStatus(StatusType t)
        {
            for (int i = Statuses.Count - 1; i >= 0; i--) if (Statuses[i].Type == t) Statuses.RemoveAt(i);
        }

        void ReduceStatus(StatusType t, float amount)
        {
            for (int i = 0; i < Statuses.Count; i++)
            {
                if (Statuses[i].Type != t) continue;
                var s = Statuses[i]; s.Time -= amount; Statuses[i] = s;
            }
        }

        public void ClearDebuffs()
        {
            for (int i = Statuses.Count - 1; i >= 0; i--)
            {
                var t = Statuses[i].Type;
                if (t == StatusType.Stun || t == StatusType.Slow || t == StatusType.Burn || t == StatusType.Bind || t == StatusType.Freeze ||
                    t == StatusType.Mark || t == StatusType.Weaken || t == StatusType.Poison || t == StatusType.Sink || t == StatusType.Bleed ||
                    t == StatusType.Confused || t == StatusType.Rot || t == StatusType.Paralysis)
                    Statuses.RemoveAt(i);
            }
        }

        void TickStatuses(float dt)
        {
            // decrement first, apply effects after: damage can add/remove statuses (lucky saves, deaths)
            float dot = 0f, jackpotHeal = 0f;
            bool jackpot = false;
            Fighter dotSrc = null;
            for (int i = Statuses.Count - 1; i >= 0; i--)
            {
                var s = Statuses[i];
                s.Time -= dt;
                switch (s.Type)
                {
                    case StatusType.Burn:
                    case StatusType.Poison:
                    case StatusType.Bleed:
                    case StatusType.Gravity:
                        dot += s.Mag * dt; dotSrc = s.Src ?? dotSrc;
                        break;
                    case StatusType.Rot:
                        dot += s.Mag * dt; dotSrc = s.Src ?? dotSrc;
                        s.Mag += 2f * dt;
                        break;
                    case StatusType.Jackpot:
                        jackpot = true; jackpotHeal += s.Mag * dt;
                        break;
                }
                if (s.Time <= 0f) { Statuses.RemoveAt(i); M.Events.StatusEnded(this, s.Type); }
                else Statuses[i] = s;
            }
            if (jackpot && !Dead) { Ce = MaxCe; Hp = Mathf.Min(MaxHp, Hp + jackpotHeal); }
            if (dot > 0f) TakeTrueDamage(dot, dotSrc);
        }

        void TickResources(float dt)
        {
            for (int i = 0; i < 4; i++)
                if (Cd[i] > 0f) Cd[i] -= dt * (Has(StatusType.Zone) ? 1.25f : 1f) * (M.Domains.IsOwnerActive(this) && M.Domains.OwnerEffect(this) == SureHit.Swords ? 2.5f : 1f);
            if (Dead) return;
            if (!IsRestricted && State != FState.RCT && State != FState.SimpleDomain)
            {
                float regen = Def.CeRegen * (Has(StatusType.Zone) ? 1.6f : 1f) * (M.Mode != null ? M.Mode.CeRegenMul : 1f);
                if (Casting == null) Ce = Mathf.Min(MaxCe, Ce + regen * dt);
            }
            if (Def.Has(Passive.AutoRCT) && Hp < MaxHp && Ce > 10f && State != FState.Hitstun)
            {
                float r = Def.RctRate * 0.15f * dt;
                Hp = Mathf.Min(MaxHp, Hp + r);
                Ce -= r * 0.35f;
            }
            if (Def.Has(Passive.CurseBody) && Hp < MaxHp && State != FState.Hitstun && Ce > 20f)
            {
                // cursed spirits mend their bodies with raw cursed energy
                float r = 4f * dt;
                Hp = Mathf.Min(MaxHp, Hp + r);
                Ce -= r * 0.6f;
            }
            if (Def.Has(Passive.Support))
            {
                foreach (var a in M.Fighters)
                    if (a != this && !a.Dead && a.Team == Team && (a.Pos - Pos).sqrMagnitude < 16f) a.Heal(4f * dt, false);
            }
            if (State != FState.Block && Guard < 100f) Guard = Mathf.Min(100f, Guard + 20f * dt);
            if (IsBoss && StaggerMeter > 0f) StaggerMeter = Mathf.Max(0f, StaggerMeter - MaxHp * 0.01f * dt);
        }

        // =====================================================================
        //  HELPERS used by domains / modes
        // =====================================================================
        public void Lock(bool locked)
        {
            if (Dead) return;
            if (locked)
            {
                if (State == FState.Locked) return;
                CancelCast();
                strike = null; CurAttack = null;
                SetState(FState.Locked);
                Vel = Vector2.zero;
            }
            else if (State == FState.Locked) SetState(Grounded ? FState.Idle : FState.Air);
        }

        /// <summary>Puts the fighter into a domain hand-sign cast (used for clash participants).</summary>
        public void EndCastIfDomain()
        {
            if (Casting is DomainAb) { Casting = null; CastSlot = -1; SetState(Grounded ? FState.Idle : FState.Air); }
        }

        public void SetVictory()
        {
            if (Dead) return;
            CancelCast(); strike = null; CurAttack = null;
            SetState(FState.Victory);
        }

        public void Knockdown(float time, Vector2 vel)
        {
            if (Dead) return;
            CancelCast(); strike = null; CurAttack = null;
            Vel = vel;
            Grounded = vel.y <= 0f && Pos.y <= 0f;
            Hitstun = time;
            SetState(FState.Hitstun);
        }

        /// <summary>Front hand position for visuals (falls back to center).</summary>
        public Vector2 HandPosOrCenter() => Rig != null ? Rig.HandF : Center;

        public float CastProgress => Casting == null ? 0f : Mathf.Clamp01(castClock / Mathf.Max(0.01f, Casting.CastTime));
        public float CastClock => castClock;
        public bool CastFired => castFired;
        public float StrikeTime => strikeTime;
        public FPose StrikePose => strike != null ? strike.Pose : FPose.Idle;
        public bool CounterActive => counter != null;
    }
}
