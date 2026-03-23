using UnityEngine;
using UnityEditor;
using System.IO;

public class SetupFonts
{
    [MenuItem("VOLK/Import Fonts from Downloads")]
    static void ImportFonts()
    {
        string fontsDir = "Assets/UI/Fonts";
        if (!AssetDatabase.IsValidFolder(fontsDir))
        {
            AssetDatabase.CreateFolder("Assets/UI", "Fonts");
        }

        string downloads = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), "Downloads");
        int imported = 0;

        // Search for Rajdhani and Inter font files
        string[] patterns = { "Rajdhani*.ttf", "Rajdhani*.otf", "Inter*.ttf", "Inter*.otf",
                              "rajdhani*.ttf", "rajdhani*.otf", "inter*.ttf", "inter*.otf" };

        foreach (var pattern in patterns)
        {
            string[] files = Directory.GetFiles(downloads, pattern, SearchOption.AllDirectories);
            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);
                string destPath = Path.Combine(Application.dataPath, "UI/Fonts", fileName);
                if (!File.Exists(destPath))
                {
                    File.Copy(file, destPath);
                    imported++;
                    Debug.Log($"[Fonts] Imported: {fileName}");
                }
            }
        }

        AssetDatabase.Refresh();

        if (imported > 0)
            Debug.Log($"[VOLK] {imported} font files imported! Now create TMP SDF assets via Window > TextMeshPro > Font Asset Creator.");
        else
            Debug.LogWarning("[VOLK] No font files found in Downloads. Place Rajdhani and Inter .ttf/.otf files in ~/Downloads/");
    }
}
