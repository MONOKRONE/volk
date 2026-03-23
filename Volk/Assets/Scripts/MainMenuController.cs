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
        // Disable old Canvas/buttons on the MainMenu scene that might conflict
        var canvases = GetComponentsInChildren<Canvas>(true);
        foreach (var c in canvases)
        {
            if (c.gameObject != gameObject)
                c.gameObject.SetActive(false);
        }

        // Disable old buttons/UI on this object
        var buttons = GetComponentsInChildren<Button>(true);
        foreach (var b in buttons) b.gameObject.SetActive(false);

        var groups = GetComponentsInChildren<CanvasGroup>(true);
        foreach (var g in groups) g.alpha = 0;
    }
}
