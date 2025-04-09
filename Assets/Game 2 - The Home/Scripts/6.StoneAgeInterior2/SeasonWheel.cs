using UnityEngine;
using System.Collections;

public class SeasonWheel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject beerVessel;
    [SerializeField] private GameObject arrowSign;
    
    [Header("Sprites")]
    [SerializeField] private Sprite beerMixed1Sprite; // Initial mixed state (current)
    [SerializeField] private Sprite beerMixed2Sprite; // After first season
    [SerializeField] private Sprite beerMixed3Sprite; // After second season
    [SerializeField] private Sprite beerMixed4Sprite; // After third season (final)
    
    [Header("Animation Settings")]
    [SerializeField] private float rotationDuration = 1.5f;
    [SerializeField] private float seasonRotationAngle = 90f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip wheelRotationSound; // Sound when wheel rotates
    [SerializeField] private AudioClip seasonCompleteSound; // Sound when a season completes
    [SerializeField] private float rotationSoundVolume = 0.7f; // Volume for rotation sound
    [SerializeField] private float completeSoundVolume = 0.8f; // Volume for season complete sound
    [SerializeField] private float pitchVariation = 0.1f; // Slight pitch variation for variety
    
    private int currentSeason = 0;
    private bool isAnimating = false;
    private SpriteRenderer wheelRenderer;
    private BoxCollider2D wheelCollider;
    private SpriteRenderer beerRenderer;
    private SpriteRenderer arrowRenderer;
    private AudioSource audioSource; // Main audio source component
    private AudioSource rotationAudioSource; // Dedicated audio source for rotation sound
    
    void Start()
    {
        // Get components
        wheelRenderer = GetComponent<SpriteRenderer>();
        wheelCollider = GetComponent<BoxCollider2D>();
        beerRenderer = beerVessel?.GetComponent<SpriteRenderer>();
        arrowRenderer = arrowSign?.GetComponent<SpriteRenderer>();
        
        // Set up main audio source for one-shot sounds
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        
        // Set up dedicated audio source for rotation sound
        rotationAudioSource = gameObject.AddComponent<AudioSource>();
        rotationAudioSource.playOnAwake = false;
        rotationAudioSource.loop = false;
        
        // Hide wheel and disable collider at start
        if (wheelRenderer != null)
        {
            wheelRenderer.enabled = false;
        }
        
        if (wheelCollider != null)
        {
            wheelCollider.enabled = false;
        }
        
        // Hide arrow sign
        if (arrowRenderer != null)
        {
            arrowRenderer.enabled = false;
            arrowSign.SetActive(false);
        }
        
        // Log warnings for missing references
        if (beerVessel == null || beerRenderer == null)
        {
            Debug.LogError("Beer vessel reference or renderer missing!");
        }
        
        if (arrowSign == null || arrowRenderer == null)
        {
            Debug.LogError("Arrow sign reference or renderer missing!");
        }
    }

    void Update()
    {
        // Check if the beer lid has been snapped
        if (BeerLid.IsLidSnapped && wheelRenderer != null && !wheelRenderer.enabled)
        {
            // Show wheel and enable collider when beer lid is snapped
            wheelRenderer.enabled = true;
            wheelCollider.enabled = true;
            Debug.Log("Season wheel revealed");
        }
    }
    
    private void OnMouseDown()
    {
        // Prevent multiple clicks during animation or after completion
        if (isAnimating || currentSeason >= 4) return;
        
        // Start season transition
        StartCoroutine(RotateWheelForSeason());
    }
    
    // Play wheel rotation sound at normal speed
    private void PlayWheelRotationSound()
    {
        if (wheelRotationSound != null && rotationAudioSource != null)
        {
            // Add slight pitch variation for more natural sound
            rotationAudioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            
            // Play sound with volume
            rotationAudioSource.clip = wheelRotationSound;
            rotationAudioSource.volume = rotationSoundVolume;
            rotationAudioSource.Play();
            
            Debug.Log("Playing wheel rotation sound");
        }
        else if (wheelRotationSound == null)
        {
            Debug.LogWarning("Wheel rotation sound clip is not assigned!");
        }
    }
    
    // Play season complete sound with slight pitch variation
    private void PlaySeasonCompleteSound()
    {
        if (seasonCompleteSound != null && audioSource != null)
        {
            // Add slight pitch variation for more natural sound
            audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            audioSource.PlayOneShot(seasonCompleteSound, completeSoundVolume);
            Debug.Log("Playing season complete sound");
        }
        else if (seasonCompleteSound == null)
        {
            Debug.LogWarning("Season complete sound clip is not assigned!");
        }
    }
    
    private IEnumerator RotateWheelForSeason()
    {
        isAnimating = true;
        
        // Play wheel rotation sound at the beginning of rotation
        PlayWheelRotationSound();
        
        // Store current rotation
        Quaternion startRotation = transform.rotation;
        
        // Calculate target rotation
        float targetZAngle = (currentSeason + 1) * seasonRotationAngle;
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetZAngle);
        
        // Rotate wheel smoothly
        float elapsedTime = 0;
        while (elapsedTime < rotationDuration)
        {
            elapsedTime += Time.deltaTime;
            float percentComplete = elapsedTime / rotationDuration;
            
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, percentComplete);
            
            yield return null;
        }
        
        // Ensure final rotation is exact
        transform.rotation = targetRotation;
        
        // Stop rotation sound when rotation finishes, regardless of audio length
        if (rotationAudioSource.isPlaying)
        {
            rotationAudioSource.Stop();
            Debug.Log("Stopped wheel rotation sound after animation completed");
        }
        
        // Increment season counter
        currentSeason++;
        
        // Update beer sprite based on current season
        UpdateBeerSprite();
        
        // Play season complete sound after rotation and sprite update
        PlaySeasonCompleteSound();
        
        // Check for final season completion
        if (currentSeason == 4)
        {
            // Enable arrow sign after the final season
            if (arrowSign != null && arrowRenderer != null)
            {
                arrowSign.SetActive(true);
                arrowRenderer.enabled = true;
                
                // Ensure arrow is on top with high sorting order
                arrowRenderer.sortingOrder = 10;
                Debug.Log("Final season complete - Arrow sign enabled");
            }
        }
        
        isAnimating = false;
    }
    
    private void UpdateBeerSprite()
    {
        if (beerRenderer == null) return;
        
        switch (currentSeason)
        {
            case 1:
                if (beerMixed2Sprite != null)
                {
                    beerRenderer.sprite = beerMixed2Sprite;
                    Debug.Log("Beer updated to Mixed2 (Spring)");
                }
                break;
                
            case 2:
                if (beerMixed3Sprite != null)
                {
                    beerRenderer.sprite = beerMixed3Sprite;
                    Debug.Log("Beer updated to Mixed3 (Summer)");
                }
                break;
                
            case 3:
                if (beerMixed4Sprite != null)
                {
                    beerRenderer.sprite = beerMixed4Sprite;
                    Debug.Log("Beer updated to Mixed4 (Fall)");
                }
                break;
                
            case 4:
                Debug.Log("Full beer fermentation cycle complete (Winter)");
                break;
        }
    }
    
    // Public method for BeerLid to check if fermentation is complete
    public bool IsFermentationComplete()
    {
        return currentSeason >= 4;
    }
    
    private void OnDisable()
    {
        // Stop any playing sounds when disabled
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        if (rotationAudioSource != null && rotationAudioSource.isPlaying)
        {
            rotationAudioSource.Stop();
        }
        
        Debug.Log("Stopped all audio on disable");
    }
}
