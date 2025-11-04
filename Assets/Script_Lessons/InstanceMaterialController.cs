using UnityEngine;

/// <summary>
/// Controls a MATERIAL INSTANCE float shader property through an animatable value.
/// This only affects THIS specific object's material instance.
/// Can be animated through Unity's animation system.
/// </summary>
public class InstanceMaterialController : MonoBehaviour
{
    [Header("Material Settings")]
    [Tooltip("The renderer component containing the material. If null, will try to get from this GameObject.")]
    public Renderer targetRenderer;

    [Tooltip("Index of the material in the renderer's materials array.")]
    public int materialIndex = 0;

    [Header("Shader Property")]
    [Tooltip("The name of the float shader property to control (e.g., '_Metallic', '_Smoothness')")]
    public string propertyName = "_Metallic";

    [Header("Value")]
    [Tooltip("The current value to set for the property. This can be animated. Only affects THIS object.")]
    public float propertyValue = 0f;

    private Material materialInstance;
    private float lastValue = float.MinValue;
    private bool instanceCreated = false;

    private void OnEnable()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        InitializeMaterialInstance();
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

    private void InitializeMaterialInstance()
    {
        if (targetRenderer == null)
        {
            Debug.LogWarning("No Renderer found on this GameObject!", this);
            return;
        }

        if (materialIndex >= targetRenderer.sharedMaterials.Length)
        {
            Debug.LogWarning($"Material index {materialIndex} is out of range!", this);
            return;
        }

        // Create a material instance by accessing .materials (not .sharedMaterials)
        // This automatically creates an instance if one doesn't exist
        if (!instanceCreated)
        {
            materialInstance = targetRenderer.materials[materialIndex];
            instanceCreated = true;
        }

        UpdateShaderProperty();
    }

    private void UpdateShaderProperty()
    {
        if (materialInstance == null || string.IsNullOrEmpty(propertyName))
            return;

        if (materialInstance.HasProperty(propertyName))
        {
            materialInstance.SetFloat(propertyName, propertyValue);
        }
    }

    private void OnDestroy()
    {
        // Clean up the material instance when the object is destroyed
        if (instanceCreated && materialInstance != null)
        {
            if (Application.isPlaying)
            {
                Destroy(materialInstance);
            }
            else
            {
                DestroyImmediate(materialInstance);
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // In editor, we need to be careful not to create instances during edit mode
        if (!Application.isPlaying)
        {
            return;
        }

        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        if (instanceCreated && materialInstance != null)
        {
            UpdateShaderProperty();
        }
    }
#endif
}
