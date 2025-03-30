using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Add TextMeshPro namespace
using System.Collections;

public class RestartGame1 : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI countdownText; // Change to TextMeshProUGUI
    [SerializeField] private float waitTime = 5f; // Configurable wait time

    private void Start()
    {
        if (countdownText == null)
        {
            Debug.LogError("Countdown Text reference is missing!");
            return;
        }
        StartCoroutine(WaitAndRestart());
    }

    private IEnumerator WaitAndRestart()
    {
        float timeRemaining = waitTime;
        
        while (timeRemaining > 0)
        {
            // Update UI text with current time (rounded to whole number)
            countdownText.text = $"Restarting in {Mathf.CeilToInt(timeRemaining)}...";
            
            // Wait for next frame
            yield return null;
            
            // Decrease time
            timeRemaining -= Time.deltaTime;
        }

        // Ensure final text shows 0
        countdownText.text = "Restarting...";
        
        // Load scene 1
        SceneManager.LoadScene(1);
    }
}