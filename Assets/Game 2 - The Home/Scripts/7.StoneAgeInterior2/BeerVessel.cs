using UnityEngine;

public class BeerVessel : MonoBehaviour
{
    [SerializeField] private Sprite beerUncovered;
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
            DragPorridge.UncoverBeerVessel(spriteRenderer, beerUncovered);
            isUncovered = true;
        }
    }
}
