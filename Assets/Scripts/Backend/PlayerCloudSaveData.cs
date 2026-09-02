using System;
using System.Collections.Generic;

[Serializable]
public class CloudEquippedCosmeticData
{
    public CosmeticCategory category;
    public string cosmeticId;

    public CloudEquippedCosmeticData()
    {
    }

    public CloudEquippedCosmeticData(CosmeticCategory category, string cosmeticId)
    {
        this.category = category;
        this.cosmeticId = cosmeticId;
    }
}

[Serializable]
public class PlayerCloudSaveData
{
    public const int CurrentSchemaVersion = 1;

    public int schemaVersion = CurrentSchemaVersion;

    public string displayName;

    public int softCurrency;

    public int totalWins;

    public int totalLosses;

    public List<string> ownedCosmeticIds = new List<string>();

    public List<CloudEquippedCosmeticData> equippedCosmetics = new List<CloudEquippedCosmeticData>();

    public static PlayerCloudSaveData FromPlayerData( PlayerData player)
    {
        PlayerCloudSaveData data = new PlayerCloudSaveData
            {
                schemaVersion = CurrentSchemaVersion,

                displayName = player.DisplayName,

                softCurrency = player.SoftCurrency,

                totalWins = player.TotalWins,

                totalLosses = player.TotalLosses
            };

        foreach (string cosmeticId in player.OwnedCosmeticIds)
        {
            if (string.IsNullOrWhiteSpace(cosmeticId))
            {
                continue;
            }

            data.ownedCosmeticIds.Add(cosmeticId);
        }

        foreach (EquippedCosmeticData equipped in player.EquippedCosmetics)
        {
            if (equipped == null)
                continue;

            if (string.IsNullOrWhiteSpace(
                    equipped.CosmeticId))
            {
                continue;
            }

            data.equippedCosmetics.Add(new CloudEquippedCosmeticData(equipped.Category, equipped.CosmeticId));
        }

        return data;
    }

    public PlayerData ToPlayerData(string authenticatedPlayerId, string fallbackDisplayName)
    {
        string resolvedDisplayName = string.IsNullOrWhiteSpace(displayName) ? fallbackDisplayName : displayName;

        PlayerData player = new PlayerData(authenticatedPlayerId, resolvedDisplayName, softCurrency, totalWins, totalLosses);

        if (ownedCosmeticIds != null)
        {
            foreach (string cosmeticId in ownedCosmeticIds)
            {
                player.AddOwnedCosmetic(cosmeticId);
            }
        }

        if (equippedCosmetics != null)
        {
            foreach (CloudEquippedCosmeticData equipped in equippedCosmetics)
            {
                if (equipped == null)
                    continue;

                if (!Enum.IsDefined(typeof(CosmeticCategory), equipped.category))
                {
                    continue;
                }

                player.SetEquippedCosmetic(equipped.category, equipped.cosmeticId);
            }
        }

        return player;
    }
}