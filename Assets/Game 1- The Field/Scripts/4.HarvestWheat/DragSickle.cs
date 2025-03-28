using UnityEngine;
using System.Collections.Generic;

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
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
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
            Vector3 newPosition = new Vector3(mousePosition.x + offset.x, mousePosition.y + offset.y, transform.position.z);
            
            // Calculate swipe speed
            if (lastPosition != Vector3.zero)
            {
                currentSwipeSpeed = Vector3.Distance(newPosition, lastPosition) / Time.deltaTime;
                
                // Only check for wheat if swipe speed is high enough and can harvest
                if (currentSwipeSpeed >= minSwipeSpeed && canHarvest)
                {
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
        //hasHarvestedThisSwipe = false;
    }

    private void OnMouseUp()
    {
        isDragging = false;
        lastPosition = Vector3.zero;
        //hasHarvestedThisSwipe = false;
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

        // Only harvest the first unharvested wheat found
        foreach (RaycastHit2D hit in hits)
        {
            GameObject wheat = hit.collider.gameObject;
            if (!harvestedWheat.Contains(wheat))
            {
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
            wheat.transform.localScale = new Vector3(0.2f, 0.2f, 1f);

            // Set specific collider size for bundled wheat
            if (collider != null)
            {
                collider.size = new Vector2(18f, 23f);
            }

            harvestedWheat.Add(wheat);
            
            // Start cooldown
            canHarvest = false;
            harvestTimer = 0f;
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
            }
        }
    }
}