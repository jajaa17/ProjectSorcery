// Minimal stand-ins for the subset of the Unity API used by Project Sorcery, so the gameplay code can run headless.
// Math types behave like Unity's; rendering, audio and UI types are inert.
#pragma warning disable
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public struct Vector2 : IEquatable<Vector2>
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new Vector2(0, 0); public static Vector2 one => new Vector2(1, 1);
        public static Vector2 up => new Vector2(0, 1); public static Vector2 down => new Vector2(0, -1);
        public static Vector2 right => new Vector2(1, 0); public static Vector2 left => new Vector2(-1, 0);
        public float sqrMagnitude => x * x + y * y; public float magnitude => (float)Math.Sqrt(x * x + y * y);
        public Vector2 normalized { get { float m = magnitude; return m > 1e-5f ? new Vector2(x / m, y / m) : zero; } }
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator -(Vector2 a) => new Vector2(-a.x, -a.y);
        public static Vector2 operator *(Vector2 a, float d) => new Vector2(a.x * d, a.y * d);
        public static Vector2 operator *(float d, Vector2 a) => new Vector2(a.x * d, a.y * d);
        public static Vector2 operator /(Vector2 a, float d) => new Vector2(a.x / d, a.y / d);
        public static bool operator ==(Vector2 a, Vector2 b) => (a - b).sqrMagnitude < 1e-10f;
        public static bool operator !=(Vector2 a, Vector2 b) => !(a == b);
        public static implicit operator Vector3(Vector2 v) => new Vector3(v.x, v.y, 0);
        public static implicit operator Vector2(Vector3 v) => new Vector2(v.x, v.y);
        public static float Dot(Vector2 a, Vector2 b) => a.x * b.x + a.y * b.y;
        public static float Distance(Vector2 a, Vector2 b) => (a - b).magnitude;
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) { t = Mathf.Clamp01(t); return a + (b - a) * t; }
        public static Vector2 LerpUnclamped(Vector2 a, Vector2 b, float t) => a + (b - a) * t;
        public static Vector2 MoveTowards(Vector2 a, Vector2 b, float d) { var v = b - a; float m = v.magnitude; if (m <= d || m == 0) return b; return a + v / m * d; }
        public bool Equals(Vector2 o) => this == o; public override bool Equals(object o) => o is Vector2 v && this == v; public override int GetHashCode() => x.GetHashCode() ^ y.GetHashCode();
        public override string ToString() => $"({x:F2},{y:F2})";
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public Vector3(float x, float y) { this.x = x; this.y = y; z = 0; }
        public static Vector3 zero => default; public static Vector3 one => new Vector3(1, 1, 1); public static Vector3 up => new Vector3(0, 1, 0);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator *(Vector3 a, float d) => new Vector3(a.x * d, a.y * d, a.z * d);
        public static Vector3 operator *(float d, Vector3 a) => a * d;
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a + (b - a) * Mathf.Clamp01(t);
    }
    public struct Vector4 { public float x, y, z, w; }
    public struct Quaternion
    {
        public static Quaternion identity => default;
        public static Quaternion Euler(float x, float y, float z) => default;
    }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; a = 1; }
        public static Color white => new Color(1, 1, 1, 1); public static Color black => new Color(0, 0, 0, 1); public static Color red => new Color(1, 0, 0, 1);
        public static Color clear => new Color(0, 0, 0, 0); public static Color gray => new Color(.5f, .5f, .5f, 1);
        public static Color Lerp(Color a, Color b, float t) { t = Mathf.Clamp01(t); return new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t, a.a + (b.a - a.a) * t); }
        public static Color HSVToRGB(float h, float s, float v)
        {
            h = (h % 1f + 1f) % 1f * 6f; int i = (int)Math.Floor(h); float f = h - i;
            float p = v * (1 - s), q = v * (1 - s * f), t = v * (1 - s * (1 - f));
            switch (i % 6) { case 0: return new Color(v, t, p); case 1: return new Color(q, v, p); case 2: return new Color(p, v, t); case 3: return new Color(p, q, v); case 4: return new Color(t, p, v); default: return new Color(v, p, q); }
        }
        public static Color operator *(Color a, float b) => new Color(a.r * b, a.g * b, a.b * b, a.a * b);
        public static Color operator +(Color a, Color b) => new Color(a.r + b.r, a.g + b.g, a.b + b.b, a.a + b.a);
        public static implicit operator Color(Color32 c) => new Color(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f);
        public static bool operator ==(Color a, Color b) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;
        public static bool operator !=(Color a, Color b) => !(a == b);
        public override bool Equals(object o) => o is Color c && this == c; public override int GetHashCode() => 0;
    }
    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static implicit operator Color32(Color c) => new Color32(B(c.r), B(c.g), B(c.b), B(c.a));
        static byte B(float v) => (byte)Math.Round(Math.Clamp(v, 0f, 1f) * 255f);
    }
    public struct Rect
    {
        public Rect(float x, float y, float w, float h) { xMin = x; yMin = y; xMax = x + w; yMax = y + h; width = w; height = h; }
        public float xMin, yMin, xMax, yMax, width, height;
    }
    public struct Resolution { public int width, height; }
    public static class Mathf
    {
        public const float PI = (float)Math.PI, Deg2Rad = (float)(Math.PI / 180.0), Rad2Deg = (float)(180.0 / Math.PI), Infinity = float.PositiveInfinity;
        public static float Sin(float f) => (float)Math.Sin(f); public static float Cos(float f) => (float)Math.Cos(f); public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
        public static float Sqrt(float f) => (float)Math.Sqrt(f); public static float Abs(float f) => Math.Abs(f); public static int Abs(int f) => Math.Abs(f);
        public static float Min(float a, float b) => a < b ? a : b; public static int Min(int a, int b) => a < b ? a : b; public static float Max(float a, float b) => a > b ? a : b; public static int Max(int a, int b) => a > b ? a : b;
        public static float Pow(float a, float b) => (float)Math.Pow(a, b); public static float Exp(float a) => (float)Math.Exp(a); public static float Sign(float f) => f >= 0f ? 1f : -1f;
        public static float Clamp(float v, float a, float b) => v < a ? a : v > b ? b : v; public static int Clamp(int v, int a, int b) => v < a ? a : v > b ? b : v; public static float Clamp01(float v) => Clamp(v, 0, 1); public static float Round(float f) => (float)Math.Round(f); public static float Acos(float f) => (float)Math.Acos(f); public static float InverseLerp(float a, float b, float v) => a != b ? Clamp01((v - a) / (b - a)) : 0f;
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float Repeat(float t, float l) => Clamp(t - (float)Math.Floor(t / l) * l, 0f, l);
        public static float DeltaAngle(float a, float b) { float d = Repeat(b - a, 360f); if (d > 180f) d -= 360f; return d; }
        public static float LerpAngle(float a, float b, float t) => a + DeltaAngle(a, b) * Clamp01(t);
        public static float MoveTowards(float a, float b, float d) => Math.Abs(b - a) <= d ? b : a + Math.Sign(b - a) * d;
        public static float PingPong(float t, float l) { t = Repeat(t, l * 2f); return l - Math.Abs(t - l); }
        public static float PerlinNoise(float x, float y) => 0.5f;
        public static int RoundToInt(float f) => (int)Math.Round(f); public static int CeilToInt(float f) => (int)Math.Ceiling(f); public static int FloorToInt(float f) => (int)Math.Floor(f);
    }
    public static class Random
    {
        static System.Random r = new System.Random(1);
        public static float Range(float a, float b) => a + (float)r.NextDouble() * (b - a); public static int Range(int a, int b) => b <= a ? a : r.Next(a, b);
        public static float value => (float)r.NextDouble(); public static Vector2 insideUnitCircle => new Vector2(value - .5f, value - .5f);
    }
    public static class Time { public static float unscaledDeltaTime, unscaledTime, deltaTime, time; }
    public static class Application
    {
        public static string version => ""; public static int targetFrameRate; public static bool runInBackground; public static bool isBatchMode; public static void Quit() { }
    }
    public static class Debug { public static void Log(object o) { } public static void LogWarning(object o) { } public static void LogError(object o) { } }
    public static class Screen
    {
        public static Resolution[] resolutions => null; public static Resolution currentResolution => default;
        public static void SetResolution(int w, int h, FullScreenMode m) { }
        public static int width, height;
    }
    public enum FullScreenMode { ExclusiveFullScreen, FullScreenWindow, MaximizedWindow, Windowed }
    public static class QualitySettings { public static int vSyncCount; }
    public static class PlayerPrefs
    {
        public static float GetFloat(string k, float d) => d; public static void SetFloat(string k, float v) { }
        public static int GetInt(string k, int d) => d; public static void SetInt(string k, int v) { }
        public static string GetString(string k, string d) => d; public static void SetString(string k, string v) { }
        public static void Save() { }
    }
    public class WaitForSecondsRealtime { public WaitForSecondsRealtime(float s) { } }
    public class WaitForEndOfFrame { }
    public static class ScreenCapture { public static Texture2D CaptureScreenshotAsTexture() => new Texture2D(1, 1, TextureFormat.RGBA32, false); }
    public static class ImageConversion { public static byte[] EncodeToPNG(this Texture2D t) => new byte[0]; }
    public static class ColorUtility
    {
        public static bool TryParseHtmlString(string s, out Color c)
        {
            c = default;
            if (string.IsNullOrEmpty(s) || s[0] != '#' || (s.Length != 7 && s.Length != 9)) return false;
            uint v = Convert.ToUInt32(s.Substring(1), 16); if (s.Length == 7) v = (v << 8) | 0xFF;
            c = new Color(((v >> 24) & 255) / 255f, ((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f);
            return true;
        }
        public static string ToHtmlStringRGB(Color c) => "";
    }
    public class Object
    {
        public string name;
        public static void Destroy(Object o) { }
        public static void DontDestroyOnLoad(Object o) { }
        public static T FindFirstObjectByType<T>() where T : Object => null;
        public static implicit operator bool(Object o) => o != null;
    }
    public class Component : Object
    {
        public GameObject gameObject => null; public Transform transform => null;
        public T GetComponent<T>() => default; public string tag;
    }
    public class Behaviour : Component { public bool enabled; }
    public class MonoBehaviour : Behaviour { }
    public sealed class GameObject : Object
    {
        public GameObject() { } public GameObject(string name) { } public GameObject(string name, params Type[] comps) { }
        public Transform transform => null; public T AddComponent<T>() where T : Component => null; public T GetComponent<T>() => default;
        public void SetActive(bool v) { } public bool activeSelf => true; public string tag;
    }
    public class Transform : Component, IEnumerable
    {
        public Vector3 position, localPosition, localScale; public Quaternion rotation, localRotation;
        public Transform parent => null;
        public void SetParent(Transform p, bool worldPositionStays) { } public void SetParent(Transform p) { }
        public void Rotate(float x, float y, float z) { } public void SetAsFirstSibling() { }
        public Vector3 TransformPoint(Vector3 p) => p;
        public IEnumerator GetEnumerator() => null;
    }
    public sealed class RectTransform : Transform
    {
        public Vector2 anchorMin, anchorMax, offsetMin, offsetMax, pivot, anchoredPosition, sizeDelta;
    }
    public static class RectTransformUtility
    {
        public static bool ScreenPointToLocalPointInRectangle(RectTransform r, Vector2 sp, Camera c, out Vector2 local) { local = default; return true; }
    }
    public class Renderer : Component
    {
        public bool enabled; public Material material, sharedMaterial; public int sortingOrder;
        public Rendering.ShadowCastingMode shadowCastingMode; public bool receiveShadows;
    }
    public enum LineTextureMode { Stretch, Tile }
    public sealed class LineRenderer : Renderer
    {
        public int positionCount; public bool useWorldSpace, loop; public float widthMultiplier, startWidth, endWidth;
        public int numCapVertices, numCornerVertices; public Color startColor, endColor; public LineTextureMode textureMode;
        public void SetPosition(int i, Vector3 p) { } public Vector3 GetPosition(int i) => default; public void SetPositions(Vector3[] p) { }
    }
    public sealed class TrailRenderer : Renderer
    {
        public float time, minVertexDistance, widthMultiplier; public AnimationCurve widthCurve; public bool emitting;
        public int numCapVertices; public Color startColor, endColor; public void Clear() { }
    }
    public sealed class SpriteRenderer : Renderer { public Sprite sprite; public Color color; public bool flipX; }
    public sealed class MeshRenderer : Renderer { }
    public sealed class MeshFilter : Component { public Mesh sharedMesh, mesh; }
    public sealed class Mesh : Object
    {
        public void Clear() { } public void SetVertices(System.Collections.Generic.List<Vector3> v) { } public void SetColors(System.Collections.Generic.List<Color> c) { } public void SetTriangles(System.Collections.Generic.List<int> t, int sub) { }
        public Vector3[] vertices; public Vector2[] uv; public Color[] colors; public int[] triangles;
        public void MarkDynamic() { } public void RecalculateBounds() { }
    }
    public sealed class Sprite : Object
    {
        public static Sprite Create(Texture2D t, Rect r, Vector2 pivot, float ppu) => null;
        public Texture2D texture => null;
    }
    public class Texture : Object { public TextureWrapMode wrapMode; public FilterMode filterMode; public int width => 0; public int height => 0; }
    public enum TextureFormat { RGBA32, ARGB32 }
    public enum TextureWrapMode { Repeat, Clamp }
    public enum FilterMode { Point, Bilinear }
    public sealed class Texture2D : Texture
    {
        public readonly int W, H; public Color32[] Pixels;
        public Texture2D(int w, int h, TextureFormat f, bool mip) { W = w; H = h; }
        public void SetPixels32(Color32[] c) { Pixels = (Color32[])c.Clone(); } public void Apply(bool mips, bool nonReadable) { } public void Apply() { }
    }
    public sealed class Shader : Object { public static Shader Find(string n) => null; }
    public class Material : Object
    {
        public Material(Shader s) { } public Material(Material m) { }
        public Texture mainTexture; public Color color;
    }
    public sealed class Font : Object { public static Font CreateDynamicFontFromOSFont(string n, int size) => null; }
    public static class Resources { public static T GetBuiltinResource<T>(string path) where T : Object => null; }
    public enum CameraClearFlags { Skybox, SolidColor, Depth, Nothing }
    public sealed class Camera : Behaviour
    {
        public static Camera main => null;
        public bool orthographic; public float orthographicSize, nearClipPlane, farClipPlane, aspect; public CameraClearFlags clearFlags; public Color backgroundColor;
        public Vector3 WorldToScreenPoint(Vector3 p) => p;
    }
    public sealed class AudioClip : Object
    {
        public static AudioClip Create(string name, int samples, int channels, int freq, bool stream) => null;
        public bool SetData(float[] d, int offset) => true;
    }
    public sealed class AudioSource : Behaviour
    {
        public AudioClip clip; public float volume, pitch, panStereo, spatialBlend, time; public bool loop, playOnAwake, isPlaying;
        public void Play() { } public void Stop() { }
    }
    public class AnimationCurve { public static AnimationCurve Linear(float a, float b, float c, float d) => null; }
    public struct GradientColorKey { public GradientColorKey(Color c, float t) { } }
    public struct GradientAlphaKey { public GradientAlphaKey(float a, float t) { } }
    public class Gradient { public void SetKeys(GradientColorKey[] c, GradientAlphaKey[] a) { } }
    public enum ParticleSystemRenderMode { Billboard, Stretch, HorizontalBillboard, VerticalBillboard, Mesh, None }
    public enum ParticleSystemSimulationSpace { Local, World, Custom }
    public enum ParticleSystemStopBehavior { StopEmittingAndClear, StopEmitting }
    public enum ParticleSystemShapeType { Sphere, Box, Circle }
    public sealed class ParticleSystemRenderer : Renderer { public ParticleSystemRenderMode renderMode; public float velocityScale, lengthScale; }
    public sealed class ParticleSystem : Component
    {
        public struct MinMaxCurve
        {
            public MinMaxCurve(float c) { } public MinMaxCurve(float a, float b) { } public MinMaxCurve(float m, AnimationCurve c) { }
            public static implicit operator MinMaxCurve(float c) => default;
        }
        public struct MinMaxGradient
        {
            public MinMaxGradient(Gradient g) { } public MinMaxGradient(Color c) { }
            public static implicit operator MinMaxGradient(Color c) => default;
        }
        public struct MainModule
        {
            public bool loop, playOnAwake; public float duration; public ParticleSystemSimulationSpace simulationSpace;
            public int maxParticles; public MinMaxCurve startSpeed, startLifetime, startSize; public MinMaxGradient startColor;
            public MinMaxCurve gravityModifier; public float simulationSpeed;
        }
        public struct EmissionModule { public bool enabled; public MinMaxCurve rateOverTime; }
        public struct ShapeModule { public bool enabled; public ParticleSystemShapeType shapeType; public Vector3 scale; }
        public struct ColorOverLifetimeModule { public bool enabled; public MinMaxGradient color; }
        public struct SizeOverLifetimeModule { public bool enabled; public MinMaxCurve size; }
        public struct LimitVelocityOverLifetimeModule { public bool enabled; public MinMaxCurve drag, limit; }
        public struct VelocityOverLifetimeModule { public bool enabled; public ParticleSystemSimulationSpace space; public MinMaxCurve x, y, z; }
        public struct NoiseModule { public bool enabled; public MinMaxCurve strength; public float frequency; }
        public struct EmitParams { public Vector3 position, velocity; public Color32 startColor; public float startSize, startLifetime; public bool applyShapeToPosition; }
        public MainModule main => default; public EmissionModule emission => default; public ShapeModule shape => default;
        public ColorOverLifetimeModule colorOverLifetime => default; public SizeOverLifetimeModule sizeOverLifetime => default;
        public LimitVelocityOverLifetimeModule limitVelocityOverLifetime => default; public VelocityOverLifetimeModule velocityOverLifetime => default;
        public NoiseModule noise => default;
        public void Emit(EmitParams p, int count) { } public void Play() { } public void Stop(bool children, ParticleSystemStopBehavior b) { } public void Clear() { }
    }
    public enum KeyCode
    {
        None = 0, Backspace = 8, Tab = 9, Return = 13, Escape = 27, Space = 32, Quote = 39, Comma = 44, Minus = 45, Period = 46, Slash = 47,
        Alpha0 = 48, Alpha1, Alpha2, Alpha3, Alpha4, Alpha5, Alpha6, Alpha7, Alpha8, Alpha9, Semicolon = 59, LeftBracket = 91, RightBracket = 93,
        A = 97, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        Keypad0 = 256, Keypad1, Keypad2, Keypad3, Keypad4, Keypad5, Keypad6, Keypad7, Keypad8, Keypad9, KeypadPeriod, KeypadDivide, KeypadMultiply, KeypadMinus, KeypadPlus, KeypadEnter,
        UpArrow = 273, DownArrow, RightArrow, LeftArrow, F1 = 282, RightShift = 303, LeftShift, RightControl, LeftControl,
        Joystick1Button0 = 350
    }
    public static class Input
    {
        public static bool GetKey(KeyCode k) => false; public static bool GetKeyDown(KeyCode k) => false; public static bool GetMouseButton(int b) => false; public static string[] GetJoystickNames() => null;
    }
    public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad }
    [AttributeUsage(AttributeTargets.Method)] public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) { } }
    [AttributeUsage(AttributeTargets.Class)] public sealed class DefaultExecutionOrderAttribute : Attribute { public DefaultExecutionOrderAttribute(int o) { } }
    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }
    public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }
    public enum HorizontalWrapMode { Wrap, Overflow }
    public enum VerticalWrapMode { Truncate, Overflow }
    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }
    public sealed class Canvas : Behaviour { public RenderMode renderMode; public int sortingOrder; }
}
namespace UnityEngine.Rendering { public enum ShadowCastingMode { Off, On } }

namespace UnityEngine.EventSystems
{
    public class UIBehaviour : MonoBehaviour { }
    public class BaseEventData { public void Use() { } }
    public class PointerEventData : BaseEventData { }
    public enum MoveDirection { Left, Up, Right, Down, None }
    public class AxisEventData : BaseEventData { public MoveDirection moveDir; }
    public interface IEventSystemHandler { }
    public interface ISelectHandler : IEventSystemHandler { void OnSelect(BaseEventData e); }
    public interface IDeselectHandler : IEventSystemHandler { void OnDeselect(BaseEventData e); }
    public interface IPointerEnterHandler : IEventSystemHandler { void OnPointerEnter(PointerEventData e); }
    public interface IPointerExitHandler : IEventSystemHandler { void OnPointerExit(PointerEventData e); }
    public interface IMoveHandler : IEventSystemHandler { void OnMove(AxisEventData e); }
    public class EventSystem : UIBehaviour
    {
        public static EventSystem current;
        public GameObject currentSelectedGameObject => null;
        public void SetSelectedGameObject(GameObject g) { }
    }
    public class BaseInputModule : UIBehaviour { }
    public class StandaloneInputModule : BaseInputModule { }
}

namespace UnityEngine.UI
{
    using UnityEngine.Events;
    public class CanvasScaler : UnityEngine.EventSystems.UIBehaviour
    {
        public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize }
        public ScaleMode uiScaleMode; public Vector2 referenceResolution; public float matchWidthOrHeight;
    }
    public class GraphicRaycaster : UnityEngine.EventSystems.UIBehaviour { }
    public abstract class Graphic : UnityEngine.EventSystems.UIBehaviour
    {
        public Color color; public bool raycastTarget; public RectTransform rectTransform => null;
    }
    public abstract class MaskableGraphic : Graphic { }
    public class Image : MaskableGraphic
    {
        public enum Type { Simple, Sliced, Tiled, Filled }
        public enum FillMethod { Horizontal, Vertical, Radial90, Radial180, Radial360 }
        public Sprite sprite; public Type type; public FillMethod fillMethod; public int fillOrigin; public float fillAmount; public bool preserveAspect;
    }
    public class Text : MaskableGraphic
    {
        public Font font; public string text; public int fontSize; public TextAnchor alignment; public FontStyle fontStyle;
        public HorizontalWrapMode horizontalOverflow; public VerticalWrapMode verticalOverflow; public bool supportRichText; public float lineSpacing;
    }
    public abstract class BaseMeshEffect : UnityEngine.EventSystems.UIBehaviour { }
    public class Shadow : BaseMeshEffect { public Color effectColor; public Vector2 effectDistance; }
    public class Outline : Shadow { }
    public struct ColorBlock { public Color normalColor, highlightedColor, pressedColor, selectedColor, disabledColor; public float fadeDuration; }
    public struct Navigation
    {
        public enum Mode { None, Horizontal, Vertical, Automatic, Explicit }
        public Mode mode; public Selectable selectOnUp, selectOnDown, selectOnLeft, selectOnRight;
    }
    public class Selectable : UnityEngine.EventSystems.UIBehaviour, UnityEngine.EventSystems.IMoveHandler, UnityEngine.EventSystems.ISelectHandler
    {
        public Graphic targetGraphic; public ColorBlock colors; public Navigation navigation; public bool interactable;
        public virtual void OnMove(UnityEngine.EventSystems.AxisEventData e) { } public virtual void OnSelect(UnityEngine.EventSystems.BaseEventData e) { }
    }
    public class Button : Selectable
    {
        public class ButtonClickedEvent : UnityEvent { }
        public ButtonClickedEvent onClick = new ButtonClickedEvent();
    }
    public class InputField : Selectable
    {
        public class OnChangeEvent : UnityEvent<string> { }
        public class SubmitEvent : UnityEvent<string> { }
        public Text textComponent; public Graphic placeholder; public string text; public int characterLimit; public bool isFocused;
        public OnChangeEvent onValueChanged = new OnChangeEvent(); public SubmitEvent onEndEdit = new SubmitEvent();
    }
}
namespace UnityEngine.Events
{
    public delegate void UnityAction(); public delegate void UnityAction<T>(T a);
    public class UnityEvent { public void AddListener(UnityAction a) { } }
    public class UnityEvent<T> { public void AddListener(UnityAction<T> a) { } }
}
