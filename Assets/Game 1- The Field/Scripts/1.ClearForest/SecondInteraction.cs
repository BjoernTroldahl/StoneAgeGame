using UnityEngine;

public class SecondInteraction : MonoBehaviour
{
    [Header("Sprite Settings")]
    [SerializeField] private Sprite firstSprite;
    [SerializeField] private Sprite secondSprite;
    [SerializeField] private float switchInterval = 0.5f; 
    
    [Header("Target Object")]
    [SerializeField] private GameObject targetObject;
    [SerializeField] private bool hideImmediately = true;
    
    [Header("Activation Settings")]
    [SerializeField] private SpriteAlternator previousInteraction;
    [SerializeField] private float activationDelay = 0.5f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    // Private variables
    private SpriteRenderer spriteRenderer;
    private Collider2D objectCollider;
    private float timeSinceLastSwitch = 0f;
    private bool isFirstSprite = true;
    private bool shouldHide = false;
    private bool isActive = false;
    private float activationTimer = 0f;
    private bool startActivationTimer = false;
    
    void Start()
    {
        // Get components
        spriteRenderer = GetComponent<SpriteRenderer>();
        objectCollider = GetComponent<Collider2D>();
        
        // Validate required components
        if (spriteRenderer == null)
        {
            Debug.LogError($"[{gameObject.name}] No SpriteRenderer found on this object!");
            enabled = false;
            return;
        }
        
        if (objectCollider == null)
        {
            Debug.LogError($"[{gameObject.name}] No Collider2D found on this object! Overlap detection requires a collider.");
            enabled = false;
            return;
        }
        
        // Set collider to be a trigger
        if (!objectCollider.isTrigger)
        {
            objectCollider.isTrigger = true;
            DebugLog($"Set collider to trigger mode");
        }
        
        // Initialize with the first sprite but hidden
        if (firstSprite != null)
        {
            spriteRenderer.sprite = firstSprite;
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] First sprite not assigned!");
        }
        
        // Initially hide and disable
        spriteRenderer.enabled = false;
        objectCollider.enabled = false;
        
        DebugLog("Object initialized in hidden state");
        
        // Check for target object
        if (targetObject == null)
        {
            Debug.LogWarning($"[{gameObject.name}] No target object assigned! This object won't be hidden on overlap.");
        }
        else
        {
            // Validate target object has a collider
            Collider2D targetCollider = targetObject.GetComponent<Collider2D>();
            if (targetCollider == null)
            {
                Debug.LogWarning($"[{gameObject.name}] Target object has no Collider2D component! Overlap detection won't work.");
            }
            else
            {
                DebugLog($"Target object '{targetObject.name}' has a {(targetCollider.isTrigger ? "trigger" : "non-trigger")} collider");
            }
        }
        
        // Check for previous interaction
        if (previousInteraction == null)
        {
            Debug.LogWarning($"[{gameObject.name}] No previous interaction assigned! This object won't know when to appear.");
        }
    }

    void Update()
    {
        // Check for activation from previous interaction
        if (!isActive && previousInteraction != null)
        {
            // If the previous interaction's gameObject is not active, start our activation timer
            if (!previousInteraction.gameObject.activeSelf && !startActivationTimer)
            {
                startActivationTimer = true;
                activationTimer = 0f;
                DebugLog("Previous interaction hidden - starting activation timer");
            }
            
            // If we're waiting to activate
            if (startActivationTimer)
            {
                activationTimer += Time.deltaTime;
                
                // Once delay is reached, activate this object
                if (activationTimer >= activationDelay)
                {
                    isActive = true;
                    spriteRenderer.enabled = true;
                    objectCollider.enabled = true;
                    DebugLog("Activated with collider enabled");
                }
            }
        }
        
        // Only process the rest if we're active
        if (!isActive)
        {
            return;
        }
        
        // Check if the object should hide
        if (shouldHide)
        {
            if (hideImmediately)
            {
                // Hide immediately and disable collider
                objectCollider.enabled = false;
                spriteRenderer.enabled = false;
                DebugLog("Object hidden immediately");
                return;
            }
            else
            {
                // Wait for the next sprite switch before hiding
                timeSinceLastSwitch += Time.deltaTime;
                if (timeSinceLastSwitch >= switchInterval)
                {
                    objectCollider.enabled = false;
                    spriteRenderer.enabled = false;
                    DebugLog("Object hidden after delay");
                    return;
                }
            }
        }
        
        // Check for manual overlap in case trigger doesn't work
        if (targetObject != null && isActive && !shouldHide)
        {
            CheckManualOverlap();
        }
        
        // Sprite alternation logic
        timeSinceLastSwitch += Time.deltaTime;
        if (timeSinceLastSwitch >= switchInterval)
        {
            timeSinceLastSwitch = 0f;
            
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
            
            isFirstSprite = !isFirstSprite;
        }
    }
    
    // Check for overlap manually as a backup
    private void CheckManualOverlap()
    {
        if (targetObject == null || objectCollider == null) return;
        
        Collider2D targetCollider = targetObject.GetComponent<Collider2D>();
        if (targetCollider == null) return;
        
        // Check for overlap between colliders
        if (objectCollider.bounds.Intersects(targetCollider.bounds))
        {
            DebugLog("Manual overlap detected with target object!");
            objectCollider.enabled = false;
            shouldHide = true;
        }
    }
    
    // Enhanced trigger detection with debugging
    private void OnTriggerEnter2D(Collider2D other)
    {
        DebugLog($"Trigger entered by: {other.gameObject.name}");
        
        // Check if the colliding object is our target
        if (other.gameObject == targetObject)
        {
            DebugLog("Target object overlapping! Hiding this object.");
            objectCollider.enabled = false;
            shouldHide = true;
        }
    }
    
    // Handle continuous collision in case OnTriggerEnter fails
    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.gameObject == targetObject && !shouldHide)
        {
            DebugLog("Target object still overlapping (OnTriggerStay)! Hiding this object.");
            objectCollider.enabled = false;
            shouldHide = true;
        }
    }
    
    // Helper to log debug messages conditionally
    private void DebugLog(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[{gameObject.name} SecondInteraction] {message}");
        }
    }
    
    // Public methods
    public void HideObject()
    {
        DebugLog("HideObject called externally");
        objectCollider.enabled = false;
        shouldHide = true;
    }
    
    public void ShowObject()
    {
        DebugLog("ShowObject called externally");
        spriteRenderer.enabled = true;
        objectCollider.enabled = true;
        shouldHide = false;
        isActive = true;
    }
    
    public void ForceActivate()
    {
        DebugLog("ForceActivate called externally");
        isActive = true;
        spriteRenderer.enabled = true;
        objectCollider.enabled = true;
    }
}
