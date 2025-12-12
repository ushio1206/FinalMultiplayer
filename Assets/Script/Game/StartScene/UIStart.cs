using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class UIStart : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button exitGameButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button returnButton;


    [Header("Lobby Buttons")]
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button joinLobbyButton;
    [SerializeField] private Button cancelHostButton;

    [Header("Canvas")]
    [SerializeField] private GameObject mainOptionPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject lobbyOptions;
    [SerializeField] private GameObject LoadingPanel;

    [Header("UI Settings")]
    [SerializeField] private TMP_Text loadingText;

    [Header("Input Settings")]
    [SerializeField] private TMP_InputField playerInputField;
    [SerializeField] private TMP_InputField lobbyInputField;

    [Header("Game Settings")]
    public string playerName;
    public string lobbyName;

    private NetworkStart _networkStart;
    private Lobby _hostLobby;
    private bool _isHostHeartbeatRunning = false;

    private void Awake()
    {
        _networkStart = GetComponent<NetworkStart>() ?? FindAnyObjectByType<NetworkStart>();

        if (startGameButton != null) startGameButton.onClick.AddListener(StartGame);
        if (creditsButton != null) creditsButton.onClick.AddListener(ShowCredits);
        if (exitGameButton != null) exitGameButton.onClick.AddListener(ExitGame);
        if (returnButton != null) returnButton.onClick.AddListener(ReturnMainOptions);

        if (cancelHostButton != null) cancelHostButton.onClick.AddListener(OnCancelLobbyClicked);
        if (createLobbyButton != null) createLobbyButton.onClick.AddListener(OnCreateLobbyClicked);
        if (joinLobbyButton != null) joinLobbyButton.onClick.AddListener(() => OnJoinLobbyClicked(lobbyInputField?.text ?? string.Empty));
    }

    private async void Start()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
                Debug.Log("UIStart: Unity Services initialized");
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"UIStart: Signed in as {AuthenticationService.Instance.PlayerId}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"UIStart: Initialization failed -> {e}");
        }

        
    }

    private void Update()
    {
        if (!playerInputField && !lobbyInputField) return;

        bool JoinEnable = !string.IsNullOrWhiteSpace(playerInputField.text) && !string.IsNullOrWhiteSpace(lobbyInputField.text);

        if (createLobbyButton != null)
            createLobbyButton.interactable = JoinEnable;

        if (joinLobbyButton != null)
            joinLobbyButton.interactable = JoinEnable;
    }

    private void StartGame()
    {
        mainOptionPanel.SetActive(false);
        lobbyOptions.SetActive(true);
        returnButton.gameObject.SetActive(true);
    }

    private void ShowCredits()
    {
        mainOptionPanel.SetActive(false);
        creditsPanel.SetActive(true);
        lobbyOptions.SetActive(false);
        returnButton.gameObject.SetActive(true);
    }

    private void ReturnMainOptions()
    {
        mainOptionPanel.SetActive(true);
        creditsPanel.SetActive(false);
        lobbyOptions.SetActive(false);
        returnButton.gameObject.SetActive(false);
    }

    private void ExitGame()
    {
        Application.Quit();
    }

    private async void OnCreateLobbyClicked()
    {
        try
        {
            SaveFieldsToSettings();

            if (LoadingPanel != null) LoadingPanel.SetActive(true);
            if (lobbyOptions != null) lobbyOptions.SetActive(false);
            returnButton.gameObject.SetActive(false);
            if (loadingText != null) loadingText.text = "Creating Lobby...";

            string relayJoinCode = await _networkStart.StartHostWithRelayAsync();

            if (string.IsNullOrEmpty(relayJoinCode))
            {
                Debug.LogError("UIStart: Failed to create Relay allocation");
                if (loadingText != null) loadingText.text = "Error: Relay server cannot be created";
                await Task.Delay(3000);
                if (LoadingPanel != null) LoadingPanel.SetActive(false);
                if (lobbyOptions != null) lobbyOptions.SetActive(true);
                returnButton.gameObject.SetActive(true);
                return;
            }

            if (loadingText != null) loadingText.text = "Creating Lobby...";

            string name = string.IsNullOrWhiteSpace(lobbyInputField?.text)
                ? (lobbyName ?? $"Lobby_{UnityEngine.Random.Range(1000, 9999)}")
                : lobbyInputField.text.Trim();
            int maxPlayers = 2;

            var createOptions = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = GetLobbyPlayer(),
                Data = new Dictionary<string, DataObject>
                {
                    { "RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Public, relayJoinCode) }
                }
            };

            _hostLobby = await LobbyService.Instance.CreateLobbyAsync(name, maxPlayers, createOptions);
            Debug.Log($"UIStart: Lobby created - Id:{_hostLobby.Id} Code:{_hostLobby.LobbyCode} Name:{_hostLobby.Name}");

            if (loadingText != null) loadingText.text = "Waiting players...";

            if (_networkStart != null)
            {
                await _networkStart.StartHostWithRelayAsync();
            }

            _isHostHeartbeatRunning = true;
            _ = HostHeartbeatLoopAsync();
        }
        catch (LobbyServiceException le)
        {
            Debug.LogError($"UIStart: Lobby creation failed -> {le}");
            if (LoadingPanel != null) LoadingPanel.SetActive(false);
            if (lobbyOptions != null) lobbyOptions.SetActive(true);
        }
    }

    private async void OnJoinLobbyClicked(string lobbyNameToFind)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(lobbyNameToFind))
            {
                Debug.LogError("UIStart: Lobby name is empty!");
                // Show Message inUI
                if (loadingText != null) loadingText.text = "Error: Lobby name not valid";
                return;
            }

            SaveFieldsToSettings();

            if (LoadingPanel != null) LoadingPanel.SetActive(true);
            if (lobbyOptions != null) lobbyOptions.SetActive(false);
            returnButton.gameObject.SetActive(false);
            if (loadingText != null) loadingText.text = $"Searching lobby '{lobbyNameToFind}'...";

            var query = await LobbyService.Instance.QueryLobbiesAsync(new QueryLobbiesOptions { Count = 25 });
            var found = query.Results.FirstOrDefault(l => string.Equals(l.Name, lobbyNameToFind, StringComparison.OrdinalIgnoreCase));

            if (found == null)
            {
                Debug.LogError($"UIStart: Lobby '{lobbyNameToFind}' not found. Available: {query.Results.Count}");

                if (query.Results.Count > 0)
                {
                    Debug.Log("Available lobbies:");
                    foreach (var lobby in query.Results)
                    {
                        Debug.Log($"  - {lobby.Name} (Players: {lobby.Players.Count}/{lobby.MaxPlayers})");
                    }
                }

                if (loadingText != null)
                {
                    loadingText.text = $"Error: Lobby '{lobbyNameToFind}' not found.\n" +
                        $"{(query.Results.Count > 0 ? $"Available lobbies: {query.Results.Count}" : "No lobbies")}";
                }

                await Task.Delay(3000);
                if (LoadingPanel != null) LoadingPanel.SetActive(false);
                if (lobbyOptions != null) lobbyOptions.SetActive(true);
                returnButton.gameObject.SetActive(true);
                return;
            }

            if (!found.Data.TryGetValue("RelayJoinCode", out var relayCodeData) ||
                string.IsNullOrWhiteSpace(relayCodeData.Value))
            {
                Debug.LogError("UIStart: Lobby missing Relay Join Code!");
                if (loadingText != null) loadingText.text = "Error: Lobby with Relay code not valid";
                await Task.Delay(3000);
                if (LoadingPanel != null) LoadingPanel.SetActive(false);
                if (lobbyOptions != null) lobbyOptions.SetActive(true);
                returnButton.gameObject.SetActive(true);
                return;
            }

            string relayJoinCode = relayCodeData.Value;
            Debug.Log($"UIStart: Found lobby '{found.Name}' with Relay code: {relayJoinCode}");

            if (loadingText != null) loadingText.text = "Joining to lobby...";

            var joinOptions = new JoinLobbyByIdOptions { Player = GetLobbyPlayer() };
            _hostLobby = await LobbyService.Instance.JoinLobbyByIdAsync(found.Id, joinOptions);
            Debug.Log($"UIStart: Joined lobby '{_hostLobby.Name}'");

            if (loadingText != null) loadingText.text = "Conecting...";

            bool connected = await _networkStart.StartClientWithRelayAsync(relayJoinCode);

            if (!connected)
            {
                Debug.LogError("UIStart: Failed to connect via Relay");
                if (loadingText != null) loadingText.text = "Error: Cannot join the server";
                await Task.Delay(3000);
                if (LoadingPanel != null) LoadingPanel.SetActive(false);
                if (lobbyOptions != null) lobbyOptions.SetActive(true);
                returnButton.gameObject.SetActive(true);
            }
            else
            {
                if (loadingText != null) loadingText.text = "Conected! waiting...";
            }
        }
        catch (LobbyServiceException le)
        {
            Debug.LogError($"UIStart: Join lobby failed -> {le}");
            if (LoadingPanel != null) LoadingPanel.SetActive(false);
            if (lobbyOptions != null) lobbyOptions.SetActive(true);
            returnButton.gameObject.SetActive(true);
        }
    }

    private async void OnCancelLobbyClicked()
    {
        try
        {
            if (_hostLobby != null)
            {
                await LobbyService.Instance.DeleteLobbyAsync(_hostLobby.Id);
                _hostLobby = null;
            }
        }
        catch (LobbyServiceException le)
        {
            Debug.LogError($"UIStart: Cancel lobby failed -> {le}");
        }
        finally
        {
            _isHostHeartbeatRunning = false;
            if (LoadingPanel != null) LoadingPanel.SetActive(false);
            if (lobbyOptions != null) lobbyOptions.SetActive(true);
            returnButton.gameObject.SetActive(true);
        }
    }

    private Player GetLobbyPlayer()
    {
        var name = playerName;
        if (string.IsNullOrWhiteSpace(name))
            name = PlayerPrefs.GetString("DesiredPlayerName", $"Player_{UnityEngine.Random.Range(1000, 9999)}");

        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, name) }
            }
        };
    }

    private async Task HostHeartbeatLoopAsync()
    {
        const int heartbeatIntervalSeconds = 15;
        while (_isHostHeartbeatRunning && _hostLobby != null)
        {
            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(_hostLobby.Id);
            }
            catch (LobbyServiceException le)
            {
                Debug.LogWarning($"UIStart: Heartbeat failed -> {le}");
            }

            await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(heartbeatIntervalSeconds));
        }
    }

    private void SaveFieldsToSettings()
    {
        playerName = (playerInputField != null) ? playerInputField.text.Trim() : playerName;
        if (string.IsNullOrEmpty(playerName))
            playerName = $"Player_{UnityEngine.Random.Range(1000, 9999)}";

        PlayerPrefs.SetString("DesiredPlayerName", playerName);
        PlayerPrefs.Save();
        Debug.Log($"UIStart: Player name saved: {playerName}");
    }
}
