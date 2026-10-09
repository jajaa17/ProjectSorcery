using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectSorcery
{
    /// <summary>
    /// Fighting-game character select. Every local player drives their own cursor with their own device
    /// (keyboard half or gamepad); the mouse drives whichever cursor is still choosing. After the humans
    /// lock in, the first player picks for each CPU slot (or Start for random).
    /// </summary>
    public sealed class CharSelectScreen : UIScreen
    {
        sealed class Cursor
        {
            public int Slot, Device, Index;
            public bool Locked;
            public Color Color;
            public RectTransform Marker;
            public Text Tag;
            public Card Card;
        }

        sealed class Card
        {
            public RectTransform Rt;
            public Image Portrait, Border;
            public Text Name, Info, Status;
        }

        readonly MatchConfig cfg;
        readonly List<int> tiles = new List<int>();       // roster index per tile, -1 = random
        readonly List<RectTransform> tileRts = new List<RectTransform>();
        readonly List<Cursor> humans = new List<Cursor>();
        readonly List<int> cpuSlots = new List<int>();
        readonly Dictionary<int, Card> cards = new Dictionary<int, Card>();
        Cursor cpuCursor;
        int cpuPick = -1;
        Text prompt;
        bool ready;
        float readyTime;
        const int Cols = 16;
        const float Tile = 92f, Gap = 98f;
        readonly System.Random visualRandom = new System.Random();

        public CharSelectScreen(MatchConfig c) { cfg = c; }

        protected override void OnBuild()
        {
            Backdrop(0.9f);
            var title = UIKit.LabelBox(Root, "CHOOSE YOUR SORCERER", 54, Color.white, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(1600f, 80f), TextAnchor.MiddleCenter, FontStyle.BoldAndItalic);
            UIKit.Outline(title, UIKit.Accent2, 3f);

            // grid
            tiles.Add(-1);
            for (int i = 0; i < Roster.Count; i++) if (!Roster.Get(i).BossOnly) tiles.Add(i);
            float gridW = Cols * Gap;
            var grid = UIKit.Box(Root, "Grid", new Vector2(0.5f, 1f), new Vector2(-gridW * 0.5f, -130f), new Vector2(gridW, 500f), new Vector2(0f, 1f));
            for (int k = 0; k < tiles.Count; k++)
            {
                var cell = UIKit.Box(grid, "Tile", new Vector2(0f, 1f), new Vector2((k % Cols) * Gap + Gap * 0.5f, -(k / Cols) * Gap - Gap * 0.5f), new Vector2(Tile, Tile));
                var bg = UIKit.Img(cell, new Color(0.12f, 0.12f, 0.16f, 1f), null, true);
                if (tiles[k] < 0)
                {
                    UIKit.Label(cell, "?", 64, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                }
                else
                {
                    UIKit.Img(UIKit.Rect(cell, "P", Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f)), Color.white, Portraits.Get(Roster.Get(tiles[k])));
                }
                var btn = cell.gameObject.AddComponent<Button>();
                btn.targetGraphic = bg;
                btn.navigation = new Navigation { mode = Navigation.Mode.None };
                int kk = k;
                btn.onClick.AddListener(() => MouseConfirm(kk));
                var hover = cell.gameObject.AddComponent<TileHover>();
                hover.OnEnter = () => MouseHover(kk);
                tileRts.Add(cell);
            }

            // cursors for local humans, CPU slots in order
            int cardIndex = 0;
            int cardCount = 0;
            foreach (var s in cfg.Slots) if (s.Control == SlotControl.Local || s.Control == SlotControl.Cpu && !(cfg.Mode == GameMode.Raid && s.Team == 1)) cardCount++;
            for (int i = 0; i < cfg.Slots.Count; i++)
            {
                var s = cfg.Slots[i];
                if (s.Control == SlotControl.None) continue;
                if (cfg.Mode == GameMode.Raid && s.Team == 1) { s.Character = Roster.BossIndex; continue; }
                var card = MakeCard(i, cardIndex++, cardCount);
                cards[i] = card;
                if (s.Control == SlotControl.Local)
                {
                    var c = new Cursor { Slot = i, Device = s.Device, Index = Mathf.Min(1 + humans.Count * 3, tiles.Count - 1), Color = Match.TeamColor(cfg.Layout == TeamLayout.FreeForAll ? i : humans.Count == 0 ? 0 : (cfg.Mode == GameMode.Versus && cfg.Layout == TeamLayout.Duel ? 1 : s.Team)), Card = card };
                    if (humans.Count > 0 && c.Color == humans[0].Color) c.Color = Match.TeamColor(humans.Count + 1);
                    MakeMarker(c, "P" + (humans.Count + 1));
                    humans.Add(c);
                }
                else cpuSlots.Add(i);
            }
            prompt = UIKit.LabelBox(Root, "", 30, new Color(1f, 0.9f, 0.6f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1800f, 50f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Outline(prompt, Color.black, 2f);
            if (humans.Count == 0) BeginCpuPicking();
            RefreshAll();
        }

        Card MakeCard(int slot, int index, int count)
        {
            float w = Mathf.Min(440f, 1800f / Mathf.Max(1, count) - 20f);
            float x = (index - (count - 1) * 0.5f) * (w + 20f);
            var c = new Card();
            c.Rt = UIKit.Box(Root, "Card", new Vector2(0.5f, 0f), new Vector2(x, 210f), new Vector2(w, 250f));
            c.Border = UIKit.Img(c.Rt, new Color(1f, 1f, 1f, 0.15f));
            UIKit.Img(UIKit.Rect(c.Rt, "Bg", Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f)), new Color(0.05f, 0.05f, 0.08f, 0.95f));
            var prt = UIKit.Box(c.Rt, "Portrait", new Vector2(0f, 1f), new Vector2(12f, -12f), new Vector2(130f, 130f), new Vector2(0f, 1f));
            c.Portrait = UIKit.Img(prt, Color.white);
            c.Name = UIKit.LabelBox(c.Rt, "", 26, Color.white, new Vector2(0f, 1f), new Vector2(155f, -14f), new Vector2(w - 165f, 130f), TextAnchor.UpperLeft, FontStyle.Bold);
            c.Name.rectTransform.pivot = new Vector2(0f, 1f);
            c.Info = UIKit.LabelBox(c.Rt, "", 17, UIKit.TextDim, new Vector2(0f, 0f), new Vector2(12f, 10f), new Vector2(w - 24f, 100f), TextAnchor.LowerLeft);
            c.Info.rectTransform.pivot = new Vector2(0f, 0f);
            c.Status = UIKit.LabelBox(c.Rt, "", 20, Color.white, new Vector2(1f, 1f), new Vector2(-10f, -8f), new Vector2(200f, 30f), TextAnchor.UpperRight, FontStyle.Bold);
            c.Status.rectTransform.pivot = new Vector2(1f, 1f);
            return c;
        }

        void MakeMarker(Cursor c, string tag)
        {
            c.Marker = UIKit.Box(tileRts[0].parent, "Cursor", new Vector2(0f, 1f), Vector2.zero, new Vector2(Tile + 12f, Tile + 12f));
            c.Marker.SetAsFirstSibling();
            var img = UIKit.Img(c.Marker, c.Color);
            c.Tag = UIKit.LabelBox(c.Marker, tag, 20, Color.white, new Vector2(0f, 1f), new Vector2(18f, 4f), new Vector2(60f, 26f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Outline(c.Tag, Color.black, 2f);
        }

        int ResolveIndex(int tile)
        {
            int idx = tiles[Mathf.Clamp(tile, 0, tiles.Count - 1)];
            return idx < 0 ? -1 : idx;
        }

        void RefreshAll()
        {
            foreach (var c in humans) PlaceMarker(c);
            if (cpuCursor != null) PlaceMarker(cpuCursor);
            for (int i = 0; i < cfg.Slots.Count; i++)
            {
                if (!cards.TryGetValue(i, out var card)) continue;
                var s = cfg.Slots[i];
                int idx;
                string status;
                var h = humans.Find(x => x.Slot == i);
                if (h != null) { idx = h.Locked ? s.Character : ResolveIndex(h.Index); status = h.Locked ? "<color=#7fff9f>READY</color>" : "<color=#ffd36a>CHOOSING</color>"; card.Border.color = h.Color; }
                else
                {
                    bool picking = cpuCursor != null && cpuPick == i;
                    bool picked = cpuPicked.Contains(i);
                    idx = picking ? ResolveIndex(cpuCursor.Index) : picked ? s.Character : -1;
                    status = picking ? "<color=#ffd36a>P1 PICKING</color>" : picked ? "<color=#7fff9f>READY</color>" : "<color=#888>CPU " + s.Diff + "</color>";
                    card.Border.color = picking ? Color.white : new Color(1f, 1f, 1f, 0.15f);
                }
                card.Status.text = status;
                if (idx < 0)
                {
                    card.Portrait.sprite = null; card.Portrait.color = new Color(0.15f, 0.15f, 0.2f);
                    card.Name.text = (h != null ? "P" + (humans.IndexOf(h) + 1) + "\n" : "CPU\n") + "<size=34>RANDOM</size>";
                    card.Info.text = "";
                }
                else
                {
                    var d = Roster.Get(idx);
                    card.Portrait.sprite = Portraits.Get(d); card.Portrait.color = Color.white;
                    card.Name.text = "<size=30>" + d.Name.ToUpperInvariant() + "</size>\n<color=#" + ColorUtility.ToHtmlStringRGB(d.Look.Aura) + "><size=20>" + d.Title + "</size></color>\n<size=16><color=#888899>" + d.Technique + "</color></size>";
                    card.Info.text = CharInfo.Kit(d, true);
                }
            }
            if (ready) prompt.text = "ALL READY  -  press CONFIRM to fight!   (Back to change)";
            else if (cpuCursor != null) prompt.text = "P1: choose a fighter for CPU slot " + (cpuPick + 1) + "     (Start / Esc = random for all CPUs)";
            else prompt.text = "Move with your controls, CONFIRM to lock in, BACK to unlock.";
        }

        void PlaceMarker(Cursor c)
        {
            if (c.Marker == null) return;
            var t = tileRts[Mathf.Clamp(c.Index, 0, tileRts.Count - 1)];
            c.Marker.anchoredPosition = t.anchoredPosition;
            c.Marker.gameObject.SetActive(!c.Locked || c == cpuCursor);
            var img = c.Marker.GetComponent<Image>();
            img.color = c.Locked ? c.Color.WithA(0.4f) : c.Color;
        }

        readonly HashSet<int> cpuPicked = new HashSet<int>();

        void BeginCpuPicking()
        {
            int next = -1;
            foreach (int s in cpuSlots) if (!cpuPicked.Contains(s)) { next = s; break; }
            if (next < 0) { cpuCursor = null; cpuPick = -1; ready = true; readyTime = Time.unscaledTime; return; }
            cpuPick = next;
            if (cpuCursor == null)
            {
                cpuCursor = new Cursor { Slot = next, Device = humans.Count > 0 ? humans[0].Device : 0, Index = 0, Color = new Color(0.75f, 0.75f, 0.8f) };
                MakeMarker(cpuCursor, "CPU");
            }
            cpuCursor.Slot = next;
            cpuCursor.Locked = false;
        }

        void Lock(Cursor c)
        {
            int idx = ResolveIndex(c.Index);
            if (idx < 0) idx = Roster.RandomPlayableIndex(visualRandom);
            cfg.Slots[c.Slot].Character = idx;
            if (c == cpuCursor)
            {
                cpuPicked.Add(c.Slot);
                BeginCpuPicking();
            }
            else
            {
                c.Locked = true;
                if (humans.TrueForAll(x => x.Locked)) BeginCpuPicking();
            }
            Audio.Ui(Sfx.UiConfirm);
            Audio.Play(Sfx.Clap, Vector2.zero, 0.5f);
        }

        void RandomizeRemainingCpus()
        {
            foreach (int s in cpuSlots)
                if (!cpuPicked.Contains(s)) { cfg.Slots[s].Character = Roster.RandomPlayableIndex(visualRandom); cpuPicked.Add(s); }
            BeginCpuPicking();
        }

        void Move(Cursor c, LocalDevice d)
        {
            int i = c.Index;
            if (d.MLeft) i--;
            if (d.MRight) i++;
            if (d.MUp) i -= Cols;
            if (d.MDown) i += Cols;
            i = Mathf.Clamp(i, 0, tiles.Count - 1);
            if (i != c.Index) { c.Index = i; Audio.Ui(Sfx.UiMove); }
        }

        Cursor ActiveForMouse()
        {
            if (cpuCursor != null) return cpuCursor;
            foreach (var c in humans) if (!c.Locked) return c;
            return null;
        }

        void MouseHover(int tile)
        {
            var c = ActiveForMouse();
            if (c == null || c.Index == tile) return;
            c.Index = tile;
            Audio.Ui(Sfx.UiMove);
            RefreshAll();
        }

        void MouseConfirm(int tile)
        {
            if (ready) { Go(); return; }
            var c = ActiveForMouse();
            if (c == null) return;
            c.Index = tile;
            Lock(c);
            RefreshAll();
        }

        void Go()
        {
            if (Time.unscaledTime - readyTime < 0.25f) return;
            var run = cfg.Copy();
            if (run.Arena < 0 && run.Mode != GameMode.Raid) run.Arena = visualRandom.Next(Arenas.SelectableCount);
            if (run.Mode == GameMode.Raid) run.Arena = Arenas.RaidArena;
            run.Seed = (uint)visualRandom.Next(1, int.MaxValue);
            GameRoot.I.StartMatch(run);
        }

        public override void Tick()
        {
            bool changed = false;
            if (ready)
            {
                foreach (var c in humans)
                {
                    var d = InputHub.Get(c.Device);
                    if (d == null) continue;
                    if (d.MConfirm) { Go(); return; }
                    if (d.MBack) { Unready(c); changed = true; }
                }
                if (humans.Count == 0 && InputHub.AnyConfirm) { Go(); return; }
                if (humans.Count == 0 && InputHub.AnyBack) { Back(); return; }
            }
            else if (cpuCursor != null)
            {
                var d = InputHub.Get(cpuCursor.Device);
                if (d != null)
                {
                    int before = cpuCursor.Index;
                    Move(cpuCursor, d);
                    if (before != cpuCursor.Index) changed = true;
                    if (d.MConfirm) { Lock(cpuCursor); changed = true; }
                    else if (d.MStart || KeyInput.Down(KeyCode.Escape)) { RandomizeRemainingCpus(); changed = true; }
                    else if (d.MBack) { if (humans.Count > 0) { Unready(humans[humans.Count - 1]); } else Back(); changed = true; }
                }
            }
            else
            {
                foreach (var c in humans)
                {
                    var d = InputHub.Get(c.Device);
                    if (d == null) continue;
                    if (!c.Locked)
                    {
                        int before = c.Index;
                        Move(c, d);
                        if (before != c.Index) changed = true;
                        if (d.MConfirm) { Lock(c); changed = true; }
                        else if (d.MBack && c == humans[0] && !humans.Exists(x => x.Locked)) { Back(); return; }
                    }
                    else if (d.MBack) { c.Locked = false; changed = true; Audio.Ui(Sfx.UiBack); }
                }
            }
            if (changed) RefreshAll();
        }

        void Unready(Cursor c)
        {
            ready = false;
            cpuPicked.Clear();
            if (cpuCursor != null) { Object.Destroy(cpuCursor.Marker.gameObject); cpuCursor = null; cpuPick = -1; }
            c.Locked = false;
            Audio.Ui(Sfx.UiBack);
        }

        public override void Back() => GameRoot.I.ShowSetupFor(cfg);
    }

    /// <summary>Forwards pointer-enter events (mouse hover on character tiles).</summary>
    public sealed class TileHover : MonoBehaviour, IPointerEnterHandler
    {
        public System.Action OnEnter;
        public void OnPointerEnter(PointerEventData e) => OnEnter?.Invoke();
    }
}
