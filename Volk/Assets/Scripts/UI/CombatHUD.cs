using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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

        [Header("Combo Display")]
        public TextMeshProUGUI comboText;
        public CanvasGroup comboGroup;
        public float comboDisplayDuration = 2f;

        [Header("Round Display")]
        public TextMeshProUGUI roundText;
        public TextMeshProUGUI timerText;

        [Header("KO Effect")]
        public CanvasGroup koOverlay;
        public TextMeshProUGUI koText;

        [Header("References")]
        public Fighter playerFighter;
        public Fighter enemyFighter;

        private int currentComboCount;
        private float comboTimer;
        private float targetPlayerHP = 1f;
        private float targetEnemyHP = 1f;
        private float displayPlayerHP = 1f;
        private float displayEnemyHP = 1f;
        private Coroutine shakeCoroutine;

        void Start()
        {
            // Apply VTheme colors
            if (playerHPFill) playerHPFill.color = VTheme.Green;
            if (enemyHPFill) enemyHPFill.color = VTheme.Red;

            if (playerNameText)
            {
                playerNameText.color = VTheme.TextPrimary;
                playerNameText.text = playerFighter?.characterData?.characterName ?? "OYUNCU";
            }
            if (enemyNameText)
            {
                enemyNameText.color = VTheme.TextPrimary;
                enemyNameText.text = enemyFighter?.characterData?.characterName ?? "RAKIP";
            }

            if (comboGroup) comboGroup.alpha = 0;
            if (koOverlay) koOverlay.alpha = 0;

            // Skill labels
            if (skill1Label && playerFighter?.characterData?.skill1 != null)
                skill1Label.text = playerFighter.characterData.skill1.skillName;
            if (skill2Label && playerFighter?.characterData?.skill2 != null)
                skill2Label.text = playerFighter.characterData.skill2.skillName;
        }

        void Update()
        {
            UpdateHPBars();
            UpdateSkillCooldowns();
            UpdateComboDisplay();
        }

        void UpdateHPBars()
        {
            if (playerFighter != null)
            {
                targetPlayerHP = playerFighter.maxHP > 0 ? playerFighter.currentHP / playerFighter.maxHP : 0;
                displayPlayerHP = Mathf.Lerp(displayPlayerHP, targetPlayerHP, Time.deltaTime * 8f);
                if (playerHPBar) playerHPBar.value = displayPlayerHP;

                // Gradient color: green → yellow → red
                if (playerHPFill)
                    playerHPFill.color = GetHPColor(displayPlayerHP);
            }

            if (enemyFighter != null)
            {
                targetEnemyHP = enemyFighter.maxHP > 0 ? enemyFighter.currentHP / enemyFighter.maxHP : 0;
                displayEnemyHP = Mathf.Lerp(displayEnemyHP, targetEnemyHP, Time.deltaTime * 8f);
                if (enemyHPBar) enemyHPBar.value = displayEnemyHP;

                if (enemyHPFill)
                    enemyHPFill.color = GetHPColor(displayEnemyHP);
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

            if (skill1CooldownFill)
            {
                skill1CooldownFill.fillAmount = playerFighter.Skill1CooldownRatio;
                skill1CooldownFill.color = playerFighter.Skill1CooldownRatio > 0 ? VTheme.TextMuted : VTheme.Blue;
            }
            if (skill2CooldownFill)
            {
                skill2CooldownFill.fillAmount = playerFighter.Skill2CooldownRatio;
                skill2CooldownFill.color = playerFighter.Skill2CooldownRatio > 0 ? VTheme.TextMuted : VTheme.Red;
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
                    // Punch scale
                    StartCoroutine(PunchScaleText(comboText.transform));
                }
            }
        }

        public void OnHPChanged(bool isPlayer)
        {
            // Shake HP bar
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

            // Slow motion
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

            // Fade out
            t = 0;
            while (t < 0.3f)
            {
                t += Time.unscaledDeltaTime;
                koOverlay.alpha = 0.5f * (1 - t / 0.3f);
                yield return null;
            }
            koOverlay.alpha = 0;
        }

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
            if (hits >= 4) return new Color(1f, 0.5f, 0f); // orange
            if (hits >= 3) return VTheme.Gold;
            return Color.white;
        }

        public void UpdateRound(int currentRound, int totalRounds, int playerWins, int enemyWins)
        {
            if (roundText != null)
                roundText.text = $"Round {currentRound}/{totalRounds}  {playerWins}-{enemyWins}";
        }

        public void UpdateTimer(float seconds)
        {
            if (timerText != null)
                timerText.text = Mathf.CeilToInt(seconds).ToString();
        }

        public void SetHUDVisible(bool visible)
        {
            // For NG+ NoHUD modifier
            var cg = GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
            cg.alpha = visible ? 1f : 0f;
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
