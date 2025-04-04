using UnityEngine;

public class DragMixingStick : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform beerVesselObject;
    [SerializeField] private Sprite mixedBeerSprite;
    
    [Header("Settings")]
    [SerializeField] private float snapDistance = 1f;
    [SerializeField] private float snapYOffset = 0.5f;
    [SerializeField] private float rotationSpeed = 200f;
    [SerializeField] private int requiredRotations = 2;
    [SerializeField] private float stirRadius = 0.5f;
    [SerializeField] private float returnMovementDuration = 1f;
    [SerializeField] private float moveToSnapDuration = 0.5f;

    public static bool HoneyHasBeenAdded { get; set; } = false;
    public static bool IsMixingComplete { get; private set; } = false;

    private bool isDragging = false;
    private Vector3 offset;
    private Camera mainCamera;
    private bool isStirring = false;
    private Vector3 startPosition;
    private float currentRotation = 0f;
    private int completedRotations = 0;
    private bool isReturning = false;
    private bool isMovingToSnap = false;
    private float stateTimer = 0f;
    private Quaternion startRotation;
    private Vector3 snapPosition;
    private Vector3 moveStartPosition;
    private bool isComplete = false;
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer beerRenderer;
    private BoxCollider2D boxCollider; // Add reference to box collider

    void Start()
    {
        mainCamera = Camera.main;
        startPosition = transform.position;
        startRotation = transform.rotation;
        spriteRenderer = GetComponent<SpriteRenderer>();
        beerRenderer = beerVesselObject?.GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>(); // Get reference to box collider

        if (mainCamera == null || spriteRenderer == null || beerRenderer == null)
        {
            Debug.LogError("Missing required references!");
        }

        // Disable box collider at the start of the game
        if (boxCollider != null)
        {
            boxCollider.enabled = false;
            Debug.Log("Mixing stick collider disabled at start");
        }
        else
        {
            Debug.LogError("BoxCollider2D missing on mixing stick!");
        }

        // Reset static variables when scene loads
        IsMixingComplete = false;
    }

    void Update()
    {
        // Check if honey has been added and update collider state
        if (HoneyHasBeenAdded && boxCollider != null && !boxCollider.enabled && !isComplete)
        {
            boxCollider.enabled = true;
            Debug.Log("Honey added - Mixing stick collider enabled for interaction");
        }

        if (isMovingToSnap)
        {
            stateTimer += Time.deltaTime;
            float t = stateTimer / moveToSnapDuration;
            Vector3 targetPosition = snapPosition + new Vector3(0, snapYOffset, 0);
            transform.position = Vector3.Lerp(moveStartPosition, targetPosition, t);

            if (t >= 1f)
            {
                isMovingToSnap = false;
                isStirring = true;
                stateTimer = 0f;
                currentRotation = 0f;
                completedRotations = 0;
                Debug.Log("Reached snap point, starting stirring motion");
            }
        }
        else if (isStirring)
        {
            currentRotation += rotationSpeed * Time.deltaTime;
            float rad = currentRotation * Mathf.Deg2Rad;
            
            Vector3 offset = new Vector3(
                Mathf.Cos(rad) * stirRadius,
                Mathf.Sin(rad) * stirRadius + snapYOffset,
                0
            );
            
            transform.position = beerVesselObject.position + offset;
            
            if (currentRotation >= 360f)
            {
                currentRotation = 0f;
                completedRotations++;
                Debug.Log($"Completed rotation {completedRotations}/{requiredRotations}");
                
                if (completedRotations >= requiredRotations)
                {
                    isStirring = false;
                    isReturning = true;
                    stateTimer = 0f;
                    moveStartPosition = transform.position;
                    // Don't change rotation here, let the lerp handle it

                    if (beerRenderer != null && mixedBeerSprite != null)
                    {
                        beerRenderer.sprite = mixedBeerSprite;
                        Debug.Log("Changed beer vessel sprite to mixed version");
                    }

                    Debug.Log("Stirring complete, returning to start");
                }
            }
        }
        else if (isReturning)
        {
            stateTimer += Time.deltaTime;
            float t = Mathf.Clamp01(stateTimer / returnMovementDuration); // Ensure t is between 0 and 1
            
            // Use SmoothStep for smoother animation
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector3.Lerp(moveStartPosition, startPosition, smoothT);
            transform.rotation = Quaternion.Lerp(Quaternion.Euler(0, 0, -30), startRotation, smoothT);

            if (t >= 1f)
            {
                isReturning = false;
                isComplete = true;
                IsMixingComplete = true;
                
                // Disable collider when mixing is complete
                if (boxCollider != null)
                {
                    boxCollider.enabled = false;
                    Debug.Log("Mixing complete - stick collider disabled");
                }
                
                // Ensure final position and rotation are exact
                transform.position = startPosition;
                transform.rotation = startRotation;
                Debug.Log("Return complete - Mixing stick locked");
            }
        }
        else if (isDragging)
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 newPosition = new Vector3(mousePosition.x + offset.x, mousePosition.y + offset.y, transform.position.z);
            transform.position = newPosition;

            float distanceToBeer = Vector2.Distance(transform.position, beerVesselObject.position);
            if (distanceToBeer < snapDistance)
            {
                isDragging = false;
                isMovingToSnap = true; // Skip initial rotation, go straight to moving
                stateTimer = 0f;
                moveStartPosition = transform.position;
                snapPosition = beerVesselObject.position;
                transform.rotation = Quaternion.Euler(0, 0, -30); // Instantly rotate to -30 degrees
                Debug.Log("Starting movement to snap point");
            }
        }
    }

    private void OnMouseDown()
    {
        if (!isComplete && HoneyHasBeenAdded && !isStirring && !isReturning && !isMovingToSnap)
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            offset = transform.position - mousePosition;
            isDragging = true;
            transform.rotation = Quaternion.Euler(0, 0, -30); // Instantly rotate when clicked
            Debug.Log("Started dragging mixing stick");
        }
    }

    private void OnMouseUp()
    {
        if (!isStirring && !isReturning && !isMovingToSnap)
        {
            isDragging = false;
            
            // If the player drops the stick away from the beer, return to start position
            if (isDragging)
            {
                transform.position = startPosition;
                transform.rotation = startRotation;
                Debug.Log("Mixing stick returned to start position");
            }
        }
    }

    // Add a public method to reset the stick if needed
    public void ResetStick()
    {
        isDragging = false;
        isStirring = false;
        isReturning = false;
        isMovingToSnap = false;
        isComplete = false;
        transform.position = startPosition;
        transform.rotation = startRotation;
        
        // Update box collider state based on whether honey has been added
        if (boxCollider != null)
        {
            boxCollider.enabled = HoneyHasBeenAdded && !isComplete;
            Debug.Log($"Mixing stick reset - collider enabled: {boxCollider.enabled}");
        }
    }
}