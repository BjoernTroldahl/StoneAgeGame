using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class TorchFire : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private ParticleSystem[] fireParticleSystems;  
    [SerializeField] private GameObject[] treeObjects;  
    [SerializeField] private float detectionRange = 2f; 
    [SerializeField] private GameObject torch;          
    [SerializeField] private float axeDetectionRange = 1.5f; 

    [Header("Tree Settings")]
    [SerializeField] private Sprite deadTreeSprite;    
    [SerializeField] private float burnDuration = 5f;  

    [Header("Audio Settings")]
    [SerializeField] private AudioClip fireSound;      
    [SerializeField] private float fireSoundVolume = 0.7f; 
    [SerializeField] private bool useSpatialSound = true;  
    [SerializeField] private float maxSoundDistance = 20f;
    
    [Header("Axe Cutting Sound")]
    [SerializeField] private AudioClip axeCuttingSound; // Sound when axe cuts tree
    [SerializeField] private float axeSoundVolume = 0.8f; // Volume for axe sound
    [SerializeField] private bool playSoundOnAllTrees = false; // If false, only plays once when last tree is cut

    private Dictionary<GameObject, Coroutine> activeAnimations = new Dictionary<GameObject, Coroutine>();
    private Dictionary<GameObject, AudioSource> activeSounds = new Dictionary<GameObject, AudioSource>(); 
    private HashSet<GameObject> burnedTrees = new HashSet<GameObject>();  
    private HashSet<GameObject> hiddenTrees = new HashSet<GameObject>();  
    private Dragging torchScript;
    private AudioSource axeAudioSource; // Dedicated audio source for axe sounds

    private void Start()
    {
        // Stop and disable all fire particle systems at start
        foreach (ParticleSystem fire in fireParticleSystems)
        {
            if (fire != null)
            {
                fire.Stop();
                fire.gameObject.SetActive(false);
            }
        }
        
        // Get reference to the Dragging script
        if (torch != null)
        {
            torchScript = torch.GetComponent<Dragging>();
            if (torchScript == null)
            {
                Debug.LogError("Dragging script not found on torch object!");
            }
        }
        
        // Set up audio source for axe cutting sounds
        if (axeCuttingSound != null)
        {
            axeAudioSource = gameObject.AddComponent<AudioSource>();
            axeAudioSource.clip = axeCuttingSound;
            axeAudioSource.volume = axeSoundVolume;
            axeAudioSource.loop = false;
            axeAudioSource.playOnAwake = false;
            
            // Configure spatial audio if needed
            if (useSpatialSound)
            {
                axeAudioSource.spatialBlend = 0.7f; // Mostly 3D but with some 2D component
                axeAudioSource.minDistance = 1.0f;
                axeAudioSource.maxDistance = maxSoundDistance;
                axeAudioSource.rolloffMode = AudioRolloffMode.Linear;
            }
        }
    }

    private void Update()
    {
        // Skip if torch reference is missing
        if (torch == null) return;
        
        // Get the torch's current state (axe or torch)
        bool isAxe = torchScript != null && torchScript.HasTransformedToAxe();
        
        // Check for torch proximity to start fires
        if (!isAxe) // Only start fires if it's still a torch
        {
            for (int i = 0; i < treeObjects.Length; i++)
            {
                if (treeObjects[i] != null && i < fireParticleSystems.Length)
                {
                    float distance = Vector2.Distance(torch.transform.position, treeObjects[i].transform.position);
                    
                    if (distance <= detectionRange)
                    {
                        StartTreeFire(treeObjects[i], fireParticleSystems[i]);
                    }
                }
            }
        }
        else // If it's an axe, check for proximity to dead trees
        {
            foreach (GameObject tree in treeObjects)
            {
                if (tree != null && IsTreeBurned(tree) && !hiddenTrees.Contains(tree))
                {
                    float distance = Vector2.Distance(torch.transform.position, tree.transform.position);
                    
                    if (distance <= axeDetectionRange)
                    {
                        HideDeadTree(tree);
                    }
                }
            }
        }
    }
    
    // Helper method to check if a tree is burned
    private bool IsTreeBurned(GameObject tree)
    {
        int treeIndex = System.Array.IndexOf(treeObjects, tree);
        return treeIndex >= 0 && burnedTrees.Contains(tree);
    }

    private void StartTreeFire(GameObject tree, ParticleSystem fireParticles)
    {
        // Skip if this tree is already burned or being burned
        if (burnedTrees.Contains(tree) || activeAnimations.ContainsKey(tree))
        {
            return;
        }

        // Start fire particle system and audio
        if (fireParticles != null)
        {
            // Activate and start the particle system
            fireParticles.gameObject.SetActive(true);
            fireParticles.Play();
            
            // Create and play fire sound
            if (fireSound != null && !activeSounds.ContainsKey(tree))
            {
                // Create audio source for this tree
                AudioSource audioSource = tree.AddComponent<AudioSource>();
                audioSource.clip = fireSound;
                audioSource.volume = fireSoundVolume;
                audioSource.loop = true;
                
                // Configure spatial audio if enabled
                if (useSpatialSound)
                {
                    audioSource.spatialBlend = 1.0f; // Full 3D sound
                    audioSource.minDistance = 1.0f;
                    audioSource.maxDistance = maxSoundDistance;
                    audioSource.rolloffMode = AudioRolloffMode.Linear;
                }
                else
                {
                    audioSource.spatialBlend = 0.0f; // 2D sound
                }
                
                audioSource.Play();
                activeSounds[tree] = audioSource;
                Debug.Log($"Started fire sound for tree: {tree.name}");
            }
            
            // Start coroutine to handle the burning process
            Coroutine burningCoroutine = StartCoroutine(BurnTree(tree, fireParticles));
            activeAnimations[tree] = burningCoroutine;
            Debug.Log($"Started fire animation for tree: {tree.name}");
        }
    }

    private IEnumerator BurnTree(GameObject tree, ParticleSystem fireParticles)
    {
        // Wait for burn duration while particles are playing
        yield return new WaitForSeconds(burnDuration);

        // Change tree to dead tree sprite
        SpriteRenderer treeRenderer = tree.GetComponent<SpriteRenderer>();
        if (treeRenderer != null)
        {
            treeRenderer.sprite = deadTreeSprite;
            burnedTrees.Add(tree);
            Debug.Log($"Tree {tree.name} changed to dead tree");
        }

        // Gradually fade out the particle system and audio
        float fadeTime = 1.0f;
        float elapsedTime = 0f;
        ParticleSystem.MainModule main = fireParticles.main;
        
        // Store original start lifetime
        float originalStartLifetime = main.startLifetime.constant;
        
        // Get the audio source if it exists
        AudioSource audioSource = null;
        float originalVolume = fireSoundVolume;
        if (activeSounds.TryGetValue(tree, out audioSource))
        {
            originalVolume = audioSource.volume;
        }
        
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeTime;
            
            // Gradually reduce particle lifetime to fade out
            main.startLifetime = Mathf.Lerp(originalStartLifetime, 0, t);
            
            // Fade out audio if it exists
            if (audioSource != null)
            {
                audioSource.volume = Mathf.Lerp(originalVolume, 0, t);
            }
            
            yield return null;
        }
        
        // Stop emitting new particles but let existing ones finish
        fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        
        // Stop and clean up audio
        if (audioSource != null)
        {
            audioSource.Stop();
            Destroy(audioSource);
            activeSounds.Remove(tree);
        }
        
        // Wait for all particles to die
        yield return new WaitForSeconds(originalStartLifetime);
        
        // Disable the particle system completely
        fireParticles.gameObject.SetActive(false);
        
        // Remove from active animations
        activeAnimations.Remove(tree);
        
        // Check if all trees are burned
        if (burnedTrees.Count == treeObjects.Length)
        {
            Debug.Log("All trees burned - player can now use axe to chop them");
        }
    }

    private void HideDeadTree(GameObject tree)
    {
        if (!hiddenTrees.Contains(tree))
        {
            // Play axe cutting sound if available
            PlayAxeCuttingSound(tree);
            
            // Hide the tree
            SpriteRenderer treeRenderer = tree.GetComponent<SpriteRenderer>();
            if (treeRenderer != null)
            {
                Color color = treeRenderer.color;
                color.a = 0f;
                treeRenderer.color = color;
                hiddenTrees.Add(tree);
                Debug.Log($"Dead tree chopped down with axe: {tree.name}");

                // Check if all burned trees are now hidden
                if (hiddenTrees.Count == burnedTrees.Count && burnedTrees.Count == treeObjects.Length)
                {
                    Debug.Log("CONGRATS YOU WON THE GAME");
                    StartCoroutine(DelayedSceneLoad());
                }
            }
        }
    }
    
    private void PlayAxeCuttingSound(GameObject tree)
    {
        if (axeCuttingSound == null) return;
        
        // If we should play for every tree OR this is the final tree
        bool isFinalTree = hiddenTrees.Count == burnedTrees.Count - 1 && burnedTrees.Count == treeObjects.Length;
        
        if (playSoundOnAllTrees || isFinalTree)
        {
            // Position audio source at tree location for spatial sound
            if (useSpatialSound && tree != null)
            {
                axeAudioSource.transform.position = tree.transform.position;
            }
            
            // If audio is already playing, stop it first
            if (axeAudioSource.isPlaying)
            {
                axeAudioSource.Stop();
            }
            
            // Play the cutting sound
            axeAudioSource.PlayOneShot(axeCuttingSound, axeSoundVolume);
            Debug.Log($"Playing axe cutting sound for tree: {tree.name}");
        }
    }
    
    private IEnumerator DelayedSceneLoad()
    {
        // Wait for 2 seconds before loading next scene
        yield return new WaitForSeconds(0f);
        SceneManager.LoadScene("2.PlantCrops"); // Load the next scene
    }
    
    private void OnDestroy()
    {
        // Clean up all audio sources when destroyed
        foreach (var audioSource in activeSounds.Values)
        {
            if (audioSource != null)
            {
                Destroy(audioSource);
            }
        }
        activeSounds.Clear();
    }
}
