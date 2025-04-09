using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement; 

public class DigUpHoles : MonoBehaviour
{
    [Header("Hole References")]
    [SerializeField] private GameObject hole1;
    [SerializeField] private GameObject hole2;
    [SerializeField] private GameObject hole3;
    [SerializeField] private GameObject hole4;
    [SerializeField] private GameObject hole5;
    [SerializeField] private GameObject hole6;

    [Header("Sprite References")]
    [SerializeField] private Sprite dirtPileSprite;

    [Header("Audio")]
    [SerializeField] private AudioClip digHoleSound;       // Sound when revealing a hole
    [SerializeField] private AudioClip coverWithDirtSound; // Sound when changing to dirt pile
    [SerializeField] private float soundVolume = 0.7f;     // Volume for sound effects

    [Header("Growth Sequence")]
    [SerializeField] private GrowPlants growPlantsController;
    private bool growthSequenceTriggered = false;

    private Dictionary<GameObject, bool> holes = new Dictionary<GameObject, bool>();
    private Dictionary<GameObject, bool> hasSnappedSeed = new Dictionary<GameObject, bool>();
    private Dictionary<GameObject, bool> isDirtPile = new Dictionary<GameObject, bool>(); // NEW: Track dirt pile state
    
    // Audio source for playing hole-related sounds
    private AudioSource audioSource;

    void Start()
    {
        // Set up audio source
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = soundVolume;
        
        // Initialize dictionaries
        if (hole1) InitializeHole(hole1);
        if (hole2) InitializeHole(hole2);
        if (hole3) InitializeHole(hole3);
        if (hole4) InitializeHole(hole4);
        if (hole5) InitializeHole(hole5);
        if (hole6) InitializeHole(hole6);

        Debug.Log($"Holes initialized: {holes.Count}");
    }

    private void InitializeHole(GameObject hole)
    {
        SpriteRenderer spriteRenderer = hole.GetComponent<SpriteRenderer>();
        BoxCollider2D boxCollider = hole.GetComponent<BoxCollider2D>();

        if (spriteRenderer != null && boxCollider != null)
        {
            holes[hole] = false;
            hasSnappedSeed[hole] = false;
            isDirtPile[hole] = false; // NEW: Initialize dirt pile state to false
            
            Color spriteColor = spriteRenderer.color;
            spriteColor.a = 0f;
            spriteRenderer.color = spriteColor;
            
            Debug.Log($"Initialized hole {hole.name}: visible = {holes[hole]}");
        }
        else
        {
            Debug.LogError($"Hole {hole.name} is missing required components");
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            if (hit.collider != null)
            {
                GameObject clickedObject = hit.collider.gameObject;
                
                if (holes.ContainsKey(clickedObject))
                {
                    if (!holes[clickedObject])
                    {
                        // First click - make hole visible
                        RevealHole(clickedObject);
                    }
                    else if (hasSnappedSeed[clickedObject] && !isDirtPile[clickedObject])
                    {
                        // Second click after seed is snapped - change to dirt pile
                        ChangeToDirtPile(clickedObject);
                    }
                }
            }
        }
    }

    private void RevealHole(GameObject hole)
    {
        SpriteRenderer spriteRenderer = hole.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // Play digging sound effect
            PlaySound(digHoleSound);
            
            // Make hole visible
            Color spriteColor = spriteRenderer.color;
            spriteColor.a = 1f;
            spriteRenderer.color = spriteColor;
            holes[hole] = true;
            Debug.Log($"Made hole {hole.name} visible");
        }
    }

    private void ChangeToDirtPile(GameObject hole)
    {
        SpriteRenderer spriteRenderer = hole.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && dirtPileSprite != null)
        {
            // Play dirt covering sound effect
            PlaySound(coverWithDirtSound);
            
            // Change sprite
            spriteRenderer.sprite = dirtPileSprite;
            
            // Set Order in Layer to 4
            spriteRenderer.sortingOrder = 4;
            
            // Set Scale
            hole.transform.localScale = new Vector3(1f, 1f, 1f);
            
            // Mark as dirt pile
            isDirtPile[hole] = true; // NEW: Update the dirt pile state
            
            Debug.Log($"Changed {hole.name} to dirt pile with sorting order 4 and adjusted scale");

            // Check if all holes are now dirt piles
            CheckGameCompletion();
        }
    }
    
    // Play a sound effect
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
            Debug.Log($"Playing sound: {clip.name}");
        }
        else if (clip == null)
        {
            Debug.LogWarning("Attempted to play a null audio clip");
        }
    }

    private void CheckGameCompletion()
    {
        bool allHolesCoveredWithDirt = true;
        int dirtPileCount = 0;
        
        foreach (GameObject hole in holes.Keys)
        {
            if (!isDirtPile[hole]) // NEW: Check dirt pile state instead of visibility
            {
                allHolesCoveredWithDirt = false;
            }
            else
            {
                dirtPileCount++;
            }
        }
        
        Debug.Log($"Dirt pile check: {dirtPileCount}/6 holes converted");
        
        if (allHolesCoveredWithDirt && !growthSequenceTriggered)
        {
            growthSequenceTriggered = true;
            Debug.Log("All holes are converted to dirt piles!");
            
            // Instead of immediately loading the next scene, trigger the growth sequence
            if (growPlantsController != null)
            {
                // Subscribe to the event that will be triggered when growth is done
                growPlantsController.onGrowthSequenceComplete.AddListener(CompleteScene);
                
                // Start the growth sequence
                growPlantsController.StartGrowthSequence();
            }
            else
            {
                Debug.LogWarning("GrowPlants controller not assigned! Loading next scene immediately.");
                CompleteScene();
            }
        }
    }

    private void CompleteScene()
    {
        Debug.Log("CONGRATULATIONS, YOU WON THE GAME");
        // Load the next scene
        SceneManager.LoadScene("3.WaterCrops");
    }

    public bool IsHoleVisible(GameObject hole)
    {
        if (holes.ContainsKey(hole))
        {
            return holes[hole];
        }
        Debug.LogWarning($"Hole {hole.name} not found in dictionary");
        return false;
    }

    // Add this method to be called from SnapController when a seed snaps
    public void NotifySeedSnapped(GameObject hole)
    {
        if (hasSnappedSeed.ContainsKey(hole))
        {
            hasSnappedSeed[hole] = true;
            Debug.Log($"Hole {hole.name} now has a snapped seed");
        }
    }
}
