using UnityEngine;
using UnityEngine.SceneManagement;
public class LoadingMiniGames : MonoBehaviour
{
    [SerializeField] private string newGameLevel = "0"; // Name of the scene to load
    public void NewGameButton()
    {
        SceneManager.LoadScene(newGameLevel);
    }
}
