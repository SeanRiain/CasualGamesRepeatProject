using TMPro;
using UnityEngine;

public class AccountHeaderDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text displayNameText;
    [SerializeField] private TMP_Text playerIdText;
    [SerializeField] private TMP_Text currencyText;
    [SerializeField] private TMP_Text overallRecordText;

    private PlayerDataManager playerDataManager;

    private void OnEnable()
    {
        playerDataManager = PlayerDataManager.Instance;

        if (playerDataManager == null)
        {
            Debug.LogError("No PlayerDataManager exists when AccountHeaderDisplay starts.");
            return;
        }

        playerDataManager.PlayerReady += HandlePlayerReady;
        playerDataManager.CurrencyChanged += UpdateCurrency;
        playerDataManager.RecordChanged += UpdateRecord;

        if (playerDataManager.IsReady)
        {
            RefreshAccountDisplay();
        }
        else
        {
            ShowLoadingState();
        }
    }

    private void OnDisable()
    {
        if (playerDataManager == null)
            return;

        playerDataManager.PlayerReady -= HandlePlayerReady;
        playerDataManager.CurrencyChanged -= UpdateCurrency;
        playerDataManager.RecordChanged -= UpdateRecord;
    }

    private void HandlePlayerReady()
    {
        RefreshAccountDisplay();
    }

    private void ShowLoadingState()
    {
        displayNameText.text = "Username: Loading...";
        playerIdText.text = "ID: Loading...";
        currencyText.text = "Currency: --";
        overallRecordText.text = "Record: --";
    }

    private void RefreshAccountDisplay()
    {
        if (playerDataManager == null || !playerDataManager.IsReady)
        {
            return;
        }

        displayNameText.text = $"Username: {playerDataManager.DisplayName}";
        playerIdText.text = $"ID: {playerDataManager.PlayerId}";

        UpdateCurrency(playerDataManager.Currency);
        UpdateRecord(playerDataManager.TotalWins, playerDataManager.TotalLosses);
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