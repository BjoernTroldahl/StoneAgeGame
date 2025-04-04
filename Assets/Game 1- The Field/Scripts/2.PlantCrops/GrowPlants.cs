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
    
    // Event that will be triggered when the entire growth sequence completes
    public UnityEvent onGrowthSequenceComplete = new UnityEvent();
    
    private bool sequenceStarted = false;

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
        
        // Wait for the second duration
        yield return new WaitForSeconds(plantDisplayDuration);
        
        // Step 3: Notify that the sequence is complete
        Debug.Log("Growth sequence complete!");
        onGrowthSequenceComplete.Invoke();
    }
}
