using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectSorcery
{
    /// <summary>The single screen-space canvas and its layers.</summary>
    public static class UIRoot
    {
        public static Canvas Canvas;
        public static RectTransform Root, Hud, Popups, Screens, CutIns, Overlay;
        static Image fade;
        static float fadeTarget, fadeValue;

        public static void Init(Transform parent)
        {
            var go = new GameObject("UI", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Canvas = go.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = 10;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            Root = (RectTransform)go.transform;

            Hud = UIKit.Rect(Root, "HUD", Vector2.zero, Vector2.one);
            Popups = UIKit.Rect(Root, "Popups", Vector2.zero, Vector2.one);
            CutIns = UIKit.Rect(Root, "CutIns", Vector2.zero, Vector2.one);
            Screens = UIKit.Rect(Root, "Screens", Vector2.zero, Vector2.one);
            Overlay = UIKit.Rect(Root, "Overlay", Vector2.zero, Vector2.one);
            fade = UIKit.Fill(Overlay, "Fade", new Color(0f, 0f, 0f, 0f));

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
                es.transform.SetParent(parent, false);
#if ENABLE_INPUT_SYSTEM
                var module = es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                module.AssignDefaultActions();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }
        }

        /// <summary>Screen fade to black (for transitions). 1 = black.</summary>
        public static void FadeTo(float target) { fadeTarget = target; }
        public static void SetFade(float v) { fadeTarget = fadeValue = v; fade.color = new Color(0f, 0f, 0f, v); }

        public static void Update()
        {
            fadeValue = Mathf.MoveTowards(fadeValue, fadeTarget, Time.unscaledDeltaTime * 3.5f);
            fade.color = new Color(0f, 0f, 0f, fadeValue);
            fade.raycastTarget = fadeValue > 0.5f;
        }

        /// <summary>True while the player is typing in a text field (menu shortcuts are suppressed).</summary>
        public static bool Typing
        {
            get
            {
                var es = EventSystem.current;
                if (es == null || es.currentSelectedGameObject == null) return false;
                var f = es.currentSelectedGameObject.GetComponent<InputField>();
                return f != null && f.isFocused;
            }
        }

        public static bool WorldToCanvas(Vector2 world, out Vector2 local)
        {
            local = Vector2.zero;
            var cam = CameraRig.I != null ? CameraRig.I.Cam : null;
            if (cam == null) return false;
            Vector3 sp = cam.WorldToScreenPoint(new Vector3(world.x, world.y, 0f));
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(Popups, sp, null, out local);
        }
    }
}
