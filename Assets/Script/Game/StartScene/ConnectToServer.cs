using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class ConnectToServer : NetworkBehaviour
{
    public NetworkVariable<FixedString128Bytes> playerName = new NetworkVariable<FixedString128Bytes>(
        writePerm: NetworkVariableWritePermission.Server,
        readPerm: NetworkVariableReadPermission.Everyone
        );

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        try
        { 
            playerName.OnValueChanged += OnPlayerNameChanged; 
        }
        catch(System.Exception e)
        {
            Debug.LogError(e);
        }

        if (!IsOwner) return;
        
        string desiredName;

        try
        {
            // Read the saved name for UI and send it to the server
            desiredName = PlayerPrefs.GetString("DesiredPlayerName", $"Player_{NetworkManager.LocalClientId}");
            SetPlayerNameServerRpc(desiredName); 
        }
        catch(System.Exception e)
        {
            desiredName = $"Player_{NetworkManager.LocalClientId}";
            Debug.LogError($"Connect to Server: Error reading PlayerPrefabs, using fallbacks '{desiredName}' -> {e}");
            Debug.LogError($"Connect to Server: Error setting player name in Server -> {e}");
        }
    }

    public override void OnDestroy()
    {
        try
        { 
            playerName.OnValueChanged -= OnPlayerNameChanged; 
        }
        catch(System.Exception e)
        {
            Debug.LogError($"Connect to Server: Error unsubscribing Playername {e}");
        }
    }

    private void OnPlayerNameChanged(FixedString128Bytes previousName, FixedString128Bytes currentName)
    {
        var name = currentName.ToString();
        if (!string.IsNullOrWhiteSpace(name)) gameObject.name = name;
    }

    [ServerRpc]
    private void SetPlayerNameServerRpc(string name, ServerRpcParams rpcParams = default)
    {
        try
        {
            // Saving the name in the server in the NetworkVariable
            if (string.IsNullOrWhiteSpace(name))
                playerName.Value = new FixedString128Bytes($"Player_{rpcParams.Receive.SenderClientId}");
            else
                playerName.Value = new FixedString128Bytes(name);

            Debug.Log($"[Server] Client {rpcParams.Receive.SenderClientId} name -> {playerName.Value}");

        }
        catch(System.Exception e)
        {
            Debug.LogError($"ConnectToServer: playerName was not established in server -> {e}");
        }
    }
}
