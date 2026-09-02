using UnityEngine;

public class CosmeticSystemInitializer : MonoBehaviour
{
    [SerializeField] private CosmeticCatalog cosmeticCatalog;

    private PlayerDataManager playerDataManager;

    private void OnEnable()
    {
        playerDataManager = PlayerDataManager.Instance;

        if (playerDataManager == null)
        {
            Debug.LogError("Cosmetic system could not initialise because PlayerDataManager is missing.");
            return;
        }

        playerDataManager.PlayerReady += HandlePlayerReady;

        if (playerDataManager.IsReady)
        {
            EnsureDefaults();
        }
    }

    private void OnDisable()
    {
        if (playerDataManager == null)
            return;

        playerDataManager.PlayerReady -= HandlePlayerReady;
    }

    private void HandlePlayerReady()
    {
        EnsureDefaults();
    }

    private void EnsureDefaults()
    {
        CosmeticService.EnsureDefaults(cosmeticCatalog);
    }
}