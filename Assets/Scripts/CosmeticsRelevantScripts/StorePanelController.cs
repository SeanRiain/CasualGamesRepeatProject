using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StorePanelController : MonoBehaviour
{
    [Header("Catalogue")]
    [SerializeField]
    private CosmeticCatalog cosmeticCatalog;

    [Header("Dynamic UI")]
    [SerializeField]
    private Transform storeItemContent;

    [SerializeField]
    private StoreItemUI storeItemPrefab;

    [Header("Feedback")]
    [SerializeField]
    private TMP_Text feedbackText;

    private readonly List<StoreItemUI> itemViews = new List<StoreItemUI>();

    private void OnEnable()
    {
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("Store cannot open because PlayerDataManager is missing.");

            return;
        }

        PlayerDataManager.Instance.CurrencyChanged += HandleCurrencyChanged;

        PlayerDataManager.Instance.RecordChanged += HandleRecordChanged;

        PlayerDataManager.Instance.CosmeticsChanged += HandleCosmeticsChanged;

        CosmeticService.EnsureDefaults(cosmeticCatalog);

        BuildStoreIfNecessary();

        RefreshStore();

        if (feedbackText != null)
        {
            feedbackText.text = string.Empty;
        }
    }

    private void OnDisable()
    {
        if (PlayerDataManager.Instance == null)
            return;

        PlayerDataManager.Instance.CurrencyChanged -= HandleCurrencyChanged;

        PlayerDataManager.Instance.RecordChanged -= HandleRecordChanged;

        PlayerDataManager.Instance.CosmeticsChanged -= HandleCosmeticsChanged;
    }

    private void BuildStoreIfNecessary()
    {
        if (itemViews.Count == cosmeticCatalog.Items.Count)
        {
            return;
        }

        foreach (Transform child in storeItemContent)
        {
            Destroy(child.gameObject);
        }

        itemViews.Clear();

        foreach (CosmeticDefinition definition in cosmeticCatalog.Items)
        {
            if (definition == null)
                continue;

            StoreItemUI item = Instantiate(storeItemPrefab, storeItemContent);

            itemViews.Add(item);
        }
    }

    private void RefreshStore()
    {
        if (PlayerDataManager.Instance == null)
            return;

        int viewIndex = 0;

        foreach (CosmeticDefinition definition in cosmeticCatalog.Items)
        {
            if (definition == null)
                continue;

            if (viewIndex >= itemViews.Count)
                break;

            CosmeticStoreState state = CosmeticService.GetState(definition);

            bool canAfford = PlayerDataManager.Instance.CanAfford(definition.SoftCurrencyPrice);

            itemViews[viewIndex].Configure(definition, state, canAfford, HandleItemAction);

            viewIndex++;
        }
    }

    private void HandleItemAction(
        CosmeticDefinition definition)
    {
        CosmeticStoreState state = CosmeticService.GetState(definition);

        string message;

        switch (state)
        {
            case CosmeticStoreState.Available:

                CosmeticService.TryPurchase(definition, out message);

                break;

            case CosmeticStoreState.Owned:

                CosmeticService.TryEquip(definition, out message);

                break;

            default:

                message = "No action is currently available.";

                break;
        }

        if (feedbackText != null)
        {
            feedbackText.text = message;
        }

        RefreshStore();
    }

    private void HandleCurrencyChanged(int amount)
    {
        RefreshStore();
    }

    private void HandleRecordChanged(int wins, int losses)
    {
        //Unlock state may depend on wins or matches.
        RefreshStore();
    }

    private void HandleCosmeticsChanged()
    {
        RefreshStore();
    }
}