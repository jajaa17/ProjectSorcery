using UnityEngine;

namespace ProjectSorcery
{
    /// <summary>Fighting style: changes idle, guard, locomotion, movesets and how springy the body is.</summary>
    public enum Stance { Brawler, Elegant, Swordsman, Brute, Feral, Caster, Agile, Floaty, Mechanical, Wrestler, Martial }

    /// <summary>
    /// Authoring pose in "facing right" space. Limb angles are absolute degrees measured from straight down,
    /// positive = rotating toward the facing direction (90 = pointing forward, 180 = straight up, -90 = pointing back).
    /// Poses are converted to <see cref="Kf"/> (hand positions + IK bend) before interpolation, so fists travel in
    /// straight lines between keys instead of swinging in arcs.
    /// </summary>
    public struct Pose
    {
        public float Lean;                       // torso from vertical, + = forward
        public float UaB, FaB, UaF, FaF;         // back / front arm: upper, fore
        public float ThB, ShB, ThF, ShF;         // back / front leg: thigh, shin
        public float Rot;                        // whole-body rotation about the hip (+ = forward flip)
        public float Head;                       // head tilt
        public float Drop;                       // hip drop (crouch, + = lower)
        public float Wr;                         // weapon angle relative to the front forearm
        public float Hx;                         // visual hip shift along facing (step-ins)
        public float Tn;                         // body turn: 0 = facing, 0.5 = back to the camera's other side, 1 = full spin

        public static Pose P(float lean, float uaB, float faB, float uaF, float faF, float thB, float shB, float thF, float shF,
                             float rot = 0f, float head = 0f, float drop = 0f, float wr = 0f, float hx = 0f, float tn = 0f)
            => new Pose { Lean = lean, UaB = uaB, FaB = faB, UaF = uaF, FaF = faF, ThB = thB, ShB = shB, ThF = thF, ShF = shF, Rot = rot, Head = head, Drop = drop, Wr = wr, Hx = hx, Tn = tn };

        /// <summary>Plain (unwrapped) interpolation: angles travel the way they are authored.</summary>
        public static Pose Lerp(in Pose a, in Pose b, float t)
        {
            return new Pose
            {
                Lean = a.Lean + (b.Lean - a.Lean) * t,
                UaB = a.UaB + (b.UaB - a.UaB) * t, FaB = a.FaB + (b.FaB - a.FaB) * t,
                UaF = a.UaF + (b.UaF - a.UaF) * t, FaF = a.FaF + (b.FaF - a.FaF) * t,
                ThB = a.ThB + (b.ThB - a.ThB) * t, ShB = a.ShB + (b.ShB - a.ShB) * t,
                ThF = a.ThF + (b.ThF - a.ThF) * t, ShF = a.ShF + (b.ShF - a.ShF) * t,
                Rot = a.Rot + (b.Rot - a.Rot) * t, Head = a.Head + (b.Head - a.Head) * t, Drop = a.Drop + (b.Drop - a.Drop) * t,
                Wr = a.Wr + (b.Wr - a.Wr) * t, Hx = a.Hx + (b.Hx - a.Hx) * t, Tn = a.Tn + (b.Tn - a.Tn) * t,
            };
        }

        /// <summary>Same pose with the near and far arms swapped (southpaw / alternate lead).</summary>
        public Pose SwapArms()
        {
            var p = this;
            p.UaB = UaF; p.FaB = FaF; p.UaF = UaB; p.FaF = FaB;
            return p;
        }

        /// <summary>Same pose with the legs swapped (for mirrored gait halves).</summary>
        public Pose SwapLegs()
        {
            var p = this;
            p.ThB = ThF; p.ShB = ShF; p.ThF = ThB; p.ShF = ShB;
            return p;
        }

        /// <summary>Knees that are only slightly bent snap to a clean, locked-out straight leg.</summary>
        public const float LockoutDeg = 24f;

        public Kf ToKf()
        {
            float shB = Mathf.Abs(ThB - ShB) < LockoutDeg ? ThB : ShB;
            float shF = Mathf.Abs(ThF - ShF) < LockoutDeg ? ThF : ShF;
            var k = new Kf { Lean = Lean, ThB = ThB, ShB = shB, ThF = ThF, ShF = shF, Rot = Rot, Head = Head, Drop = Drop, Wr = Wr, Hx = Hx, Tn = Tn };
            Kf.ArmFromAngles(UaB, FaB, out k.HBx, out k.HBy, out k.BB);
            Kf.ArmFromAngles(UaF, FaF, out k.HFx, out k.HFy, out k.BF);
            return k;
        }
    }

    /// <summary>
    /// Keyframe values the rig actually interpolates and springs. Hands are positions relative to the shoulder
    /// (size 1, facing right), plus an elbow-bend side; the elbow is solved with two-bone IK every frame.
    /// </summary>
    public struct Kf
    {
        public const int Count = 17;
        public const float Upper = 0.42f, Fore = 0.42f;

        public float Lean, HBx, HBy, BB, HFx, HFy, BF, ThB, ShB, ThF, ShF, Rot, Head, Drop, Wr, Hx, Tn;

        public static void ArmFromAngles(float ua, float fa, out float hx, out float hy, out float bend)
        {
            float ur = ua * Mathf.Deg2Rad, fr = fa * Mathf.Deg2Rad;
            float ex = Mathf.Sin(ur) * Upper, ey = -Mathf.Cos(ur) * Upper;
            hx = ex + Mathf.Sin(fr) * Fore; hy = ey - Mathf.Cos(fr) * Fore;
            float cross = hx * ey - hy * ex;    // which side of the shoulder->hand line the elbow sits on
            bend = cross >= 0f ? 1f : -1f;
        }

        public float this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0: return Lean; case 1: return HBx; case 2: return HBy; case 3: return BB; case 4: return HFx; case 5: return HFy;
                    case 6: return BF; case 7: return ThB; case 8: return ShB; case 9: return ThF; case 10: return ShF; case 11: return Rot;
                    case 12: return Head; case 13: return Drop; case 14: return Wr; case 15: return Hx; default: return Tn;
                }
            }
            set
            {
                switch (i)
                {
                    case 0: Lean = value; break; case 1: HBx = value; break; case 2: HBy = value; break; case 3: BB = value; break;
                    case 4: HFx = value; break; case 5: HFy = value; break; case 6: BF = value; break; case 7: ThB = value; break;
                    case 8: ShB = value; break; case 9: ThF = value; break; case 10: ShF = value; break; case 11: Rot = value; break;
                    case 12: Head = value; break; case 13: Drop = value; break; case 14: Wr = value; break; case 15: Hx = value; break;
                    default: Tn = value; break;
                }
            }
        }

        public static Kf Lerp(in Kf a, in Kf b, float t)
        {
            var r = new Kf();
            for (int i = 0; i < Count; i++) r[i] = a[i] + (b[i] - a[i]) * t;
            return r;
        }

        /// <summary>Catmull-Rom through p1..p2: smooth, flowing curves through every key.</summary>
        public static Kf Spline(in Kf p0, in Kf p1, in Kf p2, in Kf p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            var r = new Kf();
            for (int i = 0; i < Count; i++)
            {
                float a = p0[i], b = p1[i], c = p2[i], d = p3[i];
                r[i] = 0.5f * (2f * b + (-a + c) * t + (2f * a - 5f * b + 4f * c - d) * t2 + (-a + 3f * b - 3f * c + d) * t3);
            }
            return r;
        }

        public Kf SwapArms()
        {
            var k = this;
            k.HBx = HFx; k.HBy = HFy; k.BB = BF; k.HFx = HBx; k.HFy = HBy; k.BF = BB;
            return k;
        }
    }

    /// <summary>Reference poses shared by casts, reactions and the move library.</summary>
    public static class Poses
    {
        public static readonly Pose Idle = Pose.P(8, 22, 118, 42, 142, -18, -32, 22, -4);
        public static readonly Pose Block = Pose.P(-4, 62, 168, 72, 165, -26, -38, 28, 4, 0, 0, 0.08f);
        public static readonly Pose Down = Pose.P(0, 160, 175, 30, 60, -5, -5, 10, 0, -88, 0);
        public static readonly Pose Crouch = Pose.P(20, 30, 120, 60, 140, -55, -110, 70, -30, 0, 0, 0.25f);
        public static readonly Pose Victory = Pose.P(-6, 15, 110, 168, 176, -14, -20, 16, 0, 0, -10);
        public static readonly Pose Channel = Pose.P(0, -40, -70, 40, 70, -18, -25, 18, 0);
        public static readonly Pose HandSign = Pose.P(2, 46, 156, 52, 150, -22, -30, 24, 0);
        public static readonly Pose Chant = Pose.P(-6, 15, 100, 40, 162, -18, -26, 20, 0, 0, 8);
        public static readonly Pose Taunt = Pose.P(-4, 30, -30, 30, -20, -16, -20, 16, 0);
    }
}
