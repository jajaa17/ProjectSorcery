using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectSorcery
{
    /// <summary>Floating world texts: damage numbers, callouts, chant lines and the BLACK FLASH stamp.</summary>
    public static class Popups
    {
        sealed class Item
        {
            public Text T;
            public Outline O;
            public Vector2 World, Vel;
            public float Age, Life, Scale, Pop;
            public bool Active, Screen, Shake;
            public Fighter Follow;
        }

        static readonly List<Item> items = new List<Item>();
        static Text bfBig, bfSub;
        static float bfTimer;
        static int bfStreak;

        static Item Get()
        {
            foreach (var it in items) if (!it.Active) { it.T.enabled = true; return it; }
            if (items.Count > 80) { var old = items[0]; items.RemoveAt(0); items.Add(old); return old; }
            var rt = UIKit.Box(UIRoot.Popups, "Popup", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 120f));
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Art.Font; t.alignment = TextAnchor.MiddleCenter; t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false; t.fontStyle = FontStyle.BoldAndItalic;
            var o = rt.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.9f); o.effectDistance = new Vector2(3f, -3f);
            var item = new Item { T = t, O = o };
            items.Add(item);
            return item;
        }

        public static void World(Vector2 p, string text, Color c, float scale)
        {
            if (UIRoot.Popups == null) return;
            var it = Get();
            it.Active = true; it.Screen = false; it.Follow = null; it.Shake = false;
            it.World = p; it.Vel = new Vector2(0f, 1.2f); it.Age = 0f; it.Life = 1.1f; it.Scale = scale; it.Pop = 1.6f;
            it.T.text = text; it.T.color = c; it.T.fontSize = Mathf.RoundToInt(40 * scale);
        }

        public static void Damage(Vector2 p, float dmg, bool bf, Color c)
        {
            if (UIRoot.Popups == null) return;
            var it = Get();
            it.Active = true; it.Screen = false; it.Follow = null; it.Shake = bf;
            it.World = p + new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(0.2f, 0.6f));
            it.Vel = new Vector2(Random.Range(-1f, 1f), 2.4f);
            it.Age = 0f; it.Life = 0.8f; it.Scale = bf ? 1.4f : Mathf.Clamp(0.55f + dmg / 160f, 0.55f, 1.1f); it.Pop = 1.8f;
            it.T.text = Mathf.RoundToInt(dmg).ToString();
            it.T.color = bf ? new Color(1f, 0.15f, 0.2f) : Color.Lerp(Color.white, new Color(1f, 0.85f, 0.4f), Mathf.Clamp01(dmg / 120f));
            it.T.fontSize = Mathf.RoundToInt(34 * it.Scale);
        }

        public static void Chant(Fighter f, string line, Color c)
        {
            if (UIRoot.Popups == null) return;
            var it = Get();
            it.Active = true; it.Screen = false; it.Follow = f; it.Shake = false;
            it.World = f.HeadPos + new Vector2(0f, 1.2f + 0.45f * (f.ChantLevel - 1));
            it.Vel = Vector2.zero; it.Age = 0f; it.Life = 2.2f; it.Scale = 0.75f; it.Pop = 1.2f;
            it.T.text = "“" + line + "”";
            it.T.color = Color.Lerp(c, Color.white, 0.4f);
            it.T.fontSize = 30;
        }

        public static void BlackFlash(Vector2 p, int streak)
        {
            if (UIRoot.Popups == null) return;
            if (bfBig == null)
            {
                bfBig = UIKit.LabelBox(UIRoot.Popups, "", 150, Color.white, new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(1600f, 220f), TextAnchor.MiddleCenter, FontStyle.BoldAndItalic);
                UIKit.Outline(bfBig, new Color(0.85f, 0.02f, 0.08f), 6f);
                UIKit.Shadow(bfBig, Color.black, 10f);
                bfSub = UIKit.LabelBox(UIRoot.Popups, "", 54, new Color(1f, 0.2f, 0.25f), new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(1200f, 90f), TextAnchor.MiddleCenter, FontStyle.BoldAndItalic);
                UIKit.Outline(bfSub, Color.black, 4f);
            }
            bfTimer = 1.4f;
            bfStreak = streak;
            bfBig.text = "BLACK  FLASH";
            bfSub.text = streak > 1 ? "x" + streak + "  CONSECUTIVE" : "";
        }

        public static void Clear()
        {
            foreach (var it in items) { it.Active = false; it.T.enabled = false; }
            bfTimer = 0f;
            if (bfBig != null) { bfBig.enabled = false; bfSub.enabled = false; }
        }

        public static void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            foreach (var it in items)
            {
                if (!it.Active) continue;
                it.Age += dt;
                if (it.Age >= it.Life) { it.Active = false; it.T.enabled = false; continue; }
                if (it.Follow != null && !it.Follow.Dead) it.World = new Vector2(it.Follow.HeadPos.x, it.World.y);
                it.World += it.Vel * dt;
                it.Vel *= Mathf.Max(0f, 1f - dt * 3f);
                if (UIRoot.WorldToCanvas(it.World, out var local))
                {
                    if (it.Shake) local += Random.insideUnitCircle * 6f;
                    it.T.rectTransform.anchoredPosition = local;
                }
                float k = it.Age / it.Life;
                float pop = Mathf.Lerp(it.Pop, 1f, Mathf.Clamp01(it.Age * 9f));
                it.T.rectTransform.localScale = Vector3.one * pop;
                var c = it.T.color; c.a = k > 0.7f ? (1f - k) / 0.3f : 1f; it.T.color = c;
            }
            if (bfBig != null)
            {
                bool on = bfTimer > 0f;
                bfBig.enabled = on; bfSub.enabled = on;
                if (on)
                {
                    bfTimer -= dt;
                    float age = 1.4f - bfTimer;
                    float s = age < 0.08f ? Mathf.Lerp(2.6f, 1f, age / 0.08f) : 1f + 0.02f * Mathf.Sin(age * 60f) * Mathf.Max(0f, 0.4f - age);
                    bfBig.rectTransform.localScale = new Vector3(s, s, 1f);
                    bfBig.rectTransform.anchoredPosition = age < 0.5f ? Random.insideUnitCircle * 10f * (0.5f - age) : Vector2.zero;
                    float a = bfTimer < 0.3f ? bfTimer / 0.3f : 1f;
                    bool inv = ImpactFrames.Ink == ImpactFrames.FighterInk.Black;
                    bfBig.color = (inv ? Color.black : Color.white).WithA(a);
                    bfSub.color = new Color(1f, 0.2f, 0.25f, a);
                }
            }
        }
    }
}
