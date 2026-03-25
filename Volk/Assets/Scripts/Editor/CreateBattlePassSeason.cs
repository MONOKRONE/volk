using UnityEngine;
using UnityEditor;
using Volk.Core;

public class CreateBattlePassSeason
{
    [MenuItem("VOLK/Create Battle Pass Season 1")]
    public static void Create()
    {
        string dir = "Assets/ScriptableObjects/BattlePass";
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "BattlePass");

        var season = ScriptableObject.CreateInstance<SeasonData>();
        season.seasonName = "Sezon 1: Sokak Efsaneleri";
        season.startDate = "2026-04-01";
        season.endDate = "2026-05-27";

        season.tiers = new BattlePassTier[60];
        int cumulativeXP = 0;

        for (int i = 0; i < 60; i++)
        {
            int tier = i + 1;
            cumulativeXP += tier * 100;

            season.tiers[i] = new BattlePassTier
            {
                tierNumber = tier,
                xpRequired = cumulativeXP,
                freeReward = GetFreeReward(tier),
                premiumReward = GetPremiumReward(tier)
            };
        }

        string path = $"{dir}/Season1.asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(season, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"[VOLK] Battle Pass Season 1 created: 60 tiers, max XP={cumulativeXP}");
    }

    static string GetFreeReward(int tier)
    {
        if (tier % 10 == 0) return $"{tier * 50} Coin";
        if (tier % 10 == 5) return $"{tier * 5} Gem";
        return "";
    }

    static string GetPremiumReward(int tier)
    {
        return tier switch
        {
            1 => "EpicSkin_YILDIZ",
            10 => "HitEffect_Fire",
            15 => "500 Coin",
            20 => "ArenaUnlock_Night",
            25 => "HitEffect_Ice",
            30 => "1000 Coin",
            35 => "EpicSkin_KAYA",
            40 => "HitEffect_Lightning",
            45 => "2000 Coin",
            50 => "LegendarySkin_RUZGAR",
            55 => "3000 Coin",
            60 => "LegendarySkin_CELIK",
            _ => tier % 5 == 0 ? $"{tier * 20} Coin" : ""
        };
    }
}
