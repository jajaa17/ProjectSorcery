using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectSorcery
{
    /// <summary>
    /// Minimal fighting-game HUD: slim health / cursed energy bars, technique cooldown pips, combo counter,
    /// timer and round pips, boss bar, announcer and name tags. Updates text only when values change.
    /// </summary>
    public sealed class HUD
    {
        sealed class FighterPanel
        {
            public Fighter F;
            public RectTransform Rt;
            public Image Hp, HpLag, Ce, Portrait, Guard;
            public Text Name, Combo, Status;
            public Image[] Cd = new Image[4];
            public Image[] CdBack = new Image[4];
            public Text[] CdLabel = new Text[4];
            public float Lag = 1f, CeFlash;
            public int LastCombo = -1;
            public string LastStatus = null;
            public Text Tag;
            public bool Right;
        }

        readonly Match m;
        readonly RectTransform root;
        readonly List<FighterPanel> panels = new List<FighterPanel>();
        Text timer, header, announceBig, announceSmall;
        RectTransform pipsL, pipsR;
        float announceTimer, announceLife;
        int lastTime = -1;
        Image bossBar, bossLag; Text bossName; RectTransform bossRoot; float bossLagV = 1f;

        public HUD(Match match)
        {
            m = match;
            root = UIKit.Rect(UIRoot.Hud, "MatchHUD", Vector2.zero, Vector2.one);
            if (m.Config.Demo) { root.gameObject.SetActive(false); return; }
            Build();
            m.Events.OnAnnounce += Announce;
            m.Events.OnDomainExpanded += (f, d) => CutIns.Domain(f, d);
            m.Events.OnClashStart += fs => CutIns.Clash(fs);
            m.Events.OnClashEnd += w => CutIns.EndClash();
            m.Events.OnLowCe += f => { foreach (var p in panels) if (p.F == f) p.CeFlash = 0.4f; };
        }

        public void Destroy()
        {
            if (root != null) Object.Destroy(root.gameObject);
            CutIns.Hide();
        }

        void Build()
        {
            // timer + header
            var top = UIKit.Box(root, "Top", new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(170f, 90f));
            UIKit.Img(top, new Color(0f, 0f, 0f, 0.45f));
            timer = UIKit.Label(top, "99", 60, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Outline(timer, Color.black, 2f);
            header = UIKit.LabelBox(root, "", 28, new Color(1f, 0.9f, 0.7f), new Vector2(0.5f, 1f), new Vector2(0f, -118f), new Vector2(600f, 40f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Outline(header, Color.black, 2f);
            pipsL = UIKit.Box(root, "PipsL", new Vector2(0.5f, 1f), new Vector2(-120f, -110f), new Vector2(80f, 20f));
            pipsR = UIKit.Box(root, "PipsR", new Vector2(0.5f, 1f), new Vector2(120f, -110f), new Vector2(80f, 20f));

            // announcer
            announceBig = UIKit.LabelBox(root, "", 120, Color.white, new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(1800f, 180f), TextAnchor.MiddleCenter, FontStyle.BoldAndItalic);
            UIKit.Outline(announceBig, Color.black, 5f);
            UIKit.Shadow(announceBig, new Color(0f, 0f, 0f, 0.6f), 8f);
            announceSmall = UIKit.LabelBox(root, "", 36, new Color(0.9f, 0.9f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600f, 60f), TextAnchor.MiddleCenter, FontStyle.Italic);
            UIKit.Outline(announceSmall, Color.black, 3f);

            // fighter panels: team 0 on the left, others on the right
            int left = 0, right = 0;
            foreach (var f in m.Fighters)
            {
                if (f.IsBoss) { BuildBoss(f); continue; }
                bool isRight = f.Team != 0;
                if (m.Config.Layout == TeamLayout.FreeForAll && f.Team == 2) isRight = false;
                int idx = isRight ? right++ : left++;
                panels.Add(MakePanel(f, isRight, idx));
            }
        }

        void BuildBoss(Fighter f)
        {
            bossRoot = UIKit.Box(root, "Boss", new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(1100f, 56f));
            bossName = UIKit.LabelBox(bossRoot, f.Def.Name.ToUpperInvariant() + "  -  " + f.Def.Title.ToUpperInvariant(), 26, new Color(1f, 0.6f, 0.6f), new Vector2(0.5f, 1f), new Vector2(0f, 6f), new Vector2(1100f, 34f), TextAnchor.LowerCenter, FontStyle.Bold);
            UIKit.Outline(bossName, Color.black, 2f);
            bossLag = UIKit.Bar(bossRoot, new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(1100f, 22f), new Color(0f, 0f, 0f, 0.7f), new Color(1f, 1f, 1f, 0.8f));
            bossBar = UIKit.Bar(bossRoot, new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(1100f, 22f), new Color(0f, 0f, 0f, 0f), new Color(0.85f, 0.08f, 0.15f));
        }

        FighterPanel MakePanel(Fighter f, bool right, int idx)
        {
            var p = new FighterPanel { F = f, Right = right };
            float x = right ? -40f : 40f;
            float y = -30f - idx * 96f;
            var anchor = new Vector2(right ? 1f : 0f, 1f);
            p.Rt = UIKit.Box(root, "Panel_" + f.Tag, anchor, new Vector2(x, y), new Vector2(560f, 88f), new Vector2(right ? 1f : 0f, 1f));
            // portrait
            var prt = UIKit.Box(p.Rt, "Portrait", new Vector2(right ? 1f : 0f, 1f), Vector2.zero, new Vector2(78f, 78f), new Vector2(right ? 1f : 0f, 1f));
            UIKit.Img(prt, new Color(0f, 0f, 0f, 0.6f));
            p.Portrait = UIKit.Img(UIKit.Rect(prt, "Img", Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f)), Color.white, Portraits.Get(f.Def));
            if (right) p.Portrait.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            var teamStrip = UIKit.Rect(prt, "Team", new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 5f));
            UIKit.Img(teamStrip, f.TeamColor);

            float bx = right ? -92f : 92f;
            var pivot = new Vector2(right ? 1f : 0f, 1f);
            p.Name = UIKit.LabelBox(p.Rt, f.Tag + "  <color=#ffffffcc>" + f.Def.Name.ToUpperInvariant() + "</color>", 22, f.TeamColor, new Vector2(right ? 1f : 0f, 1f), new Vector2(bx, -2f), new Vector2(460f, 26f), right ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft, FontStyle.Bold);
            p.Name.rectTransform.pivot = pivot;
            UIKit.Outline(p.Name, Color.black, 1.5f);
            p.HpLag = UIKit.Bar(p.Rt, new Vector2(right ? 1f : 0f, 1f), new Vector2(bx, -30f), new Vector2(460f, 20f), new Color(0f, 0f, 0f, 0.65f), new Color(1f, 1f, 1f, 0.85f), pivot);
            p.Hp = UIKit.Bar(p.Rt, new Vector2(right ? 1f : 0f, 1f), new Vector2(bx, -30f), new Vector2(460f, 20f), new Color(0f, 0f, 0f, 0f), Color.Lerp(f.TeamColor, Color.white, 0.15f), pivot);
            p.Ce = UIKit.Bar(p.Rt, new Vector2(right ? 1f : 0f, 1f), new Vector2(bx, -54f), new Vector2(330f, 9f), new Color(0f, 0f, 0f, 0.65f), f.Def.Look.Aura, pivot);
            if (right) { p.Hp.fillOrigin = 1; p.HpLag.fillOrigin = 1; p.Ce.fillOrigin = 1; }
            if (f.IsRestricted) { p.Ce.transform.parent.gameObject.SetActive(false); }

            string[] keys = { "S1", "S2", "S3", "ULT" };
            for (int i = 0; i < 4; i++)
            {
                float cx = right ? -92f - i * 34f : 92f + i * 34f;
                var c = UIKit.Box(p.Rt, "Cd" + i, new Vector2(right ? 1f : 0f, 1f), new Vector2(cx, -68f), new Vector2(30f, 18f), pivot);
                p.CdBack[i] = UIKit.Img(c, new Color(0f, 0f, 0f, 0.6f));
                p.Cd[i] = UIKit.Img(UIKit.Rect(c, "Fill", Vector2.zero, Vector2.one, new Vector2(1f, 1f), new Vector2(-1f, -1f)), f.Def.Look.Aura, Art.White);
                p.Cd[i].type = Image.Type.Filled; p.Cd[i].fillMethod = Image.FillMethod.Horizontal;
                p.CdLabel[i] = UIKit.Label(c, keys[i], 12, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            p.Status = UIKit.LabelBox(p.Rt, "", 16, new Color(0.9f, 0.9f, 1f), new Vector2(right ? 1f : 0f, 1f), new Vector2(right ? -240f : 240f, -68f), new Vector2(330f, 20f), right ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft);
            p.Status.rectTransform.pivot = pivot;

            p.Combo = UIKit.LabelBox(root, "", 56, Color.white, new Vector2(right ? 1f : 0f, 0.62f), new Vector2(right ? -60f : 60f, -idx * 80f), new Vector2(400f, 80f), right ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft, FontStyle.BoldAndItalic);
            p.Combo.rectTransform.pivot = new Vector2(right ? 1f : 0f, 0.5f);
            UIKit.Outline(p.Combo, Color.black, 3f);

            p.Tag = UIKit.LabelBox(UIRoot.Hud, f.Tag, 18, f.TeamColor, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160f, 24f), TextAnchor.MiddleCenter, FontStyle.Bold);
            p.Tag.transform.SetParent(root, true);
            UIKit.Outline(p.Tag, Color.black, 1.5f);
            return p;
        }

        void Announce(string big, string small, Color c, float dur)
        {
            if (announceBig == null) return;
            if (!string.IsNullOrEmpty(big)) { announceBig.text = big; announceBig.color = c; }
            else announceBig.text = "";
            announceSmall.text = small ?? "";
            announceTimer = announceLife = dur;
        }

        public void Update()
        {
            if (m.Config.Demo || panels == null) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            var mode = m.Mode;

            // timer
            if (timer != null)
            {
                int t = mode.TimeLeft < 0f ? -1 : Mathf.CeilToInt(Mathf.Max(0f, mode.TimeLeft));
                if (t != lastTime) { lastTime = t; timer.text = t < 0 ? "∞" : t.ToString(); timer.color = t >= 0 && t <= 10 ? new Color(1f, 0.4f, 0.4f) : Color.white; }
                if (header.text != mode.Header) header.text = mode.Header;
            }
            if (mode.ShowRoundWins) DrawPips();

            foreach (var p in panels) UpdatePanel(p, dt);

            if (bossRoot != null && mode.Boss != null)
            {
                var b = mode.Boss;
                bossBar.fillAmount = b.HpFrac;
                bossLagV = Mathf.MoveTowards(bossLagV, b.HpFrac, dt * (bossLagV > b.HpFrac + 0.002f ? 0.25f : 10f));
                bossLag.fillAmount = bossLagV;
            }

            // announcer
            if (announceTimer > 0f)
            {
                announceTimer -= dt;
                float age = announceLife - announceTimer;
                float s = age < 0.1f ? Mathf.Lerp(2.2f, 1f, age / 0.1f) : 1f;
                announceBig.rectTransform.localScale = new Vector3(s, s, 1f);
                float a = announceTimer < 0.25f ? announceTimer / 0.25f : 1f;
                var c = announceBig.color; c.a = a; announceBig.color = c;
                var c2 = announceSmall.color; c2.a = a; announceSmall.color = c2;
            }
            else if (announceBig.text != "") { announceBig.text = ""; announceSmall.text = ""; }
        }

        int pipsDrawn = -1;
        void DrawPips()
        {
            int key = m.Mode.Wins[0] * 10 + m.Mode.Wins[1] + m.Config.RoundsToWin * 100;
            if (key == pipsDrawn) return;
            pipsDrawn = key;
            foreach (Transform c in pipsL) Object.Destroy(c.gameObject);
            foreach (Transform c in pipsR) Object.Destroy(c.gameObject);
            for (int i = 0; i < m.Config.RoundsToWin; i++)
            {
                var a = UIKit.Box(pipsL, "Pip", new Vector2(1f, 0.5f), new Vector2(-i * 22f, 0f), new Vector2(16f, 16f));
                UIKit.Img(a, i < m.Mode.Wins[0] ? Match.TeamColor(0) : new Color(1f, 1f, 1f, 0.2f));
                var b = UIKit.Box(pipsR, "Pip", new Vector2(0f, 0.5f), new Vector2(i * 22f, 0f), new Vector2(16f, 16f));
                UIKit.Img(b, i < m.Mode.Wins[1] ? Match.TeamColor(1) : new Color(1f, 1f, 1f, 0.2f));
            }
        }

        void UpdatePanel(FighterPanel p, float dt)
        {
            var f = p.F;
            float hp = f.HpFrac;
            p.Hp.fillAmount = hp;
            p.Lag = Mathf.MoveTowards(p.Lag, hp, dt * (p.Lag > hp + 0.002f ? (f.ComboTimer > 0f || f.State == FState.Hitstun ? 0.05f : 0.6f) : 10f));
            p.HpLag.fillAmount = p.Lag;
            p.Hp.color = hp < 0.25f ? Color.Lerp(new Color(1f, 0.3f, 0.3f), Color.white, Mathf.PingPong(Time.unscaledTime * 3f, 0.4f)) : Color.Lerp(f.TeamColor, Color.white, 0.15f);
            p.Ce.fillAmount = f.CeFrac;
            if (p.CeFlash > 0f) { p.CeFlash -= dt; p.Ce.color = Color.Lerp(f.Def.Look.Aura, Color.red, Mathf.PingPong(p.CeFlash * 10f, 1f)); }
            else p.Ce.color = f.Has(StatusType.Jackpot) ? Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime, 1f), 0.7f, 1f) : f.Def.Look.Aura;

            for (int i = 0; i < 4; i++)
            {
                var ab = f.Kit[i];
                if (ab == null) { p.Cd[i].fillAmount = 0f; continue; }
                float cdFrac = ab.Cooldown <= 0f ? 1f : 1f - Mathf.Clamp01(f.Cd[i] / ab.Cooldown);
                p.Cd[i].fillAmount = cdFrac;
                bool usable = f.CanCast(i) || (f.Cd[i] <= 0f && f.Ce >= f.CostOf(ab) && !f.Has(StatusType.Burnout) && !f.Has(StatusType.Sealed));
                p.Cd[i].color = usable ? f.Def.Look.Aura : new Color(0.35f, 0.35f, 0.4f, 0.9f);
                p.CdLabel[i].color = usable ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            }

            // combo counter
            int combo = f.ComboHits;
            if (combo != p.LastCombo)
            {
                p.LastCombo = combo;
                p.Combo.text = combo >= 2 ? combo + " HITS\n<size=26>" + Mathf.RoundToInt(f.ComboDamage) + " DMG</size>" : "";
                p.Combo.rectTransform.localScale = Vector3.one * 1.3f;
            }
            p.Combo.rectTransform.localScale = Vector3.Lerp(p.Combo.rectTransform.localScale, Vector3.one, dt * 12f);

            // status line (rebuilt only when it changes)
            string s = StatusLine(f);
            if (s != p.LastStatus) { p.LastStatus = s; p.Status.text = s; }

            // name tag above the fighter
            bool tagVisible = !f.Dead && f.State != FState.Respawning && m.Fighters.Count > 2 || (!f.Dead && !f.IsCpu);
            p.Tag.enabled = tagVisible && f.State != FState.Respawning;
            if (p.Tag.enabled && f.Rig != null && UIRoot.WorldToCanvas(f.Rig.HeadC + Vector2.up * (0.95f * f.Size), out var local))
            {
                var parent = (RectTransform)p.Tag.transform.parent;
                Vector3 world = UIRoot.Popups.TransformPoint(local);
                p.Tag.transform.position = world;
            }
        }

        static readonly System.Text.StringBuilder sb = new System.Text.StringBuilder(64);
        static string StatusLine(Fighter f)
        {
            sb.Length = 0;
            if (f.Has(StatusType.Burnout)) sb.Append("<color=#888>BURNOUT</color> ");
            if (f.Has(StatusType.Sealed)) sb.Append("<color=#aaa>SEALED</color> ");
            if (f.Has(StatusType.Zone)) sb.Append("<color=#ff3344>ZONE</color> ");
            if (f.Has(StatusType.Jackpot)) sb.Append("<color=#ffd84f>JACKPOT</color> ");
            if (f.Has(StatusType.Executioner)) sb.Append("<color=#ffd36a>EXECUTIONER</color> ");
            if (f.Has(StatusType.PowerUp) || f.Has(StatusType.Overtime)) sb.Append("<color=#ffb04a>PWR</color> ");
            if (f.Has(StatusType.Haste)) sb.Append("<color=#7affd8>SPD</color> ");
            if (f.Has(StatusType.Armor)) sb.Append("<color=#c0c0c0>ARMOR</color> ");
            if (f.Has(StatusType.Burn)) sb.Append("<color=#ff7a2a>BURN</color> ");
            if (f.Has(StatusType.Poison) || f.Has(StatusType.Rot)) sb.Append("<color=#9aff40>TOXIN</color> ");
            if (f.Has(StatusType.Bind) || f.Has(StatusType.Freeze) || f.Has(StatusType.Stun) || f.Has(StatusType.Paralysis)) sb.Append("<color=#7fc8ff>BOUND</color> ");
            if (f.Has(StatusType.Mark)) sb.Append("<color=#ff8a3d>MARKED</color> ");
            if (f.Has(StatusType.Lucky)) sb.Append("<color=#a0ffa0>LUCKY</color> ");
            if (f.ForcedBlackFlashes > 0) sb.Append("<color=#ff1a33>BF x" + f.ForcedBlackFlashes + "</color> ");
            if (f.State == FState.Respawning || (f.Dead && f.Lives > 0)) sb.Append("<color=#fff>RESPAWNING</color>");
            return sb.ToString();
        }
    }
}
