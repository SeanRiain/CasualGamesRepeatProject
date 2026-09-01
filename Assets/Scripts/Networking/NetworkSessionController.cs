using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NetworkManager))]
public class NetworkSessionController : MonoBehaviour
{
    public static NetworkSessionController Instance
    {
        get;
        private set;
    }

    private NetworkManager networkManager;

    private bool closeInProgress;
    private string pendingCloseSceneName;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        networkManager = GetComponent<NetworkManager>();
    }

    private void OnDestroy()
    {
        UnsubscribeFromSceneEvents();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool TryCloseSessionToScene(
        string sceneName)
    {
        if (closeInProgress)
        {
            return false;
        }

        if (networkManager == null)
        {
            Debug.LogError("[NetworkSession] No NetworkManager exists.");

            return false;
        }

        if (!networkManager.IsListening)
        {
            Debug.LogWarning("[NetworkSession] Networking is not active.");

            return false;
        }

        if (!networkManager.IsServer)
        {
            Debug.LogWarning("[NetworkSession] Only the server may close the network session.");

            return false;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[NetworkSession] No return scene was provided.");

            return false;
        }

        closeInProgress = true;
        pendingCloseSceneName = sceneName;

        networkManager.SceneManager.OnSceneEvent += HandleSceneEvent;

        SceneEventProgressStatus status = networkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);

        if (status != SceneEventProgressStatus.Started)
        {
            Debug.LogError("[NetworkSession] Could not begin synchronized load of '{sceneName}'. Status: {status}");

            UnsubscribeFromSceneEvents();

            closeInProgress = false;
            pendingCloseSceneName = null;

            return false;
        }

        Debug.Log("[NetworkSession] Closing session. Returning all players to '{sceneName}'.");

        return true;
    }

    private void HandleSceneEvent(SceneEvent sceneEvent)
    {
        if (!closeInProgress)
            return;

        if (networkManager == null || !networkManager.IsServer)
        {
            return;
        }

        if (sceneEvent.SceneEventType != SceneEventType.LoadEventCompleted)
        {
            return;
        }

        if (sceneEvent.SceneName != pendingCloseSceneName)
        {
            return;
        }

        if (sceneEvent.ClientsThatTimedOut != null && sceneEvent.ClientsThatTimedOut.Count > 0)
        {
            Debug.LogWarning("[NetworkSession] One or more clients timed out while returning to Menus.");
        }

        UnsubscribeFromSceneEvents();

        StartCoroutine(ShutdownAfterSceneLoad());
    }

    private IEnumerator ShutdownAfterSceneLoad()
    {
        yield return null;

        if (networkManager != null && networkManager.IsListening)
        {
            Debug.Log("[NetworkSession] All available players returned. Shutting down NGO.");

            networkManager.Shutdown();
        }

        closeInProgress = false;
        pendingCloseSceneName = null;
    }

    private void UnsubscribeFromSceneEvents()
    {
        if (networkManager == null || networkManager.SceneManager == null)
        {
            return;
        }

        networkManager.SceneManager.OnSceneEvent -= HandleSceneEvent;
    }
}