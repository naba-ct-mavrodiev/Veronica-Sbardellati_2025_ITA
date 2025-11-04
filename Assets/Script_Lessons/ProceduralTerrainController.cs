using UnityEngine;

[ExecuteInEditMode]
public class ProceduralTerrainController : MonoBehaviour
{
    [Header("Terrain Reference")]
    public Terrain targetTerrain;
    
    [Header("Radial Ripple")]
    public bool enableRipple = true;
    public Vector3 rippleCenter = new Vector3(0.5f, 0, 0.5f); // Normalized 0-1 terrain coordinates
    public float rippleRadius = 100f;
    public int rippleCount = 12;
    public float rippleAmplitude = 2f;
    [Range(0f, 1f)]
    public float rippleBlendStrength = 0.5f;
    [Range(0f, 1f)]
    public float rippleNoiseAmount = 0.2f;
    public float rippleFrequency = 1f;
    
    [Header("Directional Flow")]
    public bool enableFlow = true;
    public Vector2 flowDirection = new Vector2(1, 0);
    public float flowWaveLength = 20f;
    public float flowAmplitude = 1.5f;
    [Range(0f, 1f)]
    public float flowBlendStrength = 0.3f;
    public bool animateFlow = false;
    public float flowAnimationSpeed = 0.5f;
    
    [Header("Noise Displacement")]
    public bool enableNoise = true;
    public float noiseScale = 50f;
    public float noiseStrength = 1f;
    [Range(1, 4)]
    public int noiseOctaves = 3;
    [Range(0f, 1f)]
    public float noiseBlendStrength = 0.4f;
    
    [Header("Smoothing")]
    public bool enableSmoothing = false;
    public Vector3 smoothingCenter = new Vector3(0.5f, 0, 0.5f); // Normalized
    public float smoothingRadius = 50f;
    [Range(1, 10)]
    public int smoothingIterations = 3;
    [Range(0f, 1f)]
    public float smoothingStrength = 0.5f;

    private float[,] originalHeights;
    private float flowAnimationTime = 0f;
    private bool isInitialized = false;

    void Start()
    {
        if (!targetTerrain) targetTerrain = GetComponent<Terrain>();
        if (targetTerrain)
        {
            StoreOriginalHeights();
            ApplyDeformation();
        }
    }

    public void StoreOriginalHeights()
    {
        if (!targetTerrain) return;

        TerrainData terrainData = targetTerrain.terrainData;
        int resolution = terrainData.heightmapResolution;
        originalHeights = terrainData.GetHeights(0, 0, resolution, resolution);
        isInitialized = true;
    }

    void Update()
    {
        if (!targetTerrain) return;

        // Update animation time in play mode
        if (Application.isPlaying && animateFlow && enableFlow)
        {
            flowAnimationTime += Time.deltaTime * flowAnimationSpeed;
        }

        // Always apply continuous updates
        ApplyDeformation();
    }

    public void ApplyDeformation()
    {
        if (!targetTerrain) return;
        if (!isInitialized) StoreOriginalHeights();

        TerrainData terrainData = targetTerrain.terrainData;
        int resolution = terrainData.heightmapResolution;
        Vector3 terrainSize = terrainData.size;

        // Start with original heights
        float[,] heights = (float[,])originalHeights.Clone();
        
        // Apply each deformation in sequence
        if (enableRipple)
        {
            ApplyRadialRipple(heights, resolution, terrainSize);
        }
        
        if (enableFlow)
        {
            ApplyDirectionalFlow(heights, resolution, terrainSize);
        }
        
        if (enableNoise)
        {
            ApplyNoiseDisplacement(heights, resolution, terrainSize);
        }
        
        if (enableSmoothing)
        {
            ApplySmoothingField(heights, resolution, terrainSize);
        }
        
        // Apply to terrain
        terrainData.SetHeights(0, 0, heights);
    }

    void ApplyRadialRipple(float[,] heights, int resolution, Vector3 terrainSize)
    {
        Vector2 center = new Vector2(rippleCenter.x * resolution, rippleCenter.z * resolution);
        float radiusInHeightmapUnits = (rippleRadius / terrainSize.x) * resolution;
        
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dx = x - center.x;
                float dy = y - center.y;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                
                if (distance < radiusInHeightmapUnits)
                {
                    // Normalized distance (0 at center, 1 at edge)
                    float normalizedDist = distance / radiusInHeightmapUnits;
                    
                    // Radial falloff
                    float falloff = 1f - normalizedDist;
                    falloff = falloff * falloff; // Smooth falloff
                    
                    // Ripple wave
                    float wave = Mathf.Sin(normalizedDist * rippleCount * Mathf.PI * 2f * rippleFrequency);
                    
                    // Add noise to break perfect circles
                    float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 2f - 1f;
                    wave = Mathf.Lerp(wave, wave * noise, rippleNoiseAmount);
                    
                    // Calculate ripple height
                    float rippleHeight = wave * falloff * (rippleAmplitude / terrainSize.y);
                    
                    // Blend with original
                    heights[y, x] = Mathf.Lerp(heights[y, x], heights[y, x] + rippleHeight, rippleBlendStrength);
                }
            }
        }
    }

    void ApplyDirectionalFlow(float[,] heights, int resolution, Vector3 terrainSize)
    {
        Vector2 normalizedDir = flowDirection.normalized;
        float time = animateFlow ? flowAnimationTime : 0f;
        
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                // Project position onto flow direction
                float projectedDistance = (x * normalizedDir.x + y * normalizedDir.y) / resolution;
                
                // Create wave along direction
                float wave = Mathf.Sin((projectedDistance * terrainSize.x / flowWaveLength + time) * Mathf.PI * 2f);
                
                // Add perpendicular variation for more organic feel
                float perpDist = (x * normalizedDir.y - y * normalizedDir.x) / resolution;
                float perpVariation = Mathf.PerlinNoise(perpDist * 5f, time * 0.1f) * 0.5f;
                wave *= (1f + perpVariation);
                
                // Calculate flow height
                float flowHeight = wave * (flowAmplitude / terrainSize.y);
                
                // Blend with current
                heights[y, x] = Mathf.Lerp(heights[y, x], heights[y, x] + flowHeight, flowBlendStrength);
            }
        }
    }

    void ApplyNoiseDisplacement(float[,] heights, int resolution, Vector3 terrainSize)
    {
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float noiseValue = 0f;
                float amplitude = 1f;
                float frequency = 1f;
                float maxValue = 0f;
                
                // Multi-octave noise
                for (int octave = 0; octave < noiseOctaves; octave++)
                {
                    float sampleX = (float)x / resolution * frequency / noiseScale;
                    float sampleY = (float)y / resolution * frequency / noiseScale;
                    
                    noiseValue += Mathf.PerlinNoise(sampleX, sampleY) * amplitude;
                    maxValue += amplitude;
                    
                    amplitude *= 0.5f;
                    frequency *= 2f;
                }
                
                // Normalize
                noiseValue /= maxValue;
                noiseValue = noiseValue * 2f - 1f; // Range -1 to 1
                
                // Apply displacement
                float displacement = noiseValue * (noiseStrength / terrainSize.y);
                heights[y, x] = Mathf.Lerp(heights[y, x], heights[y, x] + displacement, noiseBlendStrength);
            }
        }
    }

    void ApplySmoothingField(float[,] heights, int resolution, Vector3 terrainSize)
    {
        Vector2 center = new Vector2(smoothingCenter.x * resolution, smoothingCenter.z * resolution);
        float radiusInHeightmapUnits = (smoothingRadius / terrainSize.x) * resolution;
        
        // Create temporary array for smoothing
        float[,] smoothed = (float[,])heights.Clone();
        
        for (int iteration = 0; iteration < smoothingIterations; iteration++)
        {
            for (int y = 1; y < resolution - 1; y++)
            {
                for (int x = 1; x < resolution - 1; x++)
                {
                    float dx = x - center.x;
                    float dy = y - center.y;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    
                    if (distance < radiusInHeightmapUnits)
                    {
                        // Normalized distance for falloff
                        float normalizedDist = distance / radiusInHeightmapUnits;
                        float falloff = 1f - normalizedDist;
                        falloff = falloff * falloff;
                        
                        // Box blur
                        float avg = (
                            heights[y - 1, x - 1] + heights[y - 1, x] + heights[y - 1, x + 1] +
                            heights[y, x - 1] + heights[y, x] + heights[y, x + 1] +
                            heights[y + 1, x - 1] + heights[y + 1, x] + heights[y + 1, x + 1]
                        ) / 9f;
                        
                        // Blend smoothed value with falloff
                        smoothed[y, x] = Mathf.Lerp(heights[y, x], avg, smoothingStrength * falloff);
                    }
                }
            }
            
            // Copy back for next iteration
            heights = (float[,])smoothed.Clone();
        }
        
        // Copy final result
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                heights[y, x] = smoothed[y, x];
            }
        }
    }

    public void ResetTerrain()
    {
        if (!targetTerrain || !isInitialized) return;

        TerrainData terrainData = targetTerrain.terrainData;
        terrainData.SetHeights(0, 0, originalHeights);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!targetTerrain) return;
        
        TerrainData terrainData = targetTerrain.terrainData;
        Vector3 terrainPos = targetTerrain.transform.position;
        Vector3 terrainSize = terrainData.size;
        
        // Draw ripple center and radius
        if (enableRipple)
        {
            Vector3 worldCenter = terrainPos + new Vector3(
                rippleCenter.x * terrainSize.x,
                terrainData.GetHeight((int)(rippleCenter.x * terrainData.heightmapResolution), 
                                     (int)(rippleCenter.z * terrainData.heightmapResolution)),
                rippleCenter.z * terrainSize.z
            );
            
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            DrawCircle(worldCenter, rippleRadius, 32);
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(worldCenter, 2f);
        }
        
        // Draw flow direction
        if (enableFlow)
        {
            Vector3 flowStart = terrainPos + new Vector3(terrainSize.x * 0.5f, 0, terrainSize.z * 0.5f);
            Vector3 flowEnd = flowStart + new Vector3(flowDirection.x, 0, flowDirection.y).normalized * 50f;
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(flowStart, flowEnd);
            DrawArrow(flowStart, flowEnd);
        }
        
        // Draw smoothing area
        if (enableSmoothing)
        {
            Vector3 worldCenter = terrainPos + new Vector3(
                smoothingCenter.x * terrainSize.x,
                terrainData.GetHeight((int)(smoothingCenter.x * terrainData.heightmapResolution), 
                                     (int)(smoothingCenter.z * terrainData.heightmapResolution)),
                smoothingCenter.z * terrainSize.z
            );
            
            Gizmos.color = new Color(1, 0, 1, 0.3f);
            DrawCircle(worldCenter, smoothingRadius, 32);
        }
    }
    
    void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
    
    void DrawArrow(Vector3 start, Vector3 end)
    {
        Vector3 direction = (end - start).normalized;
        Vector3 right = Vector3.Cross(direction, Vector3.up).normalized * 5f;
        
        Gizmos.DrawLine(end, end - direction * 10f + right);
        Gizmos.DrawLine(end, end - direction * 10f - right);
    }
#endif
}