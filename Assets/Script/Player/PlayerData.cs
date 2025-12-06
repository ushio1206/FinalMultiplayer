using UnityEngine;
using Unity.Netcode;
using System;

public class PlayerData : MonoBehaviour
{
    public string playerName = string.Empty;
    [SerializeField] private GameObject[] towers;

    //Event to notify name change
    public event Action<string> OnNameChanged;

    private void Awake()
    {
        playerName = PlayerPrefs.GetString("DesiredPlayerName", string.Empty);
    }

    // Safe set -> Notify changes
    public void SetName(string name)
    {
        string newName = name ?? string.Empty;

        if (playerName == newName) return;

        playerName = newName;
        OnNameChanged?.Invoke(playerName);
    }

    public bool IsOwnedBy(ulong clientId)
    {
        NetworkObject networkObject = GetComponent<NetworkObject>();
        return networkObject != null && networkObject.OwnerClientId == clientId;
    }
}
