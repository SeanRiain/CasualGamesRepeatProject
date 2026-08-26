using TMPro;
using UnityEngine;

public class CurrencyDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text currencyText;

    private void Start()
    {
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("No PlayerDataManager exists in the scene.");

            return;
        }

        PlayerDataManager.Instance.CurrencyChanged += UpdateCurrencyDisplay;

        UpdateCurrencyDisplay(PlayerDataManager.Instance.Currency);
    }

    private void OnDestroy()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.CurrencyChanged -= UpdateCurrencyDisplay;
        }
    }

    private void UpdateCurrencyDisplay(int currency)
    {
        currencyText.text = $"Currency: {currency}";
    }
}