using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject mainUI, creditsUI;

    //Called when play is clicked
    public void Play()
    {
        SceneManager.LoadScene("Main");
    }

    //Called when credits is clicked
    public void Credits()
    {
        mainUI.SetActive(false);
        creditsUI.SetActive(true);
    }

    //Called when quit game is called
    public void QuitGame()
    {
        Application.Quit();
    }

    //Called when back is clicked in the credits menu
    public void Back()
    {
        mainUI.SetActive(true);
        creditsUI.SetActive(false);
    }
}
