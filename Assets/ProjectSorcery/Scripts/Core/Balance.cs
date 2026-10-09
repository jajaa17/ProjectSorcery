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
            { "vessel", (1.23f, 1.11f) },
            { "vessel_shinjuku", (0.73f, 0.85f) },
            { "shadow", (0.72f, 0.85f) },
            { "nail", (1.31f, 1.15f) },
            { "blade", (1.22f, 1.10f) },
            { "blade_awakened", (0.84f, 0.91f) },
            { "corpse", (0.62f, 0.79f) },
            { "speech", (1.79f, 1.36f) },
            { "love", (0.55f, 0.70f) },
            { "love_borrowed", (0.55f, 0.70f) },
            { "bestfriend", (1.40f, 1.18f) },
            { "broke", (0.95f, 0.97f) },
            { "broom", (1.80f, 1.60f) },
            { "bullet", (1.78f, 1.34f) },
            { "bloodheir", (1.79f, 1.58f) },
            { "mech", (1.55f, 1.25f) },
            { "choir", (1.80f, 1.43f) },
            { "principal", (1.80f, 1.55f) },
            { "strongest", (0.55f, 0.70f) },
            { "overtime", (1.26f, 1.12f) },
            { "crows", (0.93f, 0.96f) },
            { "simpleedge", (0.98f, 1.00f) },
            { "dollmaker", (0.56f, 0.70f) },
            { "medic", (1.76f, 1.53f) },
            { "warp", (1.17f, 1.08f) },
            { "masks", (1.04f, 1.03f) },
            { "stopper", (1.80f, 1.60f) },
            { "starrage", (0.81f, 0.90f) },
            { "sculptor", (0.57f, 0.71f) },
            { "volcano", (1.15f, 1.07f) },
            { "verdant", (1.51f, 1.23f) },
            { "tidal", (0.90f, 0.94f) },
            { "swarm", (0.55f, 0.70f) },
            { "wheel", (0.55f, 0.70f) },
            { "shepherd", (0.69f, 0.83f) },
            { "stitched", (0.88f, 0.94f) },
            { "killer", (1.05f, 1.02f) },
            { "eldest", (1.35f, 1.16f) },
            { "rot", (0.67f, 0.82f) },
            { "jelly", (1.76f, 1.56f) },
            { "lucky", (1.50f, 1.23f) },
            { "frost", (1.70f, 1.31f) },
            { "seance", (0.79f, 0.89f) },
            { "twins", (1.19f, 1.09f) },
            { "gambler", (1.36f, 1.17f) },
            { "stars", (1.77f, 1.44f) },
            { "judge", (0.72f, 0.85f) },
            { "thunder", (0.72f, 0.85f) },
            { "seraph", (1.80f, 1.54f) },
            { "comedian", (0.95f, 0.98f) },
            { "granite", (1.48f, 1.21f) },
            { "sky", (1.77f, 1.45f) },
            { "receipt", (1.47f, 1.21f) },
            { "pen", (1.00f, 1.00f) },
            { "trails", (0.67f, 0.82f) },
            { "inverted", (0.56f, 0.71f) },
            { "metal", (1.80f, 1.60f) },
            { "rope", (1.63f, 1.28f) },
            { "heart", (1.21f, 1.10f) },
            { "frames", (0.58f, 0.72f) },
            { "elder", (0.83f, 0.91f) },
            { "flameblade", (0.92f, 0.96f) },
            { "palms", (1.78f, 1.45f) },
            { "gaze", (1.80f, 1.60f) },
            { "calamity", (0.55f, 0.70f) },
            { "calamity_shadow", (0.55f, 0.70f) },
            { "calamity_heian", (0.55f, 0.70f) },
            { "vessel_modulo", (1.04f, 1.02f) },
            { "heir", (0.73f, 0.86f) },
            { "sister", (0.85f, 0.92f) },
            { "visitor", (1.74f, 1.32f) },
        };
    }
}
