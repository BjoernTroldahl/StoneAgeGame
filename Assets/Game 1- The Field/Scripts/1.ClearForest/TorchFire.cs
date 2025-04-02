using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class TorchFire : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject[] fireSprites;  // Assign fire sprites in inspector
    [SerializeField] private GameObject[] treeObjects;  // Add reference to tree objects
    [SerializeField] private float detectionRange = 2f; // Range at which torch triggers fire
    [SerializeField] private GameObject torch;          // Reference to the torch object
    [SerializeField] private float axeDetectionRange = 1.5f; // Range for axe to detect dead trees

    [Header("Fire Animation")]
    [SerializeField] private Sprite fire1Sprite;    // Assign fire1_0 sprite in inspector
    [SerializeField] private Sprite fire2Sprite;    // Assign fire2_0 sprite in inspector
    [SerializeField] private Sprite deadTreeSprite;    // Add this field
    [SerializeField] private float switchInterval = 0.3f;
    [SerializeField] private float burnDuration = 5f;

    private Dictionary<GameObject, Coroutine> activeAnimations = new Dictionary<GameObject, Coroutine>();
    private HashSet<GameObject> burnedTrees = new HashSet<GameObject>();  // Track burned trees
    private HashSet<GameObject> hiddenTrees = new HashSet<GameObject>();  // Track hidden trees
    private Dragging torchScript; // Reference to the Dragging script

    private void Start()
    {
        // Hide all fire sprites at start
        foreach (GameObject fire in fireSprites)
        {
            if (fire != null)
            {
                SpriteRenderer spriteRenderer = fire.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    Color color = spriteRenderer.color;
                    color.a = 0f;
                    spriteRenderer.color = color;
                }
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
            foreach (GameObject fire in fireSprites)
            {
                if (fire != null)
                {
                    float distance = Vector2.Distance(torch.transform.position, fire.transform.position);
                    
                    if (distance <= detectionRange)
                    {
                        RevealFire(fire);
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
        return treeIndex >= 0 && treeIndex < fireSprites.Length && burnedTrees.Contains(fireSprites[treeIndex]);
    }

    private void RevealFire(GameObject fire)
    {
        // Skip if this tree is already burned
        if (burnedTrees.Contains(fire))
        {
            return;
        }

        SpriteRenderer spriteRenderer = fire.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.color.a < 1f)
        {
            // Stop any existing animation for this fire
            if (activeAnimations.ContainsKey(fire) && activeAnimations[fire] != null)
            {
                StopCoroutine(activeAnimations[fire]);
            }

            // Start new animation
            Coroutine newAnimation = StartCoroutine(AnimateFire(fire));
            activeAnimations[fire] = newAnimation;
            Debug.Log($"Started fire animation for: {fire.name}");
        }
    }

    private IEnumerator AnimateFire(GameObject fire)
    {
        SpriteRenderer spriteRenderer = fire.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) yield break;

        // Find corresponding tree object
        int fireIndex = System.Array.IndexOf(fireSprites, fire);
        GameObject treeObject = fireIndex >= 0 && fireIndex < treeObjects.Length ? treeObjects[fireIndex] : null;
        SpriteRenderer treeRenderer = treeObject?.GetComponent<SpriteRenderer>();

        if (treeRenderer == null)
        {
            Debug.LogError($"No tree object found for fire: {fire.name}");
            yield break;
        }

        // Make fire visible
        Color color = spriteRenderer.color;
        color.a = 1f;
        spriteRenderer.color = color;

        float elapsedTime = 0f;
        bool useFirstSprite = true;

        // Animate for burnDuration seconds
        while (elapsedTime < burnDuration)
        {
            // Switch between sprites
            spriteRenderer.sprite = useFirstSprite ? fire1Sprite : fire2Sprite;
            useFirstSprite = !useFirstSprite;

            yield return new WaitForSeconds(switchInterval);
            elapsedTime += switchInterval;
        }

        // Change tree to dead tree sprite
        treeRenderer.sprite = deadTreeSprite;
        burnedTrees.Add(fire);

        // Hide fire effect
        color.a = 0f;
        spriteRenderer.color = color;
        activeAnimations.Remove(fire);
        Debug.Log($"Tree {treeObject.name} changed to dead tree");
    }

    private void HideDeadTree(GameObject tree)
    {
        if (!hiddenTrees.Contains(tree))
        {
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
                    SceneManager.LoadScene(2); // Load the next scene
                    Debug.Log("CONGRATS YOU WON THE GAME");
                }
            }
        }
    }
}
