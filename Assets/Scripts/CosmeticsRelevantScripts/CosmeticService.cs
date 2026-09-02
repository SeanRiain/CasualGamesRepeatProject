using System;
using UnityEngine;

public static class CosmeticService
{
    public static bool IsUnlocked(CosmeticDefinition definition)
    {
        PlayerDataManager player = PlayerDataManager.Instance;

        if (player == null || !player.IsReady || definition == null)
        {
            return false;
        }

        switch (definition.UnlockType)
        {
            case CosmeticUnlockType.None:
                return true;

            case CosmeticUnlockType.TotalWins:
                return player.TotalWins >= definition.UnlockThreshold;

            case CosmeticUnlockType.TotalMatches:
                int totalMatches = player.TotalWins + player.TotalLosses;
                return totalMatches >= definition.UnlockThreshold;

            default:
                return false;
        }
    }

    public static CosmeticStoreState GetState(CosmeticDefinition definition)
    {
        PlayerDataManager player = PlayerDataManager.Instance;

        if (player == null || !player.IsReady || definition == null)
        {
            return CosmeticStoreState.Locked;
        }

        bool owned = player.OwnsCosmetic(definition.CosmeticId);
        string equippedId = player.GetEquippedCosmeticId(definition.Category);

        if (owned && equippedId == definition.CosmeticId)
        {
            return CosmeticStoreState.Equipped;
        }

        if (owned)
        {
            return CosmeticStoreState.Owned;
        }

        if (!IsUnlocked(definition))
        {
            return CosmeticStoreState.Locked;
        }

        return CosmeticStoreState.Available;
    }

    public static string GetUnlockDescription(CosmeticDefinition definition)
    {
        switch (definition.UnlockType)
        {
            case CosmeticUnlockType.None:
                return string.Empty;

            case CosmeticUnlockType.TotalWins:
                return $"Unlocks at {definition.UnlockThreshold} total wins";

            case CosmeticUnlockType.TotalMatches:
                return $"Unlocks after {definition.UnlockThreshold} matches";

            default:
                return "Locked";
        }
    }

    public static bool TryPurchase(CosmeticDefinition definition, out string message)
    {
        PlayerDataManager player = PlayerDataManager.Instance;

        if (player == null || !player.IsReady)
        {
            message = "Player account is still loading.";
            return false;
        }

        if (definition == null)
        {
            message = "Cosmetic definition is unavailable.";
            return false;
        }

        CosmeticStoreState state = GetState(definition);

        if (state == CosmeticStoreState.Locked)
        {
            message = GetUnlockDescription(definition);
            return false;
        }

        if (state == CosmeticStoreState.Owned || state == CosmeticStoreState.Equipped)
        {
            message = "This cosmetic is already owned.";
            return false;
        }

        int price = definition.SoftCurrencyPrice;

        if (price > 0 && !player.CanAfford(price))
        {
            message = $"Not enough currency. Cost: {price}.";
            return false;
        }

        if (price > 0 && !player.TrySpendCurrency(price))
        {
            message = "The purchase could not be completed.";
            return false;
        }

        if (!player.GrantCosmetic(definition.CosmeticId))
        {
            if (price > 0)
            {
                player.AddCurrency(price);
            }

            message = "The cosmetic could not be added to the account.";
            return false;
        }

        message = $"Purchased {definition.DisplayName}.";

        return true;
    }

    public static bool TryEquip(CosmeticDefinition definition, out string message)
    {
        PlayerDataManager player = PlayerDataManager.Instance;

        if (player == null || !player.IsReady)
        {
            message = "Player account is still loading.";
            return false;
        }

        if (definition == null)
        {
            message = "Cosmetic data is unavailable.";
            return false;
        }

        if (!player.OwnsCosmetic(definition.CosmeticId))
        {
            message = "You do not own this cosmetic.";
            return false;
        }

        string currentId = player.GetEquippedCosmeticId(definition.Category);

        if (currentId == definition.CosmeticId)
        {
            message = $"{definition.DisplayName} is already equipped.";
            return false;
        }

        bool equipped = player.EquipCosmetic(definition.Category, definition.CosmeticId);

        if (!equipped)
        {
            message = "The cosmetic could not be equipped.";
            return false;
        }

        message = $"Equipped {definition.DisplayName}.";

        return true;
    }

    public static void EnsureDefaults(CosmeticCatalog catalog)
    {
        PlayerDataManager player = PlayerDataManager.Instance;

        if (player == null || !player.IsReady || catalog == null)
        {
            return;
        }

        foreach (CosmeticCategory category in Enum.GetValues(typeof(CosmeticCategory)))
        {
            CosmeticDefinition defaultItem = catalog.GetDefault(category);

            if (defaultItem == null)
            {
                Debug.LogError($"No default cosmetic exists for {category}.");
                continue;
            }

            if (!player.OwnsCosmetic(defaultItem.CosmeticId))
            {
                player.GrantCosmetic(defaultItem.CosmeticId);
            }

            string equippedId = player.GetEquippedCosmeticId(category);
            CosmeticDefinition equippedDefinition = catalog.GetById(equippedId);

            bool equippedStateIsValid =
                equippedDefinition != null &&
                equippedDefinition.Category == category &&
                player.OwnsCosmetic(equippedId);

            if (!equippedStateIsValid)
            {
                player.EquipCosmetic(category, defaultItem.CosmeticId);
            }
        }
    }

    public static bool EnsureDefaults(PlayerData playerData, CosmeticCatalog catalog)
    {
        if (playerData == null || catalog == null)
        {
            return false;
        }

        bool changed = false;

        foreach (CosmeticCategory category in Enum.GetValues(typeof(CosmeticCategory)))
        {
            CosmeticDefinition defaultItem = catalog.GetDefault(category);

            if (defaultItem == null)
            {
                Debug.LogError($"No default cosmetic exists for {category}.");
                continue;
            }

            if (!playerData.OwnsCosmetic(defaultItem.CosmeticId))
            {
                changed |= playerData.AddOwnedCosmetic(defaultItem.CosmeticId);
            }

            string equippedId = playerData.GetEquippedCosmeticId(category);
            CosmeticDefinition equippedDefinition = catalog.GetById(equippedId);

            bool equippedStateIsValid =
                equippedDefinition != null &&
                equippedDefinition.Category == category &&
                playerData.OwnsCosmetic(equippedId);

            if (!equippedStateIsValid)
            {
                changed |= playerData.SetEquippedCosmetic(category, defaultItem.CosmeticId);
            }
        }

        return changed;
    }
}