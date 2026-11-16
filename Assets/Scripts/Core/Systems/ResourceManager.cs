using System;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    [Header("Starting Resources")]
    [SerializeField, Min(0)] private int startingNitrogenCanisters = 3;
    [SerializeField, Min(0f)] private float startingOxygenSeconds = 300f;
    [SerializeField, Min(0)] private int startingShipParts = 3;
    [SerializeField, Min(0f)] private float startingParanormalEnergy = 0f;

    [Header("Ship Hull")]
    [SerializeField, Min(1)] private int maxHullPips = 5;
    [SerializeField, Min(1)] private int shipPartsPerHullPip = 1;

    public int NitrogenCanisters { get; private set; }
    public float OxygenSeconds { get; private set; }
    public float OxygenCapacity => Mathf.Max(0, startingOxygenSeconds);
    public int ShipParts { get; private set; }
    public float ParanormalEnergy { get; private set; }
    public int HullPips { get; private set; }
    public int MaxHullPips => maxHullPips;
    public bool IsOxygenDepleted => OxygenSeconds <= 0f;

    public event Action ResourcesChanged;
    public event Action<int, int> HullChanged;
    public event Action OxygenDepleted;

    private bool oxygenDepletedRaised;

    private void Awake()
    {
        ResetForNewRun();
    }

    public void ResetForNewRun()
    {
        NitrogenCanisters = startingNitrogenCanisters;
        OxygenSeconds = Mathf.Max(0f, startingOxygenSeconds);
        ShipParts = startingShipParts;
        ParanormalEnergy = Mathf.Max(0f, startingParanormalEnergy);
        HullPips = maxHullPips;
        oxygenDepletedRaised = OxygenSeconds <= 0f;
        ResourcesChanged?.Invoke();
        HullChanged?.Invoke(HullPips, maxHullPips);
    }

    public bool TryConsumeNitrogen(int amount = 1)
    {
        if (amount <= 0 || NitrogenCanisters < amount)
            return false;

        NitrogenCanisters -= amount;
        NotifyResourcesChanged();
        return true;
    }

    public void AddNitrogen(int amount)
    {
        if (amount <= 0) return;
        NitrogenCanisters += amount;
        NotifyResourcesChanged();
    }

    public void DrainOxygen(float seconds)
    {
        if (seconds <= 0f || OxygenSeconds <= 0f) return;

        OxygenSeconds = Mathf.Max(0f, OxygenSeconds - seconds);
        NotifyResourcesChanged();

        if (OxygenSeconds <= 0f && !oxygenDepletedRaised)
        {
            oxygenDepletedRaised = true;
            OxygenDepleted?.Invoke();
        }
    }

    public void AddOxygen(float seconds)
    {
        if (seconds <= 0f) return;
        OxygenSeconds += seconds;
        if (OxygenSeconds > 0f) oxygenDepletedRaised = false;
        NotifyResourcesChanged();
    }

    public bool TryConsumeShipParts(int amount = 1)
    {
        if (amount <= 0 || ShipParts < amount)
            return false;

        ShipParts -= amount;
        NotifyResourcesChanged();
        return true;
    }

    public void AddShipParts(int amount)
    {
        if (amount <= 0) return;
        ShipParts += amount;
        NotifyResourcesChanged();
    }

    public bool TryConsumeParanormalEnergy(float amount)
    {
        if (amount <= 0f || ParanormalEnergy < amount)
            return false;

        ParanormalEnergy = Mathf.Max(0f, ParanormalEnergy - amount);
        NotifyResourcesChanged();
        return true;
    }

    public void AddParanormalEnergy(float amount)
    {
        if (amount <= 0f) return;
        ParanormalEnergy += amount;
        NotifyResourcesChanged();
    }

    public void ApplyHullDamage(int pips = 1)
    {
        if (pips <= 0) return;
        int previous = HullPips;
        HullPips = Mathf.Clamp(HullPips - pips, 0, maxHullPips);
        if (previous != HullPips)
            HullChanged?.Invoke(HullPips, maxHullPips);
    }

    //Call only after the repair minigame is a win
    public bool TryRepairHullPip()
    {
        if (HullPips >= maxHullPips || ShipParts < shipPartsPerHullPip)
            return false;

        ShipParts -= shipPartsPerHullPip;
        HullPips = Mathf.Min(maxHullPips, HullPips + 1);
        NotifyResourcesChanged();
        HullChanged?.Invoke(HullPips, maxHullPips);
        return true;
    }

    private void NotifyResourcesChanged()
    {
        ResourcesChanged?.Invoke();
    }
}
