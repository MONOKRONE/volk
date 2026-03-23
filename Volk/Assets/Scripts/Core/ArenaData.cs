using UnityEngine;

namespace Volk.Core
{
    [CreateAssetMenu(fileName = "NewArena", menuName = "VOLK/Arena Data")]
    public class ArenaData : ScriptableObject
    {
        [Header("Identity")]
        public string arenaName;
        [TextArea(2, 3)] public string description;
        public Sprite thumbnail;

        [Header("Floor")]
        public Color floorColor = Color.gray;
        public float floorMetallic = 0.3f;
        public float floorSmoothness = 0.4f;

        [Header("Walls")]
        public Color wallColor = new Color(0.2f, 0.2f, 0.2f);

        [Header("Lighting")]
        public Color ambientColor = new Color(0.1f, 0.1f, 0.15f);
        public Color mainLightColor = Color.white;
        public float mainLightIntensity = 1f;
        public Color[] accentLightColors;
        public float accentLightIntensity = 2f;

        [Header("Skybox")]
        public Color skyboxTop = Color.black;
        public Color skyboxBottom = new Color(0.05f, 0.05f, 0.1f);
        public float skyboxExposure = 0.5f;

        [Header("Post-Processing")]
        public float bloomIntensity = 0.5f;
        public float bloomThreshold = 1.2f;
        public float vignetteIntensity = 0.3f;
        public Color colorFilterTint = Color.white;
        public float contrast = 0f;
        public float saturation = 0f;

        [Header("Particles")]
        public ParticleType particleType = ParticleType.None;
        public Color particleColor = new Color(1f, 1f, 1f, 0.3f);
        public float particleRate = 10f;
        public float particleSize = 0.1f;
        public float particleSpeed = 0.5f;

        [Header("Fog")]
        public bool fogEnabled = false;
        public Color fogColor = new Color(0.1f, 0.1f, 0.15f, 1f);
        public float fogDensity = 0.02f;
    }

    public enum ParticleType
    {
        None,
        Fog,
        Dust,
        Stars,
        Sparks,
        SlowFog
    }
}
