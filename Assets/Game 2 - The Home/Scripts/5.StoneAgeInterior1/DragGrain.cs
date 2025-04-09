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

    [Header("Audio Settings")]
    [SerializeField] private AudioClip snapSound;        // Sound when grain snaps to a circle
    [SerializeField] private AudioClip millstoneSound;   // Sound when millstone flips
    [SerializeField] private float snapSoundVolume = 0.7f;
    [SerializeField] private float millstoneSoundVolume = 0.7f;
    [SerializeField] private float pitchVariation = 0.1f; // Slight pitch variation for more natural sound

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
    private int flipCounter = 0;
    private BoxCollider2D millstoneCollider;
    private SpriteRenderer grainRenderer;
    private BoxCollider2D grainCollider; // Add reference to own collider
    private AudioSource audioSource; // Audio source component for playing sounds

    // Static variables to track game state
    private static bool firstGrainComplete = false;  // Track if first grain is done
    private static bool secondGrainComplete = false;  // Track if second grain is done
    private static bool isAnyGrainBeingDragged = false; // NEW: Track if any grain is currently being dragged
    private static DragGrain activeDraggedGrain = null; // NEW: Reference to the grain currently being dragged
    private static SpriteRenderer sharedArrowSign = null; // NEW: Static reference to arrow
    private static bool isCircle2Occupied = false; // Track if Circle 2 is occupied
    private static bool isCircle3Occupied = false; // Track if Circle 3 is occupied
    private static bool hasResetStaticVars = false;  // Add flag to track if we've reset vars this scene load
    private static bool isFirstGrainFullyComplete = false; // Add a static flag to track if the first grain is fully complete
    
    // Add field to store original sorting order
    private int defaultSortingOrder;

    void Start()
    {
        mainCamera = Camera.main;
        grainRenderer = GetComponent<SpriteRenderer>();
        millstoneCollider = millstone.GetComponent<BoxCollider2D>();
        grainCollider = GetComponent<BoxCollider2D>(); // Get own collider reference
        
        // Set up audio source
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        
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
                
                // Play snap sound for Circle 1
                PlaySnapSound();
                
                EnableMillstone();
                return;
            }

            // Check for final position snap if milling is complete
            if (isMillingComplete && canDragAfterMilling)
            {
                // Check for Circle 2 snap (for both grains) - but only if it's not already occupied
                if (Vector2.Distance(newPosition, circle2.position) < snapDistance && !isCircle2Occupied)
                {
                    SnapToCircle2();
                    return;
                }
                
                // Check for Circle 3 snap (for both grains) - but only if it's not already occupied
                if (Vector2.Distance(newPosition, circle3.position) < snapDistance && !isCircle3Occupied)
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
                SceneManager.LoadScene("6.StoneAgeInteriorArea2");
            }
        }
    }

    // Play snap sound with slight pitch variation
    private void PlaySnapSound()
    {
        if (snapSound != null && audioSource != null)
        {
            // Add slight pitch variation for natural sound
            audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            audioSource.PlayOneShot(snapSound, snapSoundVolume);
            Debug.Log("Playing grain snap sound");
        }
        else if (snapSound == null)
        {
            Debug.LogWarning("Snap sound clip is not assigned!");
        }
    }
    
    // Play millstone flip sound with slight pitch variation
    private void PlayMillstoneSound()
    {
        if (millstoneSound != null && audioSource != null)
        {
            // Add slight pitch variation for natural sound
            audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            audioSource.PlayOneShot(millstoneSound, millstoneSoundVolume);
            Debug.Log("Playing millstone flip sound");
        }
        else if (millstoneSound == null)
        {
            Debug.LogWarning("Millstone sound clip is not assigned!");
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
        // Play millstone flip sound
        PlayMillstoneSound();
        
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
        // Prevent snapping if already occupied
        if (isCircle2Occupied)
        {
            Debug.LogWarning("Circle 2 is already occupied - cannot snap here");
            return;
        }

        transform.position = circle2.position;
        isSnappedToCircle2 = true;
        isDragging = false;
        
        // Play snap sound
        PlaySnapSound();
        
        // Release the dragging lock when snapped
        isAnyGrainBeingDragged = false;
        activeDraggedGrain = null;
        
        // Mark Circle 2 as occupied - this prevents other grains from snapping here
        isCircle2Occupied = true;
        
        // If this is the original grain, mark first grain as fully complete
        if (isOriginal)
        {
            firstGrainComplete = true;
            isFirstGrainFullyComplete = true;
            Debug.Log("First grain fully complete - second grain can now be dragged");
        }
        else
        {
            secondGrainComplete = true;
        }
        
        // Check if both circles are now occupied
        CheckCirclesOccupation();
        
        Debug.Log($"Grain [{name}] snapped to Circle 2");
    }

    private void SnapToCircle3()
    {
        // Prevent snapping if already occupied
        if (isCircle3Occupied)
        {
            Debug.LogWarning("Circle 3 is already occupied - cannot snap here");
            return;
        }

        transform.position = circle3.position;
        isSnappedToCircle2 = true;  // Reuse this flag for completion state
        isDragging = false;
        
        // Play snap sound
        PlaySnapSound();
        
        // Release the dragging lock when snapped
        isAnyGrainBeingDragged = false;
        activeDraggedGrain = null;
        
        // Mark Circle 3 as occupied - this prevents other grains from snapping here
        isCircle3Occupied = true;
        
        // If this is the original grain, mark first grain as fully complete
        if (isOriginal)
        {
            firstGrainComplete = true;
            isFirstGrainFullyComplete = true;
            Debug.Log("First grain fully complete - second grain can now be dragged");
        }
        else
        {
            secondGrainComplete = true;
        }
        
        // Check if both circles are now occupied
        CheckCirclesOccupation();
        
        Debug.Log($"Grain [{name}] snapped to Circle 3");
    }

    private void ReturnToSafePosition()
    {
        // Return to a safe position when unable to snap to an occupied circle
        // Move slightly away from the circle to prevent repeated attempts
        Vector3 safePosition = transform.position + new Vector3(1f, 0, 0);
        transform.position = safePosition;
        Debug.Log($"Grain [{name}] moved to safe position - target was occupied");
    }

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
            
            // Check if near an occupied circle but not snapped
            if (isMillingComplete && canDragAfterMilling)
            {
                Vector2 position2D = new Vector2(transform.position.x, transform.position.y);
                Vector2 circle2Pos = new Vector2(circle2.position.x, circle2.position.y);
                Vector2 circle3Pos = new Vector2(circle3.position.x, circle3.position.y);
                
                // If near Circle 2 but it's occupied
                if (Vector2.Distance(position2D, circle2Pos) < snapDistance && isCircle2Occupied && 
                    !isSnappedToCircle2)
                {
                    Debug.Log("Cannot snap to Circle 2 - already occupied");
                    ReturnToSafePosition();
                }
                // If near Circle 3 but it's occupied
                else if (Vector2.Distance(position2D, circle3Pos) < snapDistance && isCircle3Occupied && 
                    !isSnappedToCircle2)
                {
                    Debug.Log("Cannot snap to Circle 3 - already occupied");
                    ReturnToSafePosition();
                }
            }
            
            // Only release the global lock if this grain is done (or if it's not the active one)
            if (isSnappedToCircle2 || activeDraggedGrain != this)
            {
                isAnyGrainBeingDragged = false;
                activeDraggedGrain = null;
                Debug.Log($"Released dragging lock - grain [{name}] is done or no longer active");
            }
        }
    }

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
