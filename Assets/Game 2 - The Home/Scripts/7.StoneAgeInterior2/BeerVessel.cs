using UnityEngine;

public class BeerVessel : MonoBehaviour
{
    [SerializeField] private Sprite beerUncovered;
    [SerializeField] private DragPorridge porridgeScript; // Add reference to porridge script
    private SpriteRenderer spriteRenderer;
    private bool isUncovered = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnMouseDown()
    {
        if (!isUncovered)
        {
            DragPorridge.UncoverBeerVessel(spriteRenderer, beerUncovered, porridgeScript);
            isUncovered = true;
        }
    }
}
