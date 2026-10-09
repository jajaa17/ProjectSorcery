using System.Collections.Generic;
using UnityEngine;

namespace ProjectSorcery
{
    public enum ImpactKind { Heavy, BlackFlash, Domain, Ko }

    /// <summary>
    /// Visual effects facade. Particles go through a handful of batched ParticleSystems (one draw call each);
    /// lines and sprites come from pools. Effects are purely visual and never touch the simulation.
    /// </summary>
    public sealed class VFX : MonoBehaviour
    {
        static VFX I;
        public static float SimSpeed = 1f;

        ParticleSystem sparks, glow, smoke, debris, shards, embers;
        readonly List<LineFx> lines = new List<LineFx>();
        readonly List<SpriteFx> sprites = new List<SpriteFx>();
        readonly List<Ghost> ghosts = new List<Ghost>();

        public static void Create(Transform root)
        {
            var go = new GameObject("VFX");
            go.transform.SetParent(root, false);
            I = go.AddComponent<VFX>();
            I.Build();
        }

        void Build()
        {
            sparks = MakePS("Sparks", Art.AddSoft, ParticleSystemRenderMode.Stretch, 0.6f, Art.OrderVfx, 1500, true, 0f);
            glow = MakePS("Glow", Art.AddSoft, ParticleSystemRenderMode.Billboard, 0f, Art.OrderVfx, 800, true, 1.5f);
            smoke = MakePS("Smoke", Art.AlphaSoft, ParticleSystemRenderMode.Billboard, -0.05f, Art.OrderMinion - 1, 600, false, 1.2f);
            debris = MakePS("Debris", Art.Line, ParticleSystemRenderMode.Billboard, 2.2f, Art.OrderFighter + 1, 600, true, 0.5f);
            shards = MakePS("Shards", Art.AddLine, ParticleSystemRenderMode.Stretch, 1.4f, Art.OrderVfxTop, 600, true, 0.2f);
            embers = MakePS("Embers", Art.AddSoft, ParticleSystemRenderMode.Billboard, -0.25f, Art.OrderVfx, 800, true, 0.3f);
        }

        ParticleSystem MakePS(string name, Material mat, ParticleSystemRenderMode mode, float gravity, int order, int max, bool fade, float drag)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.duration = 10f;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = max;
            main.startSpeed = 0f;
            main.startLifetime = 1f;
            main.gravityModifier = gravity;
            var em = ps.emission; em.enabled = false;
            var sh = ps.shape; sh.enabled = false;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      fade ? new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) }
                           : new[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0.35f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, fade ? AnimationCurve.Linear(0f, 1f, 1f, 0.15f) : AnimationCurve.Linear(0f, 0.6f, 1f, 1.6f));
            if (drag > 0f)
            {
                var lv = ps.limitVelocityOverLifetime;
                lv.enabled = true;
                lv.drag = drag;
                lv.limit = 100f;
            }
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = mode;
            r.sortingOrder = order;
            if (mode == ParticleSystemRenderMode.Stretch) { r.velocityScale = 0.035f; r.lengthScale = 1.2f; }
            ps.Play();
            return ps;
        }

        void Update()
        {
            float raw = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float dt = raw * SimSpeed;
            SetSpeed(sparks); SetSpeed(glow); SetSpeed(smoke); SetSpeed(debris); SetSpeed(shards); SetSpeed(embers);
            for (int i = 0; i < lines.Count; i++) if (lines[i].Active) lines[i].Update(lines[i].Unscaled ? raw : dt);
            for (int i = 0; i < sprites.Count; i++) if (sprites[i].Active) sprites[i].Update(dt);
            for (int i = 0; i < ghosts.Count; i++) if (ghosts[i].Active) ghosts[i].Update(dt);
        }

        static void SetSpeed(ParticleSystem ps)
        {
            var m = ps.main;
            m.simulationSpeed = Mathf.Max(0.0001f, SimSpeed);
        }

        // ------------------------------------------------------------------ pools
        LineFx GetLine(int points, Material mat, int order)
        {
            LineFx l = null;
            for (int i = 0; i < lines.Count; i++) if (!lines[i].Active) { l = lines[i]; break; }
            if (l == null)
            {
                if (lines.Count > 260) { l = lines[0]; lines.RemoveAt(0); lines.Add(l); }
                else
                {
                    l = new LineFx { Lr = Art.NewLine(transform, "LineFx", mat, order, 0.1f, points) };
                    lines.Add(l);
                }
            }
            l.Lr.sharedMaterial = mat;
            l.Lr.sortingOrder = order;
            l.Lr.positionCount = points;
            l.Lr.loop = false;
            l.Lr.enabled = true;
            l.Active = true;
            l.Age = 0f;
            l.Unscaled = false;
            return l;
        }

        SpriteFx GetSprite(Sprite sp, Material mat, int order)
        {
            SpriteFx s = null;
            for (int i = 0; i < sprites.Count; i++) if (!sprites[i].Active) { s = sprites[i]; break; }
            if (s == null)
            {
                if (sprites.Count > 140) { s = sprites[0]; sprites.RemoveAt(0); sprites.Add(s); }
                else { s = new SpriteFx { Sr = Art.NewSprite(transform, "SpriteFx", sp, mat, order) }; sprites.Add(s); }
            }
            s.Sr.sprite = sp;
            s.Sr.sharedMaterial = mat;
            s.Sr.sortingOrder = order;
            s.Sr.enabled = true;
            s.Sr.transform.rotation = Quaternion.identity;
            s.Active = true;
            s.Age = 0f; s.Spin = 0f; s.Vel = Vector2.zero;
            return s;
        }

        static int Q(int n) => Mathf.Max(1, Mathf.RoundToInt(n * Settings.VfxMul));

        // ------------------------------------------------------------------ particle primitives
        static void Emit(ParticleSystem ps, Vector2 pos, Vector2 vel, Color c, float size, float life)
        {
            var ep = new ParticleSystem.EmitParams
            {
                position = new Vector3(pos.x, pos.y, 0f),
                velocity = new Vector3(vel.x, vel.y, 0f),
                startColor = c,
                startSize = size,
                startLifetime = life,
                applyShapeToPosition = false
            };
            ps.Emit(ep, 1);
        }

        static Vector2 Rnd(float min, float max)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            float s = Random.Range(min, max);
            return new Vector2(Mathf.Cos(a) * s, Mathf.Sin(a) * s);
        }

        static Vector2 Cone(int dir, float spreadDeg, float min, float max)
        {
            float a = Random.Range(-spreadDeg, spreadDeg) * Mathf.Deg2Rad;
            float s = Random.Range(min, max);
            return new Vector2(Mathf.Cos(a) * dir, Mathf.Sin(a)) * s;
        }

        // ------------------------------------------------------------------ public API
        public static void Sparks(Vector2 p, Color c, int n, float speed, float size = 0.12f)
        {
            if (I == null) return;
            n = Q(n);
            for (int i = 0; i < n; i++) Emit(I.sparks, p, Rnd(speed * 0.3f, speed), Color.Lerp(c, Color.white, Random.Range(0f, 0.5f)), size * Random.Range(0.6f, 1.4f), Random.Range(0.15f, 0.4f));
        }

        public static void HitSpark(Vector2 p, int dir, Color c, bool heavy)
        {
            if (I == null) return;
            int n = Q(heavy ? 22 : 10);
            for (int i = 0; i < n; i++) Emit(I.sparks, p, Cone(dir, heavy ? 70f : 50f, 6f, heavy ? 22f : 14f), Color.Lerp(c, Color.white, Random.Range(0.2f, 0.8f)), Random.Range(0.08f, 0.16f), Random.Range(0.12f, 0.3f));
            Emit(I.glow, p, Vector2.zero, c.WithA(0.9f), heavy ? 2.4f : 1.3f, 0.12f);
            Emit(I.glow, p, Vector2.zero, Color.white, heavy ? 1.1f : 0.6f, 0.08f);
            I.FlashSprite(p, Art.Ring, Art.AddSoft, c, 0.3f, heavy ? 2.2f : 1.2f, 0.16f);
            if (heavy) SpeedBurst(p, c, 10, 2.2f, 4.5f);
            else I.ArcLine(p, dir > 0 ? Random.Range(-40f, 40f) : 180f + Random.Range(-40f, 40f), 0.5f, 0.75f, 110f, Color.white, 0.07f, 0.1f);
        }

        public static void BlockSpark(Vector2 p, int facing, Color c)
        {
            if (I == null) return;
            for (int i = 0; i < Q(10); i++) Emit(I.sparks, p, Cone(-facing, 80f, 4f, 10f), new Color(0.7f, 0.9f, 1f), 0.09f, 0.2f);
            I.ArcLine(p, facing > 0 ? 0f : 180f, 0.6f, 0.8f, 140f, new Color(0.7f, 0.9f, 1f), 0.08f, 0.12f);
        }

        public static void Dust(Vector2 p, int n)
        {
            if (I == null) return;
            n = Q(n);
            for (int i = 0; i < n; i++) Emit(I.smoke, p + new Vector2(Random.Range(-0.3f, 0.3f), 0.1f), new Vector2(Random.Range(-2.5f, 2.5f), Random.Range(0.2f, 1.2f)), new Color(0.75f, 0.75f, 0.8f, 0.35f), Random.Range(0.4f, 0.8f), Random.Range(0.35f, 0.7f));
        }

        public static void Ring(Vector2 p, Color c, float radius, float life, float width)
        {
            if (I == null) return;
            var l = I.GetLine(33, Art.AddLine, Art.OrderVfx);
            l.K = LineFx.Kind.Ring; l.A = p; l.R0 = radius * 0.2f; l.R1 = radius; l.Life = life; l.Width = width; l.Color = c;
            l.Lr.loop = false;
            l.Update(0f);
        }

        public static void Lightning(Vector2 a, Vector2 b, Color c, float width, float life, int segs = 10, Material mat = null, int order = Art.OrderVfx, bool unscaled = false)
        {
            if (I == null) return;
            var l = I.GetLine(segs, mat ?? Art.AddLine, order);
            l.K = LineFx.Kind.Lightning; l.A = a; l.B = b; l.Width = width; l.Life = life; l.Color = c; l.Unscaled = unscaled;
            l.Jag();
            l.Update(0f);
        }

        void ArcLine(Vector2 center, float angle, float r0, float r1, float sweep, Color c, float width, float life, Material mat = null, int order = Art.OrderVfx)
        {
            var l = GetLine(14, mat ?? Art.AddLine, order);
            l.K = LineFx.Kind.Arc; l.A = center; l.Angle = angle; l.R0 = r0; l.R1 = r1; l.Sweep = sweep; l.Color = c; l.Width = width; l.Life = life;
            l.Update(0f);
        }

        void StraightLine(Vector2 a, Vector2 b, Color c, float width, float life, LineFx.Kind kind, Material mat, int order, bool unscaled = false)
        {
            var l = GetLine(2, mat, order);
            l.K = kind; l.A = a; l.B = b; l.Color = c; l.Width = width; l.Life = life; l.Unscaled = unscaled;
            l.Lr.SetPosition(0, a); l.Lr.SetPosition(1, b);
            l.Update(0f);
        }

        void FlashSprite(Vector2 p, Sprite sp, Material mat, Color c, float s0, float s1, float life, SpriteFx.Kind kind = SpriteFx.Kind.Flash, int order = Art.OrderVfx)
        {
            var s = GetSprite(sp, mat, order);
            s.K = kind; s.S0 = s0; s.S1 = s1; s.Life = life; s.Color = c;
            s.Sr.transform.position = new Vector3(p.x, p.y, 0f);
            s.Update(0f);
        }

        /// <summary>Anime speed lines radiating from a point.</summary>
        public static void SpeedBurst(Vector2 p, Color c, int count, float inner, float outer, bool black = false, float life = 0.18f)
        {
            if (I == null) return;
            count = Q(count);
            for (int i = 0; i < count; i++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                float r0 = inner * Random.Range(0.8f, 1.3f), r1 = outer * Random.Range(0.8f, 1.6f);
                I.StraightLine(p + d * r0, p + d * r1, black ? Color.black : Color.Lerp(c, Color.white, 0.5f), Random.Range(0.03f, 0.09f), life, LineFx.Kind.SpeedLine,
                               black ? Art.Line : Art.AddLine, black ? Art.OrderVfxTop : Art.OrderVfx, true);
            }
        }

        public static void Burst(BurstVis vis, Vector2 p, float radius, Color c)
        {
            if (I == null) return;
            switch (vis)
            {
                case BurstVis.Shockwave:
                    Ring(p, c, radius, 0.35f, 0.18f);
                    Ring(p, Color.white, radius * 0.7f, 0.25f, 0.08f);
                    Sparks(p, c, 16, radius * 6f);
                    break;
                case BurstVis.Explosion:
                case BurstVis.Fire:
                    Explosion(p, radius, c, ProjVis.Fire);
                    break;
                case BurstVis.Slashes:
                    for (int i = 0; i < Q(7); i++) I.ArcLine(p + Rnd(0f, radius * 0.5f), Random.Range(0f, 360f), radius * 0.4f, radius * 0.8f, Random.Range(60f, 140f), Color.Lerp(c, Color.white, 0.4f), 0.07f, 0.22f);
                    Sparks(p, c, 10, 10f);
                    break;
                case BurstVis.Pillar:
                    for (int i = 0; i < Q(30); i++) Emit(I.embers, p + new Vector2(Random.Range(-radius * 0.4f, radius * 0.4f), Random.Range(0f, 0.5f)), new Vector2(Random.Range(-1f, 1f), Random.Range(8f, 18f)), c, Random.Range(0.3f, 0.7f), Random.Range(0.3f, 0.6f));
                    Emit(I.glow, p + Vector2.up * 2f, Vector2.zero, c, radius * 2.5f, 0.3f);
                    break;
                case BurstVis.Sound:
                    for (int i = 0; i < 3; i++) Ring(p, c.WithA(0.7f - i * 0.2f), radius * (0.6f + i * 0.3f), 0.35f + i * 0.1f, 0.12f);
                    break;
                case BurstVis.Lightning:
                    for (int i = 0; i < Q(8); i++) Lightning(p, p + Rnd(radius * 0.5f, radius), c, 0.08f, 0.2f, 8);
                    Emit(I.glow, p, Vector2.zero, c, radius * 1.6f, 0.15f);
                    break;
                case BurstVis.Ice:
                    for (int i = 0; i < Q(18); i++) Emit(I.shards, p, Rnd(4f, 12f), Color.Lerp(c, Color.white, 0.5f), 0.12f, 0.5f);
                    Ring(p, c, radius, 0.4f, 0.1f);
                    break;
                case BurstVis.Bloom:
                    for (int i = 0; i < Q(24); i++) Emit(I.glow, p + Rnd(0f, radius * 0.6f), new Vector2(Random.Range(-1f, 1f), Random.Range(1f, 4f)), Color.Lerp(c, new Color(1f, 0.7f, 0.9f), Random.value), Random.Range(0.25f, 0.5f), Random.Range(0.6f, 1.2f));
                    break;
                case BurstVis.Gravity:
                    for (int i = 0; i < Q(20); i++) { var o = Rnd(radius * 0.8f, radius); Emit(I.sparks, p + o, -o * 3f, c, 0.12f, 0.33f); }
                    Ring(p, c, radius, 0.4f, 0.1f);
                    break;
                case BurstVis.Blood:
                    for (int i = 0; i < Q(20); i++) Emit(I.debris, p, Rnd(3f, 10f) + Vector2.up * 2f, new Color(0.75f, 0.05f, 0.1f), Random.Range(0.08f, 0.18f), Random.Range(0.4f, 0.8f));
                    Ring(p, c, radius, 0.3f, 0.12f);
                    break;
                case BurstVis.Water:
                    for (int i = 0; i < Q(28); i++) Emit(I.debris, p, Rnd(3f, 11f) + Vector2.up * 4f, Color.Lerp(c, Color.white, Random.value * 0.5f), Random.Range(0.08f, 0.2f), Random.Range(0.5f, 0.9f));
                    break;
                case BurstVis.Light:
                    Emit(I.glow, p, Vector2.zero, Color.white, radius * 2f, 0.12f);
                    Emit(I.glow, p, Vector2.zero, c, radius * 3f, 0.25f);
                    break;
                case BurstVis.Dark:
                    I.FlashSprite(p, Art.Soft, Art.AlphaSoft, new Color(0.02f, 0f, 0.04f, 0.9f), radius * 0.5f, radius * 2.5f, 0.4f, SpriteFx.Kind.Ink, Art.OrderVfx);
                    Sparks(p, c, 12, 8f);
                    break;
                case BurstVis.Stars:
                    for (int i = 0; i < Q(16); i++) Emit(I.glow, p + Rnd(0f, radius), Vector2.zero, Color.Lerp(c, Color.white, 0.6f), Random.Range(0.15f, 0.35f), Random.Range(0.4f, 0.8f));
                    break;
            }
        }

        public static void Explosion(Vector2 p, float r, Color c, ProjVis vis)
        {
            if (I == null) return;
            Emit(I.glow, p, Vector2.zero, Color.white, r * 1.6f, 0.12f);
            Emit(I.glow, p, Vector2.zero, c, r * 3f, 0.3f);
            Sparks(p, c, 26, r * 9f, 0.16f);
            for (int i = 0; i < Q(10); i++) Emit(I.smoke, p + Rnd(0f, r * 0.4f), Rnd(0.5f, 2.5f), new Color(0.15f, 0.13f, 0.15f, 0.55f), Random.Range(r * 0.6f, r * 1.2f), Random.Range(0.6f, 1.1f));
            if (vis == ProjVis.Fire) for (int i = 0; i < Q(18); i++) Emit(I.embers, p, Rnd(2f, 8f), new Color(1f, Random.Range(0.4f, 0.8f), 0.1f), Random.Range(0.1f, 0.25f), Random.Range(0.4f, 0.9f));
            Ring(p, c, r * 1.1f, 0.3f, 0.2f);
            Ring(p, Color.white, r * 0.8f, 0.2f, 0.1f);
        }

        public static void Aura(Vector2 p, Color c, float size)
        {
            if (I == null) return;
            for (int i = 0; i < Q(20); i++) Emit(I.embers, p + new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-1f, 0.8f)), new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(2f, 5f)), c, Random.Range(0.15f, 0.35f) * size, Random.Range(0.4f, 0.8f));
            Ring(p, c, 1.6f * size, 0.45f, 0.08f);
        }

        /// <summary>Cursed-energy flames rising off a fighter (called by the rig while charging).</summary>
        public static void AuraTick(Vector2 feet, float height, Color c, float intensity)
        {
            if (I == null || Random.value > intensity * Settings.VfxMul) return;
            Emit(I.embers, feet + new Vector2(Random.Range(-0.45f, 0.45f), Random.Range(0.2f, height)), new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(1.5f, 3.5f)), c.WithA(0.8f), Random.Range(0.12f, 0.3f), Random.Range(0.25f, 0.5f));
        }

        public static void Charge(Vector2 p, Color c, float duration)
        {
            if (I == null) return;
            I.FlashSprite(p, Art.Soft, Art.AddSoft, c, 2.2f, 0.3f, Mathf.Max(0.1f, duration), SpriteFx.Kind.Orb);
            for (int i = 0; i < Q(10); i++) { var o = Rnd(1.2f, 2f); Emit(I.sparks, p + o, -o / Mathf.Max(0.15f, duration), c, 0.1f, Mathf.Max(0.1f, duration)); }
        }

        public static void Summon(Vector2 p, Color c)
        {
            if (I == null) return;
            I.FlashSprite(p, Art.Soft, Art.AlphaSoft, new Color(0.02f, 0.02f, 0.05f, 0.85f), 0.4f, 2.4f, 0.5f, SpriteFx.Kind.Ink, Art.OrderMinion - 1);
            Ring(p, c, 1.8f, 0.4f, 0.1f);
            for (int i = 0; i < Q(14); i++) Emit(I.embers, p + new Vector2(Random.Range(-0.8f, 0.8f), -0.5f), new Vector2(0f, Random.Range(2f, 6f)), c, 0.25f, 0.5f);
        }

        public static void Teleport(Vector2 p, Color c)
        {
            if (I == null) return;
            Emit(I.glow, p, Vector2.zero, c, 1.6f, 0.12f);
            for (int i = 0; i < Q(10); i++) Emit(I.sparks, p, new Vector2(Random.Range(-1f, 1f), Random.Range(-6f, 6f)), c, 0.1f, 0.2f);
        }

        public static void Heal(Vector2 p, Color c)
        {
            if (I == null) return;
            for (int i = 0; i < Q(4); i++) Emit(I.glow, p + new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-0.9f, 0.5f)), new Vector2(0f, Random.Range(1f, 2.5f)), c, Random.Range(0.15f, 0.3f), 0.6f);
        }

        public static void ProjectilePop(Vector2 p, Color c, float size)
        {
            if (I == null) return;
            Emit(I.glow, p, Vector2.zero, c, size * 2.5f, 0.15f);
            Sparks(p, c, 8, 6f);
        }

        public static void Puff(Vector2 p, Color c, float size)
        {
            if (I == null) return;
            for (int i = 0; i < Q(8); i++) Emit(I.smoke, p + Rnd(0f, 0.4f * size), Rnd(0.5f, 2f), new Color(0.05f, 0.05f, 0.08f, 0.6f), Random.Range(0.5f, 1f) * size, 0.6f);
            Sparks(p, c, 6, 4f);
        }

        public static void GroundImpact(Vector2 p, Color c)
        {
            if (I == null) return;
            Dust(p, 10);
            for (int i = 0; i < Q(12); i++) Emit(I.debris, p, new Vector2(Random.Range(-5f, 5f), Random.Range(4f, 10f)), new Color(0.35f, 0.33f, 0.36f), Random.Range(0.08f, 0.18f), Random.Range(0.5f, 0.9f));
            I.StraightLine(p + new Vector2(-1.8f, 0.02f), p + new Vector2(1.8f, 0.02f), c, 0.12f, 0.25f, LineFx.Kind.Static, Art.AddLine, Art.OrderVfx);
        }

        public static void WallImpact(Vector2 p, int dir, Color c)
        {
            if (I == null) return;
            for (int i = 0; i < Q(5); i++) Lightning(p, p + new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(-2f, 2f)), new Color(0.8f, 0.8f, 0.85f), 0.05f, 0.5f, 6, Art.Line, Art.OrderFighter - 1);
            for (int i = 0; i < Q(12); i++) Emit(I.debris, p, new Vector2(dir * Random.Range(2f, 7f), Random.Range(-2f, 6f)), new Color(0.4f, 0.4f, 0.45f), 0.14f, 0.8f);
            SpeedBurst(p, c, 8, 1f, 3f);
        }

        public static void InfinityRipple(Vector2 p, Color c)
        {
            if (I == null) return;
            Ring(p, new Color(0.6f, 0.85f, 1f, 0.7f), 0.9f, 0.3f, 0.04f);
            Ring(p, new Color(0.9f, 0.95f, 1f, 0.5f), 0.5f, 0.4f, 0.03f);
        }

        public static void CounterFlash(Vector2 p, int facing, Color c)
        {
            if (I == null) return;
            I.ArcLine(p, facing > 0 ? 0f : 180f, 0.8f, 1.6f, 170f, Color.white, 0.18f, 0.2f);
            I.ArcLine(p, facing > 0 ? 0f : 180f, 0.9f, 1.8f, 150f, c, 0.1f, 0.25f);
            SpeedBurst(p, c, 14, 1f, 4f);
        }

        public static void Slash(Vector2 p, int facing, Color c, float size)
        {
            if (I == null) return;
            I.ArcLine(p, facing > 0 ? Random.Range(-30f, 30f) : 180f + Random.Range(-30f, 30f), 0.4f * size, 0.8f * size, 120f, c, 0.08f, 0.16f);
        }

        public static void SlashMark(Vector2 p, Color c)
        {
            if (I == null) return;
            float a = Random.Range(0f, 180f) * Mathf.Deg2Rad;
            Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(0.6f, 1.2f);
            Vector2 o = Rnd(0f, 0.5f);
            I.StraightLine(p + o - d, p + o + d, Color.Lerp(c, Color.white, 0.5f), 0.06f, 0.14f, LineFx.Kind.Static, Art.AddLine, Art.OrderVfxTop);
            Emit(I.debris, p + o, Rnd(1f, 4f), new Color(0.7f, 0.05f, 0.08f), 0.08f, 0.4f);
        }

        public static void Tongue(Vector2 a, Vector2 b, Color c)
        {
            if (I == null) return;
            I.StraightLine(a, b, c, 0.14f, 0.18f, LineFx.Kind.Static, Art.Line, Art.OrderMinion);
        }

        public static void Tendrils(Vector2 p, Color c)
        {
            if (I == null) return;
            for (int i = 0; i < Q(5); i++)
            {
                var l = I.GetLine(10, Art.Line, Art.OrderFighter + 1);
                l.K = LineFx.Kind.Tendril; l.A = p + new Vector2(Random.Range(-0.6f, 0.6f), 0f); l.R0 = Random.Range(0f, 6f); l.R1 = Random.Range(1.4f, 2.4f);
                l.Color = new Color(0.03f, 0.02f, 0.05f, 0.95f); l.Width = 0.12f; l.Life = 0.9f;
                l.Update(0f);
            }
        }

        public static void SoulTouch(Vector2 p, Color c)
        {
            if (I == null) return;
            I.FlashSprite(p, Art.Ring, Art.AddSoft, c, 0.2f, 3f, 0.5f);
            for (int i = 0; i < Q(5); i++) { var o = Rnd(1.5f, 2.5f); Lightning(p + o, p, c, 0.06f, 0.4f, 7); }
            Sparks(p, c, 16, 8f);
        }

        public static void Info(Vector2 p)
        {
            if (I == null) return;
            for (int i = 0; i < Q(6); i++) Emit(I.sparks, p + Rnd(1f, 2f), Rnd(2f, 5f), new Color(0.8f, 0.95f, 1f), 0.06f, 0.4f);
        }

        public static void HandSign(Fighter f, Color c)
        {
            if (I == null) return;
            Vector2 hands = f.Front(0.35f, 1.35f);
            Emit(I.glow, hands, Vector2.zero, Color.white, 0.8f, 0.4f);
            Emit(I.glow, hands, Vector2.zero, c, 2.5f, 0.9f);
            Ring(f.Center, c, 3f * f.Size, 0.9f, 0.1f);
            SpeedBurst(hands, c, 18, 1.5f, 6f, false, 0.5f);
        }

        public static void Shatter(Vector2 p, Color c)
        {
            if (I == null) return;
            for (int i = 0; i < Q(40); i++) Emit(I.shards, p + Rnd(0f, 3f), Rnd(5f, 16f), Color.Lerp(c, Color.white, Random.value), Random.Range(0.08f, 0.2f), Random.Range(0.5f, 1.1f));
            Emit(I.glow, p, Vector2.zero, c, 6f, 0.3f);
        }

        public static void KO(Vector2 p, Color c)
        {
            if (I == null) return;
            Explosion(p, 2.4f, c, ProjVis.Orb);
            SpeedBurst(p, Color.white, 26, 1.5f, 9f, false, 0.45f);
        }

        /// <summary>The Black Flash: distorted space, black lightning cracking through red light.</summary>
        public static void BlackFlash(Vector2 p, int facing, int streak)
        {
            if (I == null) return;
            ImpactFrames.Play(ImpactKind.BlackFlash);
            var red = new Color(0.95f, 0.06f, 0.12f);
            int bolts = Q(14 + streak * 3);
            for (int i = 0; i < bolts; i++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                float len = Random.Range(2.5f, 7f);
                Vector2 end = p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * len;
                Lightning(p, end, Color.black, Random.Range(0.08f, 0.2f), Random.Range(0.35f, 0.7f), 12, Art.Line, Art.OrderVfxTop, true);
                if (i % 2 == 0) Lightning(p, end, red, Random.Range(0.12f, 0.25f), Random.Range(0.25f, 0.5f), 12, Art.AddLine, Art.OrderVfx, true);
            }
            Emit(I.glow, p, Vector2.zero, red, 9f, 0.4f);
            Emit(I.glow, p, Vector2.zero, Color.white, 3f, 0.12f);
            for (int i = 0; i < Q(50); i++) Emit(I.sparks, p, Cone(facing, 75f, 10f, 34f), Random.value < 0.5f ? red : Color.white, Random.Range(0.12f, 0.24f), Random.Range(0.2f, 0.5f));
            SpeedBurst(p, Color.black, 30, 2f, 12f, true, 0.4f);
            I.FlashSprite(p, Art.Soft, Art.AlphaSoft, new Color(0f, 0f, 0f, 0.95f), 0.5f, 5f, 0.45f, SpriteFx.Kind.Ink, Art.OrderVfx - 1);
            Ring(p, red, 6f, 0.5f, 0.3f);
            Ring(p, Color.black, 4f, 0.4f, 0.2f);
            CameraRig.Punch(0.55f);
            CameraRig.Shake(0.9f);
            Popups.BlackFlash(p, streak);
        }

        public static void ImpactFrame(ImpactKind kind, Vector2 p)
        {
            ImpactFrames.Play(kind);
            if (kind == ImpactKind.Heavy) SpeedBurst(p, Color.white, 8, 1.5f, 4f, false, 0.12f);
            if (kind == ImpactKind.Domain || kind == ImpactKind.Ko) SpeedBurst(p, Color.black, 24, 2.5f, 12f, true, 0.35f);
        }

        /// <summary>Ambient particles for a hazard zone (called every frame by the zone view).</summary>
        public static void ZoneTick(ZoneVis vis, Vector2 pos, float width, float height, Color c, float alpha)
        {
            if (I == null || alpha <= 0.01f) return;
            float rate = 0.6f * Settings.VfxMul * alpha;
            if (Random.value > rate) return;
            Vector2 p = pos + new Vector2(Random.Range(-width * 0.5f, width * 0.5f), Random.Range(0f, 0.3f));
            switch (vis)
            {
                case ZoneVis.Fire: Emit(I.embers, p, new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(2f, 5f)), c, Random.Range(0.2f, 0.5f), Random.Range(0.3f, 0.7f)); break;
                case ZoneVis.Flowers: Emit(I.glow, p, new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(0.5f, 1.5f)), Color.Lerp(c, new Color(1f, 0.75f, 0.9f), Random.value), 0.22f, 1.2f); break;
                case ZoneVis.Ice: Emit(I.shards, p, new Vector2(0f, Random.Range(1f, 3f)), Color.Lerp(c, Color.white, 0.5f), 0.08f, 0.5f); break;
                case ZoneVis.Water: Emit(I.debris, p, new Vector2(Random.Range(-1f, 1f), Random.Range(2f, 5f)), c, 0.1f, 0.6f); break;
                case ZoneVis.Lightning: Lightning(p, p + new Vector2(Random.Range(-0.5f, 0.5f), height), c, 0.05f, 0.12f, 6); break;
                case ZoneVis.Shadow:
                case ZoneVis.Gravity:
                case ZoneVis.Rot:
                case ZoneVis.Smoke: Emit(I.smoke, p, new Vector2(0f, Random.Range(0.4f, 1.4f)), new Color(c.r * 0.3f, c.g * 0.3f, c.b * 0.3f, 0.5f), Random.Range(0.4f, 0.9f), 1f); break;
                case ZoneVis.Spikes: Emit(I.shards, p, new Vector2(0f, 6f), c, 0.12f, 0.25f); break;
                case ZoneVis.Blood: Emit(I.debris, p, new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(1f, 3f)), new Color(0.7f, 0.04f, 0.08f), 0.1f, 0.6f); break;
                case ZoneVis.Stars: Emit(I.glow, p + Vector2.up * Random.Range(0f, height), Vector2.zero, Color.Lerp(c, Color.white, 0.6f), 0.18f, 0.6f); break;
                case ZoneVis.Insects: Emit(I.sparks, p + Vector2.up * Random.Range(0f, height), Rnd(1f, 3f), new Color(0.15f, 0.12f, 0.1f), 0.07f, 0.4f); break;
                case ZoneVis.Sound: Ring(p + Vector2.up * height * 0.5f, c, 1f, 0.3f, 0.04f); break;
            }
        }

        public static void WorldText(Vector2 p, string text, Color c, float scale) => Popups.World(p, text, c, scale);
        public static void DamageNumber(Vector2 p, float dmg, bool bf, Color c) => Popups.Damage(p, dmg, bf, c);
        public static void Chant(Fighter f, string line, Color c) => Popups.Chant(f, line, c);

        public static void Afterimage(Fighter f, float life)
        {
            if (I == null || f.Rig == null || Settings.VfxQuality == 0) return;
            Ghost g = null;
            for (int i = 0; i < I.ghosts.Count; i++) if (!I.ghosts[i].Active) { g = I.ghosts[i]; break; }
            if (g == null)
            {
                if (I.ghosts.Count >= 24) return;
                g = new Ghost(I.transform);
                I.ghosts.Add(g);
            }
            g.Begin(f.Rig, f.Def.Look.Aura, life);
        }

        public static void Clear()
        {
            if (I == null) return;
            foreach (var l in I.lines) if (l.Active) l.Stop();
            foreach (var s in I.sprites) if (s.Active) { s.Active = false; s.Sr.enabled = false; }
            foreach (var g in I.ghosts) g.Stop();
            I.sparks.Clear(); I.glow.Clear(); I.smoke.Clear(); I.debris.Clear(); I.shards.Clear(); I.embers.Clear();
        }
    }

    /// <summary>A fading copy of a fighter's pose (dash trails, fast strikes).</summary>
    public sealed class Ghost
    {
        readonly LineRenderer[] lr = new LineRenderer[4];
        public bool Active;
        float age, life;
        Color color;

        public Ghost(Transform parent)
        {
            for (int i = 0; i < lr.Length; i++)
            {
                lr[i] = Art.NewLine(parent, "Ghost", Art.AddLine, Art.OrderFighterGlow - 1, 0.1f, 5);
                lr[i].enabled = false;
            }
        }

        public void Begin(StickRig rig, Color c, float life)
        {
            this.life = life; age = 0f; color = c; Active = true;
            for (int i = 0; i < lr.Length; i++)
            {
                var src = rig.Line(i);
                if (src == null) { lr[i].enabled = false; continue; }
                int n = src.positionCount;
                lr[i].positionCount = n;
                for (int k = 0; k < n; k++) lr[i].SetPosition(k, src.GetPosition(k));
                lr[i].loop = src.loop;
                lr[i].widthMultiplier = src.widthMultiplier * 1.2f;
                lr[i].enabled = true;
            }
            Update(0f);
        }

        public void Update(float dt)
        {
            age += dt;
            if (age >= life) { Stop(); return; }
            float a = (1f - age / life) * 0.55f;
            var c = color; c.a = a;
            for (int i = 0; i < lr.Length; i++) if (lr[i].enabled) { lr[i].startColor = c; lr[i].endColor = c; }
        }

        public void Stop()
        {
            Active = false;
            for (int i = 0; i < lr.Length; i++) lr[i].enabled = false;
        }
    }
}
