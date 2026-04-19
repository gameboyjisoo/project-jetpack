using UnityEngine;
using System;

public enum FuelTier { Low, Mid, High }

public class JetpackGas : MonoBehaviour
{
    [SerializeField] private float maxGas = 100f;
    [SerializeField] private float midThreshold = 0.5f;
    [SerializeField] private float lowThreshold = 0.2f;

    private float currentGas;

    public float CurrentGas => currentGas;
    public float MaxGas => maxGas;
    public float GasPercent => currentGas / maxGas;
    public bool HasGas => currentGas > 0f;

    public FuelTier CurrentTier
    {
        get
        {
            float pct = GasPercent;
            if (pct >= midThreshold) return FuelTier.High;
            if (pct >= lowThreshold) return FuelTier.Mid;
            return FuelTier.Low;
        }
    }

    public event Action<float> OnGasChanged;
    public event Action OnGasEmpty;
    public event Action OnGasRecharged;

    private void Awake()
    {
        currentGas = maxGas;
    }

    public void ConsumeGas(float amount)
    {
        float previous = currentGas;
        currentGas = Mathf.Max(0f, currentGas - amount);
        OnGasChanged?.Invoke(GasPercent);

        if (previous > 0f && currentGas <= 0f)
        {
            OnGasEmpty?.Invoke();
            GameEventBus.Publish(new PlayerFuelEmpty());
        }
    }

    public void Recharge()
    {
        if (currentGas < maxGas)
        {
            currentGas = maxGas;
            OnGasChanged?.Invoke(GasPercent);
            OnGasRecharged?.Invoke();
            GameEventBus.Publish(new PlayerFuelRecharged());
        }
    }

    /// <summary>
    /// Instant recharge from a mid-air pickup (like Celeste's Dash Crystal).
    /// </summary>
    public void RechargeFromPickup()
    {
        Recharge();
    }
}
