using System.Collections.Generic;
using UnityEngine;

namespace ProjectSorcery
{
    /// <summary>
    /// Procedural anime-style portraits: a stick head with its hair, marks and glowing eye, over speed lines
    /// in the character's cursed-energy color. Generated lazily and cached.
    /// </summary>
    public static class Portraits
    {
        static readonly Dictionary<int, Sprite> cache = new Dictionary<int, Sprite>();
        const int S = 192;

        public static Sprite Get(CharacterDef d)
        {
            if (cache.TryGetValue(d.Index, out var sp)) return sp;
            var tex = Paint(d);
            sp = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
            cache[d.Index] = sp;
            return sp;
        }

        internal static Texture2D Paint(CharacterDef d)
        {
            var p = new TexPainter(S, S, (uint)(d.Index * 7919 + 13));
            var look = d.Look;
            Color aura = look.Aura;
            Color dark = new Color(aura.r * 0.12f, aura.g * 0.12f, aura.b * 0.14f, 1f);
            // diagonal gradient background
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float t = (x + y) / (2f * S);
                    p.Px[y * S + x] = Color.Lerp(dark, new Color(aura.r * 0.45f, aura.g * 0.45f, aura.b * 0.5f, 1f), t * t);
                }
            // speed lines radiating from the head
            float cx = S * 0.48f, cy = S * 0.5f;
            for (int i = 0; i < 46; i++)
            {
                float a = p.Rand(0f, Mathf.PI * 2f);
                float r0 = p.Rand(55f, 80f), r1 = p.Rand(120f, 190f);
                p.Line(cx + Mathf.Cos(a) * r0, cy + Mathf.Sin(a) * r0, cx + Mathf.Cos(a) * r1, cy + Mathf.Sin(a) * r1, p.Rand(0.8f, 2.2f), aura.WithA(p.Rand(0.15f, 0.45f)));
            }
            p.Radial(cx, cy, 90f, aura.WithA(0.45f), aura.WithA(0f), true);

            Color body = look.Body.a > 0f ? look.Body : new Color(0.94f, 0.94f, 0.97f);
            Color accent = look.Accent.a > 0f ? look.Accent : body;
            float r = 34f;
            float hx = cx, hy = cy + 6f;

            // shoulders / neck
            p.Line(hx, hy - r, hx, hy - r - 26f, 7f, body);
            p.Line(hx - 58f, hy - r - 70f, hx, hy - r - 26f, 7f, body);
            p.Line(hx + 58f, hy - r - 70f, hx, hy - r - 26f, 7f, body);
            if (look.FourArms) { p.Line(hx - 66f, hy - r - 48f, hx, hy - r - 30f, 6f, body); p.Line(hx + 66f, hy - r - 48f, hx, hy - r - 30f, 6f, body); }

            // hair behind
            HairPaint(p, look.Hair, hx, hy, r, Color.Lerp(body, accent, 0.6f));

            // head ring (thick, inked)
            p.Circle(hx, hy, r + 5f, Color.black.WithA(0.6f));
            p.Circle(hx, hy, r, new Color(0.03f, 0.03f, 0.05f, 1f));
            p.RingShape(hx, hy, r, 6f, body);

            // eye(s): glowing anime slash (facing right)
            var fm = look.Face;
            bool blind = (fm & FaceMark.Blindfold) != 0;
            if (!blind)
            {
                EyeSlash(p, hx + r * 0.3f, hy + r * 0.12f, aura);
                if ((fm & FaceMark.ExtraEyes) != 0) EyeSlash(p, hx + r * 0.32f, hy - r * 0.2f, aura);
            }
            if (blind) { p.Line(hx - r - 4f, hy + r * 0.15f, hx + r + 6f, hy + r * 0.1f, 11f, accent.a > 0f ? accent : Color.white); }
            if ((fm & FaceMark.Glasses) != 0) { p.RingShape(hx + r * 0.42f, hy + r * 0.15f, 8f, 2.5f, Color.white); p.Line(hx - r * 0.5f, hy + r * 0.2f, hx + r * 0.2f, hy + r * 0.18f, 2f, Color.white); }
            if ((fm & FaceMark.Sunglasses) != 0) p.Line(hx - r * 0.2f, hy + r * 0.15f, hx + r * 0.9f, hy + r * 0.15f, 8f, new Color(0.05f, 0.05f, 0.08f));
            if ((fm & FaceMark.Goggles) != 0) { p.Rect(hx - r * 0.1f, hy + r * 0.02f, hx + r * 0.95f, hy + r * 0.32f, new Color(0.9f, 0.85f, 0.6f)); p.Line(hx - r, hy + r * 0.2f, hx - r * 0.1f, hy + r * 0.2f, 2.5f, body); }
            if ((fm & FaceMark.Marks) != 0) { p.Line(hx + r * 0.15f, hy - r * 0.05f, hx + r * 0.65f, hy - r * 0.25f, 3f, accent); p.Line(hx - r * 0.4f, hy + r * 0.55f, hx + r * 0.3f, hy + r * 0.65f, 3f, accent); }
            if ((fm & FaceMark.Stitches) != 0) { p.Line(hx - r * 0.75f, hy + r * 0.55f, hx + r * 0.75f, hy + r * 0.55f, 2.5f, accent); for (int i = -3; i <= 3; i++) p.Line(hx + i * r * 0.2f, hy + r * 0.45f, hx + i * r * 0.2f, hy + r * 0.65f, 2f, accent); }
            if ((fm & FaceMark.Patches) != 0) { p.Line(hx + r * 0.1f, hy + r * 0.9f, hx - r * 0.05f, hy - r * 0.9f, 2.5f, accent); for (int i = -3; i <= 3; i++) p.Line(hx - r * 0.12f, hy + i * r * 0.22f, hx + r * 0.12f, hy + i * r * 0.22f, 2f, accent); }
            if ((fm & FaceMark.Collar) != 0) p.Rect(hx - r * 0.6f, hy - r * 1.05f, hx + r * 0.9f, hy - r * 0.35f, new Color(0.08f, 0.08f, 0.12f));
            if ((fm & FaceMark.Mask) != 0) p.Rect(hx - r * 0.2f, hy - r * 0.8f, hx + r * 0.95f, hy - r * 0.1f, new Color(0.85f, 0.85f, 0.85f));
            if ((fm & FaceMark.Scar) != 0) p.Line(hx + r * 0.5f, hy - r * 0.25f, hx + r * 0.8f, hy - r * 0.55f, 2.5f, new Color(0.9f, 0.6f, 0.6f));
            if ((fm & FaceMark.Beard) != 0) p.Poly(new[] { new Vector2(hx - r * 0.6f, hy - r * 0.55f), new Vector2(hx + r * 0.9f, hy - r * 0.45f), new Vector2(hx + r * 0.2f, hy - r * 1.4f) }, Color.Lerp(body, accent, 0.5f));
            if ((fm & FaceMark.Halo) != 0) { p.RingShape(hx, hy + r * 1.7f, r * 0.9f, 4f, new Color(1f, 0.95f, 0.7f)); }
            if ((fm & FaceMark.Wheel) != 0) { float wy = hy + r * 1.75f; p.RingShape(hx, wy, r * 0.95f, 4f, new Color(1f, 0.92f, 0.65f)); for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4f; p.Line(hx, wy, hx + Mathf.Cos(a) * r * 0.95f, wy + Mathf.Sin(a) * r * 0.95f, 2.5f, new Color(1f, 0.92f, 0.65f)); } }
            if ((fm & FaceMark.Crown) != 0) p.Poly(new[] { new Vector2(hx - r, hy + r * 0.9f), new Vector2(hx + r, hy + r * 0.9f), new Vector2(hx + r * 0.7f, hy + r * 1.6f), new Vector2(hx, hy + r * 1.2f), new Vector2(hx - r * 0.7f, hy + r * 1.6f) }, aura);
            if ((fm & FaceMark.Wings) != 0) { p.Line(hx - r * 0.9f, hy - r * 1.6f, hx - r * 2.6f, hy + r * 0.6f, 4f, Color.white); p.Line(hx - r * 0.9f, hy - r * 1.6f, hx - r * 2.3f, hy - r * 0.6f, 4f, Color.white); }

            // dramatic half-shadow and frame
            for (int y = 0; y < S; y++) for (int x = 0; x < S * 0.22f; x++) p.Blend(x, y, new Color(0f, 0f, 0f, 0.35f * (1f - x / (S * 0.22f))));
            p.Vignette(0.6f);
            return p.ToTexture("portrait_" + d.Id);
        }

        static void EyeSlash(TexPainter p, float x, float y, Color aura)
        {
            p.Radial(x, y, 18f, aura.WithA(0.9f), aura.WithA(0f), true);
            p.Line(x - 8f, y + 2f, x + 10f, y - 2f, 5f, Color.Lerp(aura, Color.white, 0.6f));
        }

        static void HairPaint(TexPainter p, Hair hair, float hx, float hy, float r, Color c)
        {
            Vector2 P(float deg, float rr) { float a = deg * Mathf.Deg2Rad; return new Vector2(hx + Mathf.Cos(a) * rr, hy + Mathf.Sin(a) * rr); }
            void Spikes(int n, float from, float to, float inner, float outer)
            {
                var pts = new List<Vector2>();
                for (int i = 0; i <= n; i++)
                {
                    float t = i / (float)n;
                    float deg = Mathf.Lerp(from, to, t);
                    pts.Add(P(deg, i % 2 == 0 ? inner : outer));
                }
                pts.Add(new Vector2(hx, hy));
                p.Poly(pts.ToArray(), c);
            }
            switch (hair)
            {
                case Hair.Spiky: Spikes(10, 200f, -20f, r * 1.05f, r * 1.75f); break;
                case Hair.Messy: Spikes(14, 215f, -30f, r * 1.1f, r * 1.95f); break;
                case Hair.Wild: Spikes(10, 220f, -30f, r * 1.1f, r * 2.3f); break;
                case Hair.Slick: Spikes(8, 200f, 20f, r * 1.15f, r * 1.3f); break;
                case Hair.Short: Spikes(8, 190f, 10f, r * 1.08f, r * 1.25f); break;
                case Hair.Bob: Spikes(12, 250f, -70f, r * 1.2f, r * 1.32f); break;
                case Hair.Ponytail: case Hair.Braid:
                    Spikes(8, 200f, 20f, r * 1.1f, r * 1.25f);
                    p.Line(hx - r * 0.9f, hy + r * 0.4f, hx - r * 1.9f, hy - r * 0.9f, 10f, c); break;
                case Hair.Long:
                    Spikes(10, 230f, 10f, r * 1.12f, r * 1.3f);
                    p.Poly(new[] { new Vector2(hx - r * 1.1f, hy + r * 0.3f), new Vector2(hx - r * 0.5f, hy + r * 0.3f), new Vector2(hx - r * 0.8f, hy - r * 2.5f), new Vector2(hx - r * 1.5f, hy - r * 2.5f) }, c); break;
                case Hair.Twin:
                    Spikes(8, 200f, -10f, r * 1.1f, r * 1.25f);
                    p.Line(hx - r, hy, hx - r * 1.6f, hy - r * 1.6f, 9f, c); p.Line(hx + r, hy, hx + r * 1.6f, hy - r * 1.6f, 9f, c); break;
                case Hair.Bun:
                    Spikes(8, 200f, 0f, r * 1.08f, r * 1.2f);
                    p.Circle(hx - r * 0.3f, hy + r * 1.25f, r * 0.45f, c); break;
                case Hair.TopKnot:
                    Spikes(8, 200f, 0f, r * 1.05f, r * 1.15f);
                    p.Line(hx, hy + r, hx + r * 0.2f, hy + r * 1.8f, 9f, c); break;
                case Hair.Horns:
                    p.Poly(new[] { P(150f, r * 0.9f), P(125f, r * 2.5f), P(105f, r * 0.95f) }, c);
                    p.Poly(new[] { P(75f, r * 0.95f), P(55f, r * 2.5f), P(35f, r * 0.9f) }, c); break;
                case Hair.Volcano:
                    p.Poly(new[] { P(165f, r * 0.95f), P(110f, r * 2.4f), P(70f, r * 2.4f), P(15f, r * 0.95f) }, c);
                    p.Radial(hx, hy + r * 2.4f, r * 0.8f, new Color(1f, 0.5f, 0.1f, 0.9f), new Color(1f, 0.2f, 0f, 0f), true); break;
                case Hair.Ears:
                    p.Circle(hx - r * 0.75f, hy + r * 0.9f, r * 0.42f, new Color(0.08f, 0.08f, 0.1f));
                    p.Circle(hx + r * 0.75f, hy + r * 0.9f, r * 0.42f, new Color(0.08f, 0.08f, 0.1f)); break;
                case Hair.Hood: Spikes(14, 270f, -90f, r * 1.4f, r * 1.45f); break;
                case Hair.Afro: Spikes(14, 200f, -10f, r * 1.6f, r * 2.1f); break;
                case Hair.Bald: case Hair.None: default: break;
            }
        }
    }
}
