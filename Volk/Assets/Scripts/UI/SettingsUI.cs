using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;

namespace Volk.UI
{
    public class SettingsUI : MonoBehaviour
    {
        [Header("Audio")]
        public Slider musicVolumeSlider;
        public Slider sfxVolumeSlider;
        public AudioMixer masterMixer;

        [Header("Graphics")]
        public TMP_Dropdown qualityDropdown;
        public Toggle vsyncToggle;
        public Toggle screenShakeToggle;

        [Header("Controls")]
        public Slider joystickSizeSlider;
        public Toggle vibrationToggle;
        public TMP_Dropdown controlLayoutDropdown;

        [Header("Account")]
        public TextMeshProUGUI playerIdText;
        public VButton resetProgressButton;
        public VButton deleteAccountButton;

        [Header("Tabs")]
        public VTabBar tabBar;
        public GameObject audioPanel;
        public GameObject graphicsPanel;
        public GameObject controlsPanel;
        public GameObject accountPanel;

        void OnEnable() => LoadSettings();

        void LoadSettings()
        {
            if (musicVolumeSlider)
            {
                musicVolumeSlider.value = PlayerPrefs.GetFloat("music_volume", 0.8f);
                musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
            }
            if (sfxVolumeSlider)
            {
                sfxVolumeSlider.value = PlayerPrefs.GetFloat("sfx_volume", 1f);
                sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
            }
            if (qualityDropdown)
            {
                qualityDropdown.value = QualitySettings.GetQualityLevel();
                qualityDropdown.onValueChanged.AddListener(SetQuality);
            }
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
                vibrationToggle.isOn = PlayerPrefs.GetInt("vibration", 1) == 1;
                vibrationToggle.onValueChanged.AddListener(v => PlayerPrefs.SetInt("vibration", v ? 1 : 0));
            }
            if (joystickSizeSlider)
            {
                joystickSizeSlider.value = PlayerPrefs.GetFloat("joystick_size", 1f);
                joystickSizeSlider.onValueChanged.AddListener(v => PlayerPrefs.SetFloat("joystick_size", v));
            }
            if (playerIdText)
                playerIdText.text = $"ID: {SystemInfo.deviceUniqueIdentifier[..8]}";
        }

        void SetMusicVolume(float v)
        {
            PlayerPrefs.SetFloat("music_volume", v);
            masterMixer?.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(v, 0.001f)) * 20f);
        }

        void SetSFXVolume(float v)
        {
            PlayerPrefs.SetFloat("sfx_volume", v);
            if (AudioManager.Instance != null) AudioManager.Instance.sfxVolume = v;
        }

        void SetQuality(int level)
        {
            QualitySettings.SetQualityLevel(level);
            PlayerPrefs.SetInt("quality_level", level);
        }

        void SetVSync(bool on)
        {
            QualitySettings.vSyncCount = on ? 1 : 0;
            PlayerPrefs.SetInt("vsync", on ? 1 : 0);
        }

        public void OnTabChanged(int tabIndex)
        {
            if (audioPanel) audioPanel.SetActive(tabIndex == 0);
            if (graphicsPanel) graphicsPanel.SetActive(tabIndex == 1);
            if (controlsPanel) controlsPanel.SetActive(tabIndex == 2);
            if (accountPanel) accountPanel.SetActive(tabIndex == 3);
        }

        void OnDisable()
        {
            PlayerPrefs.Save();
        }
    }
}
