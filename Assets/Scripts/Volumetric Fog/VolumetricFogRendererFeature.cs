using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class VolumetricFogRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class FogSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
        public LayerMask fogVolumeLayers = -1;
        [Range(0.25f, 1f)]
        public float renderScale = 1f;
        public bool enableTemporalSmoothing = false;
    }

    public FogSettings settings = new FogSettings();
    private VolumetricFogRenderPass fogPass;

    public override void Create()
    {
        fogPass = new VolumetricFogRenderPass(settings);
        fogPass.renderPassEvent = settings.renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        fogPass.Setup(renderer);
        renderer.EnqueuePass(fogPass);
    }

    protected override void Dispose(bool disposing)
    {
        fogPass?.Dispose();
    }

    class VolumetricFogRenderPass : ScriptableRenderPass
    {
        private FogSettings settings;
        private FilteringSettings filteringSettings;
        private RTHandle tempTextureHandle;
        private ScriptableRenderer renderer;

        private readonly int tempTextureId = Shader.PropertyToID("_TempFogTexture");
        private new ProfilingSampler profilingSampler = new ProfilingSampler("Volumetric Fog");
        private MaterialPropertyBlock propertyBlock;

        public VolumetricFogRenderPass(FogSettings settings)
        {
            this.settings = settings;
            filteringSettings = new FilteringSettings(RenderQueueRange.all, settings.fogVolumeLayers);
            propertyBlock = new MaterialPropertyBlock();
            profilingSampler = new ProfilingSampler("Volumetric Fog");
        }

        public void Setup(ScriptableRenderer renderer)
        {
            this.renderer = renderer;
        }

        // RenderGraph API implementation
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            // Find all fog volumes in the scene
            FogVolume[] fogVolumes = Object.FindObjectsByType<FogVolume>(FindObjectsSortMode.None);

            if (fogVolumes.Length == 0)
                return;

            // Sort volumes by distance for proper blending
            Camera camera = cameraData.camera;
            System.Array.Sort(fogVolumes, (a, b) =>
            {
                float distA = Vector3.Distance(camera.transform.position, a.transform.position);
                float distB = Vector3.Distance(camera.transform.position, b.transform.position);
                return distB.CompareTo(distA);
            });

            // Setup render pass data
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Volumetric Fog", out var passData, profilingSampler))
            {
                passData.fogVolumes = fogVolumes;
                passData.camera = camera;
                passData.propertyBlock = propertyBlock;
                passData.cameraData = cameraData;

                // Set up render attachments - this is required for raster passes
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.ReadWrite);

                // Use the actual depth attachment, not the depth texture copy
                if (resourceData.activeDepthTexture.IsValid())
                {
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);
                }

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }
        }

        private static void ExecutePass(PassData data, RasterGraphContext context)
        {
            // Render each volume
            foreach (var volume in data.fogVolumes)
            {
                if (volume == null || !volume.enabled || !volume.gameObject.activeInHierarchy)
                    continue;

                MeshRenderer meshRenderer = volume.GetComponent<MeshRenderer>();
                MeshFilter meshFilter = volume.GetComponent<MeshFilter>();

                if (meshRenderer != null && meshFilter != null && meshFilter.sharedMesh != null)
                {
                    // Check if volume is in camera frustum
                    if (GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(data.camera),
                                                       meshRenderer.bounds))
                    {
                        meshRenderer.GetPropertyBlock(data.propertyBlock);

                        // Pass volume bounds to shader for proper scaling
                        Bounds bounds = meshRenderer.bounds;
                        data.propertyBlock.SetVector("_VolumeCenter", bounds.center);
                        data.propertyBlock.SetVector("_VolumeSize", bounds.size);
                        data.propertyBlock.SetVector("_VolumeExtents", bounds.extents);

                        context.cmd.DrawMesh(meshFilter.sharedMesh,
                                   volume.transform.localToWorldMatrix,
                                   meshRenderer.sharedMaterial,
                                   0, 0,
                                   data.propertyBlock);
                    }
                }
            }
        }

        // Compatibility mode (when RenderGraph is disabled)
        [System.Obsolete("This rendering path is for compatibility mode only")]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        [System.Obsolete("This rendering path is for compatibility mode only")]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Volumetric Fog");

            using (new ProfilingScope(cmd, profilingSampler))
            {
                // Find all fog volumes in the scene
                FogVolume[] fogVolumes = Object.FindObjectsByType<FogVolume>(FindObjectsSortMode.None);

                if (fogVolumes.Length == 0)
                {
                    CommandBufferPool.Release(cmd);
                    return;
                }

                // Sort volumes by distance for proper blending
                Camera camera = renderingData.cameraData.camera;
                System.Array.Sort(fogVolumes, (a, b) =>
                {
                    float distA = Vector3.Distance(camera.transform.position, a.transform.position);
                    float distB = Vector3.Distance(camera.transform.position, b.transform.position);
                    return distB.CompareTo(distA);
                });

                // Render each volume
                foreach (var volume in fogVolumes)
                {
                    if (volume.enabled && volume.gameObject.activeInHierarchy)
                    {
                        MeshRenderer meshRenderer = volume.GetComponent<MeshRenderer>();
                        MeshFilter meshFilter = volume.GetComponent<MeshFilter>();

                        if (meshRenderer != null && meshFilter != null && meshFilter.sharedMesh != null)
                        {
                            // Check if volume is in camera frustum
                            if (GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera),
                                                               meshRenderer.bounds))
                            {
                                meshRenderer.GetPropertyBlock(propertyBlock);

                                // Pass volume bounds to shader for proper scaling
                                Bounds bounds = meshRenderer.bounds;
                                propertyBlock.SetVector("_VolumeCenter", bounds.center);
                                propertyBlock.SetVector("_VolumeSize", bounds.size);
                                propertyBlock.SetVector("_VolumeExtents", bounds.extents);

                                cmd.DrawMesh(meshFilter.sharedMesh,
                                           volume.transform.localToWorldMatrix,
                                           meshRenderer.sharedMaterial,
                                           0, 0,
                                           propertyBlock);
                            }
                        }
                    }
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            tempTextureHandle?.Release();
        }

        private class PassData
        {
            public FogVolume[] fogVolumes;
            public Camera camera;
            public MaterialPropertyBlock propertyBlock;
            public UniversalCameraData cameraData;
        }
    }
}
