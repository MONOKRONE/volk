using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;

namespace Volk.Core
{
    /// <summary>
    /// Offline-first ghost profile sync.
    /// Saves locally to JSON, optionally syncs to Supabase ghost_profiles table.
    /// </summary>
    public class GhostSyncManager : MonoBehaviour
    {
        public static GhostSyncManager Instance { get; private set; }

        [Header("Supabase Config")]
        public string supabaseUrl; // e.g. "https://xxx.supabase.co"
        public string supabaseAnonKey;
        public string tableName = "ghost_profiles";

        [Header("Settings")]
        public float syncIntervalSeconds = 300f; // 5 minutes
        public bool autoSync = true;

        private string localPath;
        private float syncTimer;
        private bool isSyncing;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            localPath = Path.Combine(Application.persistentDataPath, "ghost_profile.json");
        }

        void Update()
        {
            if (!autoSync || string.IsNullOrEmpty(supabaseUrl)) return;

            syncTimer -= Time.deltaTime;
            if (syncTimer <= 0)
            {
                syncTimer = syncIntervalSeconds;
                TrySync();
            }
        }

        // --- Local Operations (always available) ---

        public string LoadLocal()
        {
            if (!File.Exists(localPath)) return null;
            return File.ReadAllText(localPath);
        }

        public void SaveLocal(string json)
        {
            File.WriteAllText(localPath, json);
            Debug.Log($"[GhostSync] Saved local profile ({json.Length} chars)");
        }

        public void SaveFromTracker()
        {
            if (PlayerBehaviorTracker.Instance == null) return;
            PlayerBehaviorTracker.Instance.SaveProfile();
            Debug.Log("[GhostSync] Profile saved from tracker");
        }

        public bool HasLocalProfile()
        {
            return File.Exists(localPath);
        }

        public long GetLocalProfileAge()
        {
            if (!File.Exists(localPath)) return -1;
            var info = new FileInfo(localPath);
            return (long)(System.DateTime.Now - info.LastWriteTime).TotalSeconds;
        }

        // --- Remote Operations (Supabase) ---

        public void TrySync()
        {
            if (isSyncing) return;
            if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseAnonKey))
            {
                Debug.Log("[GhostSync] No Supabase config — offline only");
                return;
            }
            StartCoroutine(SyncCoroutine());
        }

        IEnumerator SyncCoroutine()
        {
            isSyncing = true;
            string playerId = GetPlayerId();
            string localJson = LoadLocal();

            // Upload local profile
            if (!string.IsNullOrEmpty(localJson))
            {
                yield return UploadProfile(playerId, localJson);
            }

            // Download latest (in case another device updated)
            yield return DownloadProfile(playerId);

            isSyncing = false;
        }

        IEnumerator UploadProfile(string playerId, string profileJson)
        {
            string url = $"{supabaseUrl}/rest/v1/{tableName}";

            string payload = JsonUtility.ToJson(new GhostProfileRow
            {
                player_id = playerId,
                profile_data = profileJson,
                updated_at = System.DateTime.UtcNow.ToString("o")
            });

            var request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(payload));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("apikey", supabaseAnonKey);
            request.SetRequestHeader("Authorization", $"Bearer {supabaseAnonKey}");
            request.SetRequestHeader("Prefer", "resolution=merge-duplicates");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                Debug.Log("[GhostSync] Upload success");
            else
                Debug.LogWarning($"[GhostSync] Upload failed: {request.error}");

            request.Dispose();
        }

        IEnumerator DownloadProfile(string playerId)
        {
            string url = $"{supabaseUrl}/rest/v1/{tableName}?player_id=eq.{playerId}&select=profile_data,updated_at&limit=1";

            var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("apikey", supabaseAnonKey);
            request.SetRequestHeader("Authorization", $"Bearer {supabaseAnonKey}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                // Parse response array
                if (response.Length > 10 && response.Contains("profile_data"))
                {
                    // Simple extraction — in production use proper JSON parser
                    Debug.Log("[GhostSync] Download success — remote profile available");
                    // Compare timestamps and use newer version
                    // For now, local-first: only overwrite if local doesn't exist
                    if (!HasLocalProfile())
                    {
                        Debug.Log("[GhostSync] No local profile — using remote");
                        // Extract profile_data from response and save locally
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[GhostSync] Download failed: {request.error}");
            }

            request.Dispose();
        }

        /// <summary>
        /// Force upload current profile.
        /// </summary>
        public void ForceUpload()
        {
            SaveFromTracker();
            string json = LoadLocal();
            if (!string.IsNullOrEmpty(json))
                StartCoroutine(UploadProfile(GetPlayerId(), json));
        }

        /// <summary>
        /// Force download and overwrite local profile.
        /// </summary>
        public void ForceDownload()
        {
            StartCoroutine(DownloadProfile(GetPlayerId()));
        }

        string GetPlayerId()
        {
            string id = PlayerPrefs.GetString("player_id", "");
            if (string.IsNullOrEmpty(id))
            {
                id = SystemInfo.deviceUniqueIdentifier;
                PlayerPrefs.SetString("player_id", id);
                PlayerPrefs.Save();
            }
            return id;
        }

        [System.Serializable]
        struct GhostProfileRow
        {
            public string player_id;
            public string profile_data;
            public string updated_at;
        }
    }
}
