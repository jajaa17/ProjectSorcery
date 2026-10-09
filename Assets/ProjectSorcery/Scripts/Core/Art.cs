using UnityEngine;

namespace ProjectSorcery
{
    /// <summary>Shared runtime-generated materials, textures, sprites and fonts. No imported assets needed.</summary>
    public static class Art
    {
        public static Material Line;      // alpha-blended, white texture (stickman lines, solid shapes)
        public static Material AddLine;   // additive, white texture (glow lines, beams)
        public static Material AddSoft;   // additive, soft radial texture (glow particles)
        public static Material AlphaSoft; // alpha-blended soft (smoke, ink)
        public static Texture2D WhiteTex, SoftTex, RingTex, ShardTex;
        public static Sprite White, Soft, Ring, Shard;
        public static Font Font;

        // Sorting orders (lower = further back)
        public const int OrderSky = -100, OrderSkyline = -90, OrderDomain = -80, OrderGround = -50,
                         OrderZone = -20, OrderImpactBg = 3, OrderMinion = 4, OrderFighterGlow = 5,
                         OrderFighter = 6, OrderProjectile = 10, OrderVfx = 12, OrderVfxTop = 14;

        static bool ready;

        public static void Init()
        {
            if (ready) return;
            ready = true;

            var spriteShader = Shader.Find("Sprites/Default");
            var addShader = Shader.Find("ProjectSorcery/AdditiveGlow");
            if (addShader == null) addShader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (addShader == null) addShader = spriteShader;

            WhiteTex = MakeTex(8, (u, v) => 1f, "ps_white");
            SoftTex = MakeTex(64, (u, v) =>
            {
                float d = Mathf.Sqrt(u * u + v * v);
                float a = Mathf.Clamp01(1f - d);
                return a * a;
            }, "ps_soft");
            RingTex = MakeTex(128, (u, v) =>
            {
                float d = Mathf.Sqrt(u * u + v * v);
                float a = 1f - Mathf.Abs(d - 0.85f) / 0.12f;
                return Mathf.Clamp01(a);
            }, "ps_ring");
            ShardTex = MakeTex(32, (u, v) =>
            {
                // jagged triangular glass shard
                float t = (v + 1f) * 0.5f;
                return Mathf.Abs(u) < (1f - t) * 0.8f ? 1f : 0f;
            }, "ps_shard");

            White = Sprite.Create(WhiteTex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8);
            Soft = Sprite.Create(SoftTex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
            Ring = Sprite.Create(RingTex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 128);
            Shard = Sprite.Create(ShardTex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);

            Line = new Material(spriteShader) { name = "PS_Line", mainTexture = WhiteTex };
            AlphaSoft = new Material(spriteShader) { name = "PS_AlphaSoft", mainTexture = SoftTex };
            AddLine = new Material(addShader) { name = "PS_AddLine", mainTexture = WhiteTex };
            AddSoft = new Material(addShader) { name = "PS_AddSoft", mainTexture = SoftTex };

            Font = LoadFont();
        }

        static Font LoadFont()
        {
            Font f = null;
            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (f == null) { try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
            if (f == null) f = Font.CreateDynamicFontFromOSFont("Arial", 32);
            return f;
        }

        public delegate float Shape(float u, float v);

        /// <summary>Creates a square white texture whose alpha follows shape(u,v) with u,v in [-1,1].</summary>
        public static Texture2D MakeTex(int size, Shape shape, string name)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size * 2f - 1f, v = (y + 0.5f) / size * 2f - 1f;
                    byte a = (byte)(Mathf.Clamp01(shape(u, v)) * 255f);
                    px[y * size + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return tex;
        }

        public static LineRenderer NewLine(Transform parent, string name, Material mat, int order, float width, int points = 2)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = mat;
            lr.useWorldSpace = true;
            lr.positionCount = points;
            lr.widthMultiplier = width;
            lr.numCapVertices = 3;
            lr.numCornerVertices = 2;
            lr.sortingOrder = order;
            lr.textureMode = LineTextureMode.Stretch;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            return lr;
        }

        public static SpriteRenderer NewSprite(Transform parent, string name, Sprite sprite, Material mat, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = mat;
            sr.sortingOrder = order;
            return sr;
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        public static Color WithA(this Color c, float a) { c.a = a; return c; }
        public static Color Mul(this Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);
    }
}
