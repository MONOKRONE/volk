using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Volk.Core;
using Volk.Story;

namespace Volk.UI
{
    public class StoryMenuUI : MonoBehaviour
    {
        [Header("References")]
        public Transform chapterContainer;
        public GameObject chapterCardPrefab;
        public Button backButton;
        public CanvasGroup canvasGroup;

        void Awake()
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }

        void Start()
        {
            if (backButton != null)
                backButton.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));

            PopulateChapters();
        }

        void PopulateChapters()
        {
            if (StoryManager.Instance == null || chapterCardPrefab == null || chapterContainer == null) return;

            int completed = SaveManager.Instance != null ? SaveManager.Instance.Data.completedChapter : 0;

            for (int i = 0; i < StoryManager.Instance.chapters.Length; i++)
            {
                var chapter = StoryManager.Instance.chapters[i];
                var card = Instantiate(chapterCardPrefab, chapterContainer);
                int index = i;

                var nameText = card.GetComponentInChildren<TextMeshProUGUI>();
                if (nameText != null)
                    nameText.text = $"Chapter {chapter.chapterNumber}: {chapter.chapterTitle}";

                bool unlocked = i <= completed;
                var btn = card.GetComponent<Button>();
                if (btn != null)
                {
                    btn.interactable = unlocked;
                    btn.onClick.AddListener(() => StoryManager.Instance.LoadChapter(index));
                }

                var lockOverlay = card.transform.Find("LockOverlay");
                if (lockOverlay != null)
                    lockOverlay.gameObject.SetActive(!unlocked);
            }
        }
    }
}
