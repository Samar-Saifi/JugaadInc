using UnityEngine;
using UnityEngine.InputSystem;

namespace JugaadInc
{
    public class ScavengeObject : MonoBehaviour
    {
        [Header("Item Configuration")]
        [SerializeField] private ItemData itemData;
        [SerializeField] private int quantity = 1;
        [SerializeField] private bool destroyOnCollect = true;

        [Header("Visual Feedback")]
        [SerializeField] private Color highlightColor = new Color(1f, 0.9f, 0.3f);
        [SerializeField] private float pulseSpeed = 6f;

        private Vector3 baseScale;
        private Renderer objectRenderer;
        private Color baseColor;
        private bool isHighlighted;

        public ItemData ItemData => itemData;

        public void Initialize(ItemData data, int qty = 1)
        {
            itemData = data;
            quantity = qty;
            if (objectRenderer != null && itemData != null)
            {
                baseColor = itemData.DisplayColor;
                objectRenderer.material.color = baseColor;
            }
        }

        private void Awake()
        {
            baseScale = transform.localScale;
            objectRenderer = GetComponent<Renderer>();
            if (objectRenderer != null)
            {
                baseColor = itemData != null ? itemData.DisplayColor : objectRenderer.material.color;
                objectRenderer.material.color = baseColor;
            }
        }

        private void Update()
        {
            float pulse = isHighlighted ? 1.05f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * 0.03f : 1.0f;
            transform.localScale = Vector3.Lerp(transform.localScale, baseScale * pulse, Time.unscaledDeltaTime * 12f);

            if (objectRenderer != null)
            {
                Color target = isHighlighted ? Color.Lerp(baseColor, highlightColor, 0.45f) : baseColor;
                objectRenderer.material.color = Color.Lerp(objectRenderer.material.color, target, Time.unscaledDeltaTime * 10f);
            }

            CheckNewInputSystemClick();
        }

        public void SetHighlighted(bool value)
        {
            isHighlighted = value;
        }

        private void OnMouseEnter()
        {
            if (GameManager.Instance == null || GameManager.Instance.Phase == GamePhase.Playing)
            {
                SetHighlighted(true);
            }
        }

        private void OnMouseExit()
        {
            SetHighlighted(false);
        }

        private void OnMouseDown()
        {
            if (GameManager.Instance == null || GameManager.Instance.Phase == GamePhase.Playing)
            {
                CollectItem();
            }
        }

        private void CheckNewInputSystemClick()
        {
            if (GameManager.Instance != null && GameManager.Instance.Phase != GamePhase.Playing) return;
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

            // 3D Raycast check
            Ray ray = cam.ScreenPointToRay(mouseScreenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    CollectItem();
                    return;
                }
            }

            // 2D Raycast check
            Vector3 worldPoint = cam.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, Mathf.Abs(cam.transform.position.z)));
            RaycastHit2D hit2D = Physics2D.Raycast(worldPoint, Vector2.zero);
            if (hit2D.collider != null)
            {
                if (hit2D.transform == transform || hit2D.transform.IsChildOf(transform))
                {
                    CollectItem();
                }
            }
        }

        public bool CollectItem()
        {
            if (itemData == null) return false;

            var inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : FindFirstObjectByType<InventorySystem>();
            if (inventory == null) return false;

            if (inventory.IsFull)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ShowNotification("Inventory Full! Remove or craft items first.");
                }
                return false;
            }

            bool added = inventory.AddItem(itemData, quantity);
            if (added)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ShowNotification($"Found: {itemData.DisplayName} ({itemData.Category})");
                    GameManager.Instance.PlayItemCollectFx(transform.position, itemData.DisplayColor);
                }

                if (destroyOnCollect)
                {
                    Destroy(gameObject);
                }
                else
                {
                    gameObject.SetActive(false);
                }
                return true;
            }

            return false;
        }
    }

    public sealed class CameraBillboard : MonoBehaviour
    {
        private Camera targetCamera;
        private Vector3 baseScale;
        private float phase;

        private void Awake()
        {
            targetCamera = Camera.main;
            baseScale = transform.localScale;
            phase = Random.value * 6f;
        }

        private void LateUpdate()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - targetCamera.transform.position, targetCamera.transform.up);
            }
            float bob = 1f + Mathf.Sin(Time.unscaledTime * 2.4f + phase) * 0.015f;
            transform.localScale = baseScale * bob;
        }
    }
}
