using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace YourNamespace
{
    [ExecuteAlways]
    public class PostProcessController : MonoBehaviour
    {
        [Header("Volume Reference")]
        [Tooltip("The Volume component to control. If not set, will search on this GameObject.")]
        public Volume volume;

        [Header("Bloom")]
        [Range(0f, 10f)]
        [Tooltip("Threshold for bloom effect")]
        public float bloomThreshold = 1f;

        [Range(0f, 10f)]
        [Tooltip("Intensity of bloom effect")]
        public float bloomIntensity = 1f;

        [Range(0f, 1f)]
        [Tooltip("Scatter/spread of bloom")]
        public float bloomScatter = 0.7f;

        [ColorUsage(false, true)]
        [Tooltip("Tint color for bloom")]
        public Color bloomTint = Color.white;

        [Header("Color Adjustments")]
        [Range(-5f, 5f)]
        [Tooltip("Post exposure adjustment")]
        public float postExposure = 0f;

        [Range(-100f, 100f)]
        [Tooltip("Color saturation")]
        public float saturation = 0f;

        [Header("Chromatic Aberration")]
        [Range(0f, 1f)]
        [Tooltip("Intensity of chromatic aberration")]
        public float chromaticAberrationIntensity = 0f;

        [Header("Vignette")]
        [Range(0f, 1f)]
        [Tooltip("Intensity of vignette effect")]
        public float vignetteIntensity = 0f;

        [Header("Depth of Field")]
        [Range(0.1f, 100f)]
        [Tooltip("Focus distance for depth of field")]
        public float focusDistance = 10f;

        private VolumeProfile profile;
        private Bloom bloom;
        private ColorAdjustments colorAdjustments;
        private ChromaticAberration chromaticAberration;
        private Vignette vignette;
        private DepthOfField depthOfField;

        private void OnEnable()
        {
            InitializeVolume();
            ApplySettings();
        }

        private void Update()
        {
            ApplySettings();
        }

        private void InitializeVolume()
        {
            if (!volume)
            {
                volume = GetComponent<Volume>();
            }

            if (volume && volume.profile)
            {
                profile = volume.profile;

                // Cache volume components
                profile.TryGet(out bloom);
                profile.TryGet(out colorAdjustments);
                profile.TryGet(out chromaticAberration);
                profile.TryGet(out vignette);
                profile.TryGet(out depthOfField);

                // Ensure Depth of Field is set to Bokeh mode
                if (depthOfField != null)
                {
                    depthOfField.mode.value = DepthOfFieldMode.Bokeh;
                }
            }
        }

        private void ApplySettings()
        {
            if (!volume || !profile) return;

            // Apply Bloom settings
            if (bloom != null)
            {
                bloom.threshold.value = bloomThreshold;
                bloom.intensity.value = bloomIntensity;
                bloom.scatter.value = bloomScatter;
                bloom.tint.value = bloomTint;
            }

            // Apply Color Adjustments
            if (colorAdjustments != null)
            {
                colorAdjustments.postExposure.value = postExposure;
                colorAdjustments.saturation.value = saturation;
            }

            // Apply Chromatic Aberration
            if (chromaticAberration != null)
            {
                chromaticAberration.intensity.value = chromaticAberrationIntensity;
            }

            // Apply Vignette
            if (vignette != null)
            {
                vignette.intensity.value = vignetteIntensity;
            }

            // Apply Depth of Field (Bokeh)
            if (depthOfField != null)
            {
                depthOfField.focusDistance.value = focusDistance;
            }
        }
    }
}
