using UnityEngine;
using UnityEditor;
using System.IO;

public class SetupFonts
{
    [MenuItem("VOLK/Setup Font Assets")]
    static void SetupFontAssets()
    {
        string[] fontPaths = {
            "Assets/Fonts/Rajdhani",
            "Assets/Fonts/Inter"
        };

        int found = 0;

        foreach (var dir in fontPaths)
        {
            if (!AssetDatabase.IsValidFolder(dir))
            {
                Debug.LogWarning($"[VOLK] Font folder not found: {dir}");
                continue;
            }

            string[] guids = AssetDatabase.FindAssets("t:Font", new[] { dir });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Font font = AssetDatabase.LoadAssetAtPath<Font>(path);
                if (font != null)
                {
                    found++;
                    Debug.Log($"[VOLK] Font ready: {font.name} ({path})");
                }
            }
        }

        if (found > 0)
            Debug.Log($"[VOLK] {found} font assets found. Create TMP SDF assets via Window > TextMeshPro > Font Asset Creator.");
        else
            Debug.LogWarning("[VOLK] No font files found in Assets/Fonts/Rajdhani/ or Assets/Fonts/Inter/.");
    }
}
