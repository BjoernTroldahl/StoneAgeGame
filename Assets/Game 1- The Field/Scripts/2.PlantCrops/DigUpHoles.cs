using UnityEngine;
using UnityEngine.UI; // Add if holes are UI Images
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
    [SerializeField] private Sprite dirtPileSprite; // Add this reference in inspector

    private Dictionary<GameObject, bool> holes = new Dictionary<GameObject, bool>();
    private Dictionary<GameObject, bool> hasSnappedSeed = new Dictionary<GameObject, bool>();

    void Start()
    {
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
            hasSnappedSeed[hole] = false; // Track if hole has a snapped seed
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
                    else if (hasSnappedSeed[clickedObject])
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
            // Change sprite
            spriteRenderer.sprite = dirtPileSprite;
            
            // Set Order in Layer
            spriteRenderer.sortingOrder = 3;
            
            // Set Scale
            hole.transform.localScale = new Vector3(0.1408369f, 0.1408369f, 1f);
            
            Debug.Log($"Changed {hole.name} to dirt pile with adjusted order and scale");

            // Check if all holes are now dirt piles
            CheckGameCompletion();
        }
    }

    private void CheckGameCompletion()
    {
        bool allHolesCovered = true;
        
        // Check each hole's sprite
        foreach (GameObject hole in holes.Keys)
        {
            SpriteRenderer spriteRenderer = hole.GetComponent<SpriteRenderer>();
            if (spriteRenderer.sprite != dirtPileSprite)
            {
                allHolesCovered = false;
                break;
            }
        }
        
        if (allHolesCovered)
        {
            Debug.Log("CONGRATS, YOU WON THE GAME");
            SceneManager.LoadScene(2); // Load the next scene
        }
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
