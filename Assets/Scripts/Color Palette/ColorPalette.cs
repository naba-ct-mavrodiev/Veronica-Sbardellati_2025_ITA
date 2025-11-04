using System.Collections.Generic;
using UnityEngine;

namespace Dancerex.Utils.Colors
{
    /// <summary>
    /// Simple color palette for scene-wide color management.
    /// </summary>
    [CreateAssetMenu(menuName = "Tools/Color Palette", fileName = "New ColorPalette")]
    public class ColorPalette : ScriptableObject
    {
        [System.Serializable]
        public class ColorEntry
        {
            public string name;
            public Color color = Color.white;
        }

        [SerializeField] private string paletteName = "New Palette";
        
        [SerializeField]
        private List<ColorEntry> colors = new List<ColorEntry>
        {
            new ColorEntry { name = "Color01", color = Color.white },
            new ColorEntry { name = "Color02", color = Color.gray },
            new ColorEntry { name = "Color03", color = Color.black },
            new ColorEntry { name = "Color04", color = new Color(0.5f, 0.6f, 0.7f, 1f) },
            new ColorEntry { name = "Color05", color = new Color(0.9f, 0.85f, 0.8f, 1f) }
        };
        
        private Dictionary<string, Color> _colorCache;
        
        /// <summary>
        /// Gets the palette name.
        /// </summary>
        public string PaletteName => paletteName;
        
        /// <summary>
        /// Gets a color by name.
        /// </summary>
        public Color GetColor(string colorName, Color defaultColor = default)
        {
            if (string.IsNullOrEmpty(colorName))
                return defaultColor;
                
            ValidateCache();
            return _colorCache.TryGetValue(colorName, out Color color) ? color : defaultColor;
        }
        
        /// <summary>
        /// Gets all color names in this palette.
        /// </summary>
        public string[] GetColorNames()
        {
            ValidateCache();
            return new List<string>(_colorCache.Keys).ToArray();
        }
        
        private void ValidateCache()
        {
            if (_colorCache == null)
            {
                _colorCache = new Dictionary<string, Color>();
                foreach (var entry in colors)
                {
                    if (!string.IsNullOrEmpty(entry.name))
                    {
                        _colorCache[entry.name] = entry.color;
                    }
                }
            }
        }
        
        private void OnEnable()
        {
            _colorCache = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _colorCache = null;
        }
#endif
    }
}