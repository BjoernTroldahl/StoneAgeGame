using UnityEngine;
using System.Collections;

public class DragPorridge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform beerVesselObject;
    [SerializeField] private Transform pourCirclePoint; // NEW: Circle point to rotate around
    [SerializeField] private float overlapDistance = 1f;

    [Header("Sprites")]
    [SerializeField] private Sprite porridgeCovered;
    [SerializeField] private Sprite porridgeUncovered;
    [SerializeField] private Sprite porridgeEmpty;
    [SerializeField] private Sprite beerCovered;
    [SerializeField] private Sprite beerUncovered;
    [SerializeField] private Sprite beerWithPorridge;

    [Header("Animation Settings")]
    [SerializeField] private float moveToCircleDuration = 0.5f;  // NEW: Time to move to circle position
    [SerializeField] private float rotationDuration = 0.8f;      // Time to rotate to pouring position
    [SerializeField] private float pouringDuration = 1.5f;       // Time to hold the pouring position
    [SerializeField] private float returnRotationDuration = 0.8f; // Time to rotate back
    [SerializeField] private float returnMoveDuration = 1.2f;    // Time to move back to start position
    [SerializeField] private float pouringAngle = 90f;           // Angle to rotate for pouring

    private bool isDragging = false;
    private Vector3 offset;
    private Camera mainCamera;
    private Vector3 startPosition;
    private bool isPorridgeUncovered = false;
    private bool isBeerUncovered = false;
    private bool isLocked = false;
    private SpriteRenderer beerRenderer;
    private SpriteRenderer porridgeRenderer;
    private bool isAnimating = false;
    private BoxCollider2D beerCollider;

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
        beerCollider = beerVesselObject?.GetComponent<BoxCollider2D>();

        if (porridgeRenderer == null || beerRenderer == null)
        {
            Debug.LogError("Missing sprite renderer references!");
        }

        // Create the circle point if it doesn't exist
        if (pourCirclePoint == null)
        {
            GameObject circleObj = new GameObject("PouringCirclePoint");
            pourCirclePoint = circleObj.transform;
            
            // Position it relative to the beer vessel
            pourCirclePoint.position = beerVesselObject.position + new Vector3(-0.5f, 0.5f, 0);
            Debug.LogWarning("Created default pour circle point. Consider assigning one in the inspector for better control.");
        }
    }

    void Update()
    {
        // Only allow dragging if not in animation and conditions are met
        if (isDragging && isPorridgeUncovered && isBeerUncovered && !isAnimating)
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 newPosition = new Vector3(mousePosition.x + offset.x, mousePosition.y + offset.y, transform.position.z);
            transform.position = newPosition;
        }
    }

    private void OnMouseDown()
    {
        if (isLocked || isAnimating) return;

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
            
            // Temporarily disable the beer vessel's collider while dragging
            if (beerCollider != null)
            {
                beerCollider.enabled = false;
                Debug.Log("Beer vessel collider temporarily disabled for dragging");
            }
        }
    }

    private void OnMouseUp()
    {
        if (isDragging && isPorridgeUncovered && isBeerUncovered && !isAnimating) // Check all states
        {
            isDragging = false;
            
            // Re-enable the beer vessel's collider
            if (beerCollider != null)
            {
                beerCollider.enabled = true;
                Debug.Log("Beer vessel collider re-enabled");
            }

            // Check if porridge bowl overlaps with beer vessel
            float distanceToBeer = Vector2.Distance(transform.position, beerVesselObject.position);
            
            if (distanceToBeer < overlapDistance)
            {
                // Start the pouring animation
                StartCoroutine(PourAnimation());
            }
        }
    }

    private IEnumerator PourAnimation()
    {
        isAnimating = true;
        
        Vector3 originalPosition = transform.position;
        Quaternion originalRotation = transform.rotation;
        Vector3 circlePosition = pourCirclePoint.position;
        
        // 1. First move to the circle position
        Debug.Log($"Moving to circle point at {circlePosition}");
        float elapsedTime = 0;
        while (elapsedTime < moveToCircleDuration)
        {
            elapsedTime += Time.deltaTime;
            float percentComplete = Mathf.SmoothStep(0, 1, elapsedTime / moveToCircleDuration);
            
            // Move to the circle position without rotation
            transform.position = Vector3.Lerp(originalPosition, circlePosition, percentComplete);
            
            yield return null;
        }
        
        // Make sure we're exactly at the circle position
        transform.position = circlePosition;
        
        // 2. Rotate around the circle center for pouring
        Debug.Log("Starting pour rotation around circle center");
        elapsedTime = 0;
        while (elapsedTime < rotationDuration)
        {
            elapsedTime += Time.deltaTime;
            float percentComplete = elapsedTime / rotationDuration;
            
            // Rotate around circle center
            float angle = Mathf.Lerp(0, pouringAngle, percentComplete);
            transform.position = circlePosition; // Keep at circle position
            transform.rotation = Quaternion.Euler(0, 0, angle);
            
            yield return null;
        }
        
        // Ensure final rotation is exact
        transform.rotation = Quaternion.Euler(0, 0, pouringAngle);
        
        // 3. Hold for pouring duration
        Debug.Log("Holding pour position");
        yield return new WaitForSeconds(pouringDuration);
        
        // Change sprites before rotating back
        porridgeRenderer.sprite = porridgeEmpty;
        beerRenderer.sprite = beerWithPorridge;
        DragHoney.PorridgeHasBeenAdded = true; // Enable honey interaction
        Debug.Log("Porridge emptied into beer vessel");
        
        // 4. Rotate back to original orientation while still at circle position
        Debug.Log("Rotating back to original orientation");
        elapsedTime = 0;
        while (elapsedTime < returnRotationDuration)
        {
            elapsedTime += Time.deltaTime;
            float percentComplete = elapsedTime / returnRotationDuration;
            
            // Rotate back at circle position
            float angle = Mathf.Lerp(pouringAngle, 0, percentComplete);
            transform.position = circlePosition; // Keep at circle position
            transform.rotation = Quaternion.Euler(0, 0, angle);
            
            yield return null;
        }
        
        // Ensure rotation is back to zero
        transform.rotation = originalRotation;
        
        // 5. Move back to starting position
        Debug.Log("Moving back to starting position");
        Vector3 currentPosition = transform.position;
        elapsedTime = 0;
        while (elapsedTime < returnMoveDuration)
        {
            elapsedTime += Time.deltaTime;
            float percentComplete = Mathf.SmoothStep(0, 1, elapsedTime / returnMoveDuration);
            
            transform.position = Vector3.Lerp(currentPosition, startPosition, percentComplete);
            
            yield return null;
        }
        
        // Ensure final position is exact
        transform.position = startPosition;
        
        isLocked = true; // Lock the porridge bowl after animation completes
        isAnimating = false;
        Debug.Log("Pouring animation complete and porridge locked");
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
        return isLocked;
    }

    public void LockSnapping()
    {
        isLocked = true;
        Debug.Log("Snapping has been locked.");
    }
    
    // If the game is paused or the scene changes, make sure to re-enable the beer collider
    private void OnDisable()
    {
        if (beerCollider != null && !beerCollider.enabled)
        {
            beerCollider.enabled = true;
            Debug.Log("Beer vessel collider re-enabled on porridge script disable");
        }
    }
    
    private void OnDestroy()
    {
        if (beerCollider != null && !beerCollider.enabled)
        {
            beerCollider.enabled = true;
            Debug.Log("Beer vessel collider re-enabled on porridge script destroy");
        }
    }
}