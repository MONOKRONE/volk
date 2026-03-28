using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace Volk.Core
{
    /// <summary>
    /// Manages 80-stage story progression across 8 chapters (10 stages each).
    /// Loads ChapterData SOs from Resources, tracks completion via SaveManager.
    /// </summary>
    public class StageManager : MonoBehaviour
    {
        public static StageManager Instance { get; private set; }

        public const int CHAPTERS = 8;
        public const int STAGES_PER_CHAPTER = 10;
        public const int TOTAL_STAGES = CHAPTERS * STAGES_PER_CHAPTER;
        public const int GHOST_STAGES = 10;

        [Header("Data")]
        public ChapterData[] chapters;

        [Header("Ghost Simulation")]
        public StageData[] ghostStages;

        // Current progression
        public int CurrentChapter { get; private set; }
        public int CurrentStage { get; private set; }
        public StageData ActiveStage { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadChapters();
            LoadProgress();
        }

        void LoadChapters()
        {
            var loaded = Resources.LoadAll<ChapterData>("Chapters");
            if (loaded != null && loaded.Length > 0)
            {
                // Sort by chapter number
                System.Array.Sort(loaded, (a, b) => a.chapterNumber.CompareTo(b.chapterNumber));
                chapters = loaded;
            }
            else
            {
                chapters = new ChapterData[0];
                Debug.LogWarning("[StageManager] No ChapterData found in Resources/Chapters");
            }

            // Load ghost stages
            ghostStages = Resources.LoadAll<StageData>("GhostStages");
        }

        void LoadProgress()
        {
            if (SaveManager.Instance != null)
            {
                CurrentChapter = SaveManager.Instance.Data.completedChapter;
                CurrentStage = SaveManager.Instance.Data.completedStage;
            }
            else
            {
                CurrentChapter = PlayerPrefs.GetInt("story_chapter", 0);
                CurrentStage = PlayerPrefs.GetInt("story_stage", 0);
            }
        }

        // --- Stage Access ---

        public StageData GetStage(int chapterIndex, int stageIndex)
        {
            if (chapterIndex < 0 || chapterIndex >= chapters.Length) return null;
            var chapter = chapters[chapterIndex];
            if (chapter.stages == null || stageIndex < 0 || stageIndex >= chapter.stages.Length) return null;
            return chapter.stages[stageIndex];
        }

        public ChapterData GetChapter(int chapterIndex)
        {
            if (chapterIndex < 0 || chapterIndex >= chapters.Length) return null;
            return chapters[chapterIndex];
        }

        public bool IsStageCompleted(int chapterIndex, int stageIndex)
        {
            int globalIndex = chapterIndex * STAGES_PER_CHAPTER + stageIndex;
            int completedGlobal = CurrentChapter * STAGES_PER_CHAPTER + CurrentStage;
            return globalIndex < completedGlobal;
        }

        public bool IsStageUnlocked(int chapterIndex, int stageIndex)
        {
            int globalIndex = chapterIndex * STAGES_PER_CHAPTER + stageIndex;
            int completedGlobal = CurrentChapter * STAGES_PER_CHAPTER + CurrentStage;
            return globalIndex <= completedGlobal;
        }

        public bool IsChapterCompleted(int chapterIndex)
        {
            return chapterIndex < CurrentChapter;
        }

        // --- Stage Flow ---

        public void StartStage(int chapterIndex, int stageIndex)
        {
            var stage = GetStage(chapterIndex, stageIndex);
            if (stage == null)
            {
                Debug.LogWarning($"[StageManager] Stage {chapterIndex}:{stageIndex} not found");
                return;
            }

            ActiveStage = stage;

            // Configure opponent
            if (GameSettings.Instance != null)
            {
                GameSettings.Instance.currentMode = GameSettings.GameMode.Story;
                GameSettings.Instance.enemyCharacter = stage.opponentCharacter;
                GameSettings.Instance.selectedDifficulty = stage.difficulty;
            }

            // Load combat scene
            string scene = chapters[chapterIndex].arenaSceneName;
            if (string.IsNullOrEmpty(scene)) scene = "CombatTest";
            SceneManager.LoadScene(scene);
        }

        public void OnStageComplete(bool won)
        {
            if (!won || ActiveStage == null) return;

            int chapterIdx = -1;
            int stageIdx = -1;

            // Find which chapter/stage this was
            for (int c = 0; c < chapters.Length; c++)
            {
                if (chapters[c].stages == null) continue;
                for (int s = 0; s < chapters[c].stages.Length; s++)
                {
                    if (chapters[c].stages[s] == ActiveStage)
                    {
                        chapterIdx = c;
                        stageIdx = s;
                        break;
                    }
                }
                if (chapterIdx >= 0) break;
            }

            if (chapterIdx < 0) return;

            // Advance progress
            int nextStage = stageIdx + 1;
            int nextChapter = chapterIdx;

            if (nextStage >= STAGES_PER_CHAPTER)
            {
                nextStage = 0;
                nextChapter = chapterIdx + 1;
                OnChapterComplete(chapterIdx);
            }

            if (nextChapter * STAGES_PER_CHAPTER + nextStage > CurrentChapter * STAGES_PER_CHAPTER + CurrentStage)
            {
                CurrentChapter = nextChapter;
                CurrentStage = nextStage;
                SaveProgress();
            }

            // Rewards
            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.AddCoins(ActiveStage.coinReward);

            // Battle pass XP
            if (BattlePassManager.Instance != null)
                BattlePassManager.Instance.AddXP(BattlePassManager.XP_STAGE_CLEAR);

            ActiveStage = null;
        }

        void OnChapterComplete(int chapterIndex)
        {
            Debug.Log($"[StageManager] Chapter {chapterIndex + 1} completed!");

            var chapter = chapters[chapterIndex];

            // Chapter reward coins
            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.AddCoins(chapter.coinReward);

            // Character unlock check
            if (CharacterUnlockManager.Instance != null)
            {
                var allChars = Resources.LoadAll<CharacterData>("Characters");
                CharacterUnlockManager.Instance.CheckStoryUnlocks(chapterIndex + 1, allChars);
            }

            // Direct character unlock reward
            if (chapter.characterUnlockReward != null && CharacterUnlockManager.Instance != null)
            {
                CharacterUnlockManager.Instance.Unlock(chapter.characterUnlockReward);
            }
        }

        void SaveProgress()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.Data.completedChapter = CurrentChapter;
                SaveManager.Instance.Data.completedStage = CurrentStage;
                SaveManager.Instance.Save();
            }
            else
            {
                PlayerPrefs.SetInt("story_chapter", CurrentChapter);
                PlayerPrefs.SetInt("story_stage", CurrentStage);
                PlayerPrefs.Save();
            }
        }

        // --- Ghost Simulation ---

        public StageData GetGhostStage(int index)
        {
            if (ghostStages == null || index < 0 || index >= ghostStages.Length) return null;
            return ghostStages[index];
        }

        public void StartGhostStage(int index)
        {
            var stage = GetGhostStage(index);
            if (stage == null)
            {
                Debug.LogWarning($"[StageManager] Ghost stage {index} not found");
                return;
            }

            ActiveStage = stage;

            if (GameSettings.Instance != null)
                GameSettings.Instance.currentMode = GameSettings.GameMode.Ghost;

            // Trigger behavior scenario
            if (PlayerBehaviorTracker.Instance != null)
            {
                switch (stage.ghostScenarioType)
                {
                    case GhostScenarioType.AggressiveClone:
                        Debug.Log("[Ghost] Scenario: Aggressive pressure");
                        break;
                    case GhostScenarioType.DefensiveClone:
                        Debug.Log("[Ghost] Scenario: Defensive counter");
                        break;
                    case GhostScenarioType.MirrorMatch:
                        Debug.Log("[Ghost] Scenario: Mirror match");
                        break;
                }
            }

            SceneManager.LoadScene("CombatTest");
        }

        // --- Utility ---

        public int GetGlobalStageIndex()
        {
            return CurrentChapter * STAGES_PER_CHAPTER + CurrentStage;
        }

        public float GetOverallProgress()
        {
            return (float)GetGlobalStageIndex() / TOTAL_STAGES;
        }

        public string GetProgressText()
        {
            return $"Chapter {CurrentChapter + 1} - Stage {CurrentStage + 1}/{STAGES_PER_CHAPTER}";
        }
    }
}
