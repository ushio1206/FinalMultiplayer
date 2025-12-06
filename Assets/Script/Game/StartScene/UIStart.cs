using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UIStart : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button exitGameButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button returnButton;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button joinLobbyButton;
    [SerializeField] private Button CancelHostButton;

    [Header("Canvas")]
    [SerializeField] private GameObject mainOptionPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject lobbyOptions;
    [SerializeField] private GameObject LoadingPanel;

    [Header("UI Settings")]
    [SerializeField] private TMP_Text loadingText;

    [Header("Input Settings")]
    [SerializeField] private TMP_InputField playerInputField;

    [Header("Game Settings")]
    public string playerName;
    public string lobbyName;
    private NetworkStart networkStart;

    private void Awake()
    {
        NetworkStart networkStart = GetComponent<NetworkStart>();
    }
    private void Start()
    {
        if (!networkStart) networkStart = GetComponent<NetworkStart>() ?? FindAnyObjectByType<NetworkStart>();

        startGameButton.onClick.AddListener(StartGame);
        creditsButton.onClick.AddListener(ShowCredits);
        exitGameButton.onClick.AddListener(ExitGame);
        returnButton.onClick.AddListener(ReturnMainOptions);
        CancelHostButton.onClick.AddListener(OnCancelLobbyClicked);

        if (createLobbyButton != null)
            createLobbyButton.onClick.AddListener(OnCreateLobbyClicked);
        
        if (joinLobbyButton != null)
            joinLobbyButton.onClick.AddListener(OnJoinLobbyClicked);

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

    private void OnCreateLobbyClicked()
    {
        SaveFieldsToSettings();

        if (LoadingPanel != null) LoadingPanel.SetActive(true);
        if (lobbyOptions != null) lobbyOptions.SetActive(false);

        loadingText.text = "Waiting for players to join...";

        if (networkStart != null)
        {
            networkStart.StartHost();
            return;
        }
    }

    private void OnJoinLobbyClicked()
    {
        SaveFieldsToSettings();

        if (LoadingPanel != null) LoadingPanel.SetActive(true);
        if (lobbyOptions != null) lobbyOptions.SetActive(false);

        if (networkStart != null)
        {
            networkStart.StartClient();
            return;
        }
    }

    private void OnCancelLobbyClicked()
    {
        // Logic to cancel hosting or joining a lobby can be implemented here
    }

    private void SaveFieldsToSettings()
    {
        playerName = (playerInputField != null) ? playerInputField.text.Trim() : playerName;

        if (string.IsNullOrEmpty(playerName))
            playerName = $"Player_{ Random.Range(1000, 9999)}";

        PlayerPrefs.SetString("DesiredPlayerName", playerName);
        PlayerPrefs.Save();

        Debug.Log($"UIStart: Player name: {playerName}");
    }
}
