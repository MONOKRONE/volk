using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Volk.Core;

namespace Volk.UI
{
    public class QuickFightUI : MonoBehaviour
    {
        [Header("Character Data")]
        public CharacterData[] allCharacters;
        public ArenaData[] allArenas;

        [Header("Player Selection")]
        public TextMeshProUGUI playerNameText;
        public Image playerPortrait;
        public Button playerPrevButton;
        public Button playerNextButton;

        [Header("Enemy Selection")]
        public TextMeshProUGUI enemyNameText;
        public Image enemyPortrait;
        public Button enemyPrevButton;
        public Button enemyNextButton;

        [Header("Arena Selection")]
        public TextMeshProUGUI arenaNameText;
        public Image arenaThumbnail;
        public Button arenaPrevButton;
        public Button arenaNextButton;

        [Header("Difficulty")]
        public Button[] difficultyButtons; // Easy, Normal, Hard
        public Color activeColor = new Color(0.91f, 0.27f, 0.38f);
        public Color inactiveColor = new Color(0.1f, 0.1f, 0.18f);

        [Header("Actions")]
        public Button fightButton;
        public Button backButton;
        public CanvasGroup canvasGroup;

        [Header("Scenes")]
        public string combatScene = "CombatTest";

        private int playerIndex;
        private int enemyIndex;
        private int arenaIndex;
        private AIDifficulty selectedDifficulty = AIDifficulty.Normal;

        void Awake()
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }

        void Start()
        {
            if (GameSettings.Instance == null)
            {
                var go = new GameObject("GameSettings");
                go.AddComponent<GameSettings>();
            }

            // Button listeners
            if (playerPrevButton) playerPrevButton.onClick.AddListener(() => ChangePlayer(-1));
            if (playerNextButton) playerNextButton.onClick.AddListener(() => ChangePlayer(1));
            if (enemyPrevButton) enemyPrevButton.onClick.AddListener(() => ChangeEnemy(-1));
            if (enemyNextButton) enemyNextButton.onClick.AddListener(() => ChangeEnemy(1));
            if (arenaPrevButton) arenaPrevButton.onClick.AddListener(() => ChangeArena(-1));
            if (arenaNextButton) arenaNextButton.onClick.AddListener(() => ChangeArena(1));
            if (fightButton) fightButton.onClick.AddListener(StartFight);
            if (backButton) backButton.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));

            // Difficulty buttons
            if (difficultyButtons != null)
            {
                for (int i = 0; i < difficultyButtons.Length && i < 3; i++)
                {
                    int level = i;
                    if (difficultyButtons[i] != null)
                        difficultyButtons[i].onClick.AddListener(() => SetDifficulty(level));
                }
            }

            // Default: first unlocked characters
            playerIndex = 0;
            enemyIndex = Mathf.Min(1, allCharacters.Length - 1);
            arenaIndex = 0;

            UpdatePlayerDisplay();
            UpdateEnemyDisplay();
            UpdateArenaDisplay();
            SetDifficulty(1); // Normal

            StartCoroutine(FadeIn());
        }

        void ChangePlayer(int dir)
        {
            playerIndex = (playerIndex + dir + allCharacters.Length) % allCharacters.Length;
            UpdatePlayerDisplay();
        }

        void ChangeEnemy(int dir)
        {
            enemyIndex = (enemyIndex + dir + allCharacters.Length) % allCharacters.Length;
            UpdateEnemyDisplay();
        }

        void ChangeArena(int dir)
        {
            if (allArenas == null || allArenas.Length == 0) return;
            arenaIndex = (arenaIndex + dir + allArenas.Length) % allArenas.Length;
            UpdateArenaDisplay();
        }

        void UpdatePlayerDisplay()
        {
            if (allCharacters.Length == 0) return;
            var data = allCharacters[playerIndex];
            if (playerNameText) playerNameText.text = data.characterName;
            if (playerPortrait && data.portrait) playerPortrait.sprite = data.portrait;
        }

        void UpdateEnemyDisplay()
        {
            if (allCharacters.Length == 0) return;
            var data = allCharacters[enemyIndex];
            if (enemyNameText) enemyNameText.text = data.characterName;
            if (enemyPortrait && data.portrait) enemyPortrait.sprite = data.portrait;
        }

        void UpdateArenaDisplay()
        {
            if (allArenas == null || allArenas.Length == 0) return;
            var data = allArenas[arenaIndex];
            if (arenaNameText) arenaNameText.text = data.arenaName;
            if (arenaThumbnail && data.thumbnail) arenaThumbnail.sprite = data.thumbnail;
        }

        void SetDifficulty(int level)
        {
            selectedDifficulty = (AIDifficulty)level;
            if (difficultyButtons == null) return;
            for (int i = 0; i < difficultyButtons.Length && i < 3; i++)
            {
                if (difficultyButtons[i] == null) continue;
                var img = difficultyButtons[i].GetComponent<Image>();
                if (img) img.color = i == level ? activeColor : inactiveColor;
            }
        }

        void StartFight()
        {
            GameSettings.Instance.selectedCharacter = allCharacters[playerIndex];
            GameSettings.Instance.enemyCharacter = allCharacters[enemyIndex];
            GameSettings.Instance.selectedArena = (allArenas != null && allArenas.Length > 0) ? allArenas[arenaIndex] : null;
            GameSettings.Instance.selectedDifficulty = selectedDifficulty;
            GameSettings.Instance.currentMode = GameSettings.GameMode.QuickFight;

            StartCoroutine(FadeOutAndLoad());
        }

        IEnumerator FadeOutAndLoad()
        {
            if (canvasGroup != null)
            {
                float t = 0;
                while (t < 0.3f)
                {
                    t += Time.deltaTime;
                    canvasGroup.alpha = 1 - (t / 0.3f);
                    yield return null;
                }
            }
            SceneManager.LoadScene(combatScene);
        }

        IEnumerator FadeIn()
        {
            if (canvasGroup == null) yield break;
            canvasGroup.alpha = 0;
            float t = 0;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = t / 0.4f;
                yield return null;
            }
            canvasGroup.alpha = 1;
        }
    }
}
