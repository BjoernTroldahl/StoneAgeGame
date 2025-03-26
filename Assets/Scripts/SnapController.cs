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

    private Dictionary<Transform, bool> occupiedSnapPoints = new Dictionary<Transform, bool>();

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

        // Initialize snap points as unoccupied
        foreach (Transform snapPoint in snapPoints)
        {
            occupiedSnapPoints[snapPoint] = false;
        }
    }

    private void OnDragEnded(Drag draggableObject)
    {
        Transform closestSnapPoint = null;
        float shortestDistance = snapRange;

        for (int i = 0; i < snapPoints.Count; i++)
        {
            // Skip if snap point is occupied or hole isn't visible
            if (occupiedSnapPoints[snapPoints[i]] || 
                (i < holes.Count && !holeController.IsHoleVisible(holes[i])))
            {
                continue;
            }

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

        if (closestSnapPoint != null)
        {
            draggableObject.transform.localPosition = closestSnapPoint.localPosition;
            occupiedSnapPoints[closestSnapPoint] = true;
            draggableObject.LockInPlace();
            
            // Notify the hole that a seed has snapped to it
            int snapPointIndex = snapPoints.IndexOf(closestSnapPoint);
            if (snapPointIndex < holes.Count)
            {
                holeController.NotifySeedSnapped(holes[snapPointIndex]);
            }
        }
    }
}
