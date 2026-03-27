using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Audio;
using Volk.Core;

namespace Volk.UI
{
    public class SettingsUI : MonoBehaviour
    {
        [Header("Navigation")]
        public Button backButton;
        public VTopBar topBar;

        [Header("Audio")]
        public Slider musicVolumeSlider;
        public Slider sfxVolumeSlider;
        public TextMeshProUGUI musicValueText;
        public TextMeshProUGUI sfxValueText;
        public AudioMixer masterMixer;

        [Header("Graphics")]
        public Button qualityLowButton;
        public Button qualityMedButton;
        public Button qualityHighButton;
        public Toggle vsyncToggle;
        public Toggle screenShakeToggle;
        public Toggle vibrationToggle;

        [Header("Controls")]
        public Slider sensitivitySlider;
        public TextMeshProUGUI sensitivityValueText;
        public Slider joystickSizeSlider;
        public TextMeshProUGUI joystickSizeValueText;
        public TMP_Dropdown controlLayoutDropdown;

        [Header("Account")]
        public TMP_InputField playerNameInput;
        public TextMeshProUGUI playerIdText;
        public Button saveNameButton;
        public TextMeshProUGUI totalWinsText;
        public TextMeshProUGUI totalLossesText;
        public TextMeshProUGUI totalPlayTimeText;
        public TextMeshProUGUI rankedEloText;
        public VButton resetProgressButton;

        [Header("Tab Buttons")]
        public Button audioTab;
        public Button graphicsTab;
        public Button controlsTab;
        public Button accountTab;

        [Header("Tab Panels")]
        public GameObject audioPanel;
        public GameObject graphicsPanel;
        public GameObject controlsPanel;
        public GameObject accountPanel;

        [Header("Confirm Popup")]
        public GameObject confirmPopup;
        public TextMeshProUGUI confirmText;
        public Button confirmYes;
        public Button confirmNo;

        private int activeTab;
        private int selectedQuality;

        void Start()
        {
            if (backButton)
                backButton.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));

            // Tab buttons
            if (audioTab) audioTab.onClick.AddListener(() => SwitchTab(0));
            if (graphicsTab) graphicsTab.onClick.AddListener(() => SwitchTab(1));
            if (controlsTab) controlsTab.onClick.AddListener(() => SwitchTab(2));
            if (accountTab) accountTab.onClick.AddListener(() => SwitchTab(3));

            // Confirm popup
            if (confirmPopup) confirmPopup.SetActive(false);
            if (confirmNo) confirmNo.onClick.AddListener(() => confirmPopup?.SetActive(false));
            if (confirmYes) confirmYes.onClick.AddListener(OnConfirmReset);

            if (resetProgressButton)
                resetProgressButton.onClick.AddListener(ShowResetConfirm);

            if (saveNameButton)
                saveNameButton.onClick.AddListener(SavePlayerName);

            LoadSettings();
            SwitchTab(0);
        }

        void LoadSettings()
        {
            // Audio
            if (musicVolumeSlider)
            {
                musicVolumeSlider.value = PlayerPrefs.GetFloat("music_volume", 0.8f);
                musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
                UpdateVolumeLabel(musicValueText, musicVolumeSlider.value);
            }
            if (sfxVolumeSlider)
            {
                sfxVolumeSlider.value = PlayerPrefs.GetFloat("sfx_volume", 1f);
                sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
                UpdateVolumeLabel(sfxValueText, sfxVolumeSlider.value);
            }

            // Graphics
            selectedQuality = PlayerPrefs.GetInt("quality_level", QualitySettings.GetQualityLevel());
            UpdateQualityButtons();

            if (qualityLowButton) qualityLowButton.onClick.AddListener(() => SetQuality(0));
            if (qualityMedButton) qualityMedButton.onClick.AddListener(() => SetQuality(1));
            if (qualityHighButton) qualityHighButton.onClick.AddListener(() => SetQuality(2));

            if (vsyncToggle)
            {
                vsyncToggle.isOn = QualitySettings.vSyncCount > 0;
                vsyncToggle.onValueChanged.AddListener(SetVSync);
            }
            if (screenShakeToggle)
            {
                screenShakeToggle.isOn = PlayerPrefs.GetInt("screen_shake", 1) == 1;
                screenShakeToggle.onValueChanged.AddListener(v => PlayerPrefs.SetInt("screen_shake", v ? 1 : 0));
            }
            if (vibrationToggle)
            {
                vibrationToggle.isOn = VibrationManager.Instance != null
                    ? VibrationManager.Instance.IsEnabled
                    : true;
                vibrationToggle.onValueChanged.AddListener(v =>
                {
                    if (VibrationManager.Instance != null)
                        VibrationManager.Instance.SetVibration(v);
                });
            }

            // Controls
            if (sensitivitySlider)
            {
                sensitivitySlider.minValue = 0.5f;
                sensitivitySlider.maxValue = 2f;
                sensitivitySlider.value = PlayerPrefs.GetFloat("sensitivity", 1f);
                sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
                UpdateSliderLabel(sensitivityValueText, sensitivitySlider.value, "x");
            }
            if (joystickSizeSlider)
            {
                joystickSizeSlider.minValue = 0.5f;
                joystickSizeSlider.maxValue = 1.5f;
                joystickSizeSlider.value = PlayerPrefs.GetFloat("joystick_size", 1f);
                joystickSizeSlider.onValueChanged.AddListener(SetJoystickSize);
                UpdateSliderLabel(joystickSizeValueText, joystickSizeSlider.value, "x");
            }

            // Account
            if (playerNameInput)
                playerNameInput.text = PlayerPrefs.GetString("player_name", "Oyuncu");

            if (playerIdText)
                playerIdText.text = $"ID: {SystemInfo.deviceUniqueIdentifier[..8]}";

            UpdateAccountStats();
        }

        // --- Tab Management ---

        void SwitchTab(int index)
        {
            activeTab = index;
            UIAudio.Instance?.PlayClick();

            if (audioPanel) audioPanel.SetActive(index == 0);
            if (graphicsPanel) graphicsPanel.SetActive(index == 1);
            if (controlsPanel) controlsPanel.SetActive(index == 2);
            if (accountPanel) accountPanel.SetActive(index == 3);

            SetTabColor(audioTab, index == 0);
            SetTabColor(graphicsTab, index == 1);
            SetTabColor(controlsTab, index == 2);
            SetTabColor(accountTab, index == 3);
        }

        void SetTabColor(Button tab, bool active)
        {
            if (tab == null) return;
            var img = tab.GetComponent<Image>();
            if (img) img.color = active ? VTheme.Red : VTheme.Card;
            var txt = tab.GetComponentInChildren<TextMeshProUGUI>();
            if (txt) txt.color = active ? VTheme.TextPrimary : VTheme.TextMuted;
        }

        // --- Audio ---

        void SetMusicVolume(float v)
        {
            PlayerPrefs.SetFloat("music_volume", v);
            masterMixer?.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(v, 0.001f)) * 20f);
            UpdateVolumeLabel(musicValueText, v);
        }

        void SetSFXVolume(float v)
        {
            PlayerPrefs.SetFloat("sfx_volume", v);
            if (AudioManager.Instance != null) AudioManager.Instance.sfxVolume = v;
            UpdateVolumeLabel(sfxValueText, v);
        }

        void UpdateVolumeLabel(TextMeshProUGUI label, float value)
        {
            if (label) label.text = $"{Mathf.RoundToInt(value * 100)}%";
        }

        // --- Graphics ---

        void SetQuality(int level)
        {
            selectedQuality = level;
            QualitySettings.SetQualityLevel(level);
            PlayerPrefs.SetInt("quality_level", level);
            UIAudio.Instance?.PlayClick();
            UpdateQualityButtons();
        }

        void UpdateQualityButtons()
        {
            SetQualityButtonState(qualityLowButton, selectedQuality == 0, "DUSUK");
            SetQualityButtonState(qualityMedButton, selectedQuality == 1, "ORTA");
            SetQualityButtonState(qualityHighButton, selectedQuality == 2, "YUKSEK");
        }

        void SetQualityButtonState(Button btn, bool active, string label)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img) img.color = active ? VTheme.Red : VTheme.Card;
            var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt)
            {
                txt.text = label;
                txt.color = active ? VTheme.TextPrimary : VTheme.TextMuted;
            }
        }

        void SetVSync(bool on)
        {
            QualitySettings.vSyncCount = on ? 1 : 0;
            PlayerPrefs.SetInt("vsync", on ? 1 : 0);
        }

        // --- Controls ---

        void SetSensitivity(float v)
        {
            PlayerPrefs.SetFloat("sensitivity", v);
            UpdateSliderLabel(sensitivityValueText, v, "x");
        }

        void SetJoystickSize(float v)
        {
            PlayerPrefs.SetFloat("joystick_size", v);
            UpdateSliderLabel(joystickSizeValueText, v, "x");
        }

        void UpdateSliderLabel(TextMeshProUGUI label, float value, string suffix)
        {
            if (label) label.text = $"{value:F1}{suffix}";
        }

        // --- Account ---

        void SavePlayerName()
        {
            if (playerNameInput == null) return;
            string name = playerNameInput.text.Trim();
            if (string.IsNullOrEmpty(name)) return;
            PlayerPrefs.SetString("player_name", name);
            PlayerPrefs.Save();
            UIAudio.Instance?.PlayClick();
        }

        void UpdateAccountStats()
        {
            int wins = PlayerPrefs.GetInt("ranked_wins", 0) +
                       (SaveManager.Instance?.Data.totalWins ?? 0);
            int losses = PlayerPrefs.GetInt("ranked_losses", 0);
            float playTime = PlayerPrefs.GetFloat("total_play_time", 0f);
            int elo = PlayerPrefs.GetInt("ranked_elo", 1000);

            if (totalWinsText)
            {
                totalWinsText.text = $"Toplam Galibiyet: {wins}";
                totalWinsText.color = VTheme.Green;
            }
            if (totalLossesText)
            {
                totalLossesText.text = $"Toplam Maglubiyet: {losses}";
                totalLossesText.color = VTheme.Red;
            }
            if (totalPlayTimeText)
            {
                int hours = Mathf.FloorToInt(playTime / 3600f);
                int minutes = Mathf.FloorToInt((playTime % 3600f) / 60f);
                totalPlayTimeText.text = $"Oynama Suresi: {hours}s {minutes}dk";
                totalPlayTimeText.color = VTheme.TextSecondary;
            }
            if (rankedEloText)
            {
                rankedEloText.text = $"Ranked ELO: {elo}";
                rankedEloText.color = VTheme.Blue;
            }
        }

        void ShowResetConfirm()
        {
            if (confirmPopup == null) return;
            confirmPopup.SetActive(true);
            if (confirmText)
                confirmText.text = "Tum ilerleme sifirlanacak.\nEmin misin?";
        }

        void OnConfirmReset()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            confirmPopup?.SetActive(false);
            Debug.Log("[Settings] Progress reset");
            SceneManager.LoadScene("MainMenu");
        }

        // --- Tab compatibility ---

        public void OnTabChanged(int tabIndex)
        {
            SwitchTab(tabIndex);
        }

        void OnDisable()
        {
            PlayerPrefs.Save();
        }
    }
}
