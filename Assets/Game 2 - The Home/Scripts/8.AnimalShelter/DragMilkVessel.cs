using UnityEngine;
using System.Collections;

public class DragMilkVessel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform circle1;
    [SerializeField] private Sprite milkedVesselSprite;
    [SerializeField] private GameObject cow;               // Changed to GameObject
    [SerializeField] private Sprite cowIdleSprite;
    [SerializeField] private Sprite cowMilkingSprite;

    [Header("Settings")]
    [SerializeField] private float snapDistance = 1f;

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
        Debug.Log("Cow milking animation started");
        
        // Wait for half a second
        yield return new WaitForSeconds(0.5f);
        
        // Change back to idle sprite
        cowRenderer.sprite = cowIdleSprite;
        
        // Set milked state and change vessel sprite after animation
        isMilked = true;
        if (spriteRenderer != null && milkedVesselSprite != null)
        {
            spriteRenderer.sprite = milkedVesselSprite;
        }
        
        Debug.Log("Milking complete - Vessel filled");
    }
}
