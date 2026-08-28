using UnityEngine;

public class CosmeticSystemInitializer : MonoBehaviour
{
    [SerializeField]
    private CosmeticCatalog cosmeticCatalog;

    private void Start()
    {
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("Cosmetic system could not initialise because PlayerDataManager is missing.");

            return;
        }

        CosmeticService.EnsureDefaults(cosmeticCatalog);
    }
}