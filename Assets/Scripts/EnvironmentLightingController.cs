using UnityEngine;

namespace YourNamespace
{
    [ExecuteAlways]
    public class EnvironmentLightingController : MonoBehaviour
    {
        [Header("Environment Lighting")]
        [Range(0f, 8f)]
        [Tooltip("Multiplier for ambient lighting intensity")]
        public float lightingIntensityMultiplier = 1f;

        [Header("Reflections")]
        [Range(0f, 2f)]
        [Tooltip("Multiplier for reflection probe intensity")]
        public float reflectionsIntensityMultiplier = 1f;

        [Header("Fog")]
        [Range(0f, 1f)]
        [Tooltip("Density of exponential squared fog")]
        public float fogDensity = 0.01f;

        private void OnEnable()
        {
            InitializeSettings();
            ApplySettings();
        }

        private void Update()
        {
            ApplySettings();
        }

        private void InitializeSettings()
        {
            // Set ambient source to skybox if not already set
            if (RenderSettings.ambientMode != UnityEngine.Rendering.AmbientMode.Skybox)
            {
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            }

            // Set default reflection source to skybox if not already set
            if (RenderSettings.defaultReflectionMode != UnityEngine.Rendering.DefaultReflectionMode.Skybox)
            {
                RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
            }

            // Enable fog and set to exponential squared
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
        }

        private void ApplySettings()
        {
            RenderSettings.ambientIntensity = lightingIntensityMultiplier;
            RenderSettings.reflectionIntensity = reflectionsIntensityMultiplier;
            RenderSettings.fogDensity = fogDensity;
        }
    }
}
