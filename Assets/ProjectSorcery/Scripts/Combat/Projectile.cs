using UnityEngine;

namespace ProjectSorcery
{
    public sealed class Projectile
    {
        public ProjDef D;
        public Fighter Owner;
        public int Team;
        public Vector2 Pos, Vel, PrevPos;
        public float Size, Age, Power;
        public bool Dead;
        public int PierceLeft;
        public Fighter Stuck;
        Vector2 stuckOffset;
        float detonateAt;
        bool returning;

        const int MaxHits = 6;
        readonly Fighter[] hitList = new Fighter[MaxHits];
        readonly float[] hitTimers = new float[MaxHits];
        int hitCount;

        public ProjectileView View;

        public void Init(Match m, Fighter owner, ProjDef d, Vector2 pos, Vector2 dir, float power)
        {
            D = d; Owner = owner; Team = owner != null ? owner.Team : -1;
            Pos = PrevPos = pos;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
            Vel = dir.normalized * d.Speed;
            Size = d.Size * Mathf.Lerp(1f, power, 0.5f);
            Power = power;
            Age = 0f; Dead = false; Stuck = null; returning = false;
            PierceLeft = d.Pierce;
            hitCount = 0;
        }

        public void Tick(Match m, float dt)
        {
            PrevPos = Pos;
            Age += dt;

            if (Stuck != null)
            {
                Pos = Stuck.Pos + stuckOffset;
                if (Stuck.Dead || Age >= detonateAt) Explode(m);
                return;
            }

            // homing
            if (D.Homing > 0f)
            {
                var t = m.NearestEnemyTo(Pos, Team);
                if (t != null)
                {
                    Vector2 want = (t.Center - Pos).normalized * Vel.magnitude;
                    Vel = Vector2.MoveTowards(Vel, want, D.Homing * Vel.magnitude * dt);
                }
            }
            if (D.Returns && !returning && Age > D.Life * 0.45f) returning = true;
            if (returning && Owner != null)
            {
                Vector2 to = Owner.Center - Pos;
                Vel = to.normalized * D.Speed * 1.2f;
                if (to.sqrMagnitude < 0.6f) { Dead = true; return; }
            }
            if (D.Accel != 0f) Vel += Vel.normalized * D.Accel * dt;
            if (D.Drag > 0f) Vel *= Mathf.Max(0f, 1f - D.Drag * dt);
            Vel.y -= D.Gravity * dt;
            if (D.GrowRate > 0f) Size += D.GrowRate * dt;
            Pos += Vel * dt;
            if (D.Ground) Pos.y = Mathf.Max(Size * 0.5f, 0.2f);

            // gravity wells (attract / repel)
            if (D.PullRadius > 0f)
            {
                float r2 = D.PullRadius * D.PullRadius;
                var fs = m.Fighters;
                for (int i = 0; i < fs.Count; i++)
                {
                    var f = fs[i];
                    if (f.Dead || !m.IsEnemyTeam(Team, f.Team) || f.Def.IsBossAnchor()) continue;
                    Vector2 d = Pos - f.Center;
                    if (d.sqrMagnitude > r2) continue;
                    if (f.Invuln > 0f) continue;
                    float k = D.PullForce * (1f - d.magnitude / D.PullRadius);
                    f.Vel = Vector2.MoveTowards(f.Vel, d.normalized * Mathf.Sign(D.PullForce) * 9f, Mathf.Abs(k) * dt * 6f);
                    if (D.PullForce > 0f && f.Grounded && d.y > 0.5f) f.Grounded = false;
                }
            }

            // hit timers for multi-hit
            for (int i = 0; i < hitCount; i++) if (hitTimers[i] > 0f) hitTimers[i] -= dt;

            // fighters
            float rad = Size * 0.5f;
            var list = m.Fighters;
            for (int i = 0; i < list.Count; i++)
            {
                var f = list[i];
                if (f.Dead || f == Owner || !m.IsEnemyTeam(Team, f.Team)) continue;
                if (!f.Overlaps(Pos, rad)) continue;
                int idx = IndexOf(f);
                if (idx >= 0 && (D.TickInterval <= 0f || hitTimers[idx] > 0f)) continue;
                if (idx < 0 && hitCount < MaxHits) { idx = hitCount++; hitList[idx] = f; }
                if (idx >= 0) hitTimers[idx] = D.TickInterval;
                HitFighter(m, f);
                if (Dead || Stuck != null) return;
            }

            // minions
            if (D.HitsMinions)
            {
                var ms = m.Minions;
                for (int i = 0; i < ms.Count; i++)
                {
                    var mn = ms[i];
                    if (mn.Dead || mn.Owner == null || !m.IsEnemyTeam(Team, mn.Owner.Team)) continue;
                    float rr = rad + mn.Radius;
                    if ((mn.Pos - Pos).sqrMagnitude > rr * rr) continue;
                    mn.TakeDamage(D.Damage * Power * (D.TickInterval > 0f ? dt * 4f : 1f), Owner, Vel.normalized * 5f);
                    if (D.TickInterval <= 0f)
                    {
                        if (PierceLeft-- <= 0) { Finish(m); return; }
                    }
                }
            }

            // world bounds
            var A = m.Arena;
            if (!D.Ground && Pos.y < 0.05f && Vel.y < 0f) { Finish(m); return; }
            if (Pos.x < A.Left - 2f || Pos.x > A.Right + 2f)
            {
                if (D.DestroyOnWall || Pos.x < A.Left - 6f || Pos.x > A.Right + 6f) { Finish(m); return; }
            }
            if (Pos.y > A.Ceiling + 8f) { Dead = true; return; }
            if (Age >= D.Life) Finish(m);
        }

        int IndexOf(Fighter f)
        {
            for (int i = 0; i < hitCount; i++) if (hitList[i] == f) return i;
            return -1;
        }

        void HitFighter(Match m, Fighter f)
        {
            float sign = Vel.x != 0f ? Mathf.Sign(Vel.x) : (f.Pos.x >= Pos.x ? 1f : -1f);
            float dmg = D.Damage * Power;
            if (D.TickInterval > 0f) dmg *= 1f; // already balanced as per-tick damage
            var h = HitInfo.Make(Owner, HitSource.Projectile, dmg, new Vector2(D.Knockback.x * sign, D.Knockback.y), D.Hitstun,
                                 D.Flags | HitFlags.Projectile, D.Type, f.Center, D.Color);
            h.Hitstop = D.Hitstop;
            if (D.Status != StatusType.Count) h = h.WithStatus(D.Status, D.StatusTime, D.StatusMag);
            if (D.StickAndDetonate) { h.Damage *= 0.35f; h.Knockback *= 0.2f; h.Hitstun = 0.2f; }
            var res = m.Hit(f, h);
            if (D.PullOnHit && res == HitResult.Hit && Owner != null && !Owner.Dead)
            {
                Vector2 to = Owner.Front(1.0f, 0f) - f.Pos;
                f.Vel = new Vector2(to.x * 3.2f, 5f + Mathf.Max(0f, to.y) * 2f);
                f.Grounded = false;
                f.AddStatus(StatusType.Bind, 0.25f, 0f, Owner);
                Dead = true;
                return;
            }
            if (D.StickAndDetonate && res == HitResult.Hit)
            {
                Stuck = f;
                stuckOffset = Pos - f.Pos;
                detonateAt = Age + D.DetonateDelay;
                Vel = Vector2.zero;
                return;
            }
            if (res == HitResult.Nullified && D.Erase == false && D.TickInterval <= 0f) { Finish(m); return; }
            if (D.TickInterval <= 0f)
            {
                if (PierceLeft-- <= 0) Finish(m);
            }
        }

        public void Finish(Match m)
        {
            if (Dead) return;
            if (D.ExplodeRadius > 0f) { Explode(m); return; }
            Dead = true;
            VFX.ProjectilePop(Pos, D.Color, Size);
        }

        void Explode(Match m)
        {
            if (Dead) return;
            Dead = true;
            float r = D.ExplodeRadius * Mathf.Lerp(1f, Power, 0.5f);
            if (Owner != null)
            {
                var h = HitInfo.Make(Owner, HitSource.Projectile, D.ExplodeDamage * Power, D.ExplodeKb, 0.55f,
                                     (D.Flags | HitFlags.Heavy) & ~HitFlags.Light, D.Type, Pos, D.Color);
                h.Hitstop = 0.09f;
                if (D.Status != StatusType.Count) h = h.WithStatus(D.Status, D.StatusTime, D.StatusMag);
                m.Area(Owner, Pos, r, h, true);
            }
            VFX.Explosion(Pos, r, D.Color, D.Vis);
            Audio.Play(D.Impact == Sfx.None ? Sfx.Explosion : D.Impact, Pos);
            CameraRig.Shake(Mathf.Clamp(r * 0.12f, 0.1f, 0.6f));
        }

        public float Strength => D.Damage * Power * (D.TickInterval > 0f ? 4f : 1f) + D.ExplodeDamage * Power;
    }

    public static class DefExt
    {
        /// <summary>Very large bosses ignore gravity wells.</summary>
        public static bool IsBossAnchor(this CharacterDef d) => d.BossOnly;
    }
}
