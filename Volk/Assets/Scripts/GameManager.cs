using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

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

    void Awake() { Instance = this; }

    void Start() { StartCoroutine(StartRound()); }

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
            StartCoroutine(EndRound(null));
    }

    public void OnFighterDied(bool isPlayer)
    {
        if (!roundActive) return;
        roundActive = false;
        StartCoroutine(EndRound(isPlayer ? enemyFighter : playerFighter));
    }

    IEnumerator EndRound(Fighter winner)
    {
        roundActive = false;
        CurrentState = RoundState.RoundEnd;

        bool playerWon;
        if (winner == null)
            playerWon = playerFighter.currentHP > enemyFighter.currentHP;
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
        }
        else
        {
            currentRound++;
            StartCoroutine(StartRound());
        }
    }

    void RestartMatch()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    bool IsTouchTap()
    {
        foreach (Touch t in Input.touches)
            if (t.phase == TouchPhase.Began) return true;
        return false;
    }
}
