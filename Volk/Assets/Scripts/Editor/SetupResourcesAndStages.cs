using UnityEngine;
using UnityEditor;
using System.IO;
using Volk.Core;

/// <summary>
/// Moves ScriptableObjects to Resources/ folders so Resources.LoadAll works at runtime.
/// Creates StageData SOs for each chapter and links them.
/// Sets Script Execution Order for critical managers.
/// Run from: VOLK > Setup Resources & Stages
/// </summary>
public class SetupResourcesAndStages
{
    static readonly string[] characterNames = { "YILDIZ", "KAYA", "RUZGAR", "CELIK", "SIS", "TOPRAK" };
    static readonly string[] chapterTitles = {
        "Uyaniş", "İlk Adımlar", "Arena Ateşi", "Gölgeler",
        "Fırtına", "Çelik İrade", "Kayıp Dövüşçü", "Son Savaş"
    };

    [MenuItem("VOLK/Setup Resources and Stages")]
    public static void Execute()
    {
        MoveSOsToResources();
        CreateStageDataAssets();
        LinkStagesToChapters();
        SetScriptExecutionOrder();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Setup] All resources, stages, and script order configured.");
    }

    static void MoveSOsToResources()
    {
        // Ensure Resources directories exist
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Chapters");
        EnsureFolder("Assets/Resources/Characters");
        EnsureFolder("Assets/Resources/GhostStages");
        EnsureFolder("Assets/Resources/Stages");

        // Move Chapters
        MoveAssetsIfNeeded("Assets/ScriptableObjects/Chapters", "Assets/Resources/Chapters", "*.asset");
        // Move Characters
        MoveAssetsIfNeeded("Assets/ScriptableObjects/Characters", "Assets/Resources/Characters", "*.asset");
    }

    static void MoveAssetsIfNeeded(string srcDir, string dstDir, string pattern)
    {
        if (!Directory.Exists(srcDir)) return;

        var files = Directory.GetFiles(srcDir, pattern);
        foreach (var file in files)
        {
            string assetPath = file.Replace("\\", "/");
            string fileName = Path.GetFileName(assetPath);
            string destPath = $"{dstDir}/{fileName}";

            if (File.Exists(destPath)) continue; // Already moved

            string result = AssetDatabase.MoveAsset(assetPath, destPath);
            if (string.IsNullOrEmpty(result))
                Debug.Log($"[Setup] Moved {assetPath} -> {destPath}");
            else
                Debug.LogWarning($"[Setup] Failed to move {assetPath}: {result}");
        }
    }

    static void CreateStageDataAssets()
    {
        EnsureFolder("Assets/Resources/Stages");

        var characters = Resources.LoadAll<CharacterData>("Characters");
        if (characters == null || characters.Length == 0)
        {
            // Try loading from ScriptableObjects path directly
            string[] guids = AssetDatabase.FindAssets("t:CharacterData");
            characters = new CharacterData[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                characters[i] = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            }
        }

        for (int ch = 1; ch <= 8; ch++)
        {
            for (int st = 1; st <= 10; st++)
            {
                int globalIndex = (ch - 1) * 10 + st;
                string assetName = $"Stage_{ch}_{st}";
                string path = $"Assets/Resources/Stages/{assetName}.asset";

                if (File.Exists(path)) continue;

                var stage = ScriptableObject.CreateInstance<StageData>();
                stage.stageName = $"Bolum {ch} - Sahne {st}";
                stage.stageIndex = globalIndex;

                // Assign opponent from character roster (cycle through)
                if (characters.Length > 0)
                    stage.opponentCharacter = characters[(globalIndex - 1) % characters.Length];

                // Difficulty ramps up
                if (globalIndex <= 20)
                    stage.difficulty = AIDifficulty.Easy;
                else if (globalIndex <= 50)
                    stage.difficulty = AIDifficulty.Normal;
                else
                    stage.difficulty = AIDifficulty.Hard;

                // Stage 10 of each chapter is boss-like
                if (st == 10)
                {
                    stage.stageType = StageType.Boss;
                    stage.hpMultiplier = 1.5f;
                    stage.coinReward = 150;
                }
                else if (st == 5)
                {
                    stage.stageType = StageType.Timed;
                    stage.timeLimitSeconds = 60f;
                    stage.coinReward = 75;
                }
                else
                {
                    stage.stageType = StageType.Standard;
                    stage.hpMultiplier = 1f + (globalIndex - 1) * 0.02f;
                    stage.coinReward = 50;
                }

                AssetDatabase.CreateAsset(stage, path);
            }
        }

        Debug.Log("[Setup] Created 80 StageData assets in Resources/Stages/");
    }

    static void LinkStagesToChapters()
    {
        string[] chapterGuids = AssetDatabase.FindAssets("t:ChapterData");
        foreach (string guid in chapterGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var chapter = AssetDatabase.LoadAssetAtPath<ChapterData>(path);
            if (chapter == null) continue;

            int ch = chapter.chapterNumber;
            if (ch < 1 || ch > 8) continue;

            // Check if stages already populated
            if (chapter.stages != null && chapter.stages.Length == 10)
            {
                bool allValid = true;
                foreach (var s in chapter.stages)
                    if (s == null) { allValid = false; break; }
                if (allValid) continue;
            }

            var stages = new StageData[10];
            bool anyMissing = false;
            for (int st = 1; st <= 10; st++)
            {
                string stagePath = $"Assets/Resources/Stages/Stage_{ch}_{st}.asset";
                stages[st - 1] = AssetDatabase.LoadAssetAtPath<StageData>(stagePath);
                if (stages[st - 1] == null) anyMissing = true;
            }

            if (anyMissing)
            {
                Debug.LogWarning($"[Setup] Chapter {ch}: Some stages missing, skipping link");
                continue;
            }

            chapter.stages = stages;

            // Set chapter title if empty
            if (string.IsNullOrEmpty(chapter.chapterTitle) && ch <= chapterTitles.Length)
                chapter.chapterTitle = chapterTitles[ch - 1];

            EditorUtility.SetDirty(chapter);
            Debug.Log($"[Setup] Linked 10 stages to Chapter {ch}");
        }
    }

    [MenuItem("VOLK/Set Script Execution Order")]
    public static void SetScriptExecutionOrder()
    {
        SetOrder<GameManager>(-100);
        SetOrder<SaveManager>(-90);
        SetOrder<CurrencyManager>(-80);
        SetOrder<BattlePassManager>(-70);
        SetOrder<PlayerBehaviorTracker>(-60);
        Debug.Log("[Setup] Script Execution Order set for 5 managers.");
    }

    static void SetOrder<T>(int order) where T : MonoBehaviour
    {
        MonoScript script = FindMonoScript<T>();
        if (script != null && MonoImporter.GetExecutionOrder(script) != order)
        {
            MonoImporter.SetExecutionOrder(script, order);
            Debug.Log($"[Setup] {typeof(T).Name} execution order = {order}");
        }
    }

    static MonoScript FindMonoScript<T>()
    {
        string[] guids = AssetDatabase.FindAssets($"t:MonoScript {typeof(T).Name}");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (script != null && script.GetClass() == typeof(T))
                return script;
        }
        return null;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        string folder = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
