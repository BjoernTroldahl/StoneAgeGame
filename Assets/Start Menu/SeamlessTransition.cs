using UnityEngine;
using UnityEngine.SceneManagement;

public class SeamlessTransition : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int[] persistentScenes = { 0, 5, 10, 11 }; // Scenes where audio should play
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private float musicVolume = 0.7f;
    [SerializeField] private float fadeOutDuration = 2f;
    
    private static SeamlessTransition instance;
    private AudioSource audioSource;
    private float originalVolume;
    private bool shouldFadeOut = false;
    private float fadeTimer = 0f;
    
    private void Awake()
    {
        // Simple singleton pattern
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Set up audio source
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = backgroundMusic;
        audioSource.volume = musicVolume;
        audioSource.loop = true;
        
        // Store original volume for fading
        originalVolume = musicVolume;
        
        // Register for scene changes
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        // Check if we should start playing in current scene
        CheckCurrentScene();
    }
    
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void CheckCurrentScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        
        if (ShouldPlayInScene(currentSceneIndex))
        {
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
                Debug.Log($"[SeamlessTransition] Started playing music in scene {currentSceneIndex}");
            }
            shouldFadeOut = false;
            audioSource.volume = musicVolume;
        }
        else
        {
            shouldFadeOut = true;
            fadeTimer = 0f;
            Debug.Log($"[SeamlessTransition] Should not play in scene {currentSceneIndex}, fading out");
        }
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        int loadedSceneIndex = scene.buildIndex;
        Debug.Log($"[SeamlessTransition] Scene loaded: {loadedSceneIndex}");
        
        if (ShouldPlayInScene(loadedSceneIndex))
        {
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
                Debug.Log($"[SeamlessTransition] Started playing music in scene {loadedSceneIndex}");
            }
            shouldFadeOut = false;
            audioSource.volume = musicVolume;
        }
        else
        {
            shouldFadeOut = true;
            fadeTimer = 0f;
            Debug.Log($"[SeamlessTransition] Should not play in scene {loadedSceneIndex}, fading out");
        }
    }
    
    private void Update()
    {
        // Handle fading out music
        if (shouldFadeOut && audioSource.isPlaying)
        {
            fadeTimer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(originalVolume, 0f, fadeTimer / fadeOutDuration);
            
            if (audioSource.volume <= 0.01f)
            {
                audioSource.Stop();
                audioSource.volume = musicVolume; // Reset volume for next time
                shouldFadeOut = false;
                Debug.Log("[SeamlessTransition] Fade out complete");
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
    
    // Public API for manual control
    public static void StartMusic()
    {
        if (instance != null && !instance.audioSource.isPlaying)
        {
            instance.audioSource.volume = instance.musicVolume;
            instance.audioSource.Play();
            instance.shouldFadeOut = false;
            Debug.Log("[SeamlessTransition] Music manually started");
        }
    }
    
    public static void StopMusic(bool fadeOut = true)
    {
        if (instance != null)
        {
            if (fadeOut)
            {
                instance.shouldFadeOut = true;
                instance.fadeTimer = 0f;
                Debug.Log("[SeamlessTransition] Music fadeout manually triggered");
            }
            else
            {
                instance.audioSource.Stop();
                Debug.Log("[SeamlessTransition] Music manually stopped");
            }
        }
    }
    
    public static void SetVolume(float volume)
    {
        if (instance != null)
        {
            instance.musicVolume = volume;
            instance.originalVolume = volume;
            if (!instance.shouldFadeOut)
            {
                instance.audioSource.volume = volume;
            }
            Debug.Log($"[SeamlessTransition] Music volume set to {volume}");
        }
    }
}
