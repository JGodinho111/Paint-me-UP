using UnityEngine;
using UnityEngine.SceneManagement;

// Reloads the game
public class GameReset : MonoBehaviour
{
    // Called from end menu go again button to restart the game
    public void ReloadCurrentScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }
}
