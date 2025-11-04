using UnityEngine;

/// <summary>
/// MaterialPropertyController - Controls a float shader property on a shared material.
/// Can be animated through Unity's animation system.
/// </summary>
[AddComponentMenu("Effects/Material Property Controller")]
public class MaterialPropertyController : MonoBehaviour
{
    [Header("Material Settings")]
    [Tooltip("The material to modify (shared material).")]
    public Material targetMaterial;

    [Header("Shader Property")]
    [Tooltip("The name of the float shader property to control (e.g., '_Smoothness', '_Metallic').")]
    public string propertyName = "_Smoothness";

    [Header("Value")]
    [Tooltip("The current value to set for the property. This can be animated.")]
    public float propertyValue = 0f;

    private float lastValue = float.MinValue;

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
        // Only update if the value has changed
        if (!Mathf.Approximately(propertyValue, lastValue))
        {
            UpdateShaderProperty();
            lastValue = propertyValue;
        }
    }

    private void UpdateShaderProperty()
    {
        if (targetMaterial == null || string.IsNullOrEmpty(propertyName))
            return;

        if (targetMaterial.HasProperty(propertyName))
        {
            targetMaterial.SetFloat(propertyName, propertyValue);
        }
    }
}