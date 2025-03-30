using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class RestartGame1 : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(WaitAndRestart());
    }

    private IEnumerator WaitAndRestart()
    {
        // Wait for 5 seconds
        yield return new WaitForSeconds(5f);
        // Load scene 1
        SceneManager.LoadScene(1);
    }
}