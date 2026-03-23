using UnityEngine;
using UnityEditor;
using Volk.Core;

public class CreateArenaAssets
{
    [MenuItem("VOLK/Create Arena Assets")]
    static void Create()
    {
        // 1. Sokak Arenasi — gece, neon
        var sokak = ScriptableObject.CreateInstance<ArenaData>();
        sokak.arenaName = "Sokak Arenasi";
        sokak.description = "Istanbul sokaklarinin karanlik kosesi";
        sokak.floorColor = new Color(0.15f, 0.15f, 0.18f);
        sokak.floorMetallic = 0.5f;
        sokak.floorSmoothness = 0.6f;
        sokak.wallColor = new Color(0.1f, 0.1f, 0.12f);
        sokak.ambientColor = new Color(0.05f, 0.02f, 0.05f);
        sokak.mainLightColor = new Color(0.6f, 0.5f, 0.7f);
        sokak.mainLightIntensity = 0.6f;
        sokak.accentLightColors = new[] { new Color(0.9f, 0.1f, 0.2f), new Color(0.9f, 0.1f, 0.2f) };
        sokak.accentLightIntensity = 3f;
        sokak.skyboxTop = new Color(0.02f, 0.01f, 0.05f);
        sokak.skyboxBottom = new Color(0.05f, 0.02f, 0.08f);
        sokak.skyboxExposure = 0.2f;
        sokak.bloomIntensity = 1.5f;
        sokak.bloomThreshold = 0.8f;
        sokak.vignetteIntensity = 0.4f;
        sokak.particleType = ParticleType.Fog;
        sokak.particleColor = new Color(0.3f, 0.1f, 0.15f, 0.15f);
        sokak.particleRate = 5f;
        sokak.fogEnabled = true;
        sokak.fogColor = new Color(0.05f, 0.02f, 0.05f);
        sokak.fogDensity = 0.03f;
        AssetDatabase.CreateAsset(sokak, "Assets/ScriptableObjects/Chapters/Arena_Sokak.asset");

        // 2. Yeralti Ring — beton, spot
        var yeralti = ScriptableObject.CreateInstance<ArenaData>();
        yeralti.arenaName = "Yeralti Ring";
        yeralti.description = "Beton duvarli yeralti dovus ringi";
        yeralti.floorColor = new Color(0.35f, 0.28f, 0.2f);
        yeralti.floorMetallic = 0.1f;
        yeralti.floorSmoothness = 0.2f;
        yeralti.wallColor = new Color(0.25f, 0.2f, 0.15f);
        yeralti.ambientColor = new Color(0.08f, 0.06f, 0.03f);
        yeralti.mainLightColor = new Color(1f, 0.9f, 0.5f);
        yeralti.mainLightIntensity = 1.2f;
        yeralti.accentLightColors = new[] { new Color(1f, 0.85f, 0.3f), new Color(1f, 0.85f, 0.3f) };
        yeralti.accentLightIntensity = 4f;
        yeralti.skyboxTop = Color.black;
        yeralti.skyboxBottom = new Color(0.03f, 0.02f, 0.01f);
        yeralti.skyboxExposure = 0.1f;
        yeralti.bloomIntensity = 0.8f;
        yeralti.bloomThreshold = 1.0f;
        yeralti.vignetteIntensity = 0.45f;
        yeralti.particleType = ParticleType.Dust;
        yeralti.particleColor = new Color(0.6f, 0.5f, 0.3f, 0.2f);
        yeralti.particleRate = 15f;
        AssetDatabase.CreateAsset(yeralti, "Assets/ScriptableObjects/Chapters/Arena_Yeralti.asset");

        // 3. Cati Kati — gece gokyuzu
        var cati = ScriptableObject.CreateInstance<ArenaData>();
        cati.arenaName = "Cati Kati";
        cati.description = "Istanbul'un uzerinde, yildizlarin altinda";
        cati.floorColor = new Color(0.1f, 0.12f, 0.2f);
        cati.floorMetallic = 0.4f;
        cati.floorSmoothness = 0.5f;
        cati.wallColor = new Color(0.08f, 0.1f, 0.15f);
        cati.ambientColor = new Color(0.03f, 0.04f, 0.08f);
        cati.mainLightColor = new Color(0.7f, 0.8f, 1f);
        cati.mainLightIntensity = 0.8f;
        cati.accentLightColors = new[] { new Color(0.5f, 0.7f, 1f), new Color(0.8f, 0.9f, 1f) };
        cati.accentLightIntensity = 2f;
        cati.skyboxTop = new Color(0.02f, 0.03f, 0.1f);
        cati.skyboxBottom = new Color(0.05f, 0.05f, 0.15f);
        cati.skyboxExposure = 0.3f;
        cati.bloomIntensity = 0.6f;
        cati.bloomThreshold = 1.1f;
        cati.vignetteIntensity = 0.25f;
        cati.particleType = ParticleType.Stars;
        cati.particleColor = new Color(1f, 1f, 1f, 0.5f);
        cati.particleRate = 30f;
        AssetDatabase.CreateAsset(cati, "Assets/ScriptableObjects/Chapters/Arena_Cati.asset");

        // 4. Fabrika — pasli, floresan
        var fabrika = ScriptableObject.CreateInstance<ArenaData>();
        fabrika.arenaName = "Terk Edilmis Fabrika";
        fabrika.description = "Pasli metal ve floresan isiklar";
        fabrika.floorColor = new Color(0.4f, 0.25f, 0.1f);
        fabrika.floorMetallic = 0.6f;
        fabrika.floorSmoothness = 0.3f;
        fabrika.wallColor = new Color(0.3f, 0.2f, 0.1f);
        fabrika.ambientColor = new Color(0.05f, 0.08f, 0.03f);
        fabrika.mainLightColor = new Color(0.5f, 0.9f, 0.4f);
        fabrika.mainLightIntensity = 0.9f;
        fabrika.accentLightColors = new[] { new Color(0.3f, 0.8f, 0.2f), new Color(0.4f, 0.7f, 0.3f) };
        fabrika.accentLightIntensity = 2.5f;
        fabrika.skyboxTop = new Color(0.02f, 0.03f, 0.01f);
        fabrika.skyboxBottom = new Color(0.05f, 0.04f, 0.02f);
        fabrika.skyboxExposure = 0.15f;
        fabrika.bloomIntensity = 1.0f;
        fabrika.bloomThreshold = 0.9f;
        fabrika.vignetteIntensity = 0.35f;
        fabrika.colorFilterTint = new Color(0.9f, 1f, 0.8f);
        fabrika.particleType = ParticleType.Sparks;
        fabrika.particleColor = new Color(1f, 0.7f, 0.2f, 0.8f);
        fabrika.particleRate = 8f;
        AssetDatabase.CreateAsset(fabrika, "Assets/ScriptableObjects/Chapters/Arena_Fabrika.asset");

        // 5. Bogaz — aksam, sis
        var bogaz = ScriptableObject.CreateInstance<ArenaData>();
        bogaz.arenaName = "Bogaz Kiyisi";
        bogaz.description = "Istanbul Bogazinin gizemli kiyisi";
        bogaz.floorColor = new Color(0.1f, 0.2f, 0.22f);
        bogaz.floorMetallic = 0.3f;
        bogaz.floorSmoothness = 0.5f;
        bogaz.wallColor = new Color(0.08f, 0.15f, 0.18f);
        bogaz.ambientColor = new Color(0.1f, 0.06f, 0.03f);
        bogaz.mainLightColor = new Color(1f, 0.65f, 0.3f);
        bogaz.mainLightIntensity = 1.0f;
        bogaz.accentLightColors = new[] { new Color(1f, 0.5f, 0.2f), new Color(0.9f, 0.6f, 0.3f) };
        bogaz.accentLightIntensity = 1.5f;
        bogaz.skyboxTop = new Color(0.05f, 0.03f, 0.1f);
        bogaz.skyboxBottom = new Color(0.15f, 0.08f, 0.03f);
        bogaz.skyboxExposure = 0.4f;
        bogaz.bloomIntensity = 0.7f;
        bogaz.bloomThreshold = 1.0f;
        bogaz.vignetteIntensity = 0.3f;
        bogaz.colorFilterTint = new Color(1f, 0.9f, 0.85f);
        bogaz.particleType = ParticleType.SlowFog;
        bogaz.particleColor = new Color(0.5f, 0.5f, 0.6f, 0.1f);
        bogaz.particleRate = 3f;
        bogaz.fogEnabled = true;
        bogaz.fogColor = new Color(0.12f, 0.08f, 0.06f);
        bogaz.fogDensity = 0.02f;
        AssetDatabase.CreateAsset(bogaz, "Assets/ScriptableObjects/Chapters/Arena_Bogaz.asset");

        AssetDatabase.SaveAssets();
        Debug.Log("[VOLK] 5 arena assets created!");
    }
}
