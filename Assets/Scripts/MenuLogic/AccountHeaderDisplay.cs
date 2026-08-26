using TMPro;
using UnityEngine;

public class AccountHeaderDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text displayNameText;
    [SerializeField] private TMP_Text playerIdText;
    [SerializeField] private TMP_Text currencyText;

    private void Start()
    {
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("No PlayerDataManager exists when AccountHeaderDisplay starts.");

            return;
        }

        PlayerDataManager.Instance.CurrencyChanged += UpdateCurrency;

        RefreshAccountDisplay();
    }

    private void OnDestroy()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.CurrencyChanged -= UpdateCurrency;
        }
    }

    private void RefreshAccountDisplay()
    {
        displayNameText.text = PlayerDataManager.Instance.DisplayName;

        playerIdText.text = $"ID: {PlayerDataManager.Instance.PlayerId}";

        UpdateCurrency(PlayerDataManager.Instance.Currency);
    }

    private void UpdateCurrency(int amount)
    {
        currencyText.text = $"Currency: {amount}";
    }
}