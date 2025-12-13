using UnityEngine;

public class UIWinScreen : MonoBehaviour
{
    public static UIWinScreen Instance;

    [Header("Panels")]
    public GameObject winPanel;
    public GameObject losePanel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        winPanel.SetActive(false);
        losePanel.SetActive(false);
    }

    // ✅ MÉTODOS QUE EL GAMEMANAGER ESPERA
    public void ShowWin()
    {
        winPanel.SetActive(true);
    }

    public void ShowLose()
    {
        losePanel.SetActive(true);
    }
}
