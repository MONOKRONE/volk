using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

namespace Volk.UI
{
    public class ClanUI : MonoBehaviour
    {
        [Header("Navigation")]
        public Button backButton;
        public VTopBar topBar;

        [Header("Clan Info")]
        public TextMeshProUGUI clanNameText;
        public TextMeshProUGUI memberCountText;
        public TextMeshProUGUI clanLevelText;
        public Image clanBanner;

        [Header("No Clan State")]
        public GameObject noClanPanel;
        public Button createClanButton;
        public Button joinClanButton;
        public TMP_InputField clanNameInput;
        public TMP_InputField searchInput;
        public Transform searchResultsParent;

        [Header("Clan State")]
        public GameObject clanPanel;

        [Header("Tab Buttons")]
        public Button membersTab;
        public Button warTab;
        public Button settingsTab;

        [Header("Panels")]
        public GameObject membersPanel;
        public GameObject warPanel;
        public GameObject settingsPanel;

        [Header("Member List")]
        public Transform memberListParent;
        public GameObject memberEntryPrefab;

        [Header("War Status")]
        public TextMeshProUGUI warStatusText;
        public Slider warProgressBar;
        public TextMeshProUGUI warScoreText;
        public TextMeshProUGUI warOpponentText;
        public Button startWarButton;

        [Header("Settings")]
        public Button leaveClanButton;
        public TMP_InputField editClanNameInput;

        private int activeTab;

        void Awake()
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }

        void Start()
        {
            if (backButton)
                backButton.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));

            if (createClanButton)
                createClanButton.onClick.AddListener(OnCreateClan);
            if (joinClanButton)
                joinClanButton.onClick.AddListener(OnJoinClan);
            if (leaveClanButton)
                leaveClanButton.onClick.AddListener(OnLeaveClan);
            if (startWarButton)
                startWarButton.onClick.AddListener(OnStartWar);

            if (membersTab) membersTab.onClick.AddListener(() => SwitchTab(0));
            if (warTab) warTab.onClick.AddListener(() => SwitchTab(1));
            if (settingsTab) settingsTab.onClick.AddListener(() => SwitchTab(2));

            Refresh();
        }

        public void Refresh()
        {
            string clanName = PlayerPrefs.GetString("clan_name", "");
            bool hasClan = !string.IsNullOrEmpty(clanName);

            if (noClanPanel) noClanPanel.SetActive(!hasClan);
            if (clanPanel) clanPanel.SetActive(hasClan);

            if (!hasClan)
            {
                ShowNoClan();
                return;
            }

            // Clan info
            if (clanNameText)
            {
                clanNameText.text = clanName;
                clanNameText.color = VTheme.Gold;
            }

            int members = PlayerPrefs.GetInt("clan_members", 1);
            if (memberCountText)
            {
                memberCountText.text = $"{members}/30 Uye";
                memberCountText.color = VTheme.TextSecondary;
            }

            int level = PlayerPrefs.GetInt("clan_level", 1);
            if (clanLevelText)
            {
                clanLevelText.text = $"Seviye {level}";
                clanLevelText.color = VTheme.Blue;
            }

            SwitchTab(0);
            PopulateMembers();
            UpdateWarStatus();
        }

        void ShowNoClan()
        {
            if (clanNameText)
            {
                clanNameText.text = "Klana Katil";
                clanNameText.color = VTheme.TextPrimary;
            }
            if (memberCountText)
            {
                memberCountText.text = "Bir klan kur veya mevcut klana katil";
                memberCountText.color = VTheme.TextSecondary;
            }
            if (clanLevelText) clanLevelText.text = "";
        }

        // --- Tab Management ---

        void SwitchTab(int index)
        {
            activeTab = index;
            UIAudio.Instance?.PlayClick();

            if (membersPanel) membersPanel.SetActive(index == 0);
            if (warPanel) warPanel.SetActive(index == 1);
            if (settingsPanel) settingsPanel.SetActive(index == 2);

            SetTabColor(membersTab, index == 0);
            SetTabColor(warTab, index == 1);
            SetTabColor(settingsTab, index == 2);
        }

        void SetTabColor(Button tab, bool active)
        {
            if (tab == null) return;
            var img = tab.GetComponent<Image>();
            if (img) img.color = active ? VTheme.Red : VTheme.Card;
            var txt = tab.GetComponentInChildren<TextMeshProUGUI>();
            if (txt) txt.color = active ? VTheme.TextPrimary : VTheme.TextMuted;
        }

        // --- Member List ---

        void PopulateMembers()
        {
            if (memberListParent == null) return;

            foreach (Transform child in memberListParent)
                Destroy(child.gameObject);

            int memberCount = PlayerPrefs.GetInt("clan_members", 1);
            var members = GeneratePlaceholderMembers(memberCount);

            foreach (var member in members)
            {
                if (memberEntryPrefab != null)
                {
                    var entry = Instantiate(memberEntryPrefab, memberListParent);
                    SetupMemberEntry(entry, member);
                }
                else
                {
                    CreateMemberEntryRuntime(member);
                }
            }
        }

        void SetupMemberEntry(GameObject entry, MemberData member)
        {
            var texts = entry.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length > 0) texts[0].text = member.name;
            if (texts.Length > 1) texts[1].text = $"ELO: {member.elo}";
            if (texts.Length > 2)
            {
                texts[2].text = member.hasGhost ? "Ghost: Aktif" : "Ghost: -";
                texts[2].color = member.hasGhost ? VTheme.Green : VTheme.TextMuted;
            }

            var roleIcon = entry.transform.Find("RoleIcon")?.GetComponent<Image>();
            if (roleIcon != null)
                roleIcon.color = member.isLeader ? VTheme.Gold : VTheme.TextMuted;
        }

        void CreateMemberEntryRuntime(MemberData member)
        {
            var go = new GameObject(member.name, typeof(RectTransform));
            go.transform.SetParent(memberListParent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 60);

            var bg = go.AddComponent<Image>();
            bg.color = member.isLeader
                ? new Color(VTheme.Gold.r, VTheme.Gold.g, VTheme.Gold.b, 0.08f)
                : VTheme.Card;
            bg.raycastTarget = false;

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12;
            hlg.padding = new RectOffset(15, 15, 5, 5);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            // Role indicator
            var roleGO = new GameObject("Role", typeof(RectTransform));
            roleGO.transform.SetParent(go.transform, false);
            var roleTmp = roleGO.AddComponent<TextMeshProUGUI>();
            roleTmp.text = member.isLeader ? "\u2605" : "\u2022"; // star or bullet
            roleTmp.fontSize = 20;
            roleTmp.color = member.isLeader ? VTheme.Gold : VTheme.TextMuted;
            roleTmp.raycastTarget = false;
            var roleLE = roleGO.AddComponent<LayoutElement>();
            roleLE.preferredWidth = 30;

            // Name
            var nameGO = new GameObject("Name", typeof(RectTransform));
            nameGO.transform.SetParent(go.transform, false);
            var nameTmp = nameGO.AddComponent<TextMeshProUGUI>();
            nameTmp.text = member.name;
            nameTmp.fontSize = 22;
            nameTmp.color = VTheme.TextPrimary;
            nameTmp.fontStyle = member.isLeader ? FontStyles.Bold : FontStyles.Normal;
            nameTmp.raycastTarget = false;
            var nameLE = nameGO.AddComponent<LayoutElement>();
            nameLE.flexibleWidth = 1;

            // ELO
            var eloGO = new GameObject("Elo", typeof(RectTransform));
            eloGO.transform.SetParent(go.transform, false);
            var eloTmp = eloGO.AddComponent<TextMeshProUGUI>();
            eloTmp.text = $"ELO: {member.elo}";
            eloTmp.fontSize = 20;
            eloTmp.color = VTheme.Blue;
            eloTmp.raycastTarget = false;
            var eloLE = eloGO.AddComponent<LayoutElement>();
            eloLE.preferredWidth = 100;

            // Ghost status
            var ghostGO = new GameObject("Ghost", typeof(RectTransform));
            ghostGO.transform.SetParent(go.transform, false);
            var ghostTmp = ghostGO.AddComponent<TextMeshProUGUI>();
            ghostTmp.text = member.hasGhost ? "Ghost" : "-";
            ghostTmp.fontSize = 18;
            ghostTmp.color = member.hasGhost ? VTheme.Green : VTheme.TextMuted;
            ghostTmp.raycastTarget = false;
            var ghostLE = ghostGO.AddComponent<LayoutElement>();
            ghostLE.preferredWidth = 70;
        }

        // --- War Status ---

        void UpdateWarStatus()
        {
            bool warActive = PlayerPrefs.GetInt("clan_war_active", 0) == 1;

            if (warStatusText)
            {
                warStatusText.text = warActive ? "Klan Savasi Devam Ediyor!" : "Aktif savas yok";
                warStatusText.color = warActive ? VTheme.Red : VTheme.TextSecondary;
            }

            if (warProgressBar)
            {
                warProgressBar.gameObject.SetActive(warActive);
                if (warActive)
                    warProgressBar.value = PlayerPrefs.GetFloat("clan_war_progress", 0.5f);
            }

            if (warScoreText)
            {
                warScoreText.gameObject.SetActive(warActive);
                if (warActive)
                {
                    int ourScore = PlayerPrefs.GetInt("clan_war_our_score", 0);
                    int theirScore = PlayerPrefs.GetInt("clan_war_their_score", 0);
                    warScoreText.text = $"{ourScore} - {theirScore}";
                    warScoreText.color = ourScore > theirScore ? VTheme.Green : ourScore < theirScore ? VTheme.Red : VTheme.Gold;
                }
            }

            if (warOpponentText)
            {
                warOpponentText.gameObject.SetActive(warActive);
                if (warActive)
                    warOpponentText.text = $"vs {PlayerPrefs.GetString("clan_war_opponent", "???")}";
            }

            if (startWarButton)
                startWarButton.gameObject.SetActive(!warActive);
        }

        // --- Actions ---

        void OnCreateClan()
        {
            string name = clanNameInput?.text;
            if (string.IsNullOrWhiteSpace(name)) return;

            UIAudio.Instance?.PlayClick();
            PlayerPrefs.SetString("clan_name", name.Trim());
            PlayerPrefs.SetInt("clan_members", 1);
            PlayerPrefs.SetInt("clan_level", 1);
            PlayerPrefs.Save();
            Debug.Log($"[Clan] Created: {name}");
            Refresh();
        }

        void OnJoinClan()
        {
            string search = searchInput?.text;
            if (string.IsNullOrWhiteSpace(search)) return;

            UIAudio.Instance?.PlayClick();
            // Placeholder: join the searched clan directly
            PlayerPrefs.SetString("clan_name", search.Trim());
            PlayerPrefs.SetInt("clan_members", Random.Range(5, 25));
            PlayerPrefs.SetInt("clan_level", Random.Range(1, 10));
            PlayerPrefs.Save();
            Debug.Log($"[Clan] Joined: {search}");
            Refresh();
        }

        void OnLeaveClan()
        {
            UIAudio.Instance?.PlayClick();
            PlayerPrefs.DeleteKey("clan_name");
            PlayerPrefs.DeleteKey("clan_members");
            PlayerPrefs.DeleteKey("clan_level");
            PlayerPrefs.DeleteKey("clan_war_active");
            PlayerPrefs.Save();
            Debug.Log("[Clan] Left clan");
            Refresh();
        }

        void OnStartWar()
        {
            UIAudio.Instance?.PlayClick();
            PlayerPrefs.SetInt("clan_war_active", 1);
            PlayerPrefs.SetFloat("clan_war_progress", 0.5f);
            PlayerPrefs.SetInt("clan_war_our_score", 0);
            PlayerPrefs.SetInt("clan_war_their_score", 0);
            PlayerPrefs.SetString("clan_war_opponent", "Rakip Klan");
            PlayerPrefs.Save();
            Debug.Log("[Clan] War started!");
            UpdateWarStatus();
        }

        // --- Placeholder Data ---

        struct MemberData
        {
            public string name;
            public int elo;
            public bool hasGhost;
            public bool isLeader;
        }

        List<MemberData> GeneratePlaceholderMembers(int count)
        {
            var list = new List<MemberData>();
            string[] names = { "Oyuncu", "Savaşçı", "Gölge", "Fırtına", "Çelik", "Toprak", "Yıldız", "Kaya" };
            for (int i = 0; i < Mathf.Min(count, names.Length); i++)
            {
                list.Add(new MemberData
                {
                    name = names[i],
                    elo = 800 + (names.Length - i) * 120 + Random.Range(-50, 50),
                    hasGhost = Random.value > 0.4f,
                    isLeader = i == 0
                });
            }
            // Sort by elo descending
            list.Sort((a, b) => b.elo.CompareTo(a.elo));
            return list;
        }
    }
}
