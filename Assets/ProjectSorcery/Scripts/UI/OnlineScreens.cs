using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectSorcery
{
    public static class UIInput
    {
        /// <summary>A text field built in code.</summary>
        public static InputField Field(Transform parent, string label, string value, Vector2 anchor, Vector2 pos, Vector2 size, Action<string> onChange)
        {
            var rt = UIKit.Box(parent, "Field_" + label, anchor, pos, size);
            var bg = UIKit.Img(rt, new Color(0.08f, 0.08f, 0.12f, 0.85f), null, true);
            var l = UIKit.Label(rt, label, 24, UIKit.TextDim, TextAnchor.MiddleLeft);
            l.rectTransform.offsetMin = new Vector2(20f, 0f);
            l.rectTransform.anchorMax = new Vector2(0.4f, 1f);
            var textRt = UIKit.Rect(rt, "Text", new Vector2(0.4f, 0f), new Vector2(1f, 1f), new Vector2(10f, 6f), new Vector2(-16f, -6f));
            var txt = textRt.gameObject.AddComponent<Text>();
            txt.font = Art.Font; txt.fontSize = 28; txt.color = Color.white; txt.alignment = TextAnchor.MiddleLeft; txt.supportRichText = false;
            var phRt = UIKit.Rect(rt, "Placeholder", new Vector2(0.4f, 0f), new Vector2(1f, 1f), new Vector2(10f, 6f), new Vector2(-16f, -6f));
            var ph = phRt.gameObject.AddComponent<Text>();
            ph.font = Art.Font; ph.fontSize = 28; ph.color = new Color(1f, 1f, 1f, 0.3f); ph.alignment = TextAnchor.MiddleLeft; ph.fontStyle = FontStyle.Italic; ph.text = "type here";
            var f = rt.gameObject.AddComponent<InputField>();
            f.textComponent = txt;
            f.placeholder = ph;
            f.targetGraphic = bg;
            f.text = value;
            f.characterLimit = 48;
            f.onValueChanged.AddListener(v => onChange?.Invoke(v));
            return f;
        }
    }

    // =========================================================================== ONLINE (host / join)
    public sealed class OnlineScreen : UIScreen
    {
        Text status;
        protected override void OnBuild()
        {
            Backdrop(0.85f);
            Title("ONLINE", "play with friends over the internet or LAN");
            var a = new Vector2(0f, 1f);
            var name = UIInput.Field(Root, "Your name", Settings.PlayerName, a, new Vector2(560f, -250f), new Vector2(880f, 64f), v => Settings.PlayerName = v);
            nav.Add(name);
            Step("Port", a, new Vector2(560f, -330f), new Vector2(880f, 58f), () => Settings.Port.ToString(), d => Settings.Port = Mathf.Clamp(Settings.Port + d, 1024, 65535));
            Btn("HOST A LOBBY", a, new Vector2(560f, -420f), new Vector2(880f, 70f), Host, 32, UIKit.Accent2);
            var addr = UIInput.Field(Root, "Host address", Settings.LastJoinAddress, a, new Vector2(560f, -530f), new Vector2(880f, 64f), v => Settings.LastJoinAddress = v);
            nav.Add(addr);
            Btn("JOIN", a, new Vector2(560f, -610f), new Vector2(880f, 70f), Join, 32);
            Btn("BACK", a, new Vector2(560f, -700f), new Vector2(880f, 60f), Back, 28, new Color(0.5f, 0.5f, 0.6f));
            status = UIKit.LabelBox(Root, "", 24, new Color(1f, 0.85f, 0.5f), new Vector2(0f, 0f), new Vector2(124f, 80f), new Vector2(1600f, 60f), TextAnchor.MiddleLeft, FontStyle.Bold);
            status.rectTransform.pivot = new Vector2(0f, 0.5f);
            var help = UIKit.LabelBox(Root,
                "<b>HOW TO PLAY ONLINE</b>\n\n" +
                "1. One player HOSTS. Everyone else enters the host's IP and presses JOIN.\n\n" +
                "2. Same house? Use the host's local IP (e.g. 192.168.x.x).\n\n" +
                "3. Over the internet, either forward UDP port " + Settings.Port + " on the host's router, or install a free virtual LAN like <b>Tailscale</b>, <b>ZeroTier</b> or <b>Radmin VPN</b> and use the IP it gives you.\n\n" +
                "4. Allow the game through your firewall when Windows asks.\n\n" +
                "5. Everyone must run the same game version.\n\n" +
                "Controls online: Keyboard P1 keys or Gamepad 1.\nRaise the input delay in Settings if the connection feels choppy.",
                22, UIKit.TextDim, new Vector2(1f, 0.5f), new Vector2(-90f, 30f), new Vector2(640f, 760f), TextAnchor.MiddleLeft);
            help.rectTransform.pivot = new Vector2(1f, 0.5f);
        }

        void Host()
        {
            Settings.Save();
            var s = NetSession.Host(Settings.Port, Settings.PlayerName);
            if (s == null) { status.text = "Could not open port " + Settings.Port + ". Is another program using it?"; return; }
            GameRoot.I.ShowLobby();
        }

        void Join()
        {
            Settings.Save();
            var s = NetSession.Join(Settings.LastJoinAddress, Settings.Port, Settings.PlayerName);
            if (s == null) { status.text = "Invalid address."; return; }
            GameRoot.I.ShowLobby();
        }

        public override void Back() => GameRoot.I.ShowMainMenu();
    }

    // =========================================================================== LOBBY
    public sealed class LobbyScreen : UIScreen
    {
        Text slotsText, status, myInfo;
        Image myPortrait;
        int myChar;
        bool myReady;
        float refresh;

        NetSession S => NetSession.Current;

        protected override void OnBuild()
        {
            Backdrop(0.88f);
            Title("LOBBY", S != null && S.IsHost ? "you are hosting" : "connected to host");
            var a = new Vector2(0f, 1f);
            myChar = 0;
            float y = -230f;
            if (S != null && S.IsHost)
            {
                Step("Mode", a, new Vector2(520f, y), new Vector2(800f, 56f), ModeLabel, ChangeMode); y -= 64f;
                Step("Arena", a, new Vector2(520f, y), new Vector2(800f, 56f), () => S.Lobby.Mode == GameMode.Raid ? "Heian Palace" : Arenas.Get(S.Lobby.Arena).Name,
                    d => { int n = Arenas.SelectableCount; S.Lobby.Arena = ((S.Lobby.Arena + d) % n + n) % n; }); y -= 64f;
                Step("First to", a, new Vector2(520f, y), new Vector2(800f, 56f), () => S.Lobby.RoundsToWin.ToString(), d => S.Lobby.RoundsToWin = Mathf.Clamp(S.Lobby.RoundsToWin + d, 1, 5)); y -= 64f;
                Step("CPU difficulty", a, new Vector2(520f, y), new Vector2(800f, 56f), () => CpuDiff().ToString(), ChangeCpu); y -= 64f;
            }
            Step("Your fighter", a, new Vector2(520f, y), new Vector2(800f, 56f), () => Roster.Get(myChar).Name + " - " + Roster.Get(myChar).Title,
                d => { myChar = NextPlayable(myChar, d); myReady = false; S?.SendPick(myChar, myReady); RefreshMine(); }); y -= 64f;
            Btn("READY / UNREADY", a, new Vector2(520f, y), new Vector2(800f, 62f), () => { myReady = !myReady; S?.SendPick(myChar, myReady); }, 28, UIKit.Accent); y -= 74f;
            if (S != null && S.IsHost) { Btn("START MATCH", a, new Vector2(520f, y), new Vector2(800f, 66f), () => S.HostStart(), 32, UIKit.Accent2); y -= 78f; }
            Btn("LEAVE", a, new Vector2(520f, y), new Vector2(800f, 58f), Back, 28, new Color(0.5f, 0.5f, 0.6f));

            var prt = UIKit.Box(Root, "Mine", new Vector2(1f, 1f), new Vector2(-760f, -230f), new Vector2(200f, 200f), new Vector2(0f, 1f));
            myPortrait = UIKit.Img(prt, Color.white);
            myInfo = UIKit.LabelBox(Root, "", 20, Color.white, new Vector2(1f, 1f), new Vector2(-540f, -230f), new Vector2(500f, 300f), TextAnchor.UpperLeft);
            myInfo.rectTransform.pivot = new Vector2(0f, 1f);
            slotsText = UIKit.LabelBox(Root, "", 26, Color.white, new Vector2(1f, 1f), new Vector2(-760f, -560f), new Vector2(720f, 300f), TextAnchor.UpperLeft);
            slotsText.rectTransform.pivot = new Vector2(0f, 1f);
            status = UIKit.LabelBox(Root, "", 22, new Color(1f, 0.85f, 0.5f), new Vector2(0f, 0f), new Vector2(124f, 60f), new Vector2(1700f, 50f), TextAnchor.MiddleLeft, FontStyle.Bold);
            status.rectTransform.pivot = new Vector2(0f, 0.5f);
            RefreshMine();
            if (S != null) S.OnStart += OnStart;
        }

        static int NextPlayable(int i, int d)
        {
            int n = Roster.Count;
            for (int k = 0; k < n; k++)
            {
                i = ((i + d) % n + n) % n;
                if (!Roster.Get(i).BossOnly) return i;
            }
            return 0;
        }

        Difficulty CpuDiff()
        {
            foreach (var s in S.Lobby.Slots) if (s.Control == SlotControl.Cpu && s.Diff != Difficulty.Nightmare) return s.Diff;
            return Difficulty.Medium;
        }

        void ChangeCpu(int d)
        {
            var diff = (Difficulty)Mathf.Clamp((int)CpuDiff() + d, 1, 3);
            foreach (var s in S.Lobby.Slots) if (s.Control == SlotControl.Cpu && s.Diff != Difficulty.Nightmare) s.Diff = diff;
        }

        string ModeLabel()
        {
            var c = S.Lobby;
            if (c.Mode == GameMode.Survival) return "Survival (co-op)";
            if (c.Mode == GameMode.Raid) return "Calamity Raid (co-op)";
            return c.Layout == TeamLayout.Duel ? "Versus 1v1" : c.Layout == TeamLayout.TwoVsTwo ? "Team 2v2" : c.Layout == TeamLayout.TwoVsOne ? "Handicap 2v1" : "Free-for-all 1v1v1";
        }

        void ChangeMode(int d)
        {
            var c = S.Lobby;
            int cur = c.Mode == GameMode.Survival ? 4 : c.Mode == GameMode.Raid ? 5 : (int)c.Layout;
            cur = ((cur + d) % 6 + 6) % 6;
            if (cur < 4) { c.Mode = GameMode.Versus; c.Layout = (TeamLayout)cur; }
            else if (cur == 4) { c.Mode = GameMode.Survival; c.Layout = TeamLayout.Duel; }
            else { c.Mode = GameMode.Raid; c.Layout = TeamLayout.TwoVsOne; }
            // remove leftover CPUs from co-op modes, then normalize
            if (c.Mode != GameMode.Versus) for (int i = 0; i < c.Slots.Count; i++) if (c.Slots[i].Control == SlotControl.Cpu) c.Slots[i].Control = SlotControl.None;
            NetSession.NormalizeLobby(c);
        }

        void RefreshMine()
        {
            var d = Roster.Get(myChar);
            myPortrait.sprite = Portraits.Get(d);
            myInfo.text = CharInfo.Header(d) + "\n\n" + CharInfo.Kit(d, true);
        }

        void OnStart(MatchConfig cfg)
        {
            var s = S;
            if (s != null) s.OnStart -= OnStart;
            GameRoot.I.StartOnlineMatch(cfg);
        }

        public override void Tick()
        {
            var s = S;
            if (s == null) { status.text = "Disconnected."; return; }
            if (s.EndReason != null) { status.text = s.EndReason; }
            refresh -= Time.unscaledDeltaTime;
            if (refresh > 0f) return;
            refresh = 0.2f;
            var sb = new StringBuilder("<b>PLAYERS</b>\n");
            var c = s.Lobby;
            for (int i = 0; i < c.Slots.Count; i++)
            {
                var sl = c.Slots[i];
                if (sl.Control == SlotControl.None) continue;
                string who = sl.Control == SlotControl.Remote ? sl.Name : (sl.Diff == Difficulty.Nightmare ? "BOSS" : "CPU (" + sl.Diff + ")");
                string ch = sl.Control == SlotControl.Cpu ? (sl.Diff == Difficulty.Nightmare ? Roster.Get(Roster.BossIndex).Name : "random") : Roster.Get(sl.Character).Name;
                string rdy = sl.Control == SlotControl.Remote ? (sl.Ready ? "<color=#7fff9f>READY</color>" : "<color=#ffd36a>...</color>") : "";
                sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(Match.TeamColor(sl.Team))).Append(">■</color> ")
                  .Append(who).Append("  -  ").Append(ch).Append("   ").Append(rdy).Append('\n');
            }
            if (s.IsHost)
            {
                sb.Append("\n<size=20><color=#888899>");
                foreach (var p in s.Peers) if (p.Id != 0) sb.Append(p.Name).Append(": ").Append(Mathf.RoundToInt(p.Ping)).Append(" ms   ");
                sb.Append("</color></size>");
            }
            slotsText.text = sb.ToString();
            if (s.EndReason == null) status.text = s.IsHost ? (s.CanStart(out var why) ? "Everyone is ready. Start when you like." : why) : s.Status;
        }

        public override void Back()
        {
            NetSession.Current?.Close();
            GameRoot.I.ShowOnline();
        }
    }
}
