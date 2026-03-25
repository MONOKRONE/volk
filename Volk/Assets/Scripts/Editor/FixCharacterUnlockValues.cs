using UnityEngine;
using UnityEditor;
using Volk.Core;

public class FixCharacterUnlockValues
{
    [MenuItem("VOLK/Fix Character Unlock Values")]
    public static void Fix()
    {
        var fixes = new (string name, UnlockCondition type, int val)[]
        {
            ("RUZGAR", UnlockCondition.StoryProgress, 4),
            ("CELIK",  UnlockCondition.StoryProgress, 6),
            ("SIS",    UnlockCondition.StoryProgress, 8),
            ("TOPRAK", UnlockCondition.StoryProgress, 10),
        };

        foreach (var (name, unlockType, unlockVal) in fixes)
        {
            string path = $"Assets/ScriptableObjects/Characters/{name}.asset";
            var data = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            if (data == null)
            {
                Debug.LogWarning($"[Fix] Character not found: {path}");
                continue;
            }

            data.unlockType = unlockType;
            data.unlockValue = unlockVal;
            data.unlockedByDefault = false;
            EditorUtility.SetDirty(data);
            Debug.Log($"[Fix] {name}: unlockType={unlockType}, unlockValue={unlockVal}");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[Fix] Character unlock values fixed!");
    }
}
