using UnityEngine;

[CreateAssetMenu(fileName = "Cosmetic_", menuName = "Casual Pong/Cosmetics/Cosmetic Definition")]
public class CosmeticDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string cosmeticId;
    [SerializeField] private string displayName;

    [Header("Category")]
    [SerializeField] private CosmeticCategory category;

    [Header("Store")]
    [SerializeField] private int softCurrencyPrice;
    [SerializeField] private bool isDefault;

    [Header("Unlock Requirement")]
    [SerializeField] private CosmeticUnlockType unlockType;
    [SerializeField] private int unlockThreshold;

    [Header("MVP Appearance")]
    [SerializeField] private Color demoColor = Color.white;

    public string CosmeticId => cosmeticId;
    public string DisplayName => displayName;

    public CosmeticCategory Category => category;

    public int SoftCurrencyPrice => softCurrencyPrice;
    public bool IsDefault => isDefault;

    public CosmeticUnlockType UnlockType => unlockType;
    public int UnlockThreshold => unlockThreshold;

    public Color DemoColor => demoColor;

    private void OnValidate()
    {
        if (cosmeticId != null)
        {
            cosmeticId = cosmeticId.Trim();
        }

        softCurrencyPrice = Mathf.Max(0, softCurrencyPrice);

        unlockThreshold = Mathf.Max(0, unlockThreshold);

        // Default cosmetics must always be usable.
        if (isDefault)
        {
            softCurrencyPrice = 0;
            unlockType = CosmeticUnlockType.None;
            unlockThreshold = 0;
        }
    }
}