using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Volk.Core
{
    public class ArenaManager : MonoBehaviour
    {
        public static ArenaManager Instance { get; private set; }

        [Header("Arena References")]
        public Renderer floorRenderer;
        public Renderer[] wallRenderers;
        public Light mainLight;
        public Light[] accentLights;
        public ParticleSystem arenaParticles;
        public Volume postProcessVolume;

        [Header("Current Arena")]
        public ArenaData currentArena;

        private Material floorMat;
        private Material[] wallMats;
        private Material skyboxMat;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            // Check if GameSettings has arena selection
            if (GameSettings.Instance != null && GameSettings.Instance.selectedArena != null)
                currentArena = GameSettings.Instance.selectedArena;

            if (currentArena != null)
                ApplyArena(currentArena);
        }

        public void ApplyArena(ArenaData arena)
        {
            currentArena = arena;
            ApplyFloor(arena);
            ApplyWalls(arena);
            ApplyLighting(arena);
            ApplySkybox(arena);
            ApplyPostProcessing(arena);
            ApplyParticles(arena);
            ApplyFog(arena);
        }

        void ApplyFloor(ArenaData arena)
        {
            if (floorRenderer == null) return;
            floorMat = new Material(floorRenderer.sharedMaterial);
            floorMat.color = arena.floorColor;
            floorMat.SetFloat("_Metallic", arena.floorMetallic);
            floorMat.SetFloat("_Smoothness", arena.floorSmoothness);
            floorRenderer.material = floorMat;
        }

        void ApplyWalls(ArenaData arena)
        {
            if (wallRenderers == null) return;

            // Cleanup old wall materials
            if (wallMats != null)
                foreach (var m in wallMats) if (m != null) Destroy(m);

            wallMats = new Material[wallRenderers.Length];
            for (int i = 0; i < wallRenderers.Length; i++)
            {
                if (wallRenderers[i] == null) continue;
                wallMats[i] = new Material(wallRenderers[i].sharedMaterial);
                wallMats[i].color = arena.wallColor;
                wallRenderers[i].material = wallMats[i];
            }
        }

        void ApplyLighting(ArenaData arena)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = arena.ambientColor;

            if (mainLight != null)
            {
                mainLight.color = arena.mainLightColor;
                mainLight.intensity = arena.mainLightIntensity;
            }

            if (accentLights != null && arena.accentLightColors != null)
            {
                for (int i = 0; i < accentLights.Length; i++)
                {
                    if (accentLights[i] == null) continue;
                    if (i < arena.accentLightColors.Length)
                    {
                        accentLights[i].color = arena.accentLightColors[i];
                        accentLights[i].intensity = arena.accentLightIntensity;
                        accentLights[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        accentLights[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        void ApplySkybox(ArenaData arena)
        {
            var shader = Shader.Find("Skybox/Procedural");
            if (shader == null) return;
            skyboxMat = new Material(shader);
            if (skyboxMat != null)
            {
                skyboxMat.SetColor("_SkyTint", arena.skyboxTop);
                skyboxMat.SetColor("_GroundColor", arena.skyboxBottom);
                skyboxMat.SetFloat("_Exposure", arena.skyboxExposure);
                RenderSettings.skybox = skyboxMat;
            }
        }

        void ApplyPostProcessing(ArenaData arena)
        {
            if (postProcessVolume == null) return;
            var profile = postProcessVolume.profile;
            if (profile == null) return;

            if (profile.TryGet(out Bloom bloom))
            {
                bloom.intensity.value = arena.bloomIntensity;
                bloom.threshold.value = arena.bloomThreshold;
            }

            if (profile.TryGet(out Vignette vignette))
            {
                vignette.intensity.value = arena.vignetteIntensity;
            }

            if (profile.TryGet(out ColorAdjustments color))
            {
                color.colorFilter.value = arena.colorFilterTint;
                color.contrast.value = arena.contrast;
                color.saturation.value = arena.saturation;
            }
        }

        void ApplyParticles(ArenaData arena)
        {
            if (arenaParticles == null) return;

            if (arena.particleType == ParticleType.None)
            {
                arenaParticles.Stop();
                return;
            }

            var main = arenaParticles.main;
            main.startColor = arena.particleColor;
            main.startSize = arena.particleSize;
            main.startSpeed = arena.particleSpeed;

            var emission = arenaParticles.emission;
            emission.rateOverTime = arena.particleRate;

            // Adjust shape and behavior based on type
            var shape = arenaParticles.shape;
            switch (arena.particleType)
            {
                case ParticleType.Fog:
                    main.startLifetime = 8f;
                    main.startSpeed = 0.2f;
                    main.startSize = 2f;
                    shape.shapeType = ParticleSystemShapeType.Box;
                    shape.scale = new Vector3(10f, 0.5f, 10f);
                    break;
                case ParticleType.Dust:
                    main.startLifetime = 5f;
                    main.startSpeed = 0.3f;
                    main.startSize = 0.05f;
                    shape.shapeType = ParticleSystemShapeType.Box;
                    shape.scale = new Vector3(8f, 4f, 8f);
                    break;
                case ParticleType.Stars:
                    main.startLifetime = 3f;
                    main.startSpeed = 0f;
                    main.startSize = 0.03f;
                    shape.shapeType = ParticleSystemShapeType.Hemisphere;
                    shape.radius = 15f;
                    break;
                case ParticleType.Sparks:
                    main.startLifetime = 1.5f;
                    main.startSpeed = 2f;
                    main.startSize = 0.04f;
                    main.gravityModifier = 0.5f;
                    shape.shapeType = ParticleSystemShapeType.Cone;
                    shape.angle = 30f;
                    break;
                case ParticleType.SlowFog:
                    main.startLifetime = 12f;
                    main.startSpeed = 0.1f;
                    main.startSize = 3f;
                    shape.shapeType = ParticleSystemShapeType.Box;
                    shape.scale = new Vector3(12f, 0.3f, 12f);
                    break;
            }

            arenaParticles.Play();
        }

        void ApplyFog(ArenaData arena)
        {
            RenderSettings.fog = arena.fogEnabled;
            if (arena.fogEnabled)
            {
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = arena.fogColor;
                RenderSettings.fogDensity = arena.fogDensity;
            }
        }

        void OnDestroy()
        {
            if (floorMat != null) Destroy(floorMat);
            if (skyboxMat != null) Destroy(skyboxMat);
            if (wallMats != null)
                foreach (var m in wallMats) if (m != null) Destroy(m);
        }
    }
}
