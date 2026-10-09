using UnityEngine;

namespace ProjectSorcery
{
    /// <summary>Fighting-game camera: frames every fighter, punch-zooms on impacts, shakes, focuses on casters.</summary>
    public sealed class CameraRig : MonoBehaviour
    {
        public static CameraRig I;
        public Camera Cam;

        float trauma, punch, ca;
        Vector2 kick, kickVel, kickDir;
        Vector2 punchAt; bool punchAtSet;
        Fighter focus; float focusTimer;
        Vector2 pos; float size = 6.5f;
        float seedX, seedY;

        public static CameraRig Create(Transform root)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 6.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.03f, 0.05f);
            cam.transform.position = new Vector3(0f, 3.5f, -10f);
            cam.nearClipPlane = 0.1f; cam.farClipPlane = 50f;
            var rig = cam.gameObject.GetComponent<CameraRig>();
            if (rig == null) rig = cam.gameObject.AddComponent<CameraRig>();
            rig.Cam = cam;
            I = rig;
            rig.seedX = Random.value * 100f; rig.seedY = Random.value * 100f;
            return rig;
        }

        /// <summary>Adds trauma (0..1). The shake is trauma squared, so small hits stay subtle and big ones explode.</summary>
        public static void Shake(float amount) { if (I != null) I.trauma = Mathf.Clamp01(Mathf.Max(I.trauma, amount * Settings.Shake) + amount * 0.25f * Settings.Shake); }

        /// <summary>Holds trauma at a floor (sustained rumble while a beam fires or a domain manifests).</summary>
        public static void Rumble(float amount) { if (I != null) I.trauma = Mathf.Max(I.trauma, amount * Settings.Shake); }

        /// <summary>Directional kick: the frame jolts along the blow's direction and springs back.</summary>
        public static void Kick(Vector2 dir, float amount) { if (I != null && dir.sqrMagnitude > 1e-4f) { I.kickVel += dir.normalized * amount * 9f * Settings.Shake; I.kickDir = dir.normalized; } }

        /// <summary>Shake profile for a landed hit: light = micro-shake, heavy = trauma + kick along the hit.</summary>
        public static void Hit(Vector2 dir, float weight)
        {
            if (weight < 0.6f) { Shake(0.08f + 0.06f * weight); Kick(dir, 0.05f); return; }
            Shake(0.12f + 0.16f * weight);
            Kick(dir, 0.1f + 0.1f * weight);
        }

        /// <summary>Chromatic-aberration pulse (fighters split into red/cyan ghosts for a moment).</summary>
        public static void Aberrate(float amount) { if (I != null) I.ca = Mathf.Max(I.ca, amount * Mathf.Lerp(0.4f, 1f, Settings.Shake)); }
        public static float Aberration => I != null ? I.ca : 0f;
        /// <summary>Direction the colour channels split along: the last hit's kick, horizontal by default.</summary>
        public static Vector2 AberrationDir => I != null && I.kickDir.sqrMagnitude > 1e-4f ? I.kickDir : Vector2.right;
        /// <summary>The full anime "big moment" stamp: aberration split, zoom toward the point and a hard shake.</summary>
        public static void Moment(Vector2 at, float amount)
        {
            Aberrate(amount);
            PunchAt(0.2f + 0.35f * amount, at);
            Shake(0.3f + 0.5f * amount);
        }
        public static void Punch(float amount) { if (I != null) I.punch = Mathf.Max(I.punch, amount * Mathf.Lerp(0.5f, 1f, Settings.Shake)); }
        /// <summary>Zoom-punch that also nudges the frame toward the impact point.</summary>
        public static void PunchAt(float amount, Vector2 at)
        {
            if (I == null) return;
            Punch(amount);
            I.punchAt = at; I.punchAtSet = true;
        }
        public static void Focus(Fighter f, float seconds) { if (I != null) { I.focus = f; I.focusTimer = seconds; } }

        public void SnapTo(Match m)
        {
            ComputeTarget(m, out var p, out var s);
            pos = p; size = s;
            Apply(0f);
        }

        void ComputeTarget(Match m, out Vector2 target, out float targetSize)
        {
            target = new Vector2(0f, 3.5f);
            targetSize = 6.5f;
            if (m == null || m.Fighters.Count == 0) return;
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            int n = 0;
            foreach (var f in m.Fighters)
            {
                if (f.State == FState.Respawning) continue;
                if (f.Dead && m.Fighters.Count > 2) continue;
                var p = f.Rig != null ? f.Rig.RenderPos : f.Pos;
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y + 2.2f * f.Size);
                n++;
            }
            if (n == 0) return;
            if (focus != null && focusTimer > 0f && !focus.Dead)
            {
                var fp = focus.Rig != null ? focus.Rig.RenderPos : focus.Pos;
                target = new Vector2(fp.x, fp.y + 1.6f);
                targetSize = 4.2f;
                return;
            }
            float aspect = Cam != null ? Cam.aspect : 16f / 9f;
            float spanX = (maxX - minX) + 7f;
            float spanY = (maxY - minY) + 4f;
            targetSize = Mathf.Clamp(Mathf.Max(spanX / aspect * 0.5f, spanY * 0.5f), 4.8f, 9.5f);
            float cx = (minX + maxX) * 0.5f;
            float cy = Mathf.Max(minY + targetSize - 1.4f, (minY + maxY) * 0.5f);
            // keep the floor visible
            cy = Mathf.Min(cy, minY + targetSize * 0.75f);
            float halfW = targetSize * aspect;
            cx = Mathf.Clamp(cx, m.Arena.Left - 1.5f + halfW, m.Arena.Right + 1.5f - halfW);
            if (halfW * 2f > m.Arena.Width + 3f) cx = (m.Arena.Left + m.Arena.Right) * 0.5f;
            target = new Vector2(cx, cy);
        }

        void LateUpdate()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            if (focusTimer > 0f) focusTimer -= dt;
            ComputeTarget(Match.I, out var tp, out var ts);
            float k = 1f - Mathf.Exp(-dt * (focusTimer > 0f ? 9f : 5f));
            pos = Vector2.Lerp(pos, tp, k);
            size = Mathf.Lerp(size, ts, k);
            Apply(dt);
            ImpactFrames.Update(Cam);
            DomainFX.Update(Cam);
        }

        void Apply(float dt)
        {
            trauma = Mathf.MoveTowards(trauma, 0f, dt * 1.6f);
            punch = Mathf.MoveTowards(punch, 0f, dt * 2.8f);
            ca = Mathf.MoveTowards(ca, 0f, dt * 3f);
            // kick spring: snaps out along the hit, springs back with a little overshoot
            kickVel += (-kick * 220f - kickVel * 16f) * dt;
            kick += kickVel * dt;
            float t = Time.unscaledTime * 38f;
            float s = trauma * trauma * 0.9f;
            Vector2 off = new Vector2((Mathf.PerlinNoise(seedX, t) - 0.5f) * 2f, (Mathf.PerlinNoise(seedY, t) - 0.5f) * 2f) * s + kick;
            float z = size * (1f - punch * 0.12f);
            if (Cam != null) Cam.orthographicSize = z;
            if (punch <= 0f) punchAtSet = false;
            Vector2 lean = punchAtSet ? (punchAt - pos) * Mathf.Clamp01(punch) * 0.35f : Vector2.zero;
            transform.position = new Vector3(pos.x + off.x + lean.x, pos.y + off.y + lean.y, -10f);
            transform.rotation = Quaternion.Euler(0f, 0f, off.x * 1.5f);
        }
    }
}
