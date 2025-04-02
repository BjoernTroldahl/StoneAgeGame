using UnityEngine;
using System.Collections;
public class DragMilkVessel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform circle1;
    [SerializeField] private Sprite milkedVesselSprite;  // Original milk sprite
    [SerializeField] private Sprite milk1Sprite;         // Add new sprite reference
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

            // Check for snapping distance
            if (Vector2.Distance(transform.position, circle1.position) < snapDistance)
            {
                SnapToCircle();
            }
        }
    }

    private void OnMouseDown()
    {
        // Allow initial dragging without being milked
        if (!isSnapped)
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            offset = transform.position - mousePosition;
            isDragging = true;

            // Change vessel sprite immediately when dragged
            if (spriteRenderer != null && milkedVesselSprite != null)
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

    private void SnapToCircle()
    {
        transform.position = circle1.position;
        isSnapped = true;
        isDragging = false;
        
        // Enable cow collider when vessel snaps
        if (cowCollider != null)
        {
            cowCollider.enabled = true;
            Debug.Log("Cow interaction enabled");
        }
        
        Debug.Log("Milk vessel snapped to circle");
    }

    // Add separate script for cow click handling
    public void OnCowClicked()
    {
        if (isSnapped && !isMilked)
        {
            StartCoroutine(MilkCowAnimation());
            Debug.Log("Starting cow milking sequence");
        }
        else
        {
            Debug.Log($"Cow click ignored - Snapped: {isSnapped}, Milked: {isMilked}");
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
        
        // We don't need to set the sprite here anymore since it will be done by the droplet callback
        isMilked = true;
        
        Debug.Log($"Milking complete after {milkingAnimationDuration} seconds - Vessel filled");
    }

    private void OnDropletAnimationComplete()
    {
        if (spriteRenderer != null && milk1Sprite != null)
        {
            spriteRenderer.sprite = milk1Sprite;
            Debug.Log("Vessel filled with milk");
        }
    }
}
