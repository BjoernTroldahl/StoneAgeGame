using System.Collections.Specialized;
using System.Security.Cryptography;
using UnityEngine;

public class Drag : MonoBehaviour
{
    public delegate void DragEndedDelegate(Drag draggableObject);
    public DragEndedDelegate dragEndedCallback;
    private bool dragging = false;
    private Vector3 offset;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("No SpriteRenderer found on draggable object!");
        }
    }

    void Update()
    {
        if (dragging)
        {
            transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition) + offset;
        }
    }

    private void OnMouseDown()
    {
        // Only allow dragging if the object is visible (alpha > 0)
        if (spriteRenderer != null && spriteRenderer.color.a > 0)
        {
            offset = transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
            dragging = true;
        }
    }

    private void OnMouseUp()
    {
        if (dragging)
        {
            dragging = false;
            if (dragEndedCallback != null)
            {
                dragEndedCallback(this);
            }
        }
    }
}
