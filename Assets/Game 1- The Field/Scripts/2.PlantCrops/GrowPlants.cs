using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class GrowPlants : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private GameObject pouchObject; // Reference to the pouch object to hide
    [SerializeField] private SpriteRenderer[] plantSprites; // Array of 6 plant sprites
    
    [Header("Timing Settings")]
    [SerializeField] private float waitAfterPouchHide = 2.0f; // How long to wait after hiding pouch
    [SerializeField] private float plantDisplayDuration = 2.0f; // How long to show plants before scene change
    
    [Header("Audio Settings")]
    [SerializeField] private AudioClip plantGrowthSound; // Sound played during plant growth
    [SerializeField] private float growthSoundVolume = 0.7f; // Volume for growth sound
    [SerializeField] private bool loopGrowthSound = true; // Whether to loop sound during growth
    [SerializeField] private float fadeOutDuration = 1.0f; // Time to fade out audio at end of sequence
    
    // Event that will be triggered when the entire growth sequence completes
    public UnityEvent onGrowthSequenceComplete = new UnityEvent();
    
    private bool sequenceStarted = false;
    private AudioSource audioSource;

    void Start()
    {
        // Make sure all plant sprites start hidden
        foreach (SpriteRenderer plantSprite in plantSprites)
        {
            if (plantSprite != null)
            {
                plantSprite.enabled = false;
            }
        }
        
        // Set up audio source
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = plantGrowthSound;
        audioSource.volume = growthSoundVolume;
        audioSource.loop = loopGrowthSound;
        audioSource.playOnAwake = false;
    }

    // Public method that DigUpHoles will call
    public void StartGrowthSequence()
    {
        if (!sequenceStarted)
        {
            sequenceStarted = true;
            StartCoroutine(GrowthSequenceCoroutine());
        }
    }
    
    private IEnumerator GrowthSequenceCoroutine()
    {
        Debug.Log("Starting growth sequence...");
        
        // Play growth sound
        PlayGrowthSound();
        
        // Step 1: Hide pouch object
        if (pouchObject != null)
        {
            pouchObject.SetActive(false);
            Debug.Log("Pouch object hidden");
        }
        else
        {
            Debug.LogWarning("Pouch object reference is missing!");
        }
        
        // Wait for specified duration
        yield return new WaitForSeconds(waitAfterPouchHide);
        
        // Step 2: Show all plant sprites
        Debug.Log("Showing plants...");
        foreach (SpriteRenderer plantSprite in plantSprites)
        {
            if (plantSprite != null)
            {
                plantSprite.enabled = true;
            }
        }
        
        // Wait for most of the second duration, leaving time for fade out
        float remainingTime = plantDisplayDuration - fadeOutDuration;
        if (remainingTime > 0)
        {
            yield return new WaitForSeconds(remainingTime);
        }
        
        // Fade out the audio
        yield return StartCoroutine(FadeOutAudio());
        
        // Step 3: Notify that the sequence is complete
        Debug.Log("Growth sequence complete!");
        onGrowthSequenceComplete.Invoke();
    }
    
    private void PlayGrowthSound()
    {
        if (plantGrowthSound != null && audioSource != null)
        {
            audioSource.volume = growthSoundVolume;
            audioSource.Play();
            Debug.Log("Started playing plant growth sound");
        }
        else if (plantGrowthSound == null)
        {
            Debug.LogWarning("Plant growth sound clip is not assigned!");
        }
    }
    
    private IEnumerator FadeOutAudio()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            float startVolume = audioSource.volume;
            float elapsedTime = 0;
            
            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / fadeOutDuration;
                audioSource.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }
            
            // Ensure volume is set to 0 and stop playing
            audioSource.volume = 0f;
            audioSource.Stop();
            Debug.Log("Finished fading out plant growth sound");
        }
    }
    
    private void OnDestroy()
    {
        // Ensure audio is cleaned up when script is destroyed
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}
