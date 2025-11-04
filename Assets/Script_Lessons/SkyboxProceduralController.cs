using UnityEngine;

namespace YourNamespace
{
    [ExecuteAlways]
    public class SkyboxProceduralController : MonoBehaviour
    {
        [Header("Sun")]
        [Range(0f, 1f)]
        public float sunSize = 0.04f;
        
        [Range(1f, 10f)]
        public float sunSizeConvergence = 5f;
        
        [Header("Atmosphere")]
        [Range(0f, 5f)]
        public float atmosphereThickness = 1f;
        
        public Color skyTint = new Color(0.5f, 0.5f, 0.5f, 1f);
        
        public Color groundColor = new Color(0.369f, 0.349f, 0.341f, 1f);
        
        [Header("Lighting")]
        [Range(0f, 8f)]
        public float exposure = 1.3f;
        
        private Material skyboxMaterial;
        
        // Shader property IDs for performance
        private static readonly int SunSizeID = Shader.PropertyToID("_SunSize");
        private static readonly int SunSizeConvergenceID = Shader.PropertyToID("_SunSizeConvergence");
        private static readonly int AtmosphereThicknessID = Shader.PropertyToID("_AtmosphereThickness");
        private static readonly int SkyTintID = Shader.PropertyToID("_SkyTint");
        private static readonly int GroundColorID = Shader.PropertyToID("_GroundColor");
        private static readonly int ExposureID = Shader.PropertyToID("_Exposure");

        private void OnEnable()
        {
            skyboxMaterial = RenderSettings.skybox;
            
            if (skyboxMaterial == null)
            {
                Debug.LogWarning("No skybox material found in RenderSettings.");
                return;
            }
            
            UpdateSkybox();
        }

        private void Update()
        {
            UpdateSkybox();
        }

        private void UpdateSkybox()
        {
            if (skyboxMaterial == null) return;
            
            skyboxMaterial.SetFloat(SunSizeID, sunSize);
            skyboxMaterial.SetFloat(SunSizeConvergenceID, sunSizeConvergence);
            skyboxMaterial.SetFloat(AtmosphereThicknessID, atmosphereThickness);
            skyboxMaterial.SetColor(SkyTintID, skyTint);
            skyboxMaterial.SetColor(GroundColorID, groundColor);
            skyboxMaterial.SetFloat(ExposureID, exposure);
        }
    }
}