using UnityEngine;
using UnityEngine.SceneManagement;
using Volk.Core;

namespace Volk.Story
{
    public class StoryManager : MonoBehaviour
    {
        public static StoryManager Instance { get; private set; }

        public ChapterData[] chapters;
        public ChapterData CurrentChapter { get; private set; }
        public int CurrentChapterIndex { get; private set; }
        public bool IsStoryMode { get; private set; }
        public bool ShowOutroDialogue { get; set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void StartStory()
        {
            IsStoryMode = true;
            int savedChapter = SaveManager.Instance != null ? SaveManager.Instance.Data.completedChapter : 0;
            CurrentChapterIndex = Mathf.Min(savedChapter, chapters.Length - 1);
            LoadChapter(CurrentChapterIndex);
        }

        public void LoadChapter(int index)
        {
            if (index < 0 || index >= chapters.Length) return;
            CurrentChapterIndex = index;
            CurrentChapter = chapters[index];

            // Setup enemy in GameSettings
            if (GameSettings.Instance != null)
            {
                GameSettings.Instance.enemyCharacter = CurrentChapter.enemyCharacter;
            }

            // If chapter has intro dialogue, go to dialogue scene first
            ShowOutroDialogue = false;
            if (CurrentChapter.introDialogue != null && CurrentChapter.introDialogue.Length > 0)
            {
                SceneManager.LoadScene("Dialogue");
            }
            else
            {
                StartFight();
            }
        }

        public void StartFight()
        {
            SceneManager.LoadScene(CurrentChapter.arenaSceneName);
        }

        public void OnChapterWon()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.CompleteChapter(CurrentChapterIndex + 1);
                SaveManager.Instance.AddCurrency(CurrentChapter.coinReward);
                SaveManager.Instance.AddWin();

                if (CurrentChapter.characterUnlockReward != null)
                    SaveManager.Instance.UnlockCharacter(CurrentChapter.characterUnlockReward.characterName);
            }

            // Show outro dialogue or advance
            if (CurrentChapter.outroDialogue != null && CurrentChapter.outroDialogue.Length > 0)
            {
                ShowOutroDialogue = true;
                SceneManager.LoadScene("Dialogue");
            }
            else
            {
                AdvanceToNextChapter();
            }
        }

        public void OnChapterLost()
        {
            if (SaveManager.Instance != null)
                SaveManager.Instance.AddLoss();

            // Retry or go back to story menu
            SceneManager.LoadScene("StoryMenu");
        }

        public void AdvanceToNextChapter()
        {
            if (CurrentChapterIndex + 1 < chapters.Length)
            {
                LoadChapter(CurrentChapterIndex + 1);
            }
            else
            {
                // Story complete
                Debug.Log("[Story] All chapters completed!");
                IsStoryMode = false;
                SceneManager.LoadScene("MainMenu");
            }
        }

        public void ExitStoryMode()
        {
            IsStoryMode = false;
            CurrentChapter = null;
            SceneManager.LoadScene("MainMenu");
        }
    }
}
