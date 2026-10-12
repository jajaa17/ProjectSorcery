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
            { "vessel", (1.13f, 1.05f) },
            { "vessel_shinjuku", (0.83f, 0.90f) },
            { "shadow", (0.79f, 0.89f) },
            { "nail", (1.30f, 1.15f) },
            { "blade", (1.01f, 0.99f) },
            { "blade_awakened", (0.72f, 0.84f) },
            { "corpse", (0.77f, 0.88f) },
            { "speech", (1.73f, 1.35f) },
            { "love", (0.59f, 0.74f) },
            { "love_borrowed", (0.55f, 0.70f) },
            { "bestfriend", (1.12f, 1.05f) },
            { "broke", (0.95f, 0.97f) },
            { "broom", (1.73f, 1.57f) },
            { "bullet", (1.75f, 1.33f) },
            { "bloodheir", (1.73f, 1.56f) },
            { "mech", (1.50f, 1.23f) },
            { "choir", (1.59f, 1.35f) },
            { "principal", (1.68f, 1.50f) },
            { "strongest", (0.55f, 0.70f) },
            { "overtime", (1.08f, 1.04f) },
            { "crows", (0.99f, 0.98f) },
            { "simpleedge", (0.92f, 0.96f) },
            { "dollmaker", (0.56f, 0.71f) },
            { "medic", (1.64f, 1.47f) },
            { "warp", (1.32f, 1.14f) },
            { "masks", (1.32f, 1.16f) },
            { "stopper", (1.71f, 1.56f) },
            { "starrage", (0.92f, 0.95f) },
            { "sculptor", (0.60f, 0.72f) },
            { "volcano", (1.22f, 1.10f) },
            { "verdant", (1.42f, 1.23f) },
            { "tidal", (0.94f, 0.96f) },
            { "swarm", (0.74f, 0.81f) },
            { "wheel", (0.55f, 0.70f) },
            { "shepherd", (0.75f, 0.87f) },
            { "stitched", (0.85f, 0.93f) },
            { "killer", (0.92f, 0.96f) },
            { "eldest", (1.38f, 1.17f) },
            { "rot", (0.67f, 0.81f) },
            { "jelly", (1.72f, 1.57f) },
            { "lucky", (1.54f, 1.25f) },
            { "frost", (1.62f, 1.28f) },
            { "seance", (0.82f, 0.91f) },
            { "twins", (1.21f, 1.09f) },
            { "gambler", (1.26f, 1.12f) },
            { "stars", (1.73f, 1.43f) },
            { "judge", (0.75f, 0.87f) },
            { "thunder", (0.97f, 0.99f) },
            { "seraph", (1.69f, 1.55f) },
            { "comedian", (1.07f, 1.05f) },
            { "granite", (1.35f, 1.16f) },
            { "sky", (1.63f, 1.40f) },
            { "receipt", (1.53f, 1.24f) },
            { "pen", (1.02f, 1.01f) },
            { "trails", (0.74f, 0.86f) },
            { "inverted", (0.83f, 0.86f) },
            { "metal", (1.56f, 1.49f) },
            { "rope", (1.57f, 1.26f) },
            { "heart", (1.26f, 1.12f) },
            { "frames", (0.80f, 0.86f) },
            { "elder", (1.01f, 1.01f) },
            { "flameblade", (0.93f, 0.97f) },
            { "palms", (1.60f, 1.38f) },
            { "gaze", (1.80f, 1.60f) },
            { "calamity", (0.69f, 0.79f) },
            { "calamity_shadow", (0.60f, 0.73f) },
            { "calamity_heian", (0.70f, 0.80f) },
            { "vessel_modulo", (1.05f, 1.03f) },
            { "heir", (0.73f, 0.86f) },
            { "sister", (0.86f, 0.93f) },
            { "visitor", (1.69f, 1.29f) },
        };
    }
}
