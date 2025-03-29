using UnityEngine;

public class DragGrain : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform snapPoint; // Circle 1 transform
    
    [Header("Settings")]
    [SerializeField] private float snapDistance = 1f;
    
    private bool isDragging = false;
    private Vector3 offset;
    private Camera mainCamera;
    private bool isSnapped = false;

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
        if (isDragging && !isSnapped)
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 newPosition = new Vector3(mousePosition.x + offset.x, mousePosition.y + offset.y, transform.position.z);
            
            // Check if close enough to snap point
            if (Vector2.Distance(newPosition, snapPoint.position) < snapDistance)
            {
                transform.position = snapPoint.position;
                isSnapped = true;
                isDragging = false;
                return;
            }
            
            transform.position = newPosition;
        }
    }

    private void OnMouseDown()
    {
        if (!isSnapped)
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            offset = transform.position - mousePosition;
            isDragging = true;
        }
    }

    private void OnMouseUp()
    {
        if (!isSnapped)
        {
            isDragging = false;
        }
    }
}
