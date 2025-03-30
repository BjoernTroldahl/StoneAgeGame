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
    private bool hasDisabledCollider = false;

    void Start()
    {
        mainCamera = Camera.main;
        startPosition = transform.position;
        
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
        // Check if mixing is complete and collider hasn't been disabled yet
        if (DragMixingStick.IsMixingComplete && !hasDisabledCollider && beerVesselCollider != null)
        {
            beerVesselCollider.enabled = false;
            hasDisabledCollider = true;
            Debug.Log("Beer vessel collider disabled");
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
