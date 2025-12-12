using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{
    [SerializeField] private Button menuButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button exitGameButton;
    [SerializeField] private GameObject menuPanel;

    private string mainMenuSceneName = "MainMenu";
    private Lobby _currentLobby;

    private void Start()
    {
        menuPanel.SetActive(false);
        
        menuButton.onClick.AddListener(OpenMenu);
        resumeButton.onClick.AddListener(CloseMenu);
        mainMenuButton.onClick.AddListener(ReturnMainMenu);
        exitGameButton.onClick.AddListener(ExitGame);

        // Try to get current lobby or server info if needed
        TryGetCurrentLobby();
    }

    private void OpenMenu()
    {
        menuPanel.SetActive(true);
        menuButton.gameObject.SetActive(false);
    }

    private void CloseMenu()
    {
        menuPanel.SetActive(false);
        menuButton.gameObject.SetActive(true);
    }

    private void ReturnMainMenu()
    {
        print($"Player returned to the main menú");
        //
        _ = DisconnectAndReturnAsync();
    }

    private void ExitGame()
    {
        print($"Player left the game");
        //Application.Quit();
        _ = DisconnectAndQuitAsync();
    }

    private void TryGetCurrentLobby()
    {
        // Intentar obtener referencia al lobby desde UIStart (si existe)
        var uiStart = FindAnyObjectByType<UIStart>();
        if (uiStart != null)
        {
            // Nota: Necesitas exponer _hostLobby en UIStart como propiedad pública
            // _currentLobby = uiStart.CurrentLobby;
        }
    }

    private async Task DisconnectAndReturnAsync()
    {
        try
        {
            await DisconnectFromNetworkAsync();

            await LeaveLobbyAsync();

            SceneManager.LoadScene(mainMenuSceneName);
        }
        catch (Exception e)
        {
            Debug.LogError($"MenuUI: Error returning to main menu -> {e}");
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    private async Task DisconnectAndQuitAsync()
    {
        try
        {
            await DisconnectFromNetworkAsync();
            await LeaveLobbyAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"MenuUI: Error during quit -> {e}");
        }
        finally
        {
            Application.Quit();
        }
    }

    private async Task DisconnectFromNetworkAsync()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning("MenuUI: NetworkManager not found");
            return;
        }

        try
        {
            if (NetworkManager.Singleton.IsHost)
            {
                Debug.Log("MenuUI: Shutting down host...");
                NetworkManager.Singleton.Shutdown();
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                Debug.Log("MenuUI: Disconnecting client...");
                NetworkManager.Singleton.Shutdown();
            }

            // Esperar un frame para que Netcode procese la desconexión
            await Task.Delay(100);
            Debug.Log("MenuUI: Network disconnected");
        }
        catch (Exception e)
        {
            Debug.LogError($"MenuUI: Error disconnecting from network -> {e}");
        }
    }

    private async Task LeaveLobbyAsync()
    {
        if (_currentLobby == null)
        {
            Debug.LogWarning("MenuUI: No lobby to leave");
            return;
        }

        try
        {
            // Si somos host, eliminar el lobby completamente
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
            {
                Debug.Log($"MenuUI: Deleting lobby {_currentLobby.Id}");
                await LobbyService.Instance.DeleteLobbyAsync(_currentLobby.Id);
            }
            else
            {
                // Si somos cliente, solo salir del lobby
                Debug.Log($"MenuUI: Leaving lobby {_currentLobby.Id}");
                await LobbyService.Instance.RemovePlayerAsync(_currentLobby.Id,
                    Unity.Services.Authentication.AuthenticationService.Instance.PlayerId);
            }

            _currentLobby = null;
            Debug.Log("MenuUI: Left lobby successfully");
        }
        catch (LobbyServiceException le)
        {
            Debug.LogWarning($"MenuUI: Lobby service error -> {le.Reason}: {le.Message}");
        }
        catch (Exception e)
        {
            Debug.LogError($"MenuUI: Error leaving lobby -> {e}");
        }
    }
    private void OnApplicationQuit()
    {
        // Cleanup final al cerrar la aplicación
        _ = DisconnectAndQuitAsync();
    }

}
