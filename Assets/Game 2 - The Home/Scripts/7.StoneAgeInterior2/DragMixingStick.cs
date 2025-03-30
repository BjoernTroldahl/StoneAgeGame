using UnityEngine;

public class DragMixingStick : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform snapPoint;
    [SerializeField] private DragPorridge porridgeScript;
    [SerializeField] private Sprite stirredPorridgeSprite; // Add this field
    
    [Header("Settings")]
    [SerializeField] private float snapDistance = 1f;
    [SerializeField] private float snapYOffset = 0.5f; // Add this field
    [SerializeField] private float rotationSpeed = 200f;
    [SerializeField] private int requiredRotations = 2;
    [SerializeField] private float stirRadius = 0.5f;
    [SerializeField] private float initialRotationDuration = 1f;
    [SerializeField] private float finalRotationDuration = 1f;
    [SerializeField] private float returnMovementDuration = 1f;
    [SerializeField] private float moveToSnapDuration = 0.5f; // Add this field

    private bool isDragging = false;
    private Vector3 offset;
    private Camera mainCamera;
    private bool isStirring = false;
    private Vector3 startPosition;
    private float currentRotation = 0f;
    private int completedRotations = 0;
    
    // Add new state tracking
    private bool isInitialRotating = false;
    private bool isFinalRotating = false;
    private bool isReturning = false;
    private bool isMovingToSnap = false; // Add new state
    private float stateTimer = 0f;
    private Quaternion startRotation;
    private Vector3 snapPosition;
    private Vector3 moveStartPosition; // Add this field
    private bool isComplete = false; // Add this field
    private SpriteRenderer spriteRenderer; // Add this field

    // Add new state for moving back to trigger position
    private bool isMovingToTrigger = false;
    private Vector3 triggerPosition;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
            return;
        }

        startPosition = transform.position;
        startRotation = transform.rotation;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Debug.Log($"Initial sorting order: {spriteRenderer.sortingOrder}");
        }
        else
        {
            Debug.LogError("No SpriteRenderer found on MixingStick!");
        }
    }

    void Update()
    {
        // Only allow interaction if porridge is snapped and we're not currently stirring
        if (porridgeScript != null && !porridgeScript.IsSnapped())
        {
            return;
        }

        if (isMovingToSnap)
        {
            // Smoothly move to snap point
            stateTimer += Time.deltaTime;
            float t = stateTimer / moveToSnapDuration;
            // Add Y offset to snap position
            Vector3 targetPosition = snapPosition + new Vector3(0, snapYOffset, 0);
            transform.position = Vector3.Lerp(moveStartPosition, targetPosition, t);

            if (t >= 1f)
            {
                isMovingToSnap = false;
                isStirring = true;
                stateTimer = 0f;
                currentRotation = 0f;
                completedRotations = 0;
                Debug.Log("Reached snap point, starting stirring motion");
            }
        }
        else if (isInitialRotating)
        {
            // Rotate to 0 degrees first
            stateTimer += Time.deltaTime;
            float t = stateTimer / initialRotationDuration;
            transform.rotation = Quaternion.Lerp(startRotation, Quaternion.Euler(0, 0, 0), t);

            if (t >= 1f)
            {
                isInitialRotating = false;
                isMovingToSnap = true; // Move to snap point after rotation
                stateTimer = 0f;
                moveStartPosition = transform.position;
                Debug.Log("Initial rotation complete, moving to snap point");
            }
        }
        else if (isStirring)
        {
            // Calculate new position around snap point
            currentRotation += rotationSpeed * Time.deltaTime;
            float rad = currentRotation * Mathf.Deg2Rad;
            
            Vector3 offset = new Vector3(
                Mathf.Cos(rad) * stirRadius,
                Mathf.Sin(rad) * stirRadius + snapYOffset, // Add offset to stirring motion
                0
            );
            
            transform.position = snapPoint.position + offset;
            
            // Track completed rotations
            if (currentRotation >= 360f)
            {
                currentRotation = 0f;
                completedRotations++;
                Debug.Log($"Completed rotation {completedRotations}/{requiredRotations}");
                
                if (completedRotations >= requiredRotations)
                {
                    isStirring = false;
                    isMovingToTrigger = true; // First move back to trigger position
                    stateTimer = 0f;
                    moveStartPosition = transform.position;

                    // Lock Circle 1 by disabling porridge script
                    if (porridgeScript != null)
                    {
                        porridgeScript.LockSnapping();
                    }

                    // Change porridge sprite
                    if (porridgeScript != null)
                    {
                        SpriteRenderer porridgeRenderer = porridgeScript.GetPorridgeRenderer();
                        if (porridgeRenderer != null && stirredPorridgeSprite != null)
                        {
                            porridgeRenderer.sprite = stirredPorridgeSprite;
                            Debug.Log($"Changed porridge sprite to {stirredPorridgeSprite.name}");
                        }
                    }

                    Debug.Log("Stirring complete, moving back to trigger position");
                }
            }
        }
        else if (isMovingToTrigger)
        {
            stateTimer += Time.deltaTime;
            float t = stateTimer / moveToSnapDuration;
            transform.position = Vector3.Lerp(moveStartPosition, triggerPosition, t);

            if (t >= 1f)
            {
                isMovingToTrigger = false;
                isFinalRotating = true;
                stateTimer = 0f;

                // Change Order in Layer here instead of during final rotation
                if (spriteRenderer != null)
                {
                    Debug.Log($"Current sorting order before change: {spriteRenderer.sortingOrder}");
                    spriteRenderer.sortingOrder = 6;
                    Debug.Log($"Changed sorting order to: {spriteRenderer.sortingOrder}");
                }

                Debug.Log("Reached trigger position, starting rotation back to default");
            }
        }
        else if (isFinalRotating)
        {
            // Rotate back to starting rotation
            stateTimer += Time.deltaTime;
            float t = stateTimer / finalRotationDuration;
            transform.rotation = Quaternion.Lerp(Quaternion.Euler(0, 0, 0), startRotation, t);

            if (t >= 1f)
            {
                isFinalRotating = false;
                isReturning = true;
                stateTimer = 0f;
                moveStartPosition = transform.position;
                Debug.Log("Final rotation complete, returning to start");
            }
        }
        else if (isReturning)
        {
            // Return to start position from trigger position
            stateTimer += Time.deltaTime;
            float t = stateTimer / returnMovementDuration;
            transform.position = Vector3.Lerp(moveStartPosition, startPosition, t); // Use moveStartPosition instead of snapPosition

            if (t >= 1f)
            {
                isReturning = false;
                transform.rotation = startRotation;
                isComplete = true; // Lock the mixing stick
                Debug.Log("Return complete - Mixing stick locked");
            }
        }
        else if (isDragging)
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 newPosition = new Vector3(mousePosition.x + offset.x, mousePosition.y + offset.y, transform.position.z);
            transform.position = newPosition;

            // Check if close enough to snap point
            float distanceToSnap = Vector2.Distance(transform.position, snapPoint.position);
            if (distanceToSnap < snapDistance)
            {
                isDragging = false;
                isInitialRotating = true; // Start with rotation instead of moving
                stateTimer = 0f;
                triggerPosition = transform.position; // Store the position where snapping triggered
                moveStartPosition = transform.position;
                snapPosition = snapPoint.position;
                startRotation = transform.rotation;
                Debug.Log("Starting initial rotation before snap movement");
            }
        }
    }

    private void OnMouseDown()
    {
        // Only allow dragging if not complete and other conditions are met
        if (!isComplete && porridgeScript != null && porridgeScript.IsSnapped() && !isStirring)
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            offset = transform.position - mousePosition;
            isDragging = true;
            Debug.Log("Started dragging mixing stick");
        }
    }

    private void OnMouseUp()
    {
        if (!isStirring)
        {
            isDragging = false;
        }
    }

    private void ReturnToStart()
    {
        transform.position = startPosition;
        Debug.Log("Mixing stick returned to start position");
    }
}
