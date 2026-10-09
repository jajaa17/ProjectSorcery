using UnityEngine;

namespace ProjectSorcery
{
    /// <summary>Fighting-game camera: frames every fighter, punch-zooms on impacts, shakes, focuses on casters.</summary>
    public sealed class CameraRig : MonoBehaviour
    {
        public static CameraRig I;
        public Camera Cam;

        float shake, punch;
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

        public static void Shake(float amount) { if (I != null) I.shake = Mathf.Max(I.shake, amount * Settings.Shake); }
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
            shake = Mathf.MoveTowards(shake, 0f, dt * 2.2f);
            punch = Mathf.MoveTowards(punch, 0f, dt * 2.8f);
            float t = Time.unscaledTime * 38f;
            float s = shake * shake * 0.6f;
            Vector2 off = new Vector2((Mathf.PerlinNoise(seedX, t) - 0.5f) * 2f, (Mathf.PerlinNoise(seedY, t) - 0.5f) * 2f) * s;
            float z = size * (1f - punch * 0.12f);
            if (Cam != null) Cam.orthographicSize = z;
            if (punch <= 0f) punchAtSet = false;
            Vector2 lean = punchAtSet ? (punchAt - pos) * Mathf.Clamp01(punch) * 0.35f : Vector2.zero;
            transform.position = new Vector3(pos.x + off.x + lean.x, pos.y + off.y + lean.y, -10f);
            transform.rotation = Quaternion.Euler(0f, 0f, off.x * 1.5f);
        }
    }
}
