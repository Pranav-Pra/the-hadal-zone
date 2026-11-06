using System;
using UnityEngine;

// Shared charge value any script can read or change
[CreateAssetMenu(fileName = "ChargeData", menuName = "Game/Charge Data")]
public class ChargeData : ScriptableObject
{
    public int max = 10;
    [NonSerialized] public int current;
    public event Action<int> OnChanged;   // tells the UI when charge changes

    public bool IsFull => current >= max;

    public void Add(int amount)
    {
        int newValue = Mathf.Clamp(current + amount, 0, max);  // never above 10
        if (newValue == current) return;
        current = newValue;
        OnChanged?.Invoke(current);
    }

    public void ResetCharge()
    {
        current = 0;
        OnChanged?.Invoke(current);
    }
}