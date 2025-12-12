using NUnit.Framework;
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.Netcode;

public class GameUI : NetworkBehaviour
{
    [Header("UI Game Elements")]
    [SerializeField] private TextMeshProUGUI player1Text;
    [SerializeField] private TextMeshProUGUI player2Text;
    [SerializeField] private TextMeshProUGUI remainTime;
    [SerializeField] private Slider playerWinPercentage;

    [Header("Settings")]
    private bool _isMenuActivated = false;
    private string remainTimeString = "Time: ";
    private string player1Name = string.Empty;
    private string player2Name = string.Empty;

    [Header("Players Settings")]
    [SerializeField] private PlayerData player1Data;
    [SerializeField] private PlayerData player2Data;
    [SerializeField] private float percentageWinner = 100f;

    [SerializeField] private float _playerResolveTimeout = 5f;
    [SerializeField] private float _playerResolveInterval = 0.25f;

    private void OnEnable()
    {
        RefreshPlayersAndSubscribe();
    }

    private void OnDisable()
    {
        UnsubscribePlayerEvents();
    }

    private void ChangePercentageText(ushort percentage)
    {
        if (!playerWinPercentage) return;
        playerWinPercentage.value = percentage;
    }

    private void RefreshPlayersAndSubscribe()
    {
        UnsubscribePlayerEvents();

        // If player1 and Player2 are assigned already, keep them, else, try to find them
        List<PlayerData> allPlayers = FindObjectsByType<PlayerData>(FindObjectsSortMode.InstanceID).ToList();

        if (allPlayers.Count < 2)
        {
            Debug.LogWarning($"GameUI: Found {allPlayers.Count} Player(s). Waiting up to {_playerResolveTimeout}s for players to spawn.");
            StopAllCoroutines();
            StartCoroutine(WaitForPlayersAndRefresh(_playerResolveTimeout));
            return;

        }
        
        // Prioritize order by NetworkObjectId if exists, else by InstanceID
        var ordered = allPlayers.OrderBy(
            playerData =>
            {
                var netObj = playerData.GetComponent<NetworkObject>();
                return netObj != null ? (long)netObj.OwnerClientId : long.MaxValue;
            }).ToList();

        if (player1Data == null && ordered.Count > 0) player1Data = ordered[0];
        if (player2Data == null && ordered.Count > 1) player2Data = ordered[1];

        // Subscribe to NameChanges and set UI immediatelly
        if (player1Data != null) player1Data.OnNameChanged += OnPlayer1NameChanged;
        if (player2Data != null) player2Data.OnNameChanged += OnPlayer2NameChanged;

        // Initialize texts from PlayerData or PlayerPrefs as fallback
        player1Name = player1Data != null && !string.IsNullOrWhiteSpace(player1Data.playerName)
            ? player1Data.playerName : PlayerPrefs.GetString("DesiredPlayerName", "Player_1");

        player2Name = player2Data != null && !string.IsNullOrWhiteSpace(player2Data.playerName)
            ? player2Data.playerName : PlayerPrefs.GetString("DesiredPlayerName", "Player_2");

        UpdatePlayerTexts();
    }

    private IEnumerator<YieldInstruction> WaitForPlayersAndRefresh(float timeoutSeconds)
    {
        float elapsed = 0f;

        while(elapsed < timeoutSeconds)
        {
            List<PlayerData> allPlayers = FindObjectsByType<PlayerData>(FindObjectsSortMode.None).ToList();

            if(allPlayers.Count >= 2)
            {
                RefreshPlayersAndSubscribe();
                yield break;
            }

            yield return new WaitForSeconds(_playerResolveInterval);
            elapsed += _playerResolveInterval;
        }

        Debug.LogWarning($"GameUI: Timeout waiting for Players.");

        player1Name = PlayerPrefs.GetString("DesiredPlayerName", "Player_1");
        player2Name = PlayerPrefs.GetString("DesiredOpponentName", "Player_2");
        UpdatePlayerTexts();
    }

    private void UnsubscribePlayerEvents()
    {
        if (player1Data != null) player1Data.OnNameChanged -= OnPlayer1NameChanged;
        if (player2Data != null) player2Data.OnNameChanged -= OnPlayer2NameChanged;
    }

    private void OnPlayer1NameChanged(string newName) => UpdatePlayer1Name(newName);
    private void OnPlayer2NameChanged(string newName) => UpdatePlayer2Name(newName);

    private void UpdatePlayer1Name(string name)
    {
        player1Name = name ?? string.Empty;
        if (player1Text != null) player1Text.text = player1Name;
    }

    private void UpdatePlayer2Name(string name)
    {
        player2Name = name ?? string.Empty;
        if (player2Text != null) player2Text.text = player2Name;
    }

    private void UpdatePlayerTexts()
    {
        if (player1Text != null) player1Text.text = player1Name;
        if (player2Text != null) player2Text.text = player2Name;
    }
}
