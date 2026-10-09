using System.Collections.Generic;
using UnityEngine;

namespace ProjectSorcery
{
    /// <summary>
    /// Anime smear frame: a filled, fading ribbon swept by a limb or blade during the snap of a strike
    /// (the crescent you see behind a katana cut or a whipping kick). Rebuilt each frame from recent samples.
    /// </summary>
    public sealed class Smear
    {
        const int Max = 12;
        const float Life = 0.085f;

        readonly Mesh mesh;
        readonly MeshRenderer mr;
        readonly Vector2[] a = new Vector2[Max], b = new Vector2[Max];
        readonly float[] age = new float[Max];
        int count;
        readonly List<Vector3> verts = new List<Vector3>(Max * 2);
        readonly List<Color> cols = new List<Color>(Max * 2);
        readonly List<Vector2> uvs = new List<Vector2>(Max * 2);
        readonly List<int> tris = new List<int>((Max - 1) * 6);
        public Color Core = Color.white, Edge = Color.white;

        public Smear(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mf = go.AddComponent<MeshFilter>();
            mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Art.EnergySlash;
            mr.sortingOrder = Art.OrderFighterGlow;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mesh = new Mesh { name = name };
            mesh.MarkDynamic();
            mf.sharedMesh = mesh;
        }

        public void Clear() { count = 0; mesh.Clear(); }

        /// <summary>Ages the samples, optionally adds the current swept segment, rebuilds the ribbon.</summary>
        public void Tick(float dt, bool emit, Vector2 segA, Vector2 segB)
        {
            for (int i = 0; i < count; i++) age[i] += dt;
            int k = 0;
            for (int i = 0; i < count; i++) if (age[i] < Life) { a[k] = a[i]; b[k] = b[i]; age[k] = age[i]; k++; }
            count = k;
            if (emit && Settings.VfxQuality > 0)
            {
                if (count == Max) { for (int i = 1; i < Max; i++) { a[i - 1] = a[i]; b[i - 1] = b[i]; age[i - 1] = age[i]; } count--; }
                a[count] = segA; b[count] = segB; age[count] = 0f; count++;
            }
            Build();
        }

        void Build()
        {
            mesh.Clear();
            if (count < 2) return;
            verts.Clear(); cols.Clear(); tris.Clear(); uvs.Clear();
            for (int i = 0; i < count; i++)
            {
                float t = 1f - age[i] / Life;                    // 1 = newest
                float fade = t * t;
                verts.Add(new Vector3(a[i].x, a[i].y, 0f));
                verts.Add(new Vector3(b[i].x, b[i].y, 0f));
                cols.Add(Edge.WithA(0.05f + 0.6f * fade));
                cols.Add(Color.Lerp(Edge, Core, t).WithA(0.25f + 0.75f * fade));
                // u runs back along the swing; v from the faint inner edge (0) to the white-hot blade line (0.5)
                float u = age[i] / Life * 1.5f;
                uvs.Add(new Vector2(u, 0f)); uvs.Add(new Vector2(u, 0.5f));
            }
            for (int i = 0; i < count - 1; i++)
            {
                int v0 = i * 2;
                tris.Add(v0); tris.Add(v0 + 1); tris.Add(v0 + 3);
                tris.Add(v0); tris.Add(v0 + 3); tris.Add(v0 + 2);
            }
            mesh.SetVertices(verts);
            mesh.SetColors(cols);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
        }

        public void Destroy() { if (mr != null) Object.Destroy(mr.gameObject); }
    }
}
