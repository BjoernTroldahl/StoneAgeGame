using UnityEngine;

public class DragGrain : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform snapPoint; // Circle 1 snapping point
    [SerializeField] private Transform snapPoint2; // Circle 2 snapping point
    [SerializeField] private Transform snapPoint3; // Circle 3 snapping point
    [SerializeField] private SpriteRenderer millingStoneTop;
    [SerializeField] private GameObject millingStone;
    [SerializeField] private Sprite flourSprite;
    [SerializeField] private GameObject grainPrefab; // Prefab for new grain
    [SerializeField] private Transform spawnPoint; // Spawn point for new grain
    [SerializeField] private Sprite originalGrainSprite; // Original grain sprite

    [Header("Settings")]
    [SerializeField] private float snapDistance = 1f;
    [SerializeField] private int maxMillingClicks = 10;
    [SerializeField] private float clickTimeWindow = 0.5f;

    private bool isDragging = false;
    private Vector3 offset;
    private Camera mainCamera;
    private bool isSnapped = false;
    private bool isSnappedToCircle2 = false; // New flag for Circle 2 snapping
    private BoxCollider2D millingStoneTopCollider;
    private int millingCounter = 0;
    private float lastClickTime;
    private SpriteRenderer grainRenderer;
    private bool isMillingComplete = false;
    private bool isSecondGrain = false; // Track if this is the second grain
    private BoxCollider2D grainCollider; // Add this field

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
        }

        grainRenderer = GetComponent<SpriteRenderer>();
        if (grainRenderer != null)
        {
            originalGrainSprite = grainRenderer.sprite; // Store initial sprite
        }

        // Hide milling stone top and disable its collider at start
        if (millingStoneTop != null)
        {
            millingStoneTop.enabled = false;
            millingStoneTopCollider = millingStoneTop.GetComponent<BoxCollider2D>();
            if (millingStoneTopCollider != null)
            {
                millingStoneTopCollider.enabled = false;
            }
        }

        // Get the grain's collider
        grainCollider = GetComponent<BoxCollider2D>();
    }

    void Update()
    {
        if (isDragging && (!isSnapped || isMillingComplete && !isSnappedToCircle2))
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 newPosition = new Vector3(mousePosition.x + offset.x, mousePosition.y + offset.y, transform.position.z);

            // Check if close enough to Circle 1 snap point
            if (!isMillingComplete && Vector2.Distance(newPosition, snapPoint.position) < snapDistance)
            {
                transform.position = snapPoint.position;
                isSnapped = true;
                isDragging = false;
                millingCounter = 0; // Reset counter when snapping to milling position
                Debug.Log("Grain snapped to milling position - Counter reset to 0");
                return;
            }

            // Check if close enough to Circle 2/3 snap point
            if (isMillingComplete && Vector2.Distance(newPosition, 
                (isSecondGrain ? snapPoint3.position : snapPoint2.position)) < snapDistance)
            {
                transform.position = isSecondGrain ? snapPoint3.position : snapPoint2.position;
                isSnappedToCircle2 = true;
                isDragging = false;

                // Only spawn new grain if this isn't the second grain
                if (!isSecondGrain && grainPrefab != null && spawnPoint != null)
                {
                    GameObject newGrain = Instantiate(grainPrefab, spawnPoint.position, Quaternion.identity);
                    DragGrain newGrainScript = newGrain.GetComponent<DragGrain>();
                    if (newGrainScript != null)
                    {
                        // Copy all necessary references
                        newGrainScript.snapPoint = this.snapPoint;
                        newGrainScript.snapPoint2 = this.snapPoint2;
                        newGrainScript.snapPoint3 = this.snapPoint3;
                        newGrainScript.millingStoneTop = this.millingStoneTop;
                        newGrainScript.millingStone = this.millingStone;
                        newGrainScript.flourSprite = this.flourSprite;
                        newGrainScript.grainPrefab = this.grainPrefab;
                        newGrainScript.spawnPoint = this.spawnPoint;
                        newGrainScript.originalGrainSprite = this.originalGrainSprite;
                        newGrainScript.isSecondGrain = true; // Mark as second grain
                        newGrainScript.ResetGrain();
                    }
                }
                return;
            }

            transform.position = newPosition;
        }

        // Check for click on milling stone when grain is snapped
        if (isSnapped && Input.GetMouseButtonDown(0))
        {
            Vector2 clickPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(clickPosition, Vector2.zero);

            if (hit.collider != null)
            {
                // Only allow milling stone interaction if milling isn't complete
                if (!isMillingComplete && hit.collider.gameObject == millingStone && millingStoneTop.enabled == false)
                {
                    // First click on milling stone
                    millingStoneTop.enabled = true;
                    if (millingStoneTopCollider != null)
                    {
                        millingStoneTopCollider.enabled = true;
                    }
                    // Disable grain collider during milling
                    if (grainCollider != null)
                    {
                        grainCollider.enabled = false;
                    }
                }
                else if (hit.collider == millingStoneTopCollider)
                {
                    float timeSinceLastClick = Time.time - lastClickTime;
                    if (timeSinceLastClick <= clickTimeWindow)
                    {
                        millingCounter++;
                        // Force the flip state to toggle based on odd/even count
                        millingStoneTop.flipY = (millingCounter % 2 == 1);
                        Debug.Log($"Milling Counter: {millingCounter}/{maxMillingClicks}, FlipY: {millingStoneTop.flipY}");

                        if (millingCounter == maxMillingClicks)
                        {
                            Debug.Log("Maximum milling reached - Converting to flour");
                            // Milling complete
                            isMillingComplete = true;
                            millingStoneTop.enabled = false;
                            millingStoneTopCollider.enabled = false;
                            if (grainRenderer != null && flourSprite != null)
                            {
                                grainRenderer.sprite = flourSprite;
                                isDragging = true; // Allow dragging again
                            }
                            // Re-enable grain collider after milling
                            if (grainCollider != null)
                            {
                                grainCollider.enabled = true;
                            }
                        }
                    }
                    lastClickTime = Time.time;
                }
            }
        }
    }

    private void OnMouseDown()
    {
        if (!isSnappedToCircle2)
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

    public void ResetGrain()
    {
        // Reset all flags and counters
        isDragging = false;
        isSnapped = false;
        isSnappedToCircle2 = false;
        isMillingComplete = false;
        lastClickTime = 0f;
        millingCounter = 0;
        
        Debug.Log("Grain reset - Milling counter reset to 0");

        // Reset sprite to original grain sprite
        if (grainRenderer == null)
        {
            grainRenderer = GetComponent<SpriteRenderer>();
        }
        
        if (grainRenderer != null)
        {
            grainRenderer.sprite = originalGrainSprite;
            grainRenderer.enabled = true;
        }

        // Reset milling stone top state
        if (millingStoneTop != null)
        {
            millingStoneTop.enabled = false;
            // Don't reset flipY here anymore
            millingStoneTopCollider = millingStoneTop.GetComponent<BoxCollider2D>();
            if (millingStoneTopCollider != null)
            {
                millingStoneTopCollider.enabled = false;
            }
        }
    }
}
