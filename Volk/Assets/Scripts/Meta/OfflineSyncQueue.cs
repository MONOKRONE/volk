using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Volk.Core;

namespace Volk.Meta
{
    /// <summary>
    /// Offline-first sync queue. Persists operations locally and retries when
    /// internet becomes available (on app resume / focus restore).
    /// Operation types: "save_data", "ghost_profile"
    /// </summary>
    public class OfflineSyncQueue : MonoBehaviour
    {
        public static OfflineSyncQueue Instance { get; private set; }

        private const string QUEUE_FILE = "sync_queue.json";
        private const int MAX_RETRIES = 5;
        private const float PROCESS_COOLDOWN = 30f; // seconds between auto-process attempts

        private string queuePath;
        private SyncQueue queue = new SyncQueue();
        private bool isProcessing;
        private float processCooldownTimer;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            queuePath = Path.Combine(Application.persistentDataPath, QUEUE_FILE);
            LoadQueue();
        }

        void Update()
        {
            if (processCooldownTimer > 0f)
                processCooldownTimer -= Time.deltaTime;
        }

        // =====================================================================
        // PUBLIC API
        // =====================================================================

        /// <summary>
        /// Add an operation to the queue. Persists immediately to disk.
        /// </summary>
        public void EnqueueOperation(string type, string payload)
        {
            var op = new SyncOperation
            {
                id = Guid.NewGuid().ToString(),
                type = type,
                payload = payload,
                timestamp = DateTime.UtcNow.ToString("o"),
                retryCount = 0
            };

            queue.operations.Add(op);
            SaveQueue();
            Debug.Log($"[SyncQueue] Enqueued {type} (total pending: {queue.operations.Count})");

            // Attempt immediate send if online
            if (Application.internetReachability != NetworkReachability.NotReachable && !isProcessing)
                StartCoroutine(ProcessQueueCoroutine());
        }

        /// <summary>
        /// Attempt to flush the queue. Called on resume / focus / auth success.
        /// Respects cooldown to avoid hammering the server.
        /// </summary>
        public void ProcessQueue()
        {
            if (queue.operations.Count == 0) return;
            if (isProcessing) return;
            if (processCooldownTimer > 0f) return;
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Debug.Log("[SyncQueue] No internet — queue will retry later");
                return;
            }
            StartCoroutine(ProcessQueueCoroutine());
        }

        public int PendingCount => queue.operations.Count;

        // =====================================================================
        // PROCESSING
        // =====================================================================

        IEnumerator ProcessQueueCoroutine()
        {
            isProcessing = true;
            processCooldownTimer = PROCESS_COOLDOWN;

            Debug.Log($"[SyncQueue] Processing {queue.operations.Count} queued operations");

            // Iterate a snapshot so we can safely remove during iteration
            var snapshot = new List<SyncOperation>(queue.operations);

            foreach (var op in snapshot)
            {
                if (Application.internetReachability == NetworkReachability.NotReachable)
                    break; // Stop if we lost internet mid-flush

                bool success = false;

                switch (op.type)
                {
                    case "save_data":
                        yield return SendSaveData(op, result => success = result);
                        break;
                    case "ghost_profile":
                        yield return SendGhostProfile(op, result => success = result);
                        break;
                    default:
                        Debug.LogWarning($"[SyncQueue] Unknown op type: {op.type} — discarding");
                        success = true; // discard unknown types
                        break;
                }

                if (success)
                {
                    queue.operations.Remove(op);
                    Debug.Log($"[SyncQueue] Sent {op.type} — removed from queue");
                }
                else
                {
                    op.retryCount++;
                    if (op.retryCount >= MAX_RETRIES)
                    {
                        Debug.LogWarning($"[SyncQueue] {op.type} exceeded max retries — discarding");
                        queue.operations.Remove(op);
                    }
                }

                SaveQueue();
                yield return null;
            }

            isProcessing = false;
            Debug.Log($"[SyncQueue] Processing complete. Remaining: {queue.operations.Count}");
        }

        IEnumerator SendSaveData(SyncOperation op, Action<bool> onResult)
        {
            var supabase = SupabaseManager.Instance;
            if (supabase == null || !supabase.IsAuthenticated) { onResult(false); yield break; }

            SaveData data;
            try { data = JsonUtility.FromJson<SaveData>(op.payload); }
            catch { onResult(false); yield break; }

            bool done = false;
            bool succeeded = false;

            supabase.SaveToCloud(data,
                onSuccess: () => { succeeded = true; done = true; },
                onError: _ => done = true
            );

            yield return new WaitUntil(() => done);
            onResult(succeeded);
        }

        IEnumerator SendGhostProfile(SyncOperation op, Action<bool> onResult)
        {
            var ghost = GhostSyncManager.Instance;
            if (ghost == null) { onResult(false); yield break; }

            string playerId = PlayerPrefs.GetString("player_id", SystemInfo.deviceUniqueIdentifier);
            yield return ghost.UploadProfileQueued(playerId, op.payload, onResult);
        }

        // =====================================================================
        // CONFLICT RESOLUTION (last-write-wins)
        // =====================================================================

        /// <summary>
        /// Compare local and remote timestamps. Returns true if remote is newer.
        /// </summary>
        public static bool IsRemoteNewer(string localTimestamp, string remoteTimestamp)
        {
            if (string.IsNullOrEmpty(remoteTimestamp)) return false;
            if (string.IsNullOrEmpty(localTimestamp)) return true;

            if (DateTime.TryParse(localTimestamp, out var local) &&
                DateTime.TryParse(remoteTimestamp, out var remote))
            {
                return remote > local;
            }
            return false;
        }

        // =====================================================================
        // PERSISTENCE
        // =====================================================================

        void LoadQueue()
        {
            if (!File.Exists(queuePath)) { queue = new SyncQueue(); return; }
            try
            {
                string json = File.ReadAllText(queuePath);
                queue = JsonUtility.FromJson<SyncQueue>(json) ?? new SyncQueue();
                Debug.Log($"[SyncQueue] Loaded {queue.operations.Count} pending operations");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SyncQueue] Failed to load queue: {e.Message} — starting fresh");
                queue = new SyncQueue();
            }
        }

        void SaveQueue()
        {
            try
            {
                string json = JsonUtility.ToJson(queue);
                File.WriteAllText(queuePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SyncQueue] Failed to save queue: {e.Message}");
            }
        }

        // =====================================================================
        // DATA MODELS
        // =====================================================================

        [Serializable]
        public class SyncOperation
        {
            public string id;
            public string type;       // "save_data" | "ghost_profile"
            public string payload;    // JSON string
            public string timestamp;  // ISO 8601 UTC
            public int retryCount;
        }

        [Serializable]
        class SyncQueue
        {
            public List<SyncOperation> operations = new List<SyncOperation>();
        }
    }
}
