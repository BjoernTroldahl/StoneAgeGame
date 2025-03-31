using UnityEngine;
using UnityEngine.SceneManagement;

public class DragGrain : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform circle1;
    [SerializeField] private Transform circle2;
    [SerializeField] private Transform circle3;  // Add Circle 3
    [SerializeField] private SpriteRenderer millstone;
    [SerializeField] private Sprite flourSprite;
    [SerializeField] private SpriteRenderer arrowSign;
    [SerializeField] private GameObject grainPrefab;  // Add grain prefab
    [SerializeField] private Vector3 spawnPosition;   // Add spawn position

    [Header("Settings")]
    [SerializeField] private float snapDistance = 1f;
    [SerializeField] private int requiredFlips = 10;

    [Header("Click Settings")]
    [SerializeField] private float clickTimeWindow = 0.5f;
    private float lastClickTime;

    private bool isDragging = false;
    private Vector3 offset;
    private Camera mainCamera;
    private bool isSnappedToCircle1 = false;
    private bool isSnappedToCircle2 = false;
    private bool isMillingComplete = false;
    private int flipCounter = 0;
    private BoxCollider2D millstoneCollider;
    private SpriteRenderer grainRenderer;

    private static bool isSecondGrain = false;  // Track if this is the second grain
    private static bool firstGrainComplete = false;  // Track if first grain is done

    void Start()
    {
        mainCamera = Camera.main;
        grainRenderer = GetComponent<SpriteRenderer>();
        millstoneCollider = millstone.GetComponent<BoxCollider2D>();

        // Hide millstone and arrow at start
        millstone.enabled = false;
        millstoneCollider.enabled = false;
        arrowSign.enabled = false;

        lastClickTime = 0f;
    }

    void Update()
    {
        if (isDragging && !isSnappedToCircle2)
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 newPosition = new Vector3(mousePosition.x + offset.x, mousePosition.y + offset.y, transform.position.z);
            transform.position = newPosition;

            // Check for Circle 1 snap if not milling complete
            if (!isMillingComplete && Vector2.Distance(newPosition, circle1.position) < snapDistance)
            {
                transform.position = circle1.position;
                isSnappedToCircle1 = true;
                isDragging = false;
                EnableMillstone();
                return;
            }

            // Check for final position snap if milling is complete
            if (isMillingComplete)
            {
                Transform targetCircle = isSecondGrain ? circle3 : circle2;
                if (Vector2.Distance(newPosition, targetCircle.position) < snapDistance)
                {
                    if (isSecondGrain)
                    {
                        SnapToCircle3();
                    }
                    else
                    {
                        SnapToCircle2();
                    }
                    return;
                }
            }
        }

        // Handle millstone clicking with time window
        if (isSnappedToCircle1 && !isMillingComplete && Input.GetMouseButtonDown(0))
        {
            Vector2 clickPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(clickPosition, Vector2.zero);

            if (hit.collider == millstoneCollider)
            {
                float timeSinceLastClick = Time.time - lastClickTime;
                if (timeSinceLastClick <= clickTimeWindow)
                {
                    FlipMillstone();
                }
                lastClickTime = Time.time;
            }
        }

        // Modify arrow clicking to only work after second grain is complete
        if (isSnappedToCircle2 && firstGrainComplete && Input.GetMouseButtonDown(0))
        {
            Vector2 clickPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(clickPosition, Vector2.zero);

            if (hit.collider != null && hit.collider.gameObject == arrowSign.gameObject)
            {
                Debug.Log("CONGRATS YOU WON THE LEVEL");
                SceneManager.LoadScene(7);
            }
        }
    }

    private void EnableMillstone()
    {
        millstone.enabled = true;
        millstoneCollider.enabled = true;
        Debug.Log("Millstone activated");
    }

    private void FlipMillstone()
    {
        flipCounter++;
        millstone.flipY = !millstone.flipY;
        Debug.Log($"Flip counter: {flipCounter}/{requiredFlips}");

        if (flipCounter >= requiredFlips)
        {
            CompleteMilling();
        }
    }

    private void CompleteMilling()
    {
        isMillingComplete = true;
        millstone.enabled = false;
        millstoneCollider.enabled = false;
        grainRenderer.sprite = flourSprite;
        isDragging = true;
        Debug.Log("Milling complete - Converting to flour");
    }

    private void SnapToCircle2()
    {
        transform.position = circle2.position;
        isSnappedToCircle2 = true;
        isDragging = false;
        firstGrainComplete = true;

        // Spawn second grain
        if (!isSecondGrain)
        {
            SpawnSecondGrain();
        }
        
        Debug.Log("First grain snapped to Circle 2");
    }

    private void SnapToCircle3()
    {
        transform.position = circle3.position;
        isSnappedToCircle2 = true;  // Reuse this flag for completion state
        isDragging = false;
        arrowSign.enabled = true;  // Only enable arrow after second grain is complete
        Debug.Log("Second grain snapped to Circle 3 - Arrow sign enabled");
    }

    private void SpawnSecondGrain()
    {
        if (grainPrefab != null)
        {
            GameObject newGrain = Instantiate(grainPrefab, spawnPosition, Quaternion.identity);
            DragGrain newGrainScript = newGrain.GetComponent<DragGrain>();
            if (newGrainScript != null)
            {
                newGrainScript.SetAsSecondGrain();
            }
            Debug.Log("Spawned second grain");
        }
    }

    public void SetAsSecondGrain()
    {
        isSecondGrain = true;
    }

    private void OnMouseDown()
    {
        if (!isSnappedToCircle2)
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            offset = transform.position - mousePosition;
            isDragging = true;
        }
    }

    private void OnMouseUp()
    {
        isDragging = false;
    }
}
