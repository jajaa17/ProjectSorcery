using System.Collections.Generic;
using UnityEngine;

namespace ProjectSorcery
{
    /// <summary>Procedural single-stroke line art for shikigami, curses, puppets and summons.</summary>
    public static class MinionShapes
    {
        /// <summary>Writes a polyline for the minion into pts and returns the eye position.</summary>
        public static Vector2 Build(Minion m, Vector2 p, List<Vector2> pts)
        {
            float s = m.D.Size;
            int f = m.Facing;
            float t = m.AnimTime;
            float run = Mathf.Abs(m.Vel.x) > 0.3f ? 1f : 0.25f;
            float leg = Mathf.Sin(t * 14f) * 0.22f * run;
            Vector2 V(float x, float y) => p + new Vector2(x * f * s, y * s);

            switch (m.D.Shape)
            {
                case MinionShape.Dog:
                case MinionShape.Tiger:
                case MinionShape.Deer:
                case MinionShape.Ox:
                {
                    float atk = m.Attacking ? 0.25f : 0f;
                    pts.Add(V(-0.75f, 0.85f + Mathf.Sin(t * 8f) * 0.1f));           // tail
                    pts.Add(V(-0.5f, 0.6f));                                          // hip
                    pts.Add(V(-0.45f + leg, 0f));                                     // back leg
                    pts.Add(V(-0.5f, 0.6f));
                    pts.Add(V(0.35f, 0.65f));                                         // shoulder
                    pts.Add(V(0.4f - leg, 0f));                                       // front leg
                    pts.Add(V(0.35f, 0.65f));
                    pts.Add(V(0.6f + atk, 0.95f));                                    // head
                    pts.Add(V(0.95f + atk, 0.82f));                                   // snout
                    if (m.D.Shape == MinionShape.Deer) { pts.Add(V(0.6f, 0.95f)); pts.Add(V(0.5f, 1.4f)); pts.Add(V(0.7f, 1.6f)); pts.Add(V(0.5f, 1.4f)); pts.Add(V(0.35f, 1.55f)); }
                    if (m.D.Shape == MinionShape.Ox) { pts.Add(V(0.6f, 0.95f)); pts.Add(V(0.85f, 1.25f)); }
                    if (m.D.Shape == MinionShape.Dog) { pts.Add(V(0.6f + atk, 0.95f)); pts.Add(V(0.55f, 1.2f)); }
                    return V(0.72f + atk, 0.95f);
                }
                case MinionShape.Rabbit:
                    pts.Add(V(-0.3f, 0.1f)); pts.Add(V(-0.25f, 0.4f)); pts.Add(V(0.2f, 0.45f)); pts.Add(V(0.3f, 0.1f));
                    pts.Add(V(0.2f, 0.45f)); pts.Add(V(0.25f, 0.85f)); pts.Add(V(0.3f, 0.45f)); pts.Add(V(0.4f, 0.8f));
                    return V(0.25f, 0.5f);
                case MinionShape.Bird:
                case MinionShape.Crow:
                case MinionShape.Garuda:
                {
                    float flap = Mathf.Sin(t * (m.D.Shape == MinionShape.Garuda ? 8f : 16f)) * 0.5f;
                    pts.Add(V(-0.4f, 0.4f + flap)); pts.Add(V(-0.1f, 0.05f)); pts.Add(V(0.1f, 0.05f)); pts.Add(V(0.4f, 0.4f + flap));
                    pts.Add(V(0.1f, 0.05f)); pts.Add(V(0.35f, -0.05f)); pts.Add(V(0.1f, -0.1f)); pts.Add(V(-0.35f, -0.05f));
                    return V(0.25f, 0.0f);
                }
                case MinionShape.Toad:
                {
                    float b = Mathf.Sin(t * 5f) * 0.04f;
                    for (int i = 0; i <= 10; i++)
                    {
                        float a = Mathf.PI * i / 10f;
                        pts.Add(V(Mathf.Cos(a) * 0.6f, Mathf.Sin(a) * (0.55f + b)));
                    }
                    pts.Add(V(0.45f, 0.4f)); pts.Add(V(0.35f, 0.75f)); pts.Add(V(0.2f, 0.45f));
                    return V(0.38f, 0.6f);
                }
                case MinionShape.Elephant:
                    pts.Add(V(-0.9f, 0f)); pts.Add(V(-0.9f, 0.9f)); pts.Add(V(-0.3f, 1.3f)); pts.Add(V(0.5f, 1.2f)); pts.Add(V(0.9f, 0.9f));
                    pts.Add(V(1.15f, 0.3f + Mathf.Sin(t * 4f) * 0.1f)); pts.Add(V(1.25f, 0f)); pts.Add(V(0.9f, 0.9f)); pts.Add(V(0.7f, 0f)); pts.Add(V(0.6f, 0.75f)); pts.Add(V(-0.6f, 0.75f)); pts.Add(V(-0.7f, 0f));
                    return V(0.7f, 1f);
                case MinionShape.Fish:
                {
                    float w = Mathf.Sin(t * 12f) * 0.1f;
                    pts.Add(V(-0.5f, 0.2f + w)); pts.Add(V(-0.3f, 0f)); pts.Add(V(0.1f, 0.15f)); pts.Add(V(0.4f, 0f)); pts.Add(V(0.1f, -0.15f)); pts.Add(V(-0.3f, 0f)); pts.Add(V(-0.5f, -0.2f + w));
                    return V(0.25f, 0.03f);
                }
                case MinionShape.Serpent:
                case MinionShape.Dragon:
                {
                    int n = 12;
                    for (int i = 0; i < n; i++)
                    {
                        float k = i / (float)(n - 1);
                        pts.Add(V(-1.4f + k * 2.2f, 0.5f + Mathf.Sin(k * 6f - t * 6f) * 0.35f * (m.D.Shape == MinionShape.Dragon ? 1.3f : 1f)));
                    }
                    pts.Add(V(1.0f, 0.75f)); pts.Add(V(0.8f, 0.5f));
                    return V(0.85f, 0.65f);
                }
                case MinionShape.Jellyfish:
                {
                    for (int i = 0; i <= 8; i++) { float a = Mathf.PI * i / 8f; pts.Add(V(Mathf.Cos(a) * 0.45f, 0.5f + Mathf.Sin(a) * 0.35f)); }
                    for (int k = 0; k < 4; k++) { float x = -0.35f + k * 0.23f; pts.Add(V(x, 0.5f)); pts.Add(V(x + Mathf.Sin(t * 5f + k) * 0.1f, 0.05f)); pts.Add(V(x, 0.5f)); }
                    return V(0.15f, 0.65f);
                }
                case MinionShape.Insect:
                case MinionShape.Cockroach:
                {
                    float j = Mathf.Sin(t * 30f) * 0.04f;
                    pts.Add(V(-0.3f, 0.15f)); pts.Add(V(0.3f, 0.2f + j)); pts.Add(V(0.4f, 0.15f)); pts.Add(V(0.3f, 0.08f)); pts.Add(V(-0.3f, 0.12f));
                    pts.Add(V(-0.15f, 0.12f)); pts.Add(V(-0.25f + leg * 0.4f, 0f)); pts.Add(V(0f, 0.12f)); pts.Add(V(0.1f - leg * 0.4f, 0f)); pts.Add(V(0.15f, 0.14f)); pts.Add(V(0.3f + leg * 0.4f, 0f));
                    return V(0.32f, 0.18f);
                }
                case MinionShape.Blob:
                {
                    int n = 14;
                    for (int i = 0; i <= n; i++)
                    {
                        float a = i / (float)n * Mathf.PI * 2f;
                        float r = 0.5f + Mathf.Sin(a * 3f + t * 4f) * 0.08f;
                        pts.Add(V(Mathf.Cos(a) * r, 0.5f + Mathf.Sin(a) * r * 0.9f));
                    }
                    return V(0.25f, 0.65f);
                }
                case MinionShape.Car:
                    pts.Add(V(-0.9f, 0.25f)); pts.Add(V(-0.9f, 0.6f)); pts.Add(V(-0.4f, 0.65f)); pts.Add(V(-0.2f, 0.95f)); pts.Add(V(0.5f, 0.95f));
                    pts.Add(V(0.75f, 0.6f)); pts.Add(V(1f, 0.55f)); pts.Add(V(1f, 0.25f)); pts.Add(V(-0.9f, 0.25f));
                    pts.Add(V(-0.55f, 0.25f)); pts.Add(V(-0.55f, 0f)); pts.Add(V(0.6f, 0f)); pts.Add(V(0.6f, 0.25f));
                    return V(0.8f, 0.45f);
                case MinionShape.Wheel:
                case MinionShape.Giant:
                case MinionShape.Humanoid:
                case MinionShape.Corpse:
                case MinionShape.Puppet:
                case MinionShape.Judge:
                case MinionShape.Mech:
                default:
                {
                    // compact stick body with long reaching arms for giants
                    bool giant = m.D.Shape == MinionShape.Giant || m.D.Shape == MinionShape.Wheel;
                    float swing = m.Attacking ? 1f : Mathf.Sin(t * 3f) * 0.2f;
                    float arm = giant ? 1.0f : 0.5f;
                    Vector2 hip = V(0f, 0.85f), neck = V(0.05f, 1.6f), head = V(0.1f, 1.9f);
                    pts.Add(V(-0.3f + leg * 0.6f, 0f)); pts.Add(hip); pts.Add(V(0.3f - leg * 0.6f, 0f)); pts.Add(hip); pts.Add(neck);
                    pts.Add(V(-0.4f, 1.2f - swing * 0.2f)); pts.Add(neck);
                    pts.Add(V(0.3f + swing * arm * 0.6f, 1.45f + swing * 0.3f)); pts.Add(V(0.4f + arm + swing * arm, 1.4f + swing * 0.5f)); pts.Add(V(0.3f + swing * arm * 0.6f, 1.45f + swing * 0.3f)); pts.Add(neck);
                    for (int i = 0; i <= 10; i++) { float a = i / 10f * Mathf.PI * 2f - Mathf.PI * 0.5f; pts.Add(head + new Vector2(Mathf.Cos(a), Mathf.Sin(a) + 1f) * 0.22f * s); }
                    if (m.D.Shape == MinionShape.Wheel)
                    {
                        Vector2 w = head + new Vector2(0f, 0.75f * s);
                        float spin = t * 40f + m.Adapt[0] * 600f;
                        for (int i = 0; i <= 8; i++) { float a = (spin + i * 45f) * Mathf.Deg2Rad; pts.Add(w); pts.Add(w + new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.45f) * 0.45f * s); }
                    }
                    if (m.D.Shape == MinionShape.Judge)
                    {
                        Vector2 sc = V(-0.1f, 2.6f);
                        pts.Add(sc); pts.Add(sc + new Vector2(-0.4f * s, 0f)); pts.Add(sc + new Vector2(-0.4f * s, -0.3f * s)); pts.Add(sc + new Vector2(-0.4f * s, 0f)); pts.Add(sc + new Vector2(0.4f * s, 0f)); pts.Add(sc + new Vector2(0.4f * s, -0.3f * s));
                    }
                    return head + new Vector2(f * 0.08f * s, 0.24f * s);
                }
            }
        }
    }
}
