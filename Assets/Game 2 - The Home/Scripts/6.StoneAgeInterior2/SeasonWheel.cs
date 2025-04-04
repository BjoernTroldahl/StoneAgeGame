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
    
    private int currentSeason = 0;
    private bool isAnimating = false;
    private SpriteRenderer wheelRenderer;
    private BoxCollider2D wheelCollider;
    private SpriteRenderer beerRenderer;
    private SpriteRenderer arrowRenderer;

    void Start()
    {
        // Get components
        wheelRenderer = GetComponent<SpriteRenderer>();
        wheelCollider = GetComponent<BoxCollider2D>();
        beerRenderer = beerVessel?.GetComponent<SpriteRenderer>();
        arrowRenderer = arrowSign?.GetComponent<SpriteRenderer>();
        
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
    
    private IEnumerator RotateWheelForSeason()
    {
        isAnimating = true;
        
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
        
        // Increment season counter
        currentSeason++;
        
        // Update beer sprite based on current season
        UpdateBeerSprite();
        
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
}
