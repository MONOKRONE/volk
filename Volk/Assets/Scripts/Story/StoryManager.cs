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
        public int CurrentStageIndex { get; private set; }
        public StageData CurrentStage => GetCurrentStage();
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

        /// <summary>
        /// Start a specific chapter by index (used by GameFlowManager).
        /// Sets up story mode state without immediately loading a scene.
        /// </summary>
        public void StartChapter(int index)
        {
            if (chapters == null || index < 0 || index >= chapters.Length) return;
            IsStoryMode = true;
            CurrentChapterIndex = index;
            CurrentChapter = chapters[index];

            if (GameSettings.Instance != null)
            {
                GameSettings.Instance.enemyCharacter = CurrentChapter.enemyCharacter;
                GameSettings.Instance.selectedArena = CurrentChapter.arenaData;
                GameSettings.Instance.currentMode = GameSettings.GameMode.Story;
            }
        }

        public void LoadChapter(int index)
        {
            if (index < 0 || index >= chapters.Length) return;
            CurrentChapterIndex = index;
            CurrentChapter = chapters[index];

            // Setup enemy and arena in GameSettings
            if (GameSettings.Instance != null)
            {
                GameSettings.Instance.enemyCharacter = CurrentChapter.enemyCharacter;
                GameSettings.Instance.selectedArena = CurrentChapter.arenaData;
                GameSettings.Instance.currentMode = GameSettings.GameMode.Story;
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

            // Save chapter grade and star rating
            if (MatchStatsTracker.Instance != null)
            {
                var stats = MatchStatsTracker.Instance.Current;
                string grade = stats.Grade;
                string existing = PlayerPrefs.GetString($"chapter_{CurrentChapterIndex}_grade", "D");
                if (GradeRank(grade) > GradeRank(existing))
                {
                    PlayerPrefs.SetString($"chapter_{CurrentChapterIndex}_grade", grade);
                    PlayerPrefs.Save();
                }

                // Star rating
                if (StarRatingSystem.Instance != null)
                {
                    int stars = StarRatingSystem.Instance.CalculateStars(true, stats.remainingHPPercent, stats.matchDuration);
                    StarRatingSystem.Instance.SaveChapterStars(CurrentChapterIndex, stars);
                }
            }

            // XP for chapter
            LevelSystem.Instance?.AddChapterXP();

            // Check story-based character unlocks
            if (CharacterUnlockManager.Instance != null && GameSettings.Instance != null)
                CharacterUnlockManager.Instance.CheckStoryUnlocks(
                    CurrentChapterIndex + 1, GameSettings.Instance.allCharacters);

            // When using GameFlowManager, match result is shown there
            // Otherwise fallback to old dialogue/scene flow
            if (GameFlowManager.Instance == null)
            {
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
        }

        public void OnChapterLost()
        {
            if (SaveManager.Instance != null)
                SaveManager.Instance.AddLoss();

            // When using GameFlowManager, match result is shown there
            if (GameFlowManager.Instance == null)
                SceneManager.LoadScene("StoryMenu");
        }

        int GradeRank(string grade)
        {
            return grade switch { "S" => 5, "A" => 4, "B" => 3, "C" => 2, "D" => 1, _ => 0 };
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
            CurrentStageIndex = 0;
            SceneManager.LoadScene("MainMenu");
        }

        StageData GetCurrentStage()
        {
            if (CurrentChapter == null || CurrentChapter.stages == null) return null;
            if (CurrentStageIndex < 0 || CurrentStageIndex >= CurrentChapter.stages.Length) return null;
            return CurrentChapter.stages[CurrentStageIndex];
        }

        public void LoadStage(int stageIndex)
        {
            if (CurrentChapter == null) return;
            if (CurrentChapter.stages == null || stageIndex >= CurrentChapter.stages.Length)
            {
                // No more stages, check boss
                if (CurrentChapter.boss != null)
                {
                    LoadBoss();
                    return;
                }
                OnChapterWon();
                return;
            }

            CurrentStageIndex = stageIndex;
            var stage = CurrentChapter.stages[stageIndex];

            if (GameSettings.Instance != null)
            {
                GameSettings.Instance.enemyCharacter = stage.opponentCharacter ?? CurrentChapter.enemyCharacter;
                GameSettings.Instance.selectedArena = CurrentChapter.arenaData;
                GameSettings.Instance.currentMode = GameSettings.GameMode.Story;
            }

            StartFight();
        }

        void LoadBoss()
        {
            var boss = CurrentChapter.boss;
            if (GameSettings.Instance != null)
            {
                GameSettings.Instance.enemyCharacter = boss.bossCharacter;
                GameSettings.Instance.selectedArena = CurrentChapter.arenaData;
                GameSettings.Instance.currentMode = GameSettings.GameMode.Story;
            }
            StartFight();
        }

        public void OnStageWon()
        {
            var stage = CurrentStage;
            if (stage != null && SaveManager.Instance != null)
                SaveManager.Instance.AddCurrency(stage.coinReward);

            // Advance to next stage
            CurrentStageIndex++;
            if (CurrentChapter.stages != null && CurrentStageIndex < CurrentChapter.stages.Length)
            {
                LoadStage(CurrentStageIndex);
            }
            else if (CurrentChapter.boss != null && CurrentStageIndex == (CurrentChapter.stages?.Length ?? 0))
            {
                LoadBoss();
            }
            else
            {
                OnChapterWon();
            }
        }

        public bool IsBossStage => CurrentChapter != null && CurrentChapter.boss != null
            && CurrentStageIndex >= (CurrentChapter.stages?.Length ?? 0);

        public int TotalStagesInChapter => (CurrentChapter?.stages?.Length ?? 0)
            + (CurrentChapter?.boss != null ? 1 : 0);
    }
}
