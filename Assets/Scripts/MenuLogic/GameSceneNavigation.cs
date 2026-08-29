using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneNavigation : MonoBehaviour
{
    [SerializeField]
    private string menusSceneName = "Menus";

    public void ReturnToMenus()
    {
        if (FriendsManager.Instance != null)
        {
            FriendsManager.Instance.ClearActiveMatchSetup();
        }

        SceneManager.LoadScene(menusSceneName);
    }
}