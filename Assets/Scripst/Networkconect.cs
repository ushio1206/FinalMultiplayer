using UnityEngine;
using Unity.Netcode;

public class NetworkUI : MonoBehaviour
{
    public void StartHost()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.StartHost();
    }

    public void StartClient()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.StartClient();
    }
}

