using UnityEngine;
using System.Collections.Generic;

namespace Volk.Core
{
    public enum MasteryNodeType
    {
        Damage,             // Attack power nodes
        Defense,            // Defense/HP nodes
        Speed,              // Movement/attack speed nodes
        Special,            // Unique character abilities
        Trial,              // Legacy: combat trial
        ArchetypeChallenge, // Legacy: archetype challenge
        LoreUnlock,         // Legacy: lore unlock
        BonusReward         // Legacy: bonus reward
    }

    public enum MasteryRewardType
    {
        Coins,
        Gems,
        Skin,
        Title,
        StatBoost,
        Lore
    }

    [System.Serializable]
    public class MasteryNode
    {
        public int nodeIndex;
        public MasteryNodeType nodeType;
        public string description;
        public string requirement; // e.g. "10_parry", "5_wins_no_skill"
        public int targetValue;
        public int coinCost;       // Coin cost to unlock (0 = free/auto)
        public MasteryRewardType rewardType;
        public string rewardValue; // e.g. "500", "Skin_YILDIZ_Gold"
        public int[] prerequisites; // Node indices that must be completed first
    }

    [CreateAssetMenu(fileName = "NewMastery", menuName = "VOLK/Character Mastery")]
    public class CharacterMasteryData : ScriptableObject
    {
        public string characterId;
        public MasteryNode[] nodes = new MasteryNode[20];
    }

    public class CharacterMasteryManager : MonoBehaviour
    {
        public static CharacterMasteryManager Instance { get; private set; }

        [Header("Mastery Data")]
        public CharacterMasteryData[] allMasteries;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        string Key(string charId, int nodeIdx) => $"mastery_{charId}_{nodeIdx}";
        string ProgressKey(string charId, int nodeIdx) => $"mastery_{charId}_{nodeIdx}_prog";

        public bool IsCompleted(string characterId, int nodeIndex)
        {
            return PlayerPrefs.GetInt(Key(characterId, nodeIndex), 0) == 1;
        }

        public float GetProgress(string characterId, int nodeIndex)
        {
            var mastery = GetMastery(characterId);
            if (mastery == null || nodeIndex >= mastery.nodes.Length) return 0f;

            if (IsCompleted(characterId, nodeIndex)) return 1f;

            int current = PlayerPrefs.GetInt(ProgressKey(characterId, nodeIndex), 0);
            int target = mastery.nodes[nodeIndex].targetValue;
            return target > 0 ? Mathf.Clamp01((float)current / target) : 0f;
        }

        public void AddProgress(string characterId, int nodeIndex, int amount = 1)
        {
            if (IsCompleted(characterId, nodeIndex)) return;

            var mastery = GetMastery(characterId);
            if (mastery == null || nodeIndex >= mastery.nodes.Length) return;

            string pKey = ProgressKey(characterId, nodeIndex);
            int current = PlayerPrefs.GetInt(pKey, 0) + amount;
            PlayerPrefs.SetInt(pKey, current);

            if (current >= mastery.nodes[nodeIndex].targetValue)
                TryComplete(characterId, nodeIndex);
            else
                PlayerPrefs.Save();
        }

        public bool TryComplete(string characterId, int nodeIndex)
        {
            if (IsCompleted(characterId, nodeIndex)) return true;

            var mastery = GetMastery(characterId);
            if (mastery == null || nodeIndex >= mastery.nodes.Length) return false;

            var node = mastery.nodes[nodeIndex];

            // Check prerequisites
            if (!ArePrerequisitesMet(characterId, node)) return false;

            int current = PlayerPrefs.GetInt(ProgressKey(characterId, nodeIndex), 0);
            if (current < node.targetValue) return false;

            // Coin cost
            if (node.coinCost > 0)
            {
                if (CurrencyManager.Instance == null || CurrencyManager.Instance.Coins < node.coinCost)
                    return false;
                CurrencyManager.Instance.SpendCoins(node.coinCost);
            }

            PlayerPrefs.SetInt(Key(characterId, nodeIndex), 1);
            PlayerPrefs.Save();

            GrantReward(node);
            Debug.Log($"[Mastery] {characterId} node {nodeIndex} completed: {node.description}");
            return true;
        }

        /// <summary>
        /// Unlock a node by paying coin cost directly (no progress requirement).
        /// </summary>
        public bool TryPurchaseNode(string characterId, int nodeIndex)
        {
            if (IsCompleted(characterId, nodeIndex)) return true;

            var mastery = GetMastery(characterId);
            if (mastery == null || nodeIndex >= mastery.nodes.Length) return false;

            var node = mastery.nodes[nodeIndex];
            if (!ArePrerequisitesMet(characterId, node)) return false;
            if (node.coinCost <= 0) return false;

            if (CurrencyManager.Instance == null || CurrencyManager.Instance.Coins < node.coinCost)
                return false;

            CurrencyManager.Instance.SpendCoins(node.coinCost);
            PlayerPrefs.SetInt(Key(characterId, nodeIndex), 1);
            PlayerPrefs.Save();

            GrantReward(node);
            Debug.Log($"[Mastery] {characterId} node {nodeIndex} purchased for {node.coinCost} coins");
            return true;
        }

        bool ArePrerequisitesMet(string characterId, MasteryNode node)
        {
            if (node.prerequisites == null || node.prerequisites.Length == 0) return true;
            foreach (int prereq in node.prerequisites)
            {
                if (!IsCompleted(characterId, prereq)) return false;
            }
            return true;
        }

        void GrantReward(MasteryNode node)
        {
            switch (node.rewardType)
            {
                case MasteryRewardType.Coins:
                    if (int.TryParse(node.rewardValue, out int coins))
                        CurrencyManager.Instance?.AddCoins(coins);
                    break;
                case MasteryRewardType.Gems:
                    if (int.TryParse(node.rewardValue, out int gems))
                        CurrencyManager.Instance?.AddGems(gems);
                    break;
                default:
                    Debug.Log($"[Mastery] Reward: {node.rewardType} = {node.rewardValue}");
                    break;
            }
        }

        public int GetCompletedCount(string characterId)
        {
            var mastery = GetMastery(characterId);
            if (mastery == null) return 0;
            int count = 0;
            for (int i = 0; i < mastery.nodes.Length; i++)
                if (IsCompleted(characterId, i)) count++;
            return count;
        }

        public float GetOverallProgress(string characterId)
        {
            var mastery = GetMastery(characterId);
            if (mastery == null || mastery.nodes.Length == 0) return 0f;
            return (float)GetCompletedCount(characterId) / mastery.nodes.Length;
        }

        /// <summary>
        /// Add progress to the first node matching the given type.
        /// Returns true if a matching node was found and progress was added.
        /// </summary>
        public bool AddProgressByNodeType(string characterId, MasteryNodeType nodeType, int amount = 1)
        {
            int idx = FindNodeIndexByType(characterId, nodeType);
            if (idx < 0) return false;
            AddProgress(characterId, idx, amount);
            return true;
        }

        /// <summary>
        /// Returns the index of the first node matching the given type, or -1 if not found.
        /// </summary>
        public int FindNodeIndexByType(string characterId, MasteryNodeType nodeType)
        {
            var mastery = GetMastery(characterId);
            if (mastery == null) return -1;
            for (int i = 0; i < mastery.nodes.Length; i++)
            {
                if (mastery.nodes[i] != null && mastery.nodes[i].nodeType == nodeType)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Add progress to all nodes whose requirement starts with the given prefix.
        /// E.g. "hit" matches "hit_landed", "hit_parry"; "wins" matches "wins", "wins_no_skill".
        /// Returns the number of nodes that received progress.
        /// </summary>
        public int AddProgressByRequirement(string characterId, string requirementPrefix, int amount = 1)
        {
            var mastery = GetMastery(characterId);
            if (mastery == null) return 0;
            int matched = 0;
            for (int i = 0; i < mastery.nodes.Length; i++)
            {
                var node = mastery.nodes[i];
                if (node != null && !string.IsNullOrEmpty(node.requirement)
                    && node.requirement.StartsWith(requirementPrefix, System.StringComparison.OrdinalIgnoreCase))
                {
                    AddProgress(characterId, i, amount);
                    matched++;
                }
            }
            return matched;
        }

        CharacterMasteryData GetMastery(string characterId)
        {
            if (allMasteries == null) return null;
            foreach (var m in allMasteries)
                if (m != null && m.characterId == characterId) return m;
            return null;
        }
    }
}
