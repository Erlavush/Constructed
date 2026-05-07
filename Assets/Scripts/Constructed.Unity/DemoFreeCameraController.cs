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

        public bool InputEnabled { get; set; } = true;

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

            if (!InputEnabled)
            {
                UnlockCursor();
                return;
            }

            UpdateCursorLockState();
            UpdateLook();
            UpdateMovement();
        }

        private void UpdateCursorLockState()
        {
            if (DemoInputSystemAdapter.WasKeyPressedThisFrame("escapeKey"))
            {
                UnlockCursor();
                return;
            }

            if (DemoInputSystemAdapter.IsMouseButtonPressed(DemoMouseButton.Middle))
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
            if (!DemoInputSystemAdapter.IsMouseButtonPressed(DemoMouseButton.Middle))
                return;

            Vector2 mouseDelta = DemoInputSystemAdapter.ReadMouseDelta();
            yawDegrees += mouseDelta.x * lookSensitivity;
            pitchDegrees -= mouseDelta.y * lookSensitivity;
            pitchDegrees = Mathf.Clamp(pitchDegrees, -89f, 89f);
            transform.rotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
        }

        private void UpdateMovement()
        {
            Vector3 movement = Vector3.zero;
            if (DemoInputSystemAdapter.IsKeyPressed("wKey"))
                movement += transform.forward;
            if (DemoInputSystemAdapter.IsKeyPressed("sKey"))
                movement -= transform.forward;
            if (DemoInputSystemAdapter.IsKeyPressed("dKey"))
                movement += transform.right;
            if (DemoInputSystemAdapter.IsKeyPressed("aKey"))
                movement -= transform.right;
            if (DemoInputSystemAdapter.IsKeyPressed("spaceKey"))
                movement += transform.up;
            if (DemoInputSystemAdapter.IsKeyPressed("qKey") ||
                DemoInputSystemAdapter.IsKeyPressed("leftCtrlKey") ||
                DemoInputSystemAdapter.IsKeyPressed("rightCtrlKey") ||
                DemoInputSystemAdapter.IsKeyPressed("cKey"))
                movement -= transform.up;

            if (movement.sqrMagnitude <= 0f)
                return;

            float speedMultiplier =
                DemoInputSystemAdapter.IsKeyPressed("leftShiftKey") ||
                DemoInputSystemAdapter.IsKeyPressed("rightShiftKey")
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

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
