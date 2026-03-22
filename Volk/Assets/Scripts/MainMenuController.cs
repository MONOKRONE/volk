using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MainMenuController : MonoBehaviour
{
    public Button playButton;
    public CanvasGroup menuGroup;
    public string combatSceneName = "CombatTest";

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
        playButton.onClick.AddListener(OnPlayPressed);
        StartCoroutine(FadeIn(menuGroup, 1.2f));
    }

    void OnPlayPressed()
    {
        StartCoroutine(FadeOutAndLoad());
    }

    IEnumerator FadeOutAndLoad()
    {
        playButton.interactable = false;
        yield return StartCoroutine(FadeOut(menuGroup, 0.4f));
        SceneManager.LoadScene(combatSceneName);
    }

    IEnumerator FadeIn(CanvasGroup cg, float duration)
    {
        cg.alpha = 0;
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
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
            t += Time.deltaTime;
            cg.alpha = 1 - (t / duration);
            yield return null;
        }
        cg.alpha = 0;
    }
}
