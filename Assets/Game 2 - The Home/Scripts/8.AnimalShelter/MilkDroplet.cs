using UnityEngine;
using System.Collections;
public class MilkDroplet : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float fallDistance = 2f;
    [SerializeField] private float fallDuration = 1f;
    
    private SpriteRenderer spriteRenderer;
    private Vector3 startPosition;
    private Vector3 endPosition;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("No SpriteRenderer found on milk droplet!");
            return;
        }

        spriteRenderer.enabled = false;
        startPosition = transform.position;
        endPosition = startPosition + Vector3.down * fallDistance;
    }

    public void TriggerDropletFall()
    {
        StartCoroutine(FallAnimation());
    }

    private IEnumerator FallAnimation()
    {
        // Reset position and enable sprite
        transform.position = startPosition;
        spriteRenderer.enabled = true;
        Debug.Log("Droplet started falling");

        float elapsedTime = 0f;
        while (elapsedTime < fallDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fallDuration;
            
            transform.position = Vector3.Lerp(startPosition, endPosition, progress);
            yield return null;
        }

        spriteRenderer.enabled = false;
        Debug.Log("Droplet finished falling");
    }
}