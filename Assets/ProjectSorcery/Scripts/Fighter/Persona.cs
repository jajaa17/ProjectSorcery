using System.Collections.Generic;

namespace ProjectSorcery
{
    /// <summary>
    /// Per-character body language: how a fighter stands, walks and carries themselves when they aren't
    /// swinging. The strongest keeps a hand in his pocket, the calamity king stands tall and bored, the best
    /// friend folds his arms. Purely visual, except <see cref="Style"/> which picks the moveset.
    /// </summary>
    public sealed class Persona
    {
        public Pose? Idle, Victory;
        public bool Pocket;          // back hand stays in the pocket while idle, walking and doing one-handed moves
        public bool Casual;          // relaxed, upright walk instead of a fighter's run
        public bool Bouncy;          // boxer's bounce in the idle
        public MoveStyle? Style;

        static Pose P(float lean, float uaB, float faB, float uaF, float faF, float thB, float shB, float thF, float shF,
                      float rot = 0f, float head = 0f, float drop = 0f, float wr = 0f)
            => Pose.P(lean, uaB, faB, uaF, faF, thB, shB, thF, shF, rot, head, drop, wr);

        public static readonly Pose PocketArm = Pose.P(0, -15, 40, 0, 0, 0, 0, 0, 0);

        static readonly Dictionary<string, Persona> table = new Dictionary<string, Persona>
        {
            // the strongest: hand in pocket, chin up, unbothered
            ["strongest"] = new Persona { Pocket = true, Casual = true, Idle = P(-3, -15, 40, 22, 55, -12, -14, 12, 2, 0, -10), Victory = P(-6, -15, 40, 150, 190, -12, -14, 12, 2, 0, -14) },
            ["love_borrowed"] = new Persona { Casual = true, Idle = P(-2, 10, 40, 40, 70, -16, -20, 16, 0, 0, -8, 0f, 40f) },
            // the calamity king: tall, arms loose, looking down on everyone
            ["calamity"] = new Persona { Casual = true, Style = MoveStyle.Agile, Idle = P(-5, -4, 12, 8, 22, -14, -16, 14, 0, 0, -14), Victory = P(-8, -10, 30, 150, 170, -14, -16, 14, 0, 0, -18) },
            ["calamity_shadow"] = new Persona { Casual = true, Style = MoveStyle.Agile, Idle = P(-5, -4, 12, 8, 22, -14, -16, 14, 0, 0, -14) },
            ["calamity_heian"] = new Persona { Casual = true, Style = MoveStyle.Brute, Idle = P(-4, -20, 10, 20, 30, -18, -20, 18, 0, 0, -12) },
            ["calamity_boss"] = new Persona { Casual = true, Style = MoveStyle.Brute, Idle = P(-4, -20, 10, 20, 30, -18, -20, 18, 0, 0, -12) },
            ["vessel"] = new Persona { Bouncy = true },
            ["vessel_modulo"] = new Persona { Style = MoveStyle.Martial, Idle = P(2, 30, 150, 75, 110, -26, -40, 30, 0, 0, -4) },
            ["shadow"] = new Persona { Pocket = true, Style = MoveStyle.Martial, Idle = P(0, -15, 40, 60, 140, -18, -24, 20, 0, 0, -4) },
            ["bestfriend"] = new Persona { Idle = P(0, 22, 100, 18, 104, -24, -30, 26, 0, 0, -8), Victory = P(-6, 22, 100, 18, 104, -20, -26, 22, 0, 0, -16) },
            ["dollmaker"] = new Persona { Casual = true, Idle = P(0, 22, 100, 18, 104, -18, -22, 20, 0, 0, -4) },
            ["overtime"] = new Persona { Casual = true, Idle = P(0, 15, 140, 15, 40, -16, -20, 18, 0, 0, -2, 0f, 50f) },
            ["killer"] = new Persona { Casual = true, Idle = P(2, -10, 30, 40, 60, -16, -20, 18, 0, 0, -6, 0f, 70f) },
            ["medic"] = new Persona { Pocket = true, Casual = true, Idle = P(0, -15, 40, 30, 150, -12, -14, 12, 2, 0, -2) },
            ["stitched"] = new Persona { Casual = true, Idle = P(-2, 22, 100, 18, 104, -12, -14, 12, 2, 0, -8) },
            ["shepherd"] = new Persona { Casual = true, Idle = P(-2, 22, 100, 18, 104, -12, -14, 12, 2, 0, -6, 0f, 60f) },
            ["sculptor"] = new Persona { Casual = true, Idle = P(2, -10, 20, 10, 30, -16, -20, 16, 0, 0, -10) },
            ["gambler"] = new Persona { Bouncy = true },
            ["judge"] = new Persona { Casual = true, Idle = P(0, 10, 60, 30, 80, -14, -18, 16, 0, 0, -2) },
            ["starrage"] = new Persona { Casual = true, Idle = P(-2, -15, 40, 30, 70, -14, -16, 14, 0, 0, -8) },
            ["eldest"] = new Persona { Idle = P(0, 0, 20, 40, 90, -20, -26, 22, 0, 0, -4) },
            ["heir"] = new Persona { Idle = P(4, 30, 110, 50, 80, -26, -40, 30, -4, 0, -2, 0.05f, 40f) },
            ["blade_awakened"] = new Persona { Casual = true, Idle = P(2, -10, 30, 40, 70, -18, -22, 20, 0, 0, -4, 0f, 40f) },
        };

        static readonly Persona none = new Persona();

        public static Persona For(CharacterDef d) => table.TryGetValue(d.Id, out var p) ? p : none;
    }
}
