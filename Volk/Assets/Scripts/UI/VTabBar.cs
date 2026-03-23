using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace Volk.UI
{
    public class VTabBar : MonoBehaviour
    {
        [Header("Tab Buttons")]
        public Button homeTab;
        public Button storyTab;
        public Button shopTab;
        public Button questTab;
        public Button profileTab;

        [Header("Tab Icons")]
        public Image[] tabIcons;

        [Header("Scenes")]
        public string homeScene = "MainHub";
        public string storyScene = "StoryMenu";
        public string shopScene = "Shop";
        public string questScene = "MainHub"; // quests panel in hub
        public string profileScene = "MainHub"; // profile panel in hub

        private int activeTab;

        void Start()
        {
            var bg = GetComponent<Image>();
            if (bg != null) bg.color = new Color(VTheme.Panel.r, VTheme.Panel.g, VTheme.Panel.b, 0.98f);

            if (homeTab) homeTab.onClick.AddListener(() => SwitchTab(0, homeScene));
            if (storyTab) storyTab.onClick.AddListener(() => SwitchTab(1, storyScene));
            if (shopTab) shopTab.onClick.AddListener(() => SwitchTab(2, shopScene));
            if (questTab) questTab.onClick.AddListener(() => SwitchTab(3, questScene));
            if (profileTab) profileTab.onClick.AddListener(() => SwitchTab(4, profileScene));

            UpdateTabVisuals(0);
        }

        void SwitchTab(int index, string scene)
        {
            if (index == activeTab) return;
            activeTab = index;
            UpdateTabVisuals(index);
            UIAudio.Instance?.PlayClick();
            SceneManager.LoadScene(scene);
        }

        void UpdateTabVisuals(int activeIndex)
        {
            if (tabIcons == null) return;
            for (int i = 0; i < tabIcons.Length; i++)
            {
                if (tabIcons[i] == null) continue;
                tabIcons[i].color = i == activeIndex ? VTheme.Red : VTheme.TextMuted;
            }
        }
    }
}
