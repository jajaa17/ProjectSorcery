using UnityEngine;

namespace ProjectSorcery
{
    /// <summary>Shikigami, summoned curses, puppets and other autonomous helpers.</summary>
    public sealed class Minion
    {
        public MinionDef D;
        public Fighter Owner;
        public Vector2 Pos, Vel, PrevPos;
        public float Hp, MaxHp, Age, Power, AttackCd, AnimTime, HurtFlash;
        public int Facing = 1;
        public bool Dead;
        public bool Attacking;
        public float[] Adapt = new float[(int)DamageType.Count];
        public MinionView View;
        bool exploded;

        public float Radius => 0.5f * D.Size;

        public void Init(Fighter owner, MinionDef d, Vector2 pos, float power)
        {
            Owner = owner; D = d; Pos = PrevPos = pos; Vel = Vector2.zero;
            Power = power; MaxHp = Hp = d.Hp * Mathf.Lerp(1f, power, 0.5f);
            Age = 0f; AttackCd = 0.3f; Dead = false; exploded = false;
            Facing = owner != null ? owner.Facing : 1;
            for (int i = 0; i < Adapt.Length; i++) Adapt[i] = 0f;
            if (d.Brain == MinionBrain.Bomber) { Pos.y = 12f; Vel = new Vector2(0f, -4f); }
        }

        public void Tick(Match m, float dt)
        {
            PrevPos = Pos;
            Age += dt; AnimTime += dt;
            if (HurtFlash > 0f) HurtFlash -= dt;
            if (AttackCd > 0f) AttackCd -= dt;
            Attacking = false;
            if (Owner == null || Owner.Dead || Age >= D.Life) { Kill(m, false); return; }
            if (MaxHp > 0f && !D.Invulnerable && Hp > MaxHp * Balance.MinionHp) Hp = MaxHp * Balance.MinionHp;

            var target = m.NearestEnemyTo(Pos, Owner.Team);
            float tx = target != null ? target.Pos.x : Owner.Pos.x + Owner.Facing * 3f;
            float ty = target != null ? target.Center.y : 1f;

            switch (D.Brain)
            {
                case MinionBrain.Chase:
                case MinionBrain.Swarm:
                case MinionBrain.Grabber:
                    MoveToward(tx, ty, dt, D.Brain == MinionBrain.Grabber ? D.Range * 0.8f : D.Range * 0.7f);
                    TryAttack(m, target);
                    break;
                case MinionBrain.Guard:
                {
                    float gx = Owner.Pos.x - Owner.Facing * 1.4f;
                    bool engage = target != null && Mathf.Abs(target.Pos.x - Owner.Pos.x) < 6f;
                    if (engage) MoveToward(tx, ty, dt, D.Range * 0.7f); else MoveToward(gx, Owner.Center.y + (D.Fly ? 1.5f : 0f), dt, 0.3f);
                    if (engage) TryAttack(m, target);
                    break;
                }
                case MinionBrain.Kamikaze:
                    MoveToward(tx, ty, dt, 0f);
                    if (target != null && (target.Center - Pos).sqrMagnitude < (D.Range + Radius) * (D.Range + Radius)) Explode(m);
                    break;
                case MinionBrain.Static:
                    Vel = Vector2.zero;
                    TryAttack(m, target);
                    break;
                case MinionBrain.Shooter:
                {
                    float keep = 5f;
                    float dx = tx - Pos.x;
                    float want = Mathf.Abs(dx) < keep ? Pos.x - Mathf.Sign(dx) * 2f : Pos.x;
                    MoveToward(want, D.Fly ? 3.5f : 0f, dt, 0.3f);
                    if (dx != 0) Facing = dx > 0 ? 1 : -1;
                    if (AttackCd <= 0f && target != null && D.Shot != null)
                    {
                        AttackCd = D.AttackRate;
                        m.Shoot(Owner, D.Shot, Pos + new Vector2(Facing * Radius, 0.2f), (target.Center - Pos).normalized, Power);
                        Attacking = true;
                    }
                    break;
                }
                case MinionBrain.Healer:
                    MoveToward(Owner.Pos.x - Owner.Facing * 1.2f, D.Fly ? Owner.Center.y + 1f : 0f, dt, 0.4f);
                    Owner.Heal(D.HealRate * dt, false);
                    if (Age % 0.4f < dt) VFX.Heal(Owner.Center, D.Color);
                    break;
                case MinionBrain.Bomber:
                    Vel.y -= 30f * dt;
                    Pos += Vel * dt;
                    if (target != null) Pos.x = Mathf.MoveTowards(Pos.x, tx, 6f * dt);
                    if (Pos.y <= Radius * 0.6f) Explode(m);
                    return;
                case MinionBrain.Flood:
                    if (!exploded)
                    {
                        exploded = true;
                        m.SpawnZone(Owner, new ZoneDef { Width = 9f, Height = 2.2f, Life = 2.5f, TickDamage = 10f * Power, TickRate = 0.25f, Vis = ZoneVis.Water, Color = D.Color, Status = StatusType.Slow, StatusMag = 0.4f, StatusTime = 0.6f, Type = DamageType.Water },
                                    new Vector2(Pos.x, 0f), Power);
                        var h = HitInfo.Make(Owner, HitSource.Minion, D.Damage * Power, new Vector2(8f * Facing, 3f), 0.6f, D.Flags | HitFlags.Heavy, DamageType.Water, Pos, D.Color);
                        m.Area(Owner, Pos, 4.5f, h, false);
                        VFX.Burst(BurstVis.Water, Pos, 4f, D.Color);
                        Audio.Play(Sfx.Water, Pos, 1f, 0.7f);
                        CameraRig.Shake(0.45f);
                    }
                    if (Age > 1.2f) Kill(m, false);
                    break;
            }

            if (!D.Fly && D.Brain != MinionBrain.Bomber)
            {
                Vel.y -= 40f * dt;
                Pos += Vel * dt;
                if (Pos.y < 0f) { Pos.y = 0f; Vel.y = 0f; }
            }
            else if (D.Brain != MinionBrain.Bomber) Pos += Vel * dt;
            Pos.x = Mathf.Clamp(Pos.x, m.Arena.Left, m.Arena.Right);
            if (Pos.y > m.Arena.Ceiling) Pos.y = m.Arena.Ceiling;
        }

        void MoveToward(float x, float y, float dt, float stopDist)
        {
            float dx = x - Pos.x;
            if (Mathf.Abs(dx) > 0.05f) Facing = dx > 0 ? 1 : -1;
            float sp = D.Speed;
            if (Mathf.Abs(dx) <= stopDist) Vel.x = Mathf.MoveTowards(Vel.x, 0f, 30f * dt);
            else Vel.x = Mathf.MoveTowards(Vel.x, Mathf.Sign(dx) * sp, 40f * dt);
            if (D.Fly)
            {
                float wobble = Mathf.Sin(AnimTime * 4f + Owner.Slot) * 0.4f;
                float dy = (y + 0.8f + wobble) - Pos.y;
                Vel.y = Mathf.MoveTowards(Vel.y, Mathf.Clamp(dy * 3f, -sp, sp), 30f * dt);
            }
        }

        void TryAttack(Match m, Fighter target)
        {
            if (target == null || AttackCd > 0f) return;
            float r = D.Range + Radius;
            if ((target.Center - Pos).sqrMagnitude > r * r * 1.2f) return;
            AttackCd = D.AttackRate;
            Attacking = true;
            if (D.Brain == MinionBrain.Grabber)
            {
                VFX.Tongue(Pos, target.Center, D.Color);
                var g = HitInfo.Make(Owner, HitSource.Minion, D.Damage * Power, Vector2.zero, 0.4f, D.Flags | HitFlags.NoCombo, D.Type, target.Center, D.Color);
                if (m.Hit(target, g) == HitResult.Hit)
                {
                    Vector2 to = Owner.Front(1f, 0f) - target.Pos;
                    target.Vel = new Vector2(to.x * 3f, 6f);
                    target.Grounded = false;
                    target.AddStatus(StatusType.Bind, 0.35f, 0f, Owner);
                }
                return;
            }
            float sign = target.Pos.x >= Pos.x ? 1f : -1f;
            var h = HitInfo.Make(Owner, HitSource.Minion, D.Damage * Power * Balance.MinionDamage, new Vector2(D.Knockback.x * sign, D.Knockback.y) * 0.7f,
                                 Mathf.Min(D.Hitstun, Balance.MinionMaxHitstun), D.Flags | HitFlags.NoCombo, D.Type, target.Center, D.Color);
            if (D.Status != StatusType.Count) h = h.WithStatus(D.Status, D.StatusTime, D.StatusMag);
            if (D.Adapts && Owner.InfinityHitsTaken >= 4) h.Flags |= HitFlags.PierceInfinity;
            var res = m.Hit(target, h);
            if (D.Adapts && res == HitResult.Nullified) Owner.InfinityHitsTaken++;
            VFX.Slash(Pos + new Vector2(Facing * Radius, 0.2f), Facing, D.Color, D.Size);
            if (D.Brain == MinionBrain.Swarm) Audio.Play(Sfx.Insect, Pos, 0.4f);
            else Audio.Play(D.Type == DamageType.Slash ? Sfx.Slash : Sfx.Punch, Pos, 0.6f);
        }

        void Explode(Match m)
        {
            if (Dead) return;
            float r = Mathf.Max(1.5f, D.Range * 2f);
            var h = HitInfo.Make(Owner, HitSource.Minion, D.Damage * Power, D.Knockback, D.Hitstun, D.Flags | HitFlags.Heavy, D.Type, Pos, D.Color);
            if (D.Status != StatusType.Count) h = h.WithStatus(D.Status, D.StatusTime, D.StatusMag);
            h.Hitstop = 0.1f;
            m.Area(Owner, Pos, r, h, false);
            VFX.Explosion(Pos, r, D.Color, ProjVis.Orb);
            Audio.Play(Sfx.Explosion, Pos, 0.9f);
            CameraRig.Shake(0.35f);
            Kill(m, false);
        }

        public void TakeDamage(float dmg, Fighter src, Vector2 kb)
        {
            if (Dead || D.Invulnerable) return;
            if (D.Adapts)
            {
                // the wheel turns: adapts to repeated punishment
                int t = (int)DamageType.Blunt;
                dmg *= 1f - Adapt[t];
                Adapt[t] = Mathf.Min(0.8f, Adapt[t] + 0.05f);
            }
            Hp -= dmg;
            HurtFlash = 0.08f;
            if (!D.Fly) Vel += kb * 0.3f;
            if (Hp <= 0f) Kill(null, true);
        }

        public void Kill(Match m, bool destroyed)
        {
            if (Dead) return;
            Dead = true;
            VFX.Puff(Pos, D.Color, D.Size);
            if (destroyed) Audio.Play(Sfx.Summon, Pos, 0.4f, 1.4f);
        }
    }

    public sealed class Zone
    {
        public ZoneDef D;
        public Fighter Owner;
        public Vector2 Pos;
        public float Age, Power;
        public bool Dead;
        float tick;
        public ZoneView View;

        public void Init(Fighter owner, ZoneDef d, Vector2 pos, float power)
        {
            Owner = owner; D = d; Pos = pos; Power = power; Age = 0f; tick = 0f; Dead = false;
        }

        public bool Contains(Vector2 p) => Mathf.Abs(p.x - Pos.x) < D.Width * 0.5f && p.y >= Pos.y - 0.3f && p.y <= Pos.y + D.Height;

        public void Tick(Match m, float dt)
        {
            Age += dt;
            if (Owner == null || Age >= D.Life) { Dead = true; return; }
            if (D.FollowOwner && !Owner.Dead) Pos = new Vector2(Owner.Pos.x, Owner.Pos.y);
            tick -= dt;
            if (tick <= 0f)
            {
                tick = D.TickRate;
                var fs = m.Fighters;
                for (int i = 0; i < fs.Count; i++)
                {
                    var f = fs[i];
                    if (f.Dead) continue;
                    if (!Contains(f.Pos + Vector2.up * 0.5f)) continue;
                    if (m.IsEnemy(Owner, f))
                    {
                        if (D.TickDamage > 0f) f.TakeTrueDamage(D.TickDamage * Power, Owner);
                        if (D.Status != StatusType.Count) f.AddStatus(D.Status, D.StatusTime, D.StatusMag, Owner);
                        f.FlashTimer = 0.04f;
                    }
                    else if (D.HealsAllies && (f.Team == Owner.Team)) f.Heal(D.TickDamage * Power * 0.6f, false);
                }
            }
            if (D.BlocksProjectiles)
            {
                var ps = m.Projectiles;
                for (int i = 0; i < ps.Count; i++)
                {
                    var p = ps[i];
                    if (!p.Dead && !p.D.Unerasable && m.IsEnemyTeam(Owner.Team, p.Team) && Contains(p.Pos)) p.Finish(m);
                }
            }
        }
    }
}
