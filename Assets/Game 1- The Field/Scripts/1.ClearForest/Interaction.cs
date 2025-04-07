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
    
    [Header("Sequence Settings")]
    [SerializeField] private bool isFirstObject = true; // Is this the first object in the sequence?
    [SerializeField] private MonoBehaviour previousInteractionScript; // Previous object's script (either SpriteAlternator or SecondInteraction)
    [SerializeField] private float activationDelay = 0.5f; // Delay before showing after previous is hidden
    
    // Private variables
    private SpriteRenderer spriteRenderer;
    private float timeSinceLastSwitch = 0f;
    private bool isFirstSprite = true;
    private bool shouldHide = false;
    private bool isActive = true; // Is this object currently active in the sequence?
    private float activationTimer = 0f;
    private bool startActivationTimer = false;
    private GameObject previousInteractionObject; // Reference to the previous object's GameObject
    
    void Awake()
    {
        // Get the sprite renderer component
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Save reference to previous interaction GameObject
        if (previousInteractionScript != null)
        {
            previousInteractionObject = previousInteractionScript.gameObject;
            Debug.Log($"[{gameObject.name}] Previous interaction object set to: {previousInteractionObject.name}");
        }
        
        // If not the first object, make sure it's fully hidden at the very start
        if (!isFirstObject)
        {
            isActive = false;
            spriteRenderer.enabled = false;
        }
    }
    
    void Start()
    {
        // Validate components
        if (spriteRenderer == null)
        {
            Debug.LogError($"[{gameObject.name}] No SpriteRenderer found on this object!");
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
            Debug.LogWarning($"[{gameObject.name}] First sprite not assigned!");
        }
        
        // Check if the target object exists
        if (targetObject == null)
        {
            Debug.LogWarning($"[{gameObject.name}] No target object assigned! This object won't be hidden.");
        }
        
        // If this is not the first object, validate and ensure it's hidden
        if (!isFirstObject)
        {
            if (previousInteractionScript == null)
            {
                Debug.LogError($"[{gameObject.name}] Not marked as first object but no previous interaction assigned!");
            }
            else 
            {
                // Check if the previous script is of a valid type
                bool validPrevious = previousInteractionScript is SpriteAlternator || 
                                    previousInteractionScript.GetType().Name == "SecondInteraction";
                
                if (!validPrevious)
                {
                    Debug.LogError($"[{gameObject.name}] Previous interaction must be either SpriteAlternator or SecondInteraction!");
                }
            }
            
            // Ensure this object starts hidden
            isActive = false;
            spriteRenderer.enabled = false;
            Debug.Log($"[{gameObject.name}] Starting as hidden (waiting for previous interaction)");
        }
        else
        {
            Debug.Log($"[{gameObject.name}] Starting as visible (first object)");
        }
    }

    void Update()
    {
        // If not the first object and not active yet, check if we should activate
        if (!isFirstObject && !isActive && previousInteractionObject != null)
        {
            // If the previous interaction's gameObject is not active, start our activation timer
            if (!previousInteractionObject.activeSelf && !startActivationTimer)
            {
                startActivationTimer = true;
                activationTimer = 0f;
                Debug.Log($"[{gameObject.name}] Previous interaction hidden - starting activation timer");
            }
            
            // If we're waiting to activate
            if (startActivationTimer)
            {
                activationTimer += Time.deltaTime;
                
                // Once delay is reached, activate this object
                if (activationTimer >= activationDelay)
                {
                    isActive = true;
                    spriteRenderer.enabled = true; // Show the sprite
                    startActivationTimer = false;
                    Debug.Log($"[{gameObject.name}] Activation timer complete - object activated");
                }
            }
            
            // Skip rest of update if not active
            if (!isActive)
            {
                return;
            }
        }

        // Check if the object should hide
        if (shouldHide)
        {
            if (hideImmediately)
            {
                // Hide immediately
                gameObject.SetActive(false);
                Debug.Log($"[{gameObject.name}] Hidden immediately after trigger");
                return;
            }
            else
            {
                // Wait for the next sprite switch before hiding
                timeSinceLastSwitch += Time.deltaTime;
                if (timeSinceLastSwitch >= switchInterval)
                {
                    gameObject.SetActive(false);
                    Debug.Log($"[{gameObject.name}] Hidden after sprite switch delay");
                    return;
                }
            }
        }
        
        // Only process sprite alternation if active
        if (isActive)
        {
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
                    Debug.Log($"[{gameObject.name}] Target object clicked! Hiding this object.");
                    shouldHide = true;
                }
            }
        }
    }
    
    // Public method to manually hide this object (can be called from other scripts)
    public void HideObject()
    {
        Debug.Log($"[{gameObject.name}] HideObject method called");
        shouldHide = true;
    }
    
    // Public method to manually show this object (can be called from other scripts)
    public void ShowObject()
    {
        Debug.Log($"[{gameObject.name}] ShowObject method called");
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        spriteRenderer.enabled = true;
        isActive = true;
        shouldHide = false;
    }
    
    // Public method to manually toggle visibility
    public void ToggleVisibility()
    {
        Debug.Log($"[{gameObject.name}] ToggleVisibility method called");
        if (gameObject.activeSelf && spriteRenderer.enabled)
        {
            shouldHide = true;
        }
        else
        {
            ShowObject();
        }
    }
    
    // Public method to check if this object is currently visible
    public bool IsVisible()
    {
        return gameObject.activeSelf && spriteRenderer.enabled && isActive;
    }
    
    // Public method to force activation (skipping the previous object dependency)
    public void ForceActivate()
    {
        Debug.Log($"[{gameObject.name}] ForceActivate method called");
        isActive = true;
        spriteRenderer.enabled = true;
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
    }
    
    // Reset for testing purposes
    public void Reset()
    {
        if (isFirstObject)
        {
            isActive = true;
            spriteRenderer.enabled = true;
        }
        else
        {
            isActive = false;
            spriteRenderer.enabled = false;
            startActivationTimer = false;
            activationTimer = 0f;
        }
        shouldHide = false;
        isFirstSprite = true;
        timeSinceLastSwitch = 0f;
        
        if (firstSprite != null)
        {
            spriteRenderer.sprite = firstSprite;
        }
        
        Debug.Log($"[{gameObject.name}] Reset to initial state");
    }
}
