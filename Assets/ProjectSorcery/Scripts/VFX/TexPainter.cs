using UnityEngine;

namespace ProjectSorcery
{
    /// <summary>Minimal CPU rasterizer for generating backgrounds, domain interiors and portraits at runtime.</summary>
    public sealed class TexPainter
    {
        public readonly int W, H;
        public readonly Color32[] Px;
        uint seed;

        public TexPainter(int w, int h, uint seed = 1)
        {
            W = w; H = h; Px = new Color32[w * h];
            this.seed = seed == 0 ? 1u : seed;
        }

        public float Rand()
        {
            seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5;
            return (seed & 0xFFFFFF) / 16777216f;
        }
        public float Rand(float a, float b) => a + (b - a) * Rand();

        public void Blend(int x, int y, Color c)
        {
            if ((uint)x >= (uint)W || (uint)y >= (uint)H || c.a <= 0f) return;
            int i = y * W + x;
            if (c.a >= 0.999f) { Px[i] = c; return; }
            Color d = Px[i];
            float a = c.a, ia = 1f - a;
            Px[i] = new Color(c.r * a + d.r * ia, c.g * a + d.g * ia, c.b * a + d.b * ia, Mathf.Max(d.a, a + d.a * ia));
        }

        public void Add(int x, int y, Color c)
        {
            if ((uint)x >= (uint)W || (uint)y >= (uint)H) return;
            int i = y * W + x;
            Color d = Px[i];
            Px[i] = new Color(Mathf.Min(1f, d.r + c.r * c.a), Mathf.Min(1f, d.g + c.g * c.a), Mathf.Min(1f, d.b + c.b * c.a), Mathf.Max(d.a, c.a));
        }

        public void Fill(Color c) { Color32 c32 = c; for (int i = 0; i < Px.Length; i++) Px[i] = c32; }

        /// <summary>Vertical gradient from bottom color to top color.</summary>
        public void VGradient(Color bottom, Color top, float power = 1f)
        {
            for (int y = 0; y < H; y++)
            {
                Color c = Color.Lerp(bottom, top, Mathf.Pow(y / (float)(H - 1), power));
                Color32 c32 = c;
                for (int x = 0; x < W; x++) Px[y * W + x] = c32;
            }
        }

        public void Radial(float cx, float cy, float r, Color inner, Color outer, bool additive = false)
        {
            int x0 = Mathf.Max(0, (int)(cx - r)), x1 = Mathf.Min(W - 1, (int)(cx + r)), y0 = Mathf.Max(0, (int)(cy - r)), y1 = Mathf.Min(H - 1, (int)(cy + r));
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / r;
                    if (d > 1f) continue;
                    Color c = Color.Lerp(inner, outer, d);
                    c.a *= 1f - d * d;
                    if (additive) Add(x, y, c); else Blend(x, y, c);
                }
        }

        public void Rect(float x0, float y0, float x1, float y1, Color c)
        {
            int a = Mathf.Max(0, (int)x0), b = Mathf.Min(W - 1, (int)x1), cy0 = Mathf.Max(0, (int)y0), cy1 = Mathf.Min(H - 1, (int)y1);
            for (int y = cy0; y <= cy1; y++) for (int x = a; x <= b; x++) Blend(x, y, c);
        }

        public void Circle(float cx, float cy, float r, Color c)
        {
            int x0 = Mathf.Max(0, (int)(cx - r - 1)), x1 = Mathf.Min(W - 1, (int)(cx + r + 1)), y0 = Mathf.Max(0, (int)(cy - r - 1)), y1 = Mathf.Min(H - 1, (int)(cy + r + 1));
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                    float a = Mathf.Clamp01(r - d + 0.5f);
                    if (a > 0f) Blend(x, y, c.WithA(c.a * a));
                }
        }

        public void RingShape(float cx, float cy, float r, float width, Color c)
        {
            int x0 = Mathf.Max(0, (int)(cx - r - width)), x1 = Mathf.Min(W - 1, (int)(cx + r + width)), y0 = Mathf.Max(0, (int)(cy - r - width)), y1 = Mathf.Min(H - 1, (int)(cy + r + width));
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float d = Mathf.Abs(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r);
                    float a = Mathf.Clamp01(width * 0.5f - d + 0.5f);
                    if (a > 0f) Blend(x, y, c.WithA(c.a * a));
                }
        }

        public void Line(float x0, float y0, float x1, float y1, float width, Color c)
        {
            float len = Mathf.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
            int steps = Mathf.Max(1, (int)(len / Mathf.Max(0.5f, width * 0.35f)));
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Circle(Mathf.Lerp(x0, x1, t), Mathf.Lerp(y0, y1, t), width * 0.5f, c);
            }
        }

        /// <summary>Scanline fill of a simple polygon.</summary>
        public void Poly(Vector2[] pts, Color c)
        {
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var p in pts) { minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y); }
            int y0 = Mathf.Max(0, (int)minY), y1 = Mathf.Min(H - 1, (int)maxY);
            var xs = new float[pts.Length];
            for (int y = y0; y <= y1; y++)
            {
                float sy = y + 0.5f;
                int n = 0;
                for (int i = 0; i < pts.Length; i++)
                {
                    Vector2 a = pts[i], b = pts[(i + 1) % pts.Length];
                    if ((a.y <= sy && b.y > sy) || (b.y <= sy && a.y > sy))
                        xs[n++] = a.x + (sy - a.y) / (b.y - a.y) * (b.x - a.x);
                }
                System.Array.Sort(xs, 0, n);
                for (int k = 0; k + 1 < n; k += 2)
                {
                    int xa = Mathf.Max(0, Mathf.CeilToInt(xs[k] - 0.5f)), xb = Mathf.Min(W - 1, Mathf.FloorToInt(xs[k + 1] - 0.5f));
                    for (int x = xa; x <= xb; x++) Blend(x, y, c);
                }
            }
        }

        public void Stars(int count, Color c, float yMin = 0f, float maxR = 1.2f)
        {
            for (int i = 0; i < count; i++)
            {
                float x = Rand(0, W), y = Rand(yMin * H, H);
                Circle(x, y, Rand(0.4f, maxR), c.WithA(c.a * Rand(0.3f, 1f)));
            }
        }

        /// <summary>Darkens/lightens the whole image with soft value noise (texture/grain).</summary>
        public void Grain(float amount)
        {
            for (int i = 0; i < Px.Length; i++)
            {
                float n = (Rand() - 0.5f) * amount;
                Color c = Px[i];
                Px[i] = new Color(Mathf.Clamp01(c.r + n), Mathf.Clamp01(c.g + n), Mathf.Clamp01(c.b + n), c.a);
            }
        }

        public void Vignette(float strength)
        {
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float dx = x / (float)W - 0.5f, dy = y / (float)H - 0.5f;
                    float k = 1f - Mathf.Clamp01((dx * dx + dy * dy) * 2.2f) * strength;
                    int i = y * W + x;
                    Color c = Px[i];
                    Px[i] = new Color(c.r * k, c.g * k, c.b * k, c.a);
                }
        }

        public Texture2D ToTexture(string name, bool clamp = true)
        {
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false) { name = name, wrapMode = clamp ? TextureWrapMode.Clamp : TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            t.SetPixels32(Px);
            t.Apply(false, true);
            return t;
        }
    }
}
