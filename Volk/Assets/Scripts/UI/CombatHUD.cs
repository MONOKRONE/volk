using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Volk.Core;

namespace Volk.UI
{
    public class CombatHUD : MonoBehaviour
    {
        [Header("Player HP")]
        public Slider playerHPBar;
        public Image playerHPFill;
        public TextMeshProUGUI playerNameText;
        public RectTransform playerHPBarRect;

        [Header("Enemy HP")]
        public Slider enemyHPBar;
        public Image enemyHPFill;
        public TextMeshProUGUI enemyNameText;
        public RectTransform enemyHPBarRect;

        [Header("Skill Cooldowns")]
        public Image skill1CooldownFill;
        public Image skill2CooldownFill;
        public TextMeshProUGUI skill1Label;
        public TextMeshProUGUI skill2Label;
        public Image skill1ReadyGlow;
        public Image skill2ReadyGlow;

        [Header("Combo Display")]
        public TextMeshProUGUI comboText;
        public CanvasGroup comboGroup;
        public float comboDisplayDuration = 2f;

        [Header("Round Display")]
        public TextMeshProUGUI roundText;
        public TextMeshProUGUI timerText;

        [Header("Round Dots")]
        public Image[] playerRoundDots;
        public Image[] enemyRoundDots;

        [Header("KO Effect")]
        public CanvasGroup koOverlay;
        public TextMeshProUGUI koText;

        [Header("EX Meter")]
        public Image exMeterFill;
        public Image exMeterGlow;
        public TextMeshProUGUI exReadyText;

        [Header("Ghost Indicator")]
        public CanvasGroup ghostIndicatorGroup;
        public TextMeshProUGUI ghostIndicatorText;

        [Header("References")]
        public Fighter playerFighter;
        public Fighter enemyFighter;

        private int currentComboCount;
        private float comboTimer;
        private float targetPlayerHP = 1f;
        private float targetEnemyHP = 1f;
        private float displayPlayerHP = 1f;
        private float displayEnemyHP = 1f;
        private float displaySkill1 = 0f;
        private float displaySkill2 = 0f;
        private float displayExMeter = 0f;
        private Coroutine shakeCoroutine;
        private bool ghostMode;

        void Start()
        {
            // PLA-132: NG+ HUD hide
            if (Volk.Core.NewGamePlusManager.Instance != null && Volk.Core.NewGamePlusManager.Instance.ShouldHideHUD())
            {
                gameObject.SetActive(false);
                return;
            }

            if (playerHPFill) playerHPFill.color = VTheme.Green;
            if (enemyHPFill) enemyHPFill.color = VTheme.Red;

            if (playerNameText)
            {
                playerNameText.color = VTheme.TextPrimary;
                playerNameText.text = playerFighter?.characterData?.characterName ?? "PLAYER";
            }
            if (enemyNameText)
            {
                enemyNameText.color = VTheme.TextPrimary;
                enemyNameText.text = enemyFighter?.characterData?.characterName ?? "ENEMY";
            }

            if (comboGroup) comboGroup.alpha = 0;
            if (koOverlay) koOverlay.alpha = 0;

            // Skill labels
            if (skill1Label && playerFighter?.characterData?.skill1 != null)
                skill1Label.text = playerFighter.characterData.skill1.skillName;
            if (skill2Label && playerFighter?.characterData?.skill2 != null)
                skill2Label.text = playerFighter.characterData.skill2.skillName;

            // Ready glow initial state
            if (skill1ReadyGlow) skill1ReadyGlow.color = new Color(VTheme.Blue.r, VTheme.Blue.g, VTheme.Blue.b, 0f);
            if (skill2ReadyGlow) skill2ReadyGlow.color = new Color(VTheme.Red.r, VTheme.Red.g, VTheme.Red.b, 0f);

            // Round dots initial state
            InitRoundDots(playerRoundDots);
            InitRoundDots(enemyRoundDots);

            // EX meter initial state
            if (exMeterFill) exMeterFill.fillAmount = 0f;
            if (exMeterGlow) exMeterGlow.color = new Color(VTheme.Gold.r, VTheme.Gold.g, VTheme.Gold.b, 0f);
            if (exReadyText) { exReadyText.text = "EX"; exReadyText.color = new Color(VTheme.Gold.r, VTheme.Gold.g, VTheme.Gold.b, 0f); }

            // Ghost indicator
            ghostMode = GameSettings.Instance != null && GameSettings.Instance.currentMode == GameSettings.GameMode.Ghost;
            if (ghostIndicatorGroup)
            {
                ghostIndicatorGroup.alpha = ghostMode ? 1f : 0f;
                if (ghostIndicatorText) ghostIndicatorText.text = "REC";
                if (ghostIndicatorText) ghostIndicatorText.color = VTheme.Red;
            }
        }

        void Update()
        {
            UpdateHPBars();
            UpdateSkillCooldowns();
            UpdateExMeter();
            UpdateComboDisplay();
            if (ghostMode) PulseGhostIndicator();
        }

        void UpdateHPBars()
        {
            if (playerFighter != null)
            {
                targetPlayerHP = playerFighter.maxHP > 0 ? playerFighter.currentHP / playerFighter.maxHP : 0;
                displayPlayerHP = Mathf.Lerp(displayPlayerHP, targetPlayerHP, Time.deltaTime * 8f);
                if (playerHPBar) playerHPBar.value = displayPlayerHP;
                if (playerHPFill) playerHPFill.color = GetHPColor(displayPlayerHP);
            }

            if (enemyFighter != null)
            {
                targetEnemyHP = enemyFighter.maxHP > 0 ? enemyFighter.currentHP / enemyFighter.maxHP : 0;
                displayEnemyHP = Mathf.Lerp(displayEnemyHP, targetEnemyHP, Time.deltaTime * 8f);
                if (enemyHPBar) enemyHPBar.value = displayEnemyHP;
                if (enemyHPFill) enemyHPFill.color = GetHPColor(displayEnemyHP);
            }
        }

        Color GetHPColor(float ratio)
        {
            if (ratio > 0.5f)
                return Color.Lerp(VTheme.Gold, VTheme.Green, (ratio - 0.5f) * 2f);
            else
                return Color.Lerp(VTheme.Red, VTheme.Gold, ratio * 2f);
        }

        void UpdateSkillCooldowns()
        {
            if (playerFighter == null) return;

            // Smooth fill animation
            float target1 = playerFighter.Skill1CooldownRatio;
            float target2 = playerFighter.Skill2CooldownRatio;
            displaySkill1 = Mathf.Lerp(displaySkill1, target1, Time.deltaTime * 12f);
            displaySkill2 = Mathf.Lerp(displaySkill2, target2, Time.deltaTime * 12f);

            if (skill1CooldownFill)
            {
                skill1CooldownFill.fillAmount = displaySkill1;
                skill1CooldownFill.color = displaySkill1 > 0.01f ? VTheme.TextMuted : VTheme.Blue;
            }
            if (skill2CooldownFill)
            {
                skill2CooldownFill.fillAmount = displaySkill2;
                skill2CooldownFill.color = displaySkill2 > 0.01f ? VTheme.TextMuted : VTheme.Red;
            }

            // Ready glow pulse when skill is available
            if (skill1ReadyGlow)
            {
                float alpha1 = target1 < 0.01f ? (0.3f + Mathf.Sin(Time.time * 4f) * 0.3f) : 0f;
                skill1ReadyGlow.color = new Color(VTheme.Blue.r, VTheme.Blue.g, VTheme.Blue.b, alpha1);
            }
            if (skill2ReadyGlow)
            {
                float alpha2 = target2 < 0.01f ? (0.3f + Mathf.Sin(Time.time * 4f) * 0.3f) : 0f;
                skill2ReadyGlow.color = new Color(VTheme.Red.r, VTheme.Red.g, VTheme.Red.b, alpha2);
            }
        }

        void UpdateExMeter()
        {
            if (playerFighter == null) return;

            float target = playerFighter.ExMeterRatio;
            displayExMeter = Mathf.Lerp(displayExMeter, target, Time.deltaTime * 10f);

            if (exMeterFill)
            {
                exMeterFill.fillAmount = displayExMeter;
                exMeterFill.color = target >= 1f ? VTheme.Gold : VTheme.Blue;
            }

            // Glow pulse when full
            if (exMeterGlow)
            {
                float alpha = target >= 1f ? (0.4f + Mathf.Sin(Time.time * 5f) * 0.4f) : 0f;
                exMeterGlow.color = new Color(VTheme.Gold.r, VTheme.Gold.g, VTheme.Gold.b, alpha);
            }

            if (exReadyText)
            {
                float textAlpha = target >= 1f ? 1f : 0f;
                exReadyText.color = new Color(VTheme.Gold.r, VTheme.Gold.g, VTheme.Gold.b, textAlpha);
            }
        }

        void UpdateComboDisplay()
        {
            if (comboTimer > 0)
            {
                comboTimer -= Time.deltaTime;
                if (comboTimer <= 0 && comboGroup != null)
                {
                    comboGroup.alpha = 0;
                    currentComboCount = 0;
                }
            }
        }

        public void OnComboHit(int hitCount)
        {
            currentComboCount = hitCount;
            comboTimer = comboDisplayDuration;

            if (comboText != null && hitCount >= 2)
            {
                comboText.text = $"{hitCount} HIT COMBO!";
                comboText.color = GetComboColor(hitCount);

                if (comboGroup)
                {
                    comboGroup.alpha = 1;
                    StartCoroutine(PunchScaleText(comboText.transform));
                }
            }
        }

        public void OnHPChanged(bool isPlayer)
        {
            RectTransform bar = isPlayer ? playerHPBarRect : enemyHPBarRect;
            if (bar != null)
            {
                if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
                shakeCoroutine = StartCoroutine(ShakeBar(bar));
            }
        }

        public IEnumerator ShowKO()
        {
            if (koOverlay == null || koText == null) yield break;

            Time.timeScale = 0.2f;
            koText.text = "K.O.";
            koText.color = VTheme.Red;
            koText.transform.localScale = Vector3.one * 3f;

            float t = 0;
            while (t < 0.5f)
            {
                t += Time.unscaledDeltaTime;
                koOverlay.alpha = t / 0.5f * 0.5f;
                float scale = Mathf.Lerp(3f, 1f, Mathf.SmoothStep(0, 1, t / 0.5f));
                koText.transform.localScale = Vector3.one * scale;
                yield return null;
            }

            yield return new WaitForSecondsRealtime(1f);
            Time.timeScale = 1f;

            t = 0;
            while (t < 0.3f)
            {
                t += Time.unscaledDeltaTime;
                koOverlay.alpha = 0.5f * (1 - t / 0.3f);
                yield return null;
            }
            koOverlay.alpha = 0;
        }

        // --- Round Dots ---

        void InitRoundDots(Image[] dots)
        {
            if (dots == null) return;
            foreach (var dot in dots)
            {
                if (dot == null) continue;
                dot.color = VTheme.TextMuted;
            }
        }

        public void UpdateRoundDots(int playerWins, int enemyWins)
        {
            SetDotWins(playerRoundDots, playerWins, VTheme.Blue);
            SetDotWins(enemyRoundDots, enemyWins, VTheme.Red);
        }

        void SetDotWins(Image[] dots, int wins, Color winColor)
        {
            if (dots == null) return;
            for (int i = 0; i < dots.Length; i++)
            {
                if (dots[i] == null) continue;
                dots[i].color = i < wins ? winColor : VTheme.TextMuted;
            }
        }

        public void UpdateRound(int currentRound, int totalRounds, int playerWins, int enemyWins)
        {
            if (roundText != null)
                roundText.text = $"Round {currentRound}";
            UpdateRoundDots(playerWins, enemyWins);
        }

        public void UpdateTimer(float seconds)
        {
            if (timerText != null)
            {
                int secs = Mathf.CeilToInt(seconds);
                timerText.text = secs.ToString();
                timerText.color = secs <= 10 ? VTheme.Red : VTheme.TextPrimary;
            }
        }

        public void SetHUDVisible(bool visible)
        {
            var cg = GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
            cg.alpha = visible ? 1f : 0f;
        }

        // --- Ghost Indicator ---

        void PulseGhostIndicator()
        {
            if (ghostIndicatorGroup == null) return;
            float pulse = 0.6f + Mathf.Sin(Time.time * 3f) * 0.4f;
            ghostIndicatorGroup.alpha = pulse;
        }

        // --- Helpers ---

        IEnumerator ShakeBar(RectTransform bar)
        {
            Vector2 original = bar.anchoredPosition;
            float elapsed = 0;
            while (elapsed < 0.15f)
            {
                elapsed += Time.unscaledDeltaTime;
                float x = Random.Range(-3f, 3f);
                float y = Random.Range(-2f, 2f);
                bar.anchoredPosition = original + new Vector2(x, y);
                yield return null;
            }
            bar.anchoredPosition = original;
        }

        Color GetComboColor(int hits)
        {
            if (hits >= 5) return VTheme.Red;
            if (hits >= 4) return VTheme.Orange;
            if (hits >= 3) return VTheme.Gold;
            return Color.white;
        }

        IEnumerator PunchScaleText(Transform t)
        {
            t.localScale = Vector3.one * 1.5f;
            float elapsed = 0;
            while (elapsed < 0.2f)
            {
                elapsed += Time.unscaledDeltaTime;
                float scale = Mathf.Lerp(1.5f, 1f, elapsed / 0.2f);
                t.localScale = Vector3.one * scale;
                yield return null;
            }
            t.localScale = Vector3.one;
        }
    }
}
