using UnityEngine;

/// <summary>
/// MaterialColorController - Controls a color shader property on a shared material.
/// Can be animated through Unity's animation system.
/// </summary>
[AddComponentMenu("Effects/Material Color Controller")]
public class MaterialColorController : MonoBehaviour
{
    [Header("Material Settings")]
    [Tooltip("The material to modify (shared material).")]
    public Material targetMaterial;

    [Header("Shader Property")]
    [Tooltip("The name of the color shader property to control (e.g., '_Color', '_BaseColor', '_EmissionColor').")]
    public string propertyName = "_Color";

    [Header("Value")]
    [Tooltip("The current color to set for the property. This can be animated.")]
    [ColorUsage(true, true)]
    public Color propertyColor = Color.white;

    private Color lastColor = Color.clear;

    private void OnEnable()
    {
        if (targetMaterial == null && GetComponent<Renderer>())
        {
            targetMaterial = GetComponent<Renderer>().sharedMaterial;
        }

        UpdateShaderProperty();
    }

    private void Update()
    {
        // Only update if the color has changed
        if (propertyColor != lastColor)
        {
            UpdateShaderProperty();
            lastColor = propertyColor;
        }
    }

    private void UpdateShaderProperty()
    {
        if (targetMaterial == null || string.IsNullOrEmpty(propertyName))
            return;

        if (targetMaterial.HasProperty(propertyName))
        {
            targetMaterial.SetColor(propertyName, propertyColor);
        }
    }
}
