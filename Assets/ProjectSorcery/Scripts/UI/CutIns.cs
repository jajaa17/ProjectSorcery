using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectSorcery
{
    /// <summary>
    /// Anime cut-ins: diagonal portrait panels for domain expansions and N-way domain clashes,
    /// the clash push meter, and the jackpot reels.
    /// </summary>
    public static class CutIns
    {
        sealed class Panel
        {
            public RectTransform Rt;
            public Image Band, Portrait;
            public Text Name, Domain;
            public float FromX, ToX;
            public Fighter F;
        }

        static RectTransform root;
        static Image dim, lines;
        static Text title, prompt;
        static readonly List<Panel> panels = new List<Panel>();
        static readonly List<Image> meter = new List<Image>();
        static RectTransform meterRoot;
        static float age, life;
        static bool clash;

        // jackpot
        static RectTransform reelRoot;
        static readonly Text[] reels = new Text[3];
        static float reelAge; static bool reelSpinning; static bool reelHit; static float reelStopAt = -1f;

        static void Ensure()
        {
            if (root != null) return;
            root = UIKit.Rect(UIRoot.CutIns, "CutIn", Vector2.zero, Vector2.one);
            dim = UIKit.Fill(root, "Dim", new Color(0f, 0f, 0f, 0f));
            var lrt = UIKit.Box(root, "Lines", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2600f, 1300f));
            lines = UIKit.Img(lrt, new Color(1f, 1f, 1f, 0f), UIKit.SpeedLines());
            title = UIKit.LabelBox(root, "", 110, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1800f, 200f), TextAnchor.MiddleCenter, FontStyle.BoldAndItalic);
            UIKit.Outline(title, new Color(0.9f, 0.05f, 0.1f), 5f);
            UIKit.Shadow(title, Color.black, 9f);
            prompt = UIKit.LabelBox(root, "", 40, Color.white, new Vector2(0.5f, 0.2f), Vector2.zero, new Vector2(1400f, 80f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Outline(prompt, Color.black, 3f);
            meterRoot = UIKit.Box(root, "Meter", new Vector2(0.5f, 0.12f), Vector2.zero, new Vector2(1100f, 34f));
            root.gameObject.SetActive(false);

            reelRoot = UIKit.Box(UIRoot.CutIns, "Reels", new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(560f, 190f));
            UIKit.Img(reelRoot, new Color(0.1f, 0f, 0.15f, 0.92f));
            for (int i = 0; i < 3; i++)
            {
                var b = UIKit.Box(reelRoot, "Reel", new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 175f, 0f), new Vector2(155f, 160f));
                UIKit.Img(b, new Color(1f, 0.95f, 0.85f, 1f));
                reels[i] = UIKit.Label(b, "7", 120, new Color(0.9f, 0.1f, 0.3f), TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            reelRoot.gameObject.SetActive(false);
        }

        static Panel MakePanel(Fighter f, DomainDef d, int index, int count)
        {
            var p = new Panel { F = f };
            float h = count == 1 ? 360f : count == 2 ? 380f : 300f;
            float y = count == 1 ? 0f : count == 2 ? 0f : (1 - index) * 320f;
            bool fromLeft = count == 2 ? index == 0 : index % 2 == 0;
            if (count == 1) fromLeft = false;
            float width = count == 2 ? 1150f : 2400f;
            float targetX = count == 2 ? (index == 0 ? -470f : 470f) : 0f;
            p.Rt = UIKit.Box(root, "Panel", new Vector2(0.5f, 0.5f), new Vector2(fromLeft ? -2600f : 2600f, y), new Vector2(width, h));
            p.Rt.localRotation = Quaternion.Euler(0f, 0f, count == 2 ? (index == 0 ? 7f : -7f) : -5f);
            p.FromX = fromLeft ? -2600f : 2600f; p.ToX = targetX;
            p.Band = UIKit.Img(p.Rt, new Color(d.Primary.r * 0.55f, d.Primary.g * 0.55f, d.Primary.b * 0.6f, 0.95f));
            UIKit.Outline(p.Band, Color.black, 6f);
            var strip = UIKit.Rect(p.Rt, "Edge", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 10f));
            UIKit.Img(strip, Color.Lerp(d.Primary, Color.white, 0.5f));
            var prt = UIKit.Box(p.Rt, "Portrait", new Vector2(fromLeft ? 0f : 1f, 0.5f), new Vector2(fromLeft ? 220f : -220f, 0f), new Vector2(h * 1.05f, h * 1.05f));
            p.Portrait = UIKit.Img(prt, Color.white, Portraits.Get(f.Def));
            p.Portrait.preserveAspect = true;
            if (!fromLeft) prt.localScale = new Vector3(-1f, 1f, 1f);   // face inward
            float textX = fromLeft ? 0.62f : 0.38f;
            p.Name = UIKit.LabelBox(p.Rt, f.Def.Name.ToUpperInvariant(), count == 3 ? 46 : 54, Color.white, new Vector2(textX, 0.68f), Vector2.zero, new Vector2(900f, 80f), TextAnchor.MiddleCenter, FontStyle.BoldAndItalic);
            UIKit.Outline(p.Name, Color.black, 3f);
            p.Domain = UIKit.LabelBox(p.Rt, "DOMAIN EXPANSION\n<size=" + (count == 3 ? 60 : 72) + "><b>" + d.Name.ToUpperInvariant() + "</b></size>", 30, Color.Lerp(d.Primary, Color.white, 0.55f),
                                      new Vector2(textX, 0.36f), Vector2.zero, new Vector2(1000f, 200f), TextAnchor.MiddleCenter, FontStyle.Italic);
            UIKit.Outline(p.Domain, Color.black, 3f);
            return p;
        }

        static void Clear()
        {
            foreach (var p in panels) Object.Destroy(p.Rt.gameObject);
            panels.Clear();
            foreach (var m in meter) Object.Destroy(m.transform.parent.gameObject);
            meter.Clear();
        }

        public static void Domain(Fighter f, DomainDef d)
        {
            Ensure();
            Clear();
            clash = false;
            root.gameObject.SetActive(true);
            panels.Add(MakePanel(f, d, 0, 1));
            title.text = "";
            prompt.text = "";
            age = 0f; life = 1.45f;
        }

        public static void Clash(List<Fighter> fs)
        {
            Ensure();
            Clear();
            clash = true;
            root.gameObject.SetActive(true);
            var dom = Match.I != null ? Match.I.Domains : null;
            for (int i = 0; i < fs.Count; i++)
            {
                var d = dom != null && i < dom.ClashDefs.Count ? dom.ClashDefs[i] : fs[i].Def.Domain;
                if (d == null) continue;
                panels.Add(MakePanel(fs[i], d, i, fs.Count));
            }
            title.text = fs.Count >= 3 ? "TRIPLE DOMAIN CLASH" : "DOMAIN CLASH";
            prompt.text = "";
            // push meter segments
            for (int i = 0; i < fs.Count; i++)
            {
                var seg = UIKit.Rect(meterRoot, "Seg", new Vector2(i / (float)fs.Count, 0f), new Vector2((i + 1) / (float)fs.Count, 1f));
                var img = UIKit.Img(UIKit.Rect(seg, "Fill", Vector2.zero, Vector2.one), (dom != null && i < dom.ClashDefs.Count ? dom.ClashDefs[i].Primary : Color.white));
                meter.Add(img);
            }
            age = 0f; life = DomainSystem.ClashCutIn + DomainSystem.ClashPush + 0.2f;
        }

        public static void EndClash()
        {
            if (root == null) return;
            if (clash) { life = Mathf.Min(life, age + 0.25f); }
        }

        public static void JackpotSpin()
        {
            Ensure();
            reelRoot.gameObject.SetActive(true);
            reelSpinning = true; reelAge = 0f; reelStopAt = -1f;
        }

        public static void JackpotResult(bool hit)
        {
            Ensure();
            reelHit = hit; reelStopAt = reelAge;
        }

        public static void Hide()
        {
            if (root == null) return;
            Clear();
            root.gameObject.SetActive(false);
            reelRoot.gameObject.SetActive(false);
            reelSpinning = false;
        }

        public static void Update()
        {
            if (root == null) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            if (root.gameObject.activeSelf)
            {
                age += dt;
                float inK = Mathf.Clamp01(age / 0.22f);
                float outK = Mathf.Clamp01((life - age) / 0.25f);
                float vis = Mathf.Min(inK, outK);
                dim.color = new Color(0f, 0f, 0f, 0.82f * vis * (clash && age > DomainSystem.ClashCutIn ? 0.35f : 1f));
                lines.color = new Color(1f, 1f, 1f, 0.55f * vis);
                lines.rectTransform.localRotation = Quaternion.Euler(0f, 0f, age * 25f);
                lines.rectTransform.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(age * 40f));

                bool pushPhase = clash && age > DomainSystem.ClashCutIn;
                for (int i = 0; i < panels.Count; i++)
                {
                    var p = panels[i];
                    float e = 1f - Mathf.Pow(1f - Mathf.Clamp01((age - i * 0.08f) / 0.3f), 3f);
                    float x = Mathf.Lerp(p.FromX, p.ToX, e);
                    if (outK < 1f) x = Mathf.Lerp(p.ToX, -p.FromX, 1f - outK);
                    var pos = p.Rt.anchoredPosition;
                    pos.x = x + Mathf.Sin(age * 50f + i) * 3f;
                    p.Rt.anchoredPosition = pos;
                    // during the push phase the panels shrink to the top to reveal the split domain
                    float sc = pushPhase ? Mathf.Lerp(1f, 0.38f, Mathf.Clamp01((age - DomainSystem.ClashCutIn) / 0.25f)) : 1f;
                    p.Rt.localScale = new Vector3(sc, sc, 1f);
                    if (pushPhase)
                    {
                        float tx = panels.Count == 2 ? (i == 0 ? -620f : 620f) : (i - 1) * 640f;
                        p.Rt.anchoredPosition = Vector2.Lerp(p.Rt.anchoredPosition, new Vector2(tx, 420f), Mathf.Clamp01((age - DomainSystem.ClashCutIn) / 0.25f));
                    }
                }
                float ts = age < 0.12f ? Mathf.Lerp(3f, 1f, age / 0.12f) : 1f;
                title.rectTransform.localScale = new Vector3(ts, ts, 1f);
                title.color = new Color(1f, 1f, 1f, vis * (pushPhase ? 0f : 1f));

                if (clash)
                {
                    meterRoot.gameObject.SetActive(pushPhase);
                    if (pushPhase)
                    {
                        prompt.text = "MASH  LIGHT / HEAVY  TO PUSH YOUR DOMAIN!";
                        prompt.color = Color.Lerp(Color.white, new Color(1f, 0.3f, 0.35f), Mathf.PingPong(age * 4f, 1f));
                        var dom = Match.I != null ? Match.I.Domains : null;
                        if (dom != null && dom.Clashing)
                        {
                            var sh = dom.Shares();
                            float acc = 0f;
                            for (int i = 0; i < meter.Count && i < sh.Count; i++)
                            {
                                var seg = (RectTransform)meter[i].transform.parent;
                                seg.anchorMin = new Vector2(acc, 0f);
                                acc += sh[i];
                                seg.anchorMax = new Vector2(acc, 1f);
                            }
                        }
                    }
                    else prompt.text = "";
                }
                else meterRoot.gameObject.SetActive(false);

                if (age >= life) { Clear(); root.gameObject.SetActive(false); }
            }

            if (reelSpinning)
            {
                reelAge += dt;
                for (int i = 0; i < 3; i++)
                {
                    bool stopped = reelStopAt >= 0f && reelAge > reelStopAt + 0.25f + i * 0.35f;
                    if (stopped) reels[i].text = reelHit ? "7" : (i == 2 ? "4" : "7");
                    else reels[i].text = Random.Range(1, 10).ToString();
                    reels[i].color = stopped && reelHit ? new Color(1f, 0.75f, 0.1f) : new Color(0.9f, 0.1f, 0.3f);
                }
                if (reelStopAt >= 0f && reelAge > reelStopAt + 2.5f) { reelSpinning = false; reelRoot.gameObject.SetActive(false); }
                if (reelAge > 8f) { reelSpinning = false; reelRoot.gameObject.SetActive(false); }
            }
        }
    }
}
