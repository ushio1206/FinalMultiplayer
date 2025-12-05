using UnityEngine;
using Unity.Netcode;

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

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton != null && _clientSubscribed == true)
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
            // Initialize with current count
            connectedPlayers.Value = NetworkManager.Singleton.ConnectedClients.Count;

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
        else
        {
            connectedPlayers.OnValueChanged += OnConnectedPlayersChanged;
            return;
        }
    }

    public void StartHost()
    {
        if(NetworkManager.Singleton == null)
        {
            Debug.LogError("No NetworkManager found in the scene.");
            return; 
        }

        if (!_clientSubscribed)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            _clientSubscribed = true;
        }

        // Only Server/Host will load the game scene
        Debug.Log("Network: Starting Host...");
        NetworkManager.Singleton.StartHost();
    }

    public void StartClient()
    {
        if(NetworkManager.Singleton == null)
        {
            Debug.LogError("No NetworkManager found in the scene.");
            return; 
        }

        Debug.Log("Network: Starting Client...");
        NetworkManager.Singleton.StartClient();
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton) return;
        if (!NetworkManager.Singleton.IsServer) return;

        connectedPlayers.Value = NetworkManager.Singleton.ConnectedClients.Count;

        if (connectedPlayers.Value >= _requiredPlayers)
            NetworkManager.Singleton.SceneManager.LoadScene(_sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);

        Debug.Log($"[Server] Client connected: {clientId}");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;
        connectedPlayers.Value = NetworkManager.Singleton.ConnectedClients.Count;
    }

    private void OnConnectedPlayersChanged(int previousValue, int currentValue)
    {
        Debug.Log($"Connected Players changed from {previousValue} to {currentValue}");

    }
}
