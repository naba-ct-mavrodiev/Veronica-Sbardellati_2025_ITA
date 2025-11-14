using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class FogVolume : MonoBehaviour
{
    [Header("Volume Settings")]
    [SerializeField] private VolumeType volumeType = VolumeType.GroundFog;
    [SerializeField] private Vector3 volumeSize = new Vector3(10f, 5f, 10f);
    
    [Header("Fog Appearance")]
    [ColorUsage(true, true)]
    [SerializeField] private Color fogColor = new Color(0.5f, 0.6f, 0.7f, 1f);
    [Range(0f, 5f)]
    [SerializeField] private float density = 1f;
    [Range(0.1f, 10f)]
    [SerializeField] private float noiseScale = 1f;
    [SerializeField] private Vector3 noiseSpeed = new Vector3(0.1f, 0f, 0.1f);

    [Header("Height Settings (Ground Fog)")]
    [Range(0f, 10f)]
    [SerializeField] private float heightFalloff = 2f;
    [SerializeField] private float fogFloor = 0f;
    [SerializeField] private float fogCeiling = 5f;

    [Header("Noise Input")]
    [SerializeField] private Texture3D noiseTexture3D;
    [SerializeField] private bool useProceduralNoise = false;
    [SerializeField] private Texture2D detailTexture;
    [Range(0.1f, 10f)]
    [SerializeField] private float detailScale = 3f;
    [Range(0f, 1f)]
    [SerializeField] private float detailStrength = 0.3f;

    [Header("Quality")]
    [Range(4, 32)]
    [SerializeField] private int stepCount = 16;
    [Range(10f, 100f)]
    [SerializeField] private float maxDistance = 50f;
    [Range(0f, 1f)]
    [SerializeField] private float jitter = 0.5f;

    [Header("Scene Integration")]
    [Range(0f, 10f)]
    [SerializeField] private float softParticlesFactor = 1f;
    [Range(0f, 10f)]
    [SerializeField] private float depthFade = 1f;

    [Header("LOD Settings")]
    [SerializeField] private bool enableLOD = true;
    [SerializeField] private float lodDistance = 30f;
    [SerializeField] private int lodStepCount = 8;

    private Material fogMaterial;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock propertyBlock;

    public enum VolumeType
    {
        GroundFog,
        CloudVolume
    }

    void OnEnable()
    {
        SetupComponents();
        UpdateMaterial();
        Generate3DNoiseTexture();
    }

    void SetupComponents()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        
        if (meshFilter.sharedMesh == null)
        {
            // Create inverted cube mesh for volume
            meshFilter.sharedMesh = CreateInvertedCube();
        }
        
        if (fogMaterial == null)
        {
            Shader fogShader = Shader.Find("Custom/VolumetricFog");
            if (fogShader != null)
            {
                fogMaterial = new Material(fogShader);
                meshRenderer.sharedMaterial = fogMaterial;
            }
        }
        
        propertyBlock = new MaterialPropertyBlock();
    }

    void UpdateMaterial()
    {
        if (fogMaterial == null) return;
        
        meshRenderer.GetPropertyBlock(propertyBlock);
        
        // Basic settings
        propertyBlock.SetColor("_FogColor", fogColor);
        propertyBlock.SetFloat("_Density", density);
        propertyBlock.SetFloat("_NoiseScale", noiseScale);
        propertyBlock.SetVector("_NoiseSpeed", noiseSpeed);
        
        // Volume type
        propertyBlock.SetFloat("_IsGroundFog", volumeType == VolumeType.GroundFog ? 1f : 0f);
        propertyBlock.SetFloat("_HeightFalloff", heightFalloff);
        propertyBlock.SetFloat("_FogFloor", fogFloor);
        propertyBlock.SetFloat("_FogCeiling", fogCeiling);
        
        // Noise settings
        if (noiseTexture3D != null)
            propertyBlock.SetTexture("_NoiseTex3D", noiseTexture3D);
        propertyBlock.SetFloat("_UseProceduralNoise", useProceduralNoise ? 1f : 0f);
        if (detailTexture != null)
            propertyBlock.SetTexture("_DetailNoiseTex", detailTexture);
        propertyBlock.SetFloat("_DetailScale", detailScale);
        propertyBlock.SetFloat("_DetailStrength", detailStrength);
        
        // Quality settings
        int currentStepCount = stepCount;
        if (enableLOD && Camera.main != null)
        {
            float distance = Vector3.Distance(transform.position, Camera.main.transform.position);
            if (distance > lodDistance)
            {
                currentStepCount = lodStepCount;
            }
        }
        
        propertyBlock.SetFloat("_StepCount", currentStepCount);
        propertyBlock.SetFloat("_MaxDistance", maxDistance);
        propertyBlock.SetFloat("_Jitter", jitter);
        
        // Integration settings
        propertyBlock.SetFloat("_SoftParticlesFactor", softParticlesFactor);
        propertyBlock.SetFloat("_DepthFade", depthFade);
        
        meshRenderer.SetPropertyBlock(propertyBlock);
    }

    void Update()
    {
        UpdateMaterial();
        
        // Update volume bounds
        transform.localScale = volumeSize;
    }

    Mesh CreateInvertedCube()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Inverted Cube";
        
        // Vertices
        Vector3[] vertices = {
            // Front
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f,  0.5f, -0.5f),
            new Vector3(-0.5f,  0.5f, -0.5f),
            // Back
            new Vector3(-0.5f, -0.5f,  0.5f),
            new Vector3( 0.5f, -0.5f,  0.5f),
            new Vector3( 0.5f,  0.5f,  0.5f),
            new Vector3(-0.5f,  0.5f,  0.5f),
        };
        
        // Triangles (inverted winding order)
        int[] triangles = {
            // Front
            0, 2, 1, 0, 3, 2,
            // Back
            5, 6, 4, 6, 7, 4,
            // Left
            4, 3, 0, 4, 7, 3,
            // Right
            1, 2, 5, 2, 6, 5,
            // Bottom
            0, 1, 4, 1, 5, 4,
            // Top
            2, 3, 6, 3, 7, 6
        };
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        return mesh;
    }

    void Generate3DNoiseTexture()
    {
        if (noiseTexture3D != null) return;
        
        // Generate a simple 3D noise texture if none is assigned
        int size = 32;
        noiseTexture3D = new Texture3D(size, size, size, TextureFormat.R8, false);
        noiseTexture3D.wrapMode = TextureWrapMode.Repeat;
        noiseTexture3D.filterMode = FilterMode.Bilinear;
        
        Color[] colors = new Color[size * size * size];
        int index = 0;
        
        for (int z = 0; z < size; z++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 
                                 Mathf.PerlinNoise(y * 0.1f, z * 0.1f) * 
                                 Mathf.PerlinNoise(z * 0.1f, x * 0.1f);
                    colors[index] = new Color(noise, noise, noise, 1f);
                    index++;
                }
            }
        }
        
        noiseTexture3D.SetPixels(colors);
        noiseTexture3D.Apply();
    }

    void OnDrawGizmosSelected()
    {
        // Draw volume bounds
        Gizmos.color = new Color(fogColor.r, fogColor.g, fogColor.b, 0.3f);
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, volumeSize);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        
        // Draw height bounds for ground fog
        if (volumeType == VolumeType.GroundFog)
        {
            Gizmos.color = Color.yellow;
            Gizmos.matrix = Matrix4x4.identity;
            
            Vector3 floorPos = transform.position + Vector3.up * fogFloor;
            Vector3 ceilingPos = transform.position + Vector3.up * fogCeiling;
            
            Gizmos.DrawLine(floorPos + Vector3.left * volumeSize.x * 0.5f, 
                           floorPos + Vector3.right * volumeSize.x * 0.5f);
            Gizmos.DrawLine(ceilingPos + Vector3.left * volumeSize.x * 0.5f, 
                           ceilingPos + Vector3.right * volumeSize.x * 0.5f);
        }
    }
}