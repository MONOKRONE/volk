using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Volk.Core;
using System;

namespace Volk.UI
{
    public class VTopBar : MonoBehaviour
    {
        [Header("Elements")]
        public Image avatarImage;
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI titleText;
        public Slider xpBar;
        public Image xpBarFill;
        public TextMeshProUGUI coinText;
        public TextMeshProUGUI gemText;

        // Cached delegates for proper unsubscription
        private Action<int> onLevelUp;
        private Action<int> onXPGained;
        private Action<int, int> onCoinsChanged;
        private Action<int, int> onGemsChanged;

        void Start()
        {
            var bg = GetComponent<Image>();
            if (bg != null) bg.color = new Color(VTheme.Panel.r, VTheme.Panel.g, VTheme.Panel.b, 0.95f);
            if (xpBarFill != null) xpBarFill.color = VTheme.Blue;

            onLevelUp = _ => RefreshLevel();
            onXPGained = _ => RefreshLevel();
            onCoinsChanged = (_, __) => RefreshCurrency();
            onGemsChanged = (_, __) => RefreshCurrency();

            if (LevelSystem.Instance != null)
            {
                LevelSystem.Instance.OnLevelUp += onLevelUp;
                LevelSystem.Instance.OnXPGained += onXPGained;
            }

            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnCoinsChanged += onCoinsChanged;
                CurrencyManager.Instance.OnGemsChanged += onGemsChanged;
            }

            RefreshLevel();
            RefreshCurrency();
        }

        void OnDestroy()
        {
            if (LevelSystem.Instance != null)
            {
                LevelSystem.Instance.OnLevelUp -= onLevelUp;
                LevelSystem.Instance.OnXPGained -= onXPGained;
            }
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnCoinsChanged -= onCoinsChanged;
                CurrencyManager.Instance.OnGemsChanged -= onGemsChanged;
            }
        }

        void RefreshLevel()
        {
            if (LevelSystem.Instance == null) return;
            if (levelText) levelText.text = $"Lv.{LevelSystem.Instance.CurrentLevel}";
            if (titleText) titleText.text = LevelSystem.Instance.GetLevelTitle();
            if (xpBar) xpBar.value = LevelSystem.Instance.XPProgress;
        }

        void RefreshCurrency()
        {
            if (CurrencyManager.Instance != null)
            {
                if (coinText) coinText.text = CurrencyManager.Instance.Coins.ToString();
                if (gemText) gemText.text = CurrencyManager.Instance.Gems.ToString();
            }
            else if (SaveManager.Instance != null)
            {
                if (coinText) coinText.text = SaveManager.Instance.Data.currency.ToString();
            }
        }
    }
}
