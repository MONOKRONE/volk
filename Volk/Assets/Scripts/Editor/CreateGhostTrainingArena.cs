using UnityEngine;
using UnityEditor;
using Volk.Core;

public class CreateGhostTrainingArena
{
    static readonly (string name, string desc, GhostScenarioType scenario, float hpMult, float timeSec)[] Sections = new[]
    {
        ("GT_01_BasicMirror",    "Temel ayna — ghost davranislarini ogren",         GhostScenarioType.MirrorMatch,     1.0f, 0f),
        ("GT_02_AggressiveDrill","Agresif drill — surekli saldiri baskisi",         GhostScenarioType.AggressiveClone, 1.0f, 60f),
        ("GT_03_DefensiveDrill", "Defansif drill — karsi atak calistir",            GhostScenarioType.DefensiveClone,  1.0f, 60f),
        ("GT_04_LowHPSurvival",  "Dusuk can — %30 HP ile hayatta kal",             GhostScenarioType.LowHPPressure,   0.3f, 90f),
        ("GT_05_ComboTraining",  "Combo egitimi — ardiisik saldiri kaliplari",      GhostScenarioType.ComboChain,      1.5f, 0f),
        ("GT_06_ParryMaster",    "Parry usta — bloklama ve karsi atak",             GhostScenarioType.ParryCounter,    1.0f, 45f),
    };

    [MenuItem("VOLK/Create Ghost Training Arena (6 sections)")]
    public static void Create()
    {
        string dir = "Assets/ScriptableObjects/GhostTraining";
        EnsureFolder("Assets/ScriptableObjects", "GhostTraining");

        for (int i = 0; i < Sections.Length; i++)
        {
            var (name, desc, scenario, hpMult, time) = Sections[i];
            string path = $"{dir}/{name}.asset";
            AssetDatabase.DeleteAsset(path);

            var stage = ScriptableObject.CreateInstance<StageData>();
            stage.stageName = desc;
            stage.stageIndex = i;
            stage.stageType = StageType.Standard;
            stage.difficulty = AIDifficulty.Normal;
            stage.isGhostSimulation = true;
            stage.ghostScenarioType = scenario;
            stage.playerHPMultiplier = hpMult;
            stage.timeLimitSeconds = time;
            stage.coinReward = 20;

            AssetDatabase.CreateAsset(stage, path);
            Debug.Log($"[VOLK] Ghost training: {name}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[VOLK] {Sections.Length} ghost training sections created!");
    }

    static void EnsureFolder(string parent, string child)
    {
        string full = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(full))
            AssetDatabase.CreateFolder(parent, child);
    }
}
