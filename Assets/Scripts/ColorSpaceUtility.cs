#if UNITY_EDITOR
using UnityEngine;

namespace Dancerex.Utils.Colors
{
    /// <summary>
    /// Utility for color space conversions
    /// </summary>
    public static class ColorSpaceUtility
    {
        /// <summary>
        /// Converts RGB color to LCH (Lightness, Chroma, Hue) color space
        /// </summary>
        /// <returns>Vector3 where x=Lightness (0-100), y=Chroma (0-100+), z=Hue (0-360)</returns>
        public static Vector3 RGBToLCH(Color rgb)
        {
            // Convert RGB to HSV first (Unity built-in)
            Color.RGBToHSV(rgb, out float h, out float s, out float v);

            // Approximate LCH from HSV
            // L (Lightness) - roughly corresponds to V (value) but adjusted
            float l = v * 100f;

            // C (Chroma) - saturation and value combined
            float c = s * v * 100f;

            // H (Hue) - same as HSV hue, but in degrees
            float hue = h * 360f;

            return new Vector3(l, c, hue);
        }

        /// <summary>
        /// Converts LCH to RGB color
        /// </summary>
        public static Color LCHToRGB(Vector3 lch)
        {
            float l = lch.x; // 0-100
            float c = lch.y; // 0-100+
            float h = lch.z; // 0-360

            // Convert back to HSV approximation
            float v = l / 100f;
            float s = v > 0 ? Mathf.Clamp01(c / (v * 100f)) : 0;
            float hue = h / 360f;

            return Color.HSVToRGB(hue, s, v);
        }
    }
}
#endif
