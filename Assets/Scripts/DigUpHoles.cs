using UnityEngine;
using UnityEngine.UI; // Add if holes are UI Images
using System.Collections.Generic;

public class DigUpHoles : MonoBehaviour
{
    [Header("Hole References")]
    [SerializeField] private GameObject hole1;
    [SerializeField] private GameObject hole2;
    [SerializeField] private GameObject hole3;

    private Dictionary<GameObject, bool> holes = new Dictionary<GameObject, bool>();

    void Start()
    {
        // Initialize holes dictionary and set them to hidden
        if (hole1) InitializeHole(hole1);
        if (hole2) InitializeHole(hole2);
        if (hole3) InitializeHole(hole3);

        // Debug log to verify initialization
        Debug.Log($"Holes initialized: {holes.Count}");
    }

    private void InitializeHole(GameObject hole)
    {
        // Ensure the hole has required components
        SpriteRenderer spriteRenderer = hole.GetComponent<SpriteRenderer>();
        BoxCollider2D boxCollider = hole.GetComponent<BoxCollider2D>();

        if (spriteRenderer != null && boxCollider != null)
        {
            holes[hole] = false; // Use dictionary indexer instead of Add
            // Make sprite invisible but keep collider active
            Color spriteColor = spriteRenderer.color;
            spriteColor.a = 0f; // Set alpha to 0 (fully transparent)
            spriteRenderer.color = spriteColor;
            
            // Debug log to verify each hole's initial state
            Debug.Log($"Initialized hole {hole.name}: visible = {holes[hole]}");
        }
        else
        {
            Debug.LogError($"Hole {hole.name} is missing required components (SpriteRenderer or BoxCollider2D)");
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left mouse button
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            if (hit.collider != null)
            {
                GameObject clickedObject = hit.collider.gameObject;
                
                if (holes.ContainsKey(clickedObject) && !holes[clickedObject])
                {
                    // Make hole visible by setting alpha back to 1
                    SpriteRenderer spriteRenderer = clickedObject.GetComponent<SpriteRenderer>();
                    if (spriteRenderer != null)
                    {
                        Color spriteColor = spriteRenderer.color;
                        spriteColor.a = 1f;
                        spriteRenderer.color = spriteColor;
                        holes[clickedObject] = true;
                        
                        // Debug log when a hole becomes visible
                        Debug.Log($"Made hole {clickedObject.name} visible");
                    }
                }
            }
        }
    }

    // Add this method to the DigUpHoles class
    public bool IsHoleVisible(GameObject hole)
    {
        if (holes.ContainsKey(hole))
        {
            // Debug log for visibility checks
            Debug.Log($"Checking visibility for {hole.name}: {holes[hole]}");
            return holes[hole];
        }
        Debug.LogWarning($"Hole {hole.name} not found in dictionary");
        return false;
    }
}
