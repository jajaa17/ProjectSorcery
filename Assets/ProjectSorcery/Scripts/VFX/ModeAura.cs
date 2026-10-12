using System.Collections.Generic;
using UnityEngine;

namespace ProjectSorcery
{
    /// <summary>
    /// A DBZ-style power-up aura: a flickering flame envelope wrapped around the whole body, with tongues of energy
    /// licking upward off the top, a crisp bright rim, and a translucent glowing interior. Rebuilt every frame from
    /// the skeleton, so it hugs the fighter through every pose. Purely visual.
    /// </summary>
    public sealed class ModeAura
    {
        const int N = 48;   // points around the envelope

        readonly Mesh mesh;
        readonly MeshRenderer mr;
        readonly LineRenderer rim;
        readonly List<Vector3> verts = new List<Vector3>(N * 2 + 1);
        readonly List<Color> cols = new List<Color>(N * 2 + 1);
        readonly List<Vector2> uvs = new List<Vector2>(N * 2 + 1);
        readonly List<int> tris = new List<int>(N * 9);
        float level, time;

        public ModeAura(Transform parent)
        {
            var go = new GameObject("ModeAura");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh = new Mesh { name = "ModeAura" };
            mesh.MarkDynamic();
            mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Art.EnergySlash;
            mr.sortingOrder = Art.OrderFighterGlow - 1;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            rim = Art.NewLine(parent, "ModeAuraRim", Art.EnergyBeam, Art.OrderFighterGlow - 1, 0.07f, N);
            rim.loop = true;
            rim.enabled = false;
        }

        /// <summary>Current strength (0 = off). Fades in fast on activation and out gently when the mode ends.</summary>
        public float Level => level;

        public void Tick(float dt, RigSolver s, float size, bool on, Color c, float power)
        {
            level = Mathf.MoveTowards(level, on ? power : 0f, dt * (on ? 6f : 1.5f));
            time += dt;
            if (level <= 0.01f || Settings.VfxQuality == 0) { mesh.Clear(); rim.enabled = false; return; }

            // envelope around the skeleton: centred between hips and neck, reaching past the head and the feet
            Vector2 top = new Vector2(s.HeadC.x, Mathf.Max(s.HeadC.y, s.Neck.y) + 0.35f * size);
            float feet = Mathf.Min(s.FootF.y, s.FootB.y) - 0.1f * size;
            Vector2 c0 = new Vector2((s.Hip.x + s.Neck.x) * 0.5f, (top.y + feet) * 0.5f);
            float ry = (top.y - feet) * 0.5f + 0.2f * size;
            float rx = Mathf.Max(0.85f * size, Mathf.Abs(s.HandF.x - s.HandB.x) * 0.5f + 0.3f * size);
            float pulse = 1f + 0.06f * Mathf.Sin(time * 9f) + 0.04f * Mathf.Sin(time * 23f);

            verts.Clear(); cols.Clear(); uvs.Clear(); tris.Clear();
            verts.Add(c0); cols.Add(c.WithA(0.1f * level)); uvs.Add(new Vector2(0f, 0.5f));
            for (int i = 0; i < N; i++)
            {
                float a = i / (float)N * Mathf.PI * 2f;            // 0 = straight up, clockwise
                float up = Mathf.Cos(a);                           // +1 at the top, -1 at the feet
                Vector2 dir = new Vector2(Mathf.Sin(a), up);
                // flame tongues: they travel from the sides up to the crown and flicker as they go
                float side = Mathf.Abs(Mathf.Sin(a));
                float crown = Mathf.Clamp01((up + 0.3f) / 1.3f);
                float phase = (1f - up) * 5f - time * 8f;
                float wave = Mathf.Max(0f, Mathf.Sin(phase + i * 0.6f));
                float tongue = wave * wave * (0.2f + 0.8f * crown);
                float flick = 0.03f * Mathf.Sin(time * 31f + i * 2.3f);
                float r = (1f + tongue * 0.45f + flick) * pulse;
                // teardrop: the crown is drawn up into flame tips, the base hugs the feet
                Vector2 outer = c0 + new Vector2(dir.x * rx * r, dir.y * ry * r + crown * (0.35f + tongue * 0.6f) * size);
                Vector2 inner = c0 + new Vector2(dir.x * rx * 0.45f, dir.y * ry * 0.55f);
                float u = i / (float)N * 6f;
                // inner ring: hot against the body; outer ring: dissolves into the flame tips
                verts.Add(inner); cols.Add(c.WithA(0.55f * level)); uvs.Add(new Vector2(u, 0.5f));
                verts.Add(outer); cols.Add(c.WithA(0.18f * level * (0.4f + side * 0.6f))); uvs.Add(new Vector2(u, 1f));
                rim.SetPosition(i, Vector2.Lerp(inner, outer, 0.82f));
            }
            for (int i = 0; i < N; i++)
            {
                int j = (i + 1) % N;
                int ia = 1 + i * 2, ib = ia + 1, ja = 1 + j * 2, jb = ja + 1;
                tris.Add(0); tris.Add(ia); tris.Add(ja);              // translucent interior glow
                tris.Add(ia); tris.Add(ib); tris.Add(jb);             // flame band
                tris.Add(ia); tris.Add(jb); tris.Add(ja);
            }
            mesh.Clear();
            mesh.SetVertices(verts);
            mesh.SetColors(cols);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            rim.enabled = true;
            var rc = Color.Lerp(c, Color.white, 0.45f).WithA(0.55f * level);
            rim.startColor = rc; rim.endColor = rc;
            rim.widthMultiplier = 0.06f * size * (0.8f + 0.4f * Mathf.Sin(time * 17f));
        }

        public void Destroy()
        {
            if (mr != null) Object.Destroy(mr.gameObject);
            if (rim != null) Object.Destroy(rim.gameObject);
        }
    }
}
