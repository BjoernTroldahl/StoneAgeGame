using UnityEngine;
using UnityEngine.SceneManagement;

public class DragMixingStick : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform beerVesselObject;
    [SerializeField] private Sprite mixedBeerSprite;
    
    [Header("Settings")]
    [SerializeField] private float snapDistance = 1f;
    [SerializeField] private float snapYOffset = 0.5f;
    [SerializeField] private float rotationSpeed = 200f;
    [SerializeField] private int requiredRotations = 2;
    [SerializeField] private float stirRadius = 0.5f;
    [SerializeField] private float returnMovementDuration = 1f;
    [SerializeField] private float moveToSnapDuration = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip stirringSound;
    [SerializeField] private float stirringVolume = 0.7f;
    [SerializeField] private bool loopStirringSound = true;

    // Static variables
    public static bool HoneyHasBeenAdded { get; set; } = false;
    public static bool IsMixingComplete { get; private set; } = false;
    private static bool sceneLoadListenerAdded = false;

    // Instance variables
    private bool isDragging = false;
    private Vector3 offset;
    private Camera mainCamera;
    private bool isStirring = false;
    private Vector3 startPosition;
    private float currentRotation = 0f;
    private int completedRotations = 0;
    private bool isReturning = false;
    private bool isMovingToSnap = false;
    private float stateTimer = 0f;
    private Quaternion startRotation;
    private Vector3 snapPosition;
    private Vector3 moveStartPosition;
    private bool isComplete = false;
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer beerRenderer;
    private BoxCollider2D boxCollider;
    private AudioSource audioSource;

    void Awake()
    {
        // Add scene load listener (only once)
        if (!sceneLoadListenerAdded)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            sceneLoadListenerAdded = true;
            Debug.Log("MixingStick: Scene load listener added");
        }
        
        // Reset instance variables for safety
        ResetInstanceVariables();
        
        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = loopStirringSound;
            Debug.Log("MixingStick: AudioSource component added");
        }
    }
    
    void OnDestroy()
    {
        // Clean up event subscription
        SceneManager.sceneLoaded -= OnSceneLoaded;
        sceneLoadListenerAdded = false;
        Debug.Log("MixingStick: Scene load listener removed");
    }
    
    // Reset when scene loads
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reset static variables
        HoneyHasBeenAdded = false;
        IsMixingComplete = false;
        
        // Reset instance variables
        ResetInstanceVariables();
        
        // Stop any playing audio
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        Debug.Log("MixingStick: Variables reset on scene load");
    }
    
    // Reset all instance variables to default values
    private void ResetInstanceVariables()
    {
        isDragging = false;
        offset = Vector3.zero;
        isStirring = false;
        currentRotation = 0f;
        completedRotations = 0;
        isReturning = false;
        isMovingToSnap = false;
        stateTimer = 0f;
        isComplete = false;
        
        // Reset transform if we have the starting position
        if (startPosition != Vector3.zero)
        {
            transform.position = startPosition;
            transform.rotation = startRotation;
        }
        
        // Update collider state if reference exists
        if (boxCollider != null)
        {
            boxCollider.enabled = false;
        }
        
        // Stop any playing audio
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    void Start()
    {
        mainCamera = Camera.main;
        startPosition = transform.position;
        startRotation = transform.rotation;
        spriteRenderer = GetComponent<SpriteRenderer>();
        beerRenderer = beerVesselObject?.GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();

        if (mainCamera == null || spriteRenderer == null || beerRenderer == null)
        {
            Debug.LogError("Missing required references!");
        }

        // Configure audio source
        if (audioSource != null)
        {
            audioSource.clip = stirringSound;
            audioSource.volume = stirringVolume;
            audioSource.loop = loopStirringSound;
        }
        else if (stirringSound != null)
        {
            Debug.LogWarning("AudioSource component missing, but stirring sound is assigned!");
        }

        // Disable box collider at the start of the game
        if (boxCollider != null)
        {
            boxCollider.enabled = false;
            Debug.Log("Mixing stick collider disabled at start");
        }
        else
        {
            Debug.LogError("BoxCollider2D missing on mixing stick!");
        }

        // Reset static variables when scene loads (extra safety)
        HoneyHasBeenAdded = false;
        IsMixingComplete = false;
    }

    void Update()
    {
        // Check if honey has been added and update collider state
        if (HoneyHasBeenAdded && boxCollider != null && !boxCollider.enabled && !isComplete)
        {
            boxCollider.enabled = true;
            Debug.Log("Honey added - Mixing stick collider enabled for interaction");
        }

        if (isMovingToSnap)
        {
            stateTimer += Time.deltaTime;
            float t = stateTimer / moveToSnapDuration;
            Vector3 targetPosition = snapPosition + new Vector3(0, snapYOffset, 0);
            transform.position = Vector3.Lerp(moveStartPosition, targetPosition, t);

            if (t >= 1f)
            {
                isMovingToSnap = false;
                isStirring = true;
                stateTimer = 0f;
                currentRotation = 0f;
                completedRotations = 0;
                
                // Start playing stirring sound when stirring begins
                if (audioSource != null && stirringSound != null)
                {
                    audioSource.Play();
                    Debug.Log("Stirring sound started");
                }
                
                Debug.Log("Reached snap point, starting stirring motion");
            }
        }
        else if (isStirring)
        {
            // Make sure audio is playing during stirring
            if (audioSource != null && stirringSound != null && !audioSource.isPlaying)
            {
                audioSource.Play();
            }
            
            currentRotation += rotationSpeed * Time.deltaTime;
            float rad = currentRotation * Mathf.Deg2Rad;
            
            Vector3 offset = new Vector3(
                Mathf.Cos(rad) * stirRadius,
                Mathf.Sin(rad) * stirRadius + snapYOffset,
                0
            );
            
            transform.position = beerVesselObject.position + offset;
            
            if (currentRotation >= 360f)
            {
                currentRotation = 0f;
                completedRotations++;
                Debug.Log($"Completed rotation {completedRotations}/{requiredRotations}");
                
                if (completedRotations >= requiredRotations)
                {
                    isStirring = false;
                    isReturning = true;
                    stateTimer = 0f;
                    moveStartPosition = transform.position;
                    
                    // Stop stirring sound when stirring is complete
                    if (audioSource != null && audioSource.isPlaying)
                    {
                        audioSource.Stop();
                        Debug.Log("Stirring sound stopped");
                    }

                    if (beerRenderer != null && mixedBeerSprite != null)
                    {
                        beerRenderer.sprite = mixedBeerSprite;
                        Debug.Log("Changed beer vessel sprite to mixed version");
                    }

                    Debug.Log("Stirring complete, returning to start");
                }
            }
        }
        else if (isReturning)
        {
            // Make sure audio is stopped if we're not stirring anymore
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            
            stateTimer += Time.deltaTime;
            float t = Mathf.Clamp01(stateTimer / returnMovementDuration); // Ensure t is between 0 and 1
            
            // Use SmoothStep for smoother animation
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector3.Lerp(moveStartPosition, startPosition, smoothT);
            transform.rotation = Quaternion.Lerp(Quaternion.Euler(0, 0, -30), startRotation, smoothT);

            if (t >= 1f)
            {
                isReturning = false;
                isComplete = true;
                IsMixingComplete = true;
                
                // Disable collider when mixing is complete
                if (boxCollider != null)
                {
                    boxCollider.enabled = false;
                    Debug.Log("Mixing complete - stick collider disabled");
                }
                
                // Ensure final position and rotation are exact
                transform.position = startPosition;
                transform.rotation = startRotation;
                Debug.Log("Return complete - Mixing stick locked");
            }
        }
        else if (isDragging)
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 newPosition = new Vector3(mousePosition.x + offset.x, mousePosition.y + offset.y, transform.position.z);
            transform.position = newPosition;

            float distanceToBeer = Vector2.Distance(transform.position, beerVesselObject.position);
            if (distanceToBeer < snapDistance)
            {
                isDragging = false;
                isMovingToSnap = true; // Skip initial rotation, go straight to moving
                stateTimer = 0f;
                moveStartPosition = transform.position;
                snapPosition = beerVesselObject.position;
                transform.rotation = Quaternion.Euler(0, 0, -30); // Instantly rotate to -30 degrees
                Debug.Log("Starting movement to snap point");
            }
        }
        else
        {
            // Safety check - make sure audio is not playing if we're not stirring
            if (audioSource != null && audioSource.isPlaying && !isStirring)
            {
                audioSource.Stop();
            }
        }
    }

    private void OnMouseDown()
    {
        if (!isComplete && HoneyHasBeenAdded && !isStirring && !isReturning && !isMovingToSnap)
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            offset = transform.position - mousePosition;
            isDragging = true;
            transform.rotation = Quaternion.Euler(0, 0, -30); // Instantly rotate when clicked
            Debug.Log("Started dragging mixing stick");
        }
    }

    private void OnMouseUp()
    {
        if (!isStirring && !isReturning && !isMovingToSnap)
        {
            isDragging = false;
            
            // If the player drops the stick away from the beer, return to start position
            if (isDragging)
            {
                transform.position = startPosition;
                transform.rotation = startRotation;
                Debug.Log("Mixing stick returned to start position");
            }
        }
    }

    // Expand the existing ResetStick method
    public void ResetStick()
    {
        isDragging = false;
        isStirring = false;
        isReturning = false;
        isMovingToSnap = false;
        isComplete = false;
        currentRotation = 0f;
        completedRotations = 0;
        stateTimer = 0f;
        
        transform.position = startPosition;
        transform.rotation = startRotation;
        
        // Update box collider state based on whether honey has been added
        if (boxCollider != null)
        {
            boxCollider.enabled = HoneyHasBeenAdded && !isComplete;
            Debug.Log($"Mixing stick reset - collider enabled: {boxCollider.enabled}");
        }
        
        // Stop any playing audio
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("Audio stopped on reset");
        }
    }
    
    // Add method to reset static variables
    public static void ResetStaticVariables()
    {
        HoneyHasBeenAdded = false;
        IsMixingComplete = false;
        Debug.Log("MixingStick: Static variables manually reset");
    }
}