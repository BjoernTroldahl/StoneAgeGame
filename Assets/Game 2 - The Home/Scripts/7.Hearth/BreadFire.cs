using UnityEngine;
using System.Collections;

public class BreadFire : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite[] fireSprites; // Array to hold all sprites to cycle through
    
    [Header("Timing")]
    [SerializeField] private float cycleTime = 0.3f; // Time between sprite changes in seconds
    
    private SpriteRenderer spriteRenderer;
    private int currentSpriteIndex = 0;
    private float timeUntilNextChange;

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
        
        // Initialize with first sprite
        if (fireSprites.Length > 0 && fireSprites[0] != null)
        {
            spriteRenderer.sprite = fireSprites[0];
        }
        
        // Initialize timer
        timeUntilNextChange = cycleTime;
    }

    private void Update()
    {
        // Update timer
        timeUntilNextChange -= Time.deltaTime;
        
        // Check if it's time to change sprites
        if (timeUntilNextChange <= 0)
        {
            // Reset timer
            timeUntilNextChange = cycleTime;
            
            // Change to next sprite
            CycleToNextSprite();
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
}
