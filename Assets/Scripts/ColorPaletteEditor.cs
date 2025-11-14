#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using Dancerex.Utils.Colors;

namespace Dancerex.Editor.Utils.Colors
{
    [CustomEditor(typeof(ColorPalette))]
    public class ColorPaletteEditor : UnityEditor.Editor
    {
        private SerializedProperty _paletteName;
        private SerializedProperty _colors;

        private ReorderableList _colorList;
        
        // Color harmony visualization
        private int _selectedColorIndex = -1;
        private List<Color> _tonalStrip = new List<Color>();
        private List<Color> _analogousColors = new List<Color>();
        private List<Color> _triadicColors = new List<Color>();
        private Color _complementaryColor;
        
        private void OnEnable()
        {
            // Find properties
            _paletteName = serializedObject.FindProperty("paletteName");
            _colors = serializedObject.FindProperty("colors");

            // Create reorderable list
            _colorList = new ReorderableList(serializedObject, _colors, true, true, true, true)
            {
                drawHeaderCallback = DrawHeader,
                drawElementCallback = DrawColorElement,
                elementHeightCallback = GetElementHeight,
                onAddCallback = OnAddColor,
                onSelectCallback = OnSelectColor
            };
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            var palette = (ColorPalette)target;
            
            // Info box
            EditorGUILayout.HelpBox("Define colors for this scene's palette. Colors are applied to materials via the Scene Color Manager.", MessageType.Info);
            EditorGUILayout.Space(10);
            
            // Palette name
            EditorGUILayout.LabelField("Palette Info", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_paletteName, new GUIContent("Palette Name"));
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(10);
            
            // Color list
            EditorGUILayout.LabelField($"Colors ({_colors.arraySize})", EditorStyles.boldLabel);
            _colorList.DoLayoutList();
            
            // Color harmonies for selected color
            if (_selectedColorIndex >= 0 && _selectedColorIndex < _colors.arraySize)
            {
                EditorGUILayout.Space(10);
                EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), new Color(0.5f, 0.5f, 0.5f, 0.5f));
                EditorGUILayout.Space(5);
                DrawColorHarmoniesSection();
            }
            
            EditorGUILayout.Space(10);
            
            // Utility buttons
            DrawUtilityButtons();

            // Preview section
            EditorGUILayout.Space(10);
            DrawColorPreview(palette);
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawUtilityButtons()
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Add Standard Colors", EditorStyles.miniButton))
            {
                AddStandardColors();
            }
            
            if (GUILayout.Button("Sort by Name", EditorStyles.miniButton))
            {
                SortColorsByName();
            }

            if (GUILayout.Button("Clear All", EditorStyles.miniButton))
            {
                if (EditorUtility.DisplayDialog("Clear All Colors", 
                    "Are you sure you want to remove all colors?", "Yes", "No"))
                {
                    _colors.ClearArray();
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Second row of utility buttons - Auto naming
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Auto Name All", EditorStyles.miniButton))
            {
                AutoNameAllColors();
            }
            
            if (GUILayout.Button("Auto Name Selected", EditorStyles.miniButton))
            {
                if (_selectedColorIndex >= 0 && _selectedColorIndex < _colors.arraySize)
                {
                    AutoNameColor(_selectedColorIndex);
                }
            }
            
            if (GUILayout.Button("Suggest Palette Name", EditorStyles.miniButton))
            {
                SuggestPaletteName();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawHeader(Rect rect)
        {
            var nameRect = new Rect(rect.x, rect.y, rect.width * 0.4f, rect.height);
            var colorRect = new Rect(rect.x + rect.width * 0.45f, rect.y, rect.width * 0.55f, rect.height);
            
            EditorGUI.LabelField(nameRect, "Name");
            EditorGUI.LabelField(colorRect, "Color");
        }
        
        private void DrawColorElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = _colors.GetArrayElementAtIndex(index);
            var nameProperty = element.FindPropertyRelative("name");
            var colorProperty = element.FindPropertyRelative("color");
            
            rect.y += 2;
            rect.height -= 4;
            
            var nameRect = new Rect(rect.x, rect.y, rect.width * 0.4f, rect.height);
            var colorRect = new Rect(rect.x + rect.width * 0.45f, rect.y, rect.width * 0.5f, rect.height);
            
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(nameRect, nameProperty, GUIContent.none);
            EditorGUI.PropertyField(colorRect, colorProperty, GUIContent.none);
            
            // Update harmonies if color changed
            if (EditorGUI.EndChangeCheck() && index == _selectedColorIndex)
            {
                UpdateColorHarmonies();
            }
        }
        
        private float GetElementHeight(int index)
        {
            return EditorGUIUtility.singleLineHeight + 6;
        }
        
        private void OnAddColor(ReorderableList list)
        {
            var index = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = index;
            
            var element = list.serializedProperty.GetArrayElementAtIndex(index);
            var nameProperty = element.FindPropertyRelative("name");
            var colorProperty = element.FindPropertyRelative("color");
            
            // Generate a default color
            Color newColor = Color.HSVToRGB(Random.Range(0f, 1f), 0.7f, 0.8f);
            colorProperty.colorValue = newColor;
            
            // Auto-name the new color
            nameProperty.stringValue = ColorNameGenerator.GenerateName(newColor);
        }
        
        private void OnSelectColor(ReorderableList list)
        {
            _selectedColorIndex = list.index;
            UpdateColorHarmonies();
        }
        
        private void DrawColorHarmoniesSection()
        {
            var element = _colors.GetArrayElementAtIndex(_selectedColorIndex);
            var nameProperty = element.FindPropertyRelative("name");
            var colorProperty = element.FindPropertyRelative("color");
            
            // Section header
            EditorGUILayout.LabelField($"Color Harmonies: {nameProperty.stringValue}", EditorStyles.boldLabel);
            
            // LCH info for selected color
            Vector3 lch = ColorSpaceUtility.RGBToLCH(colorProperty.colorValue);
            EditorGUILayout.LabelField($"L: {lch.x:F1}  C: {lch.y:F1}  H: {lch.z:F0}°", EditorStyles.miniLabel);
            EditorGUILayout.Space(10);
            
            // Tonal Strip
            EditorGUILayout.LabelField("Tonal Variations", EditorStyles.miniLabel);
            ColorPaletteEditorUtility.DrawColorStrip(_tonalStrip, ColorPaletteEditorUtility.TONAL_STRIP_HEIGHT);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add Tonal Variations", EditorStyles.miniButton, GUILayout.Width(150)))
            {
                AddTonalVariations();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(15);
            
            // Other harmonies on one line
            EditorGUILayout.LabelField("Color Harmonies", EditorStyles.miniLabel);
            
            // Calculate widths
            float totalWidth = EditorGUIUtility.currentViewWidth - 40;
            float sectionWidth = totalWidth / 3f;
            
            EditorGUILayout.BeginHorizontal();
            
            // Complementary
            EditorGUILayout.BeginVertical(GUILayout.Width(sectionWidth));
            GUILayout.Label("Complementary", EditorStyles.miniLabel, GUILayout.Width(sectionWidth));
            Rect compRect = GUILayoutUtility.GetRect(sectionWidth, ColorPaletteEditorUtility.HARMONY_STRIP_HEIGHT);
            ColorPaletteEditorUtility.DrawColorStripInRect(compRect, new List<Color> { _complementaryColor });
            EditorGUILayout.EndVertical();
            
            // Analogous
            EditorGUILayout.BeginVertical(GUILayout.Width(sectionWidth));
            GUILayout.Label("Analogous", EditorStyles.miniLabel, GUILayout.Width(sectionWidth));
            Rect analogRect = GUILayoutUtility.GetRect(sectionWidth, ColorPaletteEditorUtility.HARMONY_STRIP_HEIGHT);
            ColorPaletteEditorUtility.DrawColorStripInRect(analogRect, _analogousColors);
            EditorGUILayout.EndVertical();
            
            // Triadic
            EditorGUILayout.BeginVertical(GUILayout.Width(sectionWidth));
            GUILayout.Label("Triadic", EditorStyles.miniLabel, GUILayout.Width(sectionWidth));
            Rect triadRect = GUILayoutUtility.GetRect(sectionWidth, ColorPaletteEditorUtility.HARMONY_STRIP_HEIGHT);
            ColorPaletteEditorUtility.DrawColorStripInRect(triadRect, _triadicColors);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            
            // Buttons strip
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.BeginVertical(GUILayout.Width(sectionWidth));
            if (GUILayout.Button("Add", EditorStyles.miniButton))
            {
                AddComplementaryColor();
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.BeginVertical(GUILayout.Width(sectionWidth));
            if (GUILayout.Button("Add", EditorStyles.miniButton))
            {
                AddAnalogousColors();
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.BeginVertical(GUILayout.Width(sectionWidth));
            if (GUILayout.Button("Add", EditorStyles.miniButton))
            {
                AddTriadicColors();
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void UpdateColorHarmonies()
        {
            if (_selectedColorIndex >= 0 && _selectedColorIndex < _colors.arraySize)
            {
                var element = _colors.GetArrayElementAtIndex(_selectedColorIndex);
                var colorProperty = element.FindPropertyRelative("color");
                Color baseColor = colorProperty.colorValue;
                
                // Generate all harmonies
                _tonalStrip = ColorHarmonyUtility.GenerateTonalStrip(baseColor, 7);
                _complementaryColor = ColorHarmonyUtility.GenerateComplementary(baseColor);
                _analogousColors = ColorHarmonyUtility.GenerateAnalogous(baseColor, 2);
                _triadicColors = ColorHarmonyUtility.GenerateTriadic(baseColor);
                
                // Remove the base color from triadic (it's the first one)
                if (_triadicColors.Count > 0)
                    _triadicColors.RemoveAt(0);
            }
            else
            {
                _tonalStrip.Clear();
                _analogousColors.Clear();
                _triadicColors.Clear();
                _complementaryColor = Color.white;
            }
        }
        
        private void AddTonalVariations()
        {
            if (_selectedColorIndex < 0 || _tonalStrip.Count == 0) return;
            
            var element = _colors.GetArrayElementAtIndex(_selectedColorIndex);
            var baseName = element.FindPropertyRelative("name").stringValue;
            var baseColor = element.FindPropertyRelative("color").colorValue;
            
            // Find where to insert (right after selected color)
            int insertIndex = _selectedColorIndex + 1;
            
            // Add each tonal variation
            for (int i = 0; i < _tonalStrip.Count; i++)
            {
                // Skip if it's too close to the original color
                if (Mathf.Abs(ColorSpaceUtility.RGBToLCH(_tonalStrip[i]).x - ColorSpaceUtility.RGBToLCH(baseColor).x) < 5f)
                    continue;
                
                _colors.InsertArrayElementAtIndex(insertIndex);
                var newElement = _colors.GetArrayElementAtIndex(insertIndex);
                
                var nameProperty = newElement.FindPropertyRelative("name");
                var colorProperty = newElement.FindPropertyRelative("color");
                
                nameProperty.stringValue = ColorPaletteEditorUtility.GenerateTonalVariationName(baseName, baseColor, _tonalStrip[i]);
                colorProperty.colorValue = _tonalStrip[i];
                
                insertIndex++;
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void AddComplementaryColor()
        {
            if (_selectedColorIndex < 0) return;
            
            var element = _colors.GetArrayElementAtIndex(_selectedColorIndex);
            var baseName = element.FindPropertyRelative("name").stringValue;
            
            // Add after selected color
            int insertIndex = _selectedColorIndex + 1;
            _colors.InsertArrayElementAtIndex(insertIndex);
            
            var newElement = _colors.GetArrayElementAtIndex(insertIndex);
            var nameProperty = newElement.FindPropertyRelative("name");
            var colorProperty = newElement.FindPropertyRelative("color");
            
            nameProperty.stringValue = ColorPaletteEditorUtility.GenerateHarmonyColorName(baseName, "Complement");
            colorProperty.colorValue = _complementaryColor;
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void AddAnalogousColors()
        {
            if (_selectedColorIndex < 0 || _analogousColors.Count == 0) return;
            
            var element = _colors.GetArrayElementAtIndex(_selectedColorIndex);
            var baseName = element.FindPropertyRelative("name").stringValue;
            
            int insertIndex = _selectedColorIndex + 1;
            
            for (int i = 0; i < _analogousColors.Count; i++)
            {
                _colors.InsertArrayElementAtIndex(insertIndex);
                var newElement = _colors.GetArrayElementAtIndex(insertIndex);
                
                var nameProperty = newElement.FindPropertyRelative("name");
                var colorProperty = newElement.FindPropertyRelative("color");
                
                nameProperty.stringValue = ColorPaletteEditorUtility.GenerateHarmonyColorName(baseName, "Analog", i);
                colorProperty.colorValue = _analogousColors[i];
                
                insertIndex++;
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void AddTriadicColors()
        {
            if (_selectedColorIndex < 0 || _triadicColors.Count == 0) return;
            
            var element = _colors.GetArrayElementAtIndex(_selectedColorIndex);
            var baseName = element.FindPropertyRelative("name").stringValue;
            
            int insertIndex = _selectedColorIndex + 1;
            
            for (int i = 0; i < _triadicColors.Count; i++)
            {
                _colors.InsertArrayElementAtIndex(insertIndex);
                var newElement = _colors.GetArrayElementAtIndex(insertIndex);
                
                var nameProperty = newElement.FindPropertyRelative("name");
                var colorProperty = newElement.FindPropertyRelative("color");
                
                nameProperty.stringValue = ColorPaletteEditorUtility.GenerateHarmonyColorName(baseName, "Triad", i);
                colorProperty.colorValue = _triadicColors[i];
                
                insertIndex++;
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawColorPreview(ColorPalette palette)
        {
            EditorGUILayout.LabelField("Palette Preview", EditorStyles.boldLabel);
            
            var rect = EditorGUILayout.GetControlRect(false, 40);
            var colors = palette.GetColorNames();
            
            if (colors.Length == 0) return;
            
            var colorWidth = rect.width / colors.Length;
            
            for (int i = 0; i < colors.Length; i++)
            {
                var colorRect = new Rect(rect.x + (i * colorWidth), rect.y, colorWidth - 2, rect.height);
                var color = palette.GetColor(colors[i]);
                
                ColorPaletteEditorUtility.DrawColorSwatch(colorRect, color, colorWidth > 50 ? colors[i] : null);
            }
        }
        
        private void AddStandardColors()
        {
            var standardColors = ColorPaletteEditorUtility.GetStandardColors();
            
            foreach (var (name, color) in standardColors)
            {
                bool exists = false;
                for (int i = 0; i < _colors.arraySize; i++)
                {
                    var element = _colors.GetArrayElementAtIndex(i);
                    if (element.FindPropertyRelative("name").stringValue != name) continue;
                    exists = true;
                    break;
                }

                if (exists) continue;
                _colors.arraySize++;
                var newElement = _colors.GetArrayElementAtIndex(_colors.arraySize - 1);
                newElement.FindPropertyRelative("name").stringValue = name;
                newElement.FindPropertyRelative("color").colorValue = color;
            }
        }
        
        private void SortColorsByName()
        {
            var colorList = new System.Collections.Generic.List<(string name, Color color)>();
            
            for (int i = 0; i < _colors.arraySize; i++)
            {
                var element = _colors.GetArrayElementAtIndex(i);
                var name = element.FindPropertyRelative("name").stringValue;
                var color = element.FindPropertyRelative("color").colorValue;
                colorList.Add((name, color));
            }
            
            colorList.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            
            for (int i = 0; i < colorList.Count; i++)
            {
                var element = _colors.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("name").stringValue = colorList[i].name;
                element.FindPropertyRelative("color").colorValue = colorList[i].color;
            }
        }
        
        private void AutoNameAllColors()
        {
            // Collect all colors
            var colors = new List<Color>();
            for (int i = 0; i < _colors.arraySize; i++)
            {
                var element = _colors.GetArrayElementAtIndex(i);
                var colorProperty = element.FindPropertyRelative("color");
                colors.Add(colorProperty.colorValue);
            }
            
            // Generate names
            var names = ColorNameGenerator.GenerateNames(colors);
            
            // Apply names
            for (int i = 0; i < _colors.arraySize && i < names.Count; i++)
            {
                var element = _colors.GetArrayElementAtIndex(i);
                var nameProperty = element.FindPropertyRelative("name");
                nameProperty.stringValue = names[i];
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void AutoNameColor(int index)
        {
            if (index < 0 || index >= _colors.arraySize) return;
            
            var element = _colors.GetArrayElementAtIndex(index);
            var nameProperty = element.FindPropertyRelative("name");
            var colorProperty = element.FindPropertyRelative("color");
            
            // Generate a unique name considering existing names
            var existingNames = new HashSet<string>();
            for (int i = 0; i < _colors.arraySize; i++)
            {
                if (i == index) continue;
                var otherElement = _colors.GetArrayElementAtIndex(i);
                existingNames.Add(otherElement.FindPropertyRelative("name").stringValue);
            }
            
            string baseName = ColorNameGenerator.GenerateName(colorProperty.colorValue);
            string finalName = baseName;
            int counter = 2;
            
            while (existingNames.Contains(finalName))
            {
                finalName = $"{baseName} {counter}";
                counter++;
            }
            
            nameProperty.stringValue = finalName;
            serializedObject.ApplyModifiedProperties();
        }
        
        private void SuggestPaletteName()
        {
            // Collect all colors
            var colors = new List<Color>();
            for (int i = 0; i < _colors.arraySize; i++)
            {
                var element = _colors.GetArrayElementAtIndex(i);
                var colorProperty = element.FindPropertyRelative("color");
                colors.Add(colorProperty.colorValue);
            }
            
            // Generate palette name suggestion
            string suggestedName = ColorNameGenerator.SuggestPaletteName(colors);
            _paletteName.stringValue = suggestedName;
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif