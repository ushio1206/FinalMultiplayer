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

public class ChatManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField messageInputField;
    [SerializeField] private TextMeshProUGUI chatDisplayText;
    [SerializeField] private ScrollRect chatScrollRect;

    private const string _channelName = "TestChannel";
    private string _playerName;
    private PlayerData _playerData;
    public Action<string> OnChatReceived;

    private void Awake()
    {
        _playerData = FindAnyObjectByType<PlayerData>();
        messageInputField.onSubmit.AddListener(OnMessageSubmit);
    }

    private async void Start()
    {
        // Initialize to the service and join 
        await UnityServices.InitializeAsync();
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log($"Signed In{AuthenticationService.Instance.PlayerId}");

        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        string playerName = PlayerPrefs.GetString("DesiredPlayerName", string.Empty);

        await LoginVivox();
        await JoinChatSession();
    }

    private async Task LoginVivox()
    {
        try
        {
            string playerName = await AuthenticationService.Instance.GetPlayerNameAsync();
            LoginOptions loginOptions = new LoginOptions()
            {
                DisplayName = _playerName
            };

            await VivoxService.Instance.LoginAsync(loginOptions);

            VivoxService.Instance.LoggedIn += OnLogin;
            VivoxService.Instance.ChannelMessageReceived += OnMessageReceived;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }

    }

    private void OnLogin()
    {
        Debug.Log("Logged to Vivox Services");
    }

    private async Task JoinChatSession(string lobbyChannelName = _channelName)
    {
        if(!VivoxService.Instance.IsLoggedIn)
        {
            Debug.LogError("User is not logged in to Vivox");
            return;
        }

        try
        {
            await VivoxService.Instance.JoinGroupChannelAsync(
                lobbyChannelName,
                ChatCapability.TextOnly
                );
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    private async Task SendChatMessage(string message, string channelName = _channelName)
    {
        if(!VivoxService.Instance.IsLoggedIn || string.IsNullOrEmpty(message))
        {
            Debug.LogError("User is not logged in to Vivox");
            return;
        }

        try
        {
            MessageOptions messageOptions = new MessageOptions
            {
                Metadata = JsonUtility.ToJson(
                    new Dictionary<string, object>()
                    {
                        { "PlayerIF", VivoxService.Instance.SignedInPlayerId}
                    }
                )
            };

            await VivoxService.Instance.SendChannelTextMessageAsync(channelName, message, messageOptions);
        }
        catch(Exception e)
        {
            Debug.LogError(e);
        }
    }

    private void OnMessageReceived(VivoxMessage message)
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

    private async void OnMessageSubmit(string text)
    {
        await SendMessageUI();
    }

    private async Task SendMessageUI()
    {
        if (!messageInputField)
        {
            Debug.LogError("Message Input Field is not assigned");
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
            Debug.LogError(e);
        }
    }

    private void AddMessageToChat(string message)
    {
        if (!chatDisplayText)
        {
            Debug.LogError("Chat Display Text is not assigned");
            return;
        }

        chatDisplayText.text += message + "\n";
        // Scroll to the bottom

        if (chatScrollRect)
        {
            StartCoroutine(ScrollToBottom());
        }
    }

    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        chatScrollRect.verticalNormalizedPosition = 0f;
    }
}
