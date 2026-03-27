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
        private Coroutine activeCoroutine;

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
            Debug.Log($"[GameFlow] ChangeState: {CurrentState} -> {newState}");
            PreviousState = CurrentState;
            CurrentState = newState;

            var ui = RuntimeUIBuilder.Instance;
            if (ui == null)
            {
                Debug.LogError("[GameFlow] RuntimeUIBuilder.Instance is null!");
                return;
            }
            if (activeCoroutine != null) { StopCoroutine(activeCoroutine); activeCoroutine = null; }
            ui.EnsureCanvas();
            ui.ClearUI();

            // Reset canvas alpha to 1 (previous FadeOut may have left it at 0)
            var rootCG = ui.CanvasRect != null ? ui.CanvasRect.GetComponent<CanvasGroup>() : null;
            if (rootCG != null) rootCG.alpha = 1f;

            switch (newState)
            {
                case GameState.Splash:          activeCoroutine = StartCoroutine(BuildSplash()); break;
                case GameState.MainHub:         BuildMainHub(); break;
                case GameState.StoryMap:        BuildStoryMap(); break;
                case GameState.CharacterSelect: BuildCharacterSelect(); break;
                case GameState.MatchResult:     BuildMatchResult(); break;
                case GameState.Settings:        BuildSettings(); break;
                default:
                    Debug.LogWarning($"[GameFlow] No Build method for state {newState}, falling back to MainHub");
                    BuildMainHub();
                    break;
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
            Debug.Log("[GameFlow] BuildSplash started");
            var ui = RuntimeUIBuilder.Instance;
            var canvas = ui.CanvasRect;

            // Create a splash container (so we can fade IT, not the canvas root)
            var containerGO = new GameObject("SplashContainer", typeof(RectTransform));
            containerGO.transform.SetParent(canvas, false);
            var containerRect = containerGO.GetComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;
            var splashCG = containerGO.AddComponent<CanvasGroup>();
            splashCG.alpha = 1;

            // Full black BG inside container
            ui.CreatePanel(containerGO.transform, Vector2.zero, Vector2.one, Color.black);

            // VOLK text inside container
            var titleTMP = ui.CreateText(containerGO.transform, "VOLK", 120, RuntimeUIBuilder.White,
                TextAlignmentOptions.Center);
            var titleRect = titleTMP.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.2f, 0.35f);
            titleRect.anchorMax = new Vector2(0.8f, 0.65f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            titleTMP.fontStyle = FontStyles.Bold;

            titleTMP.transform.localScale = Vector3.zero;

            // Scale animation: 0 -> 1.3 -> 1.0 in 0.5s
            yield return StartCoroutine(ui.ScaleOvershoot(titleTMP.transform, 0f, 1.3f, 1f, 0.5f));

            // Wait 2.5s
            yield return new WaitForSecondsRealtime(2.5f);

            // Fade out splash container only (NOT the canvas root)
            yield return StartCoroutine(ui.FadeOut(splashCG, 0.4f));

            Debug.Log("[GameFlow] BuildSplash -> transitioning to MainHub");
            ChangeState(GameState.MainHub);
        }

        // ============================================================
        // MAIN HUB
        // ============================================================
        void BuildMainHub()
        {
            Debug.Log("[GameFlow] BuildMainHub started");
            var ui = RuntimeUIBuilder.Instance;
            var canvas = ui.CanvasRect;

            // Background
            ui.CreatePanel(canvas, Vector2.zero, Vector2.one, RuntimeUIBuilder.BG);

            // === TOP BAR (top 10%) ===
            var topBar = ui.CreatePanel(canvas, new Vector2(0, 0.90f), Vector2.one, RuntimeUIBuilder.Panel);
            var topRect = topBar.GetComponent<RectTransform>();

            // Player name + level (left)
            int level = LevelSystem.Instance != null ? LevelSystem.Instance.CurrentLevel : 1;
            var nameText = ui.CreateText(topBar.transform, $"PLAYER  Lv.{level}", 26, RuntimeUIBuilder.White,
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
                ("STORY", RuntimeUIBuilder.Accent,
                    GetStoryProgress(),
                    () => ChangeState(GameState.StoryMap)),

                ("QUICK FIGHT", RuntimeUIBuilder.Neon, "",
                    () => {
                        if (GameSettings.Instance != null)
                            GameSettings.Instance.currentMode = GameSettings.GameMode.QuickFight;
                        ChangeState(GameState.CharacterSelect);
                    }),

                ("SURVIVAL", RuntimeUIBuilder.Purple,
                    $"Best: Round {PlayerPrefs.GetInt("survival_highscore", 0)}",
                    () => {
                        if (GameSettings.Instance != null)
                            GameSettings.Instance.currentMode = GameSettings.GameMode.Survival;
                        ChangeState(GameState.CharacterSelect);
                    }),

                ("TRAINING", RuntimeUIBuilder.Green, "",
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
            ui.CreateButton(canvas, "SETTINGS", RuntimeUIBuilder.Panel, RuntimeUIBuilder.Gray,
                new Vector2(0.38f, 0.03f), new Vector2(0.62f, 0.09f),
                () => ChangeState(GameState.Settings));

            Debug.Log("[GameFlow] BuildMainHub completed");
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
            return $"Chapter {completed}/{total}";
        }

        // ============================================================
        // STORY MAP
        // ============================================================
        void BuildStoryMap()
        {
            Debug.Log("[GameFlow] BuildStoryMap started");
            var ui = RuntimeUIBuilder.Instance;
            var canvas = ui.CanvasRect;

            // Background
            ui.CreatePanel(canvas, Vector2.zero, Vector2.one, RuntimeUIBuilder.BG);

            // Baslik
            var titleTMP = ui.CreateText(canvas, "STORY", 36, RuntimeUIBuilder.Accent, TextAlignmentOptions.Center);
            titleTMP.fontStyle = FontStyles.Bold;
            var titleRect = titleTMP.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.89f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            // Geri
            ui.CreateButton(canvas, "< BACK", RuntimeUIBuilder.Panel, RuntimeUIBuilder.White,
                new Vector2(0f, 0.89f), new Vector2(0.2f, 1f), () => ChangeState(GameState.MainHub));

            int completedChapter = SaveManager.Instance != null ? SaveManager.Instance.Data.completedChapter : 0;
            int chapterCount = allChapters != null && allChapters.Length > 0 ? allChapters.Length : 8;

            // Chapter listesi - scroll olmadan basit liste
            float itemH = 0.10f;
            float startY = 0.87f;
            float gap = 0.01f;

            for (int i = 0; i < chapterCount; i++)
            {
                float yMax = startY - i * (itemH + gap);
                float yMin = yMax - itemH;

                if (yMin < 0.02f) break; // ekrandan taşma

                bool isCompleted = i < completedChapter;
                bool isCurrent   = i == completedChapter;
                bool isLocked    = i > completedChapter;

                string chapterTitle = $"CHAPTER {i + 1}";
                if (allChapters != null && i < allChapters.Length && allChapters[i] != null
                    && !string.IsNullOrEmpty(allChapters[i].chapterTitle))
                    chapterTitle = $"CH{i+1}  {allChapters[i].chapterTitle}";

                Color bgColor = isLocked ? new Color(0.08f, 0.08f, 0.12f)
                              : isCurrent ? new Color(0.18f, 0.08f, 0.08f)
                              : RuntimeUIBuilder.Panel;
                Color textColor = isLocked ? RuntimeUIBuilder.Gray : RuntimeUIBuilder.White;

                ui.CreatePanel(canvas, new Vector2(0.02f, yMin), new Vector2(0.98f, yMax), bgColor);

                // Sol şerit (tamamlandıysa yeşil, mevcut kırmızı)
                Color stripe = isCompleted ? RuntimeUIBuilder.Green
                             : isCurrent   ? RuntimeUIBuilder.Accent
                             : new Color(0.2f, 0.2f, 0.3f);
                ui.CreatePanel(canvas, new Vector2(0.02f, yMin), new Vector2(0.04f, yMax), stripe);

                var rowText = ui.CreateText(canvas, chapterTitle, 24, textColor, TextAlignmentOptions.MidlineLeft);
                var rRect = rowText.GetComponent<RectTransform>();
                rRect.anchorMin = new Vector2(0.05f, yMin);
                rRect.anchorMax = new Vector2(0.65f, yMax);
                rRect.offsetMin = Vector2.zero;
                rRect.offsetMax = Vector2.zero;

                if (!isLocked)
                {
                    int ci = i;
                    ui.CreateButton(canvas, isCompleted ? "REPLAY" : "PLAY", RuntimeUIBuilder.Accent, Color.white,
                        new Vector2(0.70f, yMin + 0.01f), new Vector2(0.97f, yMax - 0.01f),
                        () => {
                            selectedChapterIndex = ci;
                            if (GameSettings.Instance != null)
                                GameSettings.Instance.currentMode = GameSettings.GameMode.Story;
                            ChangeState(GameState.CharacterSelect);
                        });
                }
                else
                {
                    var lockTMP = ui.CreateText(canvas, "LOCKED", 20, RuntimeUIBuilder.Gray, TextAlignmentOptions.MidlineRight);
                    var lRect2 = lockTMP.GetComponent<RectTransform>();
                    lRect2.anchorMin = new Vector2(0.70f, yMin);
                    lRect2.anchorMax = new Vector2(0.97f, yMax);
                    lRect2.offsetMin = Vector2.zero;
                    lRect2.offsetMax = Vector2.zero;
                }
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
            bg.raycastTarget = false; // Don't block child button clicks

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
                var playBtn = ui.CreateButton(rowGO.transform, "PLAY", RuntimeUIBuilder.Accent, RuntimeUIBuilder.White,
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
                var lockText = ui.CreateText(rowGO.transform, "LOCKED", 18, RuntimeUIBuilder.Gray,
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
        // Karakter rengi
        static Color GetCharColor(string name)
        {
            if (name == null) return RuntimeUIBuilder.Panel;
            switch (name.ToUpper())
            {
                case "YILDIZ": return new Color(1f, 0.55f, 0f);       // turuncu
                case "KAYA":   return new Color(0.4f, 0.4f, 0.45f);   // gri
                case "RUZGAR": return new Color(0f, 0.5f, 1f);        // mavi
                case "CELIK":  return new Color(0.7f, 0.7f, 0.8f);    // gümüş
                case "SIS":    return new Color(0.55f, 0f, 1f);       // mor
                case "TOPRAK": return new Color(0.55f, 0.27f, 0.07f); // kahve
                default:       return RuntimeUIBuilder.Panel;
            }
        }

        // Karakter emojisi
        static string GetCharEmoji(string name)
        {
            if (name == null) return "?";
            switch (name.ToUpper())
            {
                case "YILDIZ": return "Y";
                case "KAYA":   return "K";
                case "RUZGAR": return "R";
                case "CELIK":  return "C";
                case "SIS":    return "S";
                case "TOPRAK": return "T";
                default:       return "?";
            }
        }

        void BuildCharacterSelect()
        {
            Debug.Log("[GameFlow] BuildCharacterSelect started");
            var ui = RuntimeUIBuilder.Instance;
            var canvas = ui.CanvasRect;

            // Background
            ui.CreatePanel(canvas, Vector2.zero, Vector2.one, RuntimeUIBuilder.BG);

            // Title
            var titleTMP = ui.CreateText(canvas, "SELECT FIGHTER", 36, RuntimeUIBuilder.Accent, TextAlignmentOptions.Center);
            titleTMP.fontStyle = FontStyles.Bold;
            var titleRect = titleTMP.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.89f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            // Back button
            ui.CreateButton(canvas, "< BACK", RuntimeUIBuilder.Panel, RuntimeUIBuilder.White,
                new Vector2(0f, 0.89f), new Vector2(0.22f, 1f), () => ReturnToPrevious());

            // Selected character name display
            string selName = (selectedCharacterIndex >= 0 && allCharacters != null && selectedCharacterIndex < allCharacters.Length)
                ? allCharacters[selectedCharacterIndex].characterName
                : "--- SELECT A FIGHTER ---";
            var selText = ui.CreateText(canvas, selName, 26, RuntimeUIBuilder.Gold, TextAlignmentOptions.Center);
            selText.fontStyle = FontStyles.Bold;
            var selRect = selText.GetComponent<RectTransform>();
            selRect.anchorMin = new Vector2(0.22f, 0.89f);
            selRect.anchorMax = new Vector2(1f, 1f);
            selRect.offsetMin = Vector2.zero;
            selRect.offsetMax = Vector2.zero;

            // 3-column grid
            int cols = 3;
            float cellW = 1f / cols;
            float gridTop = 0.86f;
            float gridBot = 0.13f;
            float cellH = (gridTop - gridBot) / 2f;

            var chars = allCharacters;
            int total = chars != null ? chars.Length : 0;

            // Track card images for highlight update
            var cardImages = new UnityEngine.UI.Image[total];
            var cardAccents = new Color[total];
            Button fightBtn = null;

            for (int i = 0; i < total && i < cols * 2; i++)
            {
                int col = i % cols;
                int row = i / cols;
                float xMin = col * cellW;
                float xMax = xMin + cellW;
                float yMax = gridTop - row * cellH;
                float yMin = yMax - cellH;
                float pad = 0.012f;

                var charData = chars[i];
                bool unlocked = charData.unlockedByDefault;
                Color accent = GetCharColor(charData.characterName);
                string letter = GetCharEmoji(charData.characterName);
                string charName = charData.characterName ?? $"F{i+1}";
                int idx = i;
                cardAccents[i] = accent;

                Color cardBg = unlocked
                    ? new Color(accent.r * 0.25f, accent.g * 0.25f, accent.b * 0.25f, 1f)
                    : new Color(0.07f, 0.07f, 0.11f);

                var card = ui.CreatePanel(canvas,
                    new Vector2(xMin + pad, yMin + pad),
                    new Vector2(xMax - pad, yMax - pad),
                    cardBg);
                card.raycastTarget = true;
                cardImages[i] = card;

                // Colored top stripe
                ui.CreatePanel(card.transform, new Vector2(0f, 0.72f), Vector2.one,
                    unlocked ? accent : RuntimeUIBuilder.Gray);

                // Big letter
                var letterTMP = ui.CreateText(card.transform, letter, 52,
                    unlocked ? Color.white : RuntimeUIBuilder.Gray, TextAlignmentOptions.Center);
                var lRect = letterTMP.GetComponent<RectTransform>();
                lRect.anchorMin = new Vector2(0f, 0.35f);
                lRect.anchorMax = new Vector2(1f, 0.74f);
                lRect.offsetMin = Vector2.zero;
                lRect.offsetMax = Vector2.zero;
                letterTMP.fontStyle = FontStyles.Bold;

                // Name
                var nameTMP = ui.CreateText(card.transform, charName, 20,
                    unlocked ? Color.white : RuntimeUIBuilder.Gray, TextAlignmentOptions.Center);
                var nRect = nameTMP.GetComponent<RectTransform>();
                nRect.anchorMin = new Vector2(0f, 0.08f);
                nRect.anchorMax = new Vector2(1f, 0.36f);
                nRect.offsetMin = Vector2.zero;
                nRect.offsetMax = Vector2.zero;
                nameTMP.fontStyle = FontStyles.Bold;

                // Tap to select — NO rebuild, just update visuals
                if (unlocked)
                {
                    var btn = card.gameObject.AddComponent<Button>();
                    btn.transition = Selectable.Transition.None;
                    btn.onClick.AddListener(() => {
                        selectedCharacterIndex = idx;
                        if (GameSettings.Instance != null && allCharacters != null && idx < allCharacters.Length)
                            GameSettings.Instance.selectedCharacter = allCharacters[idx];
                        // Update all card highlights without rebuild
                        for (int j = 0; j < cardImages.Length; j++)
                        {
                            if (cardImages[j] == null) continue;
                            Color a = cardAccents[j];
                            cardImages[j].color = (j == idx)
                                ? new Color(a.r * 0.6f, a.g * 0.6f, a.b * 0.6f, 1f)
                                : new Color(a.r * 0.25f, a.g * 0.25f, a.b * 0.25f, 1f);
                        }
                        // Update header text
                        if (selText != null)
                            selText.text = allCharacters[idx].characterName;
                        // Update fight button
                        if (fightBtn != null)
                        {
                            fightBtn.GetComponentInChildren<TextMeshProUGUI>().text = "FIGHT!";
                            var img = fightBtn.GetComponent<Image>();
                            if (img) img.color = RuntimeUIBuilder.Accent;
                        }
                    });
                }
            }

            // FIGHT button
            fightBtn = ui.CreateButton(canvas, selectedCharacterIndex >= 0 ? "FIGHT!" : "SELECT A FIGHTER",
                selectedCharacterIndex >= 0 ? RuntimeUIBuilder.Accent : new Color(0.3f, 0.1f, 0.1f),
                Color.white,
                new Vector2(0.10f, 0.02f), new Vector2(0.90f, 0.12f),
                () => { if (selectedCharacterIndex >= 0) StartCombat(); });
        }

        void BuildCharacterCard(RectTransform content, int index, ref GameObject selectedBorder)
        {
            // Artık kullanılmıyor — BuildCharacterSelect grid kullanıyor
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
            Debug.Log("[GameFlow] BuildMatchResult started");
            var ui = RuntimeUIBuilder.Instance;
            var canvas = ui.CanvasRect;

            // Background
            ui.CreatePanel(canvas, Vector2.zero, Vector2.one, RuntimeUIBuilder.BG);

            // Determine result
            bool won = lastMatchWon;
            MatchStats stats = MatchStatsTracker.Instance != null ? MatchStatsTracker.Instance.Current : null;

            // VICTORY / DEFEAT title
            string resultText = won ? "VICTORY" : "DEFEAT";
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
                AddStatLine(statsPanel.transform, "Damage", $"{stats.totalDamageDealt:F0}", 0.82f);
                AddStatLine(statsPanel.transform, "Combo", $"{stats.maxComboChain}x", 0.68f);
                AddStatLine(statsPanel.transform, "Duration", $"{stats.matchDuration:F1}s", 0.54f);
                AddStatLine(statsPanel.transform, "Grade", stats.Grade, 0.40f);

                // Coin reward
                int coinReward = stats.GetCoinReward();
                AddStatLine(statsPanel.transform, "Reward", $"+{coinReward} Coin", 0.22f);

                // XP reward
                int xpReward = stats.GetXPReward();
                AddStatLine(statsPanel.transform, "XP", $"+{xpReward}", 0.08f);
            }
            else
            {
                ui.CreateText(statsPanel.transform, "No statistics", 24, RuntimeUIBuilder.Gray, TextAlignmentOptions.Center);
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
            ui.CreateButton(canvas, "CONTINUE", RuntimeUIBuilder.Accent, RuntimeUIBuilder.White,
                new Vector2(0.25f, 0.05f), new Vector2(0.75f, 0.15f),
                () => ChangeState(GameState.MainHub));

            Debug.Log("[GameFlow] BuildMatchResult completed");
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
            Debug.Log("[GameFlow] BuildSettings started");
            var ui = RuntimeUIBuilder.Instance;
            var canvas = ui.CanvasRect;

            ui.CreatePanel(canvas, Vector2.zero, Vector2.one, RuntimeUIBuilder.BG);

            // Top bar
            var topBar = ui.CreatePanel(canvas, new Vector2(0, 0.90f), Vector2.one, RuntimeUIBuilder.Panel);
            ui.CreateButton(topBar.transform, "< BACK", RuntimeUIBuilder.Panel, RuntimeUIBuilder.White,
                new Vector2(0, 0), new Vector2(0.15f, 1), () => ChangeState(GameState.MainHub));
            var titleText = ui.CreateText(topBar.transform, "SETTINGS", 32, RuntimeUIBuilder.Accent,
                TextAlignmentOptions.Center);
            titleText.fontStyle = FontStyles.Bold;

            // Sound toggle
            bool soundOn = SaveManager.Instance != null ? SaveManager.Instance.Data.soundOn : true;
            string soundLabel = "Sound: " + (soundOn ? "ON" : "OFF");
            ui.CreateButton(canvas, soundLabel, RuntimeUIBuilder.Panel, RuntimeUIBuilder.White,
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
            bool vibOn = VibrationManager.Instance != null ? VibrationManager.Instance.IsEnabled : true;
            string vibLabel = "Vibration: " + (vibOn ? "ON" : "OFF");
            ui.CreateButton(canvas, vibLabel, RuntimeUIBuilder.Panel, RuntimeUIBuilder.White,
                new Vector2(0.2f, 0.56f), new Vector2(0.8f, 0.66f),
                () => {
                    if (VibrationManager.Instance != null)
                        VibrationManager.Instance.SetVibration(!VibrationManager.Instance.IsEnabled);
                    ChangeState(GameState.Settings);
                });

            // Reset save
            ui.CreateButton(canvas, "RESET SAVE", RuntimeUIBuilder.Accent, RuntimeUIBuilder.White,
                new Vector2(0.3f, 0.20f), new Vector2(0.7f, 0.30f),
                () => {
                    if (SaveManager.Instance != null)
                        SaveManager.Instance.ResetSave();
                    ChangeState(GameState.Settings);
                });
        }
    }
}
