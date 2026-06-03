using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject mainUI, creditsUI;
    public void Play()
    {
        SceneManager.LoadScene("Main");
    }

    public void Credits()
    {
        mainUI.SetActive(false);
        creditsUI.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void Back()
    {
        mainUI.SetActive(true);
        creditsUI.SetActive(false);
    }
}
