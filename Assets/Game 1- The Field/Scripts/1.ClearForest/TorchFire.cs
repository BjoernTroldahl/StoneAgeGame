using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class TorchFire : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private ParticleSystem[] fireParticleSystems;  // Assign fire particle systems in inspector
    [SerializeField] private GameObject[] treeObjects;  // Add reference to tree objects
    [SerializeField] private float detectionRange = 2f; // Range at which torch triggers fire
    [SerializeField] private GameObject torch;          // Reference to the torch object
    [SerializeField] private float axeDetectionRange = 1.5f; // Range for axe to detect dead trees

    [Header("Tree Settings")]
    [SerializeField] private Sprite deadTreeSprite;    // Add this field
    [SerializeField] private float burnDuration = 5f;  // How long the particle system runs before tree burns

    private Dictionary<GameObject, Coroutine> activeAnimations = new Dictionary<GameObject, Coroutine>();
    private HashSet<GameObject> burnedTrees = new HashSet<GameObject>();  // Track burned trees
    private HashSet<GameObject> hiddenTrees = new HashSet<GameObject>();  // Track hidden trees
    private Dragging torchScript; // Reference to the Dragging script

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

        // Start fire particle system
        if (fireParticles != null)
        {
            // Activate and start the particle system
            fireParticles.gameObject.SetActive(true);
            fireParticles.Play();
            
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

        // Gradually fade out the particle system
        float fadeTime = 1.0f;
        float elapsedTime = 0f;
        ParticleSystem.MainModule main = fireParticles.main;
        
        // Store original start lifetime
        float originalStartLifetime = main.startLifetime.constant;
        
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeTime;
            
            // Gradually reduce particle lifetime to fade out
            main.startLifetime = Mathf.Lerp(originalStartLifetime, 0, t);
            
            yield return null;
        }
        
        // Stop emitting new particles but let existing ones finish
        fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        
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
                    Debug.Log("CONGRATS YOU WON THE GAME - Loading next scene in 2 seconds");
                    StartCoroutine(DelayedSceneLoad());
                }
            }
        }
    }
    
    private IEnumerator DelayedSceneLoad()
    {
        // Wait for 2 seconds before loading next scene
        yield return new WaitForSeconds(0f);
        SceneManager.LoadScene(2); // Load the next scene
    }
}
