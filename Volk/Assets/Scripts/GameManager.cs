using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Volk.Core;
using Volk.Story;
using Volk.Meta;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Round Settings")]
    public int totalRounds = 3;
    public float roundDuration = 99f;
    public float roundStartDelay = 1.5f;
    public float roundEndDelay = 2.0f;

    [Header("Difficulty")]
    public AIDifficulty selectedDifficulty = AIDifficulty.Normal;

    [Header("References")]
    public Fighter playerFighter;
    public Fighter enemyFighter;
    public RoundUI roundUI;

    private int currentRound = 1;
    private int playerRoundWins = 0;
    private int enemyRoundWins = 0;
    private float roundTimer;
    private bool roundActive = false;
    private bool matchOver = false;

    public enum RoundState { Intro, Fighting, RoundEnd, MatchEnd }
    public RoundState CurrentState { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        ApplySelectedCharacter();

        // Wire ComboTracker to player fighter
        if (Volk.Core.ComboTracker.Instance != null && playerFighter != null)
            Volk.Core.ComboTracker.Instance.SetTrackedFighter(playerFighter);

        // Ghost mode: attach GhostFSM to enemy if in Ghost mode
        if (GameSettings.Instance?.currentMode == GameSettings.GameMode.Ghost && enemyFighter != null)
        {
            var ghostFSM = enemyFighter.GetComponent<Volk.Core.GhostFSM>();
            if (ghostFSM == null)
                ghostFSM = enemyFighter.gameObject.AddComponent<Volk.Core.GhostFSM>();

            string matchup = $"{playerFighter?.characterData?.characterName ?? "unknown"}_vs_{enemyFighter?.characterData?.characterName ?? "unknown"}";
            ghostFSM.LoadProfile(matchup);
        }

        StartCoroutine(StartRound());
    }

    void ApplySelectedCharacter()
    {
        if (GameSettings.Instance == null) return;

        if (GameSettings.Instance.selectedCharacter != null && playerFighter != null)
            playerFighter.ApplyCharacterData(GameSettings.Instance.selectedCharacter);

        if (GameSettings.Instance.enemyCharacter != null && enemyFighter != null)
            enemyFighter.ApplyCharacterData(GameSettings.Instance.enemyCharacter);

        // Apply player equipment bonuses
        if (EquipmentManager.Instance != null && playerFighter != null)
            EquipmentManager.Instance.ApplyBonuses(playerFighter);

        // Apply difficulty from GameSettings (QuickFight mode)
        if (GameSettings.Instance.currentMode == GameSettings.GameMode.QuickFight)
            selectedDifficulty = GameSettings.Instance.selectedDifficulty;

        // Apply story mode difficulty and HP multiplier
        if (StoryManager.Instance != null && StoryManager.Instance.IsStoryMode && StoryManager.Instance.CurrentChapter != null)
        {
            var chapter = StoryManager.Instance.CurrentChapter;
            selectedDifficulty = chapter.difficulty;
            if (enemyFighter != null)
            {
                enemyFighter.maxHP *= chapter.enemyHPMultiplier;
                enemyFighter.currentHP = enemyFighter.maxHP;
            }
        }
    }

    IEnumerator StartRound()
    {
        roundActive = false;
        CurrentState = RoundState.Intro;
        roundTimer = roundDuration;

        // Reset fighters
        if (playerFighter != null) playerFighter.ResetForRound();
        if (enemyFighter != null)
        {
            enemyFighter.ResetForRound();
            enemyFighter.difficulty = selectedDifficulty;
            enemyFighter.InitAIDifficulty();
        }

        // Show "ROUND X"
        if (roundUI != null)
        {
            roundUI.ShowRoundIntro(currentRound);
            yield return new WaitForSeconds(roundStartDelay);

            roundUI.ShowFight();
            AudioManager.Instance?.PlayRoundStart();
            yield return new WaitForSeconds(0.8f);

            roundUI.HideIntro();
        }
        else
        {
            yield return new WaitForSeconds(roundStartDelay + 0.8f);
        }

        roundActive = true;
        CurrentState = RoundState.Fighting;
    }

    void Update()
    {
        if (matchOver)
        {
            if (Input.GetKeyDown(KeyCode.R) || IsTouchTap())
                RestartMatch();
            return;
        }

        if (!roundActive) return;

        roundTimer -= Time.deltaTime;
        if (roundUI != null) roundUI.UpdateTimer(roundTimer);

        if (roundTimer <= 0f)
        {
            roundActive = false;
            StartCoroutine(EndRound(null));
        }
    }

    public void OnFighterDied(bool isPlayer)
    {
        if (!roundActive) return;
        roundActive = false;

        // PLA-130: Build ghost profile on match end
        if (playerFighter != null && enemyFighter != null)
        {
            string matchup = $"{playerFighter.characterData?.characterName ?? "unknown"}_vs_{enemyFighter.characterData?.characterName ?? "unknown"}";
            Volk.Core.GhostProfileBuilder.Instance?.BuildProfileAsync(matchup, _ => Volk.Core.GhostSyncManager.Instance?.TrySync());
        }

        StartCoroutine(EndRound(isPlayer ? enemyFighter : playerFighter));
    }

    IEnumerator EndRound(Fighter winner)
    {
        roundActive = false;
        CurrentState = RoundState.RoundEnd;

        bool playerWon;
        if (winner == null)
            playerWon = (playerFighter != null ? playerFighter.currentHP : 0) > (enemyFighter != null ? enemyFighter.currentHP : 0);
        else
            playerWon = winner == playerFighter;

        if (playerWon) playerRoundWins++;
        else enemyRoundWins++;

        bool matchDone = playerRoundWins >= 2 || enemyRoundWins >= 2 || currentRound >= totalRounds;

        if (roundUI != null)
        {
            roundUI.ShowRoundResult(playerWon, winner == null);
            roundUI.UpdateRoundWins(playerRoundWins, enemyRoundWins);
        }

        yield return new WaitForSeconds(roundEndDelay);

        if (matchDone)
        {
            matchOver = true;
            CurrentState = RoundState.MatchEnd;
            if (roundUI != null)
                roundUI.ShowMatchResult(playerRoundWins > enemyRoundWins);

            // Finalize match stats
            bool playerWonMatch = playerRoundWins > enemyRoundWins;
            if (MatchStatsTracker.Instance != null)
            {
                float hpPercent = playerFighter.maxHP > 0 ? playerFighter.currentHP / playerFighter.maxHP : 0;
                MatchStatsTracker.Instance.FinalizeMatch(playerWonMatch, hpPercent);
            }
            if (StoryManager.Instance != null && StoryManager.Instance.IsStoryMode)
            {
                if (playerWonMatch)
                    StoryManager.Instance.OnChapterWon();
                else
                    StoryManager.Instance.OnChapterLost();
            }
            else if (SaveManager.Instance != null)
            {
                if (playerWonMatch)
                    SaveManager.Instance.AddWin();
                else
                    SaveManager.Instance.AddLoss();
            }

            // Daily quest progress
            if (DailyQuestManager.Instance != null)
            {
                DailyQuestManager.Instance.ReportProgress(QuestCondition.PlayMatches);
                if (playerWonMatch)
                {
                    DailyQuestManager.Instance.ReportProgress(QuestCondition.WinMatches);
                    if (selectedDifficulty == AIDifficulty.Hard)
                        DailyQuestManager.Instance.ReportProgress(QuestCondition.WinOnHard);
                    if (playerFighter.currentHP >= playerFighter.maxHP)
                        DailyQuestManager.Instance.ReportProgress(QuestCondition.WinWithoutDamage);
                }
            }

            // XP reward
            LevelSystem.Instance?.AddMatchXP(playerWonMatch);

            // Currency reward
            CurrencyManager.Instance?.AddCoins(playerWonMatch ? 100 : 25);

            // Battle Pass XP
            if (BattlePassManager.Instance != null)
            {
                int xp = playerWonMatch ? BattlePassManager.XP_PVP_WIN : BattlePassManager.XP_PVP_LOSS;
                BattlePassManager.Instance.AddXP(xp);
            }

            // Ghost behavior tracking + cloud sync
            if (PlayerBehaviorTracker.Instance != null)
            {
                PlayerBehaviorTracker.Instance.OnMatchEnd();
                PlayerBehaviorTracker.Instance.SaveProfile();
            }
            GhostSyncManager.Instance?.TrySync();

            // Ranked ELO update
            if (GameSettings.Instance?.currentMode == GameSettings.GameMode.Online)
            {
                string opponent = enemyFighter?.characterData?.characterName ?? "???";
                int eloDelta = playerWonMatch ? Random.Range(18, 32) : -Random.Range(14, 26);
                Volk.UI.RankedUI.RecordMatch(opponent, playerWonMatch, eloDelta);
            }

            // DLC grind tracking
            if (CharacterDLCManager.Instance != null && playerFighter?.characterData != null)
                CharacterDLCManager.Instance.RecordMatchPlayed(playerFighter.characterData.characterName, roundDuration * currentRound / 60f);

            // Mastery progress
            if (Volk.Core.CharacterMasteryManager.Instance != null && playerFighter?.characterData != null)
            {
                string charId = playerFighter.characterData.characterName;
                if (playerWonMatch)
                    Volk.Core.CharacterMasteryManager.Instance.AddProgressByRequirement(charId, "wins");
            }

            // Character unlock popup for story mode unlocks
            if (Volk.UI.CharacterUnlockPopup.Instance != null && StoryManager.Instance != null
                && StoryManager.Instance.IsStoryMode && StoryManager.Instance.CurrentChapter != null)
            {
                var reward = StoryManager.Instance.CurrentChapter.characterUnlockReward;
                if (reward != null && playerWonMatch)
                    Volk.UI.CharacterUnlockPopup.Instance.Show(reward);
            }

            // Submit leaderboard score
            LeaderboardManager.Instance?.SubmitScore();

            // Auto-return to MainMenu via GameFlowManager after delay
            if (Volk.Core.GameFlowManager.Instance != null)
            {
                StartCoroutine(ReturnToMainMenuDelayed(playerWonMatch, 2.5f));
            }
        }
        else
        {
            currentRound++;
            StartCoroutine(StartRound());
        }
    }

    void RestartMatch()
    {
        // If GameFlowManager exists, go back to MainMenu with match result
        if (Volk.Core.GameFlowManager.Instance != null)
        {
            bool playerWonMatch = playerRoundWins > enemyRoundWins;
            Volk.Core.GameFlowManager.Instance.returnFromCombat = true;
            Volk.Core.GameFlowManager.Instance.lastMatchWon = playerWonMatch;
            SceneManager.LoadScene("MainMenu");
            return;
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    IEnumerator ReturnToMainMenuDelayed(bool playerWon, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (Volk.Core.GameFlowManager.Instance != null)
        {
            Volk.Core.GameFlowManager.Instance.returnFromCombat = true;
            Volk.Core.GameFlowManager.Instance.lastMatchWon = playerWon;
        }
        SceneManager.LoadScene("MainMenu");
    }

    bool IsTouchTap()
    {
        foreach (Touch t in Input.touches)
            if (t.phase == TouchPhase.Began) return true;
        return false;
    }
}
