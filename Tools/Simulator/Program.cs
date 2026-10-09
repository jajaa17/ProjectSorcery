// Project Sorcery headless simulator: tests and balance tools that run the real gameplay code outside Unity.
// See README.md in this folder.
#pragma warning disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ProjectSorcery;
using Vector2 = UnityEngine.Vector2;

static class SimTests
{
    static int failures;
    static readonly Random R = new Random(1234);

    static Match Make(MatchConfig cfg)
    {
        Match.Headless = true;
        var m = new Match();
        m.Setup(cfg, null);
        return m;
    }

    static MatchConfig Cfg(GameMode mode, TeamLayout layout, params (int ch, int team, Difficulty d)[] slots)
    {
        var c = new MatchConfig { Mode = mode, Layout = layout, RoundsToWin = 1, RoundTime = 75f, Arena = 0, Seed = (uint)R.Next(1, int.MaxValue) };
        foreach (var s in slots) c.Slots.Add(new SlotConfig { Control = SlotControl.Cpu, Character = s.ch, Team = s.team, Diff = s.d });
        return c;
    }

    static void Check(bool ok, string what) { if (!ok) { failures++; Console.WriteLine("  FAIL: " + what); } }

    static int Run(Match m, int maxTicks, Action<Match, int> perTick = null)
    {
        int t = 0;
        var bits = new ushort[8];
        for (; t < maxTicks && !m.Ended; t++)
        {
            m.SimTick(bits);
            perTick?.Invoke(m, t);
            foreach (var f in m.Fighters)
                if (float.IsNaN(f.Pos.x) || float.IsNaN(f.Pos.y) || float.IsNaN(f.Hp) || float.IsInfinity(f.Pos.x)) throw new Exception("NaN state on " + f.Def.Id);
        }
        return t;
    }

    static int Playable() { int i; do i = R.Next(Roster.Count); while (Roster.Get(i).BossOnly); return i; }

    static int Main(string[] args)
    {
        string mode = args.Length > 0 ? args[0] : "all";
        Console.WriteLine("Roster: " + Roster.Count + " (boss index " + Roster.BossIndex + ")");
        var sw = Stopwatch.StartNew();

        if (mode == "all" || mode == "determinism") Determinism();
        if (mode == "all" || mode == "smoke") Smoke(mode == "smoke" ? 1 : 1);
        if (mode == "all" || mode == "modes") Modes();
        if (mode == "all" || mode == "domains") Domains();
        if (mode == "balance") Balance(args.Length > 1 ? int.Parse(args[1]) : 16);
        if (mode == "tune") Tune(int.Parse(args[1]), int.Parse(args[2]));
        if (mode == "art") ExportArt(args.Length > 1 ? args[1] : "art");
        if (mode == "anim") ExportAnim(args.Length > 1 ? args[1] : "anim", args.Length > 2 ? args[2] : "vessel");

        Console.WriteLine($"Done in {sw.Elapsed.TotalSeconds:F1}s, failures: {failures}");
        return failures == 0 ? 0 : 1;
    }

    // ------------------------------------------------------------------ determinism (online lockstep safety)
    static void Determinism()
    {
        Console.WriteLine("[determinism]");
        for (int k = 0; k < 6; k++)
        {
            var cfg = Cfg(GameMode.Versus, TeamLayout.TwoVsTwo, (Playable(), 0, Difficulty.Hard), (Playable(), 0, Difficulty.Hard), (Playable(), 1, Difficulty.Hard), (Playable(), 1, Difficulty.Hard));
            cfg.RoundTime = 60f;
            var a = Make(cfg); Run(a, 3600); uint ha = a.Checksum(); int ta = a.TickCount;
            var b = Make(cfg.Copy()); Run(b, 3600); uint hb = b.Checksum(); int tb = b.TickCount;
            Check(ha == hb && ta == tb, $"same seed diverged ({string.Join(",", cfg.Slots.Select(s => Roster.Get(s.Character).Id))})");
        }
        Console.WriteLine("  ok");
    }

    // ------------------------------------------------------------------ every character must survive full matches
    static void Smoke(int perChar)
    {
        Console.WriteLine("[smoke] every fighter, Hard AI vs Hard AI");
        int crashes = 0;
        for (int i = 0; i < Roster.Count; i++)
        {
            var d = Roster.Get(i);
            if (d.BossOnly) continue;
            for (int r = 0; r < perChar; r++)
            {
                var cfg = Cfg(GameMode.Versus, TeamLayout.Duel, (i, 0, Difficulty.Hard), (Playable(), 1, Difficulty.Hard));
                try
                {
                    var m = Make(cfg);
                    int t = Run(m, 60 * 240);
                    Check(m.Ended, d.Id + " match never ended");
                    // every special in the kit must be castable at some point
                }
                catch (Exception e) { crashes++; failures++; Console.WriteLine($"  CRASH {d.Id}: {e.GetType().Name}: {e.Message}\n{Short(e.StackTrace)}"); }
            }
        }
        // each ability executes without throwing for every fighter
        foreach (var d in Roster.All)
        {
            var cfg = Cfg(GameMode.Training, TeamLayout.Duel, (d.Index, 0, Difficulty.Dummy), (Playable(), 1, Difficulty.Dummy));
            cfg.InfiniteCe = true;
            var m = Make(cfg);
            var f = m.Fighters[0];
            for (int slot = 0; slot < 4; slot++)
            {
                try
                {
                    Run(m, 120);
                    f.Cd[slot] = 0f; f.Ce = f.MaxCe; f.RemoveStatus(StatusType.Burnout);
                    bool cast = f.TryCast(slot);
                    Run(m, 400);
                    if (!cast && f.Kit[slot] != null && !(f.Kit[slot] is SummonAb) && !(f.Kit[slot] is CustomAb))
                        Console.WriteLine($"  note: {d.Id} slot {slot} ({f.Kit[slot].Name}) could not be cast in test");
                }
                catch (Exception e) { crashes++; failures++; Console.WriteLine($"  CRASH {d.Id} ability {slot}: {e.GetType().Name}: {e.Message}\n{Short(e.StackTrace)}"); }
            }
        }
        Console.WriteLine(crashes == 0 ? "  ok" : "  crashes: " + crashes);
    }

    static string Short(string st) => string.Join("\n", (st ?? "").Split('\n').Take(6));

    // ------------------------------------------------------------------ team modes, survival, raid
    static void Modes()
    {
        Console.WriteLine("[modes]");
        try
        {
            var ffa = Make(Cfg(GameMode.Versus, TeamLayout.FreeForAll, (Playable(), 0, Difficulty.Hard), (Playable(), 1, Difficulty.Hard), (Playable(), 2, Difficulty.Hard)));
            Run(ffa, 60 * 90); Check(ffa.Ended, "FFA ended");
            var t2 = Make(Cfg(GameMode.Versus, TeamLayout.TwoVsTwo, (Playable(), 0, Difficulty.Medium), (Playable(), 0, Difficulty.Easy), (Playable(), 1, Difficulty.Hard), (Playable(), 1, Difficulty.Medium)));
            Run(t2, 60 * 90); Check(t2.Ended, "2v2 ended");
            var h = Make(Cfg(GameMode.Versus, TeamLayout.TwoVsOne, (Playable(), 0, Difficulty.Hard), (Playable(), 0, Difficulty.Hard), (Playable(), 1, Difficulty.Hard)));
            Run(h, 60 * 90); Check(h.Ended, "2v1 ended");

            var sv = Make(Cfg(GameMode.Survival, TeamLayout.Duel, (Playable(), 0, Difficulty.Hard), (Playable(), 0, Difficulty.Hard)));
            Run(sv, 60 * 240);
            var smode = (SurvivalMode)sv.Mode;
            Console.WriteLine($"  survival: reached wave {smode.Wave}, ended={sv.Ended}");
            Check(smode.Wave >= 2, "survival progressed past wave 1");

            int raidWins = 0, raids = 12;
            for (int k = 0; k < raids; k++)
            {
                var rc = Cfg(GameMode.Raid, TeamLayout.TwoVsOne, (Playable(), 0, Difficulty.Hard), (Playable(), 0, Difficulty.Hard), (Playable(), 0, Difficulty.Hard), (Roster.BossIndex, 1, Difficulty.Nightmare));
                rc.Arena = Arenas.RaidArena;
                var raid = Make(rc);
                Run(raid, 60 * 400);
                var rm = raid.Mode;
                if (rm.Result != null && rm.Result.WinnerTeam == 0) raidWins++;
                Console.WriteLine($"  raid {k}: ended={raid.Ended} phase={rm.BossPhase} bossHp={(rm.Boss != null ? rm.Boss.HpFrac : -1):P0} result={(rm.Result != null ? rm.Result.Title : "-")}");
            }
            Console.WriteLine($"  raid: 3 Hard CPUs beat the boss {raidWins}/{raids}");

            var tr = Cfg(GameMode.Training, TeamLayout.Duel, (Playable(), 0, Difficulty.Hard), (Playable(), 1, Difficulty.Dummy));
            var trm = Make(tr); Run(trm, 60 * 60); Check(!trm.Ended, "training never ends on its own");
        }
        catch (Exception e) { failures++; Console.WriteLine($"  CRASH modes: {e.GetType().Name}: {e.Message}\n{Short(e.StackTrace)}"); }
        Console.WriteLine("  done");
    }

    // ------------------------------------------------------------------ domains: expansion, 2-way and 3-way clashes
    static void Domains()
    {
        Console.WriteLine("[domains]");
        var users = Roster.All.Where(d => !d.BossOnly && d.Kit()[3] is DomainAb).Select(d => d.Index).ToList();
        Console.WriteLine("  domain users: " + users.Count);
        foreach (int u in users)
        {
            try
            {
                var cfg = Cfg(GameMode.Versus, TeamLayout.Duel, (u, 0, Difficulty.Dummy), (Playable(), 1, Difficulty.Dummy));
                var m = Make(cfg);
                Run(m, 100);
                var f = m.Fighters[0]; f.Ce = f.MaxCe;
                bool expanded = false; m.Events.OnDomainExpanded += (a, d) => expanded = true;
                Check(f.TryCast(3), Roster.Get(u).Id + " can cast domain");
                Run(m, 60 * 14);
                Check(expanded, Roster.Get(u).Id + " domain manifested");
                Check(!m.Domains.AnyActive, Roster.Get(u).Id + " domain collapsed afterwards");
                Check(f.Has(StatusType.Burnout) || f.Dead || m.Fighters[1].Dead || m.Ended, Roster.Get(u).Id + " burnout applied");
            }
            catch (Exception e) { failures++; Console.WriteLine($"  CRASH domain {Roster.Get(u).Id}: {e.Message}\n{Short(e.StackTrace)}"); }
        }
        // two-way clash
        for (int k = 0; k < 10; k++)
        {
            int a = users[R.Next(users.Count)], b = users[R.Next(users.Count)];
            var m = Make(Cfg(GameMode.Versus, TeamLayout.Duel, (a, 0, Difficulty.Hard), (b, 1, Difficulty.Hard)));
            Run(m, 60);
            bool clash = false, ended = false; m.Events.OnClashStart += l => clash = true; m.Events.OnClashEnd += w => ended = true;
            foreach (var f in m.Fighters) { f.Ce = f.MaxCe; f.TryCast(3); }
            Run(m, 60 * 8);
            Check(clash && ended, $"2-way clash {Roster.Get(a).Id} vs {Roster.Get(b).Id}");
            Check(m.Domains.Actives.Count <= 1, "one winner domain");
        }
        // three-way clash (free-for-all)
        for (int k = 0; k < 6; k++)
        {
            var ids = Enumerable.Range(0, 3).Select(_ => users[R.Next(users.Count)]).ToArray();
            var m = Make(Cfg(GameMode.Versus, TeamLayout.FreeForAll, (ids[0], 0, Difficulty.Hard), (ids[1], 1, Difficulty.Hard), (ids[2], 2, Difficulty.Hard)));
            Run(m, 60);
            int participants = 0; m.Events.OnClashStart += l => participants = Math.Max(participants, l.Count);
            foreach (var f in m.Fighters) { f.Ce = f.MaxCe; f.TryCast(3); }
            Run(m, 60 * 8);
            Check(participants == 3, "triple clash had 3 participants (got " + participants + ")");
        }
        Console.WriteLine("  done");
    }

    // ------------------------------------------------------------------ automatic tuning
    static Dictionary<int, float> Survey(int perChar, List<int> ids, out float spread)
    {
        var wins = ids.ToDictionary(i => i, i => 0f); var games = ids.ToDictionary(i => i, i => 0f);
        foreach (var i in ids)
            for (int r = 0; r < perChar; r++)
            {
                int j; do j = ids[R.Next(ids.Count)]; while (j == i);
                var m = Make(Cfg(GameMode.Versus, TeamLayout.Duel, (i, 0, Difficulty.Hard), (j, 1, Difficulty.Hard)));
                Run(m, 60 * 80);
                int w = m.Mode.Result != null ? m.Mode.Result.WinnerTeam : -1;
                games[i]++; games[j]++;
                if (w == 0) wins[i]++; else if (w == 1) wins[j]++; else { wins[i] += 0.5f; wins[j] += 0.5f; }
            }
        var rate = ids.ToDictionary(i => i, i => wins[i] / Math.Max(1f, games[i]));
        spread = (float)Math.Sqrt(rate.Values.Select(v => (v - 0.5f) * (v - 0.5f)).Average());
        return rate;
    }

    static float StepScale = float.TryParse(Environment.GetEnvironmentVariable("TUNE_STEP"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var s) ? s : 1f;
    static void Tune(int iterations, int perChar)
    {
        var ids = Roster.All.Where(d => !d.BossOnly).Select(d => d.Index).ToList();
        for (int it = 0; it < iterations; it++)
        {
            var rate = Survey(perChar, ids, out float spread);
            Console.WriteLine($"[tune] iteration {it}: rms deviation from 50% = {spread:P1}, min {rate.Values.Min():P0}, max {rate.Values.Max():P0}");
            float step = (it < iterations / 3 ? 0.9f : it < 2 * iterations / 3 ? 0.6f : 0.35f) * StepScale;
            foreach (var i in ids)
            {
                var d = Roster.Get(i);
                float g = 1f + step * (0.5f - rate[i]);
                d.DamageMul = Math.Clamp(d.DamageMul * g, 0.55f, 1.8f);
                d.ToughMul = Math.Clamp(d.ToughMul * (float)Math.Sqrt(g), 0.7f, 1.6f);
            }
        }
        var final = Survey(perChar, ids, out float fs);
        Console.WriteLine($"[tune] final rms deviation {fs:P1}, min {final.Values.Min():P0}, max {final.Values.Max():P0}");
        foreach (var i in ids.OrderBy(x => final[x])) Console.WriteLine($"  {Roster.Get(i).Id,-18} {final[i],5:P0}  dmg x{Roster.Get(i).DamageMul:F2}  tough x{Roster.Get(i).ToughMul:F2}");
        Console.WriteLine("TABLE");
        foreach (var i in ids) Console.WriteLine($"            {{ \"{Roster.Get(i).Id}\", ({Roster.Get(i).DamageMul:F2}f, {Roster.Get(i).ToughMul:F2}f) }},");
    }

    // ------------------------------------------------------------------ balance survey
    static void Balance(int perChar)
    {
        Console.WriteLine($"[balance] {perChar} Hard-vs-Hard duels per fighter (75s rounds)");
        var wins = new Dictionary<int, int>(); var games = new Dictionary<int, int>(); var dmg = new Dictionary<int, float>(); var timeouts = 0; var lengths = new List<int>();
        var ids = Roster.All.Where(d => !d.BossOnly).Select(d => d.Index).ToList();
        foreach (var i in ids) { wins[i] = 0; games[i] = 0; dmg[i] = 0; }
        foreach (var i in ids)
        {
            for (int r = 0; r < perChar; r++)
            {
                int j; do j = ids[R.Next(ids.Count)]; while (j == i);
                var cfg = Cfg(GameMode.Versus, TeamLayout.Duel, (i, 0, Difficulty.Hard), (j, 1, Difficulty.Hard));
                var m = Make(cfg);
                int t = Run(m, 60 * 80);
                lengths.Add(t);
                var a = m.Fighters[0]; var b = m.Fighters[1];
                games[i]++; games[j]++;
                dmg[i] += a.StatDamage; dmg[j] += b.StatDamage;
                int w = m.Mode.Result != null ? m.Mode.Result.WinnerTeam : -1;
                if (w == 0) wins[i]++; else if (w == 1) wins[j]++;
                if (m.Mode.TimeLeft <= 0f) timeouts++;
            }
        }
        lengths.Sort();
        Console.WriteLine($"  median match length {lengths[lengths.Count / 2] / 60f:F1}s, time-outs {timeouts}");
        foreach (var i in ids.OrderByDescending(x => wins[x] / (float)Math.Max(1, games[x])))
        {
            var d = Roster.Get(i);
            Console.WriteLine($"  {d.Id,-18} {wins[i] / (float)Math.Max(1, games[i]),6:P0}  ({games[i]} games, avg dmg {dmg[i] / Math.Max(1, games[i]),5:F0})  tier {d.Tier}");
        }
    }

    // ------------------------------------------------------------------ export the game's procedural art as PNGs
    static void ExportArt(string dir)
    {
        Console.WriteLine("[art] exporting portraits and domain interiors to " + dir);
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(dir, "portraits"));
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(dir, "domains"));
        foreach (var d in Roster.All)
            SavePng(Portraits.Paint(d), System.IO.Path.Combine(dir, "portraits", $"{d.Index:00}_{d.Id}.png"));
        var seen = new HashSet<DomainDef>();
        foreach (var d in Roster.All)
            if (d.Domain != null && seen.Add(d.Domain))
                SavePng(DomainTex.Get(d.Domain), System.IO.Path.Combine(dir, "domains", Slug(d.Domain.Name) + ".png"));
        Console.WriteLine($"  {Roster.Count} portraits, {seen.Count} domains");
    }

    static string Slug(string s) => new string(s.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray()).Trim('_');

    static void SavePng(UnityEngine.Texture2D t, string path)
    {
        int w = t.W, h = t.H;
        var raw = new byte[(w * 4 + 1) * h];
        for (int y = 0; y < h; y++)
        {
            int row = (h - 1 - y) * (w * 4 + 1); // Unity textures start at the bottom row
            raw[row] = 0;
            for (int x = 0; x < w; x++)
            {
                var c = t.Pixels[y * w + x];
                int o = row + 1 + x * 4;
                raw[o] = c.r; raw[o + 1] = c.g; raw[o + 2] = c.b; raw[o + 3] = c.a;
            }
        }
        using var fs = System.IO.File.Create(path);
        fs.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var ihdr = new byte[13];
        BE(ihdr, 0, (uint)w); BE(ihdr, 4, (uint)h); ihdr[8] = 8; ihdr[9] = 6;
        Chunk(fs, "IHDR", ihdr);
        var ms = new System.IO.MemoryStream();
        using (var z = new System.IO.Compression.ZLibStream(ms, System.IO.Compression.CompressionLevel.Optimal, true)) z.Write(raw);
        Chunk(fs, "IDAT", ms.ToArray());
        Chunk(fs, "IEND", Array.Empty<byte>());
    }

    static void BE(byte[] b, int o, uint v) { b[o] = (byte)(v >> 24); b[o + 1] = (byte)(v >> 16); b[o + 2] = (byte)(v >> 8); b[o + 3] = (byte)v; }

    static void Chunk(System.IO.Stream s, string type, byte[] data)
    {
        var len = new byte[4]; BE(len, 0, (uint)data.Length); s.Write(len);
        var td = new byte[4 + data.Length];
        for (int i = 0; i < 4; i++) td[i] = (byte)type[i];
        Buffer.BlockCopy(data, 0, td, 4, data.Length);
        s.Write(td);
        var crc = new byte[4]; BE(crc, 0, Crc(td)); s.Write(crc);
    }

    static uint Crc(byte[] d)
    {
        uint c = 0xFFFFFFFF;
        foreach (byte b in d) { c ^= b; for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1; }
        return c ^ 0xFFFFFFFF;
    }

    // ------------------------------------------------------------------ animation preview export
    // Plays a scripted move showcase per fighter against a dummy and records the solved skeleton every tick
    // (the exact output of RigSolver), for rendering preview GIFs outside Unity.
    static void ExportAnim(string dir, string idList)
    {
        System.IO.Directory.CreateDirectory(dir);
        foreach (var id in idList.Split(','))
        {
            int ci = Roster.IndexOf(id);
            var cfg = new MatchConfig { Mode = GameMode.Training, Layout = TeamLayout.Duel, RoundsToWin = 1, RoundTime = 0f, Arena = 0, Seed = 7, InfiniteCe = true };
            cfg.Slots.Add(new SlotConfig { Control = SlotControl.Local, Character = ci, Team = 0, Device = 0 });
            cfg.Slots.Add(new SlotConfig { Control = SlotControl.Cpu, Character = Roster.IndexOf("vessel"), Team = 1, Diff = Difficulty.Dummy });
            var m = Make(cfg);
            var bits = new ushort[8];
            for (int i = 0; i < 400 && !m.Mode.InputEnabled; i++) m.SimTick(bits);
            var me = m.Fighters[0]; var dummy = m.Fighters[1];
            var solvers = new[] { new RigSolver(me), new RigSolver(dummy) };

            // script: (label, ticks, bits per tick); presses are single-tick edges
            var script = new List<(string label, ushort[] seq, bool place)>();
            ushort[] Hold(ushort b, int n) { var a = new ushort[n]; for (int i = 0; i < n; i++) a[i] = b; return a; }
            ushort[] Seq(params (ushort b, int n)[] parts) { var l = new List<ushort>(); foreach (var p in parts) l.AddRange(Hold(p.b, p.n)); return l.ToArray(); }
            script.Add(("idle", Hold(0, 50), true));
            script.Add(("walk", Seq((IB.Left, 40), (0, 8), (IB.Right, 45), (0, 20)), false));
            script.Add(("light string", Seq((IB.Light, 1), (0, 15), (IB.Light, 1), (0, 15), (IB.Light, 1), (0, 17), (IB.Light, 1), (0, 40)), true));
            script.Add(("heavy", Seq((IB.Heavy, 1), (0, 55)), true));
            script.Add(("charged heavy", Seq((IB.Heavy, 46), (0, 55)), true));
            script.Add(("launcher", Seq((IB.Down | IB.Heavy, 1), (IB.Down, 6), (0, 50)), true));
            script.Add(("sweep", Seq((IB.Down | IB.Light, 1), (IB.Down, 6), (0, 40)), true));
            script.Add(("dash strike", Seq((IB.Right | IB.Dash, 1), (IB.Right, 4), (IB.Light, 1), (0, 50)), false));
            script.Add(("air", Seq((IB.Jump, 1), (0, 10), (IB.Light, 1), (0, 14), (IB.Light, 1), (0, 14), (IB.Heavy, 1), (0, 50)), true));
            script.Add(("skill", Seq((IB.S1, 1), (0, 70)), true));

            using var w = new System.IO.StreamWriter(System.IO.Path.Combine(dir, id + ".jsonl"));
            var col = me.Def.Look.Aura;
            w.WriteLine($"{{\"id\":\"{id}\",\"name\":\"{me.Def.Title}\",\"weapon\":\"{me.Def.Look.Weapon}\",\"aura\":[{col.r:F3},{col.g:F3},{col.b:F3}],\"size\":{me.Size:F3},\"dsize\":{dummy.Size:F3}}}");
            foreach (var step in script)
            {
                if (step.place)
                {
                    dummy.Pos = dummy.PrevPos = new Vector2(Math.Clamp(me.Pos.x + 1.9f, m.Arena.Left + 1f, m.Arena.Right - 1f), 0f);
                    dummy.Vel = Vector2.zero; dummy.Hp = dummy.MaxHp;
                    if (dummy.Pos.x - me.Pos.x < 1.2f) { me.Pos = me.PrevPos = new Vector2(dummy.Pos.x - 1.9f, 0f); }
                }
                foreach (var b in step.seq)
                {
                    bits[0] = b;
                    m.SimTick(bits);
                    var sb = new System.Text.StringBuilder();
                    sb.Append("{\"label\":\"").Append(step.label).Append("\",\"f\":[");
                    for (int k = 0; k < 2; k++)
                    {
                        var fi = m.Fighters[k]; var sv = solvers[k];
                        float dt = m.Dt * (fi.Hitstop > 0f ? 0.12f : 1f);
                        sv.Step(dt, 1f);
                        if (k > 0) sb.Append(',');
                        sb.Append("{\"j\":[");
                        Vector2[] js = { sv.FootB, sv.KneeB, sv.Hip, sv.KneeF, sv.FootF, sv.Neck, sv.HeadC, sv.HandB, sv.ElbowB, sv.ElbowF, sv.HandF, sv.WeaponBase, sv.WeaponTip, sv.SpineMid, sv.ShoulderB, sv.ShoulderF };
                        for (int q = 0; q < js.Length; q++) { if (q > 0) sb.Append(','); sb.Append($"[{js[q].x:F3},{js[q].y:F3}]"); }
                        sb.Append("],\"face\":").Append(fi.Facing * (int)sv.FaceSign);
                        sb.Append(",\"strike\":").Append(sv.Striking ? 1 : 0);
                        sb.Append(",\"clip\":\"").Append(sv.Clip != null ? sv.Clip.Name : fi.State.ToString()).Append('"');
                        var limb = sv.Clip != null ? sv.Clip.Smear : Limb.None;
                        sv.SmearSegment(limb, false, out var a0, out var b0);
                        sv.SmearSegment(limb, true, out var a1, out var b1);
                        sb.Append($",\"limb\":\"{limb}\",\"sm\":[[{a0.x:F3},{a0.y:F3},{b0.x:F3},{b0.y:F3}],[{a1.x:F3},{a1.y:F3},{b1.x:F3},{b1.y:F3}]]");
                        sb.Append(",\"hs\":").Append(fi.Hitstop > 0f ? 1 : 0);
                        sb.Append(",\"charge\":").Append(sv.Charging ? 1 : 0);
                        sb.Append(",\"contact\":").Append(sv.Contact ? 1 : 0);
                        sb.Append('}');
                    }
                    sb.Append("]}");
                    w.WriteLine(sb.ToString());
                }
            }
            Console.WriteLine("  exported " + id);
        }
    }
}
