using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ProjectSorcery
{
    /// <summary>
    /// Automated screenshot tour for the README / store page.
    /// Launch a build with:  ProjectSorcery -screenshots &lt;folder&gt;
    /// It plays scripted CPU fights (Black Flash, domain expansion, 2- and 3-way clashes, the raid),
    /// saves PNGs at the key moments and quits. Settings are never saved while touring.
    /// </summary>
    public sealed class ScreenshotTour : MonoBehaviour
    {
        string dir;
        int shot;

        public static bool TryStart(GameObject host)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-screenshots")
                {
                    var t = host.AddComponent<ScreenshotTour>();
                    t.dir = args[i + 1];
                    return true;
                }
            return false;
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(dir);
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            Settings.ImpactFlashes = true;
            yield return Wait(4f);
            yield return Shot("main_menu");

            GameRoot.I.ShowCharacters();
            yield return Wait(1.5f);
            yield return Shot("characters");

            // ---- team fight
            Fight(GameMode.Versus, TeamLayout.TwoVsTwo, 1, ("vessel", 0), ("shadow", 0), ("sculptor", 1), ("volcano", 1));
            yield return Wait(8f);
            yield return Shot("team_fight");
            yield return Wait(5f);
            yield return Shot("team_fight");

            // ---- black flash
            var m = Fight(GameMode.Versus, TeamLayout.Duel, 2, ("vessel", 0), ("bestfriend", 1));
            bool bf = false;
            m.Events.OnBlackFlash += (a, v, n) => bf = true;
            yield return Wait(3f);
            m.Fighters[0].ForcedBlackFlashes = 20;
            yield return Until(() => bf, 20f);
            yield return null;
            yield return Shot("black_flash");
            yield return Wait(0.25f);
            yield return Shot("black_flash");

            // ---- domain expansion
            m = Fight(GameMode.Versus, TeamLayout.Duel, 3, ("strongest", 0), ("thunder", 1));
            bool expanded = false;
            m.Events.OnDomainExpanded += (f, d) => expanded = true;
            yield return Wait(3f);
            yield return CastDomain(m.Fighters[0]);
            yield return Wait(0.5f);
            yield return Shot("domain_chant");
            yield return Until(() => expanded, 8f);
            yield return Wait(0.4f);
            yield return Shot("domain_expansion");
            yield return Wait(1.6f);
            yield return Shot("domain_expansion");

            // ---- two-way domain clash
            m = Fight(GameMode.Versus, TeamLayout.Duel, 4, ("calamity", 0), ("strongest", 1));
            yield return Clash(m, "domain_clash");

            // ---- three-way domain clash
            m = Fight(GameMode.Versus, TeamLayout.FreeForAll, 5, ("judge", 0), ("calamity", 1), ("shadow", 2));
            yield return Clash(m, "triple_domain_clash");

            // ---- raid
            m = StartRaid();
            yield return Wait(9f);
            yield return Shot("calamity_raid");
            yield return Wait(6f);
            yield return Shot("calamity_raid");

            Debug.Log("[ScreenshotTour] done: " + shot + " screenshots in " + dir);
            Application.Quit();
        }

        IEnumerator Clash(Match m, string name)
        {
            bool clash = false;
            m.Events.OnClashStart += l => clash = true;
            yield return Wait(3f);
            for (float t = 0f; t < 3f && !clash; t += 0.1f)
            {
                foreach (var f in m.Fighters) { f.Ce = f.MaxCe; f.Cd[3] = 0f; f.TryCast(3); }
                yield return new WaitForSecondsRealtime(0.1f);
                if (m.Domains.AnyActive || clash) break;
            }
            yield return Until(() => clash, 8f);
            yield return Wait(0.5f);
            yield return Shot(name);
            yield return Wait(1.5f);
            yield return Shot(name);
        }

        IEnumerator CastDomain(Fighter f)
        {
            for (float t = 0f; t < 4f; t += 0.1f)
            {
                f.Ce = f.MaxCe; f.Cd[3] = 0f;
                if (f.TryCast(3)) yield break;
                yield return new WaitForSecondsRealtime(0.1f);
            }
        }

        static Match Fight(GameMode mode, TeamLayout layout, int arena, params (string id, int team)[] fighters)
        {
            var cfg = new MatchConfig { Mode = mode, Layout = layout, Arena = arena, RoundsToWin = 1, RoundTime = 99f, Seed = (uint)Random.Range(1, int.MaxValue) };
            foreach (var f in fighters)
                cfg.Slots.Add(new SlotConfig { Control = SlotControl.Cpu, Diff = Difficulty.Hard, Character = Roster.IndexOf(f.id), Team = f.team });
            GameRoot.I.StartMatch(cfg);
            return GameRoot.I.CurrentMatch;
        }

        static Match StartRaid()
        {
            var cfg = new MatchConfig { Mode = GameMode.Raid, Layout = TeamLayout.TwoVsOne, Arena = Arenas.RaidArena, RoundsToWin = 1, RoundTime = 99f, Seed = (uint)Random.Range(1, int.MaxValue) };
            foreach (var id in new[] { "vessel_shinjuku", "love", "killer" })
                cfg.Slots.Add(new SlotConfig { Control = SlotControl.Cpu, Diff = Difficulty.Hard, Character = Roster.IndexOf(id), Team = 0 });
            cfg.Slots.Add(new SlotConfig { Control = SlotControl.Cpu, Diff = Difficulty.Nightmare, Character = Roster.BossIndex, Team = 1 });
            GameRoot.I.StartMatch(cfg);
            return GameRoot.I.CurrentMatch;
        }

        static IEnumerator Wait(float s) { yield return new WaitForSecondsRealtime(s); }

        static IEnumerator Until(System.Func<bool> cond, float timeout)
        {
            for (float t = 0f; t < timeout && !cond(); t += Time.unscaledDeltaTime) yield return null;
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            shot++;
            string path = Path.Combine(dir, $"{shot:00}_{name}.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Destroy(tex);
            Debug.Log("[ScreenshotTour] saved " + path);
        }
    }
}
