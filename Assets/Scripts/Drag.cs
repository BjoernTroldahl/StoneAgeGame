using System.Collections.Specialized;
using System.Security.Cryptography;
using UnityEngine;

public class Drag : MonoBehaviour
{
    public delegate void DragEndedDelegate(Drag draggableObject);
    public DragEndedDelegate dragEndedCallback;
    private bool dragging = false;
    private Vector3 offset;

    // Update is called once per frame
    void Update()
    {
        if (dragging)
        {
            // Move object, taking into account original offset
            transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition) + offset;
        }
        
    }

    private void OnMouseDown()
    {
        offset = transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
        dragging = true;
    }

    private void OnMouseUp()
    {
        dragging = false;
        dragEndedCallback(this);
    }
}
