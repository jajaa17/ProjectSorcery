using System.Collections.Generic;
using UnityEngine;

namespace ProjectSorcery
{
    public enum Ambient { None, Rain, Embers, Leaves, Dust, Snow, Petals }
    public enum Skyline { City, Mountains, Ruins, Wasteland, Walls, Palace }

    public sealed class ArenaDef
    {
        public string Name;
        public Color SkyTop, SkyBottom, Silhouette, Ground, Line, Accent, Moon;
        public Skyline Skyline;
        public Ambient Ambient;
        public bool RaidOnly;
    }

    public static class Arenas
    {
        public static readonly List<ArenaDef> All = new List<ArenaDef>
        {
            new ArenaDef { Name = "Neon Station", SkyTop = Art.Hex("#0b0820"), SkyBottom = Art.Hex("#3a1650"), Silhouette = Art.Hex("#07060f"), Ground = Art.Hex("#0a0912"), Line = Art.Hex("#ff4fd8"), Accent = Art.Hex("#4ff3ff"), Moon = Art.Hex("#f4e9ff"), Skyline = Skyline.City, Ambient = Ambient.Rain },
            new ArenaDef { Name = "Academy Grounds", SkyTop = Art.Hex("#2a1b3d"), SkyBottom = Art.Hex("#ff8a5c"), Silhouette = Art.Hex("#1a1020"), Ground = Art.Hex("#140c16"), Line = Art.Hex("#ffb27a"), Accent = Art.Hex("#ffd27a"), Moon = Art.Hex("#ffe6b0"), Skyline = Skyline.Mountains, Ambient = Ambient.Leaves },
            new ArenaDef { Name = "Colony Ruins", SkyTop = Art.Hex("#1c2426"), SkyBottom = Art.Hex("#5d6b62"), Silhouette = Art.Hex("#0f1414"), Ground = Art.Hex("#0d1111"), Line = Art.Hex("#9bffcf"), Accent = Art.Hex("#c7d9a0"), Moon = Art.Hex("#e8f0dd"), Skyline = Skyline.Ruins, Ambient = Ambient.Dust },
            new ArenaDef { Name = "Shinjuku Wasteland", SkyTop = Art.Hex("#2b0a0a"), SkyBottom = Art.Hex("#ff6a2a"), Silhouette = Art.Hex("#120505"), Ground = Art.Hex("#130707"), Line = Art.Hex("#ff8a3a"), Accent = Art.Hex("#ffcf6a"), Moon = Art.Hex("#ffd6a0"), Skyline = Skyline.Wasteland, Ambient = Ambient.Embers },
            new ArenaDef { Name = "Detention Center", SkyTop = Art.Hex("#05070c"), SkyBottom = Art.Hex("#1b2633"), Silhouette = Art.Hex("#03050a"), Ground = Art.Hex("#06080d"), Line = Art.Hex("#6fb6ff"), Accent = Art.Hex("#a0c8ff"), Moon = Art.Hex("#cfe3ff"), Skyline = Skyline.Walls, Ambient = Ambient.Rain },
            new ArenaDef { Name = "Snowfield Shrine", SkyTop = Art.Hex("#101a2e"), SkyBottom = Art.Hex("#7d93b8"), Silhouette = Art.Hex("#0a1020"), Ground = Art.Hex("#0d1220"), Line = Art.Hex("#d8ecff"), Accent = Art.Hex("#ffffff"), Moon = Art.Hex("#ffffff"), Skyline = Skyline.Mountains, Ambient = Ambient.Snow },
            new ArenaDef { Name = "Heian Palace", SkyTop = Art.Hex("#120003"), SkyBottom = Art.Hex("#7a0a12"), Silhouette = Art.Hex("#070001"), Ground = Art.Hex("#0d0103"), Line = Art.Hex("#ff2a3a"), Accent = Art.Hex("#ff9a3a"), Moon = Art.Hex("#ff3344"), Skyline = Skyline.Palace, Ambient = Ambient.Embers, RaidOnly = true },
        };

        public const int RaidArena = 6;
        public static ArenaDef Get(int i) => All[Mathf.Clamp(i, 0, All.Count - 1)];
        public static int SelectableCount => All.Count - 1;

        static readonly Dictionary<int, Texture2D> skyCache = new Dictionary<int, Texture2D>();
        static readonly Dictionary<int, Texture2D> lineCache = new Dictionary<int, Texture2D>();

        public static Texture2D Sky(ArenaDef d)
        {
            int k = All.IndexOf(d);
            if (skyCache.TryGetValue(k, out var t)) return t;
            var p = new TexPainter(8, 256, (uint)(k + 3));
            p.VGradient(d.SkyBottom, d.SkyTop, 0.8f);
            t = p.ToTexture("sky_" + k);
            skyCache[k] = t;
            return t;
        }

        /// <summary>Paints a 1024x256 silhouette layer for the arena's skyline.</summary>
        public static Texture2D SkylineTex(ArenaDef d)
        {
            int k = All.IndexOf(d);
            if (lineCache.TryGetValue(k, out var t)) return t;
            var p = new TexPainter(1024, 256, (uint)(k * 31 + 7));
            p.Fill(new Color(0, 0, 0, 0));
            var sil = d.Silhouette;
            // distant moon / sun
            p.Radial(760, 190, 90, d.Moon.WithA(0.45f), d.Moon.WithA(0f), true);
            p.Circle(760, 190, d.Skyline == Skyline.Palace ? 34 : 26, d.Moon.WithA(0.95f));
            p.Stars(d.Skyline == Skyline.Wasteland || d.Skyline == Skyline.Palace ? 40 : 140, Color.white.WithA(0.6f), 0.45f, 1.1f);

            switch (d.Skyline)
            {
                case Skyline.City:
                    for (float x = 0; x < 1024;)
                    {
                        float w = p.Rand(30, 80), h = p.Rand(60, 200);
                        p.Rect(x, 0, x + w, h, sil);
                        for (float wy = 10; wy < h - 10; wy += 9)
                            for (float wx = x + 5; wx < x + w - 5; wx += 8)
                                if (p.Rand() < 0.35f) p.Rect(wx, wy, wx + 3, wy + 4, (p.Rand() < 0.5f ? d.Line : d.Accent).WithA(p.Rand(0.25f, 0.8f)));
                        if (p.Rand() < 0.3f) p.Rect(x + w * 0.4f, h, x + w * 0.45f, h + 20, sil);
                        x += w + p.Rand(0, 8);
                    }
                    break;
                case Skyline.Mountains:
                {
                    var pts = new List<Vector2> { new Vector2(0, 0) };
                    float y = 120;
                    for (int x = 0; x <= 1024; x += 16) { y = Mathf.Clamp(y + p.Rand(-22, 22), 60, 210); pts.Add(new Vector2(x, y)); }
                    pts.Add(new Vector2(1024, 0));
                    p.Poly(pts.ToArray(), Color.Lerp(sil, d.SkyBottom, 0.35f));
                    pts.Clear(); pts.Add(new Vector2(0, 0)); y = 70;
                    for (int x = 0; x <= 1024; x += 12) { y = Mathf.Clamp(y + p.Rand(-12, 12), 30, 120); pts.Add(new Vector2(x, y)); }
                    pts.Add(new Vector2(1024, 0));
                    p.Poly(pts.ToArray(), sil);
                    // temple roofs
                    for (int i = 0; i < 4; i++)
                    {
                        float cx = 120 + i * 260 + p.Rand(-40, 40), by = 70;
                        p.Rect(cx - 40, 0, cx + 40, by + 30, sil);
                        p.Poly(new[] { new Vector2(cx - 70, by + 30), new Vector2(cx + 70, by + 30), new Vector2(cx + 30, by + 58), new Vector2(cx - 30, by + 58) }, sil);
                        p.Poly(new[] { new Vector2(cx - 45, by + 58), new Vector2(cx + 45, by + 58), new Vector2(cx + 15, by + 78), new Vector2(cx - 15, by + 78) }, sil);
                    }
                    break;
                }
                case Skyline.Ruins:
                case Skyline.Wasteland:
                    for (float x = 0; x < 1024;)
                    {
                        float w = p.Rand(40, 110), h = p.Rand(30, d.Skyline == Skyline.Ruins ? 180 : 110);
                        var poly = new[] { new Vector2(x, 0), new Vector2(x + w, 0), new Vector2(x + w, h * p.Rand(0.4f, 1f)), new Vector2(x + w * p.Rand(0.3f, 0.7f), h), new Vector2(x, h * p.Rand(0.5f, 0.9f)) };
                        p.Poly(poly, sil);
                        if (d.Skyline == Skyline.Ruins)
                            for (float wy = 10; wy < h * 0.6f; wy += 14) for (float wx = x + 6; wx < x + w - 8; wx += 12) if (p.Rand() < 0.25f) p.Rect(wx, wy, wx + 5, wy + 6, new Color(0, 0, 0, 0.6f));
                        x += w + p.Rand(10, 40);
                    }
                    if (d.Skyline == Skyline.Wasteland) for (int i = 0; i < 6; i++) p.Radial(p.Rand(0, 1024), p.Rand(10, 60), p.Rand(40, 90), d.Line.WithA(0.35f), d.Line.WithA(0f), true);
                    break;
                case Skyline.Walls:
                    p.Rect(0, 0, 1024, 150, sil);
                    for (int x = 0; x < 1024; x += 64) { p.Rect(x, 150, x + 6, 210, sil); p.Rect(x, 205, x + 64, 210, sil); }
                    for (int x = 20; x < 1024; x += 128) p.Rect(x, 60, x + 40, 100, d.Accent.WithA(0.08f));
                    break;
                case Skyline.Palace:
                    p.Rect(0, 0, 1024, 50, sil);
                    for (int i = 0; i < 3; i++)
                    {
                        float cx = 180 + i * 330, by = 50, s = i == 1 ? 1.4f : 1f;
                        p.Rect(cx - 90 * s, by, cx + 90 * s, by + 50 * s, sil);
                        for (int tier = 0; tier < 3; tier++)
                        {
                            float ty = by + 50 * s + tier * 36 * s, hw = (130 - tier * 30) * s;
                            p.Poly(new[] { new Vector2(cx - hw, ty), new Vector2(cx + hw, ty), new Vector2(cx + hw * 0.55f, ty + 26 * s), new Vector2(cx - hw * 0.55f, ty + 26 * s) }, sil);
                            p.Rect(cx - hw * 0.5f, ty + 22 * s, cx + hw * 0.5f, ty + 36 * s, sil);
                        }
                    }
                    for (int i = 0; i < 10; i++) p.Radial(p.Rand(0, 1024), p.Rand(0, 40), p.Rand(40, 120), d.Accent.WithA(0.4f), d.Accent.WithA(0f), true);
                    break;
            }
            t = p.ToTexture("skyline_" + k, false);
            t.wrapMode = TextureWrapMode.Repeat;
            lineCache[k] = t;
            return t;
        }
    }

    /// <summary>Renders an arena: gradient sky, parallax skyline, glowing floor line and ambient weather.</summary>
    public sealed class ArenaView
    {
        readonly GameObject root;
        readonly SpriteRenderer sky, skyline, skyline2, ground;
        readonly LineRenderer floorLine, floorGlow;
        readonly ArenaDef def;
        readonly Arena arena;
        readonly ParticleSystem weather;

        public ArenaView(Transform parent, ArenaDef d, Arena a)
        {
            def = d; arena = a;
            root = new GameObject("Arena_" + d.Name);
            root.transform.SetParent(parent, false);
            var skyTex = Arenas.Sky(d);
            sky = Art.NewSprite(root.transform, "Sky", Sprite.Create(skyTex, new Rect(0, 0, skyTex.width, skyTex.height), new Vector2(0.5f, 0.5f), 1f), Art.Line, Art.OrderSky);
            var lt = Arenas.SkylineTex(d);
            var lineSprite = Sprite.Create(lt, new Rect(0, 0, lt.width, lt.height), new Vector2(0.5f, 0f), 32f);
            skyline = Art.NewSprite(root.transform, "Skyline", lineSprite, Art.Line, Art.OrderSkyline);
            skyline2 = Art.NewSprite(root.transform, "SkylineNear", lineSprite, Art.Line, Art.OrderSkyline + 1);
            skyline2.color = new Color(0.55f, 0.55f, 0.6f, 1f);
            skyline2.flipX = true;
            ground = Art.NewSprite(root.transform, "Ground", Art.White, Art.Line, Art.OrderGround);
            ground.color = d.Ground;
            ground.transform.position = new Vector3(0f, -10f, 0f);
            ground.transform.localScale = new Vector3(80f, 20f, 1f);
            floorGlow = Art.NewLine(root.transform, "FloorGlow", Art.AddLine, Art.OrderGround + 1, 0.35f, 2);
            floorLine = Art.NewLine(root.transform, "FloorLine", Art.Line, Art.OrderGround + 2, 0.06f, 2);
            float L = a.Left - 12f, R = a.Right + 12f;
            floorGlow.SetPosition(0, new Vector3(L, 0f)); floorGlow.SetPosition(1, new Vector3(R, 0f));
            floorLine.SetPosition(0, new Vector3(L, 0f)); floorLine.SetPosition(1, new Vector3(R, 0f));
            floorGlow.startColor = floorGlow.endColor = d.Line.WithA(0.35f);
            floorLine.startColor = floorLine.endColor = d.Line;
            // arena edge posts
            foreach (float ex in new[] { a.Left - 0.4f, a.Right + 0.4f })
            {
                var post = Art.NewLine(root.transform, "Edge", Art.AddLine, Art.OrderGround + 1, 0.08f, 2);
                post.SetPosition(0, new Vector3(ex, 0f)); post.SetPosition(1, new Vector3(ex, 2.5f));
                post.startColor = d.Line.WithA(0.5f); post.endColor = d.Line.WithA(0f);
            }
            weather = MakeWeather(d);
        }

        ParticleSystem MakeWeather(ArenaDef d)
        {
            if (d.Ambient == Ambient.None) return null;
            var go = new GameObject("Weather");
            go.transform.SetParent(root.transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = Mathf.RoundToInt(500 * Settings.VfxMul);
            var em = ps.emission;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(60f, 1f, 1f);
            go.transform.position = new Vector3(0f, 16f, 0f);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sortingOrder = Art.OrderSkyline + 2;
            switch (d.Ambient)
            {
                case Ambient.Rain:
                    main.startLifetime = 1.4f; main.startSpeed = 0f; main.startSize = 0.06f;
                    main.startColor = d.Accent.WithA(0.35f);
                    main.gravityModifier = 0f;
                    em.rateOverTime = 160f * Settings.VfxMul;
                    var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
                    vel.x = new ParticleSystem.MinMaxCurve(-3f); vel.y = new ParticleSystem.MinMaxCurve(-24f); vel.z = new ParticleSystem.MinMaxCurve(0f);
                    r.sharedMaterial = Art.AddLine; r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.04f; r.lengthScale = 1f;
                    break;
                case Ambient.Snow:
                case Ambient.Dust:
                case Ambient.Petals:
                case Ambient.Leaves:
                {
                    bool leaves = d.Ambient == Ambient.Leaves || d.Ambient == Ambient.Petals;
                    main.startLifetime = 9f; main.startSpeed = 0f; main.startSize = new ParticleSystem.MinMaxCurve(0.05f, leaves ? 0.18f : 0.12f);
                    main.startColor = d.Ambient == Ambient.Snow ? Color.white.WithA(0.7f) : d.Ambient == Ambient.Dust ? d.Accent.WithA(0.3f) : d.Accent.WithA(0.75f);
                    em.rateOverTime = (d.Ambient == Ambient.Snow ? 45f : 18f) * Settings.VfxMul;
                    var v2 = ps.velocityOverLifetime; v2.enabled = true; v2.space = ParticleSystemSimulationSpace.World;
                    v2.x = new ParticleSystem.MinMaxCurve(-1.2f, 0.6f); v2.y = new ParticleSystem.MinMaxCurve(-2.2f, -0.8f); v2.z = new ParticleSystem.MinMaxCurve(0f, 0f);
                    var noise = ps.noise; noise.enabled = true; noise.strength = 0.6f; noise.frequency = 0.3f;
                    r.sharedMaterial = d.Ambient == Ambient.Dust ? Art.AddSoft : Art.AlphaSoft; r.renderMode = ParticleSystemRenderMode.Billboard;
                    break;
                }
                case Ambient.Embers:
                {
                    go.transform.position = new Vector3(0f, -1f, 0f);
                    main.startLifetime = 6f; main.startSpeed = 0f; main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
                    main.startColor = d.Accent.WithA(0.85f);
                    em.rateOverTime = 26f * Settings.VfxMul;
                    var v3 = ps.velocityOverLifetime; v3.enabled = true; v3.space = ParticleSystemSimulationSpace.World;
                    v3.x = new ParticleSystem.MinMaxCurve(-0.6f, 0.9f); v3.y = new ParticleSystem.MinMaxCurve(1f, 2.6f); v3.z = new ParticleSystem.MinMaxCurve(0f, 0f);
                    var noise = ps.noise; noise.enabled = true; noise.strength = 0.8f; noise.frequency = 0.4f;
                    r.sharedMaterial = Art.AddSoft; r.renderMode = ParticleSystemRenderMode.Billboard;
                    break;
                }
            }
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            ps.Play();
            return ps;
        }

        public void Update(Match m)
        {
            var cam = CameraRig.I != null ? CameraRig.I.Cam : null;
            if (cam == null) return;
            Vector3 cp = cam.transform.position;
            float h = cam.orthographicSize * 2f, w = h * cam.aspect;
            sky.transform.position = new Vector3(cp.x, cp.y, 0f);
            sky.transform.localScale = new Vector3(w * 1.3f / 8f, h * 1.3f / 256f, 1f);
            sky.transform.rotation = Quaternion.identity;
            // parallax skyline (moves at a fraction of the camera)
            skyline.transform.position = new Vector3(cp.x * 0.75f, -0.5f + (cp.y - 3.5f) * 0.6f, 0f);
            skyline.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
            skyline2.transform.position = new Vector3(cp.x * 0.45f + 6f, -0.8f + (cp.y - 3.5f) * 0.35f, 0f);
            skyline2.transform.localScale = new Vector3(1.3f, 1.05f, 1f);
            bool domain = m != null && m.Domains != null && (m.Domains.AnyActive || m.Domains.Clashing);
            float target = domain ? 0f : 1f;
            var c = skyline.color; c.a = Mathf.MoveTowards(c.a, target, Time.unscaledDeltaTime * 3f); skyline.color = c;
            var c2 = skyline2.color; c2.a = c.a; skyline2.color = c2;
            var sc = sky.color; sc.a = c.a; sky.color = sc;
            if (weather != null)
            {
                var main = weather.main;
                main.simulationSpeed = Mathf.Max(0.001f, VFX.SimSpeed);
                weather.transform.position = new Vector3(cp.x, weather.transform.position.y, 0f);
            }
        }

        public void Destroy() { if (root != null) Object.Destroy(root); }
    }
}
