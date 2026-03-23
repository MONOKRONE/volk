using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Volk.Meta;

namespace Volk.UI
{
    public class DailyQuestUI : MonoBehaviour
    {
        [Header("References")]
        public GameObject panel;
        public Transform questListContainer;
        public GameObject questItemPrefab;
        public Button openButton;
        public Button closeButton;
        public GameObject badgeIcon;
        public TextMeshProUGUI badgeText;

        void Start()
        {
            if (openButton != null)
                openButton.onClick.AddListener(() => { panel.SetActive(true); PopulateQuests(); });
            if (closeButton != null)
                closeButton.onClick.AddListener(() => panel.SetActive(false));

            panel.SetActive(false);
            UpdateBadge();

            if (DailyQuestManager.Instance != null)
                DailyQuestManager.Instance.OnQuestsUpdated += UpdateBadge;
        }

        void OnDestroy()
        {
            if (DailyQuestManager.Instance != null)
                DailyQuestManager.Instance.OnQuestsUpdated -= UpdateBadge;
        }

        void UpdateBadge()
        {
            if (DailyQuestManager.Instance == null) return;
            int unclaimed = DailyQuestManager.Instance.UnclaimedCount();
            if (badgeIcon != null) badgeIcon.SetActive(unclaimed > 0);
            if (badgeText != null) badgeText.text = unclaimed.ToString();
        }

        void PopulateQuests()
        {
            foreach (Transform child in questListContainer)
                Destroy(child.gameObject);

            if (DailyQuestManager.Instance == null) return;

            var state = DailyQuestManager.Instance.State;
            if (state == null) return;

            for (int i = 0; i < state.quests.Count; i++)
            {
                var quest = state.quests[i];
                int index = i;
                var item = Instantiate(questItemPrefab, questListContainer);

                var texts = item.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length > 0) texts[0].text = quest.questName;
                if (texts.Length > 1) texts[1].text = $"{quest.currentProgress}/{quest.targetCount}";
                if (texts.Length > 2) texts[2].text = $"{quest.coinReward} coin";

                // Progress bar
                var slider = item.GetComponentInChildren<Slider>();
                if (slider != null)
                    slider.value = (float)quest.currentProgress / quest.targetCount;

                // Claim button
                var btn = item.GetComponentInChildren<Button>();
                if (btn != null)
                {
                    if (quest.completed && !quest.claimed)
                    {
                        btn.interactable = true;
                        btn.onClick.AddListener(() =>
                        {
                            DailyQuestManager.Instance.ClaimReward(index);
                            PopulateQuests();
                        });
                    }
                    else
                    {
                        btn.interactable = false;
                    }
                }
            }
        }
    }
}
