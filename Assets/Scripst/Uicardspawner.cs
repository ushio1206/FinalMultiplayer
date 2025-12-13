using UnityEngine;

public class UICardSpawner : MonoBehaviour
{
    public SoldierSpawner spawner;

    public UICardElixir normal;
    public UICardElixir giant;
    public UICardElixir air;
    public UICardElixir archer;

    public void SpawnNormal()
    {
        if (!normal.TryUse()) return;
        spawner.SpawnNormalFromUI();
    }

    public void SpawnGiant()
    {
        if (!giant.TryUse()) return;
        spawner.SpawnGiantFromUI();
    }

    public void SpawnAir()
    {
        if (!air.TryUse()) return;
        spawner.SpawnAirFromUI();
    }

    public void SpawnArcher()
    {
        if (!archer.TryUse()) return;
        spawner.SpawnArcherFromUI();
    }
}
