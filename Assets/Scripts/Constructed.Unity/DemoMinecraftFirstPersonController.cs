using System;
using UnityEngine;

namespace Constructed.Unity
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class DemoMinecraftFirstPersonController : MonoBehaviour
    {
        public const string PlayerRootName = "Demo Player";
        public const float StandingWidth = 0.6f;
        public const float StandingHeight = 1.8f;
        public const float StandingEyeHeight = 1.62f;
        public const float DefaultStepOffset = 0.6f;
        public const float DefaultWalkSpeed = 4.317f;
        public const float DefaultGravity = 30f;
        public const float DefaultJumpHeight = 1.252f;
        public const float DefaultFieldOfView = 70f;

        [SerializeField]
        private Camera playerCamera;

        [SerializeField]
        private float walkSpeed = DefaultWalkSpeed;

        [SerializeField]
        private float gravity = DefaultGravity;

        [SerializeField]
        private float jumpHeight = DefaultJumpHeight;

        [SerializeField]
        private float lookSensitivity = 0.14f;

        private CharacterController characterController;
        private float yawDegrees;
        private float pitchDegrees;
        private float verticalVelocity;
        private bool hasLookState;

        public bool InputEnabled { get; set; } = true;

        public Camera PlayerCamera
        {
            get { return playerCamera; }
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            ApplyMinecraftBodyShape();
        }

        private void OnEnable()
        {
            if (characterController == null)
                characterController = GetComponent<CharacterController>();

            ApplyMinecraftBodyShape();
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

        public void SetPlayerCamera(Camera camera)
        {
            if (camera == null)
                return;

            bool sameCamera = ReferenceEquals(playerCamera, camera);
            bool alreadyAttached = camera.transform.parent == transform;
            playerCamera = camera;
            playerCamera.transform.SetParent(transform, false);
            playerCamera.transform.localPosition = new Vector3(0f, StandingEyeHeight, 0f);

            if (ShouldPreserveAttachedCamera(Application.isPlaying, sameCamera, alreadyAttached))
            {
                ApplyLookRotation();
                return;
            }

            playerCamera.transform.localRotation = Quaternion.identity;
            SyncLookStateFromTransform();
        }

        public void RefreshRigConfiguration()
        {
            if (characterController == null)
                characterController = GetComponent<CharacterController>();

            ApplyMinecraftBodyShape();
            ApplyLookRotation();
        }

        public void SetViewAngles(float yaw, float pitch)
        {
            yawDegrees = yaw;
            pitchDegrees = Mathf.Clamp(pitch, -89f, 89f);
            hasLookState = true;
            ApplyLookRotation();
        }

        public void CaptureCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (!Application.isPlaying)
                return;

            if (characterController == null)
                characterController = GetComponent<CharacterController>();
            if (!hasLookState)
                SyncLookStateFromTransform();

            if (!InputEnabled)
            {
                UnlockCursor();
                return;
            }

            if (!EnsureCursorCaptured())
                return;

            UpdateLook();
            UpdateMovement();
        }

        private bool EnsureCursorCaptured()
        {
            if (DemoInputSystemAdapter.WasKeyPressedThisFrame("escapeKey"))
            {
                UnlockCursor();
                return false;
            }

            if (Cursor.lockState == CursorLockMode.Locked)
                return true;

            if (DemoInputSystemAdapter.WasMouseButtonPressedThisFrame(DemoMouseButton.Left) ||
                DemoInputSystemAdapter.WasMouseButtonPressedThisFrame(DemoMouseButton.Right) ||
                DemoInputSystemAdapter.WasMouseButtonPressedThisFrame(DemoMouseButton.Middle))
            {
                CaptureCursor();
                return true;
            }

            return false;
        }

        private void UpdateLook()
        {
            Vector2 mouseDelta = DemoInputSystemAdapter.ReadMouseDelta();
            yawDegrees += mouseDelta.x * lookSensitivity;
            pitchDegrees -= mouseDelta.y * lookSensitivity;
            pitchDegrees = Mathf.Clamp(pitchDegrees, -89f, 89f);
            ApplyLookRotation();
        }

        private void UpdateMovement()
        {
            Vector3 movementInput = Vector3.zero;
            if (DemoInputSystemAdapter.IsKeyPressed("wKey"))
                movementInput += Vector3.forward;
            if (DemoInputSystemAdapter.IsKeyPressed("sKey"))
                movementInput += Vector3.back;
            if (DemoInputSystemAdapter.IsKeyPressed("dKey"))
                movementInput += Vector3.right;
            if (DemoInputSystemAdapter.IsKeyPressed("aKey"))
                movementInput += Vector3.left;

            movementInput = Vector3.ClampMagnitude(movementInput, 1f);
            Vector3 movement = (transform.forward * movementInput.z) + (transform.right * movementInput.x);

            if (characterController.isGrounded)
            {
                if (verticalVelocity < 0f)
                    verticalVelocity = -2f;

                if (DemoInputSystemAdapter.WasKeyPressedThisFrame("spaceKey"))
                    verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
            }
            else
            {
                verticalVelocity -= gravity * Time.unscaledDeltaTime;
            }

            movement *= walkSpeed;
            movement.y = verticalVelocity;
            characterController.Move(movement * Time.unscaledDeltaTime);
        }

        private void ApplyLookRotation()
        {
            transform.rotation = Quaternion.Euler(0f, yawDegrees, 0f);
            if (playerCamera != null)
                playerCamera.transform.localRotation = Quaternion.Euler(pitchDegrees, 0f, 0f);
        }

        private void SyncLookStateFromTransform()
        {
            yawDegrees = NormalizeSignedAngle(transform.eulerAngles.y);
            pitchDegrees = playerCamera != null
                ? NormalizeSignedAngle(playerCamera.transform.localEulerAngles.x)
                : 0f;
            hasLookState = true;
        }

        private void ApplyMinecraftBodyShape()
        {
            if (characterController == null)
                return;

            characterController.radius = StandingWidth * 0.5f;
            characterController.height = StandingHeight;
            characterController.center = new Vector3(0f, StandingHeight * 0.5f, 0f);
            characterController.stepOffset = DefaultStepOffset;
            characterController.slopeLimit = 90f;
            characterController.minMoveDistance = 0f;
        }

        private static float NormalizeSignedAngle(float degrees)
        {
            return degrees > 180f ? degrees - 360f : degrees;
        }

        private static bool ShouldPreserveAttachedCamera(bool isPlaying, bool sameCamera, bool alreadyAttached)
        {
            return isPlaying && sameCamera && alreadyAttached;
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
