using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Volk.Core;
using Volk.Meta;

namespace Volk.UI
{
    public class ShopUI : MonoBehaviour
    {
        [Header("References")]
        public Transform itemGrid;
        public GameObject itemCardPrefab;
        public TextMeshProUGUI currencyText;
        public Button backButton;
        public CanvasGroup canvasGroup;

        [Header("Confirm Popup")]
        public GameObject confirmPopup;
        public TextMeshProUGUI confirmText;
        public Button confirmYes;
        public Button confirmNo;

        private ShopItemData pendingItem;

        void Awake()
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }

        void Start()
        {
            if (backButton != null)
                backButton.onClick.AddListener(() =>
                    UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu"));

            if (confirmNo != null)
                confirmNo.onClick.AddListener(() => confirmPopup.SetActive(false));

            if (confirmYes != null)
                confirmYes.onClick.AddListener(OnConfirmPurchase);

            if (confirmPopup != null)
                confirmPopup.SetActive(false);

            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnItemPurchased += OnPurchased;
                ShopManager.Instance.OnPurchaseFailed += OnFailed;
            }

            PopulateShop();
            UpdateCurrency();
        }

        void OnDestroy()
        {
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnItemPurchased -= OnPurchased;
                ShopManager.Instance.OnPurchaseFailed -= OnFailed;
            }
        }

        void PopulateShop()
        {
            if (ShopManager.Instance == null || itemCardPrefab == null || itemGrid == null) return;

            foreach (var item in ShopManager.Instance.shopItems)
            {
                var card = Instantiate(itemCardPrefab, itemGrid);
                var capturedItem = item;

                // Name
                var texts = card.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length > 0) texts[0].text = item.itemName;
                if (texts.Length > 1) texts[1].text = $"{item.price} coin";

                // Icon
                var icon = card.transform.Find("Icon")?.GetComponent<Image>();
                if (icon != null && item.icon != null)
                    icon.sprite = item.icon;

                // Owned badge
                bool owned = ShopManager.Instance.IsOwned(item);
                var ownedBadge = card.transform.Find("OwnedBadge");
                if (ownedBadge != null)
                    ownedBadge.gameObject.SetActive(owned);

                // Button
                var btn = card.GetComponent<Button>();
                if (btn != null)
                {
                    btn.interactable = !owned;
                    btn.onClick.AddListener(() => ShowConfirm(capturedItem));
                }
            }
        }

        void ShowConfirm(ShopItemData item)
        {
            pendingItem = item;
            if (confirmPopup == null) { ShopManager.Instance.TryPurchase(item); return; }

            confirmPopup.SetActive(true);
            if (confirmText != null)
                confirmText.text = $"{item.itemName}\n{item.price} coin\nSatin al?";
        }

        void OnConfirmPurchase()
        {
            if (pendingItem != null)
            {
                ShopManager.Instance.TryPurchase(pendingItem);
                pendingItem = null;
            }
            confirmPopup.SetActive(false);
        }

        void OnPurchased(ShopItemData item)
        {
            // Rebuild shop to reflect ownership
            foreach (Transform child in itemGrid)
                Destroy(child.gameObject);
            PopulateShop();
            UpdateCurrency();
        }

        void OnFailed(string reason)
        {
            Debug.Log($"[Shop] Purchase failed: {reason}");
        }

        void UpdateCurrency()
        {
            if (currencyText != null && ShopManager.Instance != null)
                currencyText.text = $"{ShopManager.Instance.GetPlayerCurrency()} coin";
        }
    }
}
