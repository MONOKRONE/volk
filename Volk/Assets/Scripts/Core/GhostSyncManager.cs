using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.IO;
using Volk.Meta;

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

        // PLA-153: Supabase table schema (run via Supabase SQL Editor):
        // CREATE TABLE IF NOT EXISTS ghost_profiles (
        //   player_id TEXT PRIMARY KEY,
        //   profile_data JSONB NOT NULL,
        //   updated_at TIMESTAMPTZ DEFAULT now()
        // );
        // ALTER TABLE ghost_profiles ENABLE ROW LEVEL SECURITY;
        // CREATE POLICY "Users can manage own profile" ON ghost_profiles
        //   FOR ALL USING (auth.uid()::text = player_id);

        /// <summary>
        /// PLA-153: Verify ghost_profiles table exists. Logs setup instructions if missing.
        /// </summary>
        public void EnsureTable()
        {
            if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseAnonKey)) return;
            StartCoroutine(EnsureTableCoroutine());
        }

        IEnumerator EnsureTableCoroutine()
        {
            string url = $"{supabaseUrl}/rest/v1/{tableName}?select=player_id&limit=0";
            var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("apikey", supabaseAnonKey);
            request.SetRequestHeader("Authorization", $"Bearer {supabaseAnonKey}");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                Debug.Log("[GhostSync] Table exists");
            else
                Debug.LogWarning("[GhostSync] Table missing. Run CREATE TABLE from GhostSyncManager.cs comments.");
            request.Dispose();
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

            // Upload local profile — route through queue if offline
            if (!string.IsNullOrEmpty(localJson))
            {
                if (Application.internetReachability == NetworkReachability.NotReachable)
                {
                    OfflineSyncQueue.Instance?.EnqueueOperation("ghost_profile", localJson);
                    Debug.Log("[GhostSync] Offline — queued profile upload for later");
                }
                else
                {
                    yield return UploadProfile(playerId, localJson);
                }
            }

            // Download latest (in case another device updated) — skip if offline
            if (Application.internetReachability != NetworkReachability.NotReachable)
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
                if (response.Length > 10 && response.Contains("profile_data"))
                {
                    // Extract updated_at from remote response for conflict resolution
                    string remoteTimestamp = ExtractJsonField(response, "updated_at");
                    string localTimestamp = HasLocalProfile()
                        ? new FileInfo(localPath).LastWriteTimeUtc.ToString("o")
                        : null;

                    bool remoteNewer = OfflineSyncQueue.IsRemoteNewer(localTimestamp, remoteTimestamp);

                    if (remoteNewer || !HasLocalProfile())
                    {
                        // Extract profile_data and save locally
                        string remoteProfileData = ExtractJsonField(response, "profile_data");
                        if (!string.IsNullOrEmpty(remoteProfileData))
                        {
                            SaveLocal(remoteProfileData);
                            Debug.Log("[GhostSync] Remote profile is newer — updated local");
                        }
                    }
                    else
                    {
                        Debug.Log("[GhostSync] Local profile is newer — keeping local");
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
        /// Upload a profile payload on behalf of OfflineSyncQueue retry.
        /// Reports success/failure via callback.
        /// </summary>
        public IEnumerator UploadProfileQueued(string playerId, string profileJson, Action<bool> onResult)
        {
            bool success = false;
            yield return UploadProfileWithResult(playerId, profileJson, result => success = result);
            onResult(success);
        }

        IEnumerator UploadProfileWithResult(string playerId, string profileJson, Action<bool> onResult)
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

            bool ok = request.result == UnityWebRequest.Result.Success;
            if (ok) Debug.Log("[GhostSync] Upload (queued retry) success");
            else Debug.LogWarning($"[GhostSync] Upload (queued retry) failed: {request.error}");

            request.Dispose();
            onResult(ok);
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

        /// <summary>Simple field extractor for flat JSON strings (avoids full JSON parser dependency).</summary>
        string ExtractJsonField(string json, string fieldName)
        {
            string search = $"\"{fieldName}\":\"";
            int start = json.IndexOf(search, StringComparison.Ordinal);
            if (start < 0) return null;
            start += search.Length;
            int end = json.IndexOf('"', start);
            return end > start ? json.Substring(start, end - start) : null;
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
