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
            { "vessel", (1.08f, 1.03f) },
            { "vessel_shinjuku", (0.81f, 0.89f) },
            { "shadow", (0.75f, 0.87f) },
            { "nail", (1.32f, 1.16f) },
            { "blade", (1.04f, 1.00f) },
            { "blade_awakened", (0.65f, 0.80f) },
            { "corpse", (0.81f, 0.90f) },
            { "speech", (1.79f, 1.37f) },
            { "love", (0.60f, 0.74f) },
            { "love_borrowed", (0.55f, 0.70f) },
            { "bestfriend", (1.18f, 1.08f) },
            { "broke", (0.92f, 0.96f) },
            { "broom", (1.67f, 1.54f) },
            { "bullet", (1.73f, 1.32f) },
            { "bloodheir", (1.70f, 1.55f) },
            { "mech", (1.54f, 1.25f) },
            { "choir", (1.62f, 1.36f) },
            { "principal", (1.68f, 1.50f) },
            { "strongest", (0.55f, 0.70f) },
            { "overtime", (1.13f, 1.06f) },
            { "crows", (1.01f, 0.99f) },
            { "simpleedge", (0.97f, 0.99f) },
            { "dollmaker", (0.55f, 0.70f) },
            { "medic", (1.62f, 1.46f) },
            { "warp", (1.36f, 1.16f) },
            { "masks", (1.34f, 1.17f) },
            { "stopper", (1.75f, 1.58f) },
            { "starrage", (0.86f, 0.92f) },
            { "sculptor", (0.58f, 0.71f) },
            { "volcano", (1.17f, 1.08f) },
            { "verdant", (1.40f, 1.22f) },
            { "tidal", (0.97f, 0.98f) },
            { "swarm", (0.69f, 0.78f) },
            { "wheel", (0.55f, 0.70f) },
            { "shepherd", (0.72f, 0.85f) },
            { "stitched", (0.85f, 0.93f) },
            { "killer", (0.84f, 0.92f) },
            { "eldest", (1.33f, 1.15f) },
            { "rot", (0.64f, 0.79f) },
            { "jelly", (1.70f, 1.56f) },
            { "lucky", (1.58f, 1.27f) },
            { "frost", (1.65f, 1.29f) },
            { "seance", (0.84f, 0.92f) },
            { "twins", (1.21f, 1.09f) },
            { "gambler", (1.32f, 1.15f) },
            { "stars", (1.70f, 1.42f) },
            { "judge", (0.77f, 0.88f) },
            { "thunder", (1.01f, 1.01f) },
            { "seraph", (1.80f, 1.60f) },
            { "comedian", (1.06f, 1.04f) },
            { "granite", (1.35f, 1.16f) },
            { "sky", (1.67f, 1.42f) },
            { "receipt", (1.48f, 1.22f) },
            { "pen", (1.00f, 1.00f) },
            { "trails", (0.74f, 0.86f) },
            { "inverted", (0.87f, 0.88f) },
            { "metal", (1.58f, 1.50f) },
            { "rope", (1.46f, 1.22f) },
            { "heart", (1.25f, 1.12f) },
            { "frames", (0.77f, 0.84f) },
            { "elder", (0.96f, 0.98f) },
            { "flameblade", (0.93f, 0.97f) },
            { "palms", (1.75f, 1.44f) },
            { "gaze", (1.77f, 1.59f) },
            { "calamity", (0.68f, 0.78f) },
            { "calamity_shadow", (0.61f, 0.74f) },
            { "calamity_heian", (0.74f, 0.82f) },
            { "vessel_modulo", (1.07f, 1.04f) },
            { "heir", (0.65f, 0.81f) },
            { "sister", (0.84f, 0.92f) },
            { "visitor", (1.76f, 1.32f) },
        };
    }
}
