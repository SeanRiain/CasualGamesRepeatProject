using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendListItemUI : MonoBehaviour
{
    [SerializeField]
    private TMP_Text friendNameText;

    [SerializeField]
    private TMP_Text pairRecordText;

    [SerializeField]
    private TMP_Text matchesPlayedText;

    [SerializeField]
    private Button challengeButton;

    private string friendPlayerId;

    private Action<string> challengeRequested;

    public void Configure(
        PlayerProfileData friendProfile,
        FriendRelationshipData relationship,
        string currentPlayerId,
        Action<string> onChallengeRequested)
    {
        friendPlayerId = friendProfile.PlayerId;

        challengeRequested = onChallengeRequested;

        friendNameText.text = friendProfile.DisplayName;

        int wins = relationship.GetWinsFor(currentPlayerId);

        int losses = relationship.GetLossesFor(currentPlayerId);

        pairRecordText.text = $"{wins} W / {losses} L";

        matchesPlayedText.text = $"{relationship.MatchesPlayed} matches";

        challengeButton.onClick.RemoveAllListeners();

        challengeButton.onClick.AddListener(HandleChallengeClicked);
    }

    private void HandleChallengeClicked()
    {
        challengeRequested?.Invoke(friendPlayerId);
    }
}