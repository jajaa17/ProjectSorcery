using System.Collections.Generic;
using System.IO;

namespace ProjectSorcery
{
    public enum GameMode { Versus, Survival, Raid, Training }
    public enum TeamLayout { Duel, TwoVsOne, TwoVsTwo, FreeForAll }
    public enum SlotControl { None, Local, Remote, Cpu }
    public enum Difficulty { Dummy, Easy, Medium, Hard, Nightmare }

    public sealed class SlotConfig
    {
        public SlotControl Control = SlotControl.Cpu;
        public int Device = -1;           // local device id (InputHub)
        public int Peer = -1;             // online peer id (0 = host)
        public Difficulty Diff = Difficulty.Medium;
        public int Character;
        public int Team;
        public string Name = "";
        public bool Ready;

        public SlotConfig Copy() => (SlotConfig)MemberwiseClone();
    }

    public sealed class MatchConfig
    {
        public GameMode Mode = GameMode.Versus;
        public TeamLayout Layout = TeamLayout.Duel;
        public List<SlotConfig> Slots = new List<SlotConfig>();
        public int Arena;
        public int RoundsToWin = 2;
        public float RoundTime = 99f;
        public uint Seed = 12345;
        public bool Online;
        public bool Demo;                 // attract-mode fight behind the main menu
        public bool InfiniteCe;           // training
        public int InputDelay = 3;

        public static int SlotCount(TeamLayout l) => l == TeamLayout.Duel ? 2 : l == TeamLayout.FreeForAll ? 3 : l == TeamLayout.TwoVsOne ? 3 : 4;

        /// <summary>Team for a slot index in a layout.</summary>
        public static int TeamFor(TeamLayout l, int slot)
        {
            switch (l)
            {
                case TeamLayout.Duel: return slot;
                case TeamLayout.TwoVsOne: return slot < 2 ? 0 : 1;
                case TeamLayout.TwoVsTwo: return slot < 2 ? 0 : 1;
                default: return slot;
            }
        }

        public byte[] Serialize()
        {
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                w.Write((byte)Mode); w.Write((byte)Layout); w.Write((byte)Arena); w.Write((byte)RoundsToWin);
                w.Write(RoundTime); w.Write(Seed); w.Write(Online); w.Write(InfiniteCe); w.Write((byte)InputDelay);
                w.Write((byte)Slots.Count);
                foreach (var s in Slots)
                {
                    w.Write((byte)s.Control); w.Write((sbyte)s.Device); w.Write((sbyte)s.Peer); w.Write((byte)s.Diff);
                    w.Write((short)s.Character); w.Write((byte)s.Team); w.Write(s.Name ?? ""); w.Write(s.Ready);
                }
                return ms.ToArray();
            }
        }

        public static MatchConfig Deserialize(byte[] data, int offset, int count)
        {
            var c = new MatchConfig();
            using (var ms = new MemoryStream(data, offset, count))
            using (var r = new BinaryReader(ms))
            {
                c.Mode = (GameMode)r.ReadByte(); c.Layout = (TeamLayout)r.ReadByte(); c.Arena = r.ReadByte(); c.RoundsToWin = r.ReadByte();
                c.RoundTime = r.ReadSingle(); c.Seed = r.ReadUInt32(); c.Online = r.ReadBoolean(); c.InfiniteCe = r.ReadBoolean(); c.InputDelay = r.ReadByte();
                int n = r.ReadByte();
                for (int i = 0; i < n; i++)
                {
                    var s = new SlotConfig
                    {
                        Control = (SlotControl)r.ReadByte(), Device = r.ReadSByte(), Peer = r.ReadSByte(), Diff = (Difficulty)r.ReadByte(),
                        Character = r.ReadInt16(), Team = r.ReadByte(), Name = r.ReadString(), Ready = r.ReadBoolean()
                    };
                    c.Slots.Add(s);
                }
            }
            return c;
        }

        public MatchConfig Copy()
        {
            var c = (MatchConfig)MemberwiseClone();
            c.Slots = new List<SlotConfig>();
            foreach (var s in Slots) c.Slots.Add(s.Copy());
            return c;
        }
    }
}
