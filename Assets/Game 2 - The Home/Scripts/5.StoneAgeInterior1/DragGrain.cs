using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class DragGrain : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform circle1;
    [SerializeField] private Transform circle2;
    [SerializeField] private Transform circle3;  // Add Circle 3
    [SerializeField] private SpriteRenderer millstone;
    [SerializeField] private Sprite flourSprite;
    [SerializeField] private SpriteRenderer arrowSign;
    [SerializeField] private GameObject grainPrefab;  // Add grain prefab
    [SerializeField] private Vector3 spawnPosition;   // Add spawn position

    [Header("Cloning")]
    [SerializeField] private bool isOriginal = true; // Flag to identify original grain
    [SerializeField] private float cloneOffsetX = 0.5f; // Horizontal offset for the clone

    [Header("Settings")]
    [SerializeField] private float snapDistance = 1f;
    [SerializeField] private int requiredFlips = 10;

    [Header("Click Settings")]
    [SerializeField] private float clickTimeWindow = 0.5f;
    private float lastClickTime;

    private bool isDragging = false;
    private Vector3 offset;
    private Camera mainCamera;
    private bool isSnappedToCircle1 = false;
    private bool isSnappedToCircle2 = false;
    private bool isMillingComplete = false;
    private bool canDragAfterMilling = false; // Indicates if user has clicked on grain after milling
    //private bool waitingForPostMillingClick = false; // New flag to track post-milling click state
    private int flipCounter = 0;
    private BoxCollider2D millstoneCollider;
    private SpriteRenderer grainRenderer;
    private BoxCollider2D grainCollider; // Add reference to own collider

    // Static variables to track game state
    //private static bool isSecondGrain = false;  // Track if this is the second grain
    private static bool firstGrainComplete = false;  // Track if first grain is done
    private static bool secondGrainComplete = false;  // Track if second grain is done
    private static bool isAnyGrainBeingDragged = false; // NEW: Track if any grain is currently being dragged
    private static DragGrain activeDraggedGrain = null; // NEW: Reference to the grain currently being dragged
    private static SpriteRenderer sharedArrowSign = null; // NEW: Static reference to arrow
    private static bool isCircle2Occupied = false; // Track if Circle 2 is occupied
    private static bool isCircle3Occupied = false; // Track if Circle 3 is occupied
    private static bool hasResetStaticVars = false;  // Add flag to track if we've reset vars this scene load
    // Add a static flag to track if the first grain is fully complete
    private static bool isFirstGrainFullyComplete = false;
    
    // Add field to store original sorting order
    private int defaultSortingOrder;

    void Start()
    {
        mainCamera = Camera.main;
        grainRenderer = GetComponent<SpriteRenderer>();
        millstoneCollider = millstone.GetComponent<BoxCollider2D>();
        grainCollider = GetComponent<BoxCollider2D>(); // Get own collider reference
        
        // Store default sorting order
        defaultSortingOrder = grainRenderer.sortingOrder;

        // Reset all static variables when the scene is first loaded (only once per scene load)
        if (isOriginal && !hasResetStaticVars)
        {
            ResetAllStaticVariables();
        }

        // Hide millstone and arrow at start
        millstone.enabled = false;
        millstoneCollider.enabled = false;
        
        // Handle the arrow sign reference differently for original vs clone
        if (isOriginal && arrowSign != null)
        {
            // For original grain, store the reference to the static variable
            sharedArrowSign = arrowSign;
            sharedArrowSign.enabled = false;
            Debug.Log("Arrow sign hidden at start and stored as shared reference");
        }
        else if (!isOriginal)
        {
            // Cloned grain doesn't need its own arrow reference
            // It will use the static sharedArrowSign instead
            Debug.Log("Cloned grain will use shared arrow reference");
            
            // Disable the clone's collider until first grain is complete
            if (grainCollider != null)
            {
                grainCollider.enabled = false;
                Debug.Log("Second grain collider disabled until first grain is complete");
            }
        }
        else if (isOriginal && arrowSign == null)
        {
            // Only show error for original grain with missing reference
            Debug.LogError("Arrow sign reference is missing on original grain!");
        }

        lastClickTime = 0f;
        
        // Spawn the second grain at start if this is the original
        if (isOriginal && grainPrefab != null)
        {
            SpawnSecondGrainAtStart();
        }

        // Reset static dragging variables on start
        if (isOriginal)
        {
            isAnyGrainBeingDragged = false;
            activeDraggedGrain = null;
            firstGrainComplete = false;
            secondGrainComplete = false;
            isCircle2Occupied = false;
            isCircle3Occupied = false;
        }
    }

    // Update each frame to check if second grain should be draggable
    void Update()
    {
        // Enable second grain's collider once first grain is complete
        if (!isOriginal && !grainCollider.enabled && isFirstGrainFullyComplete)
        {
            grainCollider.enabled = true;
            Debug.Log("Second grain collider enabled - now draggable");
        }
        
        // Only process dragging if explicitly allowed AND no other grain is being dragged (or this is the active one)
        if (isDragging && (!isMillingComplete || canDragAfterMilling) && !isSnappedToCircle2 && 
            (activeDraggedGrain == this || !isAnyGrainBeingDragged))
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 newPosition = new Vector3(mousePosition.x + offset.x, mousePosition.y + offset.y, transform.position.z);
            transform.position = newPosition;

            // Check for Circle 1 snap if not milling complete
            if (!isMillingComplete && Vector2.Distance(newPosition, circle1.position) < snapDistance)
            {
                transform.position = circle1.position;
                isSnappedToCircle1 = true;
                isDragging = false;
                EnableMillstone();
                return;
            }

            // Check for final position snap if milling is complete
            if (isMillingComplete && canDragAfterMilling)
            {
                // Check for Circle 2 snap (for both grains)
                if (Vector2.Distance(newPosition, circle2.position) < snapDistance)
                {
                    SnapToCircle2();
                    return;
                }
                
                // Check for Circle 3 snap (for both grains)
                if (Vector2.Distance(newPosition, circle3.position) < snapDistance)
                {
                    SnapToCircle3();
                    return;
                }
            }
        }

        // Handle millstone clicking with time window
        if (isSnappedToCircle1 && !isMillingComplete && Input.GetMouseButtonDown(0))
        {
            Vector2 clickPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(clickPosition, Vector2.zero);

            if (hit.collider == millstoneCollider)
            {
                float timeSinceLastClick = Time.time - lastClickTime;
                if (timeSinceLastClick <= clickTimeWindow)
                {
                    FlipMillstone();
                }
                lastClickTime = Time.time;
            }
        }

        // Handle arrow clicking - only works when both circles are occupied
        if (sharedArrowSign != null && sharedArrowSign.enabled && Input.GetMouseButtonDown(0))
        {
            Vector2 clickPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(clickPosition, Vector2.zero);
            
            Debug.Log($"Arrow click attempt - hit collider: {(hit.collider != null ? hit.collider.name : "none")}");
            
            if (hit.collider != null && hit.collider.gameObject == sharedArrowSign.gameObject)
            {
                Debug.Log("CONGRATS YOU WON THE LEVEL");
                SceneManager.LoadScene(7);
            }
        }
    }

    private void EnableMillstone()
    {
        millstone.enabled = true;
        millstoneCollider.enabled = true;
        
        // Disable grain's collider while milling
        if (grainCollider != null)
        {
            grainCollider.enabled = false;
            Debug.Log("Grain collider disabled during milling");
        }
        
        Debug.Log("Millstone activated");
    }

    private void FlipMillstone()
    {
        flipCounter++;
        millstone.flipY = !millstone.flipY;
        Debug.Log($"Flip counter: {flipCounter}/{requiredFlips}");

        if (flipCounter >= requiredFlips)
        {
            CompleteMilling();
        }
    }

    private void CompleteMilling()
    {
        isMillingComplete = true;
        millstone.enabled = false;
        millstoneCollider.enabled = false;
        
        // Set flour sprite and flip it horizontally
        grainRenderer.sprite = flourSprite;
        grainRenderer.flipX = true;  // Flip the flour sprite horizontally
        
        // Re-enable grain's collider after milling is complete
        if (grainCollider != null)
        {
            grainCollider.enabled = true;
            Debug.Log("Grain collider re-enabled after milling");
        }
        
        // Release the dragging lock momentarily to allow re-clicking
        isDragging = false;
        isAnyGrainBeingDragged = false;
        activeDraggedGrain = null;
        
        canDragAfterMilling = true;
        
        Debug.Log("Milling complete - Flour ready to be dragged (with flipped sprite)");
    }

    private void SnapToCircle2()
    {
        transform.position = circle2.position;
        isSnappedToCircle2 = true;
        isDragging = false;
        
        // Release the dragging lock when snapped
        isAnyGrainBeingDragged = false;
        activeDraggedGrain = null;
        
        // Mark Circle 2 as occupied
        isCircle2Occupied = true;
        
        // If this is the original grain, mark first grain as fully complete
        if (isOriginal)
        {
            isFirstGrainFullyComplete = true;
            Debug.Log("First grain fully complete - second grain can now be dragged");
            
            // No need to explicitly enable second grain collider here,
            // the Update method will handle that
        }
        
        // Check if both circles are now occupied
        CheckCirclesOccupation();
        
        Debug.Log($"Grain [{name}] snapped to Circle 2");
    }

    private void SnapToCircle3()
    {
        transform.position = circle3.position;
        isSnappedToCircle2 = true;  // Reuse this flag for completion state
        isDragging = false;
        
        // Release the dragging lock when snapped
        isAnyGrainBeingDragged = false;
        activeDraggedGrain = null;
        
        // Mark Circle 3 as occupied
        isCircle3Occupied = true;
        
        // If this is the original grain, mark first grain as fully complete
        if (isOriginal)
        {
            isFirstGrainFullyComplete = true;
            Debug.Log("First grain fully complete - second grain can now be dragged");
        }
        
        // Check if both circles are now occupied
        CheckCirclesOccupation();
        
        Debug.Log($"Grain [{name}] snapped to Circle 3");
    }

    // New method to track completion of both flours
    private void CheckBothFloursComplete()
    {
        Debug.Log($"Checking completion - First grain: {firstGrainComplete}, Second grain: {secondGrainComplete}");
        
        // Check if both grains are complete - enable arrow only then
        if (firstGrainComplete && secondGrainComplete)
        {
            // Use the shared static arrow reference
            if (sharedArrowSign != null)
            {
                sharedArrowSign.enabled = true;
                sharedArrowSign.gameObject.SetActive(true); // Also activate the GameObject
                
                // Make sure arrow has a BoxCollider2D for clicking
                BoxCollider2D arrowCollider = sharedArrowSign.GetComponent<BoxCollider2D>();
                if (arrowCollider != null)
                {
                    arrowCollider.enabled = true;
                }
                else
                {
                    Debug.LogWarning("Arrow has no BoxCollider2D - add one for clicking!");
                }
                
                // Explicitly set color to fully opaque
                Color color = sharedArrowSign.color;
                sharedArrowSign.color = new Color(color.r, color.g, color.b, 1.0f);
                
                Debug.Log("Both flours complete - SHARED Arrow sign enabled and made visible!");
            }
            else
            {
                Debug.LogError("Shared arrow sign reference is null! Check inspector assignment.");
            }
        }
        else
        {
            if (sharedArrowSign != null)
            {
                sharedArrowSign.enabled = false;
                Debug.Log($"Not all flours complete yet - Arrow hidden");
            }
        }
    }

    // New method to check if both circles are occupied
    private void CheckCirclesOccupation()
    {
        Debug.Log($"Checking circle occupation - Circle2: {isCircle2Occupied}, Circle3: {isCircle3Occupied}");
        
        // Only enable the arrow when both circles are occupied
        if (isCircle2Occupied && isCircle3Occupied)
        {
            if (sharedArrowSign != null)
            {
                // Make the arrow visible and interactive
                sharedArrowSign.enabled = true;
                sharedArrowSign.gameObject.SetActive(true);
                
                // Make sure the arrow collider is enabled
                BoxCollider2D arrowCollider = sharedArrowSign.GetComponent<BoxCollider2D>();
                if (arrowCollider != null)
                {
                    arrowCollider.enabled = true;
                }
                
                Debug.Log("BOTH CIRCLES OCCUPIED - Arrow sign enabled!");
            }
            else
            {
                Debug.LogError("Cannot enable arrow - shared reference is null!");
            }
        }
    }

    // New method to spawn the second grain at start
    private void SpawnSecondGrainAtStart()
    {
        // Calculate position with offset
        Vector3 clonePosition = transform.position + new Vector3(cloneOffsetX, 0, 0);
        
        GameObject newGrain = Instantiate(grainPrefab, clonePosition, Quaternion.identity);
        DragGrain newGrainScript = newGrain.GetComponent<DragGrain>();
        if (newGrainScript != null)
        {
            newGrainScript.SetAsNonOriginal(); // Mark as non-original to prevent spawning more grains
        }
        Debug.Log("Second grain spawned at start");
    }
    
    // Add method to set as non-original
    public void SetAsNonOriginal()
    {
        isOriginal = false;
        
        // Clone doesn't need its own arrow reference
        arrowSign = null;
        
        // Set sorting order for non-original grain lower
        if (grainRenderer != null)
        {
            grainRenderer.sortingOrder = defaultSortingOrder - 1;
            Debug.Log("Second grain set to lower sorting order");
        }
    }

    // New static method to reset all static variables
    private static void ResetAllStaticVariables()
    {
        firstGrainComplete = false;
        secondGrainComplete = false;
        isAnyGrainBeingDragged = false;
        activeDraggedGrain = null;
        isCircle2Occupied = false;
        isCircle3Occupied = false;
        isFirstGrainFullyComplete = false; // Reset the flag
        hasResetStaticVars = true;  // Mark as reset for this scene load
        
        Debug.Log("All static variables have been reset for new game");
    }

    private void OnMouseDown()
    {
        // For the second (non-original) grain, only allow dragging if the first grain is fully complete
        if (!isOriginal && !isFirstGrainFullyComplete)
        {
            Debug.Log("Cannot drag second grain until first grain is fully complete");
            return;
        }

        // Only allow dragging if: 
        // 1. Not in final position
        // 2. Properly initialized for dragging
        // 3. No other grain is being dragged OR this grain was already being dragged
        if (!isSnappedToCircle2 && (!isMillingComplete || canDragAfterMilling) && 
            (!isAnyGrainBeingDragged || activeDraggedGrain == this))
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            offset = transform.position - mousePosition;
            isDragging = true;
            
            // Mark this grain as the active dragged one
            isAnyGrainBeingDragged = true;
            activeDraggedGrain = this;
            
            // Increase sorting order while dragging to appear on top
            grainRenderer.sortingOrder = defaultSortingOrder + 2;
            
            Debug.Log($"Started dragging grain [{name}] - isMillingComplete: {isMillingComplete}");
        }
        else if (isAnyGrainBeingDragged && activeDraggedGrain != this)
        {
            Debug.Log($"Cannot drag grain [{name}] - Another grain is being dragged");
        }
    }

    private void OnMouseUp()
    {
        if (isDragging)
        {
            isDragging = false;
            
            // Reset the sorting order
            grainRenderer.sortingOrder = defaultSortingOrder;
            
            // Only release the global lock if this grain is done (or if it's not the active one)
            if (isSnappedToCircle2 || activeDraggedGrain != this)
            {
                isAnyGrainBeingDragged = false;
                activeDraggedGrain = null;
                Debug.Log($"Released dragging lock - grain [{name}] is done or no longer active");
            }
        }
    }

    // Add OnDestroy method to reset flag when scene is unloaded
    private void OnDestroy()
    {
        // Reset the hasResetStaticVars flag when the scene is unloaded
        // This ensures variables will be reset next time the scene loads
        if (isOriginal)
        {
            hasResetStaticVars = false;
            Debug.Log("Static variable reset flag cleared");
        }
    }

    void OnEnable()
    {
        // Subscribe to scene loading event (only for original grain)
        if (isOriginal)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }

    void OnDisable()
    {
        // Unsubscribe from scene loading event (only for original grain)
        if (isOriginal)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    // Called when a scene is loaded
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reset variables when this scene is loaded
        if (scene.buildIndex == SceneManager.GetActiveScene().buildIndex)
        {
            ResetAllStaticVariables();
            Debug.Log("Static variables reset on scene reload");
        }
    }
}
