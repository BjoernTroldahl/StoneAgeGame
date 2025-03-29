using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class WaterVesselController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float snapRange = 1.0f;
    [SerializeField] private float moveSpeed = 2.0f;
    [SerializeField] private float rotationAngle = 45f;
    [SerializeField] private float rotationDuration = 0.5f;
    [SerializeField] private float pouringDuration = 2.0f;
    [SerializeField] private float snappingOffset = 0.5f; // Add this line for offset distance

    [Header("References")]
    [SerializeField] private List<Transform> seedlingPoints;
    [SerializeField] private List<SpriteRenderer> seedlingRenderers; // Add this line
    [SerializeField] private GameObject waterDropEffect;
    [SerializeField] private float waterDropOffset = 0.5f; // Add this for water position adjustment
    [SerializeField] private Sprite grownWheatSprite; // Add this for the wheat-03-green sprite

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isDragging = false;
    private bool isAnimating = false;
    private HashSet<Transform> wateredSeedlings = new HashSet<Transform>();

    public bool IsAnimating
    {
        get { return isAnimating; }
    }

    void Start()
    {
        if (waterDropEffect != null)
        {
            waterDropEffect.SetActive(false); // Ensure water effect is hidden at start
        }
    }

    private void OnMouseDown()
    {
        if (!isAnimating)
        {
            isDragging = true;
            originalPosition = transform.position;
        }
    }

    private void OnMouseDrag()
    {
        if (isDragging)
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            transform.position = new Vector3(mousePosition.x, mousePosition.y, transform.position.z);
        }
    }

    private void OnMouseUp()
    {
        if (isDragging)
        {
            isDragging = false;
            Transform closestSeedling = FindClosestSeedling();

            if (closestSeedling != null)
            {
                StartCoroutine(WateringSequence(closestSeedling));
            }
        }
    }

    private Transform FindClosestSeedling()
    {
        Transform closest = null;
        float shortestDistance = snapRange;

        foreach (Transform seedling in seedlingPoints)
        {
            // Skip if this seedling is already watered
            if (wateredSeedlings.Contains(seedling))
            {
                continue;
            }

            float distance = Vector2.Distance(transform.position, seedling.position);
            if (distance <= shortestDistance)
            {
                shortestDistance = distance;
                closest = seedling;
            }
        }

        return closest;
    }

    private IEnumerator WateringSequence(Transform seedling)
    {
        isAnimating = true;
        Vector3 startPosition = transform.position;
        originalRotation = transform.rotation;
        float elapsedTime = 0;

        // Determine direction based on relative position
        float direction = transform.position.x > seedling.position.x ? 1f : -1f;
        
        // Set water effect direction and position
        if (waterDropEffect != null)
        {
            SpriteRenderer waterSprite = waterDropEffect.GetComponent<SpriteRenderer>();
            if (waterSprite != null)
            {
                // Flip sprite if pouring from right side
                waterSprite.flipX = (direction > 0);
                
                // Adjust water drop position based on direction
                Vector3 waterPos = waterDropEffect.transform.localPosition;
                waterPos.x = direction > 0 ? -waterDropOffset : waterDropOffset;
                waterDropEffect.transform.localPosition = waterPos;
            }
        }

        // Calculate offset position
        Vector3 offsetPosition = seedling.position + new Vector3(snappingOffset * direction, 0, 0);

        // Move to offset position near seedling
        while (elapsedTime < 1)
        {
            elapsedTime += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(startPosition, offsetPosition, elapsedTime);
            yield return null;
        }

        // Rotate for pouring
        elapsedTime = 0;
        while (elapsedTime < rotationDuration)
        {
            elapsedTime += Time.deltaTime;
            float currentAngle = Mathf.Lerp(0, rotationAngle * direction, elapsedTime / rotationDuration);
            transform.rotation = originalRotation * Quaternion.Euler(0, 0, currentAngle);
            yield return null;
        }

        // Show water effect during pouring
        if (waterDropEffect != null)
        {
            waterDropEffect.SetActive(true);
        }

        yield return new WaitForSeconds(pouringDuration);

        // Hide water effect and grow seedling
        if (waterDropEffect != null)
        {
            waterDropEffect.SetActive(false);
        }

        // Change seedling sprite and track it
        if (!wateredSeedlings.Contains(seedling))
        {
            // Find corresponding sprite renderer
            int seedlingIndex = seedlingPoints.IndexOf(seedling);
            if (seedlingIndex >= 0 && seedlingIndex < seedlingRenderers.Count)
            {
                SpriteRenderer seedlingRenderer = seedlingRenderers[seedlingIndex];
                if (seedlingRenderer != null && grownWheatSprite != null)
                {
                    seedlingRenderer.sprite = grownWheatSprite;
                    wateredSeedlings.Add(seedling);

                    // Check if all seedlings are watered
                    if (wateredSeedlings.Count == seedlingPoints.Count)
                    {
                        Debug.Log("CONGRATULATIONS, YOU WON THE GAME");
                        SceneManager.LoadScene(4); // Load the next scene
                    }
                }
            }
        }

        // Rotate back
        elapsedTime = 0;
        while (elapsedTime < rotationDuration)
        {
            elapsedTime += Time.deltaTime;
            float currentAngle = Mathf.Lerp(rotationAngle * direction, 0, elapsedTime / rotationDuration);
            transform.rotation = originalRotation * Quaternion.Euler(0, 0, currentAngle);
            yield return null;
        }

        // Move back to original position
        elapsedTime = 0;
        while (elapsedTime < 1)
        {
            elapsedTime += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(offsetPosition, startPosition, elapsedTime);
            yield return null;
        }

        transform.rotation = originalRotation;
        isAnimating = false;
    }
}
