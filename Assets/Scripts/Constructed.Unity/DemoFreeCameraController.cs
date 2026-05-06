using System;
using System.Reflection;
using UnityEngine;

namespace Constructed.Unity
{
    [DisallowMultipleComponent]
    public sealed class DemoFreeCameraController : MonoBehaviour
    {
        private const float DefaultMoveSpeed = 10f;
        private const float DefaultFastMoveMultiplier = 3f;
        private const float DefaultLookSensitivity = 0.12f;

        [SerializeField]
        private float moveSpeed = DefaultMoveSpeed;

        [SerializeField]
        private float fastMoveMultiplier = DefaultFastMoveMultiplier;

        [SerializeField]
        private float lookSensitivity = DefaultLookSensitivity;

        private float yawDegrees;
        private float pitchDegrees;
        private bool hasLookState;

        private void OnEnable()
        {
            SyncLookStateFromTransform();
        }

        private void OnDisable()
        {
            UnlockCursor();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                UnlockCursor();
        }

        private void Update()
        {
            if (!Application.isPlaying)
                return;

            if (!hasLookState)
                SyncLookStateFromTransform();

            UpdateCursorLockState();
            UpdateLook();
            UpdateMovement();
        }

        private void UpdateCursorLockState()
        {
            object keyboard = GetKeyboardDevice();
            object mouse = GetMouseDevice();
            if (keyboard == null || mouse == null)
                return;

            if (GetButtonState(keyboard, "escapeKey", "wasPressedThisFrame"))
            {
                UnlockCursor();
                return;
            }

            if (GetButtonState(mouse, "rightButton", "isPressed"))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else if (Cursor.lockState != CursorLockMode.None)
            {
                UnlockCursor();
            }
        }

        private void UpdateLook()
        {
            object mouse = GetMouseDevice();
            if (mouse == null || !GetButtonState(mouse, "rightButton", "isPressed"))
                return;

            Vector2 mouseDelta = ReadVector2Control(mouse, "delta");
            yawDegrees += mouseDelta.x * lookSensitivity;
            pitchDegrees -= mouseDelta.y * lookSensitivity;
            pitchDegrees = Mathf.Clamp(pitchDegrees, -89f, 89f);
            transform.rotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
        }

        private void UpdateMovement()
        {
            object keyboard = GetKeyboardDevice();
            if (keyboard == null)
                return;

            Vector3 movement = Vector3.zero;
            if (GetButtonState(keyboard, "wKey", "isPressed"))
                movement += transform.forward;
            if (GetButtonState(keyboard, "sKey", "isPressed"))
                movement -= transform.forward;
            if (GetButtonState(keyboard, "dKey", "isPressed"))
                movement += transform.right;
            if (GetButtonState(keyboard, "aKey", "isPressed"))
                movement -= transform.right;
            if (GetButtonState(keyboard, "eKey", "isPressed") || GetButtonState(keyboard, "spaceKey", "isPressed"))
                movement += transform.up;
            if (GetButtonState(keyboard, "qKey", "isPressed") ||
                GetButtonState(keyboard, "leftCtrlKey", "isPressed") ||
                GetButtonState(keyboard, "rightCtrlKey", "isPressed") ||
                GetButtonState(keyboard, "cKey", "isPressed"))
                movement -= transform.up;

            if (movement.sqrMagnitude <= 0f)
                return;

            float speedMultiplier =
                GetButtonState(keyboard, "leftShiftKey", "isPressed") ||
                GetButtonState(keyboard, "rightShiftKey", "isPressed")
                    ? fastMoveMultiplier
                    : 1f;
            transform.position += movement.normalized * (moveSpeed * speedMultiplier * Time.unscaledDeltaTime);
        }

        private void SyncLookStateFromTransform()
        {
            Vector3 eulerAngles = transform.rotation.eulerAngles;
            yawDegrees = eulerAngles.y;
            pitchDegrees = NormalizeSignedAngle(eulerAngles.x);
            hasLookState = true;
        }

        private static float NormalizeSignedAngle(float degrees)
        {
            return degrees > 180f ? degrees - 360f : degrees;
        }

        // Use reflection so this demo helper can talk to the installed Input System package
        // without adding a brittle asmdef/package reference edge to the current workspace.
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

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
