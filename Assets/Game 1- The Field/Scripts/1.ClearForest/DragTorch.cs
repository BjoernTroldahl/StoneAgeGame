using UnityEngine;

public class Dragging : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Sprite axeSprite; // Reference to the axe sprite
    [SerializeField] private GameObject[] treesToBurn; // Array of trees that need to be burned
    [SerializeField] private Sprite deadTreeSprite; // Reference to identify the dead tree sprite
    
    private bool isDragging = false;
    private Vector3 offset;
    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;
    private bool hasTransformedToAxe = false;
    private Collider2D axeCollider; // Reference to the collider

    // Add this public method to expose the transformation state
    public bool HasTransformedToAxe()
    {
        return hasTransformedToAxe;
    }

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
        }
        
        // Get the SpriteRenderer component
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer component not found on this object!");
        }
        
        // If trees array is empty, find all trees with appropriate tag
        if (treesToBurn == null || treesToBurn.Length == 0)
        {
            treesToBurn = GameObject.FindGameObjectsWithTag("BurnableTree");
            Debug.Log($"Found {treesToBurn.Length} trees with tag 'BurnableTree'");
        }
        
        // Ensure we have a reference to the dead tree sprite
        if (deadTreeSprite == null)
        {
            Debug.LogError("Dead tree sprite reference is missing! Please assign it in the inspector.");
        }

        // Get or add a collider
        axeCollider = GetComponent<Collider2D>();
        if (axeCollider == null)
        {
            axeCollider = gameObject.AddComponent<BoxCollider2D>();
        }
        
        // Make it a trigger
        axeCollider.isTrigger = true;
    }

    void Update()
    {
        if (isDragging)
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            transform.position = new Vector3(mousePosition.x + offset.x, mousePosition.y + offset.y, transform.position.z);
        }
        
        // Check if all trees are burned (have dead tree sprite) and we haven't transformed yet
        if (!hasTransformedToAxe && AllTreesBurned())
        {
            TransformToAxe();
        }
    }

    private void OnMouseDown()
    {
        Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        offset = transform.position - mousePosition;
        isDragging = true;
    }

    private void OnMouseUp()
    {
        isDragging = false;
    }
    
    // Check if all trees have the dead tree sprite
    private bool AllTreesBurned()
    {
        if (treesToBurn == null || treesToBurn.Length == 0)
        {
            Debug.LogWarning("No trees to burn found!");
            return false;
        }
        
        if (deadTreeSprite == null)
        {
            Debug.LogError("Dead tree sprite reference is null! Cannot check tree states.");
            return false;
        }
        
        int burnedCount = 0;
        int totalTrees = treesToBurn.Length;
        
        for (int i = 0; i < totalTrees; i++)
        {
            GameObject tree = treesToBurn[i];
            
            if (tree == null)
            {
                Debug.LogWarning($"Tree at index {i} is null!");
                continue;
            }
            
            SpriteRenderer treeRenderer = tree.GetComponent<SpriteRenderer>();
            if (treeRenderer == null)
            {
                Debug.LogWarning($"Tree at index {i} has no SpriteRenderer!");
                continue;
            }
            
            // Compare sprite names instead of sprite references
            // This handles cases where Unity creates different instances of the same sprite
            bool isDeadTree = treeRenderer.sprite.name == deadTreeSprite.name;
            
            if (isDeadTree)
            {
                burnedCount++;
                Debug.Log($"Tree {i} has dead sprite: {treeRenderer.sprite.name}");
            }
            else
            {
                Debug.Log($"Tree {i} has NOT changed to dead sprite yet. Current: {treeRenderer.sprite.name}");
            }
        }
        
        Debug.Log($"Burned trees: {burnedCount}/{totalTrees}");
        
        // Only transform when ALL trees are burned
        return burnedCount >= totalTrees;
    }
    
    // Transform the torch into an axe
    private void TransformToAxe()
    {
        if (axeSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = axeSprite;
            hasTransformedToAxe = true;
            
            // Adjust the collider size if needed
            if (axeCollider != null && axeCollider is BoxCollider2D boxCollider)
            {
                boxCollider.size = spriteRenderer.bounds.size;
                boxCollider.offset = Vector2.zero;
            }
            
            Debug.Log("Transformed torch to axe!");
            
            // Optional: Play a transformation sound or effect
            // AudioSource.PlayClipAtPoint(transformSound, transform.position);
        }
        else
        {
            Debug.LogError("Unable to transform to axe - missing sprite or renderer!");
        }
    }
}
