using UnityEngine;

public class SpriteAlternator : MonoBehaviour
{
    [Header("Sprite Settings")]
    [SerializeField] private Sprite firstSprite;
    [SerializeField] private Sprite secondSprite;
    [SerializeField] private float switchInterval = 0.5f; // Time in seconds between sprite switches
    
    [Header("Target Object")]
    [SerializeField] private GameObject targetObject; // The object that when clicked will hide this object
    [SerializeField] private bool hideImmediately = true; // Whether to hide immediately or wait for the next sprite switch
    
    // Private variables
    private SpriteRenderer spriteRenderer;
    private float timeSinceLastSwitch = 0f;
    private bool isFirstSprite = true;
    private bool shouldHide = false;
    
    void Start()
    {
        // Get the sprite renderer component
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer == null)
        {
            Debug.LogError("No SpriteRenderer found on this object!");
            enabled = false; // Disable this script
            return;
        }
        
        // Initialize with the first sprite
        if (firstSprite != null)
        {
            spriteRenderer.sprite = firstSprite;
        }
        else
        {
            Debug.LogWarning("First sprite not assigned!");
        }
        
        // Check if the target object exists
        if (targetObject == null)
        {
            Debug.LogWarning("No target object assigned! This object won't be hidden.");
        }
    }

    void Update()
    {
        // Check if the object should hide
        if (shouldHide)
        {
            if (hideImmediately)
            {
                // Hide immediately
                gameObject.SetActive(false);
                return;
            }
            else
            {
                // Wait for the next sprite switch before hiding
                timeSinceLastSwitch += Time.deltaTime;
                if (timeSinceLastSwitch >= switchInterval)
                {
                    gameObject.SetActive(false);
                    return;
                }
            }
        }
        
        // Increment the timer
        timeSinceLastSwitch += Time.deltaTime;
        
        // Check if it's time to switch sprites
        if (timeSinceLastSwitch >= switchInterval)
        {
            // Reset timer
            timeSinceLastSwitch = 0f;
            
            // Switch the sprite
            if (isFirstSprite)
            {
                if (secondSprite != null)
                {
                    spriteRenderer.sprite = secondSprite;
                }
            }
            else
            {
                if (firstSprite != null)
                {
                    spriteRenderer.sprite = firstSprite;
                }
            }
            
            // Toggle the flag
            isFirstSprite = !isFirstSprite;
        }
        
        // Check for clicks on the target object
        if (Input.GetMouseButtonDown(0) && targetObject != null)
        {
            // Cast a ray from the camera to the mouse position
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
            
            // Check if the ray hit the target object
            if (hit.collider != null && hit.collider.gameObject == targetObject)
            {
                Debug.Log("Target object clicked! Hiding this object.");
                shouldHide = true;
            }
        }
    }
    
    // Public method to manually hide this object (can be called from other scripts)
    public void HideObject()
    {
        shouldHide = true;
    }
    
    // Public method to manually show this object (can be called from other scripts)
    public void ShowObject()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        shouldHide = false;
    }
    
    // Public method to manually toggle visibility
    public void ToggleVisibility()
    {
        if (gameObject.activeSelf)
        {
            shouldHide = true;
        }
        else
        {
            ShowObject();
        }
    }
}
