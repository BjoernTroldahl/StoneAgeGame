using UnityEngine;
using UnityEngine.SceneManagement;

public class Game2BGMusic : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private float musicVolume = 0.7f;
    [SerializeField] private float fadeInDuration = 1.5f;
    [SerializeField] private float fadeOutDuration = 2f;
    
    [Header("Scene Settings")]
    [SerializeField] private int[] persistentScenes = { 6, 7, 8, 9 }; // Scenes where this music should play
    
    // Private variables
    private static Game2BGMusic instance;
    private AudioSource audioSource;
    private float targetVolume;
    private bool shouldFadeIn = false;
    private bool shouldFadeOut = false;
    private float fadeTimer = 0f;
    
    private void Awake()
    {
        // Singleton pattern implementation
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Initialize audio source
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = backgroundMusic;
        audioSource.loop = true;
        audioSource.volume = 0f; // Start silent for fade-in
        audioSource.playOnAwake = false;
        
        // Store target volume for fading
        targetVolume = musicVolume;
        
        // Register for scene changes
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        // Check current scene
        CheckCurrentScene();
    }
    
    private void OnDestroy()
    {
        // Clean up event subscription
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void CheckCurrentScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        
        if (ShouldPlayInScene(currentSceneIndex))
        {
            if (!audioSource.isPlaying)
            {
                // Start playing with fade-in
                audioSource.Play();
                shouldFadeIn = true;
                shouldFadeOut = false;
                fadeTimer = 0f;
                Debug.Log($"[Game2BGMusic] Started playing music in scene {currentSceneIndex}");
            }
        }
        else
        {
            // If we're not in a scene where music should play, fade out and stop
            if (audioSource.isPlaying)
            {
                shouldFadeOut = true;
                shouldFadeIn = false;
                fadeTimer = 0f;
                Debug.Log($"[Game2BGMusic] Should not play in scene {currentSceneIndex}, fading out");
            }
        }
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        int loadedSceneIndex = scene.buildIndex;
        Debug.Log($"[Game2BGMusic] Scene loaded: {loadedSceneIndex}");
        
        if (ShouldPlayInScene(loadedSceneIndex))
        {
            if (!audioSource.isPlaying)
            {
                // Start playing with fade-in
                audioSource.Play();
                shouldFadeIn = true;
                shouldFadeOut = false;
                fadeTimer = 0f;
                Debug.Log($"[Game2BGMusic] Started playing music in scene {loadedSceneIndex}");
            }
            else
            {
                // Already playing, ensure we're not fading out
                shouldFadeOut = false;
                
                // Only fade in if volume is low
                if (audioSource.volume < targetVolume * 0.9f)
                {
                    shouldFadeIn = true;
                    fadeTimer = fadeInDuration * (audioSource.volume / targetVolume); // Start fading from current position
                }
                
                Debug.Log($"[Game2BGMusic] Continuing to play music in scene {loadedSceneIndex}");
            }
        }
        else
        {
            // If we're not in a scene where music should play, fade out and stop
            if (audioSource.isPlaying)
            {
                shouldFadeOut = true;
                shouldFadeIn = false;
                fadeTimer = 0f;
                Debug.Log($"[Game2BGMusic] Should not play in scene {loadedSceneIndex}, fading out");
            }
        }
    }
    
    private void Update()
    {
        // Handle fading in
        if (shouldFadeIn && audioSource.isPlaying)
        {
            fadeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(fadeTimer / fadeInDuration);
            audioSource.volume = Mathf.Lerp(0f, targetVolume, t);
            
            if (t >= 1f)
            {
                // Fade in complete
                shouldFadeIn = false;
                audioSource.volume = targetVolume;
                Debug.Log("[Game2BGMusic] Fade in complete");
            }
        }
        
        // Handle fading out
        if (shouldFadeOut && audioSource.isPlaying)
        {
            fadeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(fadeTimer / fadeOutDuration);
            audioSource.volume = Mathf.Lerp(targetVolume, 0f, t);
            
            if (t >= 1f)
            {
                // Fade out complete
                audioSource.Stop();
                audioSource.volume = 0f;
                shouldFadeOut = false;
                Debug.Log("[Game2BGMusic] Fade out complete");
            }
        }
    }
    
    private bool ShouldPlayInScene(int sceneIndex)
    {
        foreach (int index in persistentScenes)
        {
            if (index == sceneIndex)
                return true;
        }
        return false;
    }
    
    // Public methods for external control
    
    /// <summary>
    /// Manually start playing the background music
    /// </summary>
    public static void PlayMusic()
    {
        if (instance != null && instance.audioSource != null && !instance.audioSource.isPlaying)
        {
            instance.audioSource.volume = 0f;
            instance.audioSource.Play();
            instance.shouldFadeIn = true;
            instance.shouldFadeOut = false;
            instance.fadeTimer = 0f;
        }
    }
    
    /// <summary>
    /// Manually stop the background music with an optional fade out
    /// </summary>
    /// <param name="fadeOut">If true, fade out before stopping; if false, stop immediately</param>
    public static void StopMusic(bool fadeOut = true)
    {
        if (instance != null && instance.audioSource != null && instance.audioSource.isPlaying)
        {
            if (fadeOut)
            {
                instance.shouldFadeOut = true;
                instance.shouldFadeIn = false;
                instance.fadeTimer = 0f;
            }
            else
            {
                instance.audioSource.Stop();
                instance.audioSource.volume = 0f;
            }
        }
    }
    
    /// <summary>
    /// Set the music volume with an optional fade to the new level
    /// </summary>
    /// <param name="volume">New volume level (0-1)</param>
    /// <param name="fade">If true, fade to the new volume; if false, change immediately</param>
    public static void SetVolume(float volume, bool fade = true)
    {
        if (instance != null && instance.audioSource != null)
        {
            float newVolume = Mathf.Clamp01(volume);
            instance.targetVolume = newVolume;
            
            if (!fade)
            {
                instance.audioSource.volume = newVolume;
                instance.shouldFadeIn = false;
                instance.shouldFadeOut = false;
            }
            else if (instance.audioSource.isPlaying)
            {
                // Start fading to the new volume
                if (newVolume > instance.audioSource.volume)
                {
                    instance.shouldFadeIn = true;
                    instance.shouldFadeOut = false;
                    instance.fadeTimer = instance.fadeInDuration * (instance.audioSource.volume / instance.targetVolume);
                }
                else if (newVolume < instance.audioSource.volume)
                {
                    instance.shouldFadeOut = true;
                    instance.shouldFadeIn = false;
                    instance.fadeTimer = instance.fadeOutDuration * (1 - (instance.audioSource.volume / instance.targetVolume));
                }
            }
        }
    }
}
