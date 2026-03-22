using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance;

    [Header("Panel")]
    public CanvasGroup pausePanel;
    public GameObject pauseContainer;

    [Header("Buttons")]
    public Button resumeButton;
    public Button restartButton;
    public Button soundToggleButton;
    public Button vibrationToggleButton;

    [Header("Difficulty")]
    public Button easyButton;
    public Button normalButton;
    public Button hardButton;
    public Color activeColor = new Color(1f, 0.8f, 0f, 1f);
    public Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    [Header("Labels")]
    public TextMeshProUGUI soundLabel;
    public TextMeshProUGUI vibrationLabel;
    public TextMeshProUGUI soundBadge;
    public TextMeshProUGUI vibrationBadge;

    private bool isPaused = false;
    private bool soundEnabled = true;

    void Awake()
    {
        Instance = this;
        soundEnabled = PlayerPrefs.GetInt("SoundEnabled", 1) == 1;
    }

    void Start()
    {
        pauseContainer.SetActive(false);
        resumeButton.onClick.AddListener(Resume);
        restartButton.onClick.AddListener(Restart);
        soundToggleButton.onClick.AddListener(ToggleSound);
        vibrationToggleButton.onClick.AddListener(ToggleVibration);
        if (easyButton) easyButton.onClick.AddListener(() => SetDifficulty(0));
        if (normalButton) normalButton.onClick.AddListener(() => SetDifficulty(1));
        if (hardButton) hardButton.onClick.AddListener(() => SetDifficulty(2));
        ApplySoundSetting();

        int saved = PlayerPrefs.GetInt("AIDifficulty", 1);
        if (GameManager.Instance != null)
            GameManager.Instance.selectedDifficulty = (AIDifficulty)saved;
        UpdateDifficultyUI((AIDifficulty)saved);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    public void TogglePause()
    {
        if (isPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        pauseContainer.SetActive(true);
        StartCoroutine(FadeIn(pausePanel, 0.2f));
        UpdateToggleUI();
    }

    public void Resume()
    {
        StartCoroutine(ResumeCoroutine());
    }

    IEnumerator ResumeCoroutine()
    {
        yield return StartCoroutine(FadeOut(pausePanel, 0.15f));
        pauseContainer.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void SetDifficulty(int level)
    {
        AIDifficulty diff = (AIDifficulty)level;
        if (GameManager.Instance != null)
            GameManager.Instance.selectedDifficulty = diff;
        PlayerPrefs.SetInt("AIDifficulty", level);
        UpdateDifficultyUI(diff);
    }

    void UpdateDifficultyUI(AIDifficulty diff)
    {
        if (easyButton) easyButton.GetComponent<Image>().color = diff == AIDifficulty.Easy ? activeColor : inactiveColor;
        if (normalButton) normalButton.GetComponent<Image>().color = diff == AIDifficulty.Normal ? activeColor : inactiveColor;
        if (hardButton) hardButton.GetComponent<Image>().color = diff == AIDifficulty.Hard ? activeColor : inactiveColor;
    }

    public void ToggleSound()
    {
        soundEnabled = !soundEnabled;
        PlayerPrefs.SetInt("SoundEnabled", soundEnabled ? 1 : 0);
        ApplySoundSetting();
        UpdateToggleUI();
    }

    public void ToggleVibration()
    {
        bool current = VibrationManager.Instance != null && VibrationManager.Instance.IsEnabled;
        if (VibrationManager.Instance != null)
            VibrationManager.Instance.SetVibration(!current);
        UpdateToggleUI();
    }

    void ApplySoundSetting()
    {
        AudioListener.volume = soundEnabled ? 1f : 0f;
    }

    void UpdateToggleUI()
    {
        bool vib = VibrationManager.Instance != null && VibrationManager.Instance.IsEnabled;
        if (soundLabel) soundLabel.text = soundEnabled ? "SES AÇIK" : "SES KAPALI";
        if (vibrationLabel) vibrationLabel.text = vib ? "TİTREŞİM AÇIK" : "TİTREŞİM KAPALI";
        if (soundBadge) soundBadge.text = soundEnabled ? "AÇIK" : "KAPALI";
        if (vibrationBadge) vibrationBadge.text = vib ? "AÇIK" : "KAPALI";
    }

    IEnumerator FadeIn(CanvasGroup cg, float duration)
    {
        cg.alpha = 0; float t = 0;
        while (t < duration) { t += Time.unscaledDeltaTime; cg.alpha = t / duration; yield return null; }
        cg.alpha = 1;
    }

    IEnumerator FadeOut(CanvasGroup cg, float duration)
    {
        float t = 0;
        while (t < duration) { t += Time.unscaledDeltaTime; cg.alpha = 1 - (t / duration); yield return null; }
        cg.alpha = 0;
    }
}
