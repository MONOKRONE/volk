using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Volk.Core;

namespace Volk.UI
{
    public class MoveListUI : MonoBehaviour
    {
        [Header("References")]
        public Transform listContainer;
        public GameObject moveItemPrefab;
        public Button closeButton;
        public GameObject panel;

        void Start()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(() => panel.SetActive(false));
            panel.SetActive(false);
        }

        public void Show()
        {
            panel.SetActive(true);
            PopulateList();
        }

        void PopulateList()
        {
            // Clear existing
            foreach (Transform child in listContainer)
                Destroy(child.gameObject);

            if (ComboTracker.Instance == null) return;

            foreach (var combo in ComboTracker.Instance.allCombos)
            {
                var item = Instantiate(moveItemPrefab, listContainer);
                var texts = item.GetComponentsInChildren<TextMeshProUGUI>();

                bool discovered = ComboTracker.Instance.IsComboDiscovered(combo.comboName);

                if (texts.Length > 0)
                    texts[0].text = discovered ? combo.comboName : "???";

                if (texts.Length > 1)
                {
                    if (discovered)
                    {
                        string seq = "";
                        foreach (var input in combo.inputSequence)
                            seq += input == AttackType.Punch ? "P " : "K ";
                        texts[1].text = $"{seq} (x{combo.damageMultiplier})";
                    }
                    else
                    {
                        texts[1].text = "Not Discovered";
                    }
                }

                // Gray out undiscovered
                var img = item.GetComponent<Image>();
                if (img != null && !discovered)
                    img.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
        }
    }
}
