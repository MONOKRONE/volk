using UnityEngine;
using System.Collections;

public class VibrationManager : MonoBehaviour
{
    public static VibrationManager Instance;
    private bool vibrationEnabled = true;

    // Duration presets (milliseconds)
    private const long LIGHT_DURATION = 50;
    private const long HEAVY_DURATION = 150;
    private const long KO_DURATION = 300;
    private const long EX_DURATION = 200;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        vibrationEnabled = PlayerPrefs.GetInt("VibrationEnabled", 1) == 1;
    }

    public bool IsEnabled => vibrationEnabled;

    public void SetVibration(bool enabled)
    {
        vibrationEnabled = enabled;
        PlayerPrefs.SetInt("VibrationEnabled", enabled ? 1 : 0);
    }

    public void VibrateLight(float multiplier = 1f)
    {
        if (!vibrationEnabled) return;
        long duration = (long)(LIGHT_DURATION * multiplier);
        VibrateAndroid(duration);
    }

    public void VibrateHeavy(float multiplier = 1f)
    {
        if (!vibrationEnabled) return;
        long duration = (long)(HEAVY_DURATION * multiplier);
        VibrateAndroid(duration);
    }

    public void VibrateKO(float multiplier = 1f)
    {
        if (!vibrationEnabled) return;
        long duration = (long)(KO_DURATION * multiplier);
        VibrateAndroid(duration);
    }

    public void VibrateEX(float multiplier = 1f)
    {
        if (!vibrationEnabled) return;
        long duration = (long)(EX_DURATION * multiplier);
        VibrateAndroid(duration);
    }

    private void VibrateAndroid(long milliseconds)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                if (vibrator != null)
                    vibrator.Call("vibrate", milliseconds);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Vibration] Android vibrate failed: {e.Message}");
            Handheld.Vibrate(); // Fallback
        }
#endif
    }
}
