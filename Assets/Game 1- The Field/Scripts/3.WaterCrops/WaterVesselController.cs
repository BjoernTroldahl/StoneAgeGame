using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isDragging = false;
    private bool isAnimating = false;

    public bool IsAnimating
    {
        get { return isAnimating; }
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

        yield return new WaitForSeconds(pouringDuration);

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
