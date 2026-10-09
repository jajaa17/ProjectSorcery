using UnityEngine;

namespace ProjectSorcery
{
    /// <summary>Player-facing settings, persisted with PlayerPrefs.</summary>
    public static class Settings
    {
        // Audio
        public static float Master = 0.85f, Music = 0.55f, Sfx = 0.9f;
        // Feel / accessibility
        public static float Shake = 1f;              // 0..1.5
        public static bool ImpactFlashes = true;     // strobing impact frames (turn off for photosensitivity)
        public static bool DamageNumbers = true;
        public static int AnimFpsIndex = 1;          // see AnimFpsSteps: how often a fighter's drawn pose updates
        public static readonly int[] AnimFpsSteps = { 0, 24, 12 };   // 0 = smooth (every render frame), 24 = anime, 12 = on twos
        public static int AnimFps => AnimFpsSteps[AnimFpsIndex];
        // Graphics
        public static int VfxQuality = 2;            // 0 low, 1 medium, 2 high
        public static int WindowMode = 1;            // 0 windowed, 1 borderless, 2 exclusive
        public static int ResolutionIndex = -1;      // index into Screen.resolutions, -1 = native
        public static bool VSync = true;
        public static int FpsCapIndex = 1;           // see FpsCaps
        public static readonly int[] FpsCaps = { 30, 60, 120, 144, 240, -1 };
        // Online
        public static string PlayerName = "Sorcerer";
        public static string LastJoinAddress = "127.0.0.1";
        public static int Port = 7777;
        public static int InputDelay = 3;

        public static float VfxMul => VfxQuality == 0 ? 0.35f : VfxQuality == 1 ? 0.7f : 1f;

        public static void Load()
        {
            Master = PlayerPrefs.GetFloat("ps_master", Master);
            Music = PlayerPrefs.GetFloat("ps_music", Music);
            Sfx = PlayerPrefs.GetFloat("ps_sfx", Sfx);
            Shake = PlayerPrefs.GetFloat("ps_shake", Shake);
            ImpactFlashes = PlayerPrefs.GetInt("ps_flash", ImpactFlashes ? 1 : 0) == 1;
            DamageNumbers = PlayerPrefs.GetInt("ps_dmgnum", DamageNumbers ? 1 : 0) == 1;
            AnimFpsIndex = PlayerPrefs.GetInt("ps_animfps", AnimFpsIndex);
            VfxQuality = PlayerPrefs.GetInt("ps_vfx", VfxQuality);
            WindowMode = PlayerPrefs.GetInt("ps_window", WindowMode);
            ResolutionIndex = PlayerPrefs.GetInt("ps_res", ResolutionIndex);
            VSync = PlayerPrefs.GetInt("ps_vsync", VSync ? 1 : 0) == 1;
            FpsCapIndex = PlayerPrefs.GetInt("ps_fps", FpsCapIndex);
            PlayerName = PlayerPrefs.GetString("ps_name", PlayerName);
            LastJoinAddress = PlayerPrefs.GetString("ps_join", LastJoinAddress);
            Port = PlayerPrefs.GetInt("ps_port", Port);
            InputDelay = PlayerPrefs.GetInt("ps_delay", InputDelay);
            Clamp();
        }

        public static void Save()
        {
            Clamp();
            PlayerPrefs.SetFloat("ps_master", Master);
            PlayerPrefs.SetFloat("ps_music", Music);
            PlayerPrefs.SetFloat("ps_sfx", Sfx);
            PlayerPrefs.SetFloat("ps_shake", Shake);
            PlayerPrefs.SetInt("ps_flash", ImpactFlashes ? 1 : 0);
            PlayerPrefs.SetInt("ps_dmgnum", DamageNumbers ? 1 : 0);
            PlayerPrefs.SetInt("ps_animfps", AnimFpsIndex);
            PlayerPrefs.SetInt("ps_vfx", VfxQuality);
            PlayerPrefs.SetInt("ps_window", WindowMode);
            PlayerPrefs.SetInt("ps_res", ResolutionIndex);
            PlayerPrefs.SetInt("ps_vsync", VSync ? 1 : 0);
            PlayerPrefs.SetInt("ps_fps", FpsCapIndex);
            PlayerPrefs.SetString("ps_name", PlayerName);
            PlayerPrefs.SetString("ps_join", LastJoinAddress);
            PlayerPrefs.SetInt("ps_port", Port);
            PlayerPrefs.SetInt("ps_delay", InputDelay);
            PlayerPrefs.Save();
        }

        static void Clamp()
        {
            Master = Mathf.Clamp01(Master); Music = Mathf.Clamp01(Music); Sfx = Mathf.Clamp01(Sfx);
            Shake = Mathf.Clamp(Shake, 0f, 1.5f);
            VfxQuality = Mathf.Clamp(VfxQuality, 0, 2);
            AnimFpsIndex = Mathf.Clamp(AnimFpsIndex, 0, AnimFpsSteps.Length - 1);
            WindowMode = Mathf.Clamp(WindowMode, 0, 2);
            FpsCapIndex = Mathf.Clamp(FpsCapIndex, 0, FpsCaps.Length - 1);
            Port = Mathf.Clamp(Port, 1024, 65535);
            InputDelay = Mathf.Clamp(InputDelay, 1, 8);
            if (string.IsNullOrEmpty(PlayerName)) PlayerName = "Sorcerer";
            if (PlayerName.Length > 16) PlayerName = PlayerName.Substring(0, 16);
        }

        /// <summary>Applies display settings. Safe to call any time.</summary>
        public static void ApplyGraphics()
        {
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            Application.targetFrameRate = VSync ? -1 : FpsCaps[FpsCapIndex];

            var mode = WindowMode == 0 ? FullScreenMode.Windowed
                     : WindowMode == 1 ? FullScreenMode.FullScreenWindow
                     : FullScreenMode.ExclusiveFullScreen;
            var resList = Screen.resolutions;
            if (ResolutionIndex >= 0 && ResolutionIndex < resList.Length)
            {
                var r = resList[ResolutionIndex];
                Screen.SetResolution(r.width, r.height, mode);
            }
            else
            {
                var cur = Screen.currentResolution;
                if (mode == FullScreenMode.Windowed)
                    Screen.SetResolution(Mathf.Max(1280, cur.width * 3 / 4), Mathf.Max(720, cur.height * 3 / 4), mode);
                else
                    Screen.SetResolution(cur.width, cur.height, mode);
            }
        }

        public static string ResolutionLabel()
        {
            var resList = Screen.resolutions;
            if (ResolutionIndex >= 0 && ResolutionIndex < resList.Length)
                return resList[ResolutionIndex].width + " x " + resList[ResolutionIndex].height;
            return "Native";
        }
    }
}
