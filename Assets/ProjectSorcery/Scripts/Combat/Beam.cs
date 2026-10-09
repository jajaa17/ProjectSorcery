using UnityEngine;

namespace ProjectSorcery
{
    public sealed class Beam
    {
        public BeamDef D;
        public Fighter Owner;
        public Vector2 Origin, Dir;
        public float Age, Power, Length;
        public bool Dead;
        float tick;
        float localAngle;   // angle relative to facing right
        int face;
        public BeamView View;

        public bool Firing => Age >= D.Charge;
        public Vector2 End => Origin + Dir * Length;
        public float Width => D.Width * Mathf.Lerp(1f, Power, 0.5f);

        public void Init(Fighter owner, BeamDef d, Vector2 origin, Vector2 dir, float power)
        {
            Owner = owner; D = d; Origin = origin; Dir = dir.normalized; Power = power;
            Age = 0f; Dead = false; tick = 0f;
            Length = d.Length;
            face = Dir.x < -0.001f ? -1 : 1;
            localAngle = Mathf.Atan2(Dir.y, Mathf.Abs(Dir.x)) * Mathf.Rad2Deg;
        }

        public void Tick(Match m, float dt)
        {
            Age += dt;
            if (Owner == null || Owner.Dead) { if (Age < D.Charge) { Dead = true; return; } }
            if (D.Track && Owner != null && !Owner.Dead)
            {
                Origin = Owner.Front(0.85f, 1.35f);
                face = Owner.Facing;
            }
            float sweep = 0f;
            if (D.Sweep != 0f && Firing) sweep = D.Sweep * (Mathf.Clamp01((Age - D.Charge) / D.Duration) - 0.5f);
            float ang = (localAngle + sweep) * Mathf.Deg2Rad;
            Dir = new Vector2(face * Mathf.Cos(ang), Mathf.Sin(ang));
            // clip at the floor
            Length = D.Length;
            if (Dir.y < -0.01f)
            {
                float toFloor = -Origin.y / Dir.y;
                if (toFloor > 0f && toFloor < Length) Length = toFloor;
            }
            if (!Firing) return;

            if (Age - dt < D.Charge)
            {
                Audio.Play(D.Sound, Origin, 1f);
                CameraRig.Shake(0.3f);
                VFX.Burst(BurstVis.Light, Origin, 1.2f, D.Color);
            }

            tick -= dt;
            if (tick <= 0f)
            {
                tick = D.TickRate;
                Vector2 a = Origin, b = End;
                float w = Width * 0.5f;
                var fs = m.Fighters;
                for (int i = 0; i < fs.Count; i++)
                {
                    var f = fs[i];
                    if (f.Dead || f == Owner || Owner == null || !m.IsEnemy(Owner, f)) continue;
                    if (!f.OverlapsSegment(a, b, w)) continue;
                    var h = HitInfo.Make(Owner, HitSource.Beam, D.TickDamage * Power, new Vector2(D.Knockback.x * Mathf.Sign(Dir.x == 0 ? 1 : Dir.x), D.Knockback.y),
                                         D.Hitstun, D.Flags | HitFlags.NoCombo, D.Type, f.Center, D.Color);
                    h.Hitstop = 0.02f;
                    h.CeGain = 1f;
                    if (D.Status != StatusType.Count) h = h.WithStatus(D.Status, D.StatusTime, D.StatusMag);
                    m.Hit(f, h);
                }
                var ms = m.Minions;
                for (int i = 0; i < ms.Count; i++)
                {
                    var mn = ms[i];
                    if (mn.Dead || mn.Owner == null || Owner == null || !m.IsEnemyTeam(Owner.Team, mn.Owner.Team)) continue;
                    if (SegDist(mn.Pos, a, b) < w + mn.Radius) mn.TakeDamage(D.TickDamage * Power * 1.5f, Owner, Dir * 4f);
                }
                // beams burn through weaker projectiles
                var ps = m.Projectiles;
                for (int i = 0; i < ps.Count; i++)
                {
                    var p = ps[i];
                    if (p.Dead || p.D.Unerasable || Owner == null || !m.IsEnemyTeam(Owner.Team, p.Team)) continue;
                    if (SegDist(p.Pos, a, b) < w + p.Size * 0.5f) p.Finish(m);
                }
            }
            if (Age >= D.Charge + D.Duration) Dead = true;
        }

        static float SegDist(Vector2 c, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(c - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
            return (c - (a + ab * t)).magnitude;
        }
    }
}
