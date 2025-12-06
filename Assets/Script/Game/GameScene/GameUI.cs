using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("UI Game Elements")]
    [SerializeField] private TextMeshProUGUI player1Text;
    [SerializeField] private TextMeshProUGUI player2Text;
    [SerializeField] private TextMeshProUGUI remainTime;
    [SerializeField] private Slider playerWinPercentage;

    [Header("Settings")]
    private bool _isGamePaused = false;
    private string remainTimeString = "Time: ";
    private string player1Name = string.Empty;
    private string player2Name = string.Empty;

    [Header("Players Settings")]
    [SerializeField] private PlayerData player1Data;
    [SerializeField] private PlayerData player2Data;
    [SerializeField] private float percentageWinner = 100f;

    private void Awake()
    {
        
    }
}
