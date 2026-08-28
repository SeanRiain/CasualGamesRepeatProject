using System;
using UnityEngine;

[Serializable]
public class EquippedCosmeticData
{
    [SerializeField] private CosmeticCategory category;
    [SerializeField] private string cosmeticId;

    public CosmeticCategory Category => category;
    public string CosmeticId => cosmeticId;

    public EquippedCosmeticData(CosmeticCategory category, string cosmeticId)
    {
        this.category = category;
        this.cosmeticId = cosmeticId;
    }

    public void SetCosmeticId(string newCosmeticId)
    {
        cosmeticId = newCosmeticId;
    }
}
