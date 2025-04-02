using UnityEngine;
using System.Collections;
public class DragMilkVessel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform circle1;
    [SerializeField] private Transform circle2;           // Add second circle reference
    [SerializeField] private Sprite milkedVesselSprite;   // Empty vessel sprite
    [SerializeField] private Sprite milk1Sprite;          // First level milk sprite
    [SerializeField] private Sprite milk2Sprite;          // Second level milk sprite
    [SerializeField] private Sprite milk3Sprite;          // Third level milk sprite (add this)
    [SerializeField] private GameObject cow;
    [SerializeField] private Sprite cowIdleSprite;
    [SerializeField] private Sprite cowMilkingSprite;
    [SerializeField] private MilkDroplet milkDroplet;

    [Header("Settings")]
    [SerializeField] private float snapDistance = 1f;
    [SerializeField] private float milkingAnimationDuration = 0.5f;  // Add new parameter

    private bool isDragging = false;
    private bool isSnapped = false;
    private bool isMilked = false;  // Add new state variable
    private Vector3 offset;
    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;  // Add sprite renderer reference
    private BoxCollider2D cowCollider;
    private int milkLevel = 0;  // Track milk level counter

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
        
        if (cow != null)
        {
            cowCollider = cow.GetComponent<BoxCollider2D>();
            if (cowCollider != null)
            {
                cowCollider.enabled = false; // Disable cow collider at start
            }
        }

        // Subscribe to droplet animation completion
        if (milkDroplet != null)
        {
            milkDroplet.OnDropletAnimationComplete += OnDropletAnimationComplete;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (milkDroplet != null)
        {
            milkDroplet.OnDropletAnimationComplete -= OnDropletAnimationComplete;
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

            // Check for circle1 snapping only when not milked fully
            if (milkLevel < 3 && Vector2.Distance(transform.position, circle1.position) < snapDistance)
            {
                SnapToCircle(circle1, 1);
            }
            // Check for circle2 snapping only when fully milked
            else if (milkLevel >= 3 && Vector2.Distance(transform.position, circle2.position) < snapDistance)
            {
                SnapToCircle(circle2, 2);
            }
        }
    }

    private void OnMouseDown()
    {
        // Allow dragging after fully milked or if not snapped
        if ((milkLevel >= 3 && isSnapped) || !isSnapped)
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            offset = transform.position - mousePosition;
            isDragging = true;
            
            // Reset snapped state if fully milked and was previously snapped
            if (milkLevel >= 3 && isSnapped)
            {
                isSnapped = false;
                Debug.Log("Vessel ready for final placement");
            }
            
            // Only change sprite if it's first being picked up (not milk level 3)
            if (milkLevel == 0 && spriteRenderer != null && milkedVesselSprite != null)
            {
                spriteRenderer.sprite = milkedVesselSprite;
                Debug.Log("Vessel sprite changed on drag");
            }
            return;
        }
    }

    private void OnMouseUp()
    {
        isDragging = false;
    }

    public void OnCowClicked()
    {
        if (isSnapped && milkLevel < 3)  // Allow up to 3 milk levels
        {
            StartCoroutine(MilkCowAnimation());
            Debug.Log($"Starting cow milking sequence - Current milk level: {milkLevel}");
        }
        else
        {
            Debug.Log($"Cow click ignored - Snapped: {isSnapped}, Milk Level: {milkLevel}");
        }
    }

    private void SnapToCircle(Transform circleTransform, int circleNumber)
    {
        transform.position = circleTransform.position;
        isSnapped = true;
        isDragging = false;
        
        if (circleNumber == 1)
        {
            // Enable cow collider only when snapped to first circle
            if (cowCollider != null)
            {
                cowCollider.enabled = true;
                Debug.Log("Cow interaction enabled");
            }
            Debug.Log("Milk vessel snapped to circle 1");
        }
        else if (circleNumber == 2)
        {
            Debug.Log("Filled vessel placed in final position!");
            // Here you could trigger game progression or other events
        }
    }

    // Modify MilkCowAnimation to set the state after animation
    private IEnumerator MilkCowAnimation()
    {
        SpriteRenderer cowRenderer = cow.GetComponent<SpriteRenderer>();
        if (cowRenderer == null)
        {
            Debug.LogError("No SpriteRenderer found on cow!");
            yield break;
        }

        // Change to milking sprite
        cowRenderer.sprite = cowMilkingSprite;
        
        // Trigger droplet animation
        if (milkDroplet != null)
        {
            milkDroplet.TriggerDropletFall();
        }
        
        Debug.Log("Cow milking animation started");
        
        // Use the configurable duration
        yield return new WaitForSeconds(milkingAnimationDuration);
        
        // Change back to idle sprite
        cowRenderer.sprite = cowIdleSprite;
        
        // Set milked state but don't change sprite yet
        isMilked = true;
        
        Debug.Log($"Milking complete after {milkingAnimationDuration} seconds");
    }

    private void OnDropletAnimationComplete()
    {
        milkLevel++;
        
        if (spriteRenderer != null)
        {
            if (milkLevel == 1 && milk1Sprite != null)
            {
                spriteRenderer.sprite = milk1Sprite;
                Debug.Log("Vessel filled to level 1");
            }
            else if (milkLevel == 2 && milk2Sprite != null)
            {
                spriteRenderer.sprite = milk2Sprite;
                Debug.Log("Vessel filled to level 2");
            }
            else if (milkLevel == 3 && milk3Sprite != null)
            {
                spriteRenderer.sprite = milk3Sprite;
                Debug.Log("Vessel filled to level 3 (maximum) - Ready for final placement");
            }
        }
    }
}
