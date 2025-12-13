using UnityEngine;
using UnityEngine.UI;

public class UICardElixir : MonoBehaviour
{
    public int elixirCost = 3;
    public Button button;

    private void Update()
    {
        if (!ElixirManager.Instance) return;

        // Se bloquea si no hay elixir suficiente
        button.interactable = ElixirManager.Instance.CanSpend(elixirCost);
    }

    public bool TryUse()
    {
        if (!ElixirManager.Instance.CanSpend(elixirCost))
            return false;

        ElixirManager.Instance.Spend(elixirCost);
        return true;
    }
}
