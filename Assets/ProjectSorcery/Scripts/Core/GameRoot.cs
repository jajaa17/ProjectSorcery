using UnityEngine;

namespace ProjectSorcery
{
    /// <summary>
    /// Boots every system, owns the current match and screen, and routes between menus and matches.
    /// The whole game is built in code, so it runs from any (even empty) scene.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class GameRoot : MonoBehaviour
    {
        public static GameRoot I;

        Match match;
        HUD hud;
        UIScreen screen;
        public MatchConfig LastConfig { get; private set; }
        public bool Paused => match != null && match.Paused;
        bool demo;
        float resultsDelay;
        readonly System.Random rnd = new System.Random();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            var go = new GameObject("ProjectSorcery");
            go.AddComponent<GameRoot>();
        }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
            Application.runInBackground = true;   // keep online matches alive when the window loses focus

            Settings.Load();
            Settings.ApplyGraphics();
            Art.Init();
            InputHub.Init();
            CameraRig.Create(transform);
            Audio.Init(transform);
            VFX.Create(transform);
            ImpactFrames.Init(transform);
            Views.Init(transform);
            DomainFX.Init(transform);
            UIRoot.Init(transform);
            var _ = Roster.Count;   // build the roster once up front

            UIRoot.SetFade(1f);
            UIRoot.FadeTo(0f);
            ShowMainMenu();
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            InputHub.Update(dt);
            NetSession.Current?.Update(dt);
            Audio.Update(dt);
            UIRoot.Update();
            Popups.Update();
            CutIns.Update();
            hud?.Update();
            if (!UIRoot.Typing) screen?.Tick();

            if (match != null && !demo)
            {
                // pause (Esc / Start on any device)
                if (screen == null && !match.Ended && (InputHub.AnyStart || KeyInput.Down(KeyCode.Escape))) ShowPause();
                else if (screen != null && screen is PauseScreen && (InputHub.AnyStart || InputHub.AnyBack)) ResumeMatch();
                else if (screen != null && !(screen is PauseScreen) && screen.Overlay && InputHub.AnyBack) screen.Back();

                if (match.Ended && screen == null)
                {
                    resultsDelay -= dt;
                    if (resultsDelay <= 0f && match.Mode.Result != null) Show(new ResultsScreen(match.Mode.Result));
                }
                else if (!match.Ended) resultsDelay = 0.6f;
            }
            else if (screen != null && !UIRoot.Typing && InputHub.AnyBack && !(screen is CharSelectScreen) && !(screen is CharactersScreen) && !(screen is MainMenuScreen))
            {
                screen.Back();
            }

            // the attract-mode fight behind the menu restarts itself
            if (demo && match != null && match.Ended) StartDemo();
        }

        // ============================================================ screens
        void Show(UIScreen s)
        {
            screen?.Destroy();
            screen = s;
            if (s != null) s.Build(UIRoot.Screens);
        }

        void CloseScreen() { screen?.Destroy(); screen = null; }

        public void ShowMainMenu()
        {
            NetSession.Current?.Close();
            if (match == null || !demo) StartDemo();
            Show(new MainMenuScreen());
        }

        public void ShowPlay() => Show(new PlayScreen());
        public void ShowCredits() => Show(new CreditsScreen());
        public void ShowCharacters() => Show(new CharactersScreen());
        public void ShowOnline() => Show(new OnlineScreen());
        public void ShowLobby() => Show(new LobbyScreen());
        public void ShowSettings(System.Action back, int tab = 0) => Show(new SettingsScreen(back, tab));

        public void ShowSetup(GameMode mode, TeamLayout layout, string title, bool localVersus)
        {
            var cfg = new MatchConfig { Mode = mode, Layout = layout, Arena = -1, RoundsToWin = 2, RoundTime = 99f };
            int n;
            switch (mode)
            {
                case GameMode.Survival: n = 2; break;
                case GameMode.Raid: n = 4; break;
                case GameMode.Training: n = 2; break;
                default: n = MatchConfig.SlotCount(layout); break;
            }
            int firstPad = FirstConnectedPad();
            for (int i = 0; i < n; i++)
            {
                var s = new SlotConfig { Team = mode == GameMode.Versus ? MatchConfig.TeamFor(layout, i) : (mode == GameMode.Raid && i == 3 ? 1 : (mode == GameMode.Training ? i : 0)) };
                if (i == 0) { s.Control = SlotControl.Local; s.Device = 0; }
                else if (mode == GameMode.Raid && i == 3) { s.Control = SlotControl.Cpu; s.Diff = Difficulty.Nightmare; s.Character = Roster.BossIndex; }
                else if (mode == GameMode.Training) { s.Control = SlotControl.Cpu; s.Diff = Difficulty.Dummy; }
                else if (mode == GameMode.Survival || mode == GameMode.Raid) { s.Control = i == 1 && mode == GameMode.Raid ? SlotControl.Cpu : SlotControl.None; s.Diff = Difficulty.Hard; }
                else if (localVersus && i == 1) { s.Control = SlotControl.Local; s.Device = firstPad >= 0 ? firstPad : 1; }
                else { s.Control = SlotControl.Cpu; s.Diff = Difficulty.Medium; }
                cfg.Slots.Add(s);
            }
            setupTitle = title;
            Show(new SetupScreen(cfg, title));
        }

        string setupTitle = "SETUP";
        public void ShowSetupFor(MatchConfig cfg) => Show(new SetupScreen(cfg, setupTitle));

        static int FirstConnectedPad()
        {
            for (int i = 2; i < 6; i++) if (InputHub.Get(i).Connected) return i;
            return -1;
        }

        public void ShowCharSelect(MatchConfig cfg)
        {
            if (match != null && !demo) { DestroyMatch(); StartDemo(); }
            Show(new CharSelectScreen(cfg));
        }

        public void ShowPause()
        {
            if (match == null) return;
            if (!match.Config.Online) match.Paused = true;
            Show(new PauseScreen());
            Audio.Ui(Sfx.UiBack);
        }

        public void ResumeMatch()
        {
            if (match == null) return;
            match.Paused = false;
            Settings.Save();
            CloseScreen();
        }

        // ============================================================ matches
        void StartDemo()
        {
            DestroyMatch();
            var cfg = new MatchConfig { Mode = GameMode.Versus, Layout = TeamLayout.Duel, Demo = true, RoundsToWin = 1, RoundTime = 45f, Arena = rnd.Next(Arenas.SelectableCount), Seed = (uint)rnd.Next(1, int.MaxValue) };
            cfg.Slots.Add(new SlotConfig { Control = SlotControl.Cpu, Diff = Difficulty.Hard, Character = Roster.RandomPlayableIndex(rnd), Team = 0 });
            cfg.Slots.Add(new SlotConfig { Control = SlotControl.Cpu, Diff = Difficulty.Hard, Character = Roster.RandomPlayableIndex(rnd), Team = 1 });
            demo = true;
            match = Match.Create(cfg, new LocalDriver());
            hud = new HUD(match);
            Audio.PlayMusic(Track.Menu);
        }

        public void StartMatch(MatchConfig cfg)
        {
            CloseScreen();
            DestroyMatch();
            LastConfig = cfg;
            demo = false;
            VFX.Clear(); Popups.Clear();
            UIRoot.SetFade(1f); UIRoot.FadeTo(0f);
            match = Match.Create(cfg, new LocalDriver());
            hud = new HUD(match);
            Audio.PlayMusic(cfg.Mode == GameMode.Raid ? Track.Raid : Track.Battle);
        }

        public void StartOnlineMatch(MatchConfig cfg)
        {
            var s = NetSession.Current;
            if (s == null) return;
            CloseScreen();
            DestroyMatch();
            LastConfig = cfg;
            demo = false;
            VFX.Clear(); Popups.Clear();
            UIRoot.SetFade(1f); UIRoot.FadeTo(0f);
            match = Match.Create(cfg, new NetDriver(s));
            hud = new HUD(match);
            Audio.PlayMusic(cfg.Mode == GameMode.Raid ? Track.Raid : Track.Battle);
        }

        public void Rematch()
        {
            if (LastConfig == null) { ShowMainMenu(); return; }
            var cfg = LastConfig.Copy();
            cfg.Seed = (uint)rnd.Next(1, int.MaxValue);
            StartMatch(cfg);
        }

        public void LeaveMatch()
        {
            NetSession.Current?.Close();
            DestroyMatch();
            StartDemo();
            Show(new MainMenuScreen());
        }

        public void NetFailed(string reason)
        {
            if (match == null || demo) return;
            Debug.Log("[Net] " + reason);
            NetSession.Current?.Close();
            DestroyMatch();
            StartDemo();
            Show(new MainMenuScreen());
            Popups.World(Vector2.up * 4f, reason ?? "Connection lost.", new Color(1f, 0.5f, 0.5f), 0.8f);
        }

        void DestroyMatch()
        {
            hud?.Destroy();
            hud = null;
            if (match != null)
            {
                match.ClearEntities();
                Destroy(match.gameObject);
                match = null;
            }
            DomainFX.HideAll();
            ImpactFrames.Stop();
            CutIns.Hide();
            Popups.Clear();
        }

        public void Quit()
        {
            Settings.Save();
            NetSession.Current?.Close();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void OnApplicationQuit()
        {
            Settings.Save();
            NetSession.Current?.Close();
        }
    }
}
