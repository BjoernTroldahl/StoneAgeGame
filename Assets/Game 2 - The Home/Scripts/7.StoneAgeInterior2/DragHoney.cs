using UnityEngine;

public class DragHoney : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform beerVesselObject;
    [SerializeField] private float overlapDistance = 1f;

    [Header("Sprites")]
    [SerializeField] private Sprite honeyCovered;
    [SerializeField] private Sprite honeyUncovered;
    [SerializeField] private Sprite honeyEmpty;
    [SerializeField] private Sprite beerWithHoney;

    private bool isDragging = false;
    private Vector3 offset;
    private Camera mainCamera;
    private Vector3 startPosition;
    private bool isUncovered = false;
    private SpriteRenderer honeyRenderer;
    private SpriteRenderer beerRenderer;
    private bool isLocked = false;

    // Add static flag to check if porridge has been added
    public static bool PorridgeHasBeenAdded { get; set; } = false;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
            return;
        }

        startPosition = transform.position;
        honeyRenderer = GetComponent<SpriteRenderer>();
        beerRenderer = beerVesselObject?.GetComponent<SpriteRenderer>();

        if (honeyRenderer == null || beerRenderer == null)
        {
            Debug.LogError("Missing sprite renderer references!");
        }
    }

    void Update()
    {
        if (isDragging && isUncovered)
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 newPosition = new Vector3(mousePosition.x + offset.x, mousePosition.y + offset.y, transform.position.z);
            transform.position = newPosition;
        }
    }

    private void OnMouseDown()
    {
        // Only allow interaction if porridge has been added and honey isn't locked
        if (!PorridgeHasBeenAdded || isLocked)
        {
            return;
        }

        if (!isUncovered)
        {
            // First click - uncover the honey
            honeyRenderer.sprite = honeyUncovered;
            isUncovered = true;
            Debug.Log("Honey uncovered");
        }
        else
        {
            // Start dragging
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            offset = transform.position - mousePosition;
            isDragging = true;
        }
    }

    private void OnMouseUp()
    {
        if (isDragging)
        {
            isDragging = false;

            // Check if honey bowl overlaps with beer vessel
            float distanceToBeer = Vector2.Distance(transform.position, beerVesselObject.position);
            
            if (distanceToBeer < overlapDistance)
            {
                // Change sprites
                honeyRenderer.sprite = honeyEmpty;
                beerRenderer.sprite = beerWithHoney;
                
                // Return honey bowl to start
                transform.position = startPosition;
                isLocked = true; // Lock the honey bowl
                
                Debug.Log("Honey emptied into beer vessel and locked");
                DragMixingStick.HoneyHasBeenAdded = true; // Enable mixing stick interaction
            }
        }
    }
}
