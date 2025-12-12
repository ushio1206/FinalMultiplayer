using System;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Core;
using UnityEngine;

public class ConnectToServer : NetworkBehaviour
{
    public NetworkVariable<FixedString128Bytes> playerName = new NetworkVariable<FixedString128Bytes>(
        writePerm: NetworkVariableWritePermission.Server,
        readPerm: NetworkVariableReadPermission.Everyone
        );

    private async void Awake()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
                Debug.Log("ConnectToServer: Unity Services initialized");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"ConnectToServer: Unity Services init failed -> {e}");
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        try
        {
            playerName.OnValueChanged += OnPlayerNameChanged;
        }
        catch (Exception e)
        {
            Debug.LogError($"ConnectToServer: Subscribe failed -> {e}");
        }

        if (!IsOwner) return;

        try
        {
            string desiredName = PlayerPrefs.GetString("DesiredPlayerName", $"Player_{NetworkManager.LocalClientId}");
            SetPlayerNameServerRpc(desiredName);
        }
        catch (Exception e)
        {
            Debug.LogError($"ConnectToServer: Set player name failed -> {e}");
        }
    }

    public override void OnDestroy()
    {
        try
        {
            playerName.OnValueChanged -= OnPlayerNameChanged;
        }
        catch (Exception e)
        {
            Debug.LogError($"ConnectToServer: Unsubscribe failed -> {e}");
        }
    }

    private void OnPlayerNameChanged(FixedString128Bytes previousName, FixedString128Bytes currentName)
    {
        try
        {
            var name = currentName.ToString();
            if (!string.IsNullOrWhiteSpace(name))
                gameObject.name = name;

            var playerData = GetComponent<PlayerData>();
            if (playerData != null)
            {
                playerData.SetName(name);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"ConnectToServer: Name change failed -> {e}");
        }
    }

    [ServerRpc]
    private void SetPlayerNameServerRpc(string name, ServerRpcParams rpcParams = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
                playerName.Value = new FixedString128Bytes($"Player_{rpcParams.Receive.SenderClientId}");
            else
                playerName.Value = new FixedString128Bytes(name);

            Debug.Log($"[Server] Client {rpcParams.Receive.SenderClientId} name set to: {playerName.Value}");
        }
        catch (Exception e)
        {
            Debug.LogError($"ConnectToServer: Server RPC failed -> {e}");
        }
    }
}
