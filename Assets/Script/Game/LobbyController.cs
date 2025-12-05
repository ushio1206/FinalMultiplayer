using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class LobbyController : NetworkBehaviour
{
    [Header("UI Loading Settings")]
    [SerializeField] private Canvas loadingCanvas;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private Button cancelButton;

    [Header("Game Settings")]
    private int _requiredPlayers = 2;
    private string _gameSceneName = "GameScene";

    public NetworkVariable<int> connectedPlayers = new NetworkVariable<int>(
        writePerm: NetworkVariableWritePermission.Server,
        readPerm: NetworkVariableReadPermission.Everyone
    );

    private void Awake()
    {
        cancelButton.onClick.AddListener(OnLobbyCancel);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!NetworkManager.Singleton) return;
        if (IsServer)
        {
            // Initialize with current count
            connectedPlayers.Value = NetworkManager.Singleton.ConnectedClients.Count;

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedServer;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedServer;
        }
        else 
        {
            connectedPlayers.OnValueChanged += OnConnectedPlayersChanged;
            return;
        }
    }

    public override void OnDestroy()
    {
        if(NetworkManager.Singleton != null && IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedServer;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectedServer;
        }

        if(!IsServer)
        {
            connectedPlayers.OnValueChanged -= OnConnectedPlayersChanged;
        }
    }

    // Run server when client is connected
    private void OnClientConnectedServer(ulong clientId)
    {
        if (!IsServer) return;

        connectedPlayers.Value = NetworkManager.Singleton.ConnectedClients.Count;

        if (connectedPlayers.Value >= _requiredPlayers)
        {
            // Server loads the game scene when enough players are connected
            NetworkManager.Singleton.SceneManager.LoadScene(_gameSceneName, LoadSceneMode.Single);
        }
    }

    // Run server when client is disconnected
    private void OnClientDisconnectedServer(ulong clientId)
    {
        if (!IsServer) return;
        connectedPlayers.Value = NetworkManager.Singleton.ConnectedClients.Count;
    }

    private void OnConnectedPlayersChanged(int previousValue, int currentValue)
    {
        Debug.Log($"Connected Players changed from {previousValue} to {currentValue}");

        loadingText.text = $"Connected Players: {currentValue}/{_requiredPlayers}\nWaiting for more players...";
    }

    private void OnLobbyCancel()
    {
        if (IsServer)
        {
            
        }
    }
}
