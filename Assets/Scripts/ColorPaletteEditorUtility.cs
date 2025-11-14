#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using Dancerex.Utils.Colors;

namespace Dancerex.Editor.Utils.Colors
{
    /// <summary>
    /// Shared utility methods for color palette editing functionality.
    /// Used by both ColorPaletteEditor and PaletteEditorWindow.
    /// </summary>
    public static class ColorPaletteEditorUtility
    {
        // Layout constants
        public const float COLOR_SWATCH_SIZE = 40f;
        public const float COLOR_SPACING = 5f;
        public const float TONAL_STRIP_HEIGHT = 30f;
        public const float HARMONY_STRIP_HEIGHT = 35f;
        
        #region Color Strip Drawing
        
        /// <summary>
        /// Draw a horizontal strip of color swatches
        /// </summary>
        public static void DrawColorStrip(List<Color> colors, float height = TONAL_STRIP_HEIGHT, bool drawBorder = true)
        {
            if (colors == null || colors.Count == 0) return;
            
            Rect stripRect = EditorGUILayout.GetControlRect(false, height);
            DrawColorStripInRect(stripRect, colors, drawBorder);
        }
        
        /// <summary>
        /// Draw a horizontal strip of color swatches in a specific rect
        /// </summary>
        public static void DrawColorStripInRect(Rect rect, List<Color> colors, bool drawBorder = true)
        {
            if (colors == null || colors.Count == 0) return;
            
            float swatchWidth = rect.width / colors.Count;
            
            for (int i = 0; i < colors.Count; i++)
            {
                Rect swatchRect = new Rect(
                    rect.x + i * swatchWidth,
                    rect.y,
                    swatchWidth,
                    rect.height
                );
                
                EditorGUI.DrawRect(swatchRect, colors[i]);
                
                // Draw thin separator between colors
                if (i > 0)
                {
                    var separatorRect = new Rect(swatchRect.x - 1, swatchRect.y, 1, swatchRect.height);
                    EditorGUI.DrawRect(separatorRect, new Color(0, 0, 0, 0.2f));
                }
            }
            
            // Draw border
            if (drawBorder)
            {
                GUI.color = new Color(0, 0, 0, 0.3f);
                GUI.Box(rect, GUIContent.none);
                GUI.color = Color.white;
            }
        }
        
        /// <summary>
        /// Draw a single color swatch with optional label
        /// </summary>
        public static void DrawColorSwatch(Rect rect, Color color, string label = null, bool selected = false)
        {
            // Draw selection highlight
            if (selected)
            {
                Rect highlightRect = new Rect(rect.x - 2, rect.y - 2, rect.width + 4, rect.height + 4);
                EditorGUI.DrawRect(highlightRect, new Color(0.2f, 0.5f, 1f, 1f));
            }
            
            // Draw color
            EditorGUI.DrawRect(rect, color);
            
            // Draw border
            GUI.color = new Color(0, 0, 0, 0.3f);
            GUI.Box(rect, GUIContent.none);
            GUI.color = Color.white;
            
            // Draw label if provided
            if (!string.IsNullOrEmpty(label))
            {
                var labelRect = new Rect(rect.x, rect.y + rect.height - 16, rect.width, 16);
                var style = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = color.grayscale > 0.5f ? Color.black : Color.white }
                };
                EditorGUI.LabelField(labelRect, label, style);
            }
        }
        
        #endregion
        
        #region Palette Manipulation
        
        /// <summary>
        /// Update a ColorPalette asset with new colors using reflection
        /// </summary>
        public static void UpdatePaletteColors(ColorPalette palette, List<Color> colors, List<string> names = null)
        {
            if (palette == null || colors == null) return;
            
            var paletteType = palette.GetType();
            var colorsField = paletteType.GetField("colors", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (colorsField != null)
            {
                var colorEntries = new ColorPalette.ColorEntry[colors.Count];
                for (int i = 0; i < colors.Count; i++)
                {
                    colorEntries[i] = new ColorPalette.ColorEntry
                    {
                        name = names != null && i < names.Count ? names[i] : $"Color{(i + 1):00}",
                        color = colors[i]
                    };
                }
                colorsField.SetValue(palette, colorEntries);
            }
        }
        
        /// <summary>
        /// Update palette name using reflection
        /// </summary>
        public static void UpdatePaletteName(ColorPalette palette, string name)
        {
            if (palette == null) return;
            
            var paletteType = palette.GetType();
            var paletteNameField = paletteType.GetField("paletteName",
                BindingFlags.NonPublic | BindingFlags.Instance);
                
            if (paletteNameField != null)
            {
                paletteNameField.SetValue(palette, name);
            }
        }
        
        #endregion
        
        #region Color Naming
        
        /// <summary>
        /// Generate a name for a tonal variation
        /// </summary>
        public static string GenerateTonalVariationName(string baseName, Color baseColor, Color variation)
        {
            float baseLightness = ColorSpaceUtility.RGBToLCH(baseColor).x;
            float variationLightness = ColorSpaceUtility.RGBToLCH(variation).x;
            
            if (Mathf.Abs(variationLightness - baseLightness) < 5f)
                return baseName; // Too similar, use base name
            
            string suffix;
            if (variationLightness > baseLightness)
            {
                int level = Mathf.Clamp((int)((variationLightness - baseLightness) / 10), 1, 5);
                suffix = $"Light{level}";
            }
            else
            {
                int level = Mathf.Clamp((int)((baseLightness - variationLightness) / 10), 1, 5);
                suffix = $"Dark{level}";
            }
            
            return $"{baseName}_{suffix}";
        }
        
        /// <summary>
        /// Generate harmony color names
        /// </summary>
        public static string GenerateHarmonyColorName(string baseName, string harmonyType, int index = 0)
        {
            if (index == 0)
                return $"{baseName}_{harmonyType}";
            else
                return $"{baseName}_{harmonyType}{index + 1}";
        }
        
        #endregion
        
        #region Harmony UI Helpers
        
        /// <summary>
        /// Draw a compact harmony preview with action buttons
        /// </summary>
        public static void DrawHarmonyPreview(string label, List<Color> colors, System.Action onAdd, float height = HARMONY_STRIP_HEIGHT)
        {
            EditorGUILayout.BeginVertical();
            
            // Label
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
            
            // Color strip
            DrawColorStrip(colors, height);
            
            // Add button
            if (GUILayout.Button($"Add {label}", EditorStyles.miniButton))
            {
                onAdd?.Invoke();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draw a single color preview with button
        /// </summary>
        public static void DrawSingleColorPreview(string label, Color color, System.Action onAdd)
        {
            EditorGUILayout.BeginHorizontal();
            
            // Label
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel, GUILayout.Width(80));
            
            // Color preview
            Rect colorRect = EditorGUILayout.GetControlRect(false, 20, GUILayout.Width(60));
            DrawColorSwatch(colorRect, color);
            
            GUILayout.FlexibleSpace();
            
            // Add button
            if (GUILayout.Button($"Add", EditorStyles.miniButton, GUILayout.Width(50)))
            {
                onAdd?.Invoke();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        #endregion
        
        #region Standard Colors
        
        /// <summary>
        /// Get a set of standard colors for quick palette creation
        /// </summary>
        public static List<(string name, Color color)> GetStandardColors()
        {
            return new List<(string, Color)>
            {
                ("Primary", new Color(0.2f, 0.3f, 0.4f, 1f)),
                ("Secondary", new Color(0.5f, 0.6f, 0.7f, 1f)),
                ("Accent1", new Color(0.9f, 0.4f, 0.3f, 1f)),
                ("Accent2", new Color(0.3f, 0.7f, 0.9f, 1f)),
                ("Light", new Color(0.95f, 0.95f, 0.9f, 1f)),
                ("Dark", new Color(0.1f, 0.1f, 0.15f, 1f)),
                ("Neutral", new Color(0.5f, 0.5f, 0.5f, 1f))
            };
        }
        
        #endregion
    }
}
#endif 