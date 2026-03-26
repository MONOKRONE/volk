using UnityEngine;
using UnityEditor;
using Volk.Core;

public class CreateGhostStages
{
    static readonly (string name, GhostScenarioType scenario, AIDifficulty diff, string desc)[] GhostStageData = new[]
    {
        ("Ghost_01_Mirror",      GhostScenarioType.MirrorMatch,     AIDifficulty.Easy,   "Ayna mac — kendi ghost'unla dovus"),
        ("Ghost_02_Aggressive",  GhostScenarioType.AggressiveClone, AIDifficulty.Normal, "Agresif klon — surekli saldiri baskisi"),
        ("Ghost_03_Defensive",   GhostScenarioType.DefensiveClone,  AIDifficulty.Normal, "Defansif klon — karsi atak uzmanı"),
        ("Ghost_04_LowHP",       GhostScenarioType.LowHPPressure,   AIDifficulty.Normal, "Dusuk can baskisi — %30 HP ile basla"),
        ("Ghost_05_Corner",      GhostScenarioType.CornerTrap,      AIDifficulty.Hard,   "Koseye sikistirma — dar alanda dovus"),
        ("Ghost_06_SkillSpam",   GhostScenarioType.SkillPressure,   AIDifficulty.Hard,   "Skill baskisi — surekli ozel beceri"),
        ("Ghost_07_Combo",       GhostScenarioType.ComboChain,      AIDifficulty.Hard,   "Combo zinciri — ardisik saldiri kaliplari"),
        ("Ghost_08_Parry",       GhostScenarioType.ParryCounter,    AIDifficulty.Hard,   "Parry ustasi — bloklayip vurur"),
        ("Ghost_09_Adaptive",    GhostScenarioType.AdaptiveClone,   AIDifficulty.Hard,   "Adaptif klon — senin tarzina uyum saglar"),
        ("Ghost_10_Ultimate",    GhostScenarioType.MirrorMatch,     AIDifficulty.Hard,   "Ultimate ghost — tam guc ayna mac"),
    };

    [MenuItem("VOLK/Create 10 Ghost Stages")]
    public static void Create()
    {
        string dir = "Assets/ScriptableObjects/GhostStages";
        EnsureFolder("Assets/ScriptableObjects", "GhostStages");

        // Also create Resources/GhostStages for runtime loading
        EnsureFolder("Assets", "Resources");
        EnsureFolder("Assets/Resources", "GhostStages");

        for (int i = 0; i < GhostStageData.Length; i++)
        {
            var (name, scenario, diff, desc) = GhostStageData[i];
            string path = $"{dir}/{name}.asset";
            AssetDatabase.DeleteAsset(path);

            var stage = ScriptableObject.CreateInstance<StageData>();
            stage.stageName = desc;
            stage.stageIndex = i;
            stage.stageType = StageType.Standard;
            stage.difficulty = diff;
            stage.isGhostSimulation = true;
            stage.ghostScenarioType = scenario;
            stage.coinReward = 30 + (i * 10);
            stage.hpMultiplier = 1f + (i * 0.1f); // Ghost gets harder

            // Special modifiers
            if (scenario == GhostScenarioType.LowHPPressure)
                stage.playerHPMultiplier = 0.3f;
            if (scenario == GhostScenarioType.CornerTrap)
                stage.timeLimitSeconds = 60f;

            AssetDatabase.CreateAsset(stage, path);

            // Copy to Resources for runtime loading
            string resPath = $"Assets/Resources/GhostStages/{name}.asset";
            AssetDatabase.DeleteAsset(resPath);
            AssetDatabase.CopyAsset(path, resPath);

            Debug.Log($"[VOLK] Ghost stage: {name}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[VOLK] {GhostStageData.Length} ghost stages created!");
    }

    static void EnsureFolder(string parent, string child)
    {
        string full = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(full))
            AssetDatabase.CreateFolder(parent, child);
    }
}
