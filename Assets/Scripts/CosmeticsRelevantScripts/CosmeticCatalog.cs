using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CosmeticCatalog", menuName = "Casual Pong/Cosmetics/Cosmetic Catalog")]
public class CosmeticCatalog : ScriptableObject
{
    [SerializeField]
    private List<CosmeticDefinition> items = new List<CosmeticDefinition>();

    public IReadOnlyList<CosmeticDefinition> Items => items;

    public CosmeticDefinition GetById(string cosmeticId)
    {
        if (string.IsNullOrWhiteSpace(cosmeticId))
            return null;

        foreach (CosmeticDefinition item in items)
        {
            if (item != null && item.CosmeticId == cosmeticId)
            {
                return item;
            }
        }

        return null;
    }

    public CosmeticDefinition GetDefault(CosmeticCategory category)
    {
        foreach (CosmeticDefinition item in items)
        {
            if (item != null && item.Category == category && item.IsDefault)
            {
                return item;
            }
        }

        return null;
    }

    private void OnValidate()
    {
        HashSet<string> usedIds = new HashSet<string>();

        HashSet<CosmeticCategory> defaultCategories = new HashSet<CosmeticCategory>();

        foreach (CosmeticDefinition item in items)
        {
            if (item == null)
                continue;

            if (string.IsNullOrWhiteSpace(
                    item.CosmeticId))
            {
                Debug.LogError($"Cosmetic '{item.name}' has no ID.", this);

                continue;
            }

            if (!usedIds.Add(item.CosmeticId))
            {
                Debug.LogError($"Duplicate cosmetic ID: {item.CosmeticId}", this);
            }

            if (item.IsDefault && !defaultCategories.Add(item.Category))
            {
                Debug.LogError($"More than one default exists for {item.Category}.", this);
            }
        }
    }
}