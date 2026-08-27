using TMPro;
using UnityEngine;

public class AccountHeaderDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text displayNameText;
    [SerializeField] private TMP_Text playerIdText;
    [SerializeField] private TMP_Text currencyText;
    [SerializeField] private TMP_Text overallRecordText;

    private void Start()
    {
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("No PlayerDataManager exists when AccountHeaderDisplay starts.");

            return;
        }

        PlayerDataManager.Instance.CurrencyChanged += UpdateCurrency;
        PlayerDataManager.Instance.RecordChanged += UpdateRecord;

        RefreshAccountDisplay();
    }

    private void OnDestroy()
    {
        if (PlayerDataManager.Instance == null)
            return;

        PlayerDataManager.Instance.CurrencyChanged -= UpdateCurrency;
        PlayerDataManager.Instance.RecordChanged -= UpdateRecord;
    }

    private void RefreshAccountDisplay()
    {
        displayNameText.text =$"Username: {PlayerDataManager.Instance.DisplayName}";

        playerIdText.text =$"ID: {PlayerDataManager.Instance.PlayerId}";

        UpdateCurrency(PlayerDataManager.Instance.Currency);

        UpdateRecord(PlayerDataManager.Instance.TotalWins, PlayerDataManager.Instance.TotalLosses);
    }

    private void UpdateCurrency(int amount)
    {
        currencyText.text = $"Currency: {amount}";
    }

    private void UpdateRecord(int wins, int losses)
    {
        overallRecordText.text = $"Record: {wins} W / {losses} L";
    }
}