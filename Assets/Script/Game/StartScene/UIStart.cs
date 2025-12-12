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
            if (loadingText != null) loadingText.text = "Creando lobby...";

            string name = string.IsNullOrWhiteSpace(lobbyInputField?.text) ? (lobbyName ?? $"Lobby_{UnityEngine.Random.Range(1000, 9999)}") : lobbyInputField.text.Trim();
            int maxPlayers = 2;

            var createOptions = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = GetLobbyPlayer()
            };

            _hostLobby = await LobbyService.Instance.CreateLobbyAsync(name, maxPlayers, createOptions);
            Debug.Log($"UIStart: Lobby created - Id:{_hostLobby.Id} Code:{_hostLobby.LobbyCode} Name:{_hostLobby.Name}");

            if (loadingText != null) loadingText.text = "Esperando jugadores...";

            if (_networkStart != null)
            {
                _networkStart.StartHost();
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
                if (loadingText != null) loadingText.text = "Error: Ingresa un nombre de lobby";
                return;
            }

            SaveFieldsToSettings();

            if (LoadingPanel != null) LoadingPanel.SetActive(true);
            if (lobbyOptions != null) lobbyOptions.SetActive(false);
            if (loadingText != null) loadingText.text = $"Buscando lobby '{lobbyNameToFind}'...";

            var query = await LobbyService.Instance.QueryLobbiesAsync(new QueryLobbiesOptions { Count = 25 });
            var found = query.Results.FirstOrDefault(l => string.Equals(l.Name, lobbyNameToFind, StringComparison.OrdinalIgnoreCase));

            if (found == null)
            {
                // ERROR: Lobby not found
                Debug.LogError($"UIStart: No lobby found with name '{lobbyNameToFind}'. Available lobbies: {query.Results.Count}");

                // show available lobbies in the console
                if (query.Results.Count > 0)
                {
                    Debug.Log("Available lobbies:");
                    foreach (var lobby in query.Results)
                    {
                        Debug.Log($"  - {lobby.Name} (Id: {lobby.Id}, Players: {lobby.Players.Count}/{lobby.MaxPlayers})");
                    }
                }

                // Update UI
                if (loadingText != null)
                {
                    loadingText.text = $"Error: Lobby '{lobbyNameToFind}' no encontrado.\n{(query.Results.Count > 0 ? $"Lobbies disponibles: {query.Results.Count}" : "No hay lobbies disponibles")}";
                }

                // Show option to retry after delay
                await System.Threading.Tasks.Task.Delay(3000);
                if (LoadingPanel != null) LoadingPanel.SetActive(false);
                if (lobbyOptions != null) lobbyOptions.SetActive(true);
                return;
            }

            var joinOptions = new JoinLobbyByIdOptions { Player = GetLobbyPlayer() };
            _hostLobby = await LobbyService.Instance.JoinLobbyByIdAsync(found.Id, joinOptions);
            Debug.Log($"UIStart: Successfully joined lobby - Id:{_hostLobby.Id} Name:{_hostLobby.Name}");

            if (_networkStart != null)
            {
                _networkStart.StartClient();
            }
        }
        catch (LobbyServiceException le)
        {
            Debug.LogError($"UIStart: Join lobby failed -> {le}");
            if (LoadingPanel != null) LoadingPanel.SetActive(false);
            if (lobbyOptions != null) lobbyOptions.SetActive(true);
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
