using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Volk.Core;
using Volk.UI;

public class MainMenuController : MonoBehaviour
{
    void Awake()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
    }

    void Start()
    {
        // Ensure singletons exist
        EnsureSingleton<RuntimeUIBuilder>("RuntimeUIBuilder");
        EnsureSingleton<GameFlowManager>("GameFlowManager");
        EnsureSingleton<SaveManager>("SaveManager");
        EnsureSingleton<CurrencyManager>("CurrencyManager");
        EnsureSingleton<LevelSystem>("LevelSystem");
        EnsureSingleton<StarRatingSystem>("StarRatingSystem");
        EnsureSingleton<CharacterUnlockManager>("CharacterUnlockManager");
        EnsureSingleton<GameSettings>("GameSettings");
        EnsureSingleton<BattlePassManager>("BattlePassManager");
        EnsureSingleton<EquipmentManager>("EquipmentManager");
        EnsureSingleton<CosmeticManager>("CosmeticManager");
        EnsureSingleton<CharacterDLCManager>("CharacterDLCManager");
        EnsureSingleton<PlayerBehaviorTracker>("PlayerBehaviorTracker");
        EnsureSingleton<StageManager>("StageManager");

        // Disable any old UI elements on this GameObject
        DisableOldUI();

        // Check if returning from combat
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.returnFromCombat)
        {
            GameFlowManager.Instance.returnFromCombat = false;
            GameFlowManager.Instance.ChangeState(GameState.MatchResult);
        }
        else if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.ChangeState(GameState.Splash);
        }
    }

    void EnsureSingleton<T>(string name) where T : MonoBehaviour
    {
        if (FindAnyObjectByType<T>() == null)
        {
            var go = new GameObject(name);
            go.AddComponent<T>();
        }
    }

    void DisableOldUI()
    {
        // Only disable the old scene Canvas that this controller sits on
        // Do NOT touch RuntimeUIBuilder's canvas (it's on a different hierarchy)
        var myCanvas = GetComponent<Canvas>();
        if (myCanvas != null)
            myCanvas.enabled = false;

        var myGroup = GetComponent<CanvasGroup>();
        if (myGroup != null)
            myGroup.alpha = 0;
    }
}
