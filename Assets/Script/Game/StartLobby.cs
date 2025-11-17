using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using Unity.VisualScripting;

public class StartLobby : NetworkBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button exitGameButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button returnButton;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button joinLobbyButton;

    [Header("Canvas")]
    [SerializeField] private GameObject mainOptionPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject lobbyOptions;

    [Header("Lobby")]
    [SerializeField] private InputField playerNameInput;
    [SerializeField] private InputField lobbyNameInput;

    [Header("Game Settings")]
    public string playerName;
    public string lobbyName;
    private string lobbyID;

    private void Start()
    {
        startGameButton.onClick.AddListener(StartGame);
        creditsButton.onClick.AddListener(ShowCredits);
        exitGameButton.onClick.AddListener(ExitGame);
        returnButton.onClick.AddListener(ReturnMainOptions);
        createLobbyButton.onClick.AddListener(CreateLobby);
        joinLobbyButton.onClick.AddListener(JoinLobby);

        playerName = lobbyNameInput.text;
        lobbyName = lobbyNameInput.text;

    }

    private void StartGame()
    {
        mainOptionPanel.SetActive(false);
        lobbyOptions.SetActive(true);
    }

    private void ShowCredits()
    {
        mainOptionPanel.SetActive(false);
        creditsPanel.SetActive(true);
    }

    private void ReturnMainOptions()
    {
        mainOptionPanel.SetActive(true);
        creditsPanel.SetActive(false);
    }

    private void ExitGame()
    {
        Application.Quit();
    }

    private void CreateLobby()
    {
        if(lobbyNameInput.text == null || lobbyNameInput.text == string.Empty || playerNameInput == null || playerNameInput.text == string.Empty)
        {
            Debug.LogError("Player name or Lobby Name is incorrect");
            return;
        }

        lobbyName = lobbyNameInput.text;
        playerName = playerNameInput.text;

        print($"{playerName} is connected in {lobbyName}");
    }

    private void JoinLobby()
    {

    }
}
