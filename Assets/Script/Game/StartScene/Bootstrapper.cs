using System;
using System.Threading.Tasks;
using Unity.Services.Core;
using UnityEngine;

public class Bootstrapper : MonoBehaviour
{
    private static TaskCompletionSource<bool> s_tcs;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        if (s_tcs == null)
        {
            s_tcs = new TaskCompletionSource<bool>();
            _ = InitializeInternalAsync();
        }
    }

    public static Task EnsureInitialized()
    {
        if (s_tcs == null)
        {
            var go = new GameObject("UnityServicesBootstrapper");
            DontDestroyOnLoad(go);
            //go.AddComponent<UnityServicesBootstrapper>();
        }
        return s_tcs.Task;
    }

    private static async Task InitializeInternalAsync()
    {
        try
        {
            Debug.Log("UnityServicesBootstrapper: Initializing Unity Services...");
            await UnityServices.InitializeAsync();
            Debug.Log("UnityServicesBootstrapper: Initialized.");
            s_tcs.TrySetResult(true);
        }
        catch (Exception ex)
        {
            Debug.LogError($"UnityServicesBootstrapper: InitializeAsync failed -> {ex}");
            s_tcs.TrySetException(ex);
        }
    }
}
