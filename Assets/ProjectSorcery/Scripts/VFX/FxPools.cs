using UnityEngine;

namespace ProjectSorcery
{
    /// <summary>A pooled line effect: rings, lightning, slash arcs, speed lines.</summary>
    public sealed class LineFx
    {
        public enum Kind { Static, Ring, Lightning, Arc, SpeedLine, Tendril }
        public LineRenderer Lr;
        public Kind K;
        public float Age, Life, Width, R0, R1, Angle, Sweep;
        public Color Color;
        public Vector2 A, B;
        public int Seg;
        public bool Active;
        public bool Unscaled;
        float rejag;

        public void Update(float dt)
        {
            Age += dt;
            if (Age >= Life) { Stop(); return; }
            float k = Age / Life;
            float alpha = 1f - k;
            switch (K)
            {
                case Kind.Ring:
                {
                    float r = Mathf.Lerp(R0, R1, 1f - (1f - k) * (1f - k));
                    int n = Lr.positionCount;
                    for (int i = 0; i < n; i++)
                    {
                        float a = i / (float)(n - 1) * Mathf.PI * 2f;
                        Lr.SetPosition(i, new Vector3(A.x + Mathf.Cos(a) * r, A.y + Mathf.Sin(a) * r, 0f));
                    }
                    Lr.widthMultiplier = Width * (1f - k * 0.7f);
                    break;
                }
                case Kind.Lightning:
                    rejag -= dt;
                    if (rejag <= 0f) { rejag = 0.035f; Jag(); }
                    Lr.widthMultiplier = Width * (alpha * 0.7f + 0.3f);
                    break;
                case Kind.Arc:
                {
                    int n = Lr.positionCount;
                    float r = Mathf.Lerp(R0, R1, k);
                    for (int i = 0; i < n; i++)
                    {
                        float t = i / (float)(n - 1);
                        float a = (Angle + Sweep * (t - 0.5f)) * Mathf.Deg2Rad;
                        Lr.SetPosition(i, new Vector3(A.x + Mathf.Cos(a) * r, A.y + Mathf.Sin(a) * r, 0f));
                    }
                    Lr.widthMultiplier = Width * Mathf.Sin(Mathf.Clamp01(k * 1.6f) * Mathf.PI);
                    break;
                }
                case Kind.SpeedLine:
                    Lr.widthMultiplier = Width * alpha;
                    break;
                case Kind.Tendril:
                {
                    int n = Lr.positionCount;
                    float h = Mathf.Sin(k * Mathf.PI) * R1;
                    for (int i = 0; i < n; i++)
                    {
                        float t = i / (float)(n - 1);
                        float x = A.x + Mathf.Sin(t * 6f + Age * 10f + R0) * 0.25f * t;
                        Lr.SetPosition(i, new Vector3(x, A.y + t * h, 0f));
                    }
                    break;
                }
            }
            var c = Color; c.a *= alpha;
            Lr.startColor = c;
            var e = c; if (K == Kind.SpeedLine) e.a = 0f;
            Lr.endColor = K == Kind.Lightning || K == Kind.Ring || K == Kind.Arc ? c : e;
        }

        public void Jag()
        {
            int n = Lr.positionCount;
            Vector2 d = B - A;
            Vector2 nrm = new Vector2(-d.y, d.x).normalized;
            float len = d.magnitude;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                float off = (i == 0 || i == n - 1) ? 0f : Random.Range(-1f, 1f) * len * 0.12f;
                Vector2 p = A + d * t + nrm * off;
                Lr.SetPosition(i, p);
            }
        }

        public void Stop()
        {
            Active = false;
            Lr.enabled = false;
        }
    }

    /// <summary>A pooled sprite effect: flashes, glows, charge orbs, ink splashes.</summary>
    public sealed class SpriteFx
    {
        public enum Kind { Flash, Orb, Ink, Shard, Static }
        public SpriteRenderer Sr;
        public Kind K;
        public float Age, Life, S0, S1, Spin;
        public Color Color;
        public Vector2 Vel;
        public bool Active;

        public void Update(float dt)
        {
            Age += dt;
            if (Age >= Life) { Active = false; Sr.enabled = false; return; }
            float k = Age / Life;
            float s;
            float a;
            switch (K)
            {
                case Kind.Orb: s = Mathf.Lerp(S0, S1, k * k); a = Mathf.Min(1f, k * 3f); break;
                case Kind.Ink: s = Mathf.Lerp(S0, S1, 1f - (1f - k) * (1f - k)); a = 1f - k * k; break;
                case Kind.Shard: s = S0; a = 1f - k; Sr.transform.position += (Vector3)(Vel * dt); Vel.y -= 14f * dt; break;
                case Kind.Static: s = S0; a = 1f - k; break;
                default: s = Mathf.Lerp(S0, S1, 1f - (1f - k) * (1f - k)); a = 1f - k; break;
            }
            Sr.transform.localScale = new Vector3(s, s, 1f);
            if (Spin != 0f) Sr.transform.Rotate(0f, 0f, Spin * dt);
            var c = Color; c.a *= a;
            Sr.color = c;
        }
    }
}
