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
    private bool waitingForRestart = false;

    void Awake()
    {
        Instance = this;
        if (gameOverPanel) gameOverPanel.SetActive(false);
    }

    void Update()
    {
        if (!waitingForRestart) return;

        // Restart on any tap, click, or R key
        if (Input.GetKeyDown(KeyCode.R) || Input.GetMouseButtonDown(0) || Input.touchCount > 0)
        {
            if (Input.touchCount > 0 && Input.GetTouch(0).phase != TouchPhase.Began) return;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void OnFighterDied(bool isPlayer)
    {
        if (gameOver) return;
        gameOver = true;
        StartCoroutine(GameOverSequence(isPlayer));
    }

    IEnumerator GameOverSequence(bool playerDied)
    {
        yield return new WaitForSeconds(1.5f);

        if (gameOverPanel) gameOverPanel.SetActive(true);
        if (resultText) resultText.text = playerDied ? "YOU LOSE" : "YOU WIN";
        if (restartHintText) restartHintText.text = "Tap to restart";

        waitingForRestart = true;
    }
}
