using System.Collections.Specialized;
using System.Security.Cryptography;
using UnityEngine;

public class Drag : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject seedPrefab;
    [SerializeField] private Vector2 spawnPosition = new Vector2(15.65f, -5.46f);
    private static int seedCount = 0; // Track total seeds
    private const int MAX_SEEDS = 3; // Maximum number of clones (not including original seed)
    
    public delegate void DragEndedDelegate(Drag draggableObject);
    public DragEndedDelegate dragEndedCallback;
    private bool dragging = false;
    private bool isLocked = false;
    private Vector3 offset;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("No SpriteRenderer found on draggable object!");
            return;
        }
        // Initialize seed count for the first seed
        if (seedCount == 0)
        {
            seedCount = 1;
            Debug.Log("Initial seed counted: 1");
        }
    }

    void Update()
    {
        if (dragging && !isLocked)
        {
            transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition) + offset;
        }
    }

    private void OnMouseDown()
    {
        if (spriteRenderer != null && spriteRenderer.color.a > 0 && !isLocked)
        {
            offset = transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
            dragging = true;
        }
    }

    private void OnMouseUp()
    {
        if (dragging)
        {
            dragging = false;
            if (dragEndedCallback != null)
            {
                dragEndedCallback(this);
            }
        }
    }

    public void LockInPlace()
    {
        if (!isLocked)
        {
            isLocked = true;
            dragging = false;
            
            // Only spawn a new seed if we haven't reached the maximum
            if (seedCount < MAX_SEEDS)
            {
                SpawnNewSeed();
            }
        }
    }

    private void SpawnNewSeed()
    {
        if (seedPrefab != null)
        {
            seedCount++; // Increment count before spawning
            Debug.Log($"Spawning seed #{seedCount} of {MAX_SEEDS + 1}");
            
            GameObject newSeed = Instantiate(seedPrefab, spawnPosition, Quaternion.identity);
            Drag newDrag = newSeed.GetComponent<Drag>();
            if (newDrag != null)
            {
                newDrag.dragEndedCallback = this.dragEndedCallback;
            }
        }
    }
}
