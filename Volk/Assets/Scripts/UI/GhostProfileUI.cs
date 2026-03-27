using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using System.IO;
using Volk.Core;

namespace Volk.UI
{
    public class GhostProfileUI : MonoBehaviour
    {
        [Header("Navigation")]
        public Button backButton;
        public VTopBar topBar;

        [Header("Ghost Status")]
        public TextMeshProUGUI ghostStatusText;
        public TextMeshProUGUI totalMatchesText;
        public Image activationProgress;

        [Header("Character Grid")]
        public Transform characterGrid;
        public GameObject characterGhostCardPrefab;

        [Header("Active Ghost")]
        public TextMeshProUGUI activeGhostName;
        public Image activeGhostPortrait;
        public TextMeshProUGUI activeGhostPower;
        public Button changeGhostButton;

        [Header("Activation Tiers")]
        public Image tier1Fill;
        public Image tier2Fill;
        public Image tier3Fill;
        public TextMeshProUGUI tier1Label;
        public TextMeshProUGUI tier2Label;
        public TextMeshProUGUI tier3Label;

        // Tier thresholds (total recorded actions)
        private const int TIER1_THRESHOLD = 50;
        private const int TIER2_THRESHOLD = 200;
        private const int TIER3_THRESHOLD = 500;

        private string selectedCharacter;
        private Dictionary<string, GhostCharacterStats> characterStats = new Dictionary<string, GhostCharacterStats>();

        struct GhostCharacterStats
        {
            public int totalMatches;
            public int totalActions;
            public int wins;
            public float powerScore;
        }

        void Awake()
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }

        void Start()
        {
            if (backButton)
                backButton.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));

            if (changeGhostButton)
                changeGhostButton.onClick.AddListener(OnChangeGhost);

            selectedCharacter = PlayerPrefs.GetString("ghost_active_char", "YILDIZ");
            AnalyzeGhostData();
            PopulateCharacterGrid();
            UpdateActiveGhost();
            UpdateActivationTiers();
        }

        void AnalyzeGhostData()
        {
            characterStats.Clear();
            string path = Path.Combine(Application.persistentDataPath, "ghost_profile.json");
            if (!File.Exists(path))
            {
                // No ghost data yet — show empty state
                if (ghostStatusText) ghostStatusText.text = "Veri toplanmadi";
                if (totalMatchesText) totalMatchesText.text = "0 mac";
                return;
            }

            string json = File.ReadAllText(path);
            var profile = JsonUtility.FromJson<BehaviorProfile>(json);
            if (profile == null || profile.situations == null) return;

            int totalActions = 0;
            var charActionCounts = new Dictionary<string, int>();
            var charMatchCounts = new Dictionary<string, HashSet<string>>();

            foreach (var sit in profile.situations)
            {
                // key format: "CHARNAME_vs_ENEMY_Situation"
                string[] parts = sit.key.Split('_');
                if (parts.Length < 3) continue;
                string charName = parts[0];
                string matchup = $"{parts[0]}_{parts[1]}_{parts[2]}";

                if (!charActionCounts.ContainsKey(charName))
                    charActionCounts[charName] = 0;
                if (!charMatchCounts.ContainsKey(charName))
                    charMatchCounts[charName] = new HashSet<string>();

                charActionCounts[charName] += sit.actions.Count;
                charMatchCounts[charName].Add(matchup);
                totalActions += sit.actions.Count;
            }

            foreach (var kvp in charActionCounts)
            {
                int matches = charMatchCounts.ContainsKey(kvp.Key) ? charMatchCounts[kvp.Key].Count : 0;
                int actions = kvp.Value;
                float power = Mathf.Clamp01((float)actions / TIER3_THRESHOLD) * 100f;

                characterStats[kvp.Key] = new GhostCharacterStats
                {
                    totalMatches = matches,
                    totalActions = actions,
                    wins = PlayerPrefs.GetInt($"ghost_wins_{kvp.Key}", 0),
                    powerScore = power
                };
            }

            if (ghostStatusText)
            {
                int tier = GetActivationTier(totalActions);
                ghostStatusText.text = tier switch
                {
                    0 => "Ghost: Baslangiç",
                    1 => "Ghost: Katman 1",
                    2 => "Ghost: Katman 2",
                    3 => "Ghost: Katman 3 (MAX)",
                    _ => "Ghost: Aktif"
                };
                ghostStatusText.color = tier >= 3 ? VTheme.Gold : tier >= 1 ? VTheme.Green : VTheme.TextSecondary;
            }

            if (totalMatchesText)
                totalMatchesText.text = $"{totalActions} aksiyon kaydedildi";
        }

        void PopulateCharacterGrid()
        {
            if (characterGrid == null) return;
            foreach (Transform child in characterGrid)
                Destroy(child.gameObject);

            var allChars = Resources.LoadAll<CharacterData>("Characters");
            if (allChars == null) return;

            foreach (var charData in allChars)
            {
                if (characterGhostCardPrefab != null)
                {
                    var card = Instantiate(characterGhostCardPrefab, characterGrid);
                    SetupCharacterCard(card, charData);
                }
                else
                {
                    CreateCharacterCardRuntime(charData);
                }
            }
        }

        void SetupCharacterCard(GameObject card, CharacterData charData)
        {
            characterStats.TryGetValue(charData.characterName, out var stats);

            var texts = card.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length > 0)
            {
                texts[0].text = charData.characterName;
                texts[0].color = charData.characterName == selectedCharacter ? VTheme.Gold : VTheme.TextPrimary;
            }
            if (texts.Length > 1)
                texts[1].text = $"{stats.totalMatches} mac";
            if (texts.Length > 2)
            {
                float winRate = stats.totalMatches > 0 ? (float)stats.wins / stats.totalMatches * 100f : 0f;
                texts[2].text = $"%{winRate:F0}";
            }

            // Power bar
            var sliders = card.GetComponentsInChildren<Slider>();
            if (sliders.Length > 0)
                sliders[0].value = stats.powerScore / 100f;

            // Portrait
            var portrait = card.transform.Find("Portrait")?.GetComponent<Image>();
            if (portrait != null && charData.portrait != null)
                portrait.sprite = charData.portrait;

            // Active indicator
            var activeIndicator = card.transform.Find("ActiveIndicator");
            if (activeIndicator != null)
                activeIndicator.gameObject.SetActive(charData.characterName == selectedCharacter);

            // Select button
            var btn = card.GetComponent<Button>();
            if (btn != null)
            {
                string capturedName = charData.characterName;
                btn.onClick.AddListener(() => SelectGhost(capturedName));
            }
        }

        void CreateCharacterCardRuntime(CharacterData charData)
        {
            characterStats.TryGetValue(charData.characterName, out var stats);
            bool isActive = charData.characterName == selectedCharacter;

            var go = new GameObject(charData.characterName, typeof(RectTransform));
            go.transform.SetParent(characterGrid, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 80);

            var bg = go.AddComponent<Image>();
            bg.color = isActive
                ? new Color(VTheme.Gold.r, VTheme.Gold.g, VTheme.Gold.b, 0.12f)
                : VTheme.Card;
            bg.raycastTarget = true;

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12;
            hlg.padding = new RectOffset(15, 15, 8, 8);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            // Name
            var nameGO = new GameObject("Name", typeof(RectTransform));
            nameGO.transform.SetParent(go.transform, false);
            var nameTmp = nameGO.AddComponent<TextMeshProUGUI>();
            nameTmp.text = charData.characterName;
            nameTmp.fontSize = 24;
            nameTmp.fontStyle = isActive ? FontStyles.Bold : FontStyles.Normal;
            nameTmp.color = isActive ? VTheme.Gold : VTheme.TextPrimary;
            nameTmp.raycastTarget = false;
            var nameLE = nameGO.AddComponent<LayoutElement>();
            nameLE.preferredWidth = 120;

            // Match count
            var matchGO = new GameObject("Matches", typeof(RectTransform));
            matchGO.transform.SetParent(go.transform, false);
            var matchTmp = matchGO.AddComponent<TextMeshProUGUI>();
            matchTmp.text = $"{stats.totalMatches} mac";
            matchTmp.fontSize = 20;
            matchTmp.color = VTheme.TextSecondary;
            matchTmp.raycastTarget = false;
            var matchLE = matchGO.AddComponent<LayoutElement>();
            matchLE.preferredWidth = 80;

            // Win rate
            float winRate = stats.totalMatches > 0 ? (float)stats.wins / stats.totalMatches * 100f : 0f;
            var winGO = new GameObject("WinRate", typeof(RectTransform));
            winGO.transform.SetParent(go.transform, false);
            var winTmp = winGO.AddComponent<TextMeshProUGUI>();
            winTmp.text = $"%{winRate:F0}";
            winTmp.fontSize = 20;
            winTmp.color = winRate >= 50 ? VTheme.Green : VTheme.Red;
            winTmp.raycastTarget = false;
            var winLE = winGO.AddComponent<LayoutElement>();
            winLE.preferredWidth = 60;

            // Power bar background
            var barBgGO = new GameObject("PowerBarBG", typeof(RectTransform));
            barBgGO.transform.SetParent(go.transform, false);
            var barBgImg = barBgGO.AddComponent<Image>();
            barBgImg.color = VTheme.Panel;
            barBgImg.raycastTarget = false;
            var barBgLE = barBgGO.AddComponent<LayoutElement>();
            barBgLE.flexibleWidth = 1;
            barBgLE.preferredHeight = 12;

            // Power bar fill
            var barFillGO = new GameObject("PowerBarFill", typeof(RectTransform));
            barFillGO.transform.SetParent(barBgGO.transform, false);
            var fillRect = barFillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(stats.powerScore / 100f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImg = barFillGO.AddComponent<Image>();
            fillImg.color = stats.powerScore >= 80 ? VTheme.Gold : stats.powerScore >= 40 ? VTheme.Blue : VTheme.TextMuted;
            fillImg.raycastTarget = false;

            // Click handler
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            string captured = charData.characterName;
            btn.onClick.AddListener(() => SelectGhost(captured));
        }

        void SelectGhost(string characterName)
        {
            selectedCharacter = characterName;
            PlayerPrefs.SetString("ghost_active_char", characterName);
            PlayerPrefs.Save();
            UIAudio.Instance?.PlayClick();
            PopulateCharacterGrid();
            UpdateActiveGhost();
        }

        void UpdateActiveGhost()
        {
            if (activeGhostName)
            {
                activeGhostName.text = selectedCharacter;
                activeGhostName.color = VTheme.Gold;
            }

            characterStats.TryGetValue(selectedCharacter, out var stats);
            if (activeGhostPower)
                activeGhostPower.text = $"Guc: {stats.powerScore:F0}/100";

            // Try to find character portrait
            var allChars = Resources.LoadAll<CharacterData>("Characters");
            if (allChars != null && activeGhostPortrait != null)
            {
                foreach (var c in allChars)
                {
                    if (c.characterName == selectedCharacter && c.portrait != null)
                    {
                        activeGhostPortrait.sprite = c.portrait;
                        break;
                    }
                }
            }
        }

        void UpdateActivationTiers()
        {
            int totalActions = 0;
            foreach (var kvp in characterStats)
                totalActions += kvp.Value.totalActions;

            // Tier 1
            float t1 = Mathf.Clamp01((float)totalActions / TIER1_THRESHOLD);
            if (tier1Fill) tier1Fill.fillAmount = t1;
            if (tier1Label) tier1Label.text = t1 >= 1f ? "Katman 1 \u2713" : $"Katman 1: {totalActions}/{TIER1_THRESHOLD}";
            if (tier1Label) tier1Label.color = t1 >= 1f ? VTheme.Green : VTheme.TextSecondary;

            // Tier 2
            float t2 = Mathf.Clamp01((float)totalActions / TIER2_THRESHOLD);
            if (tier2Fill) tier2Fill.fillAmount = t2;
            if (tier2Label) tier2Label.text = t2 >= 1f ? "Katman 2 \u2713" : $"Katman 2: {totalActions}/{TIER2_THRESHOLD}";
            if (tier2Label) tier2Label.color = t2 >= 1f ? VTheme.Green : VTheme.TextSecondary;

            // Tier 3
            float t3 = Mathf.Clamp01((float)totalActions / TIER3_THRESHOLD);
            if (tier3Fill) tier3Fill.fillAmount = t3;
            if (tier3Label) tier3Label.text = t3 >= 1f ? "Katman 3 \u2713 MAX" : $"Katman 3: {totalActions}/{TIER3_THRESHOLD}";
            if (tier3Label) tier3Label.color = t3 >= 1f ? VTheme.Gold : VTheme.TextSecondary;

            if (activationProgress)
                activationProgress.fillAmount = Mathf.Clamp01((float)totalActions / TIER3_THRESHOLD);
        }

        int GetActivationTier(int totalActions)
        {
            if (totalActions >= TIER3_THRESHOLD) return 3;
            if (totalActions >= TIER2_THRESHOLD) return 2;
            if (totalActions >= TIER1_THRESHOLD) return 1;
            return 0;
        }

        void OnChangeGhost()
        {
            UIAudio.Instance?.PlayClick();
            // Scroll to character grid or show selection popup
            Debug.Log("[Ghost] Change ghost character");
        }
    }
}
