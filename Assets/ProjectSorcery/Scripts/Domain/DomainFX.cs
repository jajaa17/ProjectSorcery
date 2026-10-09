using System.Collections.Generic;
using UnityEngine;

namespace ProjectSorcery
{
    /// <summary>A screen-filling textured quad with adjustable rect and UVs (domain backgrounds, clash strips).</summary>
    sealed class QuadLayer
    {
        public readonly GameObject Go;
        readonly Mesh mesh;
        readonly MeshRenderer mr;
        readonly Vector3[] verts = new Vector3[4];
        readonly Vector2[] uvs = new Vector2[4];
        readonly Color[] cols = new Color[4];

        public QuadLayer(Transform parent, Texture2D tex, int order)
        {
            Go = new GameObject("DomainLayer");
            Go.transform.SetParent(parent, false);
            var mf = Go.AddComponent<MeshFilter>();
            mr = Go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = new Material(Art.Line) { mainTexture = tex };
            mr.sortingOrder = order;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mesh = new Mesh { name = "DomainQuad" };
            mesh.MarkDynamic();
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.colors = cols;
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mf.sharedMesh = mesh;
        }

        public void Set(Rect world, Rect uv, Color c)
        {
            verts[0] = new Vector3(world.xMin, world.yMin); verts[1] = new Vector3(world.xMin, world.yMax);
            verts[2] = new Vector3(world.xMax, world.yMax); verts[3] = new Vector3(world.xMax, world.yMin);
            uvs[0] = new Vector2(uv.xMin, uv.yMin); uvs[1] = new Vector2(uv.xMin, uv.yMax);
            uvs[2] = new Vector2(uv.xMax, uv.yMax); uvs[3] = new Vector2(uv.xMax, uv.yMin);
            for (int i = 0; i < 4; i++) cols[i] = c;
            mesh.vertices = verts; mesh.uv = uvs; mesh.colors = cols;
            mesh.RecalculateBounds();
        }

        public void SetOrder(int o) => mr.sortingOrder = o;
        public void Destroy() { Object.Destroy(Go); }
    }

    /// <summary>Domain expansion visuals: ink burst, interior backdrop, ambient motion, clash split-screen.</summary>
    public static class DomainFX
    {
        sealed class Layer
        {
            public DomainDef Def;
            public QuadLayer Quad;
            public float Alpha, Target;
            public Vector2 Origin;
            public float Age;
            public SpriteRenderer Ink;
        }

        static Transform root;
        static readonly List<Layer> layers = new List<Layer>();
        static readonly Dictionary<DomainDef, QuadLayer> quads = new Dictionary<DomainDef, QuadLayer>();
        static bool clash;
        static readonly List<DomainDef> clashDefs = new List<DomainDef>();
        static readonly List<QuadLayer> clashQuads = new List<QuadLayer>();
        static readonly List<float> clashShares = new List<float>();
        static readonly List<LineRenderer> clashEdges = new List<LineRenderer>();
        static float clashAge;

        public static void Init(Transform r)
        {
            var go = new GameObject("DomainFX");
            go.transform.SetParent(r, false);
            root = go.transform;
        }

        static QuadLayer QuadFor(DomainDef d)
        {
            if (quads.TryGetValue(d, out var q)) { q.Go.SetActive(true); return q; }
            q = new QuadLayer(root, DomainTex.Get(d), Art.OrderDomain);
            quads[d] = q;
            return q;
        }

        public static void Show(DomainDef d, Fighter owner)
        {
            if (root == null) return;
            foreach (var l in layers) if (l.Def == d) { l.Target = 1f; return; }
            var layer = new Layer { Def = d, Quad = QuadFor(d), Alpha = 0f, Target = 1f, Origin = owner.Center, Age = 0f };
            layer.Ink = Art.NewSprite(root, "Ink", Art.Soft, Art.AlphaSoft, Art.OrderDomain + 1);
            layer.Ink.color = new Color(0f, 0f, 0f, 0.95f);
            layer.Ink.transform.position = layer.Origin;
            layers.Add(layer);
            VFX.Ring(owner.Center, d.Primary, 14f, 0.7f, 0.25f);
            VFX.Ring(owner.Center, Color.white, 10f, 0.5f, 0.12f);
            VFX.ImpactFrame(ImpactKind.Domain, owner.Center);
            Audio.PlayMusic(Track.Domain);
        }

        public static void Hide(DomainDef d)
        {
            foreach (var l in layers) if (l.Def == d) l.Target = 0f;
        }

        public static void HideAll()
        {
            foreach (var l in layers) { if (l.Ink != null) Object.Destroy(l.Ink.gameObject); l.Quad.Go.SetActive(false); }
            layers.Clear();
            EndClash();
        }

        public static void ShowClash(List<DomainDef> defs, List<Fighter> fighters)
        {
            if (root == null) return;
            EndClash();
            clash = true; clashAge = 0f;
            // order strips left-to-right by fighter position
            var idx = new List<int>();
            for (int i = 0; i < defs.Count; i++) idx.Add(i);
            idx.Sort((a, b) => fighters[a].Pos.x.CompareTo(fighters[b].Pos.x));
            order.Clear(); order.AddRange(idx);
            for (int k = 0; k < idx.Count; k++)
            {
                var d = defs[idx[k]];
                clashDefs.Add(d);
                var q = new QuadLayer(root, DomainTex.Get(d), Art.OrderDomain + 2);
                clashQuads.Add(q);
                clashShares.Add(1f / defs.Count);
            }
            for (int k = 0; k < defs.Count - 1; k++)
            {
                var e = Art.NewLine(root, "ClashEdge", Art.AddLine, Art.OrderDomain + 4, 0.25f, 14);
                clashEdges.Add(e);
            }
            Audio.PlayMusic(Track.Domain);
        }

        static readonly List<int> order = new List<int>();

        public static void UpdateClash(List<float> shares)
        {
            if (!clash) return;
            for (int k = 0; k < order.Count && k < clashShares.Count; k++)
            {
                int src = order[k];
                if (src < shares.Count) clashShares[k] = Mathf.Lerp(clashShares[k], shares[src], 0.25f);
            }
        }

        public static void EndClash()
        {
            clash = false;
            foreach (var q in clashQuads) q.Destroy();
            foreach (var e in clashEdges) Object.Destroy(e.gameObject);
            clashQuads.Clear(); clashEdges.Clear(); clashDefs.Clear(); clashShares.Clear(); order.Clear();
        }

        public static void JackpotSpin() { if (root != null) CutIns.JackpotSpin(); }
        public static void JackpotResult(bool hit) { if (root != null) CutIns.JackpotResult(hit); }

        /// <summary>Per-frame update; called after the camera has moved.</summary>
        public static void Update(Camera cam)
        {
            if (root == null || cam == null) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float h = cam.orthographicSize * 2.3f, w = h * cam.aspect * 1.08f;
            Vector3 cp = cam.transform.position;
            Rect view = new Rect(cp.x - w * 0.5f, cp.y - h * 0.5f, w, h);
            bool anyVisible = false;

            for (int i = layers.Count - 1; i >= 0; i--)
            {
                var l = layers[i];
                l.Age += dt;
                // ink burst grows from the caster, then the interior bleeds in behind it
                float inkK = Mathf.Clamp01(l.Age / 0.55f);
                if (l.Ink != null)
                {
                    float s = Mathf.Lerp(0.5f, 70f, 1f - (1f - inkK) * (1f - inkK) * (1f - inkK));
                    l.Ink.transform.localScale = new Vector3(s, s, 1f);
                    l.Ink.color = new Color(0f, 0f, 0f, Mathf.Clamp01(1.4f - l.Age * 1.5f) * 0.95f);
                    if (l.Age > 1.2f) { Object.Destroy(l.Ink.gameObject); l.Ink = null; }
                }
                float targetA = l.Age < 0.35f ? 0f : l.Target;
                l.Alpha = Mathf.MoveTowards(l.Alpha, targetA, dt * (l.Target > 0f ? 2.2f : 2.8f));
                // subtle breathing parallax on the interior
                float drift = Mathf.Sin(Time.unscaledTime * 0.3f) * 0.02f;
                l.Quad.Set(view, new Rect(0.0f + drift, 0f, 1f, 1f), new Color(1f, 1f, 1f, l.Alpha));
                if (l.Alpha > 0.05f) { anyVisible = true; Ambient(l.Def, view, l.Alpha); }
                if (l.Target <= 0f && l.Alpha <= 0f)
                {
                    l.Quad.Go.SetActive(false);
                    if (l.Ink != null) Object.Destroy(l.Ink.gameObject);
                    layers.RemoveAt(i);
                }
            }

            if (clash)
            {
                clashAge += dt;
                float x = view.xMin;
                float t = Time.unscaledTime;
                for (int k = 0; k < clashQuads.Count; k++)
                {
                    float sw = view.width * clashShares[k];
                    var r = new Rect(x, view.yMin, sw, view.height);
                    float u0 = (x - view.xMin) / view.width;
                    clashQuads[k].Set(r, new Rect(u0, 0f, clashShares[k], 1f), new Color(1f, 1f, 1f, Mathf.Clamp01(clashAge * 3f)));
                    x += sw;
                    if (k < clashEdges.Count)
                    {
                        var e = clashEdges[k];
                        int n = e.positionCount;
                        for (int i = 0; i < n; i++)
                        {
                            float yy = view.yMin + view.height * i / (n - 1);
                            e.SetPosition(i, new Vector3(x + Random.Range(-0.35f, 0.35f), yy, 0f));
                        }
                        var c = Color.Lerp(clashDefs[k].Primary, clashDefs[k + 1].Primary, 0.5f + 0.5f * Mathf.Sin(t * 20f));
                        e.startColor = e.endColor = Color.Lerp(c, Color.white, 0.5f);
                        e.widthMultiplier = 0.2f + Random.Range(0f, 0.25f);
                        if (Random.value < 0.4f) VFX.Sparks(new Vector2(x, view.yMin + Random.Range(0f, view.height)), Color.white, 2, 10f);
                    }
                }
                anyVisible = true;
            }

            if (!anyVisible && !clash && layers.Count == 0 && Match.I != null && !Match.I.Domains.AnyActive)
            {
                var mm = Match.I;
                Audio.PlayMusic(mm.Config.Demo ? Track.Menu : mm.Config.Mode == GameMode.Raid ? Track.Raid : Track.Battle);
            }
        }

        /// <summary>Theme-specific motion layered over the interior.</summary>
        static void Ambient(DomainDef d, Rect v, float a)
        {
            if (Random.value > a * Settings.VfxMul) return;
            Vector2 rp = new Vector2(Random.Range(v.xMin, v.xMax), Random.Range(v.yMin, v.yMax));
            switch (d.Theme)
            {
                case DomainTheme.Void:
                    if (Random.value < 0.5f) VFX.Lightning(rp, rp + new Vector2(Random.Range(2f, 6f), Random.Range(-0.3f, 0.3f)), new Color(0.7f, 0.85f, 1f, 0.6f), 0.03f, 0.25f, 3);
                    break;
                case DomainTheme.Shrine:
                    if (Random.value < 0.35f) VFX.SlashMark(rp, d.Primary);
                    break;
                case DomainTheme.Caldera:
                    VFX.AuraTick(new Vector2(rp.x, v.yMin + 0.5f), 1f, new Color(1f, 0.5f, 0.1f), 1f);
                    break;
                case DomainTheme.Shore:
                case DomainTheme.Garden:
                    VFX.Burst(BurstVis.Stars, rp, 0.3f, d.Theme == DomainTheme.Shore ? new Color(0.8f, 0.95f, 1f) : new Color(1f, 0.8f, 0.9f));
                    break;
                case DomainTheme.Shadow:
                case DomainTheme.Womb:
                    if (Random.value < 0.2f) VFX.Tendrils(new Vector2(rp.x, 0f), d.Primary);
                    break;
                case DomainTheme.Jackpot:
                    VFX.Sparks(rp, Color.HSVToRGB(Random.value, 0.7f, 1f), 2, 3f, 0.14f);
                    break;
                case DomainTheme.Moon:
                    if (Random.value < 0.3f) VFX.Ring(rp, new Color(0.85f, 0.9f, 1f, 0.5f), 0.6f, 0.4f, 0.03f);
                    break;
                default:
                    VFX.Sparks(rp, d.Secondary, 1, 1.5f, 0.1f);
                    break;
            }
        }
    }
}
