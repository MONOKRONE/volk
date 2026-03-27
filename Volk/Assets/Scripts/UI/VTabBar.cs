using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;

namespace Volk.UI
{
    public class VTabBar : MonoBehaviour
    {
        [Header("Tab Buttons")]
        public Button storyTab;
        public Button quickFightTab;
        public Button onlineTab;
        public Button ghostTab;
        public Button shopTab;
        public Button profileTab;

        [Header("Tab Icons")]
        public Image[] tabIcons;

        [Header("Tab Labels")]
        public TextMeshProUGUI[] tabLabels;

        [Header("Scenes")]
        public string storyScene = "StoryMenu";
        public string quickFightScene = "QuickFight";
        public string onlineScene = "MainMenu";
        public string ghostScene = "MainMenu";
        public string shopScene = "Shop";
        public string profileScene = "MainMenu";

        public event Action<int> OnTabChanged;

        private int activeTab;

        void Start()
        {
            var bg = GetComponent<Image>();
            if (bg != null) bg.color = new Color(VTheme.Panel.r, VTheme.Panel.g, VTheme.Panel.b, 0.98f);

            if (storyTab) storyTab.onClick.AddListener(() => SwitchTab(0, storyScene));
            if (quickFightTab) quickFightTab.onClick.AddListener(() => SwitchTab(1, quickFightScene));
            if (onlineTab) onlineTab.onClick.AddListener(() => SwitchTab(2, onlineScene));
            if (ghostTab) ghostTab.onClick.AddListener(() => SwitchTab(3, ghostScene));
            if (shopTab) shopTab.onClick.AddListener(() => SwitchTab(4, shopScene));
            if (profileTab) profileTab.onClick.AddListener(() => SwitchTab(5, profileScene));

            UpdateTabVisuals(0);
        }

        public void SetActiveTab(int index)
        {
            activeTab = index;
            UpdateTabVisuals(index);
        }

        void SwitchTab(int index, string scene)
        {
            if (index == activeTab) return;
            activeTab = index;
            UpdateTabVisuals(index);
            UIAudio.Instance?.PlayClick();
            OnTabChanged?.Invoke(index);
            SceneManager.LoadScene(scene);
        }

        void UpdateTabVisuals(int activeIndex)
        {
            if (tabIcons != null)
            {
                for (int i = 0; i < tabIcons.Length; i++)
                {
                    if (tabIcons[i] == null) continue;
                    tabIcons[i].color = i == activeIndex ? VTheme.Red : VTheme.TextMuted;
                }
            }
            if (tabLabels != null)
            {
                for (int i = 0; i < tabLabels.Length; i++)
                {
                    if (tabLabels[i] == null) continue;
                    tabLabels[i].color = i == activeIndex ? VTheme.Red : VTheme.TextMuted;
                }
            }
        }
    }
}
