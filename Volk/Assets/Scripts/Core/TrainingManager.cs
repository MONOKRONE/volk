using UnityEngine;
using UnityEngine.SceneManagement;

namespace Volk.Core
{
    public class TrainingManager : MonoBehaviour
    {
        public static TrainingManager Instance { get; private set; }

        [Header("References")]
        public Fighter playerFighter;
        public Fighter enemyFighter;

        [Header("Settings")]
        public bool aiActive = false;
        public bool infiniteHP = true;
        public bool showHitData = true;

        // Stats for current session
        public int TotalHits { get; private set; }
        public int CombosLanded { get; private set; }
        public float TotalDamageDealt { get; private set; }
        public float DPS { get; private set; }

        private float sessionTimer;
        private float damageWindow;
        private float damageInWindow;
        private const float DPS_WINDOW = 3f;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            if (GameSettings.Instance == null || GameSettings.Instance.currentMode != GameSettings.GameMode.Training)
                return;

            playerFighter = GameObject.Find("Player_Root")?.GetComponent<Fighter>();
            enemyFighter = GameObject.Find("Enemy_Root")?.GetComponent<Fighter>();

            SetupTraining();
            BuildTrainingHUD();
        }

        void SetupTraining()
        {
            // Enemy AI passive by default
            if (enemyFighter != null)
            {
                enemyFighter.isAI = aiActive;
                if (aiActive)
                {
                    enemyFighter.difficulty = AIDifficulty.Easy;
                    enemyFighter.InitAIDifficulty();
                }
            }
        }

        void Update()
        {
            if (GameSettings.Instance == null || GameSettings.Instance.currentMode != GameSettings.GameMode.Training)
                return;

            sessionTimer += Time.deltaTime;

            // Infinite HP
            if (infiniteHP)
            {
                if (playerFighter != null)
                    playerFighter.currentHP = playerFighter.maxHP;
                if (enemyFighter != null && !enemyFighter.isDead)
                    enemyFighter.currentHP = enemyFighter.maxHP;
            }

            // Reset dead enemy
            if (enemyFighter != null && enemyFighter.isDead)
            {
                enemyFighter.ResetForRound();
                if (aiActive)
                {
                    enemyFighter.isAI = true;
                    enemyFighter.InitAIDifficulty();
                }
            }

            // DPS calculation
            damageWindow += Time.deltaTime;
            if (damageWindow >= DPS_WINDOW)
            {
                DPS = damageInWindow / DPS_WINDOW;
                damageInWindow = 0;
                damageWindow = 0;
            }
        }

        public void OnEnemyDefeated()
        {
            if (enemyFighter != null)
            {
                enemyFighter.ResetForRound();
                enemyFighter.isAI = aiActive;
                if (aiActive) enemyFighter.InitAIDifficulty();
            }
            if (GameManager.Instance != null) GameManager.Instance.roundActive = true;
        }

        void BuildTrainingHUD()
        {
            var ui = Volk.UI.RuntimeUIBuilder.Instance;
            if (ui == null) return;
            ui.ShowCanvas();
            ui.EnsureCanvas();

            var canvas = ui.CanvasRect;

            // Top-left: mode label
            var modeLbl = ui.CreateText(canvas, "TRAINING MODE", 22, Volk.UI.RuntimeUIBuilder.Neon, TMPro.TextAlignmentOptions.MidlineLeft);
            var mlRect = modeLbl.GetComponent<RectTransform>();
            mlRect.anchorMin = new Vector2(0.02f, 0.90f);
            mlRect.anchorMax = new Vector2(0.4f, 1f);
            mlRect.offsetMin = mlRect.offsetMax = Vector2.zero;

            // Bottom-right: EXIT button
            ui.CreateButton(canvas, "EXIT", Volk.UI.RuntimeUIBuilder.Panel, Volk.UI.RuntimeUIBuilder.White,
                new Vector2(0.78f, 0.02f), new Vector2(0.98f, 0.10f),
                () => {
                    ui.HideCanvas();
                    SceneManager.LoadScene("MainMenu");
                });

            // Bottom-left: AI TOGGLE button
            ui.CreateButton(canvas, aiActive ? "AI: ON" : "AI: OFF", Volk.UI.RuntimeUIBuilder.Panel, Volk.UI.RuntimeUIBuilder.White,
                new Vector2(0.02f, 0.02f), new Vector2(0.22f, 0.10f),
                () => ToggleAI());
        }

        public void OnHitLanded(float damage)
        {
            TotalHits++;
            TotalDamageDealt += damage;
            damageInWindow += damage;
        }

        public void OnComboLanded()
        {
            CombosLanded++;
        }

        public void ToggleAI()
        {
            aiActive = !aiActive;
            if (enemyFighter != null)
            {
                enemyFighter.isAI = aiActive;
                if (aiActive)
                {
                    enemyFighter.difficulty = AIDifficulty.Normal;
                    enemyFighter.InitAIDifficulty();
                }
            }
        }

        public void ResetStats()
        {
            TotalHits = 0;
            CombosLanded = 0;
            TotalDamageDealt = 0;
            DPS = 0;
            sessionTimer = 0;
        }

        public string GetSessionTime()
        {
            int min = (int)(sessionTimer / 60);
            int sec = (int)(sessionTimer % 60);
            return $"{min:D2}:{sec:D2}";
        }
    }
}
