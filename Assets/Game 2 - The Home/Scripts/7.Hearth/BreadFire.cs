using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement; // Added for scene management

public class BreadFire : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite[] fireSprites; // Array to hold all sprites to cycle through
    
    [Header("Timing")]
    [SerializeField] private float cycleTime = 0.3f; // Time between sprite changes in seconds
    
    [Header("Audio")]
    [SerializeField] private AudioClip fireAmbientSound; // Looping fire/crackling sound
    [SerializeField] private float fireVolume = 0.6f; // Volume of the fire sound
    [SerializeField] private float fadeInDuration = 1.0f; // Time to fade in the fire sound
    [SerializeField] private float fadeOutDuration = 1.0f; // Time to fade out the fire sound
    
    [Header("Scene Settings")]
    [SerializeField] private int targetSceneIndex = 8; // The scene where sound should play (scene 8)
    
    private SpriteRenderer spriteRenderer;
    private int currentSpriteIndex = 0;
    private float timeUntilNextChange;
    private AudioSource audioSource;
    private bool isFadingIn = false;
    private bool isFadingOut = false;
    private float targetVolume;

    private void Awake()
    {
        // Get the SpriteRenderer component
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer == null)
        {
            Debug.LogError("No SpriteRenderer found on this GameObject!");
            enabled = false; // Disable this script if no SpriteRenderer is found
            return;
        }
        
        // Validate sprite array
        if (fireSprites == null || fireSprites.Length == 0)
        {
            Debug.LogWarning("No sprites assigned to cycle through!");
            enabled = false; // Disable this script if no sprites are assigned
            return;
        }
        
        // Set up audio source for fire sound
        SetupAudioSource();
        
        // Initialize with first sprite
        if (fireSprites.Length > 0 && fireSprites[0] != null)
        {
            spriteRenderer.sprite = fireSprites[0];
        }
        
        // Initialize timer
        timeUntilNextChange = cycleTime;
        
        // Register for scene change events
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events when object is destroyed
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        
        // Make sure sound stops when object is destroyed
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Check if we're in the target scene
        if (scene.buildIndex == targetSceneIndex)
        {
            // We're in scene 8, start the fire sound
            StartFireSound();
            Debug.Log($"Scene {targetSceneIndex} loaded - Starting fire sound");
        }
        else
        {
            // We're in a different scene, stop the fire sound immediately
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
                isFadingIn = false;
                isFadingOut = false;
                Debug.Log($"Scene {scene.buildIndex} loaded - Stopping fire sound immediately");
            }
        }
    }
    
    private void OnSceneUnloaded(Scene scene)
    {
        // If we're leaving the target scene, stop the sound immediately
        if (scene.buildIndex == targetSceneIndex)
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
                isFadingIn = false;
                isFadingOut = false;
                Debug.Log($"Scene {targetSceneIndex} unloaded - Stopping fire sound immediately");
            }
        }
    }
    
    private void SetupAudioSource()
    {
        // Create audio source component if it doesn't exist
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Configure audio source for looping ambient sound
        audioSource.clip = fireAmbientSound;
        audioSource.loop = true;
        audioSource.volume = 0; // Start silent and fade in
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound
        
        targetVolume = fireVolume;
    }
    
    private void Start()
    {
        // Check if we're already in the target scene when this object starts
        if (SceneManager.GetActiveScene().buildIndex == targetSceneIndex)
        {
            StartFireSound();
            Debug.Log($"Already in scene {targetSceneIndex} at start - Starting fire sound");
        }
        else
        {
            // We're not in the target scene, make sure sound is stopped
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }
    
    // We no longer need these since we're controlling sound based on scene events
    // private void OnEnable() { ... }
    // private void OnDisable() { ... }

    private void Update()
    {
        // Only process sprite cycling and sound if we're in the target scene
        if (SceneManager.GetActiveScene().buildIndex == targetSceneIndex)
        {
            // Update timer for sprite cycling
            timeUntilNextChange -= Time.deltaTime;
            
            // Check if it's time to change sprites
            if (timeUntilNextChange <= 0)
            {
                // Reset timer
                timeUntilNextChange = cycleTime;
                
                // Change to next sprite
                CycleToNextSprite();
            }
            
            // Handle sound fading
            HandleSoundFading();
        }
    }
    
    private void HandleSoundFading()
    {
        // Handle fade in
        if (isFadingIn)
        {
            float currentVolume = audioSource.volume;
            currentVolume += Time.deltaTime / fadeInDuration * targetVolume;
            
            if (currentVolume >= targetVolume)
            {
                currentVolume = targetVolume;
                isFadingIn = false;
            }
            
            audioSource.volume = currentVolume;
        }
        
        // Handle fade out
        if (isFadingOut)
        {
            float currentVolume = audioSource.volume;
            currentVolume -= Time.deltaTime / fadeOutDuration * targetVolume;
            
            if (currentVolume <= 0)
            {
                currentVolume = 0;
                isFadingOut = false;
                audioSource.Stop();
            }
            
            audioSource.volume = currentVolume;
        }
    }

    private void CycleToNextSprite()
    {
        // Move to next sprite index, wrap around if needed
        currentSpriteIndex = (currentSpriteIndex + 1) % fireSprites.Length;
        
        // Set the new sprite
        if (fireSprites[currentSpriteIndex] != null)
        {
            spriteRenderer.sprite = fireSprites[currentSpriteIndex];
        }
        else
        {
            Debug.LogWarning($"Sprite at index {currentSpriteIndex} is null!");
        }
    }
    
    public void StartFireSound()
    {
        // Only start sound if we're in the target scene
        if (SceneManager.GetActiveScene().buildIndex == targetSceneIndex)
        {
            if (audioSource != null && fireAmbientSound != null)
            {
                // Reset fade states
                isFadingOut = false;
                isFadingIn = true;
                
                // Start playing if not already playing
                if (!audioSource.isPlaying)
                {
                    audioSource.Play();
                    Debug.Log("Started fire ambient sound");
                }
            }
            else if (fireAmbientSound == null)
            {
                Debug.LogWarning("No fire ambient sound clip assigned!");
            }
        }
    }
    
    public void StopFireSound()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            // Immediately stop sound without fading when leaving the scene
            audioSource.Stop();
            isFadingIn = false;
            isFadingOut = false;
            Debug.Log("Stopped fire ambient sound immediately");
        }
    }
}
