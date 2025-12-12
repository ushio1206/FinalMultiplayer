using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
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
    private UnityTransport _transport;

    public string RelayJoinCode { get; private set; }

    private async void Awake()
    {
        try
        {
            // Start Unity Services if not already initialized
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
                Debug.Log("NetworkStart: Unity Services initialized");
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"NetworkStart: Signed in as {AuthenticationService.Instance.PlayerId}");
            }

            // Unity Transport setup
            _transport = NetworkManager.Singleton?.GetComponent<UnityTransport>();

            if (!_transport) 
                Debug.LogError("NetworkStart: UnityTransport component not found on NetworkManager");
                return;
            
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

    public async Task<string> StartHostWithRelayAsync()
    {
        if (NetworkManager.Singleton == null || _transport == null)
        {
            Debug.LogError("NetworkStart: NetworkManager or Transport missing");
            return null;
        }

        try
        {
            // Create Relay Allocation
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);

            // Get Join Code
            RelayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log($"Relay Join Code: {RelayJoinCode}");

            // Setup Transport with Relay data
            _transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            // Subscribe to client connection events
            if (!_clientSubscribed)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                _clientSubscribed = true;
            }

            // Start Host
            NetworkManager.Singleton.StartHost();
            Debug.Log("NetworkStart: Host started via Relay");

            return RelayJoinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"NetworkStart: Relay allocation failed -> {e.Reason}: {e.Message}");
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"NetworkStart: Unexpected error starting host -> {e}");
            return null;
        }
    }

    public async Task<bool> StartClientWithRelayAsync(string joinCode)
    {
        if (NetworkManager.Singleton == null || _transport == null)
        {
            Debug.LogError("NetworkStart: NetworkManager or Transport missing");
            return false;
        }

        if (string.IsNullOrWhiteSpace(joinCode))
        {
            Debug.LogError("NetworkStart: Join code is empty!");
            return false;
        }

        try
        {
            Debug.Log($"Joining with Relay code: {joinCode}");

            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            _transport.SetClientRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                allocation.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();
            Debug.Log("NetworkStart: Client started via Relay");

            return true;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"NetworkStart: Relay join failed -> {e.Reason}: {e.Message}");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"NetworkStart: Unexpected error joining -> {e}");
            return false;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!NetworkManager.Singleton) return;

        if (NetworkManager.Singleton.IsServer)
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
