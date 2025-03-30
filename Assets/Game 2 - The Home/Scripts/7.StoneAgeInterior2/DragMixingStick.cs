using UnityEngine;

public class DragMixingStick : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform snapPoint;
    [SerializeField] private DragPorridge porridgeScript;
    
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
                isInitialRotating = true;
                stateTimer = 0f;
                Debug.Log("Reached snap point, starting initial rotation");
            }
        }
        else if (isInitialRotating)
        {
            // Rotate to 0 degrees
            stateTimer += Time.deltaTime;
            float t = stateTimer / initialRotationDuration;
            transform.rotation = Quaternion.Lerp(startRotation, Quaternion.Euler(0, 0, 0), t);

            if (t >= 1f)
            {
                isInitialRotating = false;
                isStirring = true;
                stateTimer = 0f;
                Debug.Log("Initial rotation complete, starting stir");
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
                    isFinalRotating = true;
                    stateTimer = 0f;
                    snapPosition = transform.position;
                    Debug.Log("Stirring complete, starting final rotation");
                }
            }
        }
        else if (isFinalRotating)
        {
            // Rotate to -90 degrees
            stateTimer += Time.deltaTime;
            float t = stateTimer / finalRotationDuration;
            transform.rotation = Quaternion.Lerp(Quaternion.Euler(0, 0, 0), Quaternion.Euler(0, 0, -90), t);

            if (t >= 1f)
            {
                isFinalRotating = false;
                isReturning = true;
                stateTimer = 0f;

                // Change and verify Order in Layer here instead
                if (spriteRenderer != null)
                {
                    Debug.Log($"Current sorting order before change: {spriteRenderer.sortingOrder}");
                    spriteRenderer.sortingOrder = 6;
                    Debug.Log($"Changed sorting order to: {spriteRenderer.sortingOrder}");
                }
                else
                {
                    Debug.LogError("SpriteRenderer is null when trying to change sorting order!");
                }

                Debug.Log("Final rotation complete, returning to start");
            }
        }
        else if (isReturning)
        {
            // Return to start position
            stateTimer += Time.deltaTime;
            float t = stateTimer / returnMovementDuration;
            transform.position = Vector3.Lerp(snapPosition, startPosition, t);

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
                isMovingToSnap = true;
                stateTimer = 0f;
                moveStartPosition = transform.position;
                snapPosition = snapPoint.position;
                startRotation = transform.rotation;
                Debug.Log($"Starting movement to snap point with Y offset: {snapYOffset}");
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
