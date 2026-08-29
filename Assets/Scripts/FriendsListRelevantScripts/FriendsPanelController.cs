using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FriendsPanelController : MonoBehaviour
{
    [Header("Add Friend")]
    [SerializeField]
    private TMP_InputField friendIdInput;

    [SerializeField]
    private TMP_Text addFriendFeedbackText;


    [Header("Friend List")]
    [SerializeField]
    private Transform friendListContent;

    [SerializeField]
    private FriendListItemUI friendListItemPrefab;

    [SerializeField]
    private GameObject noFriendsText;


    [Header("Challenge")]
    [SerializeField]
    private GameObject challengePanel;

    [SerializeField]
    private TMP_Text opponentNameText;

    [SerializeField]
    private TMP_InputField matchReasonInput;

    [SerializeField]
    private TMP_Text challengeFeedbackText;


    [Header("Local Match Testing")]
    [SerializeField]
    private string gameSceneName = "GameSession";


    private string selectedFriendPlayerId;


    private void OnEnable()
    {
        if (FriendsManager.Instance == null)
        {
            Debug.LogError("No FriendsManager exists.");

            return;
        }

        FriendsManager.Instance.FriendsChanged += RefreshFriendList;

        RefreshFriendList();

        CloseChallengePanel();
    }

    private void OnDisable()
    {
        if (FriendsManager.Instance != null)
        {
            FriendsManager.Instance.FriendsChanged -= RefreshFriendList;
        }
    }

    public void TryAddFriendFromInput()
    {
        if (FriendsManager.Instance == null)
            return;

        bool success = FriendsManager.Instance.TryAddFriend(friendIdInput.text, out string message);

        addFriendFeedbackText.text = message;

        if (success)
        {
            friendIdInput.text = string.Empty;
        }
    }

    private void RefreshFriendList()
    {
        if (FriendsManager.Instance == null || PlayerDataManager.Instance == null)
        {
            return;
        }

        for (int i = friendListContent.childCount - 1; i >= 0;i--)
        {
            Destroy(friendListContent.GetChild(i).gameObject);
        }

        List<FriendRelationshipData> relationships = FriendsManager.Instance.GetCurrentPlayerRelationships();

        if (noFriendsText != null)
        {
            noFriendsText.SetActive(relationships.Count == 0);
        }

        foreach (FriendRelationshipData relationship in relationships)
        {
            string friendPlayerId = relationship.GetOtherPlayerId(PlayerDataManager.Instance.PlayerId);

            PlayerProfileData friendProfile = FriendsManager.Instance.GetProfileForPlayer(friendPlayerId);

            if (friendProfile == null)
                continue;

            FriendListItemUI item = Instantiate(friendListItemPrefab,friendListContent);

            item.Configure(friendProfile, relationship, PlayerDataManager.Instance.PlayerId, OpenChallengePanel);
        }
    }

    public void OpenChallengePanel(string friendPlayerId)
    {
        PlayerProfileData friendProfile = FriendsManager.Instance.GetProfileForPlayer(friendPlayerId);

        if (friendProfile == null)
            return;

        selectedFriendPlayerId = friendPlayerId;

        opponentNameText.text = $"Challenge {friendProfile.DisplayName}";

        matchReasonInput.text = string.Empty;

        challengeFeedbackText.text = string.Empty;

        challengePanel.SetActive(true);
    }

    public void CloseChallengePanel()
    {
        selectedFriendPlayerId =
            null;

        if (matchReasonInput != null)
        {
            matchReasonInput.text = string.Empty;
        }

        if (challengeFeedbackText != null)
        {
            challengeFeedbackText.text = string.Empty;
        }

        if (challengePanel != null)
        {
            challengePanel.SetActive(false);
        }
    }

    public void PrepareChallenge()
    {
        if (string.IsNullOrEmpty(selectedFriendPlayerId))
        {
            challengeFeedbackText.text = "No friend is selected.";

            return;
        }


        bool success = FriendsManager.Instance.TryPrepareChallenge(selectedFriendPlayerId, matchReasonInput.text, out string message);

        challengeFeedbackText.text = message;

        if (success)
        {
            Debug.Log("Challenge prepared locally. Network transmission is not implemented yet.");
        }
    }

    [ContextMenu("Debug/Simulate Prepared Challenge Acceptance")]
    public void SimulatePreparedChallengeAcceptance()
    {
        if (FriendsManager.Instance == null)
        {
            Debug.LogError("No FriendsManager exists.");

            return;
        }

        bool success = FriendsManager.Instance.TryActivatePreparedChallengeForLocalTest(out string message);

        if (challengeFeedbackText != null)
        {
            challengeFeedbackText.text = message;
        }

        if (!success)
        {
            Debug.LogWarning(message);
            return;
        }

        Debug.Log("Prepared challenge accepted locally for testing. The CPU will temporarily control the opponent paddle.");

        SceneManager.LoadScene(gameSceneName);
    }
}