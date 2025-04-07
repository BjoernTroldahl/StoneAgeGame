using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DragDough : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform circle1;
    [SerializeField] private Transform circle2;
    [SerializeField] private Transform circle3;
    [SerializeField] private Transform square1;
    [SerializeField] private Transform square2;
    [SerializeField] private Transform square3;
    [SerializeField] private Transform square4;
    [SerializeField] private Transform square5;
    [SerializeField] private GameObject doughPrefab;
    
    [Header("Sprites")]
    [SerializeField] private Sprite rawBreadSprite;   
    [SerializeField] private Sprite breadHalfSprite;   
    [SerializeField] private Sprite breadFinishedSprite;
    
    [SerializeField] private Vector3 spawnPosition;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Sprite backgroundNoDoughSprite;
    [SerializeField] private SpriteRenderer arrowSign;

    [Header("Settings")]
    [SerializeField] private float snapDistance = 1f;
    [SerializeField] private float cookingTime = 5f;
    private const int MAX_DOUGH = 5;

    // Local instance variables
    private bool isDragging = false;
    private bool isLocked = false;
    private bool isHalfCooked = false;
    private bool isFullyCooked = false;
    private bool canFlip = false;
    private float cookTimer = 0f;
    private Vector3 offset;
    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;
    private int defaultSortingOrder;
    private bool isInitialDough = false;

    // Static variables 
    private static bool circle1Locked = false;
    private static bool circle2Locked = false;
    private static bool circle3Locked = false;
    private static bool square1Occupied = false;
    private static bool square2Occupied = false;
    private static bool square3Occupied = false;
    private static bool square4Occupied = false;
    private static bool square5Occupied = false;
    private static int doughCount = 0;
    
    // This will initialize all static variables
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void OnGameStart()
    {
        SceneManager.sceneLoaded += (scene, mode) => 
        {
            // Reset all statics when this specific scene is loaded
            if (scene.name == "7.Hearth")
            {
                Debug.Log("SCENE LOAD CALLBACK: Resetting all static variables for Hearth scene");
                circle1Locked = false;
                circle2Locked = false;
                circle3Locked = false;
                square1Occupied = false;
                square2Occupied = false;
                square3Occupied = false;
                square4Occupied = false;
                square5Occupied = false;
                doughCount = 0;
            }
        };
    }

    void Awake()
    {
        // Check if this is the initial dough in the scene
        // The initial dough is the one that exists in the scene at start
        // (not one that will be instantiated later)
        if (doughCount == 0)
        {
            isInitialDough = true;
            Debug.Log("Initial dough identified in Awake");
        }
    }

    void Start()
    {
        mainCamera = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Store the default sorting order
        if (spriteRenderer != null)
        {
            defaultSortingOrder = spriteRenderer.sortingOrder;
            spriteRenderer.flipY = false;
        }
        
        // Initial dough logic - always executes only once per scene load
        if (isInitialDough)
        {
            spawnPosition = transform.position;
            doughCount = 1; // Initial dough counts as 1
            Debug.Log($"Initial dough initialized at position {spawnPosition}, doughCount set to {doughCount}");
            
            // Hide arrow sign at start
            if (arrowSign != null)
            {
                arrowSign.enabled = false;
                Debug.Log("Arrow sign hidden at start");
            }
        }
        else
        {
            doughCount++; // Increment for subsequent doughs
            Debug.Log($"Additional dough initialized. doughCount: {doughCount}");
        }
    }

    void Update()
    {
        if (isLocked && !isFullyCooked)
        {
            cookTimer += Time.deltaTime;
            
            // First cooking phase
            if (!isHalfCooked && cookTimer >= cookingTime)
            {
                isHalfCooked = true;
                spriteRenderer.sprite = breadHalfSprite;
                spriteRenderer.flipY = true;
                canFlip = true;
                Debug.Log("Bread is half cooked - Click to flip - flipY = TRUE");
            }
            // Second cooking phase after manual flip
            else if (isHalfCooked && cookTimer >= cookingTime && !canFlip)
            {
                isFullyCooked = true;
                spriteRenderer.sprite = breadFinishedSprite;
                spriteRenderer.flipY = true;
                isLocked = false;
                Debug.Log("Bread is fully cooked - Ready for storage - flipY = TRUE");
            }
        }

        if (isDragging && !isLocked)
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 newPosition = new Vector3(mousePosition.x + offset.x, mousePosition.y + offset.y, transform.position.z);
            transform.position = newPosition;

            if (!isFullyCooked)
            {
                // Check circles for uncooked dough
                if (!circle1Locked && Vector2.Distance(transform.position, circle1.position) < snapDistance)
                    SnapToPosition(circle1.position, 1);
                else if (!circle2Locked && Vector2.Distance(transform.position, circle2.position) < snapDistance)
                    SnapToPosition(circle2.position, 2);
                else if (!circle3Locked && Vector2.Distance(transform.position, circle3.position) < snapDistance)
                    SnapToPosition(circle3.position, 3);
            }
            else
            {
                // Check squares for cooked bread in sequence
                if (!square1Occupied && Vector2.Distance(transform.position, square1.position) < snapDistance)
                    SnapToSquare(square1.position, 1);
                else if (square1Occupied && !square2Occupied && Vector2.Distance(transform.position, square2.position) < snapDistance)
                    SnapToSquare(square2.position, 2);
                else if (square1Occupied && square2Occupied && !square3Occupied && Vector2.Distance(transform.position, square3.position) < snapDistance)
                    SnapToSquare(square3.position, 3);
                else if (square1Occupied && square2Occupied && square3Occupied && !square4Occupied && Vector2.Distance(transform.position, square4.position) < snapDistance)
                    SnapToSquare(square4.position, 4);
                else if (square1Occupied && square2Occupied && square3Occupied && square4Occupied && !square5Occupied && Vector2.Distance(transform.position, square5.position) < snapDistance)
                    SnapToSquare(square5.position, 5);
            }
        }

        // Handle arrow click
        if (square5Occupied && arrowSign != null && arrowSign.enabled && Input.GetMouseButtonDown(0))
        {
            Vector2 clickPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(clickPosition, Vector2.zero);

            if (hit.collider != null && hit.collider.gameObject == arrowSign.gameObject)
            {
                Debug.Log("CONGRATS YOU BEAT THE LEVEL");
                SceneManager.LoadScene("8.AnimalShelter");
            }
        }
    }

    private void OnMouseDown()
    {
        // Handle final dough movement
        if (doughCount == MAX_DOUGH && !isLocked && transform.position == spawnPosition)
        {
            if (backgroundImage != null && backgroundNoDoughSprite != null)
            {
                backgroundImage.sprite = backgroundNoDoughSprite;
                Debug.Log("Changed background - No dough remaining");
            }
        }

        // Handle flipping of half-cooked bread
        if (canFlip && isHalfCooked && !isFullyCooked)
        {
            spriteRenderer.flipY = false;
            canFlip = false;
            cookTimer = 0f;
            Debug.Log("Bread flipped - Cooking second side - flipY = FALSE");
            return;
        }

        bool allCirclesLocked = circle1Locked && circle2Locked && circle3Locked;
        if (!isLocked && (!allCirclesLocked || isFullyCooked))
        {
            if (isFullyCooked && transform.position == circle1.position) circle1Locked = false;
            if (isFullyCooked && transform.position == circle2.position) circle2Locked = false;
            if (isFullyCooked && transform.position == circle3.position) circle3Locked = false;

            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            offset = transform.position - mousePosition;
            isDragging = true;
            
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = 10;
            }
            
            if (isFullyCooked)
            {
                Debug.Log("Circle position freed up for new dough");
            }
        }
    }

    private void SnapToPosition(Vector3 position, int circleNumber)
    {
        transform.position = position;
        isLocked = true;
        isDragging = false;
        cookTimer = 0f;

        if (rawBreadSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = rawBreadSprite;
            spriteRenderer.flipY = false;
            Debug.Log("Changed from dough ball to raw bread - flipY = FALSE");
        }

        switch (circleNumber)
        {
            case 1: circle1Locked = true; break;
            case 2: circle2Locked = true; break;
            case 3: circle3Locked = true; break;
        }

        if (doughCount < MAX_DOUGH) SpawnNewDough();
    }

    private void SnapToSquare(Vector3 position, int squareNumber)
    {
        transform.position = position;
        isLocked = true;
        isDragging = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = squareNumber;
            spriteRenderer.flipY = true;
            Debug.Log($"Set sorting order to {squareNumber} for bread in square {squareNumber} - flipY = TRUE");
        }

        switch (squareNumber)
        {
            case 1: square1Occupied = true; break;
            case 2: square2Occupied = true; break;
            case 3: square3Occupied = true; break;
            case 4: square4Occupied = true; break;
            case 5: 
                square5Occupied = true;
                if (doughCount == MAX_DOUGH && arrowSign != null)
                {
                    arrowSign.enabled = true;
                    
                    SpriteRenderer arrowRenderer = arrowSign.GetComponent<SpriteRenderer>();
                    if (arrowRenderer != null)
                    {
                        arrowRenderer.sortingOrder = 6;
                    }
                    
                    Debug.Log("Final bread placed - Arrow sign enabled");
                }
                break;
        }

        Debug.Log($"Bread stored in square {squareNumber}");
    }

    private void SpawnNewDough()
    {
        if (doughPrefab != null && doughCount < MAX_DOUGH)
        {
            GameObject newDough = Instantiate(doughPrefab, spawnPosition, Quaternion.identity);
            DragDough newDoughScript = newDough.GetComponent<DragDough>();
            
            if (newDoughScript != null)
            {
                newDoughScript.SetReferences(
                    circle1, circle2, circle3,
                    square1, square2, square3, square4, square5,
                    doughPrefab, spawnPosition,
                    rawBreadSprite, breadHalfSprite, breadFinishedSprite,
                    backgroundImage, backgroundNoDoughSprite,
                    arrowSign
                );
                Debug.Log($"Spawned dough #{doughCount} of {MAX_DOUGH} with references");
            }
            else
            {
                Debug.LogError("DragDough component not found on spawned prefab!");
            }
        }
    }

    public void SetReferences(
        Transform c1, Transform c2, Transform c3,
        Transform s1, Transform s2, Transform s3, Transform s4, Transform s5,
        GameObject prefab, Vector3 spawn, 
        Sprite rawSprite, Sprite halfSprite, Sprite finishedSprite,
        Image background, Sprite noDoughSprite,
        SpriteRenderer arrow)
    {
        circle1 = c1;
        circle2 = c2;
        circle3 = c3;
        square1 = s1;
        square2 = s2;
        square3 = s3;
        square4 = s4;
        square5 = s5;
        doughPrefab = prefab;
        spawnPosition = spawn;
        rawBreadSprite = rawSprite;
        breadHalfSprite = halfSprite;
        breadFinishedSprite = finishedSprite;
        backgroundImage = background;
        backgroundNoDoughSprite = noDoughSprite;
        arrowSign = arrow;
    }

    private void OnMouseUp()
    {
        isDragging = false;
        
        if (!isLocked && !isFullyCooked && spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = defaultSortingOrder;
        }
    }
}
