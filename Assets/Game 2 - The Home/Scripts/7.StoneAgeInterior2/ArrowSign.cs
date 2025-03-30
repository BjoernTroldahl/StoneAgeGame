using UnityEngine;

public class ArrowSign : MonoBehaviour
{
    private void OnMouseDown()
    {
        if (gameObject.activeSelf)
        {
            Debug.Log("CONGRATS YOU WON THE LEVEL");
        }
    }
}