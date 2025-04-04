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
    [SerializeField] private Sprite rawBreadSprite;    // Raw bread in the pan
    [SerializeField] private Sprite breadHalfSprite;   // Half-cooked bread 
    [SerializeField] private Sprite breadFinishedSprite; // Fully cooked bread
    
    [SerializeField] private Vector3 spawnPosition;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Sprite backgroundNoDoughSprite;
    [SerializeField] private SpriteRenderer arrowSign;

    [Header("Settings")]
    [SerializeField] private float snapDistance = 1f;
    [SerializeField] private float cookingTime = 5f;
    private const int MAX_DOUGH = 5;

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

    void Start()
    {
        mainCamera = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Store the default sorting order
        if (spriteRenderer != null)
        {
            defaultSortingOrder = spriteRenderer.sortingOrder;
            // Ensure dough starts with flipY = FALSE
            spriteRenderer.flipY = false;
            Debug.Log("Starting with flipY = FALSE (dough ball)");
        }
        
        if (doughCount == 0) spawnPosition = transform.position;
        doughCount++;
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
                // When bread becomes half-cooked, set flipY = TRUE
                spriteRenderer.flipY = true;
                canFlip = true;
                Debug.Log("Bread is half cooked - Click to flip - flipY = TRUE");
            }
            // Second cooking phase after manual flip
            else if (isHalfCooked && cookTimer >= cookingTime && !canFlip)
            {
                isFullyCooked = true;
                spriteRenderer.sprite = breadFinishedSprite;
                // When bread is fully cooked, set flipY = TRUE (showing top side)
                spriteRenderer.flipY = true;
                isLocked = false; // Allow dragging to squares
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
                SceneManager.LoadScene(9); // Load the next level
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
            // When manually flipping the half-cooked bread, set flipY = FALSE (showing bottom side)
            spriteRenderer.flipY = false;
            canFlip = false;
            // Reset timer for second phase of cooking
            cookTimer = 0f; // Reset to 0 to start the second timer fresh
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
            
            // Increase sorting order while dragging to appear on top
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = 10; // Higher than any stacked bread
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

        // Change sprite from dough ball to raw bread when snapped to a circle
        if (rawBreadSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = rawBreadSprite;
            // When bread is raw and placed in pan, set flipY = FALSE (showing raw top side)
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

        // Set the sorting order based on the square number
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = squareNumber;
            // Ensure fully cooked bread has flipY = TRUE when placed in storage
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
                    
                    // Make sure arrow is on top of all bread
                    SpriteRenderer arrowRenderer = arrowSign.GetComponent<SpriteRenderer>();
                    if (arrowRenderer != null)
                    {
                        arrowRenderer.sortingOrder = 6; // One higher than the highest bread
                    }
                    
                    Debug.Log("Final bread placed - Arrow sign enabled");
                }
                break;
        }

        Debug.Log($"Bread stored in square {squareNumber}");
    }

    private void SpawnNewDough()
    {
        if (doughPrefab != null)
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
                Debug.Log($"Spawned dough #{doughCount + 1} of {MAX_DOUGH} with references");
            }
            else
            {
                Debug.LogError("DragDough component not found on spawned prefab!");
            }
        }
    }

    // Add new method to set references
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
        
        // Reset sorting order when dropping objects that aren't fully cooked
        if (!isLocked && !isFullyCooked && spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = defaultSortingOrder;
        }
    }
}
