using System.Collections.Generic;
using UnityEngine;

namespace ProjectSorcery
{
    public sealed class ProjectileView
    {
        public GameObject Go;
        public SpriteRenderer Glow, Core;
        public LineRenderer Shape, Aura;
        public TrailRenderer Trail;
        public float T;
    }

    public sealed class BeamView
    {
        public GameObject Go;
        public LineRenderer Halo, Outer, Inner, Core;
        public SpriteRenderer Flare, EndFlare;
        public float ChargeFx;
    }

    public sealed class MinionView
    {
        public GameObject Go;
        public LineRenderer Body, Glow, Eye;
    }

    public sealed class ZoneView
    {
        public GameObject Go;
        public LineRenderer Edge;
        public float T;
    }

    /// <summary>Pooled visuals for simulation entities. Sync() interpolates and animates them each frame.</summary>
    public static class Views
    {
        static Transform root;
        static readonly Stack<ProjectileView> projPool = new Stack<ProjectileView>();
        static readonly Stack<BeamView> beamPool = new Stack<BeamView>();
        static readonly Stack<MinionView> minionPool = new Stack<MinionView>();
        static readonly Stack<ZoneView> zonePool = new Stack<ZoneView>();
        static readonly List<Vector2> path = new List<Vector2>(48);

        public static void Init(Transform r)
        {
            var go = new GameObject("EntityViews");
            go.transform.SetParent(r, false);
            root = go.transform;
        }

        // ------------------------------------------------------------ attach / release
        public static void Attach(Projectile p)
        {
            if (root == null) return;
            var v = projPool.Count > 0 ? projPool.Pop() : NewProj();
            v.Go.SetActive(true);
            v.T = 0f;
            var d = p.D;
            v.Glow.color = d.Color.WithA(0.75f);
            v.Core.color = d.Core;
            v.Trail.Clear();
            v.Trail.time = d.TrailTime;
            v.Trail.startColor = d.Color.WithA(0.85f);
            v.Trail.endColor = d.Color.WithA(0f);
            v.Trail.widthMultiplier = p.Size * 0.9f;
            v.Go.transform.position = p.Pos;
            v.Trail.emitting = true;
            v.Shape.enabled = false;
            v.Shape.sharedMaterial = d.Vis == ProjVis.Rock || d.Vis == ProjVis.Nail || d.Vis == ProjVis.Bird || d.Vis == ProjVis.Wood || d.Vis == ProjVis.Card || d.Vis == ProjVis.Metal ? Art.Line : Art.AddLine;
            p.View = v;
        }

        public static void Release(Projectile p)
        {
            if (p.View == null) return;
            p.View.Trail.emitting = false;
            p.View.Go.SetActive(false);
            projPool.Push(p.View);
            p.View = null;
        }

        public static void Attach(Beam b)
        {
            if (root == null) return;
            var v = beamPool.Count > 0 ? beamPool.Pop() : NewBeam();
            v.Go.SetActive(true);
            b.View = v;
        }

        public static void Release(Beam b)
        {
            if (b.View == null) return;
            b.View.Go.SetActive(false);
            beamPool.Push(b.View);
            b.View = null;
        }

        public static void Attach(Minion m)
        {
            if (root == null) return;
            var v = minionPool.Count > 0 ? minionPool.Pop() : NewMinion();
            v.Go.SetActive(true);
            v.Body.widthMultiplier = 0.09f * Mathf.Max(0.7f, m.D.Size * 0.7f);
            v.Glow.widthMultiplier = v.Body.widthMultiplier * 3f;
            m.View = v;
        }

        public static void Release(Minion m)
        {
            if (m.View == null) return;
            m.View.Go.SetActive(false);
            minionPool.Push(m.View);
            m.View = null;
        }

        public static void Attach(Zone z)
        {
            if (root == null) return;
            var v = zonePool.Count > 0 ? zonePool.Pop() : NewZone();
            v.Go.SetActive(true);
            v.T = 0f;
            z.View = v;
        }

        public static void Release(Zone z)
        {
            if (z.View == null) return;
            z.View.Go.SetActive(false);
            zonePool.Push(z.View);
            z.View = null;
        }

        public static void ReleaseAll() { /* views are released by Match.Cleanup / ClearEntities */ }

        static ProjectileView NewProj()
        {
            var go = new GameObject("Proj");
            go.transform.SetParent(root, false);
            var v = new ProjectileView { Go = go };
            v.Glow = Art.NewSprite(go.transform, "Glow", Art.Soft, Art.AddSoft, Art.OrderProjectile);
            v.Core = Art.NewSprite(go.transform, "Core", Art.Soft, Art.AddSoft, Art.OrderProjectile + 1);
            v.Shape = Art.NewLine(go.transform, "Shape", Art.AddLine, Art.OrderProjectile + 2, 0.08f, 2);
            v.Shape.useWorldSpace = true;
            // flowing aura ring around energy orbs (procedural noise shader)
            v.Aura = Art.NewLine(go.transform, "Aura", Art.EnergyBeam, Art.OrderProjectile, 0.2f, 2);
            v.Aura.useWorldSpace = true;
            v.Aura.textureMode = LineTextureMode.Stretch;
            v.Aura.enabled = false;
            var tgo = new GameObject("Trail");
            tgo.transform.SetParent(go.transform, false);
            v.Trail = tgo.AddComponent<TrailRenderer>();
            v.Trail.sharedMaterial = Art.EnergyBeam;
            v.Trail.textureMode = LineTextureMode.Stretch;
            v.Trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            v.Trail.minVertexDistance = 0.05f;
            v.Trail.sortingOrder = Art.OrderProjectile - 1;
            v.Trail.numCapVertices = 2;
            return v;
        }

        static BeamView NewBeam()
        {
            var go = new GameObject("Beam");
            go.transform.SetParent(root, false);
            var v = new BeamView { Go = go };
            // layered beam: soft bloom halo -> boiling noise aura -> solid colour body -> white-hot core
            v.Halo = Art.NewLine(go.transform, "Halo", Art.AddLine, Art.OrderProjectile - 1, 1f, 2);
            v.Outer = Art.NewLine(go.transform, "Outer", Art.EnergyBeam, Art.OrderProjectile, 1f, 12);
            v.Outer.textureMode = LineTextureMode.Tile;
            v.Inner = Art.NewLine(go.transform, "Inner", Art.AddLine, Art.OrderProjectile + 1, 0.5f, 12);
            v.Core = Art.NewLine(go.transform, "Core", Art.AddLine, Art.OrderProjectile + 2, 0.2f, 2);
            v.Flare = Art.NewSprite(go.transform, "Flare", Art.Soft, Art.AddSoft, Art.OrderProjectile + 3);
            v.EndFlare = Art.NewSprite(go.transform, "EndFlare", Art.Soft, Art.AddSoft, Art.OrderProjectile + 3);
            return v;
        }

        static MinionView NewMinion()
        {
            var go = new GameObject("Minion");
            go.transform.SetParent(root, false);
            var v = new MinionView { Go = go };
            v.Glow = Art.NewLine(go.transform, "Glow", Art.AddLine, Art.OrderMinion - 1, 0.25f, 2);
            v.Body = Art.NewLine(go.transform, "Body", Art.Line, Art.OrderMinion, 0.08f, 2);
            v.Eye = Art.NewLine(go.transform, "Eye", Art.AddLine, Art.OrderMinion + 1, 0.07f, 2);
            v.Body.numCornerVertices = 1; v.Glow.numCornerVertices = 1;
            return v;
        }

        static ZoneView NewZone()
        {
            var go = new GameObject("Zone");
            go.transform.SetParent(root, false);
            var v = new ZoneView { Go = go };
            v.Edge = Art.NewLine(go.transform, "Edge", Art.AddLine, Art.OrderZone, 0.1f, 2);
            return v;
        }

        // ------------------------------------------------------------ per-frame sync
        public static void Sync(Match m, float alpha)
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f) * VFX.SimSpeed;
            for (int i = 0; i < m.Projectiles.Count; i++) SyncProj(m.Projectiles[i], alpha, dt);
            for (int i = 0; i < m.Beams.Count; i++) SyncBeam(m.Beams[i]);
            for (int i = 0; i < m.Minions.Count; i++) SyncMinion(m.Minions[i], alpha);
            for (int i = 0; i < m.Zones.Count; i++) SyncZone(m.Zones[i], dt);
        }

        static void SyncProj(Projectile p, float alpha, float dt)
        {
            var v = p.View;
            if (v == null) return;
            v.T += dt;
            Vector2 pos = Vector2.LerpUnclamped(p.PrevPos, p.Pos, alpha);
            v.Go.transform.position = pos;
            var d = p.D;
            float s = p.Size;
            float pulse = 1f + Mathf.Sin(v.T * 30f) * 0.08f;
            v.Glow.transform.localScale = Vector3.one * s * 2.6f * pulse;
            v.Core.transform.localScale = Vector3.one * s * 0.9f;
            v.Trail.widthMultiplier = s * 0.9f;
            Vector2 dir = p.Vel.sqrMagnitude > 0.01f ? p.Vel.normalized : Vector2.right;
            Vector2 n = new Vector2(-dir.y, dir.x);
            var sh = v.Shape;
            sh.enabled = true;
            sh.widthMultiplier = Mathf.Max(0.05f, s * 0.22f);
            Color c = d.Color;
            bool orb = d.Vis == ProjVis.Orb || d.Vis == ProjVis.Sphere || d.Vis == ProjVis.Blood || d.Vis == ProjVis.Water;
            v.Aura.enabled = orb && s >= 0.45f && Settings.VfxQuality > 0;
            if (v.Aura.enabled)
            {
                Ring(v.Aura, pos, s * 1.15f * pulse, 20, -v.T * 4f);
                v.Aura.widthMultiplier = s * 0.55f;
                var ac = c.WithA(0.9f);
                v.Aura.startColor = ac; v.Aura.endColor = ac;
                if (s >= 0.9f && Random.value < 0.5f) VFX.Implode(pos, s * 2.2f, c, 2, 0.25f);   // ultimate orbs keep drinking in energy
            }
            switch (d.Vis)
            {
                case ProjVis.Orb: case ProjVis.Blood: case ProjVis.Water: case ProjVis.Skull:
                    sh.enabled = false; break;
                case ProjVis.Sphere:
                {
                    // annihilation sphere: rotating rings, crackling edge
                    Ring(sh, pos, s * 0.75f, 24, v.T * 6f);
                    sh.widthMultiplier = 0.07f;
                    if (Random.value < 0.6f) VFX.Sparks(pos + Random.insideUnitCircle * s * 0.5f, c, 1, 6f, 0.12f);
                    break;
                }
                case ProjVis.Bolt: case ProjVis.Bullet: case ProjVis.Needle:
                    Line2(sh, pos - dir * s * (d.Vis == ProjVis.Bolt ? 1.6f : 0.8f), pos + dir * s * 0.4f); break;
                case ProjVis.Slash:
                {
                    int k = 9;
                    if (sh.positionCount != k) sh.positionCount = k;
                    for (int i = 0; i < k; i++)
                    {
                        float t = i / (float)(k - 1) - 0.5f;
                        sh.SetPosition(i, pos + n * t * s * 2.4f - dir * (t * t) * s * 1.6f);
                    }
                    sh.widthMultiplier = s * 0.28f;
                    break;
                }
                case ProjVis.Arrow:
                    Poly(sh, pos - dir * s * 2.2f, pos + dir * s * 0.8f, pos + dir * s * 0.2f + n * s * 0.4f, pos + dir * s * 0.8f, pos + dir * s * 0.2f - n * s * 0.4f);
                    if (Random.value < 0.8f) VFX.Sparks(pos - dir * s, new Color(1f, 0.6f, 0.15f), 1, 3f, 0.14f);
                    break;
                case ProjVis.Nail:
                    Poly(sh, pos - dir * s * 0.6f, pos + dir * s * 0.6f, pos + dir * s * 0.45f + n * s * 0.1f);
                    sh.widthMultiplier = 0.05f; c = new Color(0.8f, 0.8f, 0.85f);
                    break;
                case ProjVis.Fire:
                    sh.enabled = false;
                    VFX.AuraTick(pos - Vector2.up * 0.3f, 0.6f, c, 0.9f);
                    break;
                case ProjVis.Rock: case ProjVis.Metal: case ProjVis.Wood:
                    Ring(sh, pos, s * 0.5f, 7, v.T * 3f); sh.widthMultiplier = s * 0.35f;
                    c = d.Vis == ProjVis.Rock ? new Color(0.45f, 0.42f, 0.4f) : d.Vis == ProjVis.Wood ? new Color(0.55f, 0.4f, 0.25f) : new Color(0.75f, 0.77f, 0.8f);
                    break;
                case ProjVis.Bird:
                {
                    float flap = Mathf.Sin(v.T * 22f) * 0.4f;
                    Poly(sh, pos - dir * s * 0.3f + n * s * (0.6f + flap), pos, pos - dir * s * 0.3f - n * s * (0.6f + flap), pos, pos + dir * s * 0.5f);
                    sh.widthMultiplier = 0.06f; c = new Color(0.05f, 0.05f, 0.08f);
                    break;
                }
                case ProjVis.Insect:
                    Line2(sh, pos + Random.insideUnitCircle * 0.08f, pos + dir * 0.15f); sh.widthMultiplier = 0.08f; break;
                case ProjVis.Lightning:
                {
                    int k = 7;
                    if (sh.positionCount != k) sh.positionCount = k;
                    for (int i = 0; i < k; i++)
                    {
                        float t = i / (float)(k - 1);
                        sh.SetPosition(i, pos - dir * s * 2f * (1f - t) + n * Random.Range(-0.25f, 0.25f) * s);
                    }
                    sh.widthMultiplier = 0.06f;
                    break;
                }
                case ProjVis.Ice:
                    Poly(sh, pos - dir * s * 0.8f, pos + n * s * 0.25f, pos + dir * s * 0.8f, pos - n * s * 0.25f, pos - dir * s * 0.8f);
                    sh.widthMultiplier = 0.06f; c = Color.Lerp(c, Color.white, 0.5f);
                    break;
                case ProjVis.Star:
                {
                    int k = 11;
                    if (sh.positionCount != k) sh.positionCount = k;
                    for (int i = 0; i < k; i++)
                    {
                        float a = i / 10f * Mathf.PI * 2f + v.T * 4f;
                        float r = (i % 2 == 0 ? 0.7f : 0.3f) * s;
                        sh.SetPosition(i, pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
                    }
                    sh.widthMultiplier = 0.05f;
                    break;
                }
                case ProjVis.Disc:
                    Ring(sh, pos, s * 0.6f, 16, 0f); sh.widthMultiplier = 0.08f; break;
                case ProjVis.Wave:
                {
                    int k = 9;
                    if (sh.positionCount != k) sh.positionCount = k;
                    for (int i = 0; i < k; i++)
                    {
                        float t = i / (float)(k - 1);
                        sh.SetPosition(i, new Vector2(pos.x - dir.x * Mathf.Sin(t * Mathf.PI) * s * 0.6f, t * s * 2.4f));
                    }
                    sh.widthMultiplier = s * 0.25f;
                    VFX.Dust(new Vector2(pos.x, 0f), 1);
                    break;
                }
                case ProjVis.Sound:
                    Ring(sh, pos, s * (0.4f + Mathf.Repeat(v.T * 3f, 0.6f)), 14, 0f); sh.widthMultiplier = 0.05f; break;
                case ProjVis.Card:
                    Poly(sh, pos + n * s * 0.3f + dir * s * 0.2f, pos + n * s * 0.3f - dir * s * 0.2f, pos - n * s * 0.3f - dir * s * 0.2f, pos - n * s * 0.3f + dir * s * 0.2f, pos + n * s * 0.3f + dir * s * 0.2f);
                    sh.widthMultiplier = 0.04f; c = Color.white;
                    break;
            }
            sh.startColor = c; sh.endColor = c;
        }

        static void Line2(LineRenderer lr, Vector2 a, Vector2 b)
        {
            if (lr.positionCount != 2) lr.positionCount = 2;
            lr.SetPosition(0, a); lr.SetPosition(1, b);
        }

        static void Poly(LineRenderer lr, Vector2 a, Vector2 b, Vector2 c, Vector2 d = default, Vector2 e = default)
        {
            int n = e != default ? 5 : d != default ? 4 : 3;
            if (lr.positionCount != n) lr.positionCount = n;
            lr.SetPosition(0, a); lr.SetPosition(1, b); lr.SetPosition(2, c);
            if (n > 3) lr.SetPosition(3, d);
            if (n > 4) lr.SetPosition(4, e);
        }

        static void Ring(LineRenderer lr, Vector2 c, float r, int n, float rot)
        {
            if (lr.positionCount != n + 1) lr.positionCount = n + 1;
            for (int i = 0; i <= n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f + rot;
                lr.SetPosition(i, c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
        }

        static void SyncBeam(Beam b)
        {
            var v = b.View;
            if (v == null) return;
            Vector2 a = b.Origin, e = b.End;
            float w = b.Width;
            var d = b.D;
            int n = v.Outer.positionCount;
            Vector2 dir = (e - a).normalized, nrm = new Vector2(-dir.y, dir.x);
            if (!b.Firing)
            {
                float k = Mathf.Clamp01(b.Age / Mathf.Max(0.01f, d.Charge));
                v.Outer.enabled = false; v.Inner.enabled = false; v.Halo.enabled = false; v.EndFlare.enabled = false;
                // charge-up: energy spirals in and collapses into the muzzle before it fires
                v.ChargeFx -= Time.unscaledDeltaTime;
                if (v.ChargeFx <= 0f) { v.ChargeFx = 0.03f; VFX.Implode(a, 1.2f + w * 1.5f, d.Color, 3, Mathf.Max(0.12f, d.Charge * 0.6f)); }
                v.Core.enabled = true;
                v.Core.SetPosition(0, a); v.Core.SetPosition(1, e);
                v.Core.widthMultiplier = 0.03f + 0.05f * k;
                var tc = d.Color.WithA(0.3f + 0.5f * Mathf.Repeat(Time.unscaledTime * 20f, 1f));
                v.Core.startColor = tc; v.Core.endColor = tc;
                v.Flare.transform.position = a;
                v.Flare.transform.localScale = Vector3.one * (0.6f + k * 1.6f);
                v.Flare.color = d.Color;
                return;
            }
            v.Outer.enabled = v.Inner.enabled = v.Core.enabled = true;
            v.Halo.enabled = Settings.VfxQuality > 0; v.EndFlare.enabled = true;
            float life = Mathf.Clamp01((b.Age - d.Charge) / Mathf.Max(0.01f, d.Duration));
            float fade = life > 0.8f ? (1f - life) / 0.2f : 1f;
            float burst = life < 0.12f ? 1f + (0.12f - life) / 0.12f * 0.8f : 1f;   // the first instant fires wide
            CameraRig.Rumble(Mathf.Clamp01(w * 0.18f) * fade);
            if (life < 0.06f) { CameraRig.Aberrate(Mathf.Clamp01(0.3f + w * 0.35f)); CameraRig.Kick(dir, 0.08f + w * 0.05f); }   // firing kicks the lens
            float t = Time.unscaledTime;
            for (int i = 0; i < n; i++)
            {
                float k = i / (float)(n - 1);
                float wob = (i == 0 || i == n - 1) ? 0f : Mathf.Sin(k * 20f + t * 40f) * w * 0.12f;
                Vector2 p = Vector2.Lerp(a, e, k) + nrm * wob;
                v.Outer.SetPosition(i, p);
                v.Inner.SetPosition(i, p);
            }
            float pulse = 1f + Mathf.Sin(t * 50f) * 0.1f;
            v.Outer.widthMultiplier = w * 2f * pulse * fade * burst;
            v.Inner.widthMultiplier = w * 0.9f * fade * burst;
            v.Core.SetPosition(0, a); v.Core.SetPosition(1, e);
            v.Core.widthMultiplier = w * 0.32f * fade * burst;
            v.Halo.SetPosition(0, a); v.Halo.SetPosition(1, e);
            v.Halo.widthMultiplier = w * 4.2f * pulse * fade * burst;
            v.Halo.startColor = v.Halo.endColor = d.Color.WithA(0.12f);
            v.Outer.startColor = v.Outer.endColor = d.Color.WithA(fade);    // vertex alpha drives the noise dissolve as it dies
            v.Inner.startColor = v.Inner.endColor = d.Color.WithA(0.85f);
            v.Core.startColor = v.Core.endColor = Color.Lerp(d.Core, Color.white, 0.6f);
            v.EndFlare.transform.position = e;
            v.EndFlare.transform.localScale = Vector3.one * w * 3f * pulse * fade;
            v.EndFlare.color = Color.Lerp(d.Color, Color.white, 0.4f);
            v.Flare.transform.position = a;
            v.Flare.transform.localScale = Vector3.one * w * 3.5f * pulse;
            v.Flare.color = d.Color;
            if (Random.value < 0.5f) VFX.Sparks(e, d.Color, 2, 8f);
        }

        static void SyncMinion(Minion m, float alpha)
        {
            var v = m.View;
            if (v == null) return;
            Vector2 pos = Vector2.LerpUnclamped(m.PrevPos, m.Pos, alpha);
            path.Clear();
            Vector2 eye = MinionShapes.Build(m, pos, path);
            Apply(v.Body, path);
            Apply(v.Glow, path);
            Color body = m.HurtFlash > 0f ? Color.white : Color.Lerp(m.D.Color, new Color(0.08f, 0.08f, 0.1f), m.D.Shape == MinionShape.Crow || m.D.Shape == MinionShape.Cockroach ? 0.8f : 0.15f);
            if (ImpactFrames.Ink == ImpactFrames.FighterInk.White) body = Color.white;
            else if (ImpactFrames.Ink == ImpactFrames.FighterInk.Black) body = Color.black;
            float fadeIn = Mathf.Clamp01(m.Age * 4f) * Mathf.Clamp01((m.D.Life - m.Age) * 3f);
            body.a *= fadeIn;
            v.Body.startColor = v.Body.endColor = body;
            var g = m.D.Color.WithA(0.25f * fadeIn);
            v.Glow.startColor = v.Glow.endColor = g;
            if (v.Eye.positionCount != 2) v.Eye.positionCount = 2;
            v.Eye.SetPosition(0, eye); v.Eye.SetPosition(1, eye + new Vector2(m.Facing * 0.12f * m.D.Size, 0.02f));
            var ec = m.Owner != null ? m.Owner.Def.Look.Aura : Color.white;
            ec.a = fadeIn;
            v.Eye.startColor = v.Eye.endColor = ec;
        }

        static void Apply(LineRenderer lr, List<Vector2> pts)
        {
            if (lr.positionCount != pts.Count) lr.positionCount = pts.Count;
            for (int i = 0; i < pts.Count; i++) lr.SetPosition(i, pts[i]);
        }

        static void SyncZone(Zone z, float dt)
        {
            var v = z.View;
            if (v == null) return;
            v.T += dt;
            var d = z.D;
            float hw = d.Width * 0.5f;
            float life = Mathf.Clamp01(z.Age / d.Life);
            float a = Mathf.Clamp01(z.Age * 5f) * Mathf.Clamp01((d.Life - z.Age) * 3f);
            int n = 18;
            if (v.Edge.positionCount != n) v.Edge.positionCount = n;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                float x = z.Pos.x - hw + t * d.Width;
                float y = z.Pos.y + 0.05f + Mathf.Sin(t * 12f + v.T * 5f) * 0.05f;
                v.Edge.SetPosition(i, new Vector2(x, y));
            }
            v.Edge.widthMultiplier = 0.12f;
            var c = d.Color.WithA(0.7f * a);
            v.Edge.startColor = v.Edge.endColor = c;
            VFX.ZoneTick(d.Vis, z.Pos, d.Width, d.Height, d.Color, a);
        }
    }
}
