using UnityEngine;

public class VibrationManager : MonoBehaviour
{
    public static VibrationManager Instance;
    private bool vibrationEnabled = true;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        vibrationEnabled = PlayerPrefs.GetInt("VibrationEnabled", 1) == 1;
    }

    public bool IsEnabled => vibrationEnabled;

    public void SetVibration(bool enabled)
    {
        vibrationEnabled = enabled;
        PlayerPrefs.SetInt("VibrationEnabled", enabled ? 1 : 0);
    }

    public void VibrateLight()
    {
        if (!vibrationEnabled) return;
        #if UNITY_ANDROID && !UNITY_EDITOR
        Handheld.Vibrate();
        #endif
    }

    public void VibrateHeavy()
    {
        if (!vibrationEnabled) return;
        #if UNITY_ANDROID && !UNITY_EDITOR
        Handheld.Vibrate();
        Handheld.Vibrate();
        #endif
    }
}
