using System.Collections.Generic;
using UnityEngine;

namespace ProjectSorcery
{
    /// <summary>Paints each domain's interior at runtime.</summary>
    public static class DomainTex
    {
        static readonly Dictionary<DomainDef, Texture2D> cache = new Dictionary<DomainDef, Texture2D>();
        const int W = 512, H = 256;

        public static Texture2D Get(DomainDef d)
        {
            if (cache.TryGetValue(d, out var t)) return t;
            var p = new TexPainter(W, H, StableHash(d.Name));
            Paint(p, d);
            p.Vignette(0.55f);
            p.Grain(0.03f);
            t = p.ToTexture("domain_" + d.Theme);
            cache[d] = t;
            return t;
        }

        // string.GetHashCode differs between runtimes; FNV-1a keeps every domain looking the same everywhere
        static uint StableHash(string s)
        {
            uint h = 2166136261u;
            foreach (char c in s) { h ^= c; h *= 16777619u; }
            return h == 0 ? 1u : h;
        }

        static void Paint(TexPainter p, DomainDef d)
        {
            Color pr = d.Primary, se = d.Secondary;
            switch (d.Theme)
            {
                case DomainTheme.Void:
                    p.VGradient(new Color(0f, 0.01f, 0.04f), new Color(0.01f, 0.02f, 0.08f));
                    p.Stars(900, new Color(0.8f, 0.9f, 1f), 0f, 1.3f);
                    for (int i = 0; i < 60; i++) { float y = p.Rand(0, H); float x = p.Rand(0, W); p.Line(x, y, x + p.Rand(20, 90), y + p.Rand(-4, 4), 0.8f, new Color(0.7f, 0.85f, 1f, 0.25f)); }
                    p.Radial(W * 0.5f, H * 0.55f, 170, new Color(0.3f, 0.5f, 1f, 0.5f), new Color(0f, 0f, 0.2f, 0f), true);
                    p.RingShape(W * 0.5f, H * 0.55f, 70, 7, new Color(0.95f, 0.97f, 1f, 0.95f));
                    p.RingShape(W * 0.5f, H * 0.55f, 80, 18, new Color(0.5f, 0.75f, 1f, 0.35f));
                    p.Circle(W * 0.5f, H * 0.55f, 66, new Color(0f, 0f, 0f, 1f));
                    break;
                case DomainTheme.Shrine:
                {
                    p.VGradient(new Color(0.15f, 0f, 0.01f), new Color(0.55f, 0.02f, 0.05f), 0.7f);
                    p.Radial(W * 0.5f, H * 0.75f, 120, new Color(1f, 0.15f, 0.1f, 0.4f), new Color(0.5f, 0f, 0f, 0f), true);
                    Color k = new Color(0.03f, 0f, 0f);
                    float cx = W * 0.5f;
                    p.Rect(cx - 95, 40, cx + 95, 120, k);
                    p.Poly(new[] { new Vector2(cx - 150, 120), new Vector2(cx + 150, 120), new Vector2(cx + 90, 155), new Vector2(cx - 90, 155) }, k);
                    p.Rect(cx - 70, 155, cx + 70, 175, k);
                    p.Poly(new[] { new Vector2(cx - 110, 175), new Vector2(cx + 110, 175), new Vector2(cx + 55, 205), new Vector2(cx - 55, 205) }, k);
                    // the maw: a gaping mouth of teeth in the shrine face
                    p.Rect(cx - 55, 55, cx + 55, 100, new Color(0.35f, 0f, 0.02f));
                    for (int i = 0; i < 9; i++) { float tx = cx - 50 + i * 12.5f; p.Poly(new[] { new Vector2(tx, 100), new Vector2(tx + 12, 100), new Vector2(tx + 6, 86) }, new Color(0.85f, 0.8f, 0.7f)); p.Poly(new[] { new Vector2(tx, 55), new Vector2(tx + 12, 55), new Vector2(tx + 6, 68) }, new Color(0.85f, 0.8f, 0.7f)); }
                    // pillars and skull piles
                    for (int i = 0; i < 6; i++) p.Rect(cx - 140 + i * 56, 0, cx - 132 + i * 56, 120, k);
                    for (int i = 0; i < 160; i++) { float x = p.Rand(0, W), y = p.Rand(0, 40) * (1f - Mathf.Abs(x - cx) / W); p.Circle(x, y, p.Rand(3, 7), new Color(0.82f, 0.78f, 0.7f, 0.9f)); p.Circle(x - 1.5f, y + 1, 1.2f, Color.black); p.Circle(x + 1.5f, y + 1, 1.2f, Color.black); }
                    // blood water reflection
                    p.Rect(0, 0, W, 10, new Color(0.4f, 0f, 0.02f, 0.8f));
                    break;
                }
                case DomainTheme.Caldera:
                    p.VGradient(new Color(1f, 0.35f, 0.05f), new Color(0.15f, 0.03f, 0.02f), 0.6f);
                    for (int i = 0; i < 10; i++) { float x = p.Rand(0, W); p.Poly(new[] { new Vector2(x - 60, H), new Vector2(x + 60, H), new Vector2(x + p.Rand(-20, 20), p.Rand(60, 160)) }, new Color(0.06f, 0.02f, 0.02f)); }
                    p.Rect(0, 0, W, 30, new Color(1f, 0.5f, 0.1f));
                    for (int i = 0; i < 40; i++) p.Radial(p.Rand(0, W), p.Rand(0, 40), p.Rand(10, 30), new Color(1f, 0.8f, 0.3f, 0.6f), new Color(1f, 0.3f, 0f, 0f), true);
                    p.Stars(200, new Color(1f, 0.7f, 0.3f), 0.2f, 1.5f);
                    break;
                case DomainTheme.Shore:
                    p.VGradient(new Color(0.5f, 0.85f, 1f), new Color(0.15f, 0.45f, 0.95f), 0.8f);
                    p.Radial(W * 0.75f, H * 0.75f, 60, new Color(1f, 1f, 0.9f, 1f), new Color(1f, 0.95f, 0.6f, 0f), true);
                    p.Rect(0, 30, W, 110, new Color(0.1f, 0.55f, 0.75f));
                    for (int y = 35; y < 110; y += 6) for (int x = 0; x < W; x += 24) p.Line(x + (y % 12), y, x + 12 + (y % 12), y, 1.2f, new Color(0.8f, 0.95f, 1f, 0.35f));
                    p.Rect(0, 0, W, 34, new Color(0.95f, 0.88f, 0.65f));
                    for (int i = 0; i < 40; i++) { float x = p.Rand(0, W), y = p.Rand(45, 105); p.Line(x, y, x + 10, y + 2, 2.5f, new Color(0.05f, 0.2f, 0.3f, 0.5f)); }
                    break;
                case DomainTheme.Shadow:
                    p.VGradient(new Color(0.02f, 0.01f, 0.04f), new Color(0.01f, 0f, 0.02f));
                    for (int i = 0; i < 30; i++) { float x = p.Rand(0, W); p.Line(x, 0, x + p.Rand(-30, 30), p.Rand(40, 180), p.Rand(3, 8), new Color(0f, 0f, 0f, 0.95f)); }
                    p.Rect(0, 0, W, 40, new Color(0.08f, 0.04f, 0.14f));
                    for (int i = 0; i < 18; i++) p.RingShape(p.Rand(0, W), p.Rand(5, 35), p.Rand(10, 40), 1.5f, new Color(0.35f, 0.25f, 0.6f, 0.4f));
                    for (int i = 0; i < 8; i++) { float x = p.Rand(0, W), y = p.Rand(80, 200); p.Circle(x, y, 2.5f, new Color(0.8f, 0.7f, 1f, 0.9f)); p.Circle(x + 9, y, 2.5f, new Color(0.8f, 0.7f, 1f, 0.9f)); }
                    break;
                case DomainTheme.Swords:
                    p.VGradient(new Color(0.12f, 0.05f, 0.12f), new Color(0.65f, 0.2f, 0.3f), 0.7f);
                    p.Radial(W * 0.5f, H * 0.65f, 140, new Color(1f, 0.85f, 0.85f, 0.35f), new Color(1f, 0.5f, 0.6f, 0f), true);
                    for (int i = 0; i < 70; i++)
                    {
                        float x = p.Rand(0, W), h = p.Rand(25, 90), ang = p.Rand(-0.25f, 0.25f);
                        float tx = x + Mathf.Sin(ang) * h, ty = Mathf.Cos(ang) * h;
                        p.Line(x, 0, tx, ty, 2.2f, new Color(0.85f, 0.85f, 0.92f, 0.95f));
                        p.Line(tx - 6, ty - 4, tx + 6, ty - 4, 2f, new Color(0.2f, 0.1f, 0.1f));
                        p.Line(tx, ty - 4, tx, ty + 10, 3f, new Color(0.15f, 0.05f, 0.08f));
                    }
                    break;
                case DomainTheme.Court:
                    p.VGradient(new Color(0.06f, 0.04f, 0.03f), new Color(0.12f, 0.08f, 0.06f));
                    p.Poly(new[] { new Vector2(W * 0.5f - 30, H), new Vector2(W * 0.5f + 30, H), new Vector2(W * 0.5f + 140, 0), new Vector2(W * 0.5f - 140, 0) }, new Color(1f, 0.92f, 0.7f, 0.18f));
                    p.Rect(W * 0.5f - 120, 40, W * 0.5f + 120, 95, new Color(0.18f, 0.1f, 0.06f));
                    p.Rect(W * 0.5f - 125, 92, W * 0.5f + 125, 100, new Color(0.3f, 0.18f, 0.1f));
                    // scales
                    p.Line(W * 0.5f, 140, W * 0.5f, 200, 3f, new Color(1f, 0.85f, 0.4f));
                    p.Line(W * 0.5f - 50, 195, W * 0.5f + 50, 195, 3f, new Color(1f, 0.85f, 0.4f));
                    p.RingShape(W * 0.5f - 50, 175, 14, 2.5f, new Color(1f, 0.85f, 0.4f));
                    p.RingShape(W * 0.5f + 50, 175, 14, 2.5f, new Color(1f, 0.85f, 0.4f));
                    for (int i = 0; i < 6; i++) p.Rect(30 + i * 85, 0, 40 + i * 85, H, new Color(0, 0, 0, 0.5f));
                    break;
                case DomainTheme.Jackpot:
                    p.VGradient(new Color(0.25f, 0f, 0.35f), new Color(1f, 0.2f, 0.6f), 0.8f);
                    for (int x = 0; x < W; x += 18) for (int y = 0; y < H; y += 18) p.Circle(x + 9, y + 9, 2.2f, ((x + y) / 18 % 3 == 0 ? new Color(1f, 0.9f, 0.3f) : (x + y) / 18 % 3 == 1 ? new Color(0.3f, 1f, 1f) : new Color(1f, 1f, 1f)).WithA(0.85f));
                    for (int i = 0; i < 5; i++) { float x = 30 + i * 100; p.Rect(x, 120, x + 70, 210, new Color(0.08f, 0f, 0.12f)); p.Rect(x + 4, 124, x + 66, 206, new Color(0.9f, 0.85f, 1f, 0.15f)); }
                    p.Rect(0, 100, W, 108, new Color(1f, 0.9f, 0.3f));
                    p.Rect(0, 0, W, 30, new Color(0.08f, 0f, 0.1f));
                    break;
                case DomainTheme.Palms:
                    p.VGradient(new Color(0.02f, 0.1f, 0.1f), new Color(0.05f, 0.2f, 0.22f));
                    for (int i = 0; i < 90; i++)
                    {
                        float x = p.Rand(0, W), y = p.Rand(0, H), sc = p.Rand(0.6f, 1.6f);
                        var col = new Color(0.55f, 0.75f, 0.72f, p.Rand(0.15f, 0.5f));
                        p.Circle(x, y, 9 * sc, col);
                        for (int fgr = 0; fgr < 5; fgr++) { float a = Mathf.PI * (0.15f + fgr * 0.17f); p.Line(x, y, x + Mathf.Cos(a) * 18 * sc, y + Mathf.Sin(a) * 18 * sc, 3.5f * sc, col); }
                    }
                    break;
                case DomainTheme.Womb:
                    p.VGradient(new Color(0.18f, 0.01f, 0.03f), new Color(0.35f, 0.05f, 0.08f));
                    for (int i = 0; i < 30; i++) p.RingShape(p.Rand(0, W), p.Rand(0, H), p.Rand(15, 50), p.Rand(2, 6), new Color(0.6f, 0.1f, 0.15f, 0.5f));
                    for (int i = 0; i < 40; i++) { float x = p.Rand(0, W), y = p.Rand(0, H); p.Line(x, y, x + p.Rand(-60, 60), y + p.Rand(-60, 60), 2f, new Color(0.5f, 0.02f, 0.06f, 0.6f)); }
                    for (int y = 10; y < H; y += 30) p.Line(0, y, W, y - 10, 1f, new Color(1f, 0.3f, 0.35f, 0.15f));
                    break;
                case DomainTheme.Moon:
                    p.VGradient(new Color(0.02f, 0.03f, 0.08f), new Color(0.08f, 0.1f, 0.25f));
                    p.Radial(W * 0.5f, H * 0.62f, 150, new Color(0.7f, 0.75f, 1f, 0.35f), new Color(0.2f, 0.25f, 0.6f, 0f), true);
                    p.Circle(W * 0.5f, H * 0.62f, 62, new Color(0.95f, 0.95f, 1f));
                    p.Circle(W * 0.5f + 18, H * 0.62f + 10, 12, new Color(0.85f, 0.85f, 0.92f));
                    for (int x = 0; x < W; x += 42) p.Line(x, 0, x, H, 1.2f, new Color(0.9f, 0.9f, 1f, 0.22f));
                    for (int y = 0; y < H; y += 32) p.Line(0, y, W, y, 1.2f, new Color(0.9f, 0.9f, 1f, 0.22f));
                    break;
                case DomainTheme.Hometown:
                    p.VGradient(new Color(0.95f, 0.55f, 0.25f), new Color(0.35f, 0.2f, 0.45f), 0.7f);
                    p.Radial(W * 0.3f, H * 0.42f, 80, new Color(1f, 0.9f, 0.6f, 0.9f), new Color(1f, 0.6f, 0.3f, 0f), true);
                    for (float x = 0; x < W;) { float w = p.Rand(30, 60), h = p.Rand(40, 110); p.Rect(x, 30, x + w, 30 + h, new Color(0.2f, 0.1f, 0.15f)); x += w + p.Rand(2, 10); }
                    p.Rect(0, 0, W, 34, new Color(0.25f, 0.18f, 0.2f));
                    p.Rect(0, 30, W, 34, new Color(0.9f, 0.8f, 0.4f));
                    for (int i = 0; i < 5; i++) { float x = 40 + i * 110; p.Line(x, 34, x, 200, 3, new Color(0.1f, 0.06f, 0.08f)); p.Line(x - 15, 190, x + 15, 190, 2, new Color(0.1f, 0.06f, 0.08f)); }
                    for (int k = 0; k < 3; k++) p.Line(0, 180 + k * 6, W, 175 + k * 8, 1f, new Color(0.1f, 0.06f, 0.08f, 0.8f));
                    break;
                case DomainTheme.Garden:
                    p.VGradient(new Color(0.15f, 0.35f, 0.18f), new Color(0.85f, 0.95f, 0.75f), 0.8f);
                    p.Radial(W * 0.5f, H, 200, new Color(1f, 1f, 0.9f, 0.6f), new Color(1f, 1f, 0.8f, 0f), true);
                    for (int i = 0; i < 12; i++) { float x = p.Rand(0, W); p.Line(x, 0, x + p.Rand(-20, 20), p.Rand(120, 220), p.Rand(5, 10), new Color(0.25f, 0.18f, 0.1f)); p.Circle(x, p.Rand(150, 220), p.Rand(30, 55), new Color(0.2f, 0.5f, 0.25f, 0.7f)); }
                    for (int i = 0; i < 250; i++) { float x = p.Rand(0, W), y = p.Rand(0, 45); p.Circle(x, y, p.Rand(2, 4), Color.HSVToRGB(p.Rand(0.8f, 1f), 0.5f, 1f)); }
                    break;
                case DomainTheme.Spheres:
                    p.VGradient(new Color(0.12f, 0.12f, 0.14f), new Color(0.35f, 0.36f, 0.4f));
                    for (int i = 0; i < 26; i++)
                    {
                        float x = p.Rand(0, W), y = p.Rand(0, H), r = p.Rand(10, 40);
                        p.Circle(x, y, r, new Color(0.55f, 0.57f, 0.62f));
                        p.Radial(x - r * 0.3f, y + r * 0.3f, r * 0.6f, new Color(1f, 1f, 1f, 0.8f), new Color(1f, 1f, 1f, 0f), true);
                    }
                    break;
            }
            // tint toward the domain's colors
            for (int i = 0; i < p.Px.Length; i += 1)
            {
                Color c = p.Px[i];
                p.Px[i] = Color.Lerp(c, new Color(c.r * pr.r * 1.3f + 0.02f, c.g * pr.g * 1.3f + 0.02f, c.b * pr.b * 1.3f + 0.02f, 1f), 0.15f);
            }
        }
    }
}
