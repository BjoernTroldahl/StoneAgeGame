using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class DragMilkVessel : MonoBehaviour
{
    // Static variables to track game state
    private static bool isTargetOccupied = false;
    private static DragMilkVessel occupyingVessel = null;
    private static int completedVessels = 0;     // Track completed vessels
    private static int totalVessels = 3;         // Total vessels needed to win

    [Header("References")]
    [SerializeField] private Transform startCircle;
    [SerializeField] private Transform targetCircle;
    [SerializeField] private Sprite milkedVesselSprite;
    [SerializeField] private Sprite milk1Sprite;
    [SerializeField] private Sprite milk2Sprite;
    [SerializeField] private Sprite milk3Sprite;
    [SerializeField] private GameObject cow;
    [SerializeField] private Sprite cowIdleSprite;
    [SerializeField] private Sprite cowMilkingSprite;
    [SerializeField] private MilkDroplet milkDroplet;

    [Header("Cloning")]
    [SerializeField] private bool isOriginal = true;
    [SerializeField] private Transform circle3;
    [SerializeField] private Transform circle4;
    [SerializeField] private GameObject vesselPrefab;

    [Header("Settings")]
    [SerializeField] private float snapDistance = 1f;
    [SerializeField] private float milkingAnimationDuration = 0.5f;
    [SerializeField] private int defaultSortingOrder = 0;   // Default order in layer 
    [SerializeField] private int dragSortingOrder = 4;      // Order in layer when dragging

    private bool isDragging = false;
    private bool isSnapped = false;
    private bool isCompleted = false;  // Renamed from isMilked - this marks if vessel has been counted
    private Vector3 offset;
    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D cowCollider;
    private int milkLevel = 0;

    // Each vessel needs its own droplet instance
    private MilkDroplet myDroplet;

    void Start()
    {
        mainCamera = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
            return;
        }
        if (spriteRenderer == null)
        {
            Debug.LogError("Sprite Renderer not found!");
            return;
        }
        
        // Always ensure cow collider starts disabled
        if (cow != null)
        {
            cowCollider = cow.GetComponent<BoxCollider2D>();
            if (cowCollider != null)
            {
                cowCollider.enabled = false;
                Debug.Log($"Cow collider disabled for [{name}] at start");
            }
        }
        
        // Clone the droplet for each vessel
        if (milkDroplet != null && !isOriginal)
        {
            GameObject dropletClone = Instantiate(milkDroplet.gameObject, milkDroplet.transform.position, Quaternion.identity);
            myDroplet = dropletClone.GetComponent<MilkDroplet>();
            if (myDroplet != null)
            {
                myDroplet.OnDropletAnimationComplete += OnDropletAnimationComplete;
            }
        }
        else
        {
            myDroplet = milkDroplet;
            if (myDroplet != null)
            {
                myDroplet.OnDropletAnimationComplete += OnDropletAnimationComplete;
            }
        }
        
        // Position at start circle
        if (startCircle != null)
        {
            transform.position = startCircle.position;
            Debug.Log($"Vessel [{name}] positioned at start circle");
        }
        
        // Spawn clones if this is the original vessel
        if (isOriginal && vesselPrefab != null)
        {
            SpawnClones();
        }
        
        // Log initial completion state
        if (isOriginal)
        {
            Debug.Log($"Game started with {completedVessels}/{totalVessels} vessels completed");
        }
    }

    private void OnDestroy()
    {
        if (myDroplet != null)
        {
            myDroplet.OnDropletAnimationComplete -= OnDropletAnimationComplete;
        }
        
        // Reset completion counter when original is destroyed
        if (isOriginal)
        {
            Debug.Log($"Original vessel destroyed, resetting completion counter from {completedVessels} to 0");
            completedVessels = 0;
        }
    }

    void Update()
    {
        if (isDragging && !isSnapped)
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            transform.position = new Vector3(mousePosition.x + offset.x, 
                                          mousePosition.y + offset.y, 
                                          transform.position.z);

            // Check for target circle snapping only when not occupied or occupied by this vessel
            if (milkLevel < 3 && Vector2.Distance(transform.position, targetCircle.position) < snapDistance && 
                (!isTargetOccupied || occupyingVessel == this))
            {
                SnapToCircle(targetCircle, 1);
            }
            else if (milkLevel >= 3 && Vector2.Distance(transform.position, startCircle.position) < snapDistance)
            {
                SnapToCircle(startCircle, 2);
            }
        }
    }

    private void OnMouseDown()
    {
        // If vessel is at target and fully milked, allow dragging
        if (isSnapped && milkLevel >= 3)
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            offset = transform.position - mousePosition;
            isDragging = true;
            isSnapped = false; // Unsnap immediately
            
            // Increase sorting order when dragging
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = dragSortingOrder;
                Debug.Log($"Vessel [{name}] sorting order set to {dragSortingOrder} while dragging");
            }
            
            // Free target if this was occupying it
            if (isTargetOccupied && occupyingVessel == this)
            {
                isTargetOccupied = false;
                occupyingVessel = null;
                
                // Disable cow collider
                if (cow != null && cowCollider != null)
                {
                    cowCollider.enabled = false;
                    Debug.Log($"Cow interaction disabled by [{name}]");
                }
                
                Debug.Log($"Target circle freed by [{name}]");
            }
            
            Debug.Log($"Vessel [{name}] (milk level {milkLevel}) being moved to final position");
            return;
        }
        
        // Allow initial dragging if not snapped yet
        if (!isSnapped)
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            offset = transform.position - mousePosition;
            isDragging = true;
            
            // Increase sorting order when dragging
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = dragSortingOrder;
                Debug.Log($"Vessel [{name}] sorting order set to {dragSortingOrder} while dragging");
            }
            
            if (milkLevel == 0 && spriteRenderer != null && milkedVesselSprite != null)
            {
                spriteRenderer.sprite = milkedVesselSprite;
                Debug.Log($"Vessel [{name}] sprite changed on drag");
            }
            return;
        }
        
        // If we get here, it means the vessel is snapped but not fully milked
        Debug.Log($"Vessel [{name}] is snapped and not draggable yet (milk level: {milkLevel})");
    }

    private void OnMouseUp()
    {
        isDragging = false;
    }

    // Add this method to check if this vessel is occupying the target
    public bool IsOccupyingTarget()
    {
        return isTargetOccupied && occupyingVessel == this;
    }

    public void OnCowClicked()
    {
        // Add more detailed logging
        Debug.Log($"OnCowClicked called for {name}, isSnapped: {isSnapped}, milkLevel: {milkLevel}");
        
        if (isSnapped && milkLevel < 3 && IsOccupyingTarget())
        {
            StartCoroutine(MilkCowAnimation());
            Debug.Log($"Vessel [{name}] starting milking - Current level: {milkLevel}");
        }
        else
        {
            Debug.Log($"Vessel [{name}] cannot be milked - isSnapped: {isSnapped}, milkLevel: {milkLevel}, occupying: {IsOccupyingTarget()}");
        }
    }

    private void SnapToCircle(Transform circleTransform, int circleNumber)
    {
        transform.position = circleTransform.position;
        isSnapped = true;
        isDragging = false;
        
        // Reset sorting order when snapped
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = defaultSortingOrder;
            Debug.Log($"Vessel [{name}] sorting order reset to {defaultSortingOrder} after snapping");
        }
        
        if (circleNumber == 1)
        {
            // Mark target as occupied by this vessel
            isTargetOccupied = true;
            occupyingVessel = this;
            
            // Enable cow collider only when a vessel is at the target circle
            if (cow != null && cowCollider != null)
            {
                cowCollider.enabled = true;
                Debug.Log($"Vessel [{name}] enabled cow interaction");
            }
            Debug.Log($"Vessel [{name}] is now occupying target circle");
        }
        else if (circleNumber == 2)
        {
            // Detailed logging
            Debug.Log($"VESSEL COMPLETION CHECK: [{name}] at final position - milkLevel: {milkLevel}, isCompleted: {isCompleted}");
            Debug.Log($"Current completion count: {completedVessels}/{totalVessels}");
            
            // Only count if this vessel is fully milked and wasn't already counted
            if (milkLevel >= 3 && !isCompleted)
            {
                isCompleted = true; // Mark as counted for completion
                completedVessels++;
                Debug.Log($"VESSEL COMPLETED: [{name}] now counts toward total! New count: {completedVessels}/{totalVessels}");
                
                // Check for win condition
                CheckWinCondition();
            }
            else
            {
                // Log why it wasn't counted
                Debug.Log($"VESSEL NOT COUNTED: [{name}] - milkLevel: {milkLevel}, isCompleted: {isCompleted}");
            }
        }
    }
    
    private void CheckWinCondition()
    {
        Debug.Log($"WIN CONDITION CHECK: {completedVessels}/{totalVessels} vessels completed");
        
        if (completedVessels >= totalVessels)
        {
            Debug.Log("*************************************************");
            Debug.Log("*************** CONGRATS! YOU WON ***************");
            Debug.Log("*************************************************");
            
            // Start coroutine to delay scene loading
            StartCoroutine(DelayedWinSequence());
        }
        else
        {
            Debug.Log($"Not enough vessels completed yet, need {totalVessels - completedVessels} more");
        }
    }

    // New coroutine for delayed win sequence
    private IEnumerator DelayedWinSequence()
    {
        // You could add visual effects here
        Debug.Log("Starting win delay - will load next scene in 2 seconds");
        
        // Wait for 2 seconds
        yield return new WaitForSeconds(1f);
        
        // Load the next scene
        Debug.Log("Delay complete - Loading next scene");
        SceneManager.LoadScene("EndScreen2");
    }

    private IEnumerator MilkCowAnimation()
    {
        SpriteRenderer cowRenderer = cow.GetComponent<SpriteRenderer>();
        if (cowRenderer == null)
        {
            Debug.LogError("No SpriteRenderer found on cow!");
            yield break;
        }

        Debug.Log($"Changing cow sprite for vessel [{name}]");
        cowRenderer.sprite = cowMilkingSprite;
        
        if (myDroplet != null)
        {
            myDroplet.TriggerDropletFall();
            Debug.Log($"Triggered droplet fall for vessel [{name}]");
        }
        else
        {
            Debug.LogError($"Droplet is null for vessel [{name}]");
        }
        
        yield return new WaitForSeconds(milkingAnimationDuration);
        
        cowRenderer.sprite = cowIdleSprite;
        Debug.Log($"Completed milking animation for vessel [{name}]");
    }

    private void OnDropletAnimationComplete()
    {
        milkLevel++;
        
        if (spriteRenderer != null)
        {
            if (milkLevel == 1 && milk1Sprite != null)
            {
                spriteRenderer.sprite = milk1Sprite;
                Debug.Log($"Vessel [{name}] filled to level 1");
            }
            else if (milkLevel == 2 && milk2Sprite != null)
            {
                spriteRenderer.sprite = milk2Sprite;
                Debug.Log($"Vessel [{name}] filled to level 2");
            }
            else if (milkLevel == 3 && milk3Sprite != null)
            {
                spriteRenderer.sprite = milk3Sprite;
                Debug.Log($"Vessel [{name}] filled to level 3 (maximum)");
                
                // Only disable cow collider when fully milked (level 3)
                if (cow != null && cowCollider != null)
                {
                    cowCollider.enabled = false;
                    Debug.Log($"Cow interaction disabled after vessel [{name}] is fully milked");
                }
            }
        }
    }

    private void SpawnClones()
    {
        if (circle3 != null)
        {
            GameObject clone1 = Instantiate(vesselPrefab, circle3.position, Quaternion.identity);
            clone1.name = "Vessel_Clone1";
            DragMilkVessel cloneScript1 = clone1.GetComponent<DragMilkVessel>();
            if (cloneScript1 != null)
            {
                cloneScript1.isOriginal = false;
                cloneScript1.startCircle = circle3;
                cloneScript1.targetCircle = targetCircle;
                Debug.Log("Spawned first clone at circle3");
            }
        }
        
        if (circle4 != null)
        {
            GameObject clone2 = Instantiate(vesselPrefab, circle4.position, Quaternion.identity);
            clone2.name = "Vessel_Clone2";
            DragMilkVessel cloneScript2 = clone2.GetComponent<DragMilkVessel>();
            if (cloneScript2 != null)
            {
                cloneScript2.isOriginal = false;
                cloneScript2.startCircle = circle4;
                cloneScript2.targetCircle = targetCircle;
                Debug.Log("Spawned second clone at circle4");
            }
        }
    }
}
