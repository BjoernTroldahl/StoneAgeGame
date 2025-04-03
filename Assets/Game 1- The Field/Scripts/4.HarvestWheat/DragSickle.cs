using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class DragSickle : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Sprite bundledWheatSprite;
    [SerializeField] private LayerMask wheatLayer;
    [SerializeField] private float harvestRadius = 0.5f;
    [SerializeField] private int totalWheatCount = 6;
    [SerializeField] private float minSwipeSpeed = 5f;

    [Header("Harvesting Settings")]
    [SerializeField] private float harvestCooldown = 1f;
    
    [Header("Bundled Wheat Settings")]
    [SerializeField] private float bundledWheatRotation = 90f;
    [SerializeField] private Vector2 bundledWheatOffset = new Vector2(0f, -0.5f);
    [SerializeField] private Vector2 bundledWheatColliderSize = new Vector2(23f, 18f);
    
    private bool isDragging = false;
    private Vector3 offset;
    private Camera mainCamera;
    private HashSet<GameObject> harvestedWheat = new HashSet<GameObject>();
    private HashSet<GameObject> hiddenWheat = new HashSet<GameObject>();
    private Vector3 lastPosition;
    private float currentSwipeSpeed;
    private bool canHarvest = true;
    private float harvestTimer = 0f;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
            return;
        }

        // Initialize the HashSets to avoid null reference
        harvestedWheat = new HashSet<GameObject>();
        hiddenWheat = new HashSet<GameObject>();

        // Find all wheat objects and verify their setup
        GameObject[] wheatObjects = GameObject.FindGameObjectsWithTag("Wheat");
        Debug.Log($"Found {wheatObjects.Length} wheat objects with 'Wheat' tag");
        
        // Check if wheat objects are on the correct layer
        foreach (GameObject wheat in wheatObjects)
        {
            if (!((1 << wheat.layer) == wheatLayer.value))
            {
                Debug.LogWarning($"Wheat object {wheat.name} is on layer {LayerMask.LayerToName(wheat.layer)}, expected layer mask {wheatLayer.value}");
                
                // Fix: Set the wheat to the correct layer
                int wheatLayerIndex = Mathf.RoundToInt(Mathf.Log(wheatLayer.value, 2));
                wheat.layer = wheatLayerIndex;
                Debug.Log($"Automatically fixed: Set {wheat.name} to layer {wheatLayerIndex}");
            }
            
            // Verify wheat has a collider
            BoxCollider2D collider = wheat.GetComponent<BoxCollider2D>();
            if (collider == null)
            {
                Debug.LogWarning($"Wheat object {wheat.name} has no BoxCollider2D - adding one");
                collider = wheat.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(1, 1); // Default size
            }
            
            // Make sure collider is enabled
            if (!collider.enabled)
            {
                Debug.LogWarning($"Wheat object {wheat.name} has disabled collider - enabling it");
                collider.enabled = true;
            }
        }
        
        // Debug info about the layer mask
        Debug.Log($"Wheat layer mask value: {wheatLayer.value}");
        for (int i = 0; i < 32; i++)
        {
            if (((1 << i) & wheatLayer.value) != 0)
            {
                Debug.Log($"Wheat layer includes layer {i} ({LayerMask.LayerToName(i)})");
            }
        }
    }

    void Update()
    {
        // Handle harvest cooldown
        if (!canHarvest)
        {
            harvestTimer += Time.deltaTime;
            if (harvestTimer >= harvestCooldown)
            {
                canHarvest = true;
                harvestTimer = 0f;
            }
        }

        if (isDragging)
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePosition.z = transform.position.z; // Keep the same z position
            Vector3 newPosition = new Vector3(mousePosition.x + offset.x, mousePosition.y + offset.y, transform.position.z);
            
            // Calculate swipe speed
            if (lastPosition != Vector3.zero)
            {
                currentSwipeSpeed = Vector3.Distance(newPosition, lastPosition) / Time.deltaTime;
                
                // Only check for wheat if swipe speed is high enough and can harvest
                if (currentSwipeSpeed >= minSwipeSpeed && canHarvest)
                {
                    Debug.Log($"Fast swipe detected! Speed: {currentSwipeSpeed:F2} - Checking for wheat");
                    CheckForWheatAlongPath(lastPosition, newPosition);
                }
            }
            
            transform.position = newPosition;
            lastPosition = newPosition;
        }

        // Handle clicks on harvested wheat
        if (Input.GetMouseButtonDown(0) && !isDragging)
        {
            Vector2 clickPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            
            // Use a larger radius for easier clicking
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(clickPosition, 0.5f, wheatLayer);
            
            foreach (Collider2D hitCollider in hitColliders)
            {
                GameObject wheat = hitCollider.gameObject;
                if (harvestedWheat.Contains(wheat) && !hiddenWheat.Contains(wheat))
                {
                    Debug.Log($"Clicked on harvested wheat at {wheat.transform.position}");
                    HideWheat(wheat);
                    break;
                }
            }
        }
        
        // Show debug info every 5 seconds or so
        if (Time.frameCount % 300 == 0)
        {
            LogWheatStatus();
        }
    }

    private void OnMouseDown()
    {
        Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        offset = transform.position - mousePosition;
        isDragging = true;
        lastPosition = transform.position;
        Debug.Log("Started dragging sickle");
    }

    private void OnMouseUp()
    {
        isDragging = false;
        lastPosition = Vector3.zero;
        Debug.Log("Stopped dragging sickle");
    }

    private void CheckForWheatAlongPath(Vector3 start, Vector3 end)
    {
        Debug.DrawLine(start, end, Color.red, 2.0f); // Visual debugging
        
        // Use a more reliable method: OverlapCircleAll along the path
        Vector3 direction = (end - start).normalized;
        float distance = Vector3.Distance(start, end);
        bool foundWheat = false;
        
        // Check multiple points along the path
        for (float d = 0; d <= distance; d += harvestRadius * 0.5f)
        {
            Vector3 point = start + direction * d;
            Collider2D[] hits = Physics2D.OverlapCircleAll(point, harvestRadius, wheatLayer);
            
            if (hits.Length > 0)
            {
                Debug.Log($"Found {hits.Length} wheat objects at point {d}/{distance} along swipe path");
                
                // Try to harvest the first unharvested wheat
                foreach (Collider2D hit in hits)
                {
                    GameObject wheat = hit.gameObject;
                    if (!harvestedWheat.Contains(wheat))
                    {
                        Debug.Log($"FOUND UNHARVESTED WHEAT at {wheat.transform.position}");
                        BundleWheat(wheat);
                        foundWheat = true;
                        break;
                    }
                }
                
                if (foundWheat) break;
            }
        }
        
        if (!foundWheat)
        {
            Debug.Log("Swipe completed, but no unharvested wheat was found in path.");
        }
    }

    private void BundleWheat(GameObject wheat)
    {
        Debug.Log($"BUNDLING WHEAT: {wheat.name} at position {wheat.transform.position}");
        
        SpriteRenderer spriteRenderer = wheat.GetComponent<SpriteRenderer>();
        BoxCollider2D collider = wheat.GetComponent<BoxCollider2D>();
        
        if (spriteRenderer != null && bundledWheatSprite != null)
        {
            // Save original state for debug
            Sprite originalSprite = spriteRenderer.sprite;
            
            // Change sprite and scale
            spriteRenderer.sprite = bundledWheatSprite;
            wheat.transform.localScale = new Vector3(1f, 1f, 1f);
            
            // Apply position offset
            Vector3 currentPosition = wheat.transform.position;
            wheat.transform.position = new Vector3(
                currentPosition.x + bundledWheatOffset.x, 
                currentPosition.y + bundledWheatOffset.y, 
                currentPosition.z
            );
            
            // Rotate the wheat
            wheat.transform.rotation = Quaternion.Euler(0, 0, bundledWheatRotation);
            
            // Set collider size
            if (collider != null)
            {
                collider.size = bundledWheatColliderSize;
            }

            // Add to harvested collection - with explicit count logging
            int countBefore = harvestedWheat.Count;
            harvestedWheat.Add(wheat);
            int countAfter = harvestedWheat.Count;
            
            Debug.Log($"WHEAT HARVESTED: {wheat.name} - Sprite changed from {originalSprite.name} to {bundledWheatSprite.name}");
            Debug.Log($"Collection count: {countBefore} → {countAfter} of {totalWheatCount}");
            
            // Start cooldown
            canHarvest = false;
            harvestTimer = 0f;
            
            // Check if all wheat has been harvested - VICTORY CONDITION
            if (harvestedWheat.Count >= totalWheatCount)
            {
                Debug.Log("ALL WHEAT HARVESTED - VICTORY CONDITION MET!");
                // Add a slight delay before scene transition to allow players to see the final wheat being harvested
                Invoke("LoadNextScene", 1.0f);
            }
            
            // Force immediate status update
            LogWheatStatus();
        }
        else
        {
            Debug.LogError($"Failed to bundle wheat - SpriteRenderer: {spriteRenderer != null}, BundledSprite: {bundledWheatSprite != null}");
        }
    }
    
    // New method to handle scene transition
    private void LoadNextScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex + 1); // Load the next scene
    }
    
    // Modified HideWheat method - no longer checks for victory condition
    private void HideWheat(GameObject wheat)
    {
        Debug.Log($"HIDING WHEAT: {wheat.name} at position {wheat.transform.position}");
        
        SpriteRenderer spriteRenderer = wheat.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
            
            int countBefore = hiddenWheat.Count;
            hiddenWheat.Add(wheat);
            int countAfter = hiddenWheat.Count;
            
            Debug.Log($"Wheat hidden successfully. Collection count: {countBefore} → {countAfter} of {totalWheatCount}");
            
            // Force immediate status update
            LogWheatStatus();
        }
        else
        {
            Debug.LogError($"Failed to hide wheat - SpriteRenderer missing on {wheat.name}");
        }
    }

    private void LogWheatStatus()
    {
        Debug.Log($"=== WHEAT STATUS REPORT ===");
        Debug.Log($"Harvested: {harvestedWheat.Count}/{totalWheatCount}, Hidden: {hiddenWheat.Count}/{totalWheatCount}");
        
        // Count wheat objects in scene
        GameObject[] allWheatObjects = GameObject.FindGameObjectsWithTag("Wheat");
        Debug.Log($"Total wheat objects in scene with 'Wheat' tag: {allWheatObjects.Length}");
        
        // Check individual wheat objects
        Debug.Log("Individual wheat objects:");
        foreach (GameObject wheat in allWheatObjects)
        {
            bool isHarvested = harvestedWheat.Contains(wheat);
            bool isHidden = hiddenWheat.Contains(wheat);
            
            Debug.Log($"- {wheat.name} at {wheat.transform.position}: " +
                     $"Layer={LayerMask.LayerToName(wheat.layer)}, " +
                     $"Tag={wheat.tag}, " +
                     $"HasCollider={wheat.GetComponent<Collider2D>() != null}, " +
                     $"Harvested={isHarvested}, " +
                     $"Hidden={isHidden}");
        }
        
        Debug.Log($"=== END STATUS REPORT ===");
    }

    // Helper method to visualize the detection area in the editor
    private void OnDrawGizmos()
    {
        if (Application.isPlaying && isDragging && lastPosition != Vector3.zero)
        {
            Vector3 current = transform.position;
            
            // Draw the swipe path
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(lastPosition, current);
            
            // Draw the harvest radius
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(current, harvestRadius);
        }
    }
}