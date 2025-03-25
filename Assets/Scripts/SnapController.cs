using System.Collections.Generic;
using UnityEngine;

public class SnapController : MonoBehaviour
{
    public List<Transform> snapPoints;
    public List<Drag> draggableObjects;
    public float snapRange = 0.5f;

    [Header("Hole References")]
    [SerializeField] private DigUpHoles holeController;
    [SerializeField] private List<GameObject> holes;  // Add holes in same order as snap points

    void Start()
    {
        if (holeController == null)
        {
            holeController = FindFirstObjectByType<DigUpHoles>();
            if (holeController == null)
            {
                Debug.LogError("DigUpHoles component not found!");
            }
        }

        foreach(Drag draggable in draggableObjects)
        {
            draggable.dragEndedCallback = OnDragEnded;
        }
    }

    private void OnDragEnded(Drag draggableObject)
    {
        Transform closestSnapPoint = null;
        float shortestDistance = snapRange;

        // Check each snap point
        for (int i = 0; i < snapPoints.Count; i++)
        {
            // Skip this snap point if its corresponding hole isn't visible
            if (i < holes.Count && !holeController.IsHoleVisible(holes[i]))
                continue;

            float currentDistance = Vector2.Distance(
                draggableObject.transform.localPosition, 
                snapPoints[i].localPosition
            );

            if (currentDistance <= shortestDistance)
            {
                shortestDistance = currentDistance;
                closestSnapPoint = snapPoints[i];
            }
        }

        // Snap the object if we found a valid snap point within range
        if (closestSnapPoint != null)
        {
            draggableObject.transform.localPosition = closestSnapPoint.localPosition;
        }
    }
}
