using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Volk.Core;

namespace Volk.UI
{
    public class LootBoxUI : MonoBehaviour
    {
        [Header("Box Display")]
        public GameObject panel;
        public CanvasGroup panelGroup;
        public RectTransform boxImage;
        public Image boxGlow;
        public TextMeshProUGUI tierLabel;

        [Header("Result Display")]
        public GameObject resultPanel;
        public Image itemIcon;
        public TextMeshProUGUI itemName;
        public TextMeshProUGUI rarityText;
        public TextMeshProUGUI newLabel;
        public TextMeshProUGUI duplicateCoinsText;
        public Image rarityBorder;

        [Header("Buttons")]
        public Button openButton;
        public Button closeButton;
        public Button skipButton;

        [Header("Colors")]
        public Color bronzeColor = new Color(0.8f, 0.5f, 0.2f);
        public Color silverColor = new Color(0.75f, 0.75f, 0.8f);
        public Color goldColor = new Color(1f, 0.84f, 0f);

        private LootBoxTier pendingTier;
        private bool isAnimating;

        void Start()
        {
            if (panel) panel.SetActive(false);
            if (resultPanel) resultPanel.SetActive(false);
            if (openButton) openButton.onClick.AddListener(OpenBox);
            if (closeButton) closeButton.onClick.AddListener(Close);
            if (skipButton) skipButton.onClick.AddListener(SkipAnimation);
        }

        public void ShowBox(LootBoxTier tier)
        {
            pendingTier = tier;
            if (panel) panel.SetActive(true);
            if (resultPanel) resultPanel.SetActive(false);

            if (tierLabel)
            {
                tierLabel.text = tier switch
                {
                    LootBoxTier.Bronze => "BRONZ KUTU",
                    LootBoxTier.Silver => "GUMUS KUTU",
                    LootBoxTier.Gold => "ALTIN KUTU",
                    _ => "KUTU"
                };
                tierLabel.color = GetTierColor(tier);
            }

            if (boxGlow) boxGlow.color = GetTierColor(tier);

            StartCoroutine(FadeIn());
        }

        void OpenBox()
        {
            if (isAnimating) return;
            StartCoroutine(OpenAnimation());
        }

        IEnumerator OpenAnimation()
        {
            isAnimating = true;
            if (openButton) openButton.gameObject.SetActive(false);

            // Shake box
            if (boxImage != null)
            {
                Vector2 original = boxImage.anchoredPosition;
                for (int i = 0; i < 15; i++)
                {
                    float intensity = Mathf.Lerp(2f, 10f, i / 15f);
                    boxImage.anchoredPosition = original + new Vector2(
                        UnityEngine.Random.Range(-intensity, intensity),
                        UnityEngine.Random.Range(-intensity * 0.5f, intensity * 0.5f));
                    yield return new WaitForSecondsRealtime(0.05f);
                }
                boxImage.anchoredPosition = original;
            }

            // Flash
            if (boxGlow)
            {
                boxGlow.color = Color.white;
                yield return new WaitForSecondsRealtime(0.2f);
            }

            // Open the box
            var result = LootBoxManager.Instance?.OpenBox(pendingTier);
            if (result == null) { Close(); yield break; }

            // Show result
            ShowResult(result);
            isAnimating = false;
        }

        void ShowResult(LootBoxResult result)
        {
            if (resultPanel) resultPanel.SetActive(true);
            if (boxImage) boxImage.gameObject.SetActive(false);

            var data = EquipmentManager.Instance?.GetEquipmentData(result.itemId);

            if (itemName)
            {
                itemName.text = data?.itemName ?? result.itemId;
                itemName.color = data?.GetRarityColor() ?? Color.white;
            }

            if (rarityText)
            {
                rarityText.text = result.rarity.ToString().ToUpper();
                rarityText.color = data?.GetRarityColor() ?? Color.white;
            }

            if (itemIcon && data?.icon != null)
                itemIcon.sprite = data.icon;

            if (rarityBorder)
                rarityBorder.color = data?.GetRarityColor() ?? Color.white;

            if (newLabel) newLabel.gameObject.SetActive(result.isNew);
            if (duplicateCoinsText)
            {
                duplicateCoinsText.gameObject.SetActive(!result.isNew);
                if (!result.isNew)
                    duplicateCoinsText.text = $"+{GetDuplicateCoins(result.rarity)} coin";
            }

            UIAudio.Instance?.PlayCoin();
        }

        int GetDuplicateCoins(EquipmentRarity rarity)
        {
            return rarity switch
            {
                EquipmentRarity.Common => 20,
                EquipmentRarity.Rare => 50,
                EquipmentRarity.Epic => 100,
                EquipmentRarity.Legendary => 250,
                _ => 20
            };
        }

        void SkipAnimation()
        {
            if (!isAnimating) return;
            StopAllCoroutines();
            var result = LootBoxManager.Instance?.OpenBox(pendingTier);
            if (result != null) ShowResult(result);
            isAnimating = false;
        }

        void Close()
        {
            if (panel) panel.SetActive(false);
            if (resultPanel) resultPanel.SetActive(false);
            if (boxImage) boxImage.gameObject.SetActive(true);
            if (openButton) openButton.gameObject.SetActive(true);
        }

        IEnumerator FadeIn()
        {
            if (panelGroup == null) yield break;
            panelGroup.alpha = 0;
            float t = 0;
            while (t < 0.3f)
            {
                t += Time.unscaledDeltaTime;
                panelGroup.alpha = t / 0.3f;
                yield return null;
            }
            panelGroup.alpha = 1;
        }

        Color GetTierColor(LootBoxTier tier)
        {
            return tier switch
            {
                LootBoxTier.Bronze => bronzeColor,
                LootBoxTier.Silver => silverColor,
                LootBoxTier.Gold => goldColor,
                _ => Color.white
            };
        }
    }
}
