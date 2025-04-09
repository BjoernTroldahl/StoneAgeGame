using System.Collections.Generic;
using UnityEngine;

public class SnapController : MonoBehaviour
{
    public List<Transform> snapPoints;
    public List<Drag> draggableObjects;
    public float snapRange = 1.0f;

    [Header("Hole References")]
    [SerializeField] private DigUpHoles holeController;
    [SerializeField] private List<GameObject> holes;  // Add holes in same order as snap points

    [Header("Audio Settings")]
    [SerializeField] private AudioClip seedSnapSound;  // Sound to play when seed snaps
    [SerializeField] private float soundVolume = 0.7f; // Volume for sound effect
    [SerializeField] private float pitchVariation = 0.1f; // Random pitch variation to add variety

    private Dictionary<Transform, bool> occupiedSnapPoints = new Dictionary<Transform, bool>();
    private AudioSource audioSource;

    void Start()
    {
        // Set up audio source
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = soundVolume;

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
            // Snap the seed to the snap point
            draggableObject.transform.localPosition = closestSnapPoint.localPosition;
            occupiedSnapPoints[closestSnapPoint] = true;
            draggableObject.LockInPlace();
            
            // Play snap sound
            PlaySnapSound();
            
            // Notify the hole that a seed has snapped to it
            int snapPointIndex = snapPoints.IndexOf(closestSnapPoint);
            if (snapPointIndex < holes.Count)
            {
                holeController.NotifySeedSnapped(holes[snapPointIndex]);
            }
            
            Debug.Log($"Seed snapped to point {closestSnapPoint.name}");
        }
    }
    
    // Play the snap sound with slight pitch variation for variety
    private void PlaySnapSound()
    {
        if (seedSnapSound != null && audioSource != null)
        {
            // Add slight random pitch variation
            audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            
            // Play the sound
            audioSource.PlayOneShot(seedSnapSound, soundVolume);
            
            Debug.Log("Playing seed snap sound");
        }
    }
    
    // Public method for external access
    public void PlaySeedSound()
    {
        PlaySnapSound();
    }
}
