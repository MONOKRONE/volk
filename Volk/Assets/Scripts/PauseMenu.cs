using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance;

    [Header("UI - set by PauseMenuBuilder")]
    public GameObject overlayObj;
    public Button resumeButton;
    public Button restartButton;
    public Button soundToggleButton;
    public Button vibrationToggleButton;
    public Button easyButton;
    public Button normalButton;
    public Button hardButton;
    public TextMeshProUGUI soundLabel;
    public TextMeshProUGUI vibrationLabel;
    public TextMeshProUGUI soundBadge;
    public TextMeshProUGUI vibrationBadge;

    [Header("Difficulty Colors")]
    public Color activeColor = new Color(0.9f, 0.72f, 0f);
    public Color inactiveColor = new Color(0.16f, 0.16f, 0.16f);

    private bool isPaused;
    private bool soundEnabled = true;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        soundEnabled = PlayerPrefs.GetInt("SoundEnabled", 1) == 1;
    }

    void Start()
    {
        if (resumeButton) resumeButton.onClick.AddListener(Resume);
        if (restartButton) restartButton.onClick.AddListener(Restart);
        if (soundToggleButton) soundToggleButton.onClick.AddListener(ToggleSound);
        if (vibrationToggleButton) vibrationToggleButton.onClick.AddListener(ToggleVibration);
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
        if (overlayObj) overlayObj.SetActive(true);
        UpdateToggleUI();
    }

    public void Resume()
    {
        if (overlayObj) overlayObj.SetActive(false);
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
        if (soundBadge) soundBadge.text = soundEnabled ? "ON" : "OFF";
        if (vibrationBadge) vibrationBadge.text = vib ? "ON" : "OFF";
        if (soundBadge) soundBadge.color = soundEnabled ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.6f, 0.3f, 0.3f);
        if (vibrationBadge) vibrationBadge.color = vib ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.6f, 0.3f, 0.3f);
    }
}
