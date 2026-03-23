using UnityEngine;
using System;
using System.Collections.Generic;
using Volk.Core;

namespace Volk.Meta
{
    [Serializable]
    public class LeaderboardEntry
    {
        public string player_id;
        public int score;
        public int win_count;
        public int streak;
    }

    [Serializable]
    public class LeaderboardResponse
    {
        public LeaderboardEntry[] entries;
    }

    public class LeaderboardManager : MonoBehaviour
    {
        public static LeaderboardManager Instance { get; private set; }

        public List<LeaderboardEntry> CachedEntries { get; private set; } = new List<LeaderboardEntry>();
        public int PlayerRank { get; private set; } = -1;
        public bool IsLoading { get; private set; }

        public event Action OnLeaderboardUpdated;
        public event Action<string> OnError;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SubmitScore()
        {
            if (SupabaseManager.Instance == null || !SupabaseManager.Instance.IsAuthenticated) return;
            if (SaveManager.Instance == null) return;

            var data = SaveManager.Instance.Data;
            int score = data.totalWins * 100 + data.completedChapter * 500;
            SupabaseManager.Instance.SubmitScore(score, data.totalWins, 0, () =>
            {
                Debug.Log($"[Leaderboard] Score submitted: {score}");
            });
        }

        public void FetchLeaderboard(int limit = 100)
        {
            if (SupabaseManager.Instance == null || !SupabaseManager.Instance.IsAuthenticated)
            {
                OnError?.Invoke("Baglanti yok");
                return;
            }

            IsLoading = true;
            SupabaseManager.Instance.GetLeaderboard(limit,
                json => ParseLeaderboard(json),
                error => { IsLoading = false; OnError?.Invoke(error); }
            );
        }

        void ParseLeaderboard(string json)
        {
            IsLoading = false;
            CachedEntries.Clear();

            // Parse JSON array manually (Unity JsonUtility doesn't handle arrays at root)
            string wrapped = $"{{\"entries\":{json}}}";
            try
            {
                var response = JsonUtility.FromJson<LeaderboardResponse>(wrapped);
                if (response?.entries != null)
                {
                    CachedEntries.AddRange(response.entries);

                    // Find player rank
                    PlayerRank = -1;
                    if (SupabaseManager.Instance != null)
                    {
                        for (int i = 0; i < CachedEntries.Count; i++)
                        {
                            // Can't compare player_id without knowing it from SupabaseManager
                            // so rank is position in list + 1
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Leaderboard] Parse error: {e.Message}");
            }

            OnLeaderboardUpdated?.Invoke();
        }
    }
}
