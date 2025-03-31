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
    private bool isPorridgeUncovered = false;
    private bool isBeerUncovered = false;
    private bool isLocked = false;
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
        if (isDragging && isPorridgeUncovered && isBeerUncovered)
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 newPosition = new Vector3(mousePosition.x + offset.x, mousePosition.y + offset.y, transform.position.z);
            transform.position = newPosition;
        }
    }

    private void OnMouseDown()
    {
        if (isLocked) return;

        if (!isPorridgeUncovered)
        {
            // First click - uncover the porridge
            porridgeRenderer.sprite = porridgeUncovered;
            isPorridgeUncovered = true;
            Debug.Log("Porridge uncovered");
        }
        else if (isBeerUncovered) // Only allow dragging if beer is also uncovered
        {
            // Start dragging
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            offset = transform.position - mousePosition;
            isDragging = true;
        }
    }

    private void OnMouseUp()
    {
        if (isDragging && isPorridgeUncovered && isBeerUncovered) // Check both states
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
                isLocked = true; // Lock the porridge bowl
                DragHoney.PorridgeHasBeenAdded = true; // Enable honey interaction
                Debug.Log("Porridge emptied into beer vessel and locked");
            }
        }
    }

    // Method to be called from beer vessel script when clicked
    public static void UncoverBeerVessel(SpriteRenderer beerRenderer, Sprite uncoveredSprite, DragPorridge porridgeScript)
    {
        if (beerRenderer != null && uncoveredSprite != null)
        {
            beerRenderer.sprite = uncoveredSprite;
            porridgeScript.isBeerUncovered = true;
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