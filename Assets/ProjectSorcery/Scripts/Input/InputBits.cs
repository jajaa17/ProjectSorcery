namespace ProjectSorcery
{
    /// <summary>
    /// A player's input for one simulation tick is 12 bits. That is all that crosses the network
    /// during online play, so it must fully describe intent. Presses are derived from bit edges.
    /// </summary>
    public static class IB
    {
        public const ushort Left = 1 << 0, Right = 1 << 1, Jump = 1 << 2, Down = 1 << 3,
                            Light = 1 << 4, Heavy = 1 << 5, Block = 1 << 6, Dash = 1 << 7,
                            S1 = 1 << 8, S2 = 1 << 9, S3 = 1 << 10, Ult = 1 << 11;
        public const ushort AttackMask = Light | Heavy | S1 | S2 | S3 | Ult;

        public static ushort Slot(int slot) => slot == 0 ? S1 : slot == 1 ? S2 : slot == 2 ? S3 : Ult;
    }

    /// <summary>Per-tick decoded input for a fighter.</summary>
    public struct InputFrame
    {
        public ushort Bits, Prev;

        public bool Held(ushort b) => (Bits & b) != 0;
        public bool Pressed(ushort b) => (Bits & b) != 0 && (Prev & b) == 0;
        public bool Released(ushort b) => (Bits & b) == 0 && (Prev & b) != 0;
        public float X => (Held(IB.Right) ? 1f : 0f) - (Held(IB.Left) ? 1f : 0f);
        public bool Down => Held(IB.Down);
        /// <summary>Number of fresh button presses this tick (used for domain clash mashing).</summary>
        public int MashCount
        {
            get
            {
                int fresh = Bits & ~Prev & IB.AttackMask;
                int c = 0;
                while (fresh != 0) { c += fresh & 1; fresh >>= 1; }
                return c;
            }
        }

        public void Push(ushort bits) { Prev = Bits; Bits = bits; }
    }
}
