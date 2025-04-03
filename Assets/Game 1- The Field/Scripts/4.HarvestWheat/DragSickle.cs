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
    [SerializeField] private float bundledWheatRotation = 90f; // Angle in degrees
    [SerializeField] private Vector2 bundledWheatOffset = new Vector2(0f, -0.5f); // Offset for position
    [SerializeField] private Vector2 bundledWheatColliderSize = new Vector2(23f, 18f); // Collider size
    
    private bool isDragging = false;
    private Vector3 offset;
    private Camera mainCamera;
    private HashSet<GameObject> harvestedWheat = new HashSet<GameObject>();
    private HashSet<GameObject> hiddenWheat = new HashSet<GameObject>();
    private Vector3 lastPosition;
    private float currentSwipeSpeed;
    private bool canHarvest = true;
    private float harvestTimer = 0f;
    //private bool hasHarvestedThisSwipe = false;

    void Start()
    {
        // Add at start of Start() method
        Application.logMessageReceived += HandleLog;

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
            return;
        }

        // More robust layer verification
        if (wheatLayer.value == 0)
        {
            // Try to find the wheat layer by name
            int layerNumber = LayerMask.NameToLayer("Wheat");
            if (layerNumber != -1)
            {
                wheatLayer = 1 << layerNumber;
                Debug.Log($"Found wheat layer automatically: {layerNumber}");
            }
            else
            {
                Debug.LogError("Wheat Layer not found in build! Please check project settings.");
            }
        }

        // Verify wheat objects are on correct layer
        GameObject[] wheatObjects = GameObject.FindGameObjectsWithTag("Wheat");
        foreach (GameObject wheat in wheatObjects)
        {
            if (!((1 << wheat.layer) == wheatLayer.value))
            {
                Debug.LogWarning($"Wheat object {wheat.name} is on wrong layer: {LayerMask.LayerToName(wheat.layer)}");
            }
        }

        Debug.Log($"DragSickle initialized with: Harvest Radius={harvestRadius}, Layer={wheatLayer.value}, Expected layer mask={(1 << LayerMask.NameToLayer("Wheat"))}");
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
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
                Debug.Log("Harvesting cooldown finished - Can harvest again");
            }
        }

        if (isDragging)
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 newPosition = new Vector3(mousePosition.x + offset.x, mousePosition.y + offset.y, transform.position.z);
            
            // Calculate swipe speed
            if (lastPosition != Vector3.zero)
            {
                currentSwipeSpeed = Vector3.Distance(newPosition, lastPosition) / Time.deltaTime;
                Debug.Log($"Current swipe speed: {currentSwipeSpeed}, Required: {minSwipeSpeed}, Can Harvest: {canHarvest}");
                
                // Only check for wheat if swipe speed is high enough and can harvest
                if (currentSwipeSpeed >= minSwipeSpeed && canHarvest)
                {
                    Debug.Log($"Checking for wheat between {lastPosition} and {newPosition}");
                    CheckForWheatAlongPath(lastPosition, newPosition);
                }
            }
            
            transform.position = newPosition;
            lastPosition = newPosition;
        }

        // Handle clicks on bundled wheat
        if (Input.GetMouseButtonDown(0) && !isDragging)
        {
            Vector2 clickPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(clickPosition, Vector2.zero, 0f, wheatLayer);

            if (hit.collider != null && harvestedWheat.Contains(hit.collider.gameObject))
            {
                HideWheat(hit.collider.gameObject);
            }
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
        RaycastHit2D[] hits = Physics2D.CircleCastAll(
            start, 
            harvestRadius, 
            (end - start).normalized, 
            Vector2.Distance(start, end), 
            wheatLayer
        );

        // Debug logging for hit detection
        if (hits.Length > 0)
        {
            Debug.Log($"Found {hits.Length} potential wheat objects in path");
        }

        // Only harvest the first unharvested wheat found
        foreach (RaycastHit2D hit in hits)
        {
            GameObject wheat = hit.collider.gameObject;
            if (!harvestedWheat.Contains(wheat))
            {
                Debug.Log($"Attempting to harvest wheat at position {wheat.transform.position}");
                BundleWheat(wheat);
                return; // Exit after harvesting one wheat
            }
        }
    }

    private void BundleWheat(GameObject wheat)
    {
        SpriteRenderer spriteRenderer = wheat.GetComponent<SpriteRenderer>();
        BoxCollider2D collider = wheat.GetComponent<BoxCollider2D>();
        
        if (spriteRenderer != null && bundledWheatSprite != null)
        {
            // Change sprite and scale
            spriteRenderer.sprite = bundledWheatSprite;
            wheat.transform.localScale = new Vector3(1f, 1f, 1f);
            
            // Apply position offset for better visual placement
            Vector3 currentPosition = wheat.transform.position;
            wheat.transform.position = new Vector3(
                currentPosition.x + bundledWheatOffset.x, 
                currentPosition.y + bundledWheatOffset.y, 
                currentPosition.z
            );
            
            // Rotate the wheat to make it appear lying down sideways
            // Using the editor parameter for rotation
            wheat.transform.rotation = Quaternion.Euler(0, 0, bundledWheatRotation);
            
            // Set specific collider size for bundled wheat
            if (collider != null)
            {
                // Use the configurable collider size
                collider.size = bundledWheatColliderSize;
            }

            harvestedWheat.Add(wheat);
            
            // Start cooldown
            canHarvest = false;
            harvestTimer = 0f;
            
            Debug.Log($"Wheat harvested and rotated {bundledWheatRotation}° with offset {bundledWheatOffset} at {wheat.transform.position}");
        }
    }

    private void HideWheat(GameObject wheat)
    {
        SpriteRenderer spriteRenderer = wheat.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
            hiddenWheat.Add(wheat);

            // Check if ALL wheat is harvested and hidden
            if (hiddenWheat.Count == totalWheatCount && 
                harvestedWheat.Count == totalWheatCount)
            {
                Debug.Log("CONGRATS YOU WON THE GAME");
                SceneManager.LoadScene(5); // Load the end screen
            }
        }
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        string filePath = System.IO.Path.Combine(Application.dataPath, "../game_log.txt");
        System.IO.File.AppendAllText(filePath, $"[{System.DateTime.Now}] {type}: {logString}\n");
        if (type == LogType.Error || type == LogType.Exception)
        {
            System.IO.File.AppendAllText(filePath, $"Stack Trace: {stackTrace}\n");
        }
    }
}