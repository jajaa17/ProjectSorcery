namespace ProjectSorcery
{
    /// <summary>
    /// Deterministic xorshift RNG used by the simulation. Everything that can change the
    /// outcome of a match (AI, crits, gambles, verdicts) must use this - never UnityEngine.Random -
    /// so online lockstep peers stay in sync. Visual-only code may use UnityEngine.Random.
    /// </summary>
    public sealed class Rng
    {
        uint s;
        public Rng(uint seed) { s = seed == 0 ? 0x9E3779B9u : seed; }
        public uint State => s;

        public uint Next()
        {
            uint x = s;
            x ^= x << 13; x ^= x >> 17; x ^= x << 5;
            s = x;
            return x;
        }

        /// <summary>[0,1)</summary>
        public float Value() => (Next() >> 8) * (1f / 16777216f);
        public float Range(float a, float b) => a + (b - a) * Value();
        /// <summary>[a,b)</summary>
        public int Range(int a, int b) => b <= a ? a : a + (int)(Next() % (uint)(b - a));
        public bool Chance(float p) => Value() < p;
        public float Sign() => (Next() & 1) == 0 ? -1f : 1f;
    }
}
