using UnityEngine;

public class DragPorridge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform snapPoint;
    [SerializeField] private Transform porridgeObject;
    [SerializeField] private Transform bowlObject;
    [SerializeField] private Vector3 bowlStartPosition;

    [Header("Settings")]
    [SerializeField] private float snapDistance = 1f;

    private bool isDragging = false;
    private Vector3 offset;
    private Camera mainCamera;
    private bool isSnapped = false;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
            return;
        }

        if (snapPoint != null)
        {
            Debug.Log($"Circle 1 initial position: {snapPoint.position}");
        }
        else
        {
            Debug.LogError("Snap point (Circle 1) not assigned!");
        }

        // Store the bowl's initial position and set initial sorting order
        if (bowlObject != null)
        {
            bowlStartPosition = bowlObject.position;
        }

        // Set initial Order in Layer
        SpriteRenderer porridgeRenderer = porridgeObject?.GetComponent<SpriteRenderer>();
        if (porridgeRenderer != null)
        {
            porridgeRenderer.sortingOrder = 4;
        }
    }

    void Update()
    {
        if (isDragging && !isSnapped)
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 newPosition = new Vector3(mousePosition.x + offset.x, mousePosition.y + offset.y, transform.position.z);
            
            // Move the entire prefab (both bowl and porridge)
            transform.position = newPosition;

            // Get the center position of the porridge object
            Vector3 porridgeCenter = porridgeObject.position;
            Vector3 snapCenter = snapPoint.position;

            // Debug positions
            Debug.Log($"Porridge center: {porridgeCenter}, Snap point center: {snapCenter}");

            // Calculate distance between centers
            float distanceToSnap = Vector2.Distance(porridgeCenter, snapCenter);
            Debug.Log($"Distance between centers: {distanceToSnap}, Required distance: {snapDistance}");

            // Check for snap distance using centers
            if (distanceToSnap < snapDistance)
            {
                Debug.Log($"Snapping - Distance: {distanceToSnap}");
                
                // Snap porridge to circle and bowl to start
                porridgeObject.position = snapCenter;
                bowlObject.position = bowlStartPosition;
                
                // Change Order in Layer
                SpriteRenderer porridgeRenderer = porridgeObject?.GetComponent<SpriteRenderer>();
                if (porridgeRenderer != null)
                {
                    porridgeRenderer.sortingOrder = 1;
                    Debug.Log("Changed porridge sorting order from 4 to 1");
                }
                
                // Lock everything
                isSnapped = true;
                DisableColliders();
                Debug.Log("Snap complete - Porridge centered on Circle 1");
            }
        }
    }

    private void OnMouseDown()
    {
        if (!isSnapped)
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            offset = transform.position - mousePosition;
            isDragging = true;
        }
    }

    private void OnMouseUp()
    {
        isDragging = false;
    }

    private void DisableColliders()
    {
        // Disable all colliders after snapping
        GetComponent<BoxCollider2D>().enabled = false;
    }

    public bool IsSnapped()
    {
        return isSnapped;
    }

    public SpriteRenderer GetPorridgeRenderer()
    {
        return porridgeObject?.GetComponent<SpriteRenderer>();
    }

    public void LockSnapping()
    {
        isSnapped = true; // This prevents further snapping
        Debug.Log("Circle 1 snapping locked");
    }
}
