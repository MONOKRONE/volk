using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Volk.Core;

namespace Volk.UI
{
    public class TrainingHUD : MonoBehaviour
    {
        [Header("Stats Panel")]
        public TextMeshProUGUI hitsText;
        public TextMeshProUGUI combosText;
        public TextMeshProUGUI damageText;
        public TextMeshProUGUI dpsText;
        public TextMeshProUGUI timerText;

        [Header("Controls")]
        public Button toggleAIButton;
        public TextMeshProUGUI aiStatusText;
        public Button resetButton;
        public Button moveListButton;
        public Button exitButton;

        [Header("Move List")]
        public MoveListUI moveListUI;

        void Start()
        {
            if (toggleAIButton) toggleAIButton.onClick.AddListener(OnToggleAI);
            if (resetButton) resetButton.onClick.AddListener(OnReset);
            if (moveListButton && moveListUI != null) moveListButton.onClick.AddListener(() => moveListUI.Show());
            if (exitButton) exitButton.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));

            UpdateAIStatus();
        }

        void Update()
        {
            if (TrainingManager.Instance == null) return;

            if (hitsText) hitsText.text = $"HITS: {TrainingManager.Instance.TotalHits}";
            if (combosText) combosText.text = $"COMBOS: {TrainingManager.Instance.CombosLanded}";
            if (damageText) damageText.text = $"DAMAGE: {TrainingManager.Instance.TotalDamageDealt:F0}";
            if (dpsText) dpsText.text = $"DPS: {TrainingManager.Instance.DPS:F1}";
            if (timerText) timerText.text = TrainingManager.Instance.GetSessionTime();
        }

        void OnToggleAI()
        {
            TrainingManager.Instance?.ToggleAI();
            UpdateAIStatus();
        }

        void OnReset()
        {
            TrainingManager.Instance?.ResetStats();
        }

        void UpdateAIStatus()
        {
            if (aiStatusText == null || TrainingManager.Instance == null) return;
            aiStatusText.text = TrainingManager.Instance.aiActive ? "AI: ON" : "AI: OFF";
        }
    }
}
