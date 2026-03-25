using UnityEngine;
using UnityEditor;
using Volk.Core;

public class CreatePaletteMaterials
{
    static readonly (string name, float hueShift, float saturation, float valueMod, Color tint)[] Palettes = new[]
    {
        ("YILDIZ", 45f,   1.0f,  0.1f,  new Color(1f, 0.9f, 0.5f)),   // gold/yellow
        ("KAYA",   0f,    1.2f,  0.0f,  new Color(1f, 0.4f, 0.3f)),   // red
        ("RUZGAR", 240f,  1.0f,  0.0f,  new Color(0.4f, 0.6f, 1f)),   // blue
        ("CELIK",  0f,    -0.5f, 0.15f, new Color(0.8f, 0.8f, 0.85f)),// silver/grey
        ("SIS",    270f,  0.8f,  -0.1f, new Color(0.7f, 0.4f, 1f)),   // purple
        ("TOPRAK", 30f,   0.9f,  -0.1f, new Color(0.7f, 0.5f, 0.3f)), // brown
    };

    [MenuItem("VOLK/Create Palette Materials")]
    public static void Create()
    {
        string dir = "Assets/Materials/Characters";
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/Materials", "Characters");

        Shader paletteShader = Shader.Find("VOLK/CharacterPaletteSwap");
        if (paletteShader == null)
        {
            Debug.LogError("[VOLK] Shader 'VOLK/CharacterPaletteSwap' not found. Import the shader first.");
            return;
        }

        foreach (var (name, hue, sat, val, tint) in Palettes)
        {
            string path = $"{dir}/{name}_Mat.mat";
            AssetDatabase.DeleteAsset(path);

            var mat = new Material(paletteShader);
            mat.SetFloat("_HueShift", hue);
            mat.SetFloat("_Saturation", sat);
            mat.SetFloat("_ValueMod", val);
            mat.SetColor("_Color", tint);
            mat.SetFloat("_Smoothness", 0.4f);
            mat.SetFloat("_Metallic", 0.05f);

            AssetDatabase.CreateAsset(mat, path);

            // Link to CharacterData if exists
            string charPath = $"Assets/ScriptableObjects/Characters/{name}.asset";
            var charData = AssetDatabase.LoadAssetAtPath<CharacterData>(charPath);
            if (charData != null)
            {
                charData.characterMaterial = mat;
                EditorUtility.SetDirty(charData);
            }

            Debug.Log($"[VOLK] Created material: {path}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[VOLK] 6 palette materials created!");
    }
}
