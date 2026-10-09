using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectSorcery
{
    public abstract class UIScreen
    {
        public RectTransform Root;
        protected readonly List<Selectable> nav = new List<Selectable>();
        public virtual bool Overlay => false;
        protected virtual bool CustomNav => false;

        public void Build(RectTransform parent)
        {
            Root = UIKit.Rect(parent, GetType().Name, Vector2.zero, Vector2.one);
            OnBuild();
            if (nav.Count > 0) { if (!CustomNav) UIKit.ChainVertical(nav); UIKit.Select(nav[0]); }
        }

        protected abstract void OnBuild();
        public virtual void Tick() { }
        public virtual void Back() { }
        public void Destroy() { if (Root != null) UnityEngine.Object.Destroy(Root.gameObject); }

        protected Button Btn(string text, Vector2 anchor, Vector2 pos, Vector2 size, Action a, int font = 30, Color? accent = null)
        {
            var b = UIKit.Button(Root, text, anchor, pos, size, a, font, accent);
            nav.Add(b);
            return b;
        }

        protected Stepper Step(string label, Vector2 anchor, Vector2 pos, Vector2 size, Func<string> value, Action<int> change)
        {
            var s = UIKit.Stepper(Root, label, anchor, pos, size, value, change);
            nav.Add(s.Selectable);
            return s;
        }

        protected Text Title(string text, string sub = null)
        {
            var t = UIKit.LabelBox(Root, text, 72, Color.white, new Vector2(0f, 1f), new Vector2(120f, -90f), new Vector2(1400f, 100f), TextAnchor.MiddleLeft, FontStyle.BoldAndItalic);
            t.rectTransform.pivot = new Vector2(0f, 0.5f);
            UIKit.Outline(t, UIKit.Accent, 3f);
            if (sub != null)
            {
                var s = UIKit.LabelBox(Root, sub, 26, UIKit.TextDim, new Vector2(0f, 1f), new Vector2(124f, -148f), new Vector2(1400f, 40f), TextAnchor.MiddleLeft, FontStyle.Italic);
                s.rectTransform.pivot = new Vector2(0f, 0.5f);
            }
            return t;
        }

        protected void Backdrop(float alpha = 0.78f)
        {
            var bg = UIKit.Fill(Root, "Backdrop", new Color(0.02f, 0.02f, 0.04f, alpha));
            bg.raycastTarget = true;
        }
    }

    // =========================================================================== MAIN MENU
    public sealed class MainMenuScreen : UIScreen
    {
        Text title, sub;
        float t;
        protected override void OnBuild()
        {
            // left gradient over the demo fight
            var shade = UIKit.Rect(Root, "Shade", new Vector2(0f, 0f), new Vector2(0.55f, 1f));
            UIKit.Img(shade, new Color(0.01f, 0.01f, 0.03f, 0.82f));
            var edge = UIKit.Rect(Root, "Edge", new Vector2(0.55f, 0f), new Vector2(0.55f, 1f), new Vector2(0f, 0f), new Vector2(6f, 0f));
            UIKit.Img(edge, UIKit.Accent2);

            title = UIKit.LabelBox(Root, "PROJECT\nSORCERY", 128, Color.white, new Vector2(0f, 1f), new Vector2(120f, -210f), new Vector2(1000f, 300f), TextAnchor.MiddleLeft, FontStyle.BoldAndItalic);
            title.rectTransform.pivot = new Vector2(0f, 0.5f);
            title.lineSpacing = 0.85f;
            UIKit.Outline(title, UIKit.Accent2, 4f);
            UIKit.Shadow(title, Color.black, 10f);
            sub = UIKit.LabelBox(Root, "a stickman cursed-energy arena fighter", 28, UIKit.TextDim, new Vector2(0f, 1f), new Vector2(126f, -380f), new Vector2(900f, 40f), TextAnchor.MiddleLeft, FontStyle.Italic);
            sub.rectTransform.pivot = new Vector2(0f, 0.5f);

            var a = new Vector2(0f, 1f);
            float y = -480f;
            Btn("PLAY", a, new Vector2(400f, y), new Vector2(560f, 72f), () => GameRoot.I.ShowPlay(), 38, UIKit.Accent2); y -= 86f;
            Btn("ONLINE", a, new Vector2(400f, y), new Vector2(560f, 72f), () => GameRoot.I.ShowOnline(), 38); y -= 86f;
            Btn("CHARACTERS", a, new Vector2(400f, y), new Vector2(560f, 72f), () => GameRoot.I.ShowCharacters(), 38); y -= 86f;
            Btn("SETTINGS", a, new Vector2(400f, y), new Vector2(560f, 72f), () => GameRoot.I.ShowSettings(() => GameRoot.I.ShowMainMenu()), 38); y -= 86f;
            Btn("CREDITS", a, new Vector2(400f, y), new Vector2(560f, 72f), () => GameRoot.I.ShowCredits(), 38); y -= 86f;
            Btn("EXIT", a, new Vector2(400f, y), new Vector2(560f, 72f), () => GameRoot.I.Quit(), 38, new Color(0.5f, 0.5f, 0.6f));

            var foot = UIKit.LabelBox(Root, "v" + Application.version + "   |   Fan-made, open-source tribute. Not affiliated with any publisher.   |   " + Roster.Count + " fighters",
                                      18, new Color(1f, 1f, 1f, 0.4f), new Vector2(0f, 0f), new Vector2(120f, 40f), new Vector2(1600f, 30f), TextAnchor.MiddleLeft);
            foot.rectTransform.pivot = new Vector2(0f, 0.5f);
        }

        public override void Tick()
        {
            t += Time.unscaledDeltaTime;
            // cursed title: subtle jitter and color breathing
            title.rectTransform.anchoredPosition = new Vector2(120f + (Mathf.PerlinNoise(t * 9f, 0f) - 0.5f) * 3f, -210f + (Mathf.PerlinNoise(0f, t * 9f) - 0.5f) * 3f);
            title.color = Color.Lerp(Color.white, new Color(1f, 0.85f, 0.9f), Mathf.PingPong(t * 0.6f, 1f));
        }

        public override void Back() { }
    }

    // =========================================================================== PLAY (mode select)
    public sealed class PlayScreen : UIScreen
    {
        protected override void OnBuild()
        {
            Backdrop(0.7f);
            Title("PLAY", "choose a mode");
            var a = new Vector2(0f, 1f);
            float y = -240f;
            void M(string name, string desc, Action act, Color? c = null)
            {
                var b = Btn(name, a, new Vector2(450f, y), new Vector2(660f, 66f), act, 32, c);
                var d = UIKit.LabelBox(Root, desc, 22, UIKit.TextDim, a, new Vector2(800f, y), new Vector2(900f, 60f), TextAnchor.MiddleLeft, FontStyle.Italic);
                d.rectTransform.pivot = new Vector2(0f, 0.5f);
                y -= 78f;
            }
            M("VERSUS CPU", "1 vs 1 against the AI. Pick Easy, Medium or Hard.", () => GameRoot.I.ShowSetup(GameMode.Versus, TeamLayout.Duel, "VERSUS CPU", false), UIKit.Accent2);
            M("LOCAL VERSUS", "1 vs 1 on one machine: keyboard halves or gamepads.", () => GameRoot.I.ShowSetup(GameMode.Versus, TeamLayout.Duel, "LOCAL VERSUS", true));
            M("TEAM BATTLE 2v2", "Two teams of two. Mix players and CPUs freely.", () => GameRoot.I.ShowSetup(GameMode.Versus, TeamLayout.TwoVsTwo, "TEAM BATTLE", false));
            M("HANDICAP 2v1", "Two against one. Good luck to the one.", () => GameRoot.I.ShowSetup(GameMode.Versus, TeamLayout.TwoVsOne, "HANDICAP 2v1", false));
            M("FREE-FOR-ALL 1v1v1", "Three-way brawl - and three-way domain clashes.", () => GameRoot.I.ShowSetup(GameMode.Versus, TeamLayout.FreeForAll, "FREE-FOR-ALL", false));
            M("SURVIVAL", "Endless waves of sorcerers and curses. Solo or co-op.", () => GameRoot.I.ShowSetup(GameMode.Survival, TeamLayout.Duel, "SURVIVAL", false));
            M("CALAMITY RAID", "Team up against the overpowered Heian-era calamity king.", () => GameRoot.I.ShowSetup(GameMode.Raid, TeamLayout.TwoVsOne, "CALAMITY RAID", false), new Color(1f, 0.2f, 0.25f));
            M("TRAINING", "Practice combos, black flash timing and domains on a dummy.", () => GameRoot.I.ShowSetup(GameMode.Training, TeamLayout.Duel, "TRAINING", false));
            M("BACK", "", () => GameRoot.I.ShowMainMenu(), new Color(0.5f, 0.5f, 0.6f));
        }
        public override void Back() => GameRoot.I.ShowMainMenu();
    }

    // =========================================================================== SETUP
    public sealed class SetupScreen : UIScreen
    {
        readonly MatchConfig cfg;
        readonly string title;
        Text warn;
        static readonly string[] opts = { "P1 Keyboard", "P2 Keyboard", "Gamepad 1", "Gamepad 2", "Gamepad 3", "Gamepad 4", "CPU Easy", "CPU Medium", "CPU Hard", "Off" };

        public SetupScreen(MatchConfig c, string title) { cfg = c; this.title = title; }

        public static int OptionOf(SlotConfig s)
        {
            if (s.Control == SlotControl.None) return 9;
            if (s.Control == SlotControl.Cpu) return s.Diff == Difficulty.Easy ? 6 : s.Diff == Difficulty.Hard ? 8 : 7;
            return Mathf.Clamp(s.Device, 0, 5);
        }

        public static void Apply(SlotConfig s, int opt)
        {
            if (opt == 9) { s.Control = SlotControl.None; return; }
            if (opt >= 6) { s.Control = SlotControl.Cpu; s.Diff = opt == 6 ? Difficulty.Easy : opt == 7 ? Difficulty.Medium : Difficulty.Hard; s.Device = -1; return; }
            s.Control = SlotControl.Local; s.Device = opt;
        }

        protected override void OnBuild()
        {
            Backdrop(0.75f);
            Title(title, cfg.Mode == GameMode.Raid ? "up to three sorcerers against one calamity" : "who's fighting?");
            var a = new Vector2(0f, 1f);
            float y = -250f;
            for (int i = 0; i < cfg.Slots.Count; i++)
            {
                var s = cfg.Slots[i];
                int idx = i;
                bool boss = cfg.Mode == GameMode.Raid && s.Team == 1;
                bool dummy = cfg.Mode == GameMode.Training && i == 1;
                if (boss) { var l = UIKit.LabelBox(Root, "BOSS:  The Calamity King (Heian form)", 28, new Color(1f, 0.4f, 0.4f), a, new Vector2(520f, y), new Vector2(800f, 60f), TextAnchor.MiddleLeft, FontStyle.Bold); l.rectTransform.pivot = new Vector2(0f, 0.5f); l.rectTransform.anchoredPosition = new Vector2(124f, y); y -= 70f; continue; }
                string label = dummy ? "Dummy" : (cfg.Mode == GameMode.Versus && cfg.Layout != TeamLayout.Duel ? "Slot " + (i + 1) + "  (" + TeamName(s.Team) + ")" : "Player " + (i + 1));
                if (dummy)
                {
                    Step(label, a, new Vector2(520f, y), new Vector2(800f, 58f), () => s.Diff == Difficulty.Dummy ? (TrainingDummy.Block ? "Dummy (Block)" : "Dummy (Idle)") : "CPU " + s.Diff,
                        d =>
                        {
                            int k = s.Diff == Difficulty.Dummy ? (TrainingDummy.Block ? 1 : 0) : 1 + (int)s.Diff;
                            k = (k + d + 5) % 5;
                            if (k == 0) { s.Diff = Difficulty.Dummy; TrainingDummy.Block = false; }
                            else if (k == 1) { s.Diff = Difficulty.Dummy; TrainingDummy.Block = true; }
                            else s.Diff = (Difficulty)(k - 1);
                            s.Control = SlotControl.Cpu;
                        });
                }
                else
                {
                    bool allowOff = (cfg.Mode == GameMode.Survival || cfg.Mode == GameMode.Raid) && i > 0;
                    Step(label, a, new Vector2(520f, y), new Vector2(800f, 58f), () => opts[OptionOf(s)],
                        d =>
                        {
                            int o = OptionOf(s);
                            int n = allowOff ? 10 : 9;
                            o = ((o + d) % n + n) % n;
                            Apply(s, o);
                            ValidateWarn();
                        });
                }
                y -= 70f;
            }
            y -= 14f;
            if (cfg.Mode == GameMode.Versus)
            {
                Step("First to", a, new Vector2(520f, y), new Vector2(800f, 58f), () => cfg.RoundsToWin + (cfg.RoundsToWin == 1 ? " round" : " rounds"),
                    d => { int[] v = { 1, 2, 3, 5 }; int i = Array.IndexOf(v, cfg.RoundsToWin); i = (i + d + v.Length) % v.Length; cfg.RoundsToWin = v[i]; });
                y -= 70f;
                Step("Round time", a, new Vector2(520f, y), new Vector2(800f, 58f), () => cfg.RoundTime <= 0f ? "Unlimited" : cfg.RoundTime + "s",
                    d => { float[] v = { 60f, 99f, 150f, -1f }; int i = Array.IndexOf(v, cfg.RoundTime); if (i < 0) i = 1; i = (i + d + v.Length) % v.Length; cfg.RoundTime = v[i]; });
                y -= 70f;
            }
            if (cfg.Mode == GameMode.Training)
            {
                Step("Infinite cursed energy", a, new Vector2(520f, y), new Vector2(800f, 58f), () => cfg.InfiniteCe ? "On" : "Off", d => cfg.InfiniteCe = !cfg.InfiniteCe);
                y -= 70f;
            }
            if (cfg.Mode != GameMode.Raid)
            {
                Step("Arena", a, new Vector2(520f, y), new Vector2(800f, 58f), () => cfg.Arena < 0 ? "Random" : Arenas.Get(cfg.Arena).Name,
                    d => { int n = Arenas.SelectableCount + 1; int i = cfg.Arena + 1; i = ((i + d) % n + n) % n; cfg.Arena = i - 1; });
                y -= 70f;
            }
            y -= 20f;
            Btn("CHOOSE FIGHTERS", a, new Vector2(520f, y), new Vector2(800f, 70f), () => { if (Validate()) GameRoot.I.ShowCharSelect(cfg); }, 34, UIKit.Accent2);
            y -= 84f;
            Btn("BACK", a, new Vector2(520f, y), new Vector2(800f, 60f), Back, 28, new Color(0.5f, 0.5f, 0.6f));
            warn = UIKit.LabelBox(Root, "", 24, new Color(1f, 0.45f, 0.45f), new Vector2(0f, 0f), new Vector2(124f, 70f), new Vector2(1600f, 40f), TextAnchor.MiddleLeft, FontStyle.Bold);
            warn.rectTransform.pivot = new Vector2(0f, 0.5f);

            var help = UIKit.LabelBox(Root, ControlsHelp(), 20, UIKit.TextDim, new Vector2(1f, 0.5f), new Vector2(-90f, 0f), new Vector2(560f, 700f), TextAnchor.MiddleLeft);
            help.rectTransform.pivot = new Vector2(1f, 0.5f);
            ValidateWarn();
        }

        public static string ControlsHelp()
        {
            return "<b><color=#ffffff>CONTROLS</color></b>\n\n<b>Keyboard P1</b>\n" + InputHub.P1.Description.Replace("  |  ", "\n") +
                   "\n\n<b>Keyboard P2</b>\n" + InputHub.P2.Description.Replace("  |  ", "\n") +
                   "\n\n<b>Gamepad</b>\n" + InputHub.GamepadDescription.Replace("  |  ", "\n") +
                   "\n\n<b>Everyone</b>\nDown + Light = sweep, Down + Heavy = launcher\nHold Block + Down = Reverse Technique / Simple Domain\nBlock + Dash while juggled = Burst\nHold a skill button = chant (if chantable)\nLight hit, then Heavy in rhythm = BLACK FLASH";
        }

        static string TeamName(int t) => t == 0 ? "Azure" : t == 1 ? "Crimson" : "Jade";

        void ValidateWarn() { Validate(); }

        bool Validate()
        {
            var used = new HashSet<int>();
            int humans = 0;
            foreach (var s in cfg.Slots)
            {
                if (s.Control != SlotControl.Local) continue;
                humans++;
                if (!used.Add(s.Device)) { warn.text = "Two players can't share the same input device."; return false; }
                if (s.Device >= 2 && !InputHub.Get(s.Device).Connected) { warn.text = InputHub.DeviceName(s.Device) + " is not connected."; return false; }
            }
            if (humans == 0 && cfg.Mode != GameMode.Versus) { warn.text = "This mode needs at least one player."; return false; }
            warn.text = "";
            return true;
        }

        public override void Back() => GameRoot.I.ShowPlay();
    }

    // =========================================================================== CHARACTER INFO HELPERS
    public static class CharInfo
    {
        public static string Passives(CharacterDef d)
        {
            var sb = new StringBuilder();
            void P(Passive p, string text) { if (d.Has(p)) sb.Append("<color=#ffd36a>◆</color> ").Append(text).Append('\n'); }
            P(Passive.Infinity, "Boundless: most hits stop before touching you (drains CE).");
            P(Passive.SixEyes, "Prism Sight: techniques cost 40% less, faster domain recovery.");
            P(Passive.AutoRCT, "Regenerates automatically with reverse cursed energy.");
            P(Passive.RCT, "Reverse Technique: hold Block + Down to heal.");
            P(Passive.BlackFlashAffinity, "Black Flash affinity: wider timing window.");
            P(Passive.HeavenlyRestriction, "Heavenly Restriction: no cursed energy, superhuman body, invisible to sure-hit.");
            P(Passive.Restricted, "Reverse restriction: huge cursed energy, fragile body.");
            P(Passive.Adaptation, "Adapts to any damage type that keeps hitting it.");
            P(Passive.SoulResist, "Soul-resistant: soul attacks barely hurt.");
            P(Passive.SimpleDomainMaster, "Simple domain master: fully negates sure-hit.");
            P(Passive.CurseBody, "Cursed spirit: mends itself with cursed energy.");
            P(Passive.PerfectBody, "Reshapes its body: only soul attacks truly hurt.");
            P(Passive.ToughBody, "Super armor during heavy attacks.");
            P(Passive.Flight, "Hold Jump to glide.");
            P(Passive.LuckStored, "Survives one lethal hit per round.");
            P(Passive.Support, "Slowly heals nearby allies.");
            P(Passive.RotBlood, "Attacks inflict rot.");
            P(Passive.WeaknessInverted, "Strong hits against him are weakened; weak ones strengthened.");
            return sb.ToString();
        }

        public static string Kit(CharacterDef d, bool compact = false)
        {
            var sb = new StringBuilder();
            var kit = d.Kit != null ? d.Kit() : null;
            string[] keys = { "S1", "S2", "S3", "ULT" };
            if (kit == null) return "";
            for (int i = 0; i < kit.Length && i < 4; i++)
            {
                var ab = kit[i];
                if (ab == null) continue;
                string col = ColorUtility.ToHtmlStringRGB(ab.Color);
                string name = ab is DomainAb da ? "Domain: " + da.Domain.Name : ab.Name;
                sb.Append("<color=#").Append(col).Append("><b>").Append(keys[i]).Append("  ").Append(name).Append("</b></color>");
                if (!d.NoCE && !ab.Physical) sb.Append("  <color=#8888aa>").Append(Mathf.RoundToInt(ab.Cost)).Append(" CE</color>");
                if (ab.Chantable) sb.Append("  <color=#c8a0ff>[hold to chant]</color>");
                sb.Append('\n');
                if (!compact) sb.Append("<color=#c0c0d0>").Append(ab is DomainAb ? ab.Summary() : ab.Desc).Append("</color>\n");
            }
            return sb.ToString();
        }

        public static string Header(CharacterDef d)
            => "<size=54><b>" + d.Name.ToUpperInvariant() + "</b></size>\n<color=#" + ColorUtility.ToHtmlStringRGB(d.Look.Aura) + ">" + d.Title + "</color>   <color=#888899>" + d.Technique + "  -  " + EraName(d.Era) + "</color>";

        public static string EraName(Era e) => e == Era.Anime ? "Season era" : e == Era.Manga ? "Manga era" : e == Era.Modulo ? "Modulo era" : "Raid boss";
    }

    // =========================================================================== CREDITS
    public sealed class CreditsScreen : UIScreen
    {
        protected override void OnBuild()
        {
            Backdrop(0.85f);
            Title("CREDITS");
            var t = UIKit.LabelBox(Root,
                "<b>PROJECT SORCERY</b>\nCreated by <b>jajaa17</b>\n\nAn open-source fan tribute - stickman arena fighting inspired by a certain cursed-energy manga & anime.\n" +
                "All character names, technique names and art in this game are original. No copyrighted assets are used.\n\n" +
                "Every sound effect and music track is synthesized in code at runtime.\nEvery visual - fighters, arenas, domains, portraits - is drawn procedurally.\n\n" +
                "Built with Unity. Licensed under the MIT License - fork it, mod it, learn from it, build your own game with it.\n\n" +
                "Development assisted by Claude (Anthropic).",
                28, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(1500f, 700f), TextAnchor.MiddleCenter);
            Btn("BACK", new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(400f, 64f), Back, 30, new Color(0.5f, 0.5f, 0.6f));
        }
        public override void Back() => GameRoot.I.ShowMainMenu();
    }

    // =========================================================================== SETTINGS
    public sealed class SettingsScreen : UIScreen
    {
        readonly Action onBack;
        int tab;
        public override bool Overlay => true;
        public SettingsScreen(Action back, int tab = 0) { onBack = back; this.tab = tab; }

        static string Pct(float v) => Mathf.RoundToInt(v * 100f) + "%";

        protected override void OnBuild()
        {
            Backdrop(0.88f);
            Title("SETTINGS");
            string[] tabs = { "AUDIO", "GRAPHICS", "GAMEPLAY", "CONTROLS" };
            for (int i = 0; i < tabs.Length; i++)
            {
                int k = i;
                var b = UIKit.Button(Root, tabs[i], new Vector2(0f, 1f), new Vector2(260f + i * 290f, -200f), new Vector2(270f, 56f), () => GameRoot.I.ShowSettings(onBack, k), 26, i == tab ? UIKit.Accent2 : UIKit.Accent);
                nav.Add(b);
            }
            var a = new Vector2(0f, 1f);
            float y = -300f;
            Vector2 size = new Vector2(900f, 58f);
            float x = 580f;
            switch (tab)
            {
                case 0:
                    Step("Master volume", a, new Vector2(x, y), size, () => Pct(Settings.Master), d => Settings.Master = Mathf.Clamp01(Settings.Master + d * 0.1f)); y -= 68f;
                    Step("Music volume", a, new Vector2(x, y), size, () => Pct(Settings.Music), d => Settings.Music = Mathf.Clamp01(Settings.Music + d * 0.1f)); y -= 68f;
                    Step("Effects volume", a, new Vector2(x, y), size, () => Pct(Settings.Sfx), d => { Settings.Sfx = Mathf.Clamp01(Settings.Sfx + d * 0.1f); Audio.Play(Sfx.PunchHeavy, Vector2.zero, 0.8f); }); y -= 68f;
                    break;
                case 1:
                    Step("Window mode", a, new Vector2(x, y), size, () => Settings.WindowMode == 0 ? "Windowed" : Settings.WindowMode == 1 ? "Borderless" : "Fullscreen",
                        d => { Settings.WindowMode = (Settings.WindowMode + d + 3) % 3; Settings.ApplyGraphics(); }); y -= 68f;
                    Step("Resolution", a, new Vector2(x, y), size, Settings.ResolutionLabel,
                        d => { int n = Screen.resolutions.Length; Settings.ResolutionIndex = Mathf.Clamp(Settings.ResolutionIndex + d, -1, n - 1); Settings.ApplyGraphics(); }); y -= 68f;
                    Step("V-Sync", a, new Vector2(x, y), size, () => Settings.VSync ? "On" : "Off", d => { Settings.VSync = !Settings.VSync; Settings.ApplyGraphics(); }); y -= 68f;
                    Step("Frame rate cap", a, new Vector2(x, y), size, () => Settings.VSync ? "V-Sync" : (Settings.FpsCaps[Settings.FpsCapIndex] < 0 ? "Unlimited" : Settings.FpsCaps[Settings.FpsCapIndex] + " fps"),
                        d => { Settings.FpsCapIndex = (Settings.FpsCapIndex + d + Settings.FpsCaps.Length) % Settings.FpsCaps.Length; Settings.ApplyGraphics(); }); y -= 68f;
                    Step("VFX quality", a, new Vector2(x, y), size, () => Settings.VfxQuality == 0 ? "Low" : Settings.VfxQuality == 1 ? "Medium" : "High", d => Settings.VfxQuality = (Settings.VfxQuality + d + 3) % 3); y -= 68f;
                    break;
                case 2:
                    Step("Screen shake", a, new Vector2(x, y), size, () => Settings.Shake <= 0f ? "Off" : Settings.Shake < 0.75f ? "Low" : Settings.Shake < 1.25f ? "Normal" : "Intense",
                        d => { float[] v = { 0f, 0.5f, 1f, 1.5f }; int i = Mathf.Clamp(Mathf.RoundToInt(Settings.Shake * 2f), 0, 3); i = (i + d + 4) % 4; Settings.Shake = v[i]; }); y -= 68f;
                    Step("Impact frame flashes", a, new Vector2(x, y), size, () => Settings.ImpactFlashes ? "Full (anime strobe)" : "Reduced (photosensitive)", d => Settings.ImpactFlashes = !Settings.ImpactFlashes); y -= 68f;
                    Step("Damage numbers", a, new Vector2(x, y), size, () => Settings.DamageNumbers ? "On" : "Off", d => Settings.DamageNumbers = !Settings.DamageNumbers); y -= 68f;
                    Step("Online input delay", a, new Vector2(x, y), size, () => Settings.InputDelay + " frames", d => Settings.InputDelay = Mathf.Clamp(Settings.InputDelay + d, 1, 8)); y -= 68f;
                    break;
                case 3:
                {
                    var t = UIKit.LabelBox(Root, SetupScreen.ControlsHelp(), 24, Color.white, a, new Vector2(124f, -320f), new Vector2(1500f, 600f), TextAnchor.UpperLeft);
                    t.rectTransform.pivot = new Vector2(0f, 1f);
                    break;
                }
            }
            Btn("BACK", new Vector2(0f, 0f), new Vector2(420f, 110f), new Vector2(560f, 64f), Back, 30, new Color(0.5f, 0.5f, 0.6f));
        }

        public override void Back()
        {
            Settings.Save();
            onBack?.Invoke();
        }
    }

    // =========================================================================== CHARACTERS (gallery + move list)
    public sealed class CharactersScreen : UIScreen
    {
        int cursor;
        Text info, passives, kit;
        Image big;
        readonly List<Image> frames = new List<Image>();
        const int Cols = 12;

        protected override void OnBuild()
        {
            Backdrop(0.9f);
            Title("CHARACTERS", Roster.Count - 1 + " fighters  -  arrows / stick to browse, Back to return");
            var grid = UIKit.Box(Root, "Grid", new Vector2(0f, 1f), new Vector2(120f, -200f), new Vector2(Cols * 70f, 700f), new Vector2(0f, 1f));
            int k = 0;
            for (int i = 0; i < Roster.Count; i++)
            {
                var d = Roster.Get(i);
                if (d.BossOnly) continue;
                int idx = i;
                var cell = UIKit.Box(grid, "Cell", new Vector2(0f, 1f), new Vector2((k % Cols) * 70f, -(k / Cols) * 70f), new Vector2(64f, 64f), new Vector2(0f, 1f));
                var frame = UIKit.Img(cell, new Color(1f, 1f, 1f, 0.15f), null, true);
                frames.Add(frame);
                UIKit.Img(UIKit.Rect(cell, "P", Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f)), Color.white, Portraits.Get(d));
                var btn = cell.gameObject.AddComponent<Button>();
                btn.targetGraphic = frame;
                btn.navigation = new Navigation { mode = Navigation.Mode.None };
                int kk = k;
                btn.onClick.AddListener(() => { cursor = kk; Refresh(); });
                k++;
            }
            var bigRt = UIKit.Box(Root, "Big", new Vector2(1f, 1f), new Vector2(-760f, -200f), new Vector2(260f, 260f), new Vector2(0f, 1f));
            big = UIKit.Img(bigRt, Color.white);
            info = UIKit.LabelBox(Root, "", 26, Color.white, new Vector2(1f, 1f), new Vector2(-480f, -200f), new Vector2(460f, 260f), TextAnchor.UpperLeft);
            info.rectTransform.pivot = new Vector2(0f, 1f);
            passives = UIKit.LabelBox(Root, "", 22, Color.white, new Vector2(1f, 1f), new Vector2(-760f, -480f), new Vector2(700f, 140f), TextAnchor.UpperLeft);
            passives.rectTransform.pivot = new Vector2(0f, 1f);
            kit = UIKit.LabelBox(Root, "", 22, Color.white, new Vector2(1f, 1f), new Vector2(-760f, -610f), new Vector2(700f, 420f), TextAnchor.UpperLeft);
            kit.rectTransform.pivot = new Vector2(0f, 1f);
            Refresh();
        }

        CharacterDef At(int k)
        {
            int n = 0;
            for (int i = 0; i < Roster.Count; i++) { var d = Roster.Get(i); if (d.BossOnly) continue; if (n == k) return d; n++; }
            return Roster.Get(0);
        }

        void Refresh()
        {
            var d = At(cursor);
            big.sprite = Portraits.Get(d);
            info.text = CharInfo.Header(d) + "\n\n<size=20><color=#c8c8d8>" + d.Bio + "</color></size>";
            passives.text = CharInfo.Passives(d);
            kit.text = CharInfo.Kit(d);
            for (int i = 0; i < frames.Count; i++) frames[i].color = i == cursor ? d.Look.Aura : new Color(1f, 1f, 1f, 0.15f);
        }

        public override void Tick()
        {
            int n = frames.Count;
            int c = cursor;
            if (InputHub.AnyLeft) c--;
            if (InputHub.AnyRight) c++;
            if (InputHub.AnyUp) c -= Cols;
            if (InputHub.AnyDown) c += Cols;
            c = Mathf.Clamp(c, 0, n - 1);
            if (c != cursor) { cursor = c; Refresh(); Audio.Ui(Sfx.UiMove); }
            if (InputHub.AnyBack || InputHub.AnyStart) Back();
        }

        public override void Back() => GameRoot.I.ShowMainMenu();
    }

    // =========================================================================== PAUSE
    public sealed class PauseScreen : UIScreen
    {
        public override bool Overlay => true;
        protected override void OnBuild()
        {
            Backdrop(0.65f);
            var m = Match.I;
            Title("PAUSED", m != null && m.Config.Online ? "online match keeps running" : null);
            var a = new Vector2(0f, 1f);
            float y = -260f;
            Btn("RESUME", a, new Vector2(400f, y), new Vector2(520f, 66f), Back, 32, UIKit.Accent2); y -= 80f;
            if (m != null && m.Config.Mode == GameMode.Training)
            {
                Btn("DUMMY: " + (TrainingDummy.Block ? "BLOCK" : "IDLE"), a, new Vector2(400f, y), new Vector2(520f, 66f), () => { TrainingDummy.Block = !TrainingDummy.Block; GameRoot.I.ShowPause(); }, 28); y -= 80f;
            }
            Btn("SETTINGS", a, new Vector2(400f, y), new Vector2(520f, 66f), () => GameRoot.I.ShowSettings(() => GameRoot.I.ShowPause()), 32); y -= 80f;
            Btn("QUIT TO MENU", a, new Vector2(400f, y), new Vector2(520f, 66f), () => GameRoot.I.LeaveMatch(), 32, new Color(0.6f, 0.3f, 0.35f)); y -= 80f;

            // move list for the first local player
            var f = m != null ? m.FirstHuman() : null;
            if (f != null)
            {
                var t = UIKit.LabelBox(Root, CharInfo.Header(f.Def) + "\n\n" + CharInfo.Passives(f.Def) + "\n" + CharInfo.Kit(f.Def), 22, Color.white,
                                       new Vector2(1f, 1f), new Vector2(-90f, -220f), new Vector2(820f, 800f), TextAnchor.UpperLeft);
                t.rectTransform.pivot = new Vector2(1f, 1f);
            }
        }
        public override void Back() => GameRoot.I.ResumeMatch();
    }

    // =========================================================================== RESULTS
    public sealed class ResultsScreen : UIScreen
    {
        readonly MatchResult r;
        public override bool Overlay => true;
        protected override bool CustomNav => true;
        public ResultsScreen(MatchResult result) { r = result; }

        protected override void OnBuild()
        {
            Backdrop(0.72f);
            var t = UIKit.LabelBox(Root, r.Title, 110, r.Color, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(1800f, 160f), TextAnchor.MiddleCenter, FontStyle.BoldAndItalic);
            UIKit.Outline(t, Color.black, 5f);
            UIKit.LabelBox(Root, r.Subtitle, 30, UIKit.TextDim, new Vector2(0.5f, 1f), new Vector2(0f, -240f), new Vector2(1600f, 50f), TextAnchor.MiddleCenter, FontStyle.Italic);
            var sb = new StringBuilder();
            sb.Append("<b><color=#888899>FIGHTER                          DAMAGE     MAX COMBO     BLACK FLASH     DOMAINS     K.O.</color></b>\n\n");
            foreach (var f in r.Ranking)
            {
                string c = ColorUtility.ToHtmlStringRGB(f.TeamColor);
                string name = (f.Tag + "  " + f.Def.Name).PadRight(30);
                sb.Append("<color=#").Append(c).Append(">").Append(name).Append("</color>")
                  .Append(Mathf.RoundToInt(f.StatDamage).ToString().PadLeft(8))
                  .Append(f.StatMaxCombo.ToString().PadLeft(12))
                  .Append(f.StatBlackFlashes.ToString().PadLeft(15))
                  .Append(f.StatDomainWins.ToString().PadLeft(14))
                  .Append(f.StatKOs.ToString().PadLeft(9)).Append('\n');
            }
            var table = UIKit.LabelBox(Root, sb.ToString(), 26, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(1500f, 420f), TextAnchor.UpperCenter);
            float x = -440f;
            var cfg = GameRoot.I.LastConfig;
            if (cfg != null && !cfg.Online)
            {
                Btn("REMATCH", new Vector2(0.5f, 0f), new Vector2(x, 120f), new Vector2(400f, 66f), () => GameRoot.I.Rematch(), 30, UIKit.Accent2); x += 440f;
                Btn("CHANGE FIGHTERS", new Vector2(0.5f, 0f), new Vector2(x, 120f), new Vector2(400f, 66f), () => GameRoot.I.ShowCharSelect(cfg), 30); x += 440f;
            }
            Btn("MAIN MENU", new Vector2(0.5f, 0f), new Vector2(x, 120f), new Vector2(400f, 66f), () => GameRoot.I.LeaveMatch(), 30, new Color(0.5f, 0.5f, 0.6f));
            // horizontal navigation between the result buttons
            for (int i = 0; i < nav.Count; i++)
            {
                var n = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = nav[(i - 1 + nav.Count) % nav.Count], selectOnRight = nav[(i + 1) % nav.Count] };
                nav[i].navigation = n;
            }
        }
    }
}
