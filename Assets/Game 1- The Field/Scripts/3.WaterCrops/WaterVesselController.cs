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
    [SerializeField] private float snappingOffset = 0.5f;

    [Header("References")]
    [SerializeField] private List<Transform> seedlingPoints;
    [SerializeField] private List<SpriteRenderer> seedlingRenderers;
    [SerializeField] private GameObject waterParticleSystem;
    [SerializeField] private Sprite grownWheatSprite;

    [Header("Water Particle Settings")]
    [SerializeField] private float waterXOffset = 0.5f;   // Horizontal offset for water particles
    [SerializeField] private float waterYOffset = -0.2f;  // Vertical offset for water particles (negative = down)
    [SerializeField] private float waterRotationLeft = 270f;  // Rotation when watering from right to left (in degrees)
    [SerializeField] private float waterVelocityModifier = 1f; // Multiplier for particle velocity

    [Header("Audio Settings")]
    [SerializeField] private AudioClip waterPouringSound; // Sound of water pouring
    [SerializeField] private float waterSoundVolume = 0.7f; // Volume for water sound
    [SerializeField] private float waterFadeInTime = 0.25f; // Time to fade in the water sound
    [SerializeField] private float waterFadeOutTime = 0.5f; // Time to fade out the water sound

    [Header("Grown Wheat Settings")]
    [SerializeField] private float grownWheatYOffset = 0.0f; // Vertical offset for grown wheat sprites
    [SerializeField] private float grownWheatXOffset = 0.0f; // Optional horizontal offset for grown wheat sprites

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isDragging = false;
    private bool isAnimating = false;
    private HashSet<Transform> wateredSeedlings = new HashSet<Transform>();
    private ParticleSystem waterParticles;
    private Dictionary<Transform, Vector3> seedlingOriginalPositions = new Dictionary<Transform, Vector3>();
    private AudioSource audioSource; // Audio source for water pouring sound

    public bool IsAnimating
    {
        get { return isAnimating; }
    }

    void Start()
    {
        // Store original positions of seedlings for reference
        foreach (Transform seedling in seedlingPoints)
        {
            seedlingOriginalPositions[seedling] = seedling.position;
        }

        // Get the ParticleSystem component from the GameObject
        if (waterParticleSystem != null)
        {
            waterParticles = waterParticleSystem.GetComponent<ParticleSystem>();
            if (waterParticles != null)
            {
                // Make sure it's stopped at start
                waterParticles.Stop();
                waterParticleSystem.SetActive(false);
                Debug.Log("Water particle system initialized and stopped");
            }
            else
            {
                Debug.LogError("Water particle system GameObject doesn't have a ParticleSystem component!");
            }
        }
        else
        {
            Debug.LogWarning("Water particle system not assigned!");
        }
        
        // Set up audio source
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = waterPouringSound;
        audioSource.loop = true;
        audioSource.volume = 0; // Start silent
        audioSource.playOnAwake = false;
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

        // Always position on the right side of the seedling
        
        // Set up water particle system
        if (waterParticleSystem != null && waterParticles != null)
        {
            // We'll position it during the pouring phase
            waterParticleSystem.SetActive(false);
        }

        // Calculate offset position - always to the right of seedling
        Vector3 offsetPosition = seedling.position + new Vector3(snappingOffset, 0, 0);

        // Move to offset position on the right side of seedling
        while (elapsedTime < 1)
        {
            elapsedTime += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(startPosition, offsetPosition, elapsedTime);
            yield return null;
        }

        // Rotate for pouring - always pour from right to left
        elapsedTime = 0;
        while (elapsedTime < rotationDuration)
        {
            elapsedTime += Time.deltaTime;
            // Always rotate the vessel counterclockwise (negative angle)
            float currentAngle = Mathf.Lerp(0, -rotationAngle, elapsedTime / rotationDuration);
            transform.rotation = originalRotation * Quaternion.Euler(0, 0, currentAngle);
            yield return null;
        }

        // Activate and play water particle system during pouring
        if (waterParticleSystem != null && waterParticles != null)
        {
            // Calculate water position
            Vector3 emissionPoint = transform.position;
            // Apply horizontal offset - always on the left side of the vessel
            emissionPoint.x -= waterXOffset;
            // Apply vertical offset
            emissionPoint.y += waterYOffset;
            waterParticleSystem.transform.position = emissionPoint;
            
            // Always use the left rotation (watering from right to left)
            waterParticleSystem.transform.rotation = Quaternion.Euler(0, 0, waterRotationLeft);
            
            // Modify velocity if needed
            var mainModule = waterParticles.main;
            // Store original start speed
            float originalStartSpeed = mainModule.startSpeed.constant;
            mainModule.startSpeed = originalStartSpeed * waterVelocityModifier;
            
            // Activate and play
            waterParticleSystem.SetActive(true);
            waterParticles.Play();
            Debug.Log($"Started water particle effect with rotation {waterRotationLeft}°");
            
            // Start playing water sound with fade-in
            StartCoroutine(FadeInWaterSound());
        }

        yield return new WaitForSeconds(pouringDuration);

        // Stop water particle system
        if (waterParticleSystem != null && waterParticles != null)
        {
            waterParticles.Stop();
            waterParticleSystem.SetActive(false);
            Debug.Log("Stopped water particle effect");
            
            // Fade out water sound
            StartCoroutine(FadeOutWaterSound());
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
                    // Change the sprite
                    seedlingRenderer.sprite = grownWheatSprite;
                    
                    // Add to watered seedlings
                    wateredSeedlings.Add(seedling);
                    
                    // Apply position offset to the sprite renderer's transform
                    if (grownWheatYOffset != 0 || grownWheatXOffset != 0)
                    {
                        // Option 1: If the SpriteRenderer is on the same GameObject as the Transform
                        // Adjust the sprite's local offset using the renderer's transform
                        Transform spriteTransform = seedlingRenderer.transform;
                        
                        // Store original local position
                        Vector3 originalLocalPos = spriteTransform.localPosition;
                        
                        // Apply the offsets to the sprite's local position
                        Vector3 newLocalPos = new Vector3(
                            originalLocalPos.x + grownWheatXOffset,
                            originalLocalPos.y + grownWheatYOffset,
                            originalLocalPos.z
                        );
                        
                        // Set the new local position
                        spriteTransform.localPosition = newLocalPos;
                        
                        Debug.Log($"Adjusted sprite position for seedling {seedlingIndex}: Local offset applied (X:{grownWheatXOffset}, Y:{grownWheatYOffset})");
                    }
                    
                    // Check if all seedlings are watered
                    if (wateredSeedlings.Count == seedlingPoints.Count)
                    {
                        Debug.Log("CONGRATULATIONS, YOU WON THE GAME");
                        SceneManager.LoadScene("4.HarvestWheat"); // Load the next scene
                    }
                }
            }
        }

        // Rotate back - always rotate clockwise back to starting position
        elapsedTime = 0;
        while (elapsedTime < rotationDuration)
        {
            elapsedTime += Time.deltaTime;
            float currentAngle = Mathf.Lerp(-rotationAngle, 0, elapsedTime / rotationDuration);
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
    
    // Fade in water pouring sound
    private IEnumerator FadeInWaterSound()
    {
        if (waterPouringSound != null && audioSource != null)
        {
            // Start playing at zero volume
            audioSource.volume = 0f;
            audioSource.Play();
            
            float timeElapsed = 0f;
            
            // Gradually increase volume during fadeInTime
            while (timeElapsed < waterFadeInTime)
            {
                timeElapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(timeElapsed / waterFadeInTime);
                audioSource.volume = Mathf.Lerp(0f, waterSoundVolume, normalizedTime);
                yield return null;
            }
            
            // Ensure we reach target volume
            audioSource.volume = waterSoundVolume;
            Debug.Log("Water pouring sound faded in");
        }
    }
    
    // Fade out water pouring sound
    private IEnumerator FadeOutWaterSound()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            float startVolume = audioSource.volume;
            float timeElapsed = 0f;
            
            // Gradually decrease volume during fadeOutTime
            while (timeElapsed < waterFadeOutTime)
            {
                timeElapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(timeElapsed / waterFadeOutTime);
                audioSource.volume = Mathf.Lerp(startVolume, 0f, normalizedTime);
                yield return null;
            }
            
            // Ensure we reach zero volume and stop playing
            audioSource.volume = 0f;
            audioSource.Stop();
            Debug.Log("Water pouring sound faded out");
        }
    }
    
    private void OnDestroy()
    {
        // Clean up audio when destroyed
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}
