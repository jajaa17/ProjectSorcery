using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ProjectSorcery
{
    /// <summary>A physical input device owned by one local player (keyboard half or gamepad).</summary>
    public abstract class LocalDevice
    {
        public abstract string Label { get; }
        public abstract bool Connected { get; }

        public ushort Held;
        ushort lastHeld, latch;

        // Menu edges for this frame
        public bool MUp, MDown, MLeft, MRight, MConfirm, MBack, MStart;
        bool pUp, pDown, pLeft, pRight, pConfirm, pBack, pStart;
        float repeatTimer; int repeatDir;

        public void Sample(float dt)
        {
            ushort h = Connected ? ReadHeld() : (ushort)0;
            latch |= (ushort)(h & ~lastHeld);
            lastHeld = h;
            Held = h;

            ReadMenu(out bool up, out bool down, out bool left, out bool right, out bool confirm, out bool back, out bool start);
            if (!Connected) up = down = left = right = confirm = back = start = false;
            MUp = up && !pUp; MDown = down && !pDown; MLeft = left && !pLeft; MRight = right && !pRight;
            MConfirm = confirm && !pConfirm; MBack = back && !pBack; MStart = start && !pStart;

            // auto-repeat directions when held (menu scrolling)
            int dir = up ? 1 : down ? 2 : left ? 3 : right ? 4 : 0;
            if (dir != 0 && dir == repeatDir)
            {
                repeatTimer -= dt;
                if (repeatTimer <= 0f)
                {
                    repeatTimer = 0.09f;
                    if (dir == 1) MUp = true; else if (dir == 2) MDown = true; else if (dir == 3) MLeft = true; else MRight = true;
                }
            }
            else { repeatDir = dir; repeatTimer = 0.38f; }

            pUp = up; pDown = down; pLeft = left; pRight = right; pConfirm = confirm; pBack = back; pStart = start;
        }

        /// <summary>Input bits for the next simulation tick: held keys plus any tap since the last tick.</summary>
        public ushort TakeTick()
        {
            ushort r = (ushort)(Held | latch);
            latch = 0;
            return r;
        }

        public void ClearLatch() { latch = 0; }

        protected abstract ushort ReadHeld();
        protected abstract void ReadMenu(out bool up, out bool down, out bool left, out bool right, out bool confirm, out bool back, out bool start);
    }

    /// <summary>Keyboard P1 + Gamepad 1 combined (online play, single-player menus).</summary>
    public sealed class MergedDevice : LocalDevice
    {
        public override string Label => "Keyboard / Gamepad";
        public override bool Connected => true;
        protected override ushort ReadHeld()
        {
            var d = InputHub.Devices;
            return (ushort)(d[0].Held | (d.Count > 2 ? d[2].Held : 0));
        }
        protected override void ReadMenu(out bool up, out bool down, out bool left, out bool right, out bool confirm, out bool back, out bool start)
        {
            var d = InputHub.Devices;
            var a = d[0]; var b = d[2];
            up = a.MUp || b.MUp; down = a.MDown || b.MDown; left = a.MLeft || b.MLeft; right = a.MRight || b.MRight;
            confirm = a.MConfirm || b.MConfirm; back = a.MBack || b.MBack; start = a.MStart || b.MStart;
        }
    }

    public sealed class KeyboardScheme
    {
        public KeyCode Left, Right, Jump, Down, Light, Heavy, Block, Dash, S1, S2, S3, Ult, Confirm, Back;
        public KeyCode[] Alt; // same order as primary bits: Left,Right,Jump,Down,Light,Heavy,Block,Dash,S1,S2,S3,Ult
        public string Description;
    }

    public sealed class KeyboardDevice : LocalDevice
    {
        public readonly KeyboardScheme Scheme;
        public readonly string Name;
        public bool UseAlt = true;
        public KeyboardDevice(string name, KeyboardScheme s) { Name = name; Scheme = s; }
        public override string Label => Name;
        public override bool Connected => KeyInput.KeyboardPresent;

        protected override ushort ReadHeld()
        {
            var s = Scheme;
            int b = 0;
            if (K(s.Left, 0)) b |= IB.Left;
            if (K(s.Right, 1)) b |= IB.Right;
            if (K(s.Jump, 2)) b |= IB.Jump;
            if (K(s.Down, 3)) b |= IB.Down;
            if (K(s.Light, 4)) b |= IB.Light;
            if (K(s.Heavy, 5)) b |= IB.Heavy;
            if (K(s.Block, 6)) b |= IB.Block;
            if (K(s.Dash, 7)) b |= IB.Dash;
            if (K(s.S1, 8)) b |= IB.S1;
            if (K(s.S2, 9)) b |= IB.S2;
            if (K(s.S3, 10)) b |= IB.S3;
            if (K(s.Ult, 11)) b |= IB.Ult;
            return (ushort)b;
        }

        bool K(KeyCode primary, int altIndex)
        {
            if (KeyInput.Held(primary)) return true;
            if (UseAlt && Scheme.Alt != null && altIndex < Scheme.Alt.Length && Scheme.Alt[altIndex] != KeyCode.None)
                return KeyInput.Held(Scheme.Alt[altIndex]);
            return false;
        }

        protected override void ReadMenu(out bool up, out bool down, out bool left, out bool right, out bool confirm, out bool back, out bool start)
        {
            var s = Scheme;
            up = K(s.Jump, 2);
            down = K(s.Down, 3);
            left = K(s.Left, 0);
            right = K(s.Right, 1);
            confirm = KeyInput.Held(s.Confirm) || K(s.Light, 4);
            back = KeyInput.Held(s.Back) || K(s.Heavy, 5);
            start = KeyInput.Held(KeyCode.Escape);
        }
    }

    public sealed class GamepadDevice : LocalDevice
    {
        public readonly int Index;
        public GamepadDevice(int index) { Index = index; }
        public override string Label => "Gamepad " + (Index + 1);

#if ENABLE_INPUT_SYSTEM
        Gamepad Pad => Index < Gamepad.all.Count ? Gamepad.all[Index] : null;
        public override bool Connected => Pad != null;

        protected override ushort ReadHeld()
        {
            var p = Pad; if (p == null) return 0;
            Vector2 st = p.leftStick.ReadValue();
            Vector2 dp = p.dpad.ReadValue();
            float x = Mathf.Abs(dp.x) > 0.5f ? dp.x : st.x;
            float y = Mathf.Abs(dp.y) > 0.5f ? dp.y : st.y;
            int b = 0;
            if (x < -0.4f) b |= IB.Left;
            if (x > 0.4f) b |= IB.Right;
            if (y < -0.55f) b |= IB.Down;
            if (p.buttonSouth.isPressed || y > 0.75f && Mathf.Abs(dp.y) > 0.5f) b |= IB.Jump;
            if (p.buttonWest.isPressed) b |= IB.Light;
            if (p.buttonNorth.isPressed) b |= IB.Heavy;
            if (p.rightShoulder.isPressed) b |= IB.Block;
            if (p.buttonEast.isPressed || p.leftStickButton.isPressed) b |= IB.Dash;
            if (p.leftShoulder.isPressed) b |= IB.S1;
            if (p.leftTrigger.isPressed) b |= IB.S2;
            if (p.rightStickButton.isPressed) b |= IB.S3;
            if (p.rightTrigger.isPressed) b |= IB.Ult;
            // right stick flicks as extra special shortcuts (left = S3, up = S3, down = S2)
            Vector2 rs = p.rightStick.ReadValue();
            if (rs.magnitude > 0.7f)
            {
                if (rs.y > 0.5f) b |= IB.S3;
                else if (rs.y < -0.5f) b |= IB.S2;
                else b |= IB.S1;
            }
            return (ushort)b;
        }

        protected override void ReadMenu(out bool up, out bool down, out bool left, out bool right, out bool confirm, out bool back, out bool start)
        {
            var p = Pad;
            if (p == null) { up = down = left = right = confirm = back = start = false; return; }
            Vector2 st = p.leftStick.ReadValue();
            Vector2 dp = p.dpad.ReadValue();
            up = dp.y > 0.5f || st.y > 0.6f;
            down = dp.y < -0.5f || st.y < -0.6f;
            left = dp.x < -0.5f || st.x < -0.6f;
            right = dp.x > 0.5f || st.x > 0.6f;
            confirm = p.buttonSouth.isPressed;
            back = p.buttonEast.isPressed;
            start = p.startButton.isPressed;
        }
#else
        // Legacy Input Manager: gamepad buttons only through joystick KeyCodes (no analog axes configured).
        public override bool Connected => Input.GetJoystickNames().Length > Index && !string.IsNullOrEmpty(Input.GetJoystickNames()[Index]);
        KeyCode B(int n) => (KeyCode)((int)KeyCode.Joystick1Button0 + Index * 20 + n);
        protected override ushort ReadHeld()
        {
            int b = 0;
            if (Input.GetKey(B(0))) b |= IB.Jump;
            if (Input.GetKey(B(2))) b |= IB.Light;
            if (Input.GetKey(B(3))) b |= IB.Heavy;
            if (Input.GetKey(B(1))) b |= IB.Dash;
            if (Input.GetKey(B(4))) b |= IB.S1;
            if (Input.GetKey(B(5))) b |= IB.Block;
            if (Input.GetKey(B(8))) b |= IB.S3;
            if (Input.GetKey(B(9))) b |= IB.Ult;
            return (ushort)b;
        }
        protected override void ReadMenu(out bool up, out bool down, out bool left, out bool right, out bool confirm, out bool back, out bool start)
        {
            up = down = left = right = false;
            confirm = Input.GetKey(B(0)); back = Input.GetKey(B(1)); start = Input.GetKey(B(7));
        }
#endif
    }

    /// <summary>Keyboard abstraction that works with both the new Input System and the legacy Input Manager.</summary>
    public static class KeyInput
    {
#if ENABLE_INPUT_SYSTEM
        public static bool KeyboardPresent => Keyboard.current != null;
        public static bool Held(KeyCode k)
        {
            var kb = Keyboard.current;
            if (kb == null || k == KeyCode.None) return false;
            var key = Map(k);
            return key != Key.None && kb[key].isPressed;
        }
        public static bool Down(KeyCode k)
        {
            var kb = Keyboard.current;
            if (kb == null || k == KeyCode.None) return false;
            var key = Map(k);
            return key != Key.None && kb[key].wasPressedThisFrame;
        }

        static Key Map(KeyCode k)
        {
            if (k >= KeyCode.A && k <= KeyCode.Z) return Key.A + (k - KeyCode.A);
            if (k >= KeyCode.Alpha0 && k <= KeyCode.Alpha9) return k == KeyCode.Alpha0 ? Key.Digit0 : Key.Digit1 + (k - KeyCode.Alpha1);
            if (k >= KeyCode.Keypad0 && k <= KeyCode.Keypad9) return Key.Numpad0 + (k - KeyCode.Keypad0);
            switch (k)
            {
                case KeyCode.LeftArrow: return Key.LeftArrow;
                case KeyCode.RightArrow: return Key.RightArrow;
                case KeyCode.UpArrow: return Key.UpArrow;
                case KeyCode.DownArrow: return Key.DownArrow;
                case KeyCode.Space: return Key.Space;
                case KeyCode.Return: return Key.Enter;
                case KeyCode.KeypadEnter: return Key.NumpadEnter;
                case KeyCode.KeypadPlus: return Key.NumpadPlus;
                case KeyCode.KeypadMinus: return Key.NumpadMinus;
                case KeyCode.KeypadPeriod: return Key.NumpadPeriod;
                case KeyCode.Escape: return Key.Escape;
                case KeyCode.LeftShift: return Key.LeftShift;
                case KeyCode.RightShift: return Key.RightShift;
                case KeyCode.LeftControl: return Key.LeftCtrl;
                case KeyCode.RightControl: return Key.RightCtrl;
                case KeyCode.Comma: return Key.Comma;
                case KeyCode.Period: return Key.Period;
                case KeyCode.Slash: return Key.Slash;
                case KeyCode.Semicolon: return Key.Semicolon;
                case KeyCode.Quote: return Key.Quote;
                case KeyCode.LeftBracket: return Key.LeftBracket;
                case KeyCode.RightBracket: return Key.RightBracket;
                case KeyCode.Backspace: return Key.Backspace;
                case KeyCode.Tab: return Key.Tab;
                case KeyCode.F1: return Key.F1;
                default: return Key.None;
            }
        }
#else
        public static bool KeyboardPresent => true;
        public static bool Held(KeyCode k) => k != KeyCode.None && Input.GetKey(k);
        public static bool Down(KeyCode k) => k != KeyCode.None && Input.GetKeyDown(k);
#endif
    }

    /// <summary>All local devices. Device ids: 0 = Keyboard P1, 1 = Keyboard P2, 2..5 = Gamepad 1..4, 6 = merged KB1+Pad1.</summary>
    public static class InputHub
    {
        public static readonly List<LocalDevice> Devices = new List<LocalDevice>();
        public const int MaxGamepads = 4;

        public static readonly KeyboardScheme P1 = new KeyboardScheme
        {
            Left = KeyCode.A, Right = KeyCode.D, Jump = KeyCode.W, Down = KeyCode.S,
            Light = KeyCode.F, Heavy = KeyCode.G, Block = KeyCode.H, Dash = KeyCode.LeftShift,
            S1 = KeyCode.R, S2 = KeyCode.T, S3 = KeyCode.Y, Ult = KeyCode.V,
            Confirm = KeyCode.Return, Back = KeyCode.Escape,
            // Solo-friendly alternates (disabled automatically when a 2nd keyboard player joins)
            Alt = new[] { KeyCode.None, KeyCode.None, KeyCode.Space, KeyCode.None,
                          KeyCode.J, KeyCode.K, KeyCode.L, KeyCode.None,
                          KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P },
            Description = "Move WASD  |  Light F/J  Heavy G/K  Block H/L  Dash L-Shift  |  Skills R T Y (U I O)  Ultimate V/P"
        };

        public static readonly KeyboardScheme P2 = new KeyboardScheme
        {
            Left = KeyCode.LeftArrow, Right = KeyCode.RightArrow, Jump = KeyCode.UpArrow, Down = KeyCode.DownArrow,
            Light = KeyCode.Keypad1, Heavy = KeyCode.Keypad2, Block = KeyCode.Keypad3, Dash = KeyCode.Keypad0,
            S1 = KeyCode.Keypad4, S2 = KeyCode.Keypad5, S3 = KeyCode.Keypad6, Ult = KeyCode.KeypadPlus,
            Confirm = KeyCode.KeypadEnter, Back = KeyCode.Backspace,
            Alt = new[] { KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.None,
                          KeyCode.Comma, KeyCode.Period, KeyCode.Slash, KeyCode.RightShift,
                          KeyCode.K, KeyCode.L, KeyCode.Semicolon, KeyCode.Quote },
            Description = "Move Arrows  |  Light Num1/,  Heavy Num2/.  Block Num3//  Dash Num0/R-Shift  |  Skills Num4-6 (K L ;)  Ultimate Num+/'"
        };

        public const string GamepadDescription = "Move L-Stick/D-Pad  |  Jump A  Light X  Heavy Y  Dash B  Block RB  |  Skills LB, LT, R3 (or R-Stick)  Ultimate RT";

        static bool ready;

        public static void Init()
        {
            if (ready) return;
            ready = true;
            Devices.Add(new KeyboardDevice("Keyboard P1", P1));
            Devices.Add(new KeyboardDevice("Keyboard P2", P2));
            for (int i = 0; i < MaxGamepads; i++) Devices.Add(new GamepadDevice(i));
            Devices.Add(new MergedDevice()); // id 6 (sampled last, reads the others)
        }

        public const int Merged = 6;

        public static LocalDevice Get(int id) => id >= 0 && id < Devices.Count ? Devices[id] : null;

        public static string DeviceName(int id)
        {
            var d = Get(id);
            return d == null ? "None" : d.Label;
        }

        /// <summary>When P2 is on the same keyboard, P1's alternate keys would collide; turn them off.</summary>
        public static void SetSharedKeyboard(bool shared)
        {
            if (Devices.Count > 0) ((KeyboardDevice)Devices[0]).UseAlt = !shared;
        }

        public static void Update(float dt)
        {
            for (int i = 0; i < Devices.Count; i++) Devices[i].Sample(dt);
        }

        // Aggregate menu input from every device (used by screens that any player can drive).
        public static bool AnyUp { get { foreach (var d in Devices) if (d.MUp) return true; return false; } }
        public static bool AnyDown { get { foreach (var d in Devices) if (d.MDown) return true; return false; } }
        public static bool AnyLeft { get { foreach (var d in Devices) if (d.MLeft) return true; return false; } }
        public static bool AnyRight { get { foreach (var d in Devices) if (d.MRight) return true; return false; } }
        public static bool AnyConfirm { get { foreach (var d in Devices) if (d.MConfirm) return true; return false; } }
        public static bool AnyBack { get { foreach (var d in Devices) if (d.MBack) return true; return false; } }
        public static bool AnyStart { get { foreach (var d in Devices) if (d.MStart) return true; return false; } }

        /// <summary>Returns the first device id with a confirm press this frame, or -1.</summary>
        public static int FirstConfirm()
        {
            for (int i = 0; i < Devices.Count; i++) if (Devices[i].MConfirm) return i;
            return -1;
        }
    }
}
