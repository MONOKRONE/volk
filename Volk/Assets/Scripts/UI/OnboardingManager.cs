using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace Volk.UI
{
    public enum GhostArchetype { Aggressive, Balanced, Defensive }

    public class OnboardingManager : MonoBehaviour
    {
        public static OnboardingManager Instance;

        [Header("Panels")]
        public GameObject welcomePanel;
        public GameObject archetypePanel;
        public GameObject tutorialPanel;
        public GameObject completePanel;

        [Header("Archetype Selection")]
        public VButton aggressiveButton;
        public VButton balancedButton;
        public VButton defensiveButton;
        public TextMeshProUGUI archetypeDescText;

        [Header("Tutorial Steps")]
        public TextMeshProUGUI tutorialStepText;
        public TextMeshProUGUI tutorialInstructionText;
        public VButton skipButton;
        public VButton nextButton;
        public Slider tutorialProgress;

        private int currentStep;
        private GhostArchetype selectedArchetype;

        static readonly (string title, string instruction)[] TutorialSteps = {
            ("Hareket", "Sol joystick'i kullanarak hareket et"),
            ("Yumruk", "Yumruk butonuna dokun → hafif saldiri"),
            ("Tekme", "Tekme butonuna dokun → tekme saldirisi"),
            ("Agir Saldiri", "Butonu basili tut → agir saldiri"),
            ("Blok", "Blok butonuna dokun → savunma"),
            ("Skill", "Butona cift dokun → ozel beceri"),
        };

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            if (PlayerPrefs.GetInt("onboarding_done", 0) == 1)
            {
                gameObject.SetActive(false);
                return;
            }
            ShowWelcome();
        }

        void ShowWelcome()
        {
            SetPanel(welcomePanel);
        }

        public void OnWelcomeContinue()
        {
            SetPanel(archetypePanel);
        }

        public void SelectArchetype(int type)
        {
            selectedArchetype = (GhostArchetype)type;
            PlayerPrefs.SetInt("ghost_archetype", type);

            if (archetypeDescText)
            {
                archetypeDescText.text = selectedArchetype switch
                {
                    GhostArchetype.Aggressive => "Agresif: Hizli saldiri, az savunma. Riskli ama oldurucu.",
                    GhostArchetype.Balanced => "Dengeli: Saldiri ve savunma dengesinde. Her duruma uygun.",
                    GhostArchetype.Defensive => "Defansif: Guclu savunma, karsi atak odakli. Sabir gerektirir.",
                    _ => ""
                };
            }
        }

        public void OnArchetypeContinue()
        {
            currentStep = 0;
            SetPanel(tutorialPanel);
            ShowTutorialStep();
        }

        void ShowTutorialStep()
        {
            if (currentStep >= TutorialSteps.Length)
            {
                CompleteTutorial();
                return;
            }

            var step = TutorialSteps[currentStep];
            if (tutorialStepText) tutorialStepText.text = $"Adim {currentStep + 1}/{TutorialSteps.Length}: {step.title}";
            if (tutorialInstructionText) tutorialInstructionText.text = step.instruction;
            if (tutorialProgress) tutorialProgress.value = (float)currentStep / TutorialSteps.Length;
        }

        public void OnNextStep()
        {
            currentStep++;
            ShowTutorialStep();
        }

        public void OnSkipTutorial()
        {
            CompleteTutorial();
        }

        void CompleteTutorial()
        {
            SetPanel(completePanel);
            PlayerPrefs.SetInt("onboarding_done", 1);
            PlayerPrefs.Save();
            Debug.Log($"[Onboarding] Complete! Archetype: {selectedArchetype}");
        }

        public void OnCompleteContinue()
        {
            gameObject.SetActive(false);
        }

        void SetPanel(GameObject panel)
        {
            if (welcomePanel) welcomePanel.SetActive(panel == welcomePanel);
            if (archetypePanel) archetypePanel.SetActive(panel == archetypePanel);
            if (tutorialPanel) tutorialPanel.SetActive(panel == tutorialPanel);
            if (completePanel) completePanel.SetActive(panel == completePanel);
        }
    }
}
