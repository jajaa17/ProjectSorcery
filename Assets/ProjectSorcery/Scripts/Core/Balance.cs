using System.Collections.Generic;

namespace ProjectSorcery
{
    /// <summary>Global tuning knobs (validated with the headless balance simulator).</summary>
    public static class Balance
    {
        /// <summary>Multiplies every fighter's health so rounds last long enough to breathe.</summary>
        public const float GlobalHp = 1.35f;

        /// <summary>Absolute raid boss health for a party size.</summary>
        public static float RaidBossHp(int players) => 2200f + 1250f * players;

        /// <summary>Summons hit a little softer and are a little frailer than their raw numbers.</summary>
        public const float MinionDamage = 0.5f, MinionHp = 0.6f, MinionMaxHitstun = 0.2f;

        /// <summary>
        /// Per-fighter (outgoing damage, toughness) multipliers produced by the headless auto-tuner
        /// (thousands of Hard-AI vs Hard-AI duels). Kits stay readable at 1.0; this table evens out the rest.
        /// Fighters not listed use (1, 1).
        /// </summary>
        public static readonly Dictionary<string, (float dmg, float tough)> Tuning = new Dictionary<string, (float dmg, float tough)>
        {
            { "vessel", (0.91f, 0.95f) },
            { "vessel_shinjuku", (0.66f, 0.81f) },
            { "shadow", (0.77f, 0.88f) },
            { "nail", (1.28f, 1.14f) },
            { "blade", (0.92f, 0.95f) },
            { "blade_awakened", (0.72f, 0.84f) },
            { "corpse", (0.93f, 0.97f) },
            { "speech", (1.65f, 1.30f) },
            { "love", (0.56f, 0.71f) },
            { "love_borrowed", (0.56f, 0.70f) },
            { "bestfriend", (1.13f, 1.06f) },
            { "broke", (0.94f, 0.97f) },
            { "broom", (1.63f, 1.52f) },
            { "bullet", (1.62f, 1.28f) },
            { "bloodheir", (1.61f, 1.50f) },
            { "mech", (1.42f, 1.20f) },
            { "choir", (1.59f, 1.35f) },
            { "principal", (1.62f, 1.47f) },
            { "strongest", (0.55f, 0.70f) },
            { "overtime", (1.13f, 1.06f) },
            { "crows", (1.02f, 1.00f) },
            { "simpleedge", (0.94f, 0.98f) },
            { "dollmaker", (0.57f, 0.71f) },
            { "medic", (1.50f, 1.41f) },
            { "warp", (1.65f, 1.28f) },
            { "masks", (1.38f, 1.19f) },
            { "stopper", (1.58f, 1.50f) },
            { "starrage", (0.91f, 0.95f) },
            { "sculptor", (0.62f, 0.74f) },
            { "volcano", (1.21f, 1.10f) },
            { "verdant", (1.42f, 1.19f) },
            { "tidal", (0.94f, 0.96f) },
            { "swarm", (0.77f, 0.83f) },
            { "wheel", (0.58f, 0.72f) },
            { "shepherd", (0.80f, 0.90f) },
            { "stitched", (0.85f, 0.93f) },
            { "killer", (0.89f, 0.94f) },
            { "eldest", (1.35f, 1.16f) },
            { "rot", (0.66f, 0.81f) },
            { "jelly", (1.65f, 1.51f) },
            { "lucky", (1.49f, 1.23f) },
            { "frost", (1.53f, 1.24f) },
            { "seance", (0.84f, 0.92f) },
            { "twins", (1.24f, 1.11f) },
            { "gambler", (1.34f, 1.16f) },
            { "stars", (1.57f, 1.36f) },
            { "judge", (0.84f, 0.92f) },
            { "thunder", (1.12f, 1.06f) },
            { "seraph", (1.75f, 1.52f) },
            { "comedian", (1.07f, 1.04f) },
            { "granite", (1.33f, 1.15f) },
            { "sky", (1.52f, 1.35f) },
            { "receipt", (1.50f, 1.22f) },
            { "pen", (1.11f, 1.06f) },
            { "trails", (0.74f, 0.86f) },
            { "inverted", (0.91f, 0.90f) },
            { "metal", (1.80f, 1.60f) },
            { "rope", (1.42f, 1.20f) },
            { "heart", (1.12f, 1.06f) },
            { "frames", (0.77f, 0.83f) },
            { "elder", (0.98f, 0.99f) },
            { "flameblade", (0.91f, 0.96f) },
            { "palms", (1.68f, 1.41f) },
            { "gaze", (1.64f, 1.53f) },
            { "calamity", (0.58f, 0.72f) },
            { "calamity_shadow", (0.55f, 0.70f) },
            { "calamity_heian", (0.73f, 0.81f) },
            { "vessel_modulo", (0.84f, 0.92f) },
            { "heir", (0.72f, 0.85f) },
            { "sister", (0.90f, 0.95f) },
            { "visitor", (1.61f, 1.27f) },
        };
    }
}
