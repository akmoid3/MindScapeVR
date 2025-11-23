using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }


    public void LoadCampaignScene()
    {
        LoadScene("Countryside");
    }

    public void LoadBeachScene()
    {
        LoadScene("Beach");
    }

    public void LoadGenerateScene()
    {
        LoadScene("GenerateScene");
    }

    public void LoadMainMenu()
    {
        LoadScene("MainMenu");
    }
    
    
    public void Quit()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

}
