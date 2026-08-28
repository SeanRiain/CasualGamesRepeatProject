using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreItemUI : MonoBehaviour
{
    [SerializeField]
    private Image previewImage;

    [SerializeField]
    private TMP_Text itemNameText;

    [SerializeField]
    private TMP_Text categoryText;

    [SerializeField]
    private TMP_Text requirementText;

    [SerializeField]
    private TMP_Text priceText;

    [SerializeField]
    private Button actionButton;

    [SerializeField]
    private TMP_Text actionButtonText;

    private CosmeticDefinition definition;

    private Action<CosmeticDefinition> actionRequested;

    public void Configure(CosmeticDefinition item, CosmeticStoreState state, bool canAfford, Action<CosmeticDefinition> onActionRequested)
    {
        definition = item;
        actionRequested = onActionRequested;

        previewImage.color = item.DemoColor;

        itemNameText.text = item.DisplayName;

        categoryText.text = item.Category.ToString();

        if (item.IsDefault)
        {
            priceText.text = "Default";
        }
        else
        {
            priceText.text = $"Price: {item.SoftCurrencyPrice}";
        }

        requirementText.text = state == CosmeticStoreState.Locked ? CosmeticService.GetUnlockDescription(item) : string.Empty;

        actionButton.onClick.RemoveAllListeners();

        actionButton.onClick.AddListener(HandleActionClicked);

        switch (state)
        {
            case CosmeticStoreState.Locked:

                actionButton.interactable = false;
                actionButtonText.text = "Locked";
                break;

            case CosmeticStoreState.Available:

                actionButton.interactable = canAfford;

                actionButtonText.text = canAfford ? "Buy" : "Need Currency";

                break;

            case CosmeticStoreState.Owned:

                actionButton.interactable = true;
                actionButtonText.text = "Equip";
                break;

            case CosmeticStoreState.Equipped:

                actionButton.interactable = false;
                actionButtonText.text = "Equipped";
                break;
        }
    }

    private void HandleActionClicked()
    {
        actionRequested?.Invoke(definition);
    }
}