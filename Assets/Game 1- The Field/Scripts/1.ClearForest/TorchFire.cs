using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TorchFire : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject[] fireSprites;  // Assign fire sprites in inspector
    [SerializeField] private float detectionRange = 2f; // Range at which torch triggers fire
    [SerializeField] private GameObject torch;          // Reference to the torch object

    [Header("Fire Animation")]
    [SerializeField] private Sprite fire1Sprite;    // Assign fire1_0 sprite in inspector
    [SerializeField] private Sprite fire2Sprite;    // Assign fire2_0 sprite in inspector
    [SerializeField] private float switchInterval = 0.3f;
    [SerializeField] private float burnDuration = 5f;

    private Dictionary<GameObject, Coroutine> activeAnimations = new Dictionary<GameObject, Coroutine>();

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
    }

    private void Update()
    {
        if (torch != null)
        {
            // Check distance between torch and each fire sprite
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
    }

    private void RevealFire(GameObject fire)
    {
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

        // Hide fire after animation
        color.a = 0f;
        spriteRenderer.color = color;
        activeAnimations.Remove(fire);
        Debug.Log($"Finished fire animation for: {fire.name}");
    }
}
