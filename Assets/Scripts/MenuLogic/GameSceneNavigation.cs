using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneNavigation : MonoBehaviour
{
    [SerializeField] private string menusSceneName = "Menus";

    public void ReturnToMenus()
    {
        SceneManager.LoadScene(menusSceneName);
    }
}
