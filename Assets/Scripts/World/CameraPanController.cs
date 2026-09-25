using UnityEngine;
using UnityEngine.InputSystem;

namespace JugaadInc
{
    [RequireComponent(typeof(Camera))]
    public class CameraPanController : MonoBehaviour
    {
        [Header("Panning Settings")]
        [SerializeField] private float panSpeed = 12f;
        [SerializeField] private float edgeThresholdPixels = 35f;
        [SerializeField] private bool enableEdgePanning = true;
        [SerializeField] private float smoothTime = 0.12f;

        [Header("Scene Boundaries (World Space)")]
        [SerializeField] private bool enforceBounds = true;
        [SerializeField] private Vector2 minBounds = new Vector2(-20f, -12f);
        [SerializeField] private Vector2 maxBounds = new Vector2(20f, 12f);

        private Camera targetCamera;
        private Vector3 targetPosition;
        private Vector3 currentVelocity;

        public bool EnableEdgePanning
        {
            get => enableEdgePanning;
            set => enableEdgePanning = value;
        }

        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
            if (targetCamera == null) targetCamera = Camera.main;
            targetPosition = transform.position;
        }

        private void Update()
        {
            if (!enableEdgePanning || Mouse.current == null) return;

            if (GameManager.Instance != null && GameManager.Instance.Phase != GamePhase.Playing) return;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector3 panDirection = Vector3.zero;

            if (mousePos.x >= Screen.width - edgeThresholdPixels)
            {
                panDirection.x += 1f;
            }
            else if (mousePos.x <= edgeThresholdPixels)
            {
                panDirection.x -= 1f;
            }

            if (mousePos.y >= Screen.height - edgeThresholdPixels)
            {
                panDirection.y += 1f;
            }
            else if (mousePos.y <= edgeThresholdPixels)
            {
                panDirection.y -= 1f;
            }

            if (panDirection.sqrMagnitude > 0.01f)
            {
                panDirection.Normalize();
                targetPosition += panDirection * (panSpeed * Time.unscaledDeltaTime);

                if (enforceBounds)
                {
                    ClampCameraTargetPosition();
                }
            }

            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
        }

        private void ClampCameraTargetPosition()
        {
            if (targetCamera == null) return;

            float vertExtent = targetCamera.orthographic ? targetCamera.orthographicSize : Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * Mathf.Abs(transform.position.z);
            float horizExtent = vertExtent * targetCamera.aspect;

            float minX = minBounds.x + horizExtent;
            float maxX = maxBounds.x - horizExtent;
            float minY = minBounds.y + vertExtent;
            float maxY = maxBounds.y - vertExtent;

            float clampedX = minX > maxX ? (minBounds.x + maxBounds.x) * 0.5f : Mathf.Clamp(targetPosition.x, minX, maxX);
            float clampedY = minY > maxY ? (minBounds.y + maxBounds.y) * 0.5f : Mathf.Clamp(targetPosition.y, minY, maxY);

            targetPosition.x = clampedX;
            targetPosition.y = clampedY;
        }

        public void SetBounds(Vector2 min, Vector2 max)
        {
            minBounds = min;
            maxBounds = max;
            ClampCameraTargetPosition();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 center = new Vector3((minBounds.x + maxBounds.x) * 0.5f, (minBounds.y + maxBounds.y) * 0.5f, transform.position.z);
            Vector3 size = new Vector3(Mathf.Abs(maxBounds.x - minBounds.x), Mathf.Abs(maxBounds.y - minBounds.y), 1f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
