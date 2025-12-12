using System;
using Unity.Netcode;
using Unity.Services.Core;
using UnityEngine;

public class NetworkStart : NetworkBehaviour
{
    public NetworkVariable<int> connectedPlayers = new NetworkVariable<int>(
        0,
        writePerm: NetworkVariableWritePermission.Server,
        readPerm: NetworkVariableReadPermission.Everyone
    );

    
    private int _requiredPlayers = 2;
    private bool _clientSubscribed = false;
    private string _sceneName = "GameScene";

    private async void Awake()
    {
        try
        {
            // Inicializar Unity Services si aún no se ha hecho
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
                Debug.Log("NetworkStart: Unity Services initialized");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"NetworkStart: Unity Services init failed -> {e}");
        }
    }

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton != null && _clientSubscribed)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            _clientSubscribed = false;
        }

        if (!IsServer)
        {
            connectedPlayers.OnValueChanged -= OnConnectedPlayersChanged;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!NetworkManager.Singleton) return;

        if (IsServer)
        {
            connectedPlayers.Value = NetworkManager.Singleton.ConnectedClients.Count;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
        else
        {
            connectedPlayers.OnValueChanged += OnConnectedPlayersChanged;
        }
    }

    public void StartHost()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkStart: No NetworkManager found");
            return;
        }

        if (!_clientSubscribed)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            _clientSubscribed = true;
        }

        Debug.Log("NetworkStart: Starting Host...");
        NetworkManager.Singleton.StartHost();
    }

    public void StartClient()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkStart: No NetworkManager found");
            return;
        }

        Debug.Log("NetworkStart: Starting Client...");
        NetworkManager.Singleton.StartClient();
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsServer) return;

        connectedPlayers.Value = NetworkManager.Singleton.ConnectedClients.Count;
        Debug.Log($"[Server] Client {clientId} connected. Total: {connectedPlayers.Value}");

        if (connectedPlayers.Value >= _requiredPlayers)
        {
            Debug.Log("[Server] Required players reached. Loading game scene...");
            NetworkManager.Singleton.SceneManager.LoadScene(_sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;
        connectedPlayers.Value = NetworkManager.Singleton.ConnectedClients.Count;
        Debug.Log($"[Server] Client {clientId} disconnected. Total: {connectedPlayers.Value}");
    }

    private void OnConnectedPlayersChanged(int previousValue, int currentValue)
    {
        Debug.Log($"[Client] Connected players: {previousValue} -> {currentValue}");
    }
}
