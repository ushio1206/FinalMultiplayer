using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ElixirManager : MonoBehaviour
{
    public static ElixirManager Instance;

    [Header("Elixir Settings")]
    public int maxElixir = 10;
    public float regenRate = 1f; // segundos por 1 elixir

    private int currentElixir;
    private float timer;

    [Header("UI")]
    public Image elixirBar;
    public TextMeshProUGUI elixirText;

    private void Awake()
    {
        Instance = this;
        currentElixir = maxElixir;
        UpdateUI();
    }

    private void Update()
    {
        if (currentElixir >= maxElixir) return;

        timer += Time.deltaTime;
        if (timer >= regenRate)
        {
            timer = 0f;
            currentElixir++;
            UpdateUI();
        }
    }

    public bool CanSpend(int cost)
    {
        return currentElixir >= cost;
    }

    public void Spend(int cost)
    {
        currentElixir -= cost;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (elixirBar)
            elixirBar.fillAmount = (float)currentElixir / maxElixir;

        if (elixirText)
            elixirText.text = currentElixir.ToString();
    }
}

