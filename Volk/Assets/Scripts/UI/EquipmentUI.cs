using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Volk.Core;

namespace Volk.UI
{
    public class EquipmentUI : MonoBehaviour
    {
        [Header("Equipped Slots")]
        public Image glovesSlotIcon;
        public Image bootsSlotIcon;
        public Image guardSlotIcon;
        public Image headgearSlotIcon;
        public Button glovesSlotButton;
        public Button bootsSlotButton;
        public Button guardSlotButton;
        public Button headgearSlotButton;
        public Image[] slotBorders; // 4 borders for rarity coloring

        [Header("Inventory Grid")]
        public Transform inventoryGrid;
        public GameObject inventoryItemPrefab;

        [Header("Filter")]
        public Button filterAllButton;
        public Button filterGlovesButton;
        public Button filterBootsButton;
        public Button filterGuardButton;
        public Button filterHeadgearButton;

        [Header("Sort")]
        public Button sortRarityButton;
        public Button sortStatButton;

        [Header("Detail Popup")]
        public GameObject detailPopup;
        public Image detailIcon;
        public TextMeshProUGUI detailName;
        public TextMeshProUGUI detailDescription;
        public TextMeshProUGUI detailRarity;
        public TextMeshProUGUI detailStat;
        public Image detailRarityBorder;
        public Button detailEquipButton;
        public TextMeshProUGUI detailEquipLabel;
        public Button detailCloseButton;

        [Header("Upgrade Section")]
        public TextMeshProUGUI upgradeCurrentStat;
        public TextMeshProUGUI upgradeNewStat;
        public TextMeshProUGUI upgradeCostText;
        public Button upgradeButton;
        public TextMeshProUGUI upgradeLevelText;

        [Header("Background")]
        public Image backgroundImage;

        private EquipmentSlot? currentFilter;
        private bool sortByRarity = true;
        private string selectedItemId;

        void Start()
        {
            if (backgroundImage) backgroundImage.color = VTheme.Background;
            if (detailPopup) detailPopup.SetActive(false);

            // Slot buttons
            if (glovesSlotButton) glovesSlotButton.onClick.AddListener(() => OnSlotClicked(EquipmentSlot.Gloves));
            if (bootsSlotButton) bootsSlotButton.onClick.AddListener(() => OnSlotClicked(EquipmentSlot.Boots));
            if (guardSlotButton) guardSlotButton.onClick.AddListener(() => OnSlotClicked(EquipmentSlot.Guard));
            if (headgearSlotButton) headgearSlotButton.onClick.AddListener(() => OnSlotClicked(EquipmentSlot.Headgear));

            // Filter buttons
            if (filterAllButton) filterAllButton.onClick.AddListener(() => SetFilter(null));
            if (filterGlovesButton) filterGlovesButton.onClick.AddListener(() => SetFilter(EquipmentSlot.Gloves));
            if (filterBootsButton) filterBootsButton.onClick.AddListener(() => SetFilter(EquipmentSlot.Boots));
            if (filterGuardButton) filterGuardButton.onClick.AddListener(() => SetFilter(EquipmentSlot.Guard));
            if (filterHeadgearButton) filterHeadgearButton.onClick.AddListener(() => SetFilter(EquipmentSlot.Headgear));

            // Sort
            if (sortRarityButton) sortRarityButton.onClick.AddListener(() => { sortByRarity = true; RefreshInventory(); });
            if (sortStatButton) sortStatButton.onClick.AddListener(() => { sortByRarity = false; RefreshInventory(); });

            // Detail
            if (detailCloseButton) detailCloseButton.onClick.AddListener(() => detailPopup.SetActive(false));
            if (detailEquipButton) detailEquipButton.onClick.AddListener(OnEquipToggle);
            if (upgradeButton) upgradeButton.onClick.AddListener(OnUpgrade);

            RefreshSlots();
            RefreshInventory();
        }

        void SetFilter(EquipmentSlot? slot)
        {
            currentFilter = slot;
            RefreshInventory();
            UIAudio.Instance?.PlayClick();
        }

        void RefreshSlots()
        {
            if (EquipmentManager.Instance == null) return;

            UpdateSlot(EquipmentSlot.Gloves, glovesSlotIcon, 0);
            UpdateSlot(EquipmentSlot.Boots, bootsSlotIcon, 1);
            UpdateSlot(EquipmentSlot.Guard, guardSlotIcon, 2);
            UpdateSlot(EquipmentSlot.Headgear, headgearSlotIcon, 3);
        }

        void UpdateSlot(EquipmentSlot slot, Image icon, int borderIndex)
        {
            string itemId = EquipmentManager.Instance.GetEquippedInSlot(slot);
            if (itemId != null)
            {
                var data = EquipmentManager.Instance.GetEquipmentData(itemId);
                if (data != null)
                {
                    if (icon && data.icon) icon.sprite = data.icon;
                    if (slotBorders != null && borderIndex < slotBorders.Length && slotBorders[borderIndex])
                        slotBorders[borderIndex].color = data.GetRarityColor();
                }
            }
            else
            {
                if (slotBorders != null && borderIndex < slotBorders.Length && slotBorders[borderIndex])
                    slotBorders[borderIndex].color = VTheme.TextMuted;
            }
        }

        void RefreshInventory()
        {
            foreach (Transform child in inventoryGrid)
                Destroy(child.gameObject);

            if (EquipmentManager.Instance == null || inventoryItemPrefab == null) return;

            var items = GetFilteredSortedItems();

            foreach (var owned in items)
            {
                var data = EquipmentManager.Instance.GetEquipmentData(owned.itemId);
                if (data == null) continue;

                var card = Instantiate(inventoryItemPrefab, inventoryGrid);
                string capturedId = owned.itemId;

                var texts = card.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length > 0) texts[0].text = data.itemName;
                if (texts.Length > 1) texts[1].text = $"+{data.GetStatAtLevel(owned.upgradeLevel):F0}";

                var cardIcon = card.transform.Find("Icon")?.GetComponent<Image>();
                if (cardIcon && data.icon) cardIcon.sprite = data.icon;

                // Rarity border
                var border = card.transform.Find("Border")?.GetComponent<Image>();
                if (border) border.color = data.GetRarityColor();

                // Equipped indicator
                bool isEquipped = EquipmentManager.Instance.GetEquippedInSlot(data.slot) == owned.itemId;
                var equippedBadge = card.transform.Find("EquippedBadge");
                if (equippedBadge) equippedBadge.gameObject.SetActive(isEquipped);

                var btn = card.GetComponent<Button>();
                if (btn) btn.onClick.AddListener(() => ShowDetail(capturedId));
            }
        }

        List<OwnedEquipment> GetFilteredSortedItems()
        {
            var filtered = new List<OwnedEquipment>();
            foreach (var item in EquipmentManager.Instance.Inventory)
            {
                if (currentFilter.HasValue)
                {
                    var data = EquipmentManager.Instance.GetEquipmentData(item.itemId);
                    if (data == null || data.slot != currentFilter.Value) continue;
                }
                filtered.Add(item);
            }

            filtered.Sort((a, b) =>
            {
                var dataA = EquipmentManager.Instance.GetEquipmentData(a.itemId);
                var dataB = EquipmentManager.Instance.GetEquipmentData(b.itemId);
                if (dataA == null || dataB == null) return 0;

                if (sortByRarity)
                    return dataB.rarity.CompareTo(dataA.rarity);
                else
                    return dataB.GetStatAtLevel(b.upgradeLevel).CompareTo(dataA.GetStatAtLevel(a.upgradeLevel));
            });

            return filtered;
        }

        void ShowDetail(string itemId)
        {
            selectedItemId = itemId;
            var data = EquipmentManager.Instance.GetEquipmentData(itemId);
            var owned = EquipmentManager.Instance.GetOwned(itemId);
            if (data == null || owned == null) return;

            if (detailPopup) detailPopup.SetActive(true);
            if (detailIcon && data.icon) detailIcon.sprite = data.icon;
            if (detailName) { detailName.text = data.itemName; detailName.color = data.GetRarityColor(); }
            if (detailDescription) detailDescription.text = data.description;
            if (detailRarity) { detailRarity.text = data.rarity.ToString().ToUpper(); detailRarity.color = data.GetRarityColor(); }
            if (detailStat) detailStat.text = $"+{data.GetStatAtLevel(owned.upgradeLevel):F1}";
            if (detailRarityBorder) detailRarityBorder.color = data.GetRarityColor();

            bool isEquipped = EquipmentManager.Instance.GetEquippedInSlot(data.slot) == itemId;
            if (detailEquipLabel) detailEquipLabel.text = isEquipped ? "CIKAR" : "TAK";

            // Upgrade section
            if (upgradeLevelText) upgradeLevelText.text = $"Lv.{owned.upgradeLevel}/{data.maxUpgradeLevel}";
            bool canUpgrade = owned.upgradeLevel < data.maxUpgradeLevel;
            if (upgradeButton) upgradeButton.interactable = canUpgrade;

            if (canUpgrade)
            {
                if (upgradeCurrentStat) upgradeCurrentStat.text = $"{data.GetStatAtLevel(owned.upgradeLevel):F1}";
                if (upgradeNewStat)
                {
                    upgradeNewStat.text = $"{data.GetStatAtLevel(owned.upgradeLevel + 1):F1}";
                    upgradeNewStat.color = VTheme.Green;
                }
                if (upgradeCostText) upgradeCostText.text = $"{data.GetUpgradeCost(owned.upgradeLevel)} coin";
            }
            else
            {
                if (upgradeCurrentStat) upgradeCurrentStat.text = $"{data.GetStatAtLevel(owned.upgradeLevel):F1}";
                if (upgradeNewStat) { upgradeNewStat.text = "MAX"; upgradeNewStat.color = VTheme.Gold; }
                if (upgradeCostText) upgradeCostText.text = "";
            }

            UIAudio.Instance?.PlayClick();
        }

        void OnEquipToggle()
        {
            if (selectedItemId == null || EquipmentManager.Instance == null) return;
            var data = EquipmentManager.Instance.GetEquipmentData(selectedItemId);
            if (data == null) return;

            bool isEquipped = EquipmentManager.Instance.GetEquippedInSlot(data.slot) == selectedItemId;
            if (isEquipped)
                EquipmentManager.Instance.Unequip(data.slot);
            else
                EquipmentManager.Instance.Equip(selectedItemId);

            RefreshSlots();
            RefreshInventory();
            ShowDetail(selectedItemId); // refresh popup
        }

        void OnUpgrade()
        {
            if (selectedItemId == null || EquipmentManager.Instance == null) return;
            if (EquipmentManager.Instance.Upgrade(selectedItemId))
            {
                RefreshSlots();
                RefreshInventory();
                ShowDetail(selectedItemId);
                UIAudio.Instance?.PlayCoin();
            }
            else
            {
                UIAudio.Instance?.PlayError();
            }
        }

        void OnSlotClicked(EquipmentSlot slot)
        {
            string equipped = EquipmentManager.Instance?.GetEquippedInSlot(slot);
            if (equipped != null)
                ShowDetail(equipped);
            else
                SetFilter(slot);
        }
    }
}
