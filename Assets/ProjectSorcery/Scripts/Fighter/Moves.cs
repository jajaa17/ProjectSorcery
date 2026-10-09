using UnityEngine;

namespace ProjectSorcery
{
    /// <summary>Shared normal attacks. Character stats (Power, weapon) scale them at runtime.</summary>
    public static class Moves
    {
        public static readonly AttackDef[] LightChain =
        {
            new AttackDef { Name = "Jab", Pose = FPose.Jab, Startup = 0.055f, Active = 0.06f, Recovery = 0.13f, Damage = 26f, Knockback = new Vector2(1.6f, 0.6f), Hitstun = 0.32f, Lunge = new Vector2(3.5f, 0f) },
            new AttackDef { Name = "Cross", Pose = FPose.Cross, Startup = 0.06f, Active = 0.06f, Recovery = 0.14f, Damage = 28f, Knockback = new Vector2(1.9f, 0.6f), Hitstun = 0.34f, Lunge = new Vector2(3.5f, 0f) },
            new AttackDef { Name = "Hook", Pose = FPose.Hook, Startup = 0.065f, Active = 0.06f, Recovery = 0.15f, Damage = 30f, Knockback = new Vector2(2.2f, 1.0f), Hitstun = 0.36f, Lunge = new Vector2(4f, 0f) },
            new AttackDef { Name = "Roundhouse", Pose = FPose.Kick, Startup = 0.09f, Active = 0.08f, Recovery = 0.26f, Damage = 44f, Knockback = new Vector2(7.5f, 4f), Hitstun = 0.5f, Hitstop = 0.08f, Offset = new Vector2(0.95f, 1.1f), Radius = 0.62f, Lunge = new Vector2(5f, 0f) },
        };

        public static readonly AttackDef Sweep = new AttackDef
        {
            Name = "Low Sweep", Pose = FPose.Sweep, Startup = 0.08f, Active = 0.08f, Recovery = 0.24f, Damage = 34f,
            Knockback = new Vector2(2f, 5.5f), Hitstun = 0.55f, Offset = new Vector2(1.0f, 0.3f), Radius = 0.6f, Lunge = new Vector2(4f, 0f)
        };

        public static readonly AttackDef Heavy = new AttackDef
        {
            Name = "Heavy Strike", Pose = FPose.HeavyPunch, Startup = 0.16f, Active = 0.08f, Recovery = 0.3f, Damage = 70f,
            Knockback = new Vector2(10.5f, 4.5f), Hitstun = 0.6f, Hitstop = 0.1f, Flags = HitFlags.Heavy, Offset = new Vector2(0.9f, 1.3f),
            Radius = 0.65f, Lunge = new Vector2(7f, 0f), CeGain = 6f
        };

        public static readonly AttackDef Launcher = new AttackDef
        {
            Name = "Rising Uppercut", Pose = FPose.Uppercut, Startup = 0.12f, Active = 0.09f, Recovery = 0.28f, Damage = 55f,
            Knockback = new Vector2(1.4f, 15.5f), Hitstun = 0.75f, Hitstop = 0.09f, Flags = HitFlags.Heavy | HitFlags.Launch,
            Offset = new Vector2(0.7f, 1.7f), Radius = 0.62f, Lunge = new Vector2(3f, 0f), CeGain = 6f
        };

        public static readonly AttackDef[] AirChain =
        {
            new AttackDef { Name = "Air Kick", Pose = FPose.AirKick, Startup = 0.05f, Active = 0.08f, Recovery = 0.12f, Damage = 24f, Knockback = new Vector2(2f, 3.2f), Hitstun = 0.4f, Air = true, Lunge = Vector2.zero, Offset = new Vector2(0.8f, 0.9f) },
            new AttackDef { Name = "Air Slash", Pose = FPose.AirSlash, Startup = 0.06f, Active = 0.08f, Recovery = 0.14f, Damage = 28f, Knockback = new Vector2(2.4f, 3.4f), Hitstun = 0.42f, Air = true, Lunge = Vector2.zero },
        };

        public static readonly AttackDef AirHeavy = new AttackDef
        {
            Name = "Air Smash", Pose = FPose.HeavyPunch, Startup = 0.12f, Active = 0.08f, Recovery = 0.22f, Damage = 55f,
            Knockback = new Vector2(9f, 2f), Hitstun = 0.55f, Hitstop = 0.09f, Flags = HitFlags.Heavy, Air = true, Lunge = new Vector2(2f, 0f), CeGain = 5f
        };

        public static readonly AttackDef DiveKick = new AttackDef
        {
            Name = "Meteor Heel", Pose = FPose.DiveKick, Startup = 0.1f, Active = 0.12f, Recovery = 0.2f, Damage = 50f,
            Knockback = new Vector2(3f, -16f), Hitstun = 0.6f, Hitstop = 0.1f, Flags = HitFlags.Heavy | HitFlags.Spike, Air = true,
            Offset = new Vector2(0.5f, 0.2f), Radius = 0.7f, Lunge = new Vector2(4f, -10f), CeGain = 5f
        };
    }
}
