using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class RoundUI : MonoBehaviour
{
    [Header("Round Intro")]
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI fightText;
    public CanvasGroup introGroup;

    [Header("HUD")]
    public TextMeshProUGUI timerText;
    public Image[] playerRoundDots;
    public Image[] enemyRoundDots;

    [Header("Round Result")]
    public TextMeshProUGUI resultText;
    public CanvasGroup resultGroup;

    [Header("Match Result")]
    public GameObject matchResultPanel;
    public TextMeshProUGUI matchResultText;
    public TextMeshProUGUI restartText;

    [Header("Colors")]
    public Color dotActiveColor = Color.white;
    public Color dotInactiveColor = new Color(1, 1, 1, 0.2f);

    public void ShowRoundIntro(int round)
    {
        if (resultGroup != null) { resultGroup.alpha = 0; resultGroup.gameObject.SetActive(false); }
        if (matchResultPanel != null) matchResultPanel.SetActive(false);

        introGroup.gameObject.SetActive(true);
        roundText.gameObject.SetActive(true);
        roundText.text = $"ROUND {round}";
        fightText.gameObject.SetActive(false);
        StartCoroutine(FadeIn(introGroup, 0.3f));
    }

    public void ShowFight()
    {
        roundText.gameObject.SetActive(false);
        fightText.gameObject.SetActive(true);
        fightText.text = "FIGHT!";
        StartCoroutine(PunchScale(fightText.transform));
    }

    public void HideIntro()
    {
        StartCoroutine(FadeOut(introGroup, 0.3f));
    }

    public void UpdateTimer(float time)
    {
        if (timerText == null) return;
        int t = Mathf.CeilToInt(Mathf.Max(0, time));
        timerText.text = t.ToString();
        timerText.color = time <= 10f ? Color.red : Color.white;
    }

    public void ShowRoundResult(bool playerWon, bool isTimeout)
    {
        resultGroup.gameObject.SetActive(true);
        resultText.text = isTimeout ? "TIME" : "K.O.";
        resultText.color = playerWon ? Color.yellow : Color.red;
        StartCoroutine(PunchScale(resultText.transform));
        StartCoroutine(FadeIn(resultGroup, 0.1f));
    }

    public void UpdateRoundWins(int playerWins, int enemyWins)
    {
        if (playerRoundDots != null)
            for (int i = 0; i < playerRoundDots.Length; i++)
                playerRoundDots[i].color = i < playerWins ? dotActiveColor : dotInactiveColor;
        if (enemyRoundDots != null)
            for (int i = 0; i < enemyRoundDots.Length; i++)
                enemyRoundDots[i].color = i < enemyWins ? dotActiveColor : dotInactiveColor;
    }

    public void ShowMatchResult(bool playerWon)
    {
        if (resultGroup != null) { resultGroup.alpha = 0; resultGroup.gameObject.SetActive(false); }
        matchResultPanel.SetActive(true);
        matchResultText.text = playerWon ? "YOU WIN" : "YOU LOSE";
        matchResultText.color = playerWon ? Color.yellow : Color.red;
        restartText.text = "TAP TO RESTART";
        StartCoroutine(PunchScale(matchResultText.transform));
    }

    IEnumerator FadeIn(CanvasGroup cg, float duration)
    {
        cg.alpha = 0;
        float t = 0;
        while (t < duration) { t += Time.unscaledDeltaTime; cg.alpha = t / duration; yield return null; }
        cg.alpha = 1;
    }

    IEnumerator FadeOut(CanvasGroup cg, float duration)
    {
        float t = 0;
        while (t < duration) { t += Time.unscaledDeltaTime; cg.alpha = 1 - (t / duration); yield return null; }
        cg.alpha = 0;
        cg.gameObject.SetActive(false);
    }

    IEnumerator PunchScale(Transform tr)
    {
        tr.localScale = Vector3.one * 1.4f;
        float elapsed = 0;
        while (elapsed < 0.2f)
        {
            elapsed += Time.unscaledDeltaTime;
            float s = Mathf.Lerp(1.4f, 1f, elapsed / 0.2f);
            tr.localScale = Vector3.one * s;
            yield return null;
        }
        tr.localScale = Vector3.one;
    }
}
