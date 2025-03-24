using System.Collections.Generic;
using UnityEngine;

public class SnapController : MonoBehaviour
{
    public List<Transform> snapPoints;
    public List<Drag> draggableObjects;
    public float snapRange = 0.5f;
    public ParticleSystem snapEffect;

    void Start()
    {
        foreach(Drag draggable in draggableObjects)
        {
            draggable.dragEndedCallback = OnDragEnded;
        }
    }

    private void OnDragEnded(Drag draggableObject)
    {
        float closestDistance = -1;
        Transform closestSnapPoint = null;

        foreach (Transform snapPoint in snapPoints){
            float currentDistance = Vector2.Distance(draggableObject.transform.localPosition, snapPoint.localPosition);
            if(closestSnapPoint == null || currentDistance < closestDistance){
                closestSnapPoint = snapPoint;
                closestDistance = currentDistance;
            }
        }

        if(closestSnapPoint != null && closestDistance <= snapRange){
            draggableObject.transform.localPosition = closestSnapPoint.localPosition;
            
            // Play the particle effect at the snap position
            if (snapEffect != null)
            {
                snapEffect.transform.position = closestSnapPoint.position;
                snapEffect.Play();
            }
        }	
    }
}
