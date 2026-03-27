using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Volk.Core;

namespace Volk.UI
{
    public class MainHubUI : MonoBehaviour
    {
        [Header("Top Bar")]
        public VTopBar topBar;

        [Header("Mode Cards")]
        public Button storyCard;
        public Button quickFightCard;
        public Button onlineCard;
        public Button ghostCard;
        public Button survivalCard;
        public Button trainingCard;

        [Header("Card Titles")]
        public TextMeshProUGUI storyTitle;
        public TextMeshProUGUI quickFightTitle;
        public TextMeshProUGUI onlineTitle;
        public TextMeshProUGUI ghostTitle;
        public TextMeshProUGUI survivalTitle;
        public TextMeshProUGUI trainingTitle;

        [Header("Card Subtitles")]
        public TextMeshProUGUI storySubtitle;
        public TextMeshProUGUI quickFightSubtitle;
        public TextMeshProUGUI onlineSubtitle;
        public TextMeshProUGUI ghostSubtitle;
        public TextMeshProUGUI survivalSubtitle;
        public TextMeshProUGUI trainingSubtitle;

        [Header("Card Icons")]
        public Image storyIcon;
        public Image quickFightIcon;
        public Image onlineIcon;
        public Image ghostIcon;
        public Image survivalIcon;
        public Image trainingIcon;

        [Header("Tab Bar")]
        public VTabBar tabBar;

        [Header("Daily Quest Badge")]
        public GameObject questBadge;
        public TextMeshProUGUI questBadgeCount;

        [Header("Background")]
        public Image backgroundImage;

        [Header("Scenes")]
        public string storyScene = "StoryMenu";
        public string quickFightScene = "QuickFight";
        public string combatScene = "CombatTest";

        void Start()
        {
            EnsureSingletons();

            if (backgroundImage)
                backgroundImage.color = VTheme.Background;

            // Card setup
            SetupCard(storyCard, storyTitle, storySubtitle, "STORY", GetStorySubtitle(), VTheme.Red);
            SetupCard(quickFightCard, quickFightTitle, quickFightSubtitle, "QUICK FIGHT", "Pick fighter & arena", VTheme.Blue);
            SetupCard(onlineCard, onlineTitle, onlineSubtitle, "ONLINE", "Live 1v1 PvP", VTheme.Purple);
            SetupCard(ghostCard, ghostTitle, ghostSubtitle, "GHOST", "Fight your ghost", VTheme.Cyan);
            SetupCard(survivalCard, survivalTitle, survivalSubtitle, "SURVIVAL", GetSurvivalSubtitle(), VTheme.Orange);
            SetupCard(trainingCard, trainingTitle, trainingSubtitle, "TRAINING", "Practice combos", VTheme.Green);

            // Card click listeners
            if (storyCard) storyCard.onClick.AddListener(() => LoadScene(storyScene));
            if (quickFightCard) quickFightCard.onClick.AddListener(() => LoadScene(quickFightScene));
            if (onlineCard) onlineCard.onClick.AddListener(OnOnlineClick);
            if (ghostCard) ghostCard.onClick.AddListener(StartGhost);
            if (survivalCard) survivalCard.onClick.AddListener(StartSurvival);
            if (trainingCard) trainingCard.onClick.AddListener(StartTraining);

            UpdateQuestBadge();
            StartCoroutine(AnimateCardsIn());
        }

        void EnsureSingletons()
        {
            if (GameSettings.Instance == null)
            {
                var go = new GameObject("GameSettings");
                go.AddComponent<GameSettings>();
            }
            if (SaveManager.Instance == null)
            {
                var go = new GameObject("SaveManager");
                go.AddComponent<SaveManager>();
            }
            if (LevelSystem.Instance == null)
            {
                var go = new GameObject("LevelSystem");
                go.AddComponent<LevelSystem>();
            }
        }

        void SetupCard(Button card, TextMeshProUGUI title, TextMeshProUGUI subtitle, string titleStr, string subtitleStr, Color accentColor)
        {
            if (card != null)
            {
                var img = card.GetComponent<Image>();
                if (img) img.color = VTheme.Card;
            }
            if (title)
            {
                title.text = titleStr;
                title.color = accentColor;
            }
            if (subtitle)
            {
                subtitle.text = subtitleStr;
                subtitle.color = VTheme.TextSecondary;
            }
        }

        string GetStorySubtitle()
        {
            int chapter = SaveManager.Instance?.Data.completedChapter ?? 0;
            if (chapter == 0) return "Start the adventure";
            return $"Chapter {chapter + 1} - continue";
        }

        string GetSurvivalSubtitle()
        {
            int highScore = PlayerPrefs.GetInt("survival_highscore", 0);
            if (highScore == 0) return "How long can you last?";
            return $"Best: {highScore} pts";
        }

        void OnOnlineClick()
        {
            UIAudio.Instance?.PlayClick();
            // Online not yet implemented — show coming soon feedback
            Debug.Log("[VOLK] Online mode coming soon");
        }

        void StartGhost()
        {
            GameSettings.Instance.currentMode = GameSettings.GameMode.Ghost;
            LoadScene("CombatTest");
        }

        void StartSurvival()
        {
            GameSettings.Instance.currentMode = GameSettings.GameMode.Survival;
            LoadScene("CombatTest");
        }

        void StartTraining()
        {
            GameSettings.Instance.currentMode = GameSettings.GameMode.Training;
            LoadScene("CombatTest");
        }

        void LoadScene(string scene)
        {
            UIAudio.Instance?.PlayClick();
            StartCoroutine(FadeOutAndLoad(scene));
        }

        void UpdateQuestBadge()
        {
            if (Volk.Meta.DailyQuestManager.Instance != null)
            {
                int count = Volk.Meta.DailyQuestManager.Instance.UnclaimedCount();
                if (questBadge) questBadge.SetActive(count > 0);
                if (questBadgeCount) questBadgeCount.text = count.ToString();
            }
            else
            {
                if (questBadge) questBadge.SetActive(false);
            }
        }

        IEnumerator AnimateCardsIn()
        {
            Button[] cards = { storyCard, quickFightCard, onlineCard, ghostCard, survivalCard, trainingCard };
            foreach (var card in cards)
            {
                if (card == null) continue;
                var cg = card.GetComponent<CanvasGroup>();
                if (cg == null) cg = card.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0;
            }

            yield return new WaitForSeconds(0.2f);

            foreach (var card in cards)
            {
                if (card == null) continue;
                var cg = card.GetComponent<CanvasGroup>();
                var rt = card.GetComponent<RectTransform>();
                if (cg == null || rt == null) continue;

                Vector2 originalPos = rt.anchoredPosition;
                rt.anchoredPosition = originalPos + new Vector2(0, -50f);
                float t = 0;
                while (t < 0.3f)
                {
                    t += Time.deltaTime;
                    float ease = 1f - Mathf.Pow(1f - t / 0.3f, 3f);
                    cg.alpha = ease;
                    rt.anchoredPosition = Vector2.Lerp(originalPos + new Vector2(0, -50f), originalPos, ease);
                    yield return null;
                }
                cg.alpha = 1;
                rt.anchoredPosition = originalPos;
            }
        }

        IEnumerator FadeOutAndLoad(string scene)
        {
            if (VScreenTransition.Instance != null)
                yield return VScreenTransition.Instance.FadeOut();
            else
                yield return new WaitForSeconds(0.1f);
            SceneManager.LoadScene(scene);
        }
    }
}
