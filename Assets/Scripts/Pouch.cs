using UnityEngine;

public class Pouch : MonoBehaviour
{
    [Header("Sprite References")]
    [SerializeField] private Sprite closedPouchSprite;
    [SerializeField] private Sprite openPouchSprite;

    [Header("Seed Reference")]
    [SerializeField] private GameObject wheatSeed;  // Reference to the wheat seed object

    private SpriteRenderer spriteRenderer;
    private bool isOpen = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("No SpriteRenderer found on Pouch object!");
            return;
        }

        // Set initial sprite to closed pouch
        spriteRenderer.sprite = closedPouchSprite;

        // Hide the wheat seed at start
        if (wheatSeed != null)
        {
            SpriteRenderer seedRenderer = wheatSeed.GetComponent<SpriteRenderer>();
            if (seedRenderer != null)
            {
                Color seedColor = seedRenderer.color;
                seedColor.a = 0f; // Make fully transparent
                seedRenderer.color = seedColor;
            }
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            if (hit.collider != null && hit.collider.gameObject == gameObject && !isOpen)
            {
                OpenPouch();
            }
        }
    }

    private void OpenPouch()
    {
        isOpen = true;
        spriteRenderer.sprite = openPouchSprite;

        // Show the wheat seed
        if (wheatSeed != null)
        {
            SpriteRenderer seedRenderer = wheatSeed.GetComponent<SpriteRenderer>();
            if (seedRenderer != null)
            {
                Color seedColor = seedRenderer.color;
                seedColor.a = 1f; // Make fully visible
                seedRenderer.color = seedColor;
            }
        }

        Debug.Log("Pouch opened and seed revealed!");
    }
}
