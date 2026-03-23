using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Volk.Meta;

namespace Volk.UI
{
    public class LeaderboardUI : MonoBehaviour
    {
        [Header("References")]
        public Transform listContainer;
        public GameObject entryPrefab;
        public TextMeshProUGUI playerRankText;
        public Button refreshButton;
        public Button backButton;
        public GameObject loadingIndicator;
        public CanvasGroup canvasGroup;

        [Header("Tabs")]
        public Button allTimeTab;
        public Button weeklyTab;

        void Awake()
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }

        void Start()
        {
            if (refreshButton != null)
                refreshButton.onClick.AddListener(Refresh);
            if (backButton != null)
                backButton.onClick.AddListener(() =>
                    UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu"));

            if (allTimeTab != null)
                allTimeTab.onClick.AddListener(Refresh);
            if (weeklyTab != null)
                weeklyTab.onClick.AddListener(Refresh);

            if (LeaderboardManager.Instance != null)
            {
                LeaderboardManager.Instance.OnLeaderboardUpdated += PopulateList;
                LeaderboardManager.Instance.OnError += ShowError;
            }

            Refresh();
        }

        void OnDestroy()
        {
            if (LeaderboardManager.Instance != null)
            {
                LeaderboardManager.Instance.OnLeaderboardUpdated -= PopulateList;
                LeaderboardManager.Instance.OnError -= ShowError;
            }
        }

        void Refresh()
        {
            if (loadingIndicator != null) loadingIndicator.SetActive(true);
            LeaderboardManager.Instance?.FetchLeaderboard(100);
        }

        void PopulateList()
        {
            if (loadingIndicator != null) loadingIndicator.SetActive(false);

            foreach (Transform child in listContainer)
                Destroy(child.gameObject);

            if (LeaderboardManager.Instance == null) return;

            var entries = LeaderboardManager.Instance.CachedEntries;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var item = Instantiate(entryPrefab, listContainer);
                var texts = item.GetComponentsInChildren<TextMeshProUGUI>();

                if (texts.Length > 0) texts[0].text = $"#{i + 1}";
                if (texts.Length > 1) texts[1].text = $"Player"; // display_name needed
                if (texts.Length > 2) texts[2].text = $"{entry.score}";
                if (texts.Length > 3) texts[3].text = $"{entry.win_count}W";
            }

            // Player rank
            if (playerRankText != null)
            {
                int rank = LeaderboardManager.Instance.PlayerRank;
                playerRankText.text = rank > 0 ? $"Siran: #{rank}" : "Siralamada yoksun";
            }
        }

        void ShowError(string error)
        {
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
            Debug.Log($"[Leaderboard] Error: {error}");

            if (playerRankText != null)
                playerRankText.text = "Baglanti hatasi";
        }
    }
}
