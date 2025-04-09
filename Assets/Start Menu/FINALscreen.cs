using UnityEngine;
using UnityEngine.SceneManagement;

public class FINALscreen : MonoBehaviour
{
    // Singleton instance to prevent duplicates
    private static FINALscreen instance;
    
    // Static variables to track game completion (persist between scene loads)
    private static bool game1Completed = false;
    private static bool game2Completed = false;
    
    // Constants for scene indices
    private const int MAIN_MENU_SCENE = 0;
    private const int FINAL_SCENE = 11;
    
    // Game 1 scene ranges (inclusive)
    private const int GAME1_START_SCENE = 1;
    private const int GAME1_END_SCENE = 5;
    
    // Game 2 scene ranges (inclusive)
    private const int GAME2_START_SCENE = 6;
    private const int GAME2_END_SCENE = 10;
    
    [Header("Audio Settings")]
    [SerializeField] private AudioClip finalSceneSound;     // Sound to play when final scene loads
    [SerializeField] private float soundVolume = 0.8f;      // Volume for final scene sound
    
    // Flag to track if we've already played the final scene sound
    private static bool finalSoundPlayed = false;
    
    // Audio source reference
    private AudioSource audioSource;

    void Awake()
    {
        // Singleton pattern to prevent multiple instances
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Set up audio source
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            
            Debug.Log("FINALscreen instance created and set to persist between scenes");
        }
        else
        {
            // If an instance already exists, destroy this duplicate
            Debug.Log("Duplicate FINALscreen instance found and destroyed");
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Register for scene loaded events
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        // Check current scene on start (in case this script is in every scene)
        CheckCurrentScene();
        
        Debug.Log($"Game progress - Game 1: {game1Completed}, Game 2: {game2Completed}");
    }

    void OnDestroy()
    {
        // Unregister from scene events when destroyed
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Check the scene that was just loaded
        CheckCurrentScene();
        
        // Check if we've loaded the final scene and should play sound
        if (scene.buildIndex == FINAL_SCENE && !finalSoundPlayed && audioSource != null && finalSceneSound != null)
        {
            PlayFinalSceneSound();
        }
    }
    
    // Play the final scene sound once
    private void PlayFinalSceneSound()
    {
        if (finalSoundPlayed) return; // Skip if already played
        
        audioSource.clip = finalSceneSound;
        audioSource.volume = soundVolume;
        audioSource.Play();
        
        finalSoundPlayed = true;
        
        Debug.Log("Final scene sound played");
    }
    
    // Reset the sound played flag when returning to main menu
    private void ResetFinalSound()
    {
        finalSoundPlayed = false;
        Debug.Log("Final sound flag reset");
    }
    
    private void CheckCurrentScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        
        // Check if we're at the end of Game 1
        if (currentSceneIndex == GAME1_END_SCENE)
        {
            game1Completed = true;
            Debug.Log("Game 1 completed!");
        }
        
        // Check if we're at the end of Game 2
        if (currentSceneIndex == GAME2_END_SCENE)
        {
            game2Completed = true;
            Debug.Log("Game 2 completed!");
        }
        
        // Check if we're at the main menu - reset progress if coming from final scene
        if (currentSceneIndex == MAIN_MENU_SCENE && game1Completed && game2Completed)
        {
            Debug.Log("Returning to main menu after completion - resetting progress");
            game1Completed = false;
            game2Completed = false;
            ResetFinalSound(); // Reset the sound flag so it can play again next time
        }
        
        // If both games are completed, load the final scene
        if (game1Completed && game2Completed && currentSceneIndex != FINAL_SCENE)
        {
            Debug.Log("Both games completed! Loading final scene.");
            Invoke("LoadFinalScene", 0f); // Small delay before loading final scene
        }
    }
    
    private void LoadFinalScene()
    {
        SceneManager.LoadScene("FINALscene");
    }
    
    // Public static method to check completion status
    public static bool AreBothGamesCompleted()
    {
        return game1Completed && game2Completed;
    }
    
    // Public static method to manually set completion (for testing)
    public static void SetGameCompletion(bool game1Status, bool game2Status)
    {
        game1Completed = game1Status;
        game2Completed = game2Status;
        Debug.Log($"Game completion manually set - Game 1: {game1Completed}, Game 2: {game2Completed}");
    }
    
    // Public method to reset progress (can be called from UI button)
    public void ResetProgress()
    {
        game1Completed = false;
        game2Completed = false;
        ResetFinalSound(); // Reset the sound flag as well
        Debug.Log("Game progress reset");
    }
}
