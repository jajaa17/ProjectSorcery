#pragma warning disable
using System.Collections.Generic;
namespace UnityEngine.InputSystem.Controls
{
    public class InputControl { }
    public class ButtonControl : InputControl { public bool isPressed => false; public bool wasPressedThisFrame => false; }
    public class KeyControl : ButtonControl { }
    public class Vector2Control : InputControl { public Vector2 ReadValue() => default; }
    public class StickControl : Vector2Control { }
    public class DpadControl : Vector2Control { }
}
namespace UnityEngine.InputSystem
{
    using UnityEngine.InputSystem.Controls;
    using UnityEngine.InputSystem.Utilities;
    public enum Key
    {
        None, Space, Enter, Tab, Backquote, Quote, Semicolon, Comma, Period, Slash, Backslash, LeftBracket, RightBracket, Minus, Equals,
        A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        Digit1, Digit2, Digit3, Digit4, Digit5, Digit6, Digit7, Digit8, Digit9, Digit0,
        LeftShift, RightShift, LeftAlt, RightAlt, LeftCtrl, RightCtrl, LeftMeta, RightMeta, ContextMenu, Escape,
        LeftArrow, RightArrow, UpArrow, DownArrow, Backspace, PageDown, PageUp, Home, End, Insert, Delete, CapsLock, NumLock, PrintScreen, ScrollLock, Pause,
        NumpadEnter, NumpadDivide, NumpadMultiply, NumpadPlus, NumpadMinus, NumpadPeriod, NumpadEquals,
        Numpad0, Numpad1, Numpad2, Numpad3, Numpad4, Numpad5, Numpad6, Numpad7, Numpad8, Numpad9,
        F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12
    }
    public class InputDevice { }
    public class Keyboard : InputDevice
    {
        public static Keyboard current => null;
        public KeyControl this[Key k] => null;
    }
    public class Gamepad : InputDevice
    {
        public static ReadOnlyArray<Gamepad> all => default;
        public StickControl leftStick, rightStick; public DpadControl dpad;
        public ButtonControl buttonSouth, buttonNorth, buttonEast, buttonWest, leftShoulder, rightShoulder, leftTrigger, rightTrigger,
                             startButton, selectButton, leftStickButton, rightStickButton;
    }
}
namespace UnityEngine.InputSystem.Utilities
{
    public struct ReadOnlyArray<T> { public int Count => 0; public T this[int i] => default; }
}
namespace UnityEngine.InputSystem.UI
{
    public class InputSystemUIInputModule : UnityEngine.EventSystems.BaseInputModule { public void AssignDefaultActions() { } }
}
