using System.Collections.Generic;
using UnityEngine;

namespace GokCephesi
{
    public struct PointerInfo
    {
        public int id;
        public Vector2 pos;   // ekran pikseli, sol-alt köşe (0,0)
    }

    public enum GameKey { Up, Down, Left, Right, Fire, Missile, Boost, Brake, Target, Menu }

    /// <summary>
    /// Dokunmatik ekran, fare ve klavyeyi tek yerden okur.
    /// Projede eski (Input Manager) veya yeni (Input System) giriş sistemi seçili olsa da çalışır.
    /// </summary>
    public static class InputBridge
    {
        static readonly List<PointerInfo> list = new List<PointerInfo>();

        public static List<PointerInfo> Pointers()
        {
            list.Clear();
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) continue;
                    list.Add(new PointerInfo { id = t.fingerId, pos = t.position });
                }
            }
            else if (Input.GetMouseButton(0))
            {
                list.Add(new PointerInfo { id = -1, pos = new Vector2(Input.mousePosition.x, Input.mousePosition.y) });
            }
#elif ENABLE_INPUT_SYSTEM
            bool any = false;
            var ts = UnityEngine.InputSystem.Touchscreen.current;
            if (ts != null)
            {
                var touches = ts.touches;
                for (int i = 0; i < touches.Count; i++)
                {
                    var t = touches[i];
                    if (!t.press.isPressed) continue;
                    any = true;
                    list.Add(new PointerInfo { id = t.touchId.ReadValue(), pos = t.position.ReadValue() });
                }
            }
            var m = UnityEngine.InputSystem.Mouse.current;
            if (!any && m != null && m.leftButton.isPressed)
                list.Add(new PointerInfo { id = -1, pos = m.position.ReadValue() });
#endif
            return list;
        }

        public static bool Key(GameKey k)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            switch (k)
            {
                case GameKey.Up: return Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
                case GameKey.Down: return Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
                case GameKey.Left: return Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
                case GameKey.Right: return Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
                case GameKey.Fire: return Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.J);
                case GameKey.Missile: return Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.K);
                case GameKey.Boost: return Input.GetKey(KeyCode.LeftShift);
                case GameKey.Brake: return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.Q);
                case GameKey.Target: return Input.GetKey(KeyCode.Tab) || Input.GetKey(KeyCode.T);
                case GameKey.Menu: return Input.GetKey(KeyCode.Escape);
            }
#elif ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                switch (k)
                {
                    case GameKey.Up: return kb.wKey.isPressed || kb.upArrowKey.isPressed;
                    case GameKey.Down: return kb.sKey.isPressed || kb.downArrowKey.isPressed;
                    case GameKey.Left: return kb.aKey.isPressed || kb.leftArrowKey.isPressed;
                    case GameKey.Right: return kb.dKey.isPressed || kb.rightArrowKey.isPressed;
                    case GameKey.Fire: return kb.spaceKey.isPressed || kb.jKey.isPressed;
                    case GameKey.Missile: return kb.eKey.isPressed || kb.kKey.isPressed;
                    case GameKey.Boost: return kb.leftShiftKey.isPressed;
                    case GameKey.Brake: return kb.leftCtrlKey.isPressed || kb.qKey.isPressed;
                    case GameKey.Target: return kb.tabKey.isPressed || kb.tKey.isPressed;
                    case GameKey.Menu: return kb.escapeKey.isPressed;
                }
            }
#endif
            return false;
        }
    }
}
