using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Volk.Core;

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

        void Start()
        {
            var bg = GetComponent<Image>();
            if (bg != null) bg.color = new Color(VTheme.Panel.r, VTheme.Panel.g, VTheme.Panel.b, 0.95f);

            if (xpBarFill != null) xpBarFill.color = VTheme.Blue;
        }

        void Update()
        {
            if (LevelSystem.Instance != null)
            {
                if (levelText) levelText.text = $"Lv.{LevelSystem.Instance.CurrentLevel}";
                if (titleText) titleText.text = LevelSystem.Instance.GetLevelTitle();
                if (xpBar) xpBar.value = LevelSystem.Instance.XPProgress;
            }

            if (SaveManager.Instance != null)
            {
                if (coinText) coinText.text = $"{SaveManager.Instance.Data.currency}";
            }
        }
    }
}
