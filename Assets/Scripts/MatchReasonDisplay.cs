using TMPro;
using UnityEngine;

public class MatchReasonDisplay : MonoBehaviour
{
    [Header("Match Reason UI")]
    [SerializeField]
    private GameObject reasonPanel;

    [SerializeField]
    private TMP_Text reasonText;

    private void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        HideReason();

        if (FriendsManager.Instance == null)
            return;

        MatchSetupData activeMatch = FriendsManager.Instance.ActiveMatchSetup;

        if (activeMatch == null)
            return;

        string reason = activeMatch.MatchReason?.Trim();

        if (string.IsNullOrWhiteSpace(reason))
            return;

        reasonText.text = reason;
        reasonPanel.SetActive(true);
    }

    private void HideReason()
    {
        if (reasonText != null)
        {
            reasonText.text = string.Empty;
        }

        if (reasonPanel != null)
        {
            reasonPanel.SetActive(false);
        }
    }
}