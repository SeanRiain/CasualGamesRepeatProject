using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using UnityEngine;

[RequireComponent(typeof(PlayerDataManager))]
public class PlayerCloudPersistence : MonoBehaviour
{
    private const string AccountKey = "player_account_v1";

    [Header("Cosmetics")]
    [SerializeField] private CosmeticCatalog cosmeticCatalog;

    [Header("Saving")]
    [SerializeField]
    [Min(0.1f)]
    private float saveDebounceSeconds = 0.75f;

    private PlayerDataManager playerDataManager;

    private bool initializationComplete;
    private bool saveRequested;
    private bool saveInProgress;

    private Coroutine pendingSaveCoroutine;

    public bool IsInitialized => initializationComplete;

    public bool IsSaving => saveInProgress;

    private bool suppressAutoSave;

    private void Awake()
    {
        playerDataManager = GetComponent<PlayerDataManager>();
    }

    private void OnEnable()
    {
        playerDataManager.CurrencyChanged += HandleCurrencyChanged;
        playerDataManager.RecordChanged += HandleRecordChanged;
        playerDataManager.CosmeticsChanged += HandleCosmeticsChanged;
    }

    private async void Start()
    {
        await InitializeCloudAccountAsync();
    }

    private void OnDisable()
    {
        if (playerDataManager == null)
            return;

        playerDataManager.CurrencyChanged -= HandleCurrencyChanged;
        playerDataManager.RecordChanged -= HandleRecordChanged;
        playerDataManager.CosmeticsChanged -= HandleCosmeticsChanged;
    }

    private async Task InitializeCloudAccountAsync()
    {
        if (cosmeticCatalog == null)
        {
            Debug.LogError("[CloudSave] No CosmeticCatalog has been assigned.");
            InitializeLocalFallback();
            return;
        }

        NetworkSessionController services = NetworkSessionController.Instance;

        if (services == null)
        {
            Debug.LogError("[CloudSave] No NetworkSessionController exists. Start the project from Bootstrap.");
            InitializeLocalFallback();
            return;
        }

        Debug.Log("[CloudSave] Waiting for UGS authentication.");

        bool servicesReady = await services.EnsureServicesReadyAsync();

        if (!servicesReady)
        {
            Debug.LogError("[CloudSave] UGS authentication was unavailable.");
            InitializeLocalFallback();
            return;
        }

        string authenticatedPlayerId = services.AuthenticatedPlayerId;

        if (string.IsNullOrWhiteSpace(authenticatedPlayerId))
        {
            Debug.LogError("[CloudSave] Authentication returned no Player ID.");
            InitializeLocalFallback();
            return;
        }

        try
        {
            Debug.Log($"[CloudSave] Loading account for UGS Player {authenticatedPlayerId}.");

            var results = await CloudSaveService.Instance.Data.Player.LoadAsync(
                new HashSet<string>
                {
                    AccountKey
                });

            if (results.TryGetValue(AccountKey, out var savedItem))
            {
                PlayerCloudSaveData cloudData = savedItem.Value.GetAs<PlayerCloudSaveData>();

                if (cloudData == null)
                {
                    Debug.LogError("[CloudSave] Saved account could not be deserialized.");
                    InitializeLocalFallback();
                    return;
                }

                if (cloudData.schemaVersion > PlayerCloudSaveData.CurrentSchemaVersion)
                {
                    Debug.LogError($"[CloudSave] Account schema {cloudData.schemaVersion} is newer than this client supports.");
                    InitializeLocalFallback();
                    return;
                }

                PlayerData loadedPlayer = cloudData.ToPlayerData(authenticatedPlayerId, playerDataManager.DefaultDisplayName);

                bool defaultsRepaired = CosmeticService.EnsureDefaults(loadedPlayer, cosmeticCatalog);

                playerDataManager.InitializePlayer(loadedPlayer, cloudBacked: true);

                initializationComplete = true;

                Debug.Log("[CloudSave] Existing player account loaded successfully.");

                if (defaultsRepaired || cloudData.schemaVersion != PlayerCloudSaveData.CurrentSchemaVersion)
                {
                    Debug.Log("[CloudSave] Loaded account was normalized. Saving updated snapshot.");
                    await SaveCurrentPlayerAsync();
                }

                return;
            }

            Debug.Log("[CloudSave] No existing account was found. Creating a new one.");

            PlayerData newPlayer = playerDataManager.CreateDefaultCloudPlayer(authenticatedPlayerId);

            CosmeticService.EnsureDefaults(newPlayer, cosmeticCatalog);

            playerDataManager.InitializePlayer(newPlayer, cloudBacked: true);

            initializationComplete = true;

            bool initialSaveSucceeded = await SaveCurrentPlayerAsync();

            if (initialSaveSucceeded)
            {
                Debug.Log("[CloudSave] New player account created and saved.");
            }
            else
            {
                Debug.LogWarning("[CloudSave] New account exists locally, but its first cloud save failed.");
            }
        }
        catch (CloudSaveException exception)
        {
            Debug.LogError("[CloudSave] Account load failed.");
            Debug.LogException(exception);

            InitializeLocalFallback();
        }
        catch (Exception exception)
        {
            Debug.LogError("[CloudSave] Unexpected account initialization failure.");
            Debug.LogException(exception);

            InitializeLocalFallback();
        }
    }

    private void InitializeLocalFallback()
    {
        if (playerDataManager.IsReady)
            return;

        PlayerData fallbackPlayer = playerDataManager.CreateLocalFallbackPlayer();

        if (cosmeticCatalog != null)
        {
            CosmeticService.EnsureDefaults(fallbackPlayer, cosmeticCatalog);
        }

        playerDataManager.InitializePlayer(fallbackPlayer, cloudBacked: false);

        initializationComplete = true;

        Debug.LogWarning("[CloudSave] Using local fallback player data. Changes in this session will not be cloud-persisted.");
    }

    private void HandleCurrencyChanged(int amount)
    {
        QueueSave();
    }

    private void HandleRecordChanged(int wins, int losses)
    {
        QueueSave();
    }

    private void HandleCosmeticsChanged()
    {
        QueueSave();
    }

    private void QueueSave()
    {

        if (suppressAutoSave)
            return;

        if (!initializationComplete)
            return;

        if (!playerDataManager.IsReady || !playerDataManager.IsCloudBacked)
        {
            return;
        }

        saveRequested = true;

        if (pendingSaveCoroutine != null)
        {
            StopCoroutine(pendingSaveCoroutine);
        }

        pendingSaveCoroutine = StartCoroutine(SaveAfterDebounce());
    }

    public async Task<bool> ReloadCurrentPlayerFromCloudAsync()
    {
        if (!initializationComplete)
        {
            Debug.LogWarning("[CloudSave] Cannot reload because initialization is incomplete.");
            return false;
        }

        if (!playerDataManager.IsCloudBacked)
        {
            Debug.LogWarning("[CloudSave] Cannot reload a non-cloud-backed account.");
            return false;
        }

        NetworkSessionController services = NetworkSessionController.Instance;

        if (services == null)
        {
            Debug.LogError("[CloudSave] No NetworkSessionController exists.");
            return false;
        }

        if (!await services.EnsureServicesReadyAsync())
        {
            return false;
        }

        string authenticatedPlayerId = services.AuthenticatedPlayerId;

        try
        {
            var results = await CloudSaveService.Instance.Data.Player.LoadAsync(
                new HashSet<string>
                {
                AccountKey
                });

            if (!results.TryGetValue(AccountKey, out var savedItem))
            {
                Debug.LogError("[CloudSave] Existing cloud account disappeared during reload.");
                return false;
            }

            PlayerCloudSaveData cloudData = savedItem.Value.GetAs<PlayerCloudSaveData>();

            if (cloudData == null)
            {
                Debug.LogError("[CloudSave] Reloaded account could not be deserialized.");
                return false;
            }

            if (cloudData.schemaVersion > PlayerCloudSaveData.CurrentSchemaVersion)
            {
                Debug.LogError("[CloudSave] Reloaded account uses an unsupported schema.");
                return false;
            }

            PlayerData loadedPlayer = cloudData.ToPlayerData(authenticatedPlayerId, playerDataManager.DefaultDisplayName);

            bool defaultsRepaired = CosmeticService.EnsureDefaults(loadedPlayer, cosmeticCatalog);

            suppressAutoSave = true;

            try
            {
                playerDataManager.InitializePlayer(loadedPlayer, cloudBacked: true);
            }
            finally
            {
                suppressAutoSave = false;
            }

            Debug.Log("[CloudSave] Player account reloaded after backend update.");

            if (defaultsRepaired || cloudData.schemaVersion != PlayerCloudSaveData.CurrentSchemaVersion)
            {
                await SaveCurrentPlayerAsync();
            }

            return true;
        }
        catch (CloudSaveException exception)
        {
            Debug.LogError("[CloudSave] Post-settlement account reload failed.");
            Debug.LogException(exception);

            return false;
        }
        catch (Exception exception)
        {
            Debug.LogError("[CloudSave] Unexpected post-settlement reload failure.");
            Debug.LogException(exception);

            return false;
        }
    }

    private IEnumerator SaveAfterDebounce()
    {
        yield return new WaitForSecondsRealtime(saveDebounceSeconds);

        pendingSaveCoroutine = null;

        _ = FlushSaveQueueAsync();
    }

    private async Task FlushSaveQueueAsync()
    {
        if (saveInProgress)
            return;

        if (!initializationComplete || !playerDataManager.IsCloudBacked)
        {
            return;
        }

        saveInProgress = true;

        try
        {
            while (saveRequested)
            {
                saveRequested = false;

                bool saveSucceeded = await SaveCurrentPlayerAsync();

                if (!saveSucceeded)
                {
                    // Keep the account dirty.
                    // The next mutation or manual
                    // save will try again.
                    saveRequested = true;

                    break;
                }
            }
        }
        finally
        {
            saveInProgress = false;
        }
    }

    private async Task<bool> SaveCurrentPlayerAsync()
    {
        if (!playerDataManager.IsReady || !playerDataManager.IsCloudBacked || playerDataManager.CurrentPlayer == null)
        {
            return false;
        }

        PlayerCloudSaveData snapshot = PlayerCloudSaveData.FromPlayerData(playerDataManager.CurrentPlayer);

        Dictionary<string, object> data = new Dictionary<string, object>
        {
            {
                AccountKey,
                snapshot
            }
        };

        try
        {
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);

            Debug.Log($"[CloudSave] Saved player account. Currency: {snapshot.softCurrency}, Record: {snapshot.totalWins}-{snapshot.totalLosses}.");

            return true;
        }
        catch (CloudSaveException exception)
        {
            Debug.LogError("[CloudSave] Player account save failed.");
            Debug.LogException(exception);

            return false;
        }
        catch (Exception exception)
        {
            Debug.LogError("[CloudSave] Unexpected account save failure.");
            Debug.LogException(exception);

            return false;
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (!paused)
            return;

        if (!initializationComplete || !playerDataManager.IsCloudBacked)
        {
            return;
        }

        saveRequested = true;

        _ = FlushSaveQueueAsync();
    }

    [ContextMenu("Debug/Force Cloud Save Now")]
    private void DebugForceCloudSaveNow()
    {
        if (!Application.isPlaying)
            return;

        if (!initializationComplete || !playerDataManager.IsCloudBacked)
        {
            Debug.LogWarning("[CloudSave] No cloud-backed account is ready.");
            return;
        }

        saveRequested = true;

        _ = FlushSaveQueueAsync();
    }
}