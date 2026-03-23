using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Volk.UI;

namespace Volk.Core
{
    public enum GameState
    {
        Splash, MainHub, StoryMap, CharacterSelect, QuickFight, Survival,
        Training, Combat, MatchResult, Equipment, Achievements, Shop, Settings
    }

    public class GameFlowManager : MonoBehaviour
    {
        public static GameFlowManager Instance { get; private set; }

        public GameState CurrentState { get; private set; }
        public GameState PreviousState { get; private set; }

        // Set before loading CombatTest to signal return
        public bool returnFromCombat;
        public bool lastMatchWon;

        // Story mode tracking
        public int selectedChapterIndex;
        private int selectedCharacterIndex;

        // Character/chapter data loaded from Resources
        private CharacterData[] allCharacters;
        private ChapterData[] allChapters;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadResources();
        }

        void LoadResources()
        {
            allCharacters = Resources.LoadAll<CharacterData>("Characters");
            allChapters = Resources.LoadAll<ChapterData>("Chapters");

            // Sort chapters by number
            if (allChapters != null && allChapters.Length > 0)
                System.Array.Sort(allChapters, (a, b) => a.chapterNumber.CompareTo(b.chapterNumber));
        }

        public void ChangeState(GameState newState)
        {
            PreviousState = CurrentState;
            CurrentState = newState;

            var ui = RuntimeUIBuilder.Instance;
            if (ui == null) return;
            ui.EnsureCanvas();
            ui.ClearUI();

            switch (newState)
            {
                case GameState.Splash:       StartCoroutine(BuildSplash()); break;
                case GameState.MainHub:      BuildMainHub(); break;
                case GameState.StoryMap:     BuildStoryMap(); break;
                case GameState.CharacterSelect: BuildCharacterSelect(); break;
                case GameState.MatchResult:  BuildMatchResult(); break;
                case GameState.Settings:     BuildSettings(); break;
                default: BuildMainHub(); break;
            }
        }

        public void ReturnToPrevious()
        {
            ChangeState(PreviousState);
        }

        // ============================================================
        // SPLASH
        // ============================================================
        IEnumerator BuildSplash()
        {
            var ui = RuntimeUIBuilder.Instance;
            var canvas = ui.CanvasRect;

            // Full black BG
            ui.CreatePanel(canvas, Vector2.zero, Vector2.one, Color.black);

            // VOLK text
            var textGO = new GameObject("VolkTitle", typeof(RectTransform));
            textGO.transform.SetParent(canvas, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.3f, 0.35f);
            textRect.anchorMax = new Vector2(0.7f, 0.65f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "VOLK";
            tmp.fontSize = 120;
            tmp.color = RuntimeUIBuilder.White;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;

            textGO.transform.localScale = Vector3.zero;

            var cg = ui.EnsureCanvasGroup(canvas.gameObject);
            cg.alpha = 1;

            // Scale animation: 0 -> 1.3 -> 1.0 in 0.5s
            yield return StartCoroutine(ui.ScaleOvershoot(textGO.transform, 0f, 1.3f, 1f, 0.5f));

            // Wait 2.5s
            yield return new WaitForSecondsRealtime(2.5f);

            // Fade out
            var splashCG = ui.EnsureCanvasGroup(canvas.GetChild(0).gameObject);
            // Actually fade entire canvas
            yield return StartCoroutine(ui.FadeOut(cg, 0.4f));

            ChangeState(GameState.MainHub);
        }

        // ============================================================
        // MAIN HUB
        // ============================================================
        void BuildMainHub()
        {
            var ui = RuntimeUIBuilder.Instance;
            var canvas = ui.CanvasRect;

            // Background
            ui.CreatePanel(canvas, Vector2.zero, Vector2.one, RuntimeUIBuilder.BG);

            // === TOP BAR (top 10%) ===
            var topBar = ui.CreatePanel(canvas, new Vector2(0, 0.90f), Vector2.one, RuntimeUIBuilder.Panel);
            var topRect = topBar.GetComponent<RectTransform>();

            // Player name + level (left)
            int level = LevelSystem.Instance != null ? LevelSystem.Instance.CurrentLevel : 1;
            string title = LevelSystem.Instance != null ? LevelSystem.Instance.GetLevelTitle() : "Cirak";
            var nameText = ui.CreateText(topBar.transform, $"SAVASCI  Lv.{level}", 26, RuntimeUIBuilder.White,
                TextAlignmentOptions.MidlineLeft);
            var nameRect = nameText.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.02f, 0);
            nameRect.anchorMax = new Vector2(0.4f, 1);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            // Currency (right)
            int coins = CurrencyManager.Instance != null ? CurrencyManager.Instance.Coins : 0;
            int gems = CurrencyManager.Instance != null ? CurrencyManager.Instance.Gems : 0;
            var currText = ui.CreateText(topBar.transform, $"<color=#FFD700>@ {coins}</color>    <color=#00D4FF>* {gems}</color>", 24,
                RuntimeUIBuilder.White, TextAlignmentOptions.MidlineRight);
            var currRect = currText.GetComponent<RectTransform>();
            currRect.anchorMin = new Vector2(0.55f, 0);
            currRect.anchorMax = new Vector2(0.98f, 1);
            currRect.offsetMin = Vector2.zero;
            currRect.offsetMax = Vector2.zero;
            currText.richText = true;

            // XP bar (thin line below top bar)
            float xpProgress = LevelSystem.Instance != null ? LevelSystem.Instance.XPProgress : 0f;
            var xpBG = ui.CreatePanel(canvas, new Vector2(0, 0.888f), new Vector2(1, 0.895f), new Color(0.15f, 0.15f, 0.25f));
            var xpFill = ui.CreatePanel(canvas, new Vector2(0, 0.888f), new Vector2(xpProgress, 0.895f), RuntimeUIBuilder.Gold);

            // === MODE CARDS (center area) ===
            float cardStartY = 0.72f;
            float cardHeight = 0.14f;
            float cardGap = 0.02f;
            float cardXMin = 0.10f;
            float cardXMax = 0.90f;

            var cardConfigs = new (string name, Color stripe, string extra, System.Action action)[]
            {
                ("HIKAYE MODU", RuntimeUIBuilder.Accent,
                    GetStoryProgress(),
                    () => ChangeState(GameState.StoryMap)),

                ("SERBEST DOVUS", RuntimeUIBuilder.Neon, "",
                    () => {
                        if (GameSettings.Instance != null)
                            GameSettings.Instance.currentMode = GameSettings.GameMode.QuickFight;
                        ChangeState(GameState.CharacterSelect);
                    }),

                ("HAYATTA KAL", RuntimeUIBuilder.Purple,
                    $"Rekor: {PlayerPrefs.GetInt("survival_highscore", 0)}. Round",
                    () => {
                        if (GameSettings.Instance != null)
                            GameSettings.Instance.currentMode = GameSettings.GameMode.Survival;
                        ChangeState(GameState.CharacterSelect);
                    }),

                ("ANTRENMAN", RuntimeUIBuilder.Green, "",
                    () => {
                        if (GameSettings.Instance != null)
                            GameSettings.Instance.currentMode = GameSettings.GameMode.Training;
                        ChangeState(GameState.CharacterSelect);
                    })
            };

            for (int i = 0; i < cardConfigs.Length; i++)
            {
                float yMax = cardStartY - i * (cardHeight + cardGap);
                float yMin = yMax - cardHeight;
                BuildModeCard(canvas, cardConfigs[i].name, cardConfigs[i].stripe,
                    cardConfigs[i].extra, cardXMin, cardXMax, yMin, yMax, cardConfigs[i].action, i);
            }

            // === SETTINGS BUTTON (bottom center) ===
            ui.CreateButton(canvas, "AYARLAR", RuntimeUIBuilder.Panel, RuntimeUIBuilder.Gray,
                new Vector2(0.38f, 0.03f), new Vector2(0.62f, 0.09f),
                () => ChangeState(GameState.Settings));

            // Animate: fade in
            var cg = ui.EnsureCanvasGroup(canvas.GetChild(0).gameObject);
            // Fade entire canvas
            var rootCG = ui.EnsureCanvasGroup(canvas.gameObject);
            rootCG.alpha = 0;
            StartCoroutine(ui.FadeIn(rootCG, 0.3f));
        }

        void BuildModeCard(RectTransform canvas, string title, Color stripeColor, string extra,
            float xMin, float xMax, float yMin, float yMax, System.Action onClick, int index)
        {
            var ui = RuntimeUIBuilder.Instance;

            // Card background (button)
            var btn = ui.CreateButton(canvas, "", RuntimeUIBuilder.Panel, RuntimeUIBuilder.White,
                new Vector2(xMin, yMin), new Vector2(xMax, yMax), onClick);
            var cardRect = btn.GetComponent<RectTransform>();

            // Left stripe
            var stripe = ui.CreatePanel(btn.transform, new Vector2(0, 0), new Vector2(0.02f, 1), stripeColor);

            // Card title
            var titleText = ui.CreateText(btn.transform, title, 32, RuntimeUIBuilder.White,
                TextAlignmentOptions.MidlineLeft);
            var titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0);
            titleRect.anchorMax = new Vector2(0.7f, 1);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            titleText.fontStyle = FontStyles.Bold;

            // Extra text (right side)
            if (!string.IsNullOrEmpty(extra))
            {
                var extraText = ui.CreateText(btn.transform, extra, 20, RuntimeUIBuilder.Gray,
                    TextAlignmentOptions.MidlineRight);
                var extraRect = extraText.GetComponent<RectTransform>();
                extraRect.anchorMin = new Vector2(0.6f, 0);
                extraRect.anchorMax = new Vector2(0.95f, 1);
                extraRect.offsetMin = Vector2.zero;
                extraRect.offsetMax = Vector2.zero;
            }

            // Stagger slide-in animation
            StartCoroutine(StaggerSlideIn(cardRect, index));
        }

        IEnumerator StaggerSlideIn(RectTransform rect, int index)
        {
            yield return new WaitForSecondsRealtime(index * 0.1f);
            var from = new Vector2(rect.anchoredPosition.x - 600, rect.anchoredPosition.y);
            yield return StartCoroutine(RuntimeUIBuilder.Instance.SlideIn(rect, from, 0.35f));
        }

        string GetStoryProgress()
        {
            int completed = SaveManager.Instance != null ? SaveManager.Instance.Data.completedChapter : 0;
            int total = allChapters != null && allChapters.Length > 0 ? allChapters.Length : 12;
            return $"Bolum {completed}/{total}";
        }

        // ============================================================
        // STORY MAP
        // ============================================================
        void BuildStoryMap()
        {
            var ui = RuntimeUIBuilder.Instance;
            var canvas = ui.CanvasRect;

            // Background
            ui.CreatePanel(canvas, Vector2.zero, Vector2.one, RuntimeUIBuilder.BG);

            // Top bar
            var topBar = ui.CreatePanel(canvas, new Vector2(0, 0.90f), Vector2.one, RuntimeUIBuilder.Panel);

            // Back button
            ui.CreateButton(topBar.transform, "< GERI", RuntimeUIBuilder.Panel, RuntimeUIBuilder.White,
                new Vector2(0, 0), new Vector2(0.15f, 1), () => ChangeState(GameState.MainHub));

            // Title
            var titleText = ui.CreateText(topBar.transform, "HIKAYE MODU", 32, RuntimeUIBuilder.Accent,
                TextAlignmentOptions.Center);
            titleText.fontStyle = FontStyles.Bold;

            // ScrollRect for chapters
            var scrollRect = ui.CreateScrollRect(canvas, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.88f));
            var content = scrollRect.content;

            int completedChapter = SaveManager.Instance != null ? SaveManager.Instance.Data.completedChapter : 0;
            int chapterCount = allChapters != null && allChapters.Length > 0 ? allChapters.Length : 12;

            for (int i = 0; i < chapterCount; i++)
            {
                BuildChapterRow(content, i, completedChapter, chapterCount);
            }
        }

        void BuildChapterRow(RectTransform content, int index, int completedChapter, int totalChapters)
        {
            var ui = RuntimeUIBuilder.Instance;

            var rowGO = new GameObject($"Chapter_{index}", typeof(RectTransform));
            rowGO.transform.SetParent(content, false);
            var rowRect = rowGO.GetComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(0, 100);
            var le = rowGO.AddComponent<LayoutElement>();
            le.preferredHeight = 100;
            le.flexibleWidth = 1;

            bool isCompleted = index < completedChapter;
            bool isCurrent = index == completedChapter;
            bool isLocked = index > completedChapter;

            Color bgColor = isLocked ? new Color(0.08f, 0.08f, 0.12f) : RuntimeUIBuilder.Panel;
            var bg = rowGO.AddComponent<Image>();
            bg.color = bgColor;

            // Current chapter: red border effect
            if (isCurrent)
            {
                var border = ui.CreatePanel(rowGO.transform, new Vector2(0, 0), new Vector2(0.005f, 1), RuntimeUIBuilder.Accent);
                var borderR = ui.CreatePanel(rowGO.transform, new Vector2(0.995f, 0), new Vector2(1, 1), RuntimeUIBuilder.Accent);
            }

            // Chapter number
            string chapterName = $"Bolum {index + 1}";
            if (allChapters != null && index < allChapters.Length && allChapters[index] != null)
                chapterName = allChapters[index].chapterTitle ?? chapterName;

            var numText = ui.CreateText(rowGO.transform, $"Bolum {index + 1}", 22,
                isLocked ? RuntimeUIBuilder.Gray : RuntimeUIBuilder.White, TextAlignmentOptions.MidlineLeft);
            var numRect = numText.GetComponent<RectTransform>();
            numRect.anchorMin = new Vector2(0.02f, 0);
            numRect.anchorMax = new Vector2(0.25f, 1);
            numRect.offsetMin = Vector2.zero;
            numRect.offsetMax = Vector2.zero;

            // Enemy name / chapter title
            string enemyName = "";
            if (allChapters != null && index < allChapters.Length && allChapters[index] != null)
            {
                if (allChapters[index].enemyCharacter != null)
                    enemyName = allChapters[index].enemyCharacter.characterName;
            }
            var midText = ui.CreateText(rowGO.transform, enemyName, 20,
                isLocked ? RuntimeUIBuilder.Gray : RuntimeUIBuilder.Neon, TextAlignmentOptions.Center);
            var midRect = midText.GetComponent<RectTransform>();
            midRect.anchorMin = new Vector2(0.25f, 0);
            midRect.anchorMax = new Vector2(0.55f, 1);
            midRect.offsetMin = Vector2.zero;
            midRect.offsetMax = Vector2.zero;

            // Stars
            if (isCompleted)
            {
                int stars = StarRatingSystem.Instance != null ? StarRatingSystem.Instance.GetChapterStars(index) : 0;
                string starStr = "";
                for (int s = 0; s < 3; s++)
                    starStr += s < stars ? "<color=#FFD700>#</color>" : "<color=#8888AA>#</color>";
                var starText = ui.CreateText(rowGO.transform, starStr, 24, RuntimeUIBuilder.White, TextAlignmentOptions.Center);
                starText.richText = true;
                var starRect = starText.GetComponent<RectTransform>();
                starRect.anchorMin = new Vector2(0.55f, 0);
                starRect.anchorMax = new Vector2(0.72f, 1);
                starRect.offsetMin = Vector2.zero;
                starRect.offsetMax = Vector2.zero;
            }

            // PLAY button
            if (!isLocked)
            {
                int chapterIdx = index;
                var playBtn = ui.CreateButton(rowGO.transform, "OYNA", RuntimeUIBuilder.Accent, RuntimeUIBuilder.White,
                    new Vector2(0.75f, 0.15f), new Vector2(0.97f, 0.85f),
                    () => {
                        selectedChapterIndex = chapterIdx;
                        if (GameSettings.Instance != null)
                            GameSettings.Instance.currentMode = GameSettings.GameMode.Story;
                        ChangeState(GameState.CharacterSelect);
                    });
            }
            else
            {
                // Locked indicator
                var lockText = ui.CreateText(rowGO.transform, "KILITLI", 18, RuntimeUIBuilder.Gray,
                    TextAlignmentOptions.Center);
                var lockRect = lockText.GetComponent<RectTransform>();
                lockRect.anchorMin = new Vector2(0.75f, 0);
                lockRect.anchorMax = new Vector2(0.97f, 1);
                lockRect.offsetMin = Vector2.zero;
                lockRect.offsetMax = Vector2.zero;
            }
        }

        // ============================================================
        // CHARACTER SELECT
        // ============================================================
        void BuildCharacterSelect()
        {
            var ui = RuntimeUIBuilder.Instance;
            var canvas = ui.CanvasRect;

            // Background
            ui.CreatePanel(canvas, Vector2.zero, Vector2.one, RuntimeUIBuilder.BG);

            // Top bar
            var topBar = ui.CreatePanel(canvas, new Vector2(0, 0.90f), Vector2.one, RuntimeUIBuilder.Panel);
            ui.CreateButton(topBar.transform, "< GERI", RuntimeUIBuilder.Panel, RuntimeUIBuilder.White,
                new Vector2(0, 0), new Vector2(0.15f, 1), () => ReturnToPrevious());
            var titleText = ui.CreateText(topBar.transform, "KARAKTER SEC", 32, RuntimeUIBuilder.Accent,
                TextAlignmentOptions.Center);
            titleText.fontStyle = FontStyles.Bold;

            // ScrollRect for characters
            var scrollRect = ui.CreateScrollRect(canvas, new Vector2(0.05f, 0.14f), new Vector2(0.95f, 0.88f));
            var content = scrollRect.content;

            selectedCharacterIndex = -1;
            GameObject selectedBorder = null;

            if (allCharacters != null && allCharacters.Length > 0)
            {
                for (int i = 0; i < allCharacters.Length; i++)
                {
                    BuildCharacterCard(content, i, ref selectedBorder);
                }
            }
            else
            {
                // No characters found - placeholder
                ui.CreateText(content, "Karakter bulunamadi", 24, RuntimeUIBuilder.Gray, TextAlignmentOptions.Center);
            }

            // FIGHT button (bottom)
            ui.CreateButton(canvas, "SAVAS", RuntimeUIBuilder.Accent, RuntimeUIBuilder.White,
                new Vector2(0.10f, 0.02f), new Vector2(0.90f, 0.11f),
                () => StartCombat());
        }

        void BuildCharacterCard(RectTransform content, int index, ref GameObject selectedBorder)
        {
            var ui = RuntimeUIBuilder.Instance;
            var charData = allCharacters[index];
            bool unlocked = CharacterUnlockManager.Instance != null
                ? CharacterUnlockManager.Instance.IsUnlocked(charData)
                : charData.unlockedByDefault;

            var cardGO = new GameObject($"Char_{index}", typeof(RectTransform));
            cardGO.transform.SetParent(content, false);
            var cardRect = cardGO.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(0, 160);
            var le = cardGO.AddComponent<LayoutElement>();
            le.preferredHeight = 160;
            le.flexibleWidth = 1;

            var bg = cardGO.AddComponent<Image>();
            bg.color = unlocked ? RuntimeUIBuilder.Panel : new Color(0.08f, 0.08f, 0.12f);

            // Character name
            var nameText = ui.CreateText(cardGO.transform, charData.characterName ?? $"Fighter {index + 1}", 28,
                unlocked ? RuntimeUIBuilder.White : RuntimeUIBuilder.Gray, TextAlignmentOptions.MidlineLeft);
            nameText.fontStyle = FontStyles.Bold;
            var nameRect = nameText.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.03f, 0.6f);
            nameRect.anchorMax = new Vector2(0.4f, 0.95f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            if (unlocked)
            {
                // Stats bars
                BuildStatBar(cardGO.transform, "SPD", charData.speed / 10f, RuntimeUIBuilder.Neon, 0.45f, 0.03f);
                BuildStatBar(cardGO.transform, "PWR", charData.power / 10f, RuntimeUIBuilder.Accent, 0.35f, 0.03f);
                BuildStatBar(cardGO.transform, "DEF", charData.defense / 10f, RuntimeUIBuilder.Green, 0.25f, 0.03f);

                // SELECT button
                int charIdx = index;
                ui.CreateButton(cardGO.transform, "SEC", RuntimeUIBuilder.Neon, RuntimeUIBuilder.BG,
                    new Vector2(0.78f, 0.2f), new Vector2(0.97f, 0.8f),
                    () => SelectCharacter(charIdx));
            }
            else
            {
                // Lock info
                string desc = CharacterUnlockManager.Instance != null
                    ? CharacterUnlockManager.Instance.GetUnlockDescription(charData)
                    : "Kilitli";
                var lockText = ui.CreateText(cardGO.transform, $"KILITLI - {desc}", 18,
                    RuntimeUIBuilder.Gray, TextAlignmentOptions.Center);
                var lockRect = lockText.GetComponent<RectTransform>();
                lockRect.anchorMin = new Vector2(0.03f, 0.05f);
                lockRect.anchorMax = new Vector2(0.97f, 0.55f);
                lockRect.offsetMin = Vector2.zero;
                lockRect.offsetMax = Vector2.zero;
            }
        }

        void BuildStatBar(Transform parent, string label, float fill, Color color, float yCenter, float height)
        {
            var ui = RuntimeUIBuilder.Instance;

            // Label
            var labelText = ui.CreateText(parent, label, 16, RuntimeUIBuilder.Gray, TextAlignmentOptions.MidlineRight);
            var labelRect = labelText.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.03f, yCenter - height);
            labelRect.anchorMax = new Vector2(0.12f, yCenter + height);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            // BG bar
            ui.CreatePanel(parent, new Vector2(0.13f, yCenter - height * 0.5f),
                new Vector2(0.75f, yCenter + height * 0.5f), new Color(0.15f, 0.15f, 0.25f));

            // Fill bar
            float fillWidth = 0.13f + (0.75f - 0.13f) * Mathf.Clamp01(fill);
            ui.CreatePanel(parent, new Vector2(0.13f, yCenter - height * 0.5f),
                new Vector2(fillWidth, yCenter + height * 0.5f), color);
        }

        void SelectCharacter(int index)
        {
            selectedCharacterIndex = index;
            if (GameSettings.Instance != null && allCharacters != null && index < allCharacters.Length)
                GameSettings.Instance.selectedCharacter = allCharacters[index];

            // Rebuild to show selection
            ChangeState(GameState.CharacterSelect);
        }

        void StartCombat()
        {
            if (selectedCharacterIndex < 0 && allCharacters != null && allCharacters.Length > 0)
            {
                // Auto-select first if none selected
                selectedCharacterIndex = 0;
                if (GameSettings.Instance != null)
                    GameSettings.Instance.selectedCharacter = allCharacters[0];
            }

            // Set enemy for story mode
            if (GameSettings.Instance != null && GameSettings.Instance.currentMode == GameSettings.GameMode.Story)
            {
                if (allChapters != null && selectedChapterIndex < allChapters.Length &&
                    allChapters[selectedChapterIndex] != null)
                {
                    var chapter = allChapters[selectedChapterIndex];
                    if (chapter.enemyCharacter != null)
                        GameSettings.Instance.enemyCharacter = chapter.enemyCharacter;

                    // Set story manager
                    if (Volk.Story.StoryManager.Instance != null)
                    {
                        Volk.Story.StoryManager.Instance.StartChapter(selectedChapterIndex);
                    }
                }
            }

            returnFromCombat = true;
            SceneManager.LoadScene("CombatTest");
        }

        // ============================================================
        // MATCH RESULT
        // ============================================================
        void BuildMatchResult()
        {
            var ui = RuntimeUIBuilder.Instance;
            var canvas = ui.CanvasRect;

            // Background
            ui.CreatePanel(canvas, Vector2.zero, Vector2.one, RuntimeUIBuilder.BG);

            // Determine result
            bool won = lastMatchWon;
            MatchStats stats = MatchStatsTracker.Instance != null ? MatchStatsTracker.Instance.Current : null;

            // VICTORY / DEFEAT title
            string resultText = won ? "ZAFER" : "YENILGI";
            Color resultColor = won ? RuntimeUIBuilder.Gold : RuntimeUIBuilder.Accent;
            var titleTMP = ui.CreateText(canvas, resultText, 72, resultColor, TextAlignmentOptions.Center);
            titleTMP.fontStyle = FontStyles.Bold;
            var titleRect = titleTMP.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.15f, 0.72f);
            titleRect.anchorMax = new Vector2(0.85f, 0.92f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            // Scale animation for title
            titleTMP.transform.localScale = Vector3.zero;
            StartCoroutine(ui.ScaleOvershoot(titleTMP.transform, 0f, 1.2f, 1f, 0.4f));

            // Stats panel
            var statsPanel = ui.CreatePanel(canvas, new Vector2(0.15f, 0.30f), new Vector2(0.85f, 0.70f), RuntimeUIBuilder.Panel);

            if (stats != null)
            {
                AddStatLine(statsPanel.transform, "Hasar", $"{stats.totalDamageDealt:F0}", 0.82f);
                AddStatLine(statsPanel.transform, "Combo", $"{stats.maxComboChain}x", 0.68f);
                AddStatLine(statsPanel.transform, "Sure", $"{stats.matchDuration:F1}s", 0.54f);
                AddStatLine(statsPanel.transform, "Derece", stats.Grade, 0.40f);

                // Coin reward
                int coinReward = stats.GetCoinReward();
                AddStatLine(statsPanel.transform, "Odul", $"+{coinReward} Coin", 0.22f);

                // XP reward
                int xpReward = stats.GetXPReward();
                AddStatLine(statsPanel.transform, "XP", $"+{xpReward}", 0.08f);
            }
            else
            {
                ui.CreateText(statsPanel.transform, "Istatistik yok", 24, RuntimeUIBuilder.Gray, TextAlignmentOptions.Center);
            }

            // XP bar animation
            float xpProgress = LevelSystem.Instance != null ? LevelSystem.Instance.XPProgress : 0f;
            ui.CreatePanel(canvas, new Vector2(0.15f, 0.24f), new Vector2(0.85f, 0.28f), new Color(0.15f, 0.15f, 0.25f));
            var xpFill = ui.CreatePanel(canvas, new Vector2(0.15f, 0.24f), new Vector2(0.15f + 0.7f * xpProgress, 0.28f),
                RuntimeUIBuilder.Gold);
            int currentLevel = LevelSystem.Instance != null ? LevelSystem.Instance.CurrentLevel : 1;
            ui.CreateText(canvas, $"Lv.{currentLevel}", 18, RuntimeUIBuilder.White, TextAlignmentOptions.MidlineLeft)
                .GetComponent<RectTransform>().anchorMin = new Vector2(0.15f, 0.19f);

            // CONTINUE button
            ui.CreateButton(canvas, "DEVAM", RuntimeUIBuilder.Accent, RuntimeUIBuilder.White,
                new Vector2(0.25f, 0.05f), new Vector2(0.75f, 0.15f),
                () => ChangeState(GameState.MainHub));

            // Fade in
            var rootCG = ui.EnsureCanvasGroup(canvas.gameObject);
            rootCG.alpha = 0;
            StartCoroutine(ui.FadeIn(rootCG, 0.3f));
        }

        void AddStatLine(Transform parent, string label, string value, float yNorm)
        {
            var ui = RuntimeUIBuilder.Instance;

            var labelTMP = ui.CreateText(parent, label, 22, RuntimeUIBuilder.Gray, TextAlignmentOptions.MidlineLeft);
            var labelRect = labelTMP.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.05f, yNorm - 0.06f);
            labelRect.anchorMax = new Vector2(0.45f, yNorm + 0.06f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var valTMP = ui.CreateText(parent, value, 24, RuntimeUIBuilder.White, TextAlignmentOptions.MidlineRight);
            valTMP.fontStyle = FontStyles.Bold;
            var valRect = valTMP.GetComponent<RectTransform>();
            valRect.anchorMin = new Vector2(0.55f, yNorm - 0.06f);
            valRect.anchorMax = new Vector2(0.95f, yNorm + 0.06f);
            valRect.offsetMin = Vector2.zero;
            valRect.offsetMax = Vector2.zero;
        }

        // ============================================================
        // SETTINGS (Basic)
        // ============================================================
        void BuildSettings()
        {
            var ui = RuntimeUIBuilder.Instance;
            var canvas = ui.CanvasRect;

            ui.CreatePanel(canvas, Vector2.zero, Vector2.one, RuntimeUIBuilder.BG);

            // Top bar
            var topBar = ui.CreatePanel(canvas, new Vector2(0, 0.90f), Vector2.one, RuntimeUIBuilder.Panel);
            ui.CreateButton(topBar.transform, "< GERI", RuntimeUIBuilder.Panel, RuntimeUIBuilder.White,
                new Vector2(0, 0), new Vector2(0.15f, 1), () => ChangeState(GameState.MainHub));
            var titleText = ui.CreateText(topBar.transform, "AYARLAR", 32, RuntimeUIBuilder.Accent,
                TextAlignmentOptions.Center);
            titleText.fontStyle = FontStyles.Bold;

            // Sound toggle
            bool soundOn = SaveManager.Instance != null ? SaveManager.Instance.Data.soundOn : true;
            ui.CreateButton(canvas, $"Ses: {(soundOn ? "ACIK" : "KAPALI")}", RuntimeUIBuilder.Panel, RuntimeUIBuilder.White,
                new Vector2(0.2f, 0.70f), new Vector2(0.8f, 0.80f),
                () => {
                    if (SaveManager.Instance != null)
                    {
                        SaveManager.Instance.Data.soundOn = !SaveManager.Instance.Data.soundOn;
                        SaveManager.Instance.Save();
                    }
                    ChangeState(GameState.Settings);
                });

            // Vibration toggle
            bool vibOn = SaveManager.Instance != null ? SaveManager.Instance.Data.vibrationOn : true;
            ui.CreateButton(canvas, $"Titresim: {(vibOn ? "ACIK" : "KAPALI")}", RuntimeUIBuilder.Panel, RuntimeUIBuilder.White,
                new Vector2(0.2f, 0.56f), new Vector2(0.8f, 0.66f),
                () => {
                    if (SaveManager.Instance != null)
                    {
                        SaveManager.Instance.Data.vibrationOn = !SaveManager.Instance.Data.vibrationOn;
                        SaveManager.Instance.Save();
                    }
                    ChangeState(GameState.Settings);
                });

            // Reset save
            ui.CreateButton(canvas, "Kaydi Sifirla", RuntimeUIBuilder.Accent, RuntimeUIBuilder.White,
                new Vector2(0.3f, 0.20f), new Vector2(0.7f, 0.30f),
                () => {
                    if (SaveManager.Instance != null)
                        SaveManager.Instance.ResetSave();
                    ChangeState(GameState.Settings);
                });
        }
    }
}
