using UnityEngine;
using UnityEngine.SceneManagement;

public class BeerLid : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform snapPoint;
    [SerializeField] private GameObject arrowSign;
    [SerializeField] private GameObject beerVessel; // Add reference to beer vessel
    [SerializeField] private float snapDistance = 1f;
    [SerializeField] private SeasonWheel seasonWheel;

    private bool isDragging = false;
    private Vector3 offset;
    private Camera mainCamera;
    private bool isSnapped = false;
    private Vector3 startPosition;
    private SpriteRenderer arrowRenderer;
    private BoxCollider2D beerVesselCollider;
    private BoxCollider2D lidCollider; // Reference to own collider
    private bool hasDisabledBeerCollider = false;

    private SpriteRenderer lidRenderer; // Reference to lid sprite renderer
    private int defaultSortingOrder = 0; // Default sorting order for lid

    public static bool IsLidSnapped { get; private set; } = false;

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

        // Get lid sprite renderer and store default sorting order
        lidRenderer = GetComponent<SpriteRenderer>();
        if (lidRenderer != null)
        {
            defaultSortingOrder = lidRenderer.sortingOrder;
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

        // Handle arrow click - only if fermentation is complete
        if (arrowSign != null && arrowSign.activeSelf &&
            seasonWheel != null && seasonWheel.IsFermentationComplete() &&
            Input.GetMouseButtonDown(0))
        {
            Vector2 clickPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(clickPosition, Vector2.zero);

            if (hit.collider != null && hit.collider.gameObject == arrowSign)
            {
                Debug.Log("CONGRATS YOU COMPLETED THE BREWING!");
                SceneManager.LoadScene(9); // Load the next level
            }
        }
    }

    private void SnapLidToPosition()
    {
        transform.position = snapPoint.position;
        isSnapped = true;
        IsLidSnapped = true; // Set the static property for other scripts to access

        // Reset sorting order to default when snapped
        if (lidRenderer != null)
        {
            lidRenderer.sortingOrder = defaultSortingOrder;
            Debug.Log($"Reset lid sorting order to default ({defaultSortingOrder}) after snapping");
        }

        // Disable collider when snapped
        if (lidCollider != null)
        {
            lidCollider.enabled = false;
            Debug.Log("Beer lid collider disabled after snapping");
        }

        // Don't show arrow yet - wait for season cycle to complete
        Debug.Log("Lid snapped to final position - Season wheel will appear");
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

    private void OnDestroy()
    {
        IsLidSnapped = false;
    }
}
