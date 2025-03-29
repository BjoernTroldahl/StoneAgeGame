using UnityEngine;

public class DragGrain : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform snapPoint; // Circle 1 snapping point
    [SerializeField] private Transform snapPoint2; // Circle 2 snapping point
    [SerializeField] private SpriteRenderer millingStoneTop;
    [SerializeField] private GameObject millingStone;
    [SerializeField] private Sprite flourSprite;

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
    private BoxCollider2D grainCollider; // Add this field
    private Vector2 originalColliderSize; // Add this field

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
        }

        grainRenderer = GetComponent<SpriteRenderer>();
        grainCollider = GetComponent<BoxCollider2D>();
        
        // Store original collider size
        if (grainCollider != null)
        {
            originalColliderSize = grainCollider.size;
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
                return;
            }

            // Check if close enough to Circle 2 snap point
            if (isMillingComplete && Vector2.Distance(newPosition, snapPoint2.position) < snapDistance)
            {
                transform.position = snapPoint2.position;
                isSnappedToCircle2 = true;
                isDragging = false;
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
                    // Show milling stone top and enable its collider
                    millingStoneTop.enabled = true;
                    if (millingStoneTopCollider != null)
                    {
                        millingStoneTopCollider.enabled = true;
                    }
                }
                else if (hit.collider == millingStoneTopCollider)
                {
                    // Clicking on milling stone top
                    float timeSinceLastClick = Time.time - lastClickTime;
                    if (timeSinceLastClick <= clickTimeWindow)
                    {
                        millingCounter++;
                        millingStoneTop.flipY = !millingStoneTop.flipY;

                        if (millingCounter >= maxMillingClicks)
                        {
                            // Milling complete - hide sprite and disable collider
                            isMillingComplete = true;
                            millingStoneTop.enabled = false;
                            if (millingStoneTopCollider != null)
                            {
                                millingStoneTopCollider.enabled = false;
                            }
                            
                            // Change to flour sprite and allow dragging
                            if (grainRenderer != null && flourSprite != null)
                            {
                                grainRenderer.sprite = flourSprite;
                                isSnapped = false; // Allow dragging again
                                isDragging = true;
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
}
