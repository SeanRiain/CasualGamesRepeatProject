using UnityEngine;

public class GameCosmeticApplier : MonoBehaviour
{
    [Header("Catalogue")]
    [SerializeField]
    private CosmeticCatalog cosmeticCatalog;

    [Header("Paddle")]
    [SerializeField]
    private SpriteRenderer localPaddleRenderer;

    [Header("Background")]
    [SerializeField]
    private Camera gameCamera;

    [Header("Match Border")]
    [SerializeField]
    private SpriteRenderer[] borderRenderers;

    private void Start()
    {
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogWarning("No PlayerDataManager exists. Cosmetics cannot be applied.");

            return;
        }

        if (PlayerDataManager.Instance.IsReady)
        {
            ApplyCurrentPlayerCosmetics();

            return;
        }

        PlayerDataManager.Instance.PlayerReady += HandlePlayerReady;
    }

    private void OnDestroy()
    {
        if (PlayerDataManager.Instance == null)
            return;

        PlayerDataManager.Instance.PlayerReady -= HandlePlayerReady;
    }

    private void HandlePlayerReady()
    {
        PlayerDataManager.Instance.PlayerReady -= HandlePlayerReady;

        ApplyCurrentPlayerCosmetics();
    }

    public void ApplyCurrentPlayerCosmetics()
    {
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogWarning("No PlayerDataManager exists. Cosmetics cannot be applied.");

            return;
        }

        ApplyPaddle();
        ApplyBackground();
        ApplyBorder();
    }

    private void ApplyPaddle()
    {
        CosmeticDefinition definition = GetEquippedDefinition(CosmeticCategory.Paddle);

        if (definition != null && localPaddleRenderer != null)
        {
            localPaddleRenderer.color = definition.DemoColor;
        }
    }

    private void ApplyBackground()
    {
        CosmeticDefinition definition = GetEquippedDefinition(CosmeticCategory.Background);

        if (definition != null && gameCamera != null)
        {
            gameCamera.backgroundColor = definition.DemoColor;
        }
    }

    private void ApplyBorder()
    {
        CosmeticDefinition definition = GetEquippedDefinition(CosmeticCategory.MatchBorder);

        if (definition == null)
            return;

        foreach (SpriteRenderer borderRenderer in borderRenderers)
        {
            if (borderRenderer != null)
            {
                borderRenderer.color = definition.DemoColor;
            }
        }
    }

    private CosmeticDefinition GetEquippedDefinition(CosmeticCategory category)
    {
        string equippedId = PlayerDataManager.Instance.GetEquippedCosmeticId(category);

        CosmeticDefinition definition = cosmeticCatalog.GetById(equippedId);

        if (definition != null && definition.Category == category)
        {
            return definition;
        }

        return cosmeticCatalog.GetDefault(category);
    }
}