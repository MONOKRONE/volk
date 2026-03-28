using UnityEngine;
using UnityEditor;
using Volk.Core;

public class CreateMasteryAssets
{
    static readonly string[] Characters = { "YILDIZ", "KAYA", "RUZGAR", "CELIK", "SIS", "TOPRAK" };

    [MenuItem("VOLK/Create Mastery Assets (6 Characters)")]
    public static void Create()
    {
        string dir = "Assets/ScriptableObjects/Mastery";
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Mastery");

        foreach (string charId in Characters)
        {
            var mastery = ScriptableObject.CreateInstance<CharacterMasteryData>();
            mastery.characterId = charId;
            mastery.nodes = new MasteryNode[20];

            // 5 Trials (0-4)
            string[] trials = { "10 parry yap", "20 punch combo yap", "5 skill ile KO", "3 perfect round", "30 kick vur" };
            for (int i = 0; i < 5; i++)
            {
                mastery.nodes[i] = new MasteryNode
                {
                    nodeIndex = i,
                    nodeType = MasteryNodeType.Trial,
                    description = trials[i],
                    requirement = $"trial_{i}",
                    targetValue = new[] { 10, 20, 5, 3, 30 }[i],
                    rewardType = MasteryRewardType.Coins,
                    rewardValue = (100 + i * 50).ToString()
                };
            }

            // 5 Archetype Challenges (5-9)
            string[] archetypes = { "5 mac skill kullanmadan kazan", "10 mac 50%+ HP ile bitir",
                "3 mac art arda kazan", "1 mac 0 hasar al", "Tum chapter boss'larini yen" };
            for (int i = 0; i < 5; i++)
            {
                mastery.nodes[5 + i] = new MasteryNode
                {
                    nodeIndex = 5 + i,
                    nodeType = MasteryNodeType.ArchetypeChallenge,
                    description = archetypes[i],
                    requirement = $"archetype_{i}",
                    targetValue = new[] { 5, 10, 3, 1, 8 }[i],
                    rewardType = MasteryRewardType.Gems,
                    rewardValue = (20 + i * 10).ToString()
                };
            }

            // 5 Lore Unlocks (10-14)
            for (int i = 0; i < 5; i++)
            {
                mastery.nodes[10 + i] = new MasteryNode
                {
                    nodeIndex = 10 + i,
                    nodeType = MasteryNodeType.LoreUnlock,
                    description = $"{charId} History - Chapter {i + 1}",
                    requirement = $"lore_{i}",
                    targetValue = (i + 1) * 5, // play X matches with this char
                    rewardType = MasteryRewardType.Lore,
                    rewardValue = $"Lore_{charId}_{i + 1}"
                };
            }

            // 5 Bonus Rewards (15-19)
            for (int i = 0; i < 5; i++)
            {
                mastery.nodes[15 + i] = new MasteryNode
                {
                    nodeIndex = 15 + i,
                    nodeType = MasteryNodeType.BonusReward,
                    description = $"{charId} Bonus {i + 1}",
                    requirement = $"bonus_{i}",
                    targetValue = (i + 1) * 10,
                    rewardType = i < 3 ? MasteryRewardType.Coins : (i == 3 ? MasteryRewardType.Title : MasteryRewardType.Skin),
                    rewardValue = i < 3 ? (200 + i * 100).ToString() : (i == 3 ? $"Title_{charId}_Master" : $"Skin_{charId}_Gold")
                };
            }

            string path = $"{dir}/Mastery_{charId}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mastery, path);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[VOLK] 6 character mastery assets created (20 nodes each)!");
    }
}
