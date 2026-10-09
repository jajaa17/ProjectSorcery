using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectSorcery
{
    /// <summary>Builds uGUI elements in code with a consistent dark, ink-and-neon style.</summary>
    public static class UIKit
    {
        public static readonly Color Ink = new Color(0.03f, 0.03f, 0.05f, 0.92f);
        public static readonly Color Panel = new Color(0.06f, 0.06f, 0.09f, 0.86f);
        public static readonly Color Accent = new Color(0.55f, 0.35f, 1f);
        public static readonly Color Accent2 = new Color(1f, 0.25f, 0.4f);
        public static readonly Color TextDim = new Color(0.72f, 0.72f, 0.8f);

        public static RectTransform Rect(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 offMin = default, Vector2 offMax = default)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
            return rt;
        }

        /// <summary>Anchored at a point with a fixed size.</summary>
        public static RectTransform Box(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Img(RectTransform rt, Color c, Sprite sprite = null, bool raycast = false)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            img.sprite = sprite;
            img.raycastTarget = raycast;
            return img;
        }

        public static Image Fill(Transform parent, string name, Color c)
        {
            var rt = Rect(parent, name, Vector2.zero, Vector2.one);
            return Img(rt, c);
        }

        public static Text Label(Transform parent, string text, int size, Color c, TextAnchor align = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var rt = Rect(parent, "Label", Vector2.zero, Vector2.one);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Art.Font;
            t.text = text;
            t.fontSize = size;
            t.color = c;
            t.alignment = align;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            return t;
        }

        public static Text LabelBox(Transform parent, string text, int size, Color c, Vector2 anchor, Vector2 pos, Vector2 boxSize, TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var rt = Box(parent, "Label", anchor, pos, boxSize);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Art.Font; t.text = text; t.fontSize = size; t.color = c; t.alignment = align; t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false; t.supportRichText = true;
            return t;
        }

        public static void Outline(Graphic g, Color c, float dist = 2f)
        {
            var o = g.gameObject.AddComponent<Outline>();
            o.effectColor = c;
            o.effectDistance = new Vector2(dist, -dist);
        }

        public static void Shadow(Graphic g, Color c, float dist = 3f)
        {
            var o = g.gameObject.AddComponent<Shadow>();
            o.effectColor = c;
            o.effectDistance = new Vector2(dist, -dist);
        }

        /// <summary>A styled menu button with hover/selection animation and sounds.</summary>
        public static Button Button(Transform parent, string text, Vector2 anchor, Vector2 pos, Vector2 size, Action onClick, int fontSize = 30, Color? accent = null)
        {
            var rt = Box(parent, "Btn_" + text, anchor, pos, size);
            var bg = Img(rt, new Color(0.08f, 0.08f, 0.12f, 0.75f), null, true);
            var bar = Img(Rect(rt, "Bar", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(6f, 0f)), accent ?? Accent);
            var label = Label(rt, text, fontSize, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            label.rectTransform.offsetMin = new Vector2(24f, 0f);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = bg;
            var cb = b.colors;
            cb.normalColor = Color.white; cb.highlightedColor = Color.white; cb.selectedColor = Color.white; cb.pressedColor = new Color(0.8f, 0.8f, 0.8f); cb.fadeDuration = 0.05f;
            b.colors = cb;
            b.onClick.AddListener(() => { Audio.Ui(Sfx.UiConfirm); onClick?.Invoke(); });
            var fx = rt.gameObject.AddComponent<FancyButton>();
            fx.Init(bg, bar, label, accent ?? Accent);
            return b;
        }

        /// <summary>A "label  < value >" row for settings / setup screens.</summary>
        public static Stepper Stepper(Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size, Func<string> value, Action<int> change, int fontSize = 26)
        {
            var rt = Box(parent, "Step_" + label, anchor, pos, size);
            var bg = Img(rt, new Color(0.08f, 0.08f, 0.12f, 0.65f), null, true);
            var l = Label(rt, label, fontSize, TextDim, TextAnchor.MiddleLeft);
            l.rectTransform.offsetMin = new Vector2(20f, 0f);
            l.rectTransform.anchorMax = new Vector2(0.55f, 1f);
            var v = Label(rt, value(), fontSize, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            v.rectTransform.anchorMin = new Vector2(0.55f, 0f);
            var left = LabelBox(rt, "<", fontSize + 4, Accent, new Vector2(0.55f, 0.5f), new Vector2(20f, 0f), new Vector2(40f, 40f));
            var right = LabelBox(rt, ">", fontSize + 4, Accent, new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(40f, 40f));
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = bg;
            var st = rt.gameObject.AddComponent<Stepper>();
            st.Init(b, bg, v, value, change);
            b.onClick.AddListener(() => st.Change(1));
            return st;
        }

        public static Image Bar(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color back, Color fill, Vector2? pivot = null)
        {
            var rt = Box(parent, "Bar", anchor, pos, size, pivot);
            Img(rt, back);
            var f = Rect(rt, "Fill", Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
            var img = Img(f, fill, Art.White);
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = 0;
            img.fillAmount = 1f;
            return img;
        }

        /// <summary>Wires explicit vertical navigation through a list of selectables (gamepad / keyboard).</summary>
        public static void ChainVertical(IList<Selectable> items, bool wrap = true)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                nav.selectOnUp = i > 0 ? items[i - 1] : (wrap ? items[items.Count - 1] : null);
                nav.selectOnDown = i < items.Count - 1 ? items[i + 1] : (wrap ? items[0] : null);
                items[i].navigation = nav;
            }
        }

        public static void Select(Selectable s)
        {
            if (s == null || EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(s.gameObject);
        }

        static Sprite speedLines;
        /// <summary>A radial speed-lines texture for cut-ins.</summary>
        public static Sprite SpeedLines()
        {
            if (speedLines != null) return speedLines;
            var p = new TexPainter(512, 256, 77);
            p.Fill(new Color(0, 0, 0, 0));
            for (int i = 0; i < 140; i++)
            {
                float a = p.Rand(0f, Mathf.PI * 2f);
                float r0 = p.Rand(60f, 140f), r1 = p.Rand(260f, 420f);
                p.Line(256 + Mathf.Cos(a) * r0, 128 + Mathf.Sin(a) * r0 * 0.6f, 256 + Mathf.Cos(a) * r1, 128 + Mathf.Sin(a) * r1 * 0.6f, p.Rand(1f, 3.5f), new Color(1f, 1f, 1f, p.Rand(0.3f, 0.9f)));
            }
            var tex = p.ToTexture("speedlines");
            speedLines = Sprite.Create(tex, new Rect(0, 0, 512, 256), new Vector2(0.5f, 0.5f), 100f);
            return speedLines;
        }
    }

    /// <summary>Hover / select feedback for menu buttons.</summary>
    public sealed class FancyButton : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        Image bg, bar; Text label; Color accent;
        bool hot;
        float k;
        RectTransform rt;

        public void Init(Image bg, Image bar, Text label, Color accent)
        {
            this.bg = bg; this.bar = bar; this.label = label; this.accent = accent;
            rt = (RectTransform)transform;
        }

        public void OnSelect(BaseEventData e) { hot = true; Audio.Ui(Sfx.UiMove); }
        public void OnDeselect(BaseEventData e) { hot = false; }
        public void OnPointerEnter(PointerEventData e) { if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject); }
        public void OnPointerExit(PointerEventData e) { }

        void Update()
        {
            k = Mathf.MoveTowards(k, hot ? 1f : 0f, Time.unscaledDeltaTime * 8f);
            bg.color = Color.Lerp(new Color(0.08f, 0.08f, 0.12f, 0.75f), new Color(accent.r * 0.35f, accent.g * 0.35f, accent.b * 0.35f, 0.9f), k);
            var bt = (RectTransform)bar.transform;
            bt.offsetMax = new Vector2(6f + 10f * k, 0f);
            label.rectTransform.offsetMin = new Vector2(24f + 14f * k, 0f);
            float s = 1f + 0.03f * k;
            rt.localScale = new Vector3(s, s, 1f);
        }
    }

    /// <summary>Settings stepper: left/right (keyboard, gamepad or click) changes the value.</summary>
    public sealed class Stepper : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IMoveHandler
    {
        Button button; Image bg; Text value;
        Func<string> getter; Action<int> change;
        bool hot;
        public Selectable Selectable => button;

        public void Init(Button b, Image bg, Text value, Func<string> getter, Action<int> change)
        {
            button = b; this.bg = bg; this.value = value; this.getter = getter; this.change = change;
        }

        public void Change(int dir)
        {
            change?.Invoke(dir);
            value.text = getter();
            Audio.Ui(Sfx.UiMove);
        }

        public void Refresh() { value.text = getter(); }

        public void OnSelect(BaseEventData e) { hot = true; Audio.Ui(Sfx.UiMove); }
        public void OnDeselect(BaseEventData e) { hot = false; }
        public void OnPointerEnter(PointerEventData e) { if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject); }

        public void OnMove(AxisEventData e)
        {
            if (e.moveDir == MoveDirection.Left) { Change(-1); e.Use(); }
            else if (e.moveDir == MoveDirection.Right) { Change(1); e.Use(); }
        }

        void Update()
        {
            bg.color = hot ? new Color(0.25f, 0.15f, 0.45f, 0.9f) : new Color(0.08f, 0.08f, 0.12f, 0.65f);
        }
    }
}
