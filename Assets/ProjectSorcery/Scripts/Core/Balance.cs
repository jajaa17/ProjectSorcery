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
            { "vessel", (1.04f, 1.01f) },
            { "vessel_shinjuku", (0.79f, 0.88f) },
            { "shadow", (0.80f, 0.90f) },
            { "nail", (1.21f, 1.11f) },
            { "blade", (1.01f, 0.99f) },
            { "blade_awakened", (0.75f, 0.86f) },
            { "corpse", (0.84f, 0.92f) },
            { "speech", (1.73f, 1.33f) },
            { "love", (0.60f, 0.74f) },
            { "love_borrowed", (0.58f, 0.71f) },
            { "bestfriend", (1.20f, 1.09f) },
            { "broke", (0.98f, 0.99f) },
            { "broom", (1.71f, 1.56f) },
            { "bullet", (1.73f, 1.32f) },
            { "bloodheir", (1.71f, 1.55f) },
            { "mech", (1.51f, 1.24f) },
            { "choir", (1.46f, 1.29f) },
            { "principal", (1.59f, 1.46f) },
            { "strongest", (0.55f, 0.70f) },
            { "overtime", (1.17f, 1.08f) },
            { "crows", (0.94f, 0.96f) },
            { "simpleedge", (1.02f, 1.02f) },
            { "dollmaker", (0.55f, 0.70f) },
            { "medic", (1.45f, 1.38f) },
            { "warp", (1.21f, 1.10f) },
            { "masks", (1.45f, 1.22f) },
            { "stopper", (1.73f, 1.57f) },
            { "starrage", (0.93f, 0.96f) },
            { "sculptor", (0.59f, 0.72f) },
            { "volcano", (1.17f, 1.08f) },
            { "verdant", (1.70f, 1.35f) },
            { "tidal", (1.02f, 1.00f) },
            { "swarm", (0.62f, 0.74f) },
            { "wheel", (0.64f, 0.75f) },
            { "shepherd", (0.84f, 0.92f) },
            { "stitched", (0.80f, 0.90f) },
            { "killer", (0.92f, 0.96f) },
            { "eldest", (1.40f, 1.18f) },
            { "rot", (0.63f, 0.79f) },
            { "jelly", (1.73f, 1.55f) },
            { "lucky", (1.43f, 1.21f) },
            { "frost", (1.57f, 1.26f) },
            { "seance", (0.88f, 0.94f) },
            { "twins", (1.36f, 1.16f) },
            { "gambler", (1.41f, 1.19f) },
            { "stars", (1.64f, 1.39f) },
            { "judge", (0.77f, 0.88f) },
            { "thunder", (1.15f, 1.08f) },
            { "seraph", (1.79f, 1.55f) },
            { "comedian", (1.12f, 1.07f) },
            { "granite", (1.29f, 1.13f) },
            { "sky", (1.45f, 1.32f) },
            { "receipt", (1.59f, 1.26f) },
            { "pen", (0.98f, 0.99f) },
            { "trails", (0.76f, 0.87f) },
            { "inverted", (0.94f, 0.91f) },
            { "metal", (1.62f, 1.52f) },
            { "rope", (1.34f, 1.17f) },
            { "heart", (1.06f, 1.03f) },
            { "frames", (0.71f, 0.80f) },
            { "elder", (0.90f, 0.95f) },
            { "flameblade", (0.97f, 0.99f) },
            { "palms", (1.58f, 1.37f) },
            { "gaze", (1.80f, 1.60f) },
            { "calamity", (0.63f, 0.75f) },
            { "calamity_shadow", (0.55f, 0.70f) },
            { "calamity_heian", (0.67f, 0.78f) },
            { "vessel_modulo", (0.92f, 0.96f) },
            { "heir", (0.77f, 0.88f) },
            { "sister", (0.92f, 0.96f) },
            { "visitor", (1.72f, 1.31f) },
        };
    }
}
