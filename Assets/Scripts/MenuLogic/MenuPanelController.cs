using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPanelController : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject friendsPanel;
    [SerializeField] private GameObject storePanel;

    [Header("Scenes")]
    [SerializeField] private string gameSceneName = "Game";

    private void Start()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        SetActivePanel(mainMenuPanel);
    }

    public void ShowFriends()
    {
        SetActivePanel(friendsPanel);
    }

    public void ShowStore()
    {
        SetActivePanel(storePanel);
    }

    private void SetActivePanel(GameObject targetPanel)
    {
        mainMenuPanel.SetActive(targetPanel == mainMenuPanel);
        friendsPanel.SetActive(targetPanel == friendsPanel);
        storePanel.SetActive(targetPanel == storePanel);
    }
}