using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuUI : MonoBehaviour
{
    [SerializeField] private Button menuButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button exitGameButton;
    [SerializeField] private GameObject menuPanel;

    private string mainMenuSceneName = "MainMenu";

    private void Start()
    {
        menuPanel.SetActive(false);
        
        menuButton.onClick.AddListener(OpenMenu);
        resumeButton.onClick.AddListener(CloseMenu);
        mainMenuButton.onClick.AddListener(ReturnMainMenu);
        exitGameButton.onClick.AddListener(ExitGame);
    }

    private void OpenMenu()
    {
        menuPanel.SetActive(true);
        menuButton.gameObject.SetActive(false);
    }

    private void CloseMenu()
    {
        menuPanel.SetActive(false);
        menuButton.gameObject.SetActive(true);
    }

    private void ReturnMainMenu()
    {
        print($"Player returned to the main menú");
        // SceneManager.LoadScene(mainMenuSceneName);
    }

    private void ExitGame()
    {
        print($"Player left the game");
        // Application.Quit();
    }
}
