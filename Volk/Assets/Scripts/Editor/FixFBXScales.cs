#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class FixFBXScales : AssetPostprocessor
{
    // Run once via menu
    [MenuItem("VOLK/Cinematic/Fix FBX Import Scales")]
    static void FixScales()
    {
        var configs = new (string path, float scale)[]
        {
            ("Assets/Characters/YILDIZ", 0.1f),
            ("Assets/Characters/KAYA",   0.1f),
            ("Assets/Characters/RUZGAR", 0.1f),
            ("Assets/Characters/TOPRAK", 0.1f),
            ("Assets/Characters/CELIK",  1.0f),
            ("Assets/Characters/SIS",    10.0f),
        };

        foreach (var (folder, scale) in configs)
        {
            var guids = AssetDatabase.FindAssets("t:Model", new[] { folder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) continue;
                importer.globalScale = scale;
                importer.SaveAndReimport();
                Debug.Log($"[FBX Scale] {path} → {scale}");
            }
        }
        Debug.Log("[FBX Scale] Done! All FBX scales fixed.");
    }
}
#endif
