using UnityEngine;
using UnityEngine.SceneManagement;

public class BeerVessel : MonoBehaviour
{
    [SerializeField] private Sprite beerUncovered;
    [SerializeField] private DragPorridge porridgeScript; // Add reference to porridge script
    private SpriteRenderer spriteRenderer;
    private bool isUncovered = false;
    
    // Track scene load listener to avoid duplicate registrations
    private static bool sceneLoadListenerAdded = false;

    void Awake()
    {
        // Add scene load listener once
        if (!sceneLoadListenerAdded)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            sceneLoadListenerAdded = true;
            Debug.Log("BeerVessel: Scene load listener added");
        }
        
        // Reset variables on Awake
        ResetInstanceVariables();
    }
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        Debug.Log("BeerVessel: Started in covered state");
    }
    
    // Clean up event subscription when object is destroyed
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        sceneLoadListenerAdded = false;
        Debug.Log("BeerVessel: Cleaned up event handlers");
    }
    
    // Handle scene load events
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetInstanceVariables();
        Debug.Log("BeerVessel: Variables reset on scene load");
    }
    
    // Reset all instance variables to default values
    private void ResetInstanceVariables()
    {
        isUncovered = false;
        
        // Reset sprite if reference exists
        if (spriteRenderer != null)
        {
            // Reset to original covered sprite if we have it stored
            Sprite originalSprite = GetOriginalSprite();
            if (originalSprite != null)
            {
                spriteRenderer.sprite = originalSprite;
            }
        }
    }
    
    // Helper method to get original sprite
    private Sprite GetOriginalSprite()
    {
        // If we don't have the renderer yet, try to get it
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        // If we still have the renderer, return its current sprite
        if (spriteRenderer != null)
        {
            return spriteRenderer.sprite;
        }
        
        return null;
    }

    private void OnMouseDown()
    {
        if (!isUncovered)
        {
            DragPorridge.UncoverBeerVessel(spriteRenderer, beerUncovered, porridgeScript);
            isUncovered = true;
            Debug.Log("BeerVessel: Uncovered through mouse click");
        }
    }
    
    // Public method to reset state programmatically if needed
    public void Reset()
    {
        ResetInstanceVariables();
        Debug.Log("BeerVessel: Reset manually");
    }
}
