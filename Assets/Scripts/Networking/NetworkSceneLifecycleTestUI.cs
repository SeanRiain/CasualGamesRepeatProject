using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkSceneLifecycleTestUI : MonoBehaviour
{
    [SerializeField]
    private string targetSceneName;

    public void LoadTargetSceneAsHost()
    {
        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager == null)
        {
            Debug.LogError("[NetworkSceneTest] No NetworkManager exists.");

            return;
        }

        if (!networkManager.IsListening)
        {
            Debug.LogWarning("[NetworkSceneTest] Networking is not active.");

            return;
        }

        if (!networkManager.IsServer)
        {
            Debug.LogWarning("[NetworkSceneTest] Only the host/server may start a synchronized scene load.");

            return;
        }

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogError("[NetworkSceneTest] No target scene has been configured.");

            return;
        }

        SceneEventProgressStatus status = networkManager.SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);

        Debug.Log($"[NetworkSceneTest] Requested synchronized load of '{targetSceneName}'. Status: {status}");

        if (status != SceneEventProgressStatus.Started)
        {
            Debug.LogWarning($"[NetworkSceneTest] Scene load did not start. Status: {status}");
        }
    }
}