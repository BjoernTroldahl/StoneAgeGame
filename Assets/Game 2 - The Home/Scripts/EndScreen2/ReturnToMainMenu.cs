using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToMainMenu : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void LoadMainMenu()
    {
        SceneManager.LoadScene("Start Menu"); // Load the Main Menu scene
    }
}
