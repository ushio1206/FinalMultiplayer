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

        //messageInputField.onSubmit.AddListener(OnMessageSubmit);
    }

    private async void Start()
    {
        try
        {
            // Initialize services
            await UnityServices.InitializeAsync();
            AuthenticationService.Instance.SignedIn += OnSignedIn;
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            _playerData = await ResolveLocalPlayerDataAsync(timeoutSeconds: 5f);

            // Login Vivox with name
            string prefsName = PlayerPrefs.GetString("DesiredPlayerName", string.Empty);
            string displayName = !string.IsNullOrEmpty(prefsName) ? _playerData.playerName : (!string.IsNullOrWhiteSpace(prefsName) ? prefsName : AuthenticationService.Instance.PlayerId ?? $"Player_{Guid.NewGuid()}");

            // Login to Vivox
            await LoginVivox(displayName);

            // Try to join chat session with delay to ensure login is complete
            await TryJoinChannelWithRetry(_channelName, 5, 1f);

            // If joined, enable input and subscribe events
            if (_isJoined && messageInputField != null)
            {
                messageInputField.interactable = true;
                messageInputField.onSubmit.AddListener(OnMessageSubmit);
            }

            //await JoinChatSession();

        }
        catch (Exception e)
        {
            Debug.LogError($"ChatManager: Start error -> {e}");
        }
    }

    private void OnSignedIn()
    {
        Debug.Log($"Signed In: {AuthenticationService.Instance.PlayerId}");
    }

    private async Task LoginVivox(string displayName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = AuthenticationService.Instance.PlayerId ?? $"Player_{Guid.NewGuid()}";

            LoginOptions loginOptions = new LoginOptions()
            {
                DisplayName = displayName
            };

            // Register event before login
            await VivoxService.Instance.LoginAsync(loginOptions);

            VivoxService.Instance.LoggedIn += OnLogin;
            VivoxService.Instance.ChannelMessageReceived += OnMessageReceived;

            _isLoggedIn = VivoxService.Instance.IsLoggedIn;

            Debug.Log($"ChatManager: Vivox login completed. Is logged in:{_isLoggedIn}");
        }
        catch (Exception e)
        {
            Debug.LogError($"ChatManager: LoginVivox error -> {e}");
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
            Debug.LogError("ChatManager: Channel Name is empty");
            return;
        }

        for(int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            if (VivoxService.Instance != null && VivoxService.Instance.IsLoggedIn)
            {
                try
                {
                    await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.TextOnly);
                    _isJoined = true;
                    Debug.Log($"ChatManager: Joined channel {channelName} successfully on attempt {attempt}");
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogError($"ChatManager: Failed to join channel {channelName} on attempt {attempt}. Error: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning("ChatManager: VivoxService is not ready or user not logged in yet. Retrying...");
            }

            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }

        Debug.LogError($"ChatManager: Failed to join channel {channelName} after {maxAttempts} attempts.");
        _isJoined = false;
    }

    private async Task SendChatMessage(string message, string channelName = _channelName)
    {
        if(!_isJoined || VivoxService.Instance == null || string.IsNullOrEmpty(message))
        {
            Debug.LogError("ChatManager: Not join to any Vivox channel or empty message");
            return;
        }

        try
        {
            MessageOptions messageOptions = new MessageOptions
            {
                Metadata = JsonUtility.ToJson(
                    new Dictionary<string, object>()
                    {
                        { "PlayerID", VivoxService.Instance.SignedInPlayerId}
                    }
                )
            };

            await VivoxService.Instance.SendChannelTextMessageAsync(channelName, message, messageOptions);
        }
        catch (Exception e)
        {
            Debug.LogError($"ChatManager: Error sending message -> {e}");
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
        if(string.IsNullOrEmpty(message)) return;
        
        try
        {
            await SendChatMessage(message, _channelName);
            messageInputField.text = string.Empty;
            messageInputField.ActivateInputField();
        }
        catch(Exception e)
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

                foreach(var player in allPlayers)
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
        if(chatScrollRect != null)
            chatScrollRect.verticalNormalizedPosition = 0f;
    }
}
