using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI restartHintText;

    private bool gameOver = false;

    void Awake()
    {
        Instance = this;
        if (gameOverPanel) gameOverPanel.SetActive(false);
    }

    public void OnFighterDied(bool isPlayer)
    {
        if (gameOver) return;
        gameOver = true;
        StartCoroutine(GameOverSequence(isPlayer));
    }

    IEnumerator GameOverSequence(bool playerDied)
    {
        // Wait for death animation
        yield return new WaitForSeconds(1.5f);

        if (gameOverPanel) gameOverPanel.SetActive(true);
        if (resultText) resultText.text = playerDied ? "YOU LOSE" : "YOU WIN";
        if (restartHintText) restartHintText.text = "Press R to restart";

        // Wait for R key
        while (!Input.GetKeyDown(KeyCode.R))
            yield return null;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
