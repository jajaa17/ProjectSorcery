using UnityEngine;

namespace ProjectSorcery
{
    /// <summary>Fighting style: changes idle, guard, locomotion, strike shapes and how springy the body is.</summary>
    public enum Stance { Brawler, Elegant, Swordsman, Brute, Feral, Caster, Agile, Floaty, Mechanical, Wrestler, Martial }

    /// <summary>
    /// Stick pose in "facing right" space. Limb angles are absolute degrees measured from straight down,
    /// positive = rotating toward the facing direction (90 = pointing forward, 180 = straight up).
    /// </summary>
    public struct Pose
    {
        public float Lean;                       // torso from vertical, + = forward
        public float UaB, FaB, UaF, FaF;         // back / front arm: upper, fore
        public float ThB, ShB, ThF, ShF;         // back / front leg: thigh, shin
        public float Rot;                        // whole-body rotation about the hip
        public float Head;                       // head tilt
        public float Drop;                       // extra hip drop (crouch)

        public static Pose Lerp(in Pose a, in Pose b, float t)
        {
            return new Pose
            {
                Lean = Mathf.Lerp(a.Lean, b.Lean, t),
                UaB = Mathf.LerpAngle(a.UaB, b.UaB, t), FaB = Mathf.LerpAngle(a.FaB, b.FaB, t),
                UaF = Mathf.LerpAngle(a.UaF, b.UaF, t), FaF = Mathf.LerpAngle(a.FaF, b.FaF, t),
                ThB = Mathf.Lerp(a.ThB, b.ThB, t), ShB = Mathf.Lerp(a.ShB, b.ShB, t),
                ThF = Mathf.Lerp(a.ThF, b.ThF, t), ShF = Mathf.Lerp(a.ShF, b.ShF, t),
                Rot = Mathf.Lerp(a.Rot, b.Rot, t), Head = Mathf.Lerp(a.Head, b.Head, t), Drop = Mathf.Lerp(a.Drop, b.Drop, t),
            };
        }

        public static Pose P(float lean, float uaB, float faB, float uaF, float faF, float thB, float shB, float thF, float shF, float rot = 0f, float head = 0f, float drop = 0f)
            => new Pose { Lean = lean, UaB = uaB, FaB = faB, UaF = uaF, FaF = faF, ThB = thB, ShB = shB, ThF = thF, ShF = shF, Rot = rot, Head = head, Drop = drop };
    }

    public static class Poses
    {
        public static readonly Pose Idle = Pose.P(8, 22, 118, 42, 142, -18, -32, 22, -4);
        public static readonly Pose Jump = Pose.P(6, 120, 150, 140, 165, 30, -40, 70, -10);
        public static readonly Pose Fall = Pose.P(-2, -100, -80, 100, 120, -10, -32, 32, 2);
        public static readonly Pose Block = Pose.P(-4, 62, 168, 72, 165, -26, -38, 28, 4, 0, 0, 0.08f);
        public static readonly Pose Hurt = Pose.P(-26, -95, -60, -62, -30, -12, -6, 26, 10, 0, -25);
        public static readonly Pose Down = Pose.P(0, 160, 175, 30, 60, -5, -5, 10, 0, -88, 0);
        public static readonly Pose Dash = Pose.P(38, -72, -50, -52, -30, -42, -72, 62, 20);
        public static readonly Pose Crouch = Pose.P(20, 30, 120, 60, 140, -55, -110, 70, -30, 0, 0, 0.25f);
        public static readonly Pose Victory = Pose.P(-6, 15, 110, 168, 176, -14, -20, 16, 0, 0, -10);
        public static readonly Pose Channel = Pose.P(0, -40, -70, 40, 70, -18, -25, 18, 0);
        public static readonly Pose HandSign = Pose.P(2, 46, 156, 52, 150, -22, -30, 24, 0);
        public static readonly Pose Chant = Pose.P(-6, 15, 100, 40, 162, -18, -26, 20, 0, 0, 8);
        public static readonly Pose Taunt = Pose.P(-4, 30, -30, 30, -20, -16, -20, 16, 0);

        // strike frames
        public static readonly Pose Jab = Pose.P(16, 18, 118, 90, 90, -26, -40, 32, 0);
        public static readonly Pose JabWind = Pose.P(10, 20, 120, 38, 152, -20, -34, 26, -2);
        public static readonly Pose Cross = Pose.P(24, 90, 90, 30, 140, -34, -48, 36, 4);
        public static readonly Pose Hook = Pose.P(22, 20, 110, 88, 150, -30, -44, 34, 2);
        public static readonly Pose Kick = Pose.P(-16, -60, -30, 30, 100, -10, -16, 100, 96);
        public static readonly Pose KickWind = Pose.P(-6, -30, -20, 40, 120, -14, -20, 60, -30);
        public static readonly Pose Uppercut = Pose.P(4, 10, 80, 152, 176, -22, -22, 10, 0);
        public static readonly Pose UpperWind = Pose.P(26, 30, 110, 22, 100, -44, -84, 52, -22, 0, 0, 0.2f);
        public static readonly Pose Sweep = Pose.P(40, -20, 40, 60, 50, -62, -122, 95, 90, 0, 0, 0.35f);
        public static readonly Pose HeavyWind = Pose.P(-12, 40, 120, -62, -24, -18, -30, 30, 0);
        public static readonly Pose Heavy = Pose.P(32, -50, -20, 92, 92, -46, -54, 52, 12);
        public static readonly Pose AirKick = Pose.P(-8, 120, 150, 140, 160, 20, -40, 98, 88);
        public static readonly Pose AirSlashWind = Pose.P(0, 60, 120, 170, 182, 10, -40, 60, -10);
        public static readonly Pose AirSlash = Pose.P(20, -30, -10, 70, 58, 10, -30, 50, -10);
        public static readonly Pose DiveKick = Pose.P(30, 140, 170, 120, 150, 90, -20, 32, 38);
        public static readonly Pose Point = Pose.P(6, 18, 110, 92, 92, -20, -30, 24, 0);
        public static readonly Pose Palm = Pose.P(12, 80, 86, 88, 88, -26, -40, 28, 2);
        public static readonly Pose TwoPalm = Pose.P(14, 92, 92, 94, 94, -32, -48, 34, 6, 0, 0, 0.1f);
        public static readonly Pose Raise = Pose.P(-4, 164, 176, 168, 178, -20, -28, 20, 0);
        public static readonly Pose ThrowWind = Pose.P(-14, 40, 100, 170, 230, -16, -24, 26, 0);
        public static readonly Pose Throw = Pose.P(26, -20, 10, 80, 62, -32, -50, 36, 6);
        public static readonly Pose SlashWind = Pose.P(-6, 30, 110, 150, 172, -18, -28, 26, 0);
        public static readonly Pose Slash = Pose.P(26, -10, 40, 40, 22, -40, -56, 46, 10);
        public static readonly Pose StabWind = Pose.P(-4, 30, 120, 30, 20, -20, -30, 26, 0);
        public static readonly Pose Stab = Pose.P(30, -30, 10, 95, 92, -48, -58, 56, 14);
        public static readonly Pose Slam = Pose.P(42, 30, 20, 36, 18, -50, -100, 62, -18, 0, 0, 0.3f);
        public static readonly Pose ClapWind = Pose.P(-4, -100, -90, 110, 100, -18, -26, 20, 0);
        public static readonly Pose Clap = Pose.P(6, 68, 150, 72, 152, -20, -28, 22, 0);
        public static readonly Pose Grab = Pose.P(22, 40, 90, 96, 96, -32, -48, 36, 4);
        public static readonly Pose Bow = Pose.P(4, 82, 176, 90, 90, -24, -34, 26, 0);
        public static readonly Pose Shoot = Pose.P(6, 84, 100, 94, 94, -22, -30, 24, 0);
        public static readonly Pose Spin = Pose.P(10, -90, -90, 90, 90, -20, -30, 30, 0);

        public static Pose Run(float phase)
        {
            float s = Mathf.Sin(phase), s2 = Mathf.Sin(phase + Mathf.PI);
            float thF = 48f * s, thB = 48f * s2;
            float shF = thF - 18f - 62f * Mathf.Max(0f, Mathf.Sin(phase - 0.9f));
            float shB = thB - 18f - 62f * Mathf.Max(0f, Mathf.Sin(phase + Mathf.PI - 0.9f));
            float armF = -45f * s, armB = -45f * s2;
            return Pose.P(18f + 3f * Mathf.Sin(phase * 2f), armB, armB + 95f, armF, armF + 95f, thB, shB, thF, shF);
        }

        /// <summary>Windup and strike frames for a pose id.</summary>
        public static void Frames(FPose p, out Pose wind, out Pose strike)
        {
            switch (p)
            {
                case FPose.Jab: wind = JabWind; strike = Jab; break;
                case FPose.Cross: wind = JabWind; strike = Cross; break;
                case FPose.Hook: wind = JabWind; strike = Hook; break;
                case FPose.Kick: wind = KickWind; strike = Kick; break;
                case FPose.Uppercut: wind = UpperWind; strike = Uppercut; break;
                case FPose.Sweep: wind = Crouch; strike = Sweep; break;
                case FPose.HeavyPunch: wind = HeavyWind; strike = Heavy; break;
                case FPose.AirKick: wind = Jump; strike = AirKick; break;
                case FPose.AirSlash: wind = AirSlashWind; strike = AirSlash; break;
                case FPose.DiveKick: wind = Jump; strike = DiveKick; break;
                case FPose.Point: wind = Idle; strike = Point; break;
                case FPose.Palm: wind = HeavyWind; strike = Palm; break;
                case FPose.TwoPalm: wind = HandSign; strike = TwoPalm; break;
                case FPose.HandSign: wind = Idle; strike = HandSign; break;
                case FPose.Raise: wind = Crouch; strike = Raise; break;
                case FPose.Throw: wind = ThrowWind; strike = Throw; break;
                case FPose.Slash: wind = SlashWind; strike = Slash; break;
                case FPose.Stab: wind = StabWind; strike = Stab; break;
                case FPose.Spin: wind = Idle; strike = Spin; break;
                case FPose.Slam: wind = Raise; strike = Slam; break;
                case FPose.Chant: wind = Idle; strike = Chant; break;
                case FPose.Channel: wind = Idle; strike = Channel; break;
                case FPose.Clap: wind = ClapWind; strike = Clap; break;
                case FPose.Crouch: wind = Idle; strike = Crouch; break;
                case FPose.Grab: wind = JabWind; strike = Grab; break;
                case FPose.Bow: wind = Idle; strike = Bow; break;
                case FPose.Shoot: wind = Idle; strike = Shoot; break;
                case FPose.Dash: wind = Dash; strike = Dash; break;
                case FPose.Block: wind = Block; strike = Block; break;
                case FPose.Victory: wind = Victory; strike = Victory; break;
                case FPose.Taunt: wind = Taunt; strike = Taunt; break;
                default: wind = Idle; strike = Jab; break;
            }
        }
    }
}
