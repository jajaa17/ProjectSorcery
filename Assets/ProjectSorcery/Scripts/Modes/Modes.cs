using System.Collections.Generic;
using UnityEngine;

namespace ProjectSorcery
{
    public sealed class MatchResult
    {
        public string Title = "", Subtitle = "";
        public Color Color = Color.white;
        public int WinnerTeam = -1;
        public List<Fighter> Ranking = new List<Fighter>();
    }

    /// <summary>Rules of a game mode. Runs inside the deterministic simulation.</summary>
    public abstract class ModeController
    {
        protected Match M;
        protected MatchConfig C;
        public bool InputEnabled = true;
        public float TimeLeft = -1f;
        public int[] Wins = new int[4];
        public int Round = 1;
        public MatchResult Result;
        public string Header = "";        // shown at the top of the HUD (wave count, boss phase...)
        public int BossPhase = 1;
        public Fighter Boss;

        public virtual float BossDamageMul => 1f;
        public virtual float CeRegenMul => 1f;
        public virtual float DomainDurationMul(Fighter f) => 1f;
        public virtual bool ShowRoundWins => false;
        public virtual Arena ConfigureArena(Arena a) => a;
        public virtual bool IsBossSlot(int slot) => false;
        public virtual void PrepareFighter(Fighter f) { }
        public abstract void Begin();
        public abstract void Tick(float dt);

        public static ModeController Create(Match m, MatchConfig c)
        {
            ModeController mc;
            switch (c.Mode)
            {
                case GameMode.Survival: mc = new SurvivalMode(); break;
                case GameMode.Raid: mc = new RaidMode(); break;
                case GameMode.Training: mc = new TrainingMode(); break;
                default: mc = new VersusMode(); break;
            }
            mc.M = m; mc.C = c;
            return mc;
        }

        protected Vector2 SpawnPos(int slot, int count, int team)
        {
            switch (C.Layout)
            {
                case TeamLayout.Duel: return new Vector2(slot == 0 ? -4.5f : 4.5f, 0f);
                case TeamLayout.FreeForAll: return new Vector2(slot == 0 ? -7f : slot == 1 ? 7f : 0f, slot == 2 ? 4f : 0f);
                case TeamLayout.TwoVsOne: return new Vector2(slot == 0 ? -6f : slot == 1 ? -3.5f : 5f, 0f);
                default: return new Vector2(slot == 0 ? -6.5f : slot == 1 ? -3.5f : slot == 2 ? 3.5f : 6.5f, 0f);
            }
        }

        protected void ResetAll()
        {
            M.ClearEntities();
            for (int i = 0; i < M.Fighters.Count; i++)
            {
                var f = M.Fighters[i];
                Vector2 p = SpawnPos(f.Slot, M.Fighters.Count, f.Team);
                f.ResetForRound(p, p.x < 0 ? 1 : -1);
                if (C.InfiniteCe) f.Ce = f.MaxCe;
            }
        }

        protected void LockAll(bool locked)
        {
            InputEnabled = !locked;
        }

        protected int TeamsAlive(out int lastTeam)
        {
            lastTeam = -1;
            int teams = 0;
            var seen = 0;
            for (int i = 0; i < M.Fighters.Count; i++)
            {
                var f = M.Fighters[i];
                if (f.Dead || f.State == FState.Respawning && f.Lives <= 0) continue;
                int bit = 1 << f.Team;
                if ((seen & bit) == 0) { seen |= bit; teams++; lastTeam = f.Team; }
            }
            return teams;
        }

        protected void Finish(string title, string sub, Color c, int winner)
        {
            if (Result != null) return;
            Result = new MatchResult { Title = title, Subtitle = sub, Color = c, WinnerTeam = winner };
            var list = new List<Fighter>(M.Fighters);
            list.Sort((x, y) =>
            {
                int wx = x.Team == winner ? 1 : 0, wy = y.Team == winner ? 1 : 0;
                if (wx != wy) return wy.CompareTo(wx);
                return y.StatDamage.CompareTo(x.StatDamage);
            });
            Result.Ranking = list;
            M.Ended = true;
        }
    }

    // =========================================================================
    public sealed class VersusMode : ModeController
    {
        enum Phase { Intro, Fight, Ko, Done }
        Phase phase;
        float t;
        int koWinner;

        public override bool ShowRoundWins => !C.Demo;

        public override void Begin() { StartRound(1); }

        void StartRound(int r)
        {
            Round = r;
            ResetAll();
            phase = Phase.Intro; t = 0f;
            TimeLeft = C.RoundTime;
            LockAll(true);
            if (!C.Demo)
            {
                bool final = false;
                for (int i = 0; i < Wins.Length; i++) if (Wins[i] == C.RoundsToWin - 1 && C.RoundsToWin > 1) final = true;
                M.Events.Announce(final && r > 1 ? "FINAL ROUND" : "ROUND " + r, "", Color.white, 1.2f);
                Audio.Play(Sfx.Gong, Vector2.zero, 0.9f);
            }
        }

        public override void Tick(float dt)
        {
            t += dt;
            switch (phase)
            {
                case Phase.Intro:
                    if (t >= (C.Demo ? 0.4f : 1.5f))
                    {
                        phase = Phase.Fight; t = 0f;
                        LockAll(false);
                        if (!C.Demo) { M.Events.Announce("FIGHT!", "", new Color(1f, 0.3f, 0.35f), 0.8f); Audio.Play(Sfx.Bell, Vector2.zero, 1f); }
                    }
                    break;
                case Phase.Fight:
                    if (!M.Domains.Clashing && M.FreezeTimer <= 0f) TimeLeft -= dt;
                    int teams = TeamsAlive(out int last);
                    if (teams <= 1)
                    {
                        koWinner = teams == 1 ? last : -1;
                        phase = Phase.Ko; t = 0f;
                        if (!C.Demo) M.Events.Announce(teams == 0 ? "DOUBLE K.O." : "K.O.", "", new Color(1f, 0.85f, 0.3f), 1.8f);
                    }
                    else if (TimeLeft <= 0f && C.RoundTime > 0f)
                    {
                        // time over: team with the most remaining health wins
                        float[] hp = new float[4]; float best = -1f; koWinner = -1;
                        foreach (var f in M.Fighters) if (!f.Dead) hp[f.Team] += f.HpFrac;
                        for (int i = 0; i < 4; i++) if (hp[i] > best + 0.001f) { best = hp[i]; koWinner = i; } else if (Mathf.Abs(hp[i] - best) <= 0.001f && hp[i] > 0f) koWinner = -1;
                        phase = Phase.Ko; t = 0f;
                        M.Events.Announce("TIME", "", Color.white, 1.6f);
                    }
                    break;
                case Phase.Ko:
                    if (t > 1.2f) foreach (var f in M.Fighters) if (!f.Dead && f.Team == koWinner && f.State != FState.Victory && f.Grounded && f.State != FState.Hitstun) f.SetVictory();
                    if (t >= 2.8f)
                    {
                        if (koWinner >= 0) Wins[koWinner]++;
                        bool done = C.Demo || (koWinner >= 0 && Wins[koWinner] >= C.RoundsToWin) || Round >= 9;
                        if (done)
                        {
                            phase = Phase.Done;
                            string name = TeamName(koWinner);
                            Finish(koWinner < 0 ? "DRAW" : name + " WINS", Round + (Round == 1 ? " round" : " rounds"), koWinner < 0 ? Color.white : Match.TeamColor(koWinner), koWinner);
                        }
                        else StartRound(Round + 1);
                    }
                    break;
            }
        }

        string TeamName(int team)
        {
            if (team < 0) return "NOBODY";
            if (C.Layout == TeamLayout.Duel || C.Layout == TeamLayout.FreeForAll)
            {
                foreach (var f in M.Fighters) if (f.Team == team) return f.Def.Name.ToUpperInvariant();
            }
            return team == 0 ? "TEAM AZURE" : team == 1 ? "TEAM CRIMSON" : "TEAM " + (team + 1);
        }
    }

    // =========================================================================
    public sealed class TrainingMode : ModeController
    {
        readonly Dictionary<Fighter, float> respawn = new Dictionary<Fighter, float>();
        public override float CeRegenMul => C.InfiniteCe ? 50f : 1f;
        public override void Begin()
        {
            ResetAll();
            TimeLeft = -1f;
            Header = "TRAINING";
            M.Events.Announce("TRAINING", "Pause menu: toggle dummy behaviour and exit", Color.white, 1.6f);
        }

        public override void Tick(float dt)
        {
            foreach (var f in M.Fighters)
            {
                if (C.InfiniteCe) f.Ce = f.MaxCe;
                if (f.Dead)
                {
                    if (!respawn.ContainsKey(f)) respawn[f] = 1.6f;
                    respawn[f] -= dt;
                    if (respawn[f] <= 0f)
                    {
                        respawn.Remove(f);
                        f.Revive(SpawnPos(f.Slot, 2, f.Team), 1f);
                    }
                }
                else if (f.IsDummy && f.State == FState.Idle && f.Hp < f.MaxHp && f.ComboTimer <= 0f)
                {
                    f.Heal(f.MaxHp * 0.5f * dt, false);
                }
            }
        }
    }

    // =========================================================================
    public sealed class SurvivalMode : ModeController
    {
        enum Phase { Intro, Fight, Break, Over }
        Phase phase;
        float t;
        public int Wave;
        readonly List<Fighter> enemies = new List<Fighter>();

        public override Arena ConfigureArena(Arena a) { a.Left = -14f; a.Right = 14f; return a; }

        public override void Begin()
        {
            ResetAll();
            phase = Phase.Intro; t = 0f; Wave = 0;
            LockAll(true);
            M.Events.Announce("SURVIVAL", "Hold out against endless curses", new Color(0.7f, 0.85f, 1f), 1.6f);
        }

        public override void Tick(float dt)
        {
            t += dt;
            switch (phase)
            {
                case Phase.Intro:
                    if (t > 1.6f) { LockAll(false); NextWave(); }
                    break;
                case Phase.Fight:
                {
                    bool anyPlayer = false;
                    foreach (var f in M.Fighters) if (f.Team == 0 && !f.Dead) anyPlayer = true;
                    if (!anyPlayer)
                    {
                        phase = Phase.Over;
                        M.Events.Announce("DEFEATED", "Reached wave " + Wave, new Color(1f, 0.35f, 0.35f), 2.5f);
                        t = 0f;
                        break;
                    }
                    bool anyEnemy = false;
                    foreach (var e in enemies) if (!e.Dead) anyEnemy = true;
                    if (!anyEnemy)
                    {
                        phase = Phase.Break; t = 0f;
                        M.Events.Announce("WAVE " + Wave + " CLEARED", "Recovering...", new Color(0.6f, 1f, 0.7f), 1.6f);
                        foreach (var f in M.Fighters)
                        {
                            if (f.Team != 0) continue;
                            if (f.Dead) f.Revive(new Vector2(-3f - f.Slot, 0f), 0.4f);
                            else { f.Heal(f.MaxHp * 0.35f); f.Ce = f.MaxCe; }
                        }
                    }
                    break;
                }
                case Phase.Break:
                    if (t > 2.8f) NextWave();
                    break;
                case Phase.Over:
                    if (t > 2.6f) Finish("SURVIVED " + (Wave - 1) + " WAVES", "Wave reached: " + Wave, new Color(0.7f, 0.85f, 1f), -1);
                    break;
            }
            Header = "WAVE " + Wave;
        }

        void NextWave()
        {
            Wave++;
            foreach (var e in enemies) M.RemoveFighter(e);
            enemies.Clear();
            M.ClearEntities();
            int players = 0;
            foreach (var f in M.Fighters) if (f.Team == 0) players++;
            int count = 1 + (Wave >= 4 ? 1 : 0) + (Wave >= 10 && players > 1 ? 1 : 0);
            Difficulty d = Wave <= 2 ? Difficulty.Easy : Wave <= 6 ? Difficulty.Medium : Difficulty.Hard;
            bool elite = Wave % 5 == 0;
            for (int i = 0; i < count; i++)
            {
                CharacterDef def;
                if (elite && i == 0) def = Roster.RandomTier(M.Rng, 1);
                else def = Roster.RandomPlayable(M.Rng);
                float hpMul = 1f + 0.05f * (Wave - 1) + (elite && i == 0 ? 0.5f : 0f);
                float x = i % 2 == 0 ? 8f : -9f;
                var e = M.AddFighter(def, 1, elite && i == 0 ? Difficulty.Hard : d, hpMul, new Vector2(x, 0f), x > 0 ? -1 : 1);
                e.Tag = elite && i == 0 ? "ELITE" : "WAVE " + Wave;
                enemies.Add(e);
            }
            phase = Phase.Fight; t = 0f;
            M.Events.Announce("WAVE " + Wave, elite ? "An elite sorcerer approaches" : "", elite ? new Color(1f, 0.5f, 0.3f) : Color.white, 1.4f);
            Audio.Play(Sfx.Gong, Vector2.zero, 0.8f);
        }
    }

    // =========================================================================
    /// <summary>Co-op boss raid against the Heian-era calamity king.</summary>
    public sealed class RaidMode : ModeController
    {
        enum Phase { Intro, Fight, Over }
        Phase phase;
        float t;
        int bossSlot = -1;
        int players;
        bool summoned;
        readonly Dictionary<Fighter, float> respawn = new Dictionary<Fighter, float>();

        public override float BossDamageMul => 0.95f + 0.06f * Mathf.Max(0, players - 1) + (BossPhase >= 3 ? 0.15f : 0f);
        public override float DomainDurationMul(Fighter f) => f.IsBoss ? 1.2f : 1f;
        public override Arena ConfigureArena(Arena a) { a.Left = -15f; a.Right = 15f; a.Ceiling = 12f; return a; }

        public override bool IsBossSlot(int slot)
        {
            if (bossSlot < 0)
                for (int i = 0; i < C.Slots.Count; i++) if (C.Slots[i].Team == 1) { bossSlot = i; break; }
            return slot == bossSlot;
        }

        public override void PrepareFighter(Fighter f)
        {
            if (f.IsBoss)
            {
                int p = 0;
                foreach (var s in C.Slots) if (s.Team == 0 && s.Control != SlotControl.None) p++;
                players = Mathf.Max(1, p);
                f.HpMul = Balance.RaidBossHp(players) / (f.Def.Hp * Balance.GlobalHp);
                f.SizeMul = 1.12f;
                Boss = f;
            }
            else f.Lives = 2;
        }

        new Vector2 SpawnPos(int slot, int count, int team) => team == 1 ? new Vector2(8f, 0f) : new Vector2(-8f + slot * 2.2f, 0f);

        public override void Begin()
        {
            M.ClearEntities();
            foreach (var f in M.Fighters)
            {
                Vector2 p = SpawnPos(f.Slot, 0, f.Team);
                f.ResetForRound(p, f.Team == 1 ? -1 : 1);
                if (!f.IsBoss) f.Lives = 2;
            }
            phase = Phase.Intro; t = 0f;
            LockAll(true);
            M.Events.Announce("CALAMITY RAID", "The King of Calamity, Heian Form", new Color(1f, 0.25f, 0.3f), 2.2f);
            Audio.Play(Sfx.Rumble, Vector2.zero, 1f);
            Header = "PHASE I";
        }

        public override void Tick(float dt)
        {
            t += dt;
            switch (phase)
            {
                case Phase.Intro:
                    if (t > 2.4f) { phase = Phase.Fight; LockAll(false); M.Events.Announce("SURVIVE", "", Color.white, 0.8f); }
                    break;
                case Phase.Fight:
                    if (Boss == null) break;
                    if (Boss.Dead)
                    {
                        phase = Phase.Over; t = 0f;
                        M.Events.Announce("CALAMITY SEALED", "", new Color(1f, 0.9f, 0.5f), 3f);
                        break;
                    }
                    // phases
                    float hp = Boss.HpFrac;
                    if (BossPhase == 1 && hp < 0.66f)
                    {
                        BossPhase = 2; Header = "PHASE II";
                        M.Events.Announce("PHASE II", "The king grows serious - domain unlocked", new Color(1f, 0.3f, 0.3f), 2f);
                        Boss.AddStatus(StatusType.Invuln, 1.2f, 0f, Boss);
                        VFX.ImpactFrame(ImpactKind.Domain, Boss.Center);
                        Audio.Play(Sfx.Rumble, Boss.Center);
                        Boss.Ce = Boss.MaxCe;
                    }
                    else if (BossPhase == 2 && hp < 0.33f)
                    {
                        BossPhase = 3; Header = "PHASE III";
                        M.Events.Announce("PHASE III", "Enraged: the world itself will be cut", new Color(1f, 0.15f, 0.15f), 2.2f);
                        Boss.AddStatus(StatusType.Invuln, 1.5f, 0f, Boss);
                        Boss.AddStatus(StatusType.PowerUp, 999f, 0.2f, Boss);
                        Boss.Ce = Boss.MaxCe;
                        VFX.ImpactFrame(ImpactKind.BlackFlash, Boss.Center);
                        if (!summoned)
                        {
                            summoned = true;
                            M.SpawnMinion(Boss, Roster.WheelGeneral, new Vector2(Boss.Pos.x - Boss.Facing * 2f, 0f), 1.2f);
                            M.Events.Announce("", "The Wheel General answers the king", new Color(1f, 0.95f, 0.7f), 1.6f);
                        }
                    }
                    // player lives
                    bool anyAlive = false;
                    foreach (var f in M.Fighters)
                    {
                        if (f.Team != 0) continue;
                        if (f.Dead && f.Lives > 0 && !respawn.ContainsKey(f)) { respawn[f] = 5f; f.Lives--; }
                        if (respawn.TryGetValue(f, out float r))
                        {
                            r -= dt; respawn[f] = r;
                            if (r <= 0f) { respawn.Remove(f); f.Revive(new Vector2(-10f, 0f), 0.6f); }
                        }
                        if (!f.Dead || respawn.ContainsKey(f)) anyAlive = true;
                    }
                    if (!anyAlive)
                    {
                        phase = Phase.Over; t = 0f;
                        M.Events.Announce("THE CALAMITY REIGNS", "", new Color(1f, 0.2f, 0.25f), 3f);
                    }
                    break;
                case Phase.Over:
                    if (t > 3f)
                    {
                        bool won = Boss != null && Boss.Dead;
                        Finish(won ? "CALAMITY SEALED" : "DEFEAT", won ? "The Heian king falls" : "Phase " + BossPhase + " reached - boss at " + Mathf.CeilToInt(Boss.HpFrac * 100f) + "%",
                               won ? new Color(1f, 0.85f, 0.4f) : new Color(1f, 0.3f, 0.3f), won ? 0 : 1);
                    }
                    break;
            }
        }

        public float RespawnLeft(Fighter f) => respawn.TryGetValue(f, out float r) ? r : 0f;
    }
}
