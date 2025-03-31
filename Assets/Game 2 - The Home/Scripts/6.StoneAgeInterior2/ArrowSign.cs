using UnityEngine;
using UnityEngine.SceneManagement;

public class ArrowSign : MonoBehaviour
{
    private void OnMouseDown()
    {
        if (gameObject.activeSelf)
        {
            Debug.Log("CONGRATS YOU WON THE LEVEL");
            SceneManager.LoadScene(8);
        }
    }
}