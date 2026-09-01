using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapLoader : MonoBehaviour
{
    [SerializeField]
    private string menusSceneName = "Menus";

    private void Start()
    {
        SceneManager.LoadScene(menusSceneName, LoadSceneMode.Single);
    }
}