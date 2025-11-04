#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace Dancerex.Utils.Colors
{
    /// <summary>
    /// Utility for generating descriptive color names
    /// </summary>
    public static class ColorNameGenerator
    {
        private static readonly Dictionary<float, string> HueNames = new Dictionary<float, string>
        {
            { 0, "Red" },
            { 15, "RedOrange" },
            { 30, "Orange" },
            { 45, "YellowOrange" },
            { 60, "Yellow" },
            { 75, "YellowGreen" },
            { 90, "Lime" },
            { 120, "Green" },
            { 150, "Cyan" },
            { 180, "Teal" },
            { 210, "SkyBlue" },
            { 240, "Blue" },
            { 270, "Purple" },
            { 300, "Magenta" },
            { 330, "Pink" }
        };

        /// <summary>
        /// Generate a single color name
        /// </summary>
        public static string GenerateName(Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);

            // Handle grayscale
            if (s < 0.1f)
            {
                if (v > 0.9f) return "White";
                if (v < 0.2f) return "Black";
                if (v > 0.7f) return "LightGray";
                if (v < 0.4f) return "DarkGray";
                return "Gray";
            }

            // Get hue name
            string hueName = GetHueName(h * 360f);

            // Add lightness modifier
            if (v < 0.4f)
                return "Dark" + hueName;
            else if (v > 0.8f && s < 0.5f)
                return "Light" + hueName;
            else if (s < 0.3f)
                return "Pale" + hueName;

            return hueName;
        }

        /// <summary>
        /// Generate names for multiple colors
        /// </summary>
        public static List<string> GenerateNames(List<Color> colors)
        {
            var names = new List<string>();
            var usedNames = new HashSet<string>();

            foreach (var color in colors)
            {
                string baseName = GenerateName(color);
                string finalName = baseName;
                int counter = 2;

                while (usedNames.Contains(finalName))
                {
                    finalName = $"{baseName}{counter}";
                    counter++;
                }

                names.Add(finalName);
                usedNames.Add(finalName);
            }

            return names;
        }

        /// <summary>
        /// Suggest a palette name based on colors
        /// </summary>
        public static string SuggestPaletteName(List<Color> colors)
        {
            if (colors == null || colors.Count == 0)
                return "Empty Palette";

            // Check if it's a monochromatic palette
            bool isMonochromatic = true;
            Color.RGBToHSV(colors[0], out float baseHue, out _, out _);

            foreach (var color in colors)
            {
                Color.RGBToHSV(color, out float h, out float s, out _);
                if (s > 0.1f && Mathf.Abs(h - baseHue) > 0.1f)
                {
                    isMonochromatic = false;
                    break;
                }
            }

            if (isMonochromatic)
            {
                string hueName = GetHueName(baseHue * 360f);
                return $"{hueName} Monochrome";
            }

            // Check for warm/cool palette
            int warmCount = 0;
            int coolCount = 0;

            foreach (var color in colors)
            {
                Color.RGBToHSV(color, out float h, out float s, out _);
                if (s < 0.1f) continue; // Skip grays

                float hue = h * 360f;
                if ((hue >= 0 && hue < 60) || (hue >= 300 && hue < 360))
                    warmCount++;
                else if (hue >= 180 && hue < 300)
                    coolCount++;
            }

            if (warmCount > coolCount * 2)
                return "Warm Palette";
            if (coolCount > warmCount * 2)
                return "Cool Palette";

            return "Mixed Palette";
        }

        private static string GetHueName(float hue)
        {
            float closestHue = 0;
            float minDiff = 360;

            foreach (var kvp in HueNames)
            {
                float diff = Mathf.Abs(hue - kvp.Key);
                if (diff > 180) diff = 360 - diff; // Wrap around

                if (diff < minDiff)
                {
                    minDiff = diff;
                    closestHue = kvp.Key;
                }
            }

            return HueNames[closestHue];
        }
    }
}
#endif
