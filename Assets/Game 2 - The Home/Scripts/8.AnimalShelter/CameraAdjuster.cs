using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraAdjuster : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private SpriteRenderer backgroundSprite;
    [SerializeField] private Vector2 referenceResolution = new Vector2(3840, 2160);
    [SerializeField] private float pixelsPerUnit = 100f;

    private Camera mainCamera;

    private void Start()
    {
        if (backgroundSprite == null)
        {
            Debug.LogError("Background sprite reference is missing!");
            return;
        }

        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            Debug.LogError("Camera component not found!");
            return;
        }

        AdjustCamera();
    }

    private void AdjustCamera()
    {
        float targetAspect = referenceResolution.x / referenceResolution.y;
        float screenAspect = (float)Screen.width / Screen.height;
        float scaleHeight = screenAspect / targetAspect;

        // Calculate the orthographic size needed to fully cover the view
        float orthoSize = (referenceResolution.y / 2f) / pixelsPerUnit;

        if (scaleHeight > 1f)
        {
            // Screen is wider than target - adjust height to fill screen
            mainCamera.orthographicSize = orthoSize * scaleHeight;
        }
        else
        {
            // Screen is taller or same ratio - use base ortho size
            mainCamera.orthographicSize = orthoSize;
        }

        // Center camera on sprite
        Vector3 spriteWorldCenter = backgroundSprite.transform.position;
        transform.position = new Vector3(spriteWorldCenter.x, spriteWorldCenter.y, -10f);

        Debug.Log($"Camera adjusted - Orthographic size: {mainCamera.orthographicSize}");
    }

    private void OnValidate()
    {
        if (mainCamera != null && backgroundSprite != null)
        {
            AdjustCamera();
        }
    }
}

