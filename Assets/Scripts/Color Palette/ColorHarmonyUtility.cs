#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace Dancerex.Utils.Colors
{
    /// <summary>
    /// Utility for generating color harmonies
    /// </summary>
    public static class ColorHarmonyUtility
    {
        /// <summary>
        /// Generates a tonal strip (lightness variations) of a base color
        /// </summary>
        public static List<Color> GenerateTonalStrip(Color baseColor, int count = 7)
        {
            var result = new List<Color>();
            Vector3 lch = ColorSpaceUtility.RGBToLCH(baseColor);

            float minLightness = 20f;
            float maxLightness = 90f;
            float step = (maxLightness - minLightness) / (count - 1);

            for (int i = 0; i < count; i++)
            {
                Vector3 newLch = new Vector3(
                    minLightness + (step * i),
                    lch.y, // Keep same chroma
                    lch.z  // Keep same hue
                );
                result.Add(ColorSpaceUtility.LCHToRGB(newLch));
            }

            return result;
        }

        /// <summary>
        /// Generates a complementary color (180° hue shift)
        /// </summary>
        public static Color GenerateComplementary(Color baseColor)
        {
            Vector3 lch = ColorSpaceUtility.RGBToLCH(baseColor);

            Vector3 complementLch = new Vector3(
                lch.x,
                lch.y,
                (lch.z + 180f) % 360f
            );

            return ColorSpaceUtility.LCHToRGB(complementLch);
        }

        /// <summary>
        /// Generates analogous colors (±30° hue shift)
        /// </summary>
        public static List<Color> GenerateAnalogous(Color baseColor, int countPerSide = 2)
        {
            var result = new List<Color>();
            Vector3 lch = ColorSpaceUtility.RGBToLCH(baseColor);

            float angleStep = 30f;

            // Colors on one side
            for (int i = 1; i <= countPerSide; i++)
            {
                Vector3 newLch = new Vector3(
                    lch.x,
                    lch.y,
                    (lch.z - angleStep * i + 360f) % 360f
                );
                result.Add(ColorSpaceUtility.LCHToRGB(newLch));
            }

            // Colors on the other side
            for (int i = 1; i <= countPerSide; i++)
            {
                Vector3 newLch = new Vector3(
                    lch.x,
                    lch.y,
                    (lch.z + angleStep * i) % 360f
                );
                result.Add(ColorSpaceUtility.LCHToRGB(newLch));
            }

            return result;
        }

        /// <summary>
        /// Generates triadic colors (120° hue shifts)
        /// </summary>
        public static List<Color> GenerateTriadic(Color baseColor)
        {
            var result = new List<Color>();
            Vector3 lch = ColorSpaceUtility.RGBToLCH(baseColor);

            // Add base color
            result.Add(baseColor);

            // Add two triadic colors
            for (int i = 1; i <= 2; i++)
            {
                Vector3 newLch = new Vector3(
                    lch.x,
                    lch.y,
                    (lch.z + 120f * i) % 360f
                );
                result.Add(ColorSpaceUtility.LCHToRGB(newLch));
            }

            return result;
        }
    }
}
#endif
