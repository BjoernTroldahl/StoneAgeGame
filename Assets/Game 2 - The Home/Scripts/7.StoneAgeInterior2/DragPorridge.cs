using UnityEngine;

public class DragPorridge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform beerVesselObject;
    [SerializeField] private float overlapDistance = 1f;

    [Header("Sprites")]
    [SerializeField] private Sprite porridgeCovered;
    [SerializeField] private Sprite porridgeUncovered;
    [SerializeField] private Sprite porridgeEmpty;
    [SerializeField] private Sprite beerCovered;
    [SerializeField] private Sprite beerUncovered;
    [SerializeField] private Sprite beerWithPorridge;

    private bool isDragging = false;
    private Vector3 offset;
    private Camera mainCamera;
    private Vector3 startPosition;
    private bool isUncovered = false;
    // Duplicate declaration removed
    private SpriteRenderer beerRenderer;
    private SpriteRenderer porridgeRenderer;

    void Awake()
    {
        porridgeRenderer = GetComponent<SpriteRenderer>();
        if (porridgeRenderer == null)
        {
            Debug.LogError("SpriteRenderer not found on DragPorridge!");
        }
    }

    public SpriteRenderer GetPorridgeRenderer()
    {
        return porridgeRenderer;
    }

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
            return;
        }

        startPosition = transform.position;
        porridgeRenderer = GetComponent<SpriteRenderer>();
        beerRenderer = beerVesselObject?.GetComponent<SpriteRenderer>();

        if (porridgeRenderer == null || beerRenderer == null)
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
        if (!isUncovered)
        {
            // First click - uncover the porridge
            porridgeRenderer.sprite = porridgeUncovered;
            isUncovered = true;
            Debug.Log("Porridge uncovered");
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

            // Check if porridge bowl overlaps with beer vessel
            float distanceToBeer = Vector2.Distance(transform.position, beerVesselObject.position);
            
            if (distanceToBeer < overlapDistance)
            {
                // Change sprites
                porridgeRenderer.sprite = porridgeEmpty;
                beerRenderer.sprite = beerWithPorridge;
                
                // Return porridge bowl to start
                transform.position = startPosition;
                
                Debug.Log("Porridge emptied into beer vessel");
            }
        }
    }

    // Method to be called from beer vessel script when clicked
    public static void UncoverBeerVessel(SpriteRenderer beerRenderer, Sprite uncoveredSprite)
    {
        if (beerRenderer != null && uncoveredSprite != null)
        {
            beerRenderer.sprite = uncoveredSprite;
            Debug.Log("Beer vessel uncovered");
        }
    }

    public bool IsSnapped()
    {
        // Replace this with the actual logic to determine if the porridge is snapped
        return true; // Example: Always return true for now
    }

    public void LockSnapping()
    {
        // Implement logic to lock snapping here
        Debug.Log("Snapping has been locked.");
    }
}