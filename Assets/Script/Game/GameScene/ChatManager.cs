using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class ChatManager : NetworkBehaviour
{
    [SerializeField] private TMP_InputField messageInputField;
    [SerializeField] private TextMeshProUGUI chatDisplayText;
    [SerializeField] private ScrollRect chatScrollRect;

    private const string _channelName = "TestChannel";
    private PlayerData _playerData;
    public Action<string> OnChatReceived;

    private bool _isLoggedIn = false;
    private bool _isJoined = false;

    private void Awake()
    {
        // Not subscription here to avoid send messages before login
        if (messageInputField != null)
            messageInputField.interactable = false;

    }

    private async void Start()
    {
        try
        {
            //Check if Unity Services is initialized
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
                Debug.Log("ChatManager: Unity Services initialized");
            }

            // Verify authentication
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"ChatManager: Signed in as {AuthenticationService.Instance.PlayerId}");
            }

            // Resolver local PlayerData
            _playerData = await ResolveLocalPlayerDataAsync(timeoutSeconds: 5f);

            string displayName = string.Empty;
            // Check Display Name
            if (_playerData != null && !string.IsNullOrWhiteSpace(_playerData.playerName))
                displayName = _playerData.playerName;
            else
            {
                string prefsName = PlayerPrefs.GetString("DesiredPlayerName", string.Empty);
                if (!string.IsNullOrWhiteSpace(prefsName))
                    displayName = prefsName;
                else
                    displayName = AuthenticationService.Instance.PlayerId ?? $"Player_{Guid.NewGuid()}";
            }

            Debug.Log($"ChatManager: Using display name: {displayName}");

            // Login to Vivox
            await LoginVivox(displayName);

            // Try join Channel
            await TryJoinChannelWithRetry(_channelName, 6, 1f);

            // Enable message input
            if (_isJoined && messageInputField != null)
            {
                messageInputField.interactable = true;
                messageInputField.onSubmit.AddListener(OnMessageSubmit);
                Debug.Log("ChatManager: Chat input enabled");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"ChatManager: Start error -> {e}");
        }
    }

    private async Task LoginVivox(string displayName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = AuthenticationService.Instance.PlayerId ?? $"Player_{Guid.NewGuid()}";
                Debug.LogWarning($"ChatManager: DisplayName was empty, using fallback '{displayName}'");
            }

            var loginOptions = new LoginOptions { DisplayName = displayName };
            await VivoxService.Instance.LoginAsync(loginOptions);

            VivoxService.Instance.LoggedIn += OnLogin;
            VivoxService.Instance.ChannelMessageReceived += OnMessageReceived;

            _isLoggedIn = VivoxService.Instance.IsLoggedIn;
            Debug.Log($"ChatManager: Vivox login completed. IsLoggedIn={_isLoggedIn}");
        }
        catch (Exception e)
        {
            Debug.LogError($"ChatManager: Vivox login failed -> {e}");
            _isLoggedIn = false;
        }
    }

    private void OnLogin()
    {
        Debug.Log("Logged to Vivox Services");
        _isLoggedIn = true;
    }

    private async Task TryJoinChannelWithRetry(string channelName, int maxAttempts, float delaySeconds)
    {
        if (string.IsNullOrWhiteSpace(channelName))
        {
            Debug.LogError("ChatManager: Channel name is empty, cannot join");
            return;
        }

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            if (VivoxService.Instance != null && VivoxService.Instance.IsLoggedIn)
            {
                try
                {
                    await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.TextOnly);
                    _isJoined = true;
                    Debug.Log($"ChatManager: Successfully joined channel '{channelName}' on attempt {attempt}");
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"ChatManager: Attempt {attempt}/{maxAttempts} to join channel '{channelName}' failed -> {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"ChatManager: VivoxService not ready (attempt {attempt}/{maxAttempts})");
            }

            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }

        Debug.LogError($"ChatManager: Failed to join channel '{channelName}' after {maxAttempts} attempts");
        _isJoined = false;
    }

    private async Task SendChatMessage(string message, string channelName = _channelName)
    {
        if (!_isJoined || VivoxService.Instance == null || string.IsNullOrEmpty(message))
        {
            Debug.LogError("ChatManager: Cannot send message - not joined to channel or message empty");
            return;
        }

        try
        {
            var messageOptions = new MessageOptions
            {
                Metadata = JsonUtility.ToJson(new Dictionary<string, object>
                {
                    { "PlayerID", VivoxService.Instance.SignedInPlayerId }
                })
            };

            await VivoxService.Instance.SendChannelTextMessageAsync(channelName, message, messageOptions);
        }
        catch (Exception e)
        {
            Debug.LogError($"ChatManager: Failed to send message -> {e}");
        }
    }

    private void OnMessageReceived(VivoxMessage message)
    {
        try
        {
            var channelName = message.ChannelName;
            var senderName = message.SenderDisplayName;
            var senderId = message.SenderPlayerId;
            var messageText = message.MessageText;
            var timeReceived = message.ReceivedTime;

            string formattedMessage = senderName + ": " + messageText;
            OnChatReceived?.Invoke(formattedMessage);
            AddMessageToChat(formattedMessage);
        }
        catch (Exception e)
        {
            Debug.LogError($"ChatManager: Error processing received message -> {e}");
        }
    }

    private async void OnMessageSubmit(string text)
    {
        await SendMessageUI();
    }

    private async Task SendMessageUI()
    {
        if (messageInputField == null)
        {
            Debug.LogError("ChatMessage: Message Input Field is not assigned");
            return;
        }

        string message = messageInputField.text;
        if (string.IsNullOrEmpty(message)) return;

        try
        {
            await SendChatMessage(message, _channelName);
            messageInputField.text = string.Empty;
            messageInputField.ActivateInputField();
        }
        catch (Exception e)
        {
            Debug.LogError($"ChatManager: Error in SendMessageUI -> {e}");
        }
    }

    private void AddMessageToChat(string message)
    {
        if (chatDisplayText == null)
        {
            Debug.LogError("ChatManager: Chat Display Text is not assigned");
            return;
        }

        chatDisplayText.text += message + "\n";
        // Scroll to the bottom

        if (chatScrollRect)
            StartCoroutine(ScrollToBottom());
    }

    private async Task<PlayerData> ResolveLocalPlayerDataAsync(float timeoutSeconds = 5f)
    {
        // Get PlayerData when NetworkObject -> OwnerClientId matches local client id
        float elapsedTime = 0f;
        float interval = 0.1f;
        while (elapsedTime < timeoutSeconds)
        {
            if (NetworkManager.Singleton != null)
            {
                PlayerData[] allPlayers = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);

                foreach (var player in allPlayers)
                {
                    NetworkObject networkObject = player.GetComponent<NetworkObject>();

                    if (networkObject != null && networkObject.OwnerClientId == NetworkManager.Singleton.LocalClientId)
                    {
                        return player;
                    }
                }

                // If only one PlayerData exists and no NetworkObject present, return it

                if (allPlayers.Length == 1 && allPlayers[0].GetComponent<NetworkObject>() == null)
                    return allPlayers[0];
            }

            await Task.Delay(TimeSpan.FromMilliseconds((int)(interval * 1000)));
            elapsedTime += interval;
        }

        Debug.LogWarning("ChatManager: Local PlayerData not found within timeout. Using PlayerPrefs fallback.");
        // fallback: create a lightweight PlayerData or null (ChatManager will fallback to PlayerPrefs)
        return null;
    }

    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        if (chatScrollRect != null)
            chatScrollRect.verticalNormalizedPosition = 0f;
    }

    public override void OnDestroy()
    {
        if (messageInputField != null)
        {
            messageInputField.onSubmit.RemoveListener(OnMessageSubmit);
        }

        if (VivoxService.Instance != null)
        {
            VivoxService.Instance.LoggedIn -= OnLogin;
            VivoxService.Instance.ChannelMessageReceived -= OnMessageReceived;
        }
    }
}
