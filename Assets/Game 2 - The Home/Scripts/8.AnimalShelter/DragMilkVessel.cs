using UnityEngine;

public class DragMilkVessel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform circle1;
    [SerializeField] private Sprite milkedVesselSprite;  // Add new sprite reference

    [Header("Settings")]
    [SerializeField] private float snapDistance = 1f;

    private bool isDragging = false;
    private bool isSnapped = false;
    private bool isMilked = false;  // Add new state variable
    private Vector3 offset;
    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;  // Add sprite renderer reference

    void Start()
    {
        mainCamera = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
            return;
        }
        if (spriteRenderer == null)
        {
            Debug.LogError("Sprite Renderer not found!");
            return;
        }
    }

    void Update()
    {
        if (isDragging && !isSnapped)
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            transform.position = new Vector3(mousePosition.x + offset.x, 
                                          mousePosition.y + offset.y, 
                                          transform.position.z);

            // Check for snapping distance
            if (Vector2.Distance(transform.position, circle1.position) < snapDistance)
            {
                SnapToCircle();
            }
        }
    }

    private void OnMouseDown()
    {
        if (!isMilked)
        {
            if (spriteRenderer != null && milkedVesselSprite != null)
            {
                spriteRenderer.sprite = milkedVesselSprite;
                isMilked = true;
                Debug.Log("Vessel milked - Ready for transport");
            }
            return;
        }

        if (!isSnapped && isMilked)
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            offset = transform.position - mousePosition;
            isDragging = true;
        }
    }

    private void OnMouseUp()
    {
        isDragging = false;
    }

    private void SnapToCircle()
    {
        transform.position = circle1.position;
        isSnapped = true;
        isDragging = false;

        // Change sprite when snapped
        if (spriteRenderer != null && milkedVesselSprite != null)
        {
            spriteRenderer.sprite = milkedVesselSprite;
            Debug.Log("Changed to milked vessel sprite");
        }

        Debug.Log("Milk vessel snapped to circle");
    }
}
