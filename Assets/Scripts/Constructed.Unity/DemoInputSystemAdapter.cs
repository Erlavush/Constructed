using System;
using System.Reflection;
using UnityEngine;

namespace Constructed.Unity
{
    internal enum DemoMouseButton
    {
        Left,
        Right,
        Middle
    }

    internal static class DemoInputSystemAdapter
    {
        public static bool IsKeyPressed(string controlPropertyName)
        {
            object keyboard = GetKeyboardDevice();
            return keyboard != null && GetButtonState(keyboard, controlPropertyName, "isPressed");
        }

        public static bool WasKeyPressedThisFrame(string controlPropertyName)
        {
            object keyboard = GetKeyboardDevice();
            return keyboard != null && GetButtonState(keyboard, controlPropertyName, "wasPressedThisFrame");
        }

        public static bool IsMouseButtonPressed(DemoMouseButton button)
        {
            object mouse = GetMouseDevice();
            return mouse != null && GetButtonState(mouse, GetMouseButtonPropertyName(button), "isPressed");
        }

        public static bool WasMouseButtonPressedThisFrame(DemoMouseButton button)
        {
            object mouse = GetMouseDevice();
            return mouse != null && GetButtonState(mouse, GetMouseButtonPropertyName(button), "wasPressedThisFrame");
        }

        public static Vector2 ReadMouseDelta()
        {
            object mouse = GetMouseDevice();
            return mouse == null ? Vector2.zero : ReadVector2Control(mouse, "delta");
        }

        public static Vector2 ReadMousePosition()
        {
            object mouse = GetMouseDevice();
            return mouse == null ? Vector2.zero : ReadVector2Control(mouse, "position");
        }

        public static Vector2 ReadMouseScrollDelta()
        {
            object mouse = GetMouseDevice();
            return mouse == null ? Vector2.zero : ReadVector2Control(mouse, "scroll");
        }

        private static object GetKeyboardDevice()
        {
            Type keyboardType = Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
            return keyboardType?.GetProperty("current", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        }

        private static object GetMouseDevice()
        {
            Type mouseType = Type.GetType("UnityEngine.InputSystem.Mouse, Unity.InputSystem");
            return mouseType?.GetProperty("current", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        }

        private static string GetMouseButtonPropertyName(DemoMouseButton button)
        {
            switch (button)
            {
                case DemoMouseButton.Left:
                    return "leftButton";
                case DemoMouseButton.Right:
                    return "rightButton";
                case DemoMouseButton.Middle:
                    return "middleButton";
                default:
                    throw new ArgumentOutOfRangeException(nameof(button), button, null);
            }
        }

        private static bool GetButtonState(object device, string controlPropertyName, string statePropertyName)
        {
            object control = GetPropertyValue(device, controlPropertyName);
            if (control == null)
                return false;

            object value = control.GetType().GetProperty(statePropertyName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(control);
            return value is bool pressed && pressed;
        }

        private static Vector2 ReadVector2Control(object device, string controlPropertyName)
        {
            object control = GetPropertyValue(device, controlPropertyName);
            if (control == null)
                return Vector2.zero;

            object xControl = GetPropertyValue(control, "x");
            object yControl = GetPropertyValue(control, "y");
            return new Vector2(ReadFloatControl(xControl), ReadFloatControl(yControl));
        }

        private static float ReadFloatControl(object control)
        {
            if (control == null)
                return 0f;

            MethodInfo readValueMethod = control.GetType().GetMethod("ReadValue", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            object value = readValueMethod?.Invoke(control, null);
            return value is float floatValue ? floatValue : 0f;
        }

        private static object GetPropertyValue(object target, string propertyName)
        {
            return target?.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(target);
        }
    }
}
