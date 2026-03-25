using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Volk.UI
{
    public class ClanUI : MonoBehaviour
    {
        [Header("Clan Info")]
        public TextMeshProUGUI clanNameText;
        public TextMeshProUGUI memberCountText;
        public TextMeshProUGUI clanLevelText;
        public Image clanBanner;

        [Header("Tabs")]
        public VTabBar tabBar;

        [Header("Panels")]
        public GameObject membersPanel;
        public GameObject warPanel;
        public GameObject settingsPanel;

        [Header("War Status")]
        public TextMeshProUGUI warStatusText;
        public Slider warProgressBar;

        void OnEnable() => Refresh();

        public void Refresh()
        {
            // Placeholder data — backend integration later
            string clanName = PlayerPrefs.GetString("clan_name", "");

            if (string.IsNullOrEmpty(clanName))
            {
                ShowNoClan();
                return;
            }

            if (clanNameText) clanNameText.text = clanName;
            if (memberCountText) memberCountText.text = $"{PlayerPrefs.GetInt("clan_members", 1)}/30 Uye";
            if (clanLevelText) clanLevelText.text = $"Seviye {PlayerPrefs.GetInt("clan_level", 1)}";

            bool warActive = PlayerPrefs.GetInt("clan_war_active", 0) == 1;
            if (warStatusText) warStatusText.text = warActive ? "Savas Devam Ediyor!" : "Savas Yok";
            if (warProgressBar) warProgressBar.gameObject.SetActive(warActive);
        }

        void ShowNoClan()
        {
            if (clanNameText) clanNameText.text = "Klan Yok";
            if (memberCountText) memberCountText.text = "Bir klana katil veya klan kur";
            if (clanLevelText) clanLevelText.text = "";
        }

        public void OnTabChanged(int tabIndex)
        {
            if (membersPanel) membersPanel.SetActive(tabIndex == 0);
            if (warPanel) warPanel.SetActive(tabIndex == 1);
            if (settingsPanel) settingsPanel.SetActive(tabIndex == 2);
        }
    }
}
