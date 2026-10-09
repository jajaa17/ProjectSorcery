using UnityEngine;

namespace ProjectSorcery
{
    /// <summary>
    /// Anime impact frames: for a few frames the world drops to flat black / white / red and fighters
    /// invert to stark silhouettes. Runs on real time so it reads at full speed during slow-motion.
    /// With "Impact Flashes" off, a single soft tint is used instead of strobing.
    /// </summary>
    public static class ImpactFrames
    {
        public enum FighterInk { Normal, White, Black }

        struct Frame { public Color Bg; public FighterInk Ink; public float Dur; }   // Dur = 60 fps frames (each lasts at least one rendered frame)

        static SpriteRenderer overlay;
        static Frame[] seq;
        static int idx, shown;
        static float t;
        public static FighterInk Ink = FighterInk.Normal;
        public static bool Active => seq != null;

        static readonly Color Black = new Color(0.01f, 0.01f, 0.015f, 1f);
        static readonly Color White = new Color(0.97f, 0.96f, 0.94f, 1f);
        static readonly Color Red = new Color(0.78f, 0.02f, 0.06f, 1f);

        public static void Init(Transform root)
        {
            overlay = Art.NewSprite(root, "ImpactOverlay", Art.White, Art.Line, Art.OrderImpactBg);
            overlay.enabled = false;
        }

        public static void Play(ImpactKind kind)
        {
            if (overlay == null) return;
            if (!Settings.ImpactFlashes)
            {
                seq = new[] { new Frame { Bg = kind == ImpactKind.BlackFlash ? Red.WithA(0.35f) : Black.WithA(0.3f), Ink = FighterInk.Normal, Dur = 5f } };
            }
            else switch (kind)
            {
                case ImpactKind.Heavy:
                    if (Active) return; // never interrupt a bigger sequence
                    seq = new[] { new Frame { Bg = Black.WithA(0.92f), Ink = FighterInk.White, Dur = 2f } };
                    break;
                case ImpactKind.BlackFlash:
                    seq = new[]
                    {
                        new Frame { Bg = Black, Ink = FighterInk.White, Dur = 3f },
                        new Frame { Bg = Red, Ink = FighterInk.Black, Dur = 4f },
                        new Frame { Bg = Black, Ink = FighterInk.White, Dur = 3f },
                        new Frame { Bg = White, Ink = FighterInk.Black, Dur = 3f },
                        new Frame { Bg = Red.WithA(0.55f), Ink = FighterInk.Black, Dur = 4f },
                    };
                    break;
                case ImpactKind.Domain:
                    seq = new[]
                    {
                        new Frame { Bg = White, Ink = FighterInk.Black, Dur = 4f },
                        new Frame { Bg = Black, Ink = FighterInk.White, Dur = 4f },
                    };
                    break;
                case ImpactKind.Ko:
                    seq = new[]
                    {
                        new Frame { Bg = Black, Ink = FighterInk.White, Dur = 5f },
                        new Frame { Bg = Red, Ink = FighterInk.Black, Dur = 6f },
                        new Frame { Bg = Black.WithA(0.6f), Ink = FighterInk.White, Dur = 4f },
                    };
                    break;
            }
            idx = 0; t = 0f; shown = 0;
            Apply();
        }

        static void Apply()
        {
            if (seq == null) { overlay.enabled = false; Ink = FighterInk.Normal; return; }
            var f = seq[idx];
            overlay.enabled = true;
            overlay.color = f.Bg;
            Ink = f.Ink;
        }

        /// <summary>Called every frame after the camera moves.</summary>
        public static void Update(Camera cam)
        {
            if (overlay == null) return;
            if (seq != null)
            {
                // a frame advances only once it has been on screen for its frame count (at 60 fps timing)
                // AND been rendered at least that many times, so 2 frames really means 2 visible frames
                t += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                shown++;
                while (seq != null && t >= seq[idx].Dur / 60f && shown >= seq[idx].Dur)
                {
                    t = 0f; shown = 0;
                    idx++;
                    if (idx >= seq.Length) seq = null;
                }
                Apply();
            }
            if (cam != null && overlay.enabled)
            {
                float h = cam.orthographicSize * 2.2f, w = h * cam.aspect * 1.1f;
                var p = cam.transform.position;
                overlay.transform.position = new Vector3(p.x, p.y, 0f);
                overlay.transform.localScale = new Vector3(w, h, 1f);
            }
        }

        public static void Stop() { seq = null; if (overlay != null) Apply(); }
    }
}
