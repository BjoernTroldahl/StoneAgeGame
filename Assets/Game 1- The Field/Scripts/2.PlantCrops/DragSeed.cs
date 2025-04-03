using System.Collections.Specialized;
using System.Security.Cryptography;
using UnityEngine;

public class Drag : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject seedPrefab;
    [SerializeField] private Vector2 spawnPosition = new Vector2(13.59f, -4.87f);
    private static int seedCount = 0; // Track total seeds
    private const int MAX_SEEDS = 6; // Maximum number of clones (not including original seed)
    
    public delegate void DragEndedDelegate(Drag draggableObject);
    public DragEndedDelegate dragEndedCallback;
    private bool dragging = false;
    private bool isLocked = false;
    private Vector3 offset;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("No SpriteRenderer found on draggable object!");
            return;
        }
        
        // Get reference to the box collider
        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            Debug.LogWarning("No BoxCollider2D found on draggable object. Collision detection might not work correctly.");
        }
        
        // Initialize seed count for the first seed
        if (seedCount == 0)
        {
            seedCount = 1;
            Debug.Log("Initial seed counted: 1");
        }

        // Ensure this seed starts with its collider enabled
        if (boxCollider != null)
        {
            boxCollider.enabled = true;
        }
    }

    void Update()
    {
        if (dragging && !isLocked)
        {
            transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition) + offset;
        }
    }

    private void OnMouseDown()
    {
        // Debug to check if the collider is working
        Debug.Log($"Mouse down on seed at {transform.position}, isLocked: {isLocked}");
        
        if (spriteRenderer != null && spriteRenderer.color.a > 0 && !isLocked)
        {
            offset = transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
            dragging = true;
            Debug.Log($"Started dragging seed at {transform.position}");
        }
    }

    private void OnMouseUp()
    {
        if (dragging)
        {
            dragging = false;
            Debug.Log($"Stopped dragging seed at {transform.position}");
            
            if (dragEndedCallback != null)
            {
                dragEndedCallback(this);
            }
        }
    }

    public void LockInPlace()
    {
        if (!isLocked)
        {
            Debug.Log($"Locking seed at position {transform.position}");
            isLocked = true;
            dragging = false;
            
            // Disable ONLY this specific seed's box collider
            if (boxCollider != null)
            {
                boxCollider.enabled = false;
                Debug.Log($"Disabled box collider for seed at position {transform.position} (instance ID: {gameObject.GetInstanceID()})");
            }
            
            // Only spawn a new seed if we haven't reached the maximum
            if (seedCount < MAX_SEEDS)
            {
                SpawnNewSeed();
            }
        }
    }

    private void SpawnNewSeed()
    {
        if (seedPrefab != null)
        {
            seedCount++; // Increment count before spawning
            Debug.Log($"Spawning seed #{seedCount} of {MAX_SEEDS + 1}");
            
            GameObject newSeed = Instantiate(seedPrefab, spawnPosition, Quaternion.identity);
            Drag newDrag = newSeed.GetComponent<Drag>();
            if (newDrag != null)
            {
                // Ensure the new seed has its collider enabled
                BoxCollider2D newCollider = newSeed.GetComponent<BoxCollider2D>();
                if (newCollider != null)
                {
                    newCollider.enabled = true;
                    Debug.Log($"Enabled collider for new seed (instance ID: {newSeed.GetInstanceID()})");
                }
                
                newDrag.dragEndedCallback = this.dragEndedCallback;
            }
        }
    }

    private void OnDestroy()
    {
        ResetDragVariables();
    }

    private static void ResetDragVariables()
    {
        seedCount = 0;
        Debug.Log("Reset DragSeed variables to default values");
    }

    // You can also make this public if you need to reset from elsewhere
    public static void ResetAll()
    {
        ResetDragVariables();
    }
}
