using UnityEngine;
using System.Collections;
using System;

public class MilkDroplet : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float fallDistance = 2f;
    [SerializeField] private float fallDuration = 1f;
    [SerializeField] private float fallDistance2 = 2.5f;  // Second milk level distance
    [SerializeField] private float fallDuration2 = 1.2f;  // Second milk level duration
    [SerializeField] private float fallDistance3 = 3f;    // Third milk level distance
    [SerializeField] private float fallDuration3 = 1.5f;  // Third milk level duration
    
    private SpriteRenderer spriteRenderer;
    private Vector3 startPosition;
    private Vector3 endPosition;
    private int currentFallCount = 0;

    // Event to notify when animation completes
    public event Action OnDropletAnimationComplete;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("No SpriteRenderer found on milk droplet!");
            return;
        }

        spriteRenderer.enabled = false;
        startPosition = transform.position;
        // End position will be calculated per animation
    }

    public void TriggerDropletFall()
    {
        currentFallCount++;
        StartCoroutine(FallAnimation());
    }

    private IEnumerator FallAnimation()
    {
        // Calculate the right parameters based on fall count
        float currentDistance = fallDistance;
        float currentDuration = fallDuration;
        
        if (currentFallCount == 2)
        {
            currentDistance = fallDistance2;
            currentDuration = fallDuration2;
        }
        else if (currentFallCount >= 3)
        {
            currentDistance = fallDistance3;
            currentDuration = fallDuration3;
        }
        
        Vector3 currentEndPosition = startPosition + Vector3.down * currentDistance;
        
        // Reset position and enable sprite
        transform.position = startPosition;
        spriteRenderer.enabled = true;
        Debug.Log($"Droplet started falling (level {currentFallCount})");

        float elapsedTime = 0f;
        while (elapsedTime < currentDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / currentDuration;
            
            transform.position = Vector3.Lerp(startPosition, currentEndPosition, progress);
            yield return null;
        }

        spriteRenderer.enabled = false;
        Debug.Log($"Droplet finished falling after {currentDuration} seconds");
        
        // Notify listeners that animation is complete
        OnDropletAnimationComplete?.Invoke();
    }
}