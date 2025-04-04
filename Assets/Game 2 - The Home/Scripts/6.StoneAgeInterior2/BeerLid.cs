using UnityEngine;

public class BeerLid : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform snapPoint;
    [SerializeField] private GameObject arrowSign;
    [SerializeField] private GameObject beerVessel; // Add reference to beer vessel
    [SerializeField] private float snapDistance = 1f;

    private bool isDragging = false;
    private Vector3 offset;
    private Camera mainCamera;
    private bool isSnapped = false;
    private Vector3 startPosition;
    private SpriteRenderer arrowRenderer;
    private BoxCollider2D beerVesselCollider;
    private BoxCollider2D lidCollider; // Reference to own collider
    private bool hasDisabledBeerCollider = false;

    void Start()
    {
        mainCamera = Camera.main;
        startPosition = transform.position;
        
        // Get and disable own collider
        lidCollider = GetComponent<BoxCollider2D>();
        if (lidCollider != null)
        {
            lidCollider.enabled = false;
            Debug.Log("Beer lid collider disabled at start");
        }
        else
        {
            Debug.LogError("BoxCollider2D not found on beer lid!");
        }
        
        // Get arrow sign sprite renderer and disable it
        if (arrowSign != null)
        {
            arrowRenderer = arrowSign.GetComponent<SpriteRenderer>();
            if (arrowRenderer != null)
            {
                arrowRenderer.enabled = false;
                arrowSign.SetActive(false);
            }
        }

        if (beerVessel != null)
        {
            beerVesselCollider = beerVessel.GetComponent<BoxCollider2D>();
            if (beerVesselCollider == null)
            {
                Debug.LogError("BoxCollider2D not found on beer vessel!");
            }
        }
    }

    void Update()
    {
        // Check if mixing is complete
        if (DragMixingStick.IsMixingComplete)
        {
            // Disable beer vessel collider if not already done
            if (!hasDisabledBeerCollider && beerVesselCollider != null)
            {
                beerVesselCollider.enabled = false;
                hasDisabledBeerCollider = true;
                Debug.Log("Beer vessel collider disabled");
            }
            
            // Enable lid collider if not already snapped
            if (!isSnapped && lidCollider != null && !lidCollider.enabled)
            {
                lidCollider.enabled = true;
                Debug.Log("Beer lid collider enabled - mixing complete");
            }
        }

        // Only allow dragging if mixing is complete and not already snapped
        if (isDragging && !isSnapped && DragMixingStick.IsMixingComplete)
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 newPosition = new Vector3(
                mousePosition.x + offset.x, 
                mousePosition.y + offset.y, 
                transform.position.z
            );
            transform.position = newPosition;

            // Check for snap point overlap
            float distanceToSnap = Vector2.Distance(transform.position, snapPoint.position);
            if (distanceToSnap < snapDistance)
            {
                SnapLidToPosition();
            }
        }
    }

    private void SnapLidToPosition()
    {
        transform.position = snapPoint.position;
        isSnapped = true;
        
        // Disable collider when snapped
        if (lidCollider != null)
        {
            lidCollider.enabled = false;
            Debug.Log("Beer lid collider disabled after snapping");
        }
        
        // Enable both the GameObject and its sprite renderer
        if (arrowSign != null && arrowRenderer != null)
        {
            arrowSign.SetActive(true);
            arrowRenderer.enabled = true;
            Debug.Log("Arrow sign enabled and visible");
        }
        
        Debug.Log("Lid snapped to final position");
    }

    private void OnMouseDown()
    {
        // Only allow interaction if mixing is complete and not already snapped
        if (DragMixingStick.IsMixingComplete && !isSnapped)
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            offset = transform.position - mousePosition;
            isDragging = true;
            Debug.Log("Started dragging lid");
        }
    }

    private void OnMouseUp()
    {
        isDragging = false;
    }
}
