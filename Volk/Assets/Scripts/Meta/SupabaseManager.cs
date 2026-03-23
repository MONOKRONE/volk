using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;
using Volk.Core;

namespace Volk.Meta
{
    public class SupabaseManager : MonoBehaviour
    {
        public static SupabaseManager Instance { get; private set; }

        [Header("Supabase Config")]
        public string supabaseUrl = "https://ktiyviyypeuutvtfkjnu.supabase.co";
        public string supabaseAnonKey = "sb_publishable_sNXopE96fnyPfK7iQqeT9w_Yask869v";

        private string accessToken;
        private string playerId;
        private string deviceId;

        public string PlayerId => playerId;

        public bool IsAuthenticated => !string.IsNullOrEmpty(accessToken);
        public event Action OnAuthSuccess;
        public event Action<string> OnAuthFailed;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            deviceId = SystemInfo.deviceUniqueIdentifier;
        }

        void Start()
        {
            StartCoroutine(AnonymousLogin());
        }

        // === AUTH ===

        IEnumerator AnonymousLogin()
        {
            string url = $"{supabaseUrl}/auth/v1/signup";
            var body = new AuthBody { email = $"{deviceId}@device.volk", password = deviceId };
            string json = JsonUtility.ToJson(body);

            using var request = CreateRequest(url, "POST", json);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
                accessToken = response.access_token;
                playerId = response.user?.id;
                OnAuthSuccess?.Invoke();
                Debug.Log($"[Supabase] Auth success. Player: {playerId}");

                // Ensure player record exists
                StartCoroutine(UpsertPlayer());
            }
            else
            {
                // Try login if signup fails (already registered)
                StartCoroutine(LoginWithPassword());
            }
        }

        IEnumerator LoginWithPassword()
        {
            string url = $"{supabaseUrl}/auth/v1/token?grant_type=password";
            var body = new AuthBody { email = $"{deviceId}@device.volk", password = deviceId };
            string json = JsonUtility.ToJson(body);

            using var request = CreateRequest(url, "POST", json);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
                accessToken = response.access_token;
                playerId = response.user?.id;
                OnAuthSuccess?.Invoke();
                Debug.Log($"[Supabase] Login success. Player: {playerId}");
            }
            else
            {
                string error = request.downloadHandler?.text ?? request.error;
                OnAuthFailed?.Invoke(error);
                Debug.LogWarning($"[Supabase] Auth failed: {error}");
            }
        }

        IEnumerator UpsertPlayer()
        {
            string url = $"{supabaseUrl}/rest/v1/players";
            string json = $"{{\"id\":\"{playerId}\",\"device_id\":\"{deviceId}\",\"display_name\":\"Player\"}}";

            using var request = CreateRequest(url, "POST", json);
            request.SetRequestHeader("Prefer", "resolution=merge-duplicates");
            yield return request.SendWebRequest();
        }

        // === CLOUD SAVE ===

        public void SaveToCloud(SaveData data, Action onSuccess = null, Action<string> onError = null)
        {
            if (!IsAuthenticated) { onError?.Invoke("Not authenticated"); return; }
            StartCoroutine(DoSaveToCloud(data, onSuccess, onError));
        }

        IEnumerator DoSaveToCloud(SaveData data, Action onSuccess, Action<string> onError)
        {
            string url = $"{supabaseUrl}/rest/v1/save_data";
            string dataJson = JsonUtility.ToJson(data);
            string json = $"{{\"player_id\":\"{playerId}\",\"data\":{dataJson},\"updated_at\":\"{DateTime.UtcNow:o}\"}}";

            using var request = CreateRequest(url, "POST", json);
            request.SetRequestHeader("Prefer", "resolution=merge-duplicates");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke();
                Debug.Log("[Supabase] Cloud save success");
            }
            else
            {
                onError?.Invoke(request.error);
                Debug.LogWarning($"[Supabase] Cloud save failed: {request.error}");
            }
        }

        public void LoadFromCloud(Action<SaveData> onSuccess, Action<string> onError = null)
        {
            if (!IsAuthenticated) { onError?.Invoke("Not authenticated"); return; }
            StartCoroutine(DoLoadFromCloud(onSuccess, onError));
        }

        IEnumerator DoLoadFromCloud(Action<SaveData> onSuccess, Action<string> onError)
        {
            string url = $"{supabaseUrl}/rest/v1/save_data?player_id=eq.{playerId}&select=data&limit=1";

            using var request = CreateRequest(url, "GET");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                // Response is an array, parse first element
                if (responseText.Length > 2) // not empty array "[]"
                {
                    // Extract data field from response
                    int dataStart = responseText.IndexOf("\"data\":", StringComparison.Ordinal);
                    if (dataStart >= 0)
                    {
                        dataStart += 7; // skip "data":
                        int braceCount = 0;
                        int dataEnd = dataStart;
                        for (int i = dataStart; i < responseText.Length; i++)
                        {
                            if (responseText[i] == '{') braceCount++;
                            else if (responseText[i] == '}') { braceCount--; if (braceCount == 0) { dataEnd = i + 1; break; } }
                        }
                        string dataJson = responseText.Substring(dataStart, dataEnd - dataStart);
                        var data = JsonUtility.FromJson<SaveData>(dataJson);
                        onSuccess?.Invoke(data);
                        Debug.Log("[Supabase] Cloud load success");
                        yield break;
                    }
                }
                onSuccess?.Invoke(null);
            }
            else
            {
                onError?.Invoke(request.error);
                Debug.LogWarning($"[Supabase] Cloud load failed: {request.error}");
            }
        }

        // === LEADERBOARD ===

        public void SubmitScore(int score, int winCount, int streak, Action onSuccess = null)
        {
            if (!IsAuthenticated) return;
            StartCoroutine(DoSubmitScore(score, winCount, streak, onSuccess));
        }

        IEnumerator DoSubmitScore(int score, int winCount, int streak, Action onSuccess)
        {
            string url = $"{supabaseUrl}/rest/v1/leaderboard";
            string json = $"{{\"player_id\":\"{playerId}\",\"score\":{score},\"win_count\":{winCount},\"streak\":{streak},\"updated_at\":\"{DateTime.UtcNow:o}\"}}";

            using var request = CreateRequest(url, "POST", json);
            request.SetRequestHeader("Prefer", "resolution=merge-duplicates");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke();
                Debug.Log("[Supabase] Score submitted");
            }
        }

        public void GetLeaderboard(int limit, Action<string> onSuccess, Action<string> onError = null)
        {
            if (!IsAuthenticated) { onError?.Invoke("Not authenticated"); return; }
            StartCoroutine(DoGetLeaderboard(limit, onSuccess, onError));
        }

        IEnumerator DoGetLeaderboard(int limit, Action<string> onSuccess, Action<string> onError)
        {
            string url = $"{supabaseUrl}/rest/v1/leaderboard?order=score.desc&limit={limit}&select=player_id,score,win_count,streak";

            using var request = CreateRequest(url, "GET");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(request.downloadHandler.text);
            }
            else
            {
                onError?.Invoke(request.error);
            }
        }

        // === HELPERS ===

        UnityWebRequest CreateRequest(string url, string method, string body = null)
        {
            var request = new UnityWebRequest(url, method);
            request.downloadHandler = new DownloadHandlerBuffer();

            if (body != null)
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(body);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            }

            request.SetRequestHeader("apikey", supabaseAnonKey);
            request.SetRequestHeader("Content-Type", "application/json");

            if (!string.IsNullOrEmpty(accessToken))
                request.SetRequestHeader("Authorization", $"Bearer {accessToken}");

            return request;
        }

        // === JSON MODELS ===

        [Serializable] class AuthBody { public string email; public string password; }
        [Serializable] class AuthResponse { public string access_token; public AuthUser user; }
        [Serializable] class AuthUser { public string id; }
    }
}
