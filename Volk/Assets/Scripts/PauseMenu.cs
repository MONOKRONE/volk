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

    [Header("Labels")]
    public TextMeshProUGUI soundLabel;
    public TextMeshProUGUI vibrationLabel;

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
        ApplySoundSetting();
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
    }

    IEnumerator FadeIn(CanvasGroup cg, float duration)
    {
        cg.alpha = 0;
        float t = 0;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = t / duration;
            yield return null;
        }
        cg.alpha = 1;
    }

    IEnumerator FadeOut(CanvasGroup cg, float duration)
    {
        float t = 0;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = 1 - (t / duration);
            yield return null;
        }
        cg.alpha = 0;
    }
}
