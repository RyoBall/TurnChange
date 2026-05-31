using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Full-screen post-processing after opaque+transparent, before URP post-processing (UI renders later).
/// Matches URP FullScreenPassRendererFeature: copy camera color → DrawProcedural. No SwapColorBuffer.
/// </summary>
public class FieldDomainRenderFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader effectShader;

    private FieldDomainRenderPass m_RenderPass;
    private Material m_FallbackMaterial;

    public override void Create()
    {
        m_RenderPass ??= new FieldDomainRenderPass();
        EnsureFallbackMaterial();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (UniversalRenderer.IsOffscreenDepthTexture(in renderingData.cameraData))
        {
            return;
        }

        Material material = ResolveMaterial();
        if (material == null || m_RenderPass == null)
        {
            return;
        }

        if (!FieldDomainScreenEffectController.IsRendering)
        {
            return;
        }

        if (renderingData.cameraData.cameraType != CameraType.Game)
        {
            return;
        }

        m_RenderPass.Setup(material);
        renderer.EnqueuePass(m_RenderPass);
    }

    protected override void Dispose(bool disposing)
    {
        m_RenderPass?.DisposePass();
        m_RenderPass = null;
        CoreUtils.Destroy(m_FallbackMaterial);
        m_FallbackMaterial = null;
    }

    private Material ResolveMaterial()
    {
        Material controllerMaterial = FieldDomainScreenEffectController.Instance?.GetEffectMaterial();
        if (controllerMaterial != null)
        {
            return controllerMaterial;
        }

        EnsureFallbackMaterial();
        return m_FallbackMaterial;
    }

    private void EnsureFallbackMaterial()
    {
        if (m_FallbackMaterial != null)
        {
            return;
        }

        if (effectShader == null)
        {
            effectShader = Shader.Find("Hidden/TurnChange/FieldDomainEffect");
        }

        if (effectShader != null)
        {
            m_FallbackMaterial = CoreUtils.CreateEngineMaterial(effectShader);
        }
    }

    private sealed class FieldDomainRenderPass : ScriptableRenderPass
    {
        private static readonly int BlitTextureId = Shader.PropertyToID("_BlitTexture");
        private static readonly int BlitScaleBiasId = Shader.PropertyToID("_BlitScaleBias");
        private static readonly MaterialPropertyBlock s_PropertyBlock = new MaterialPropertyBlock();

        private Material m_Material;
        private RTHandle m_CopiedColor;

        public FieldDomainRenderPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            profilingSampler = new ProfilingSampler("FieldDomainEffect");
        }

        public void Setup(Material material)
        {
            m_Material = material;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ResetTarget();

            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.msaaSamples = 1;
            descriptor.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(
                ref m_CopiedColor,
                descriptor,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_FieldDomainColorCopy");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_Material == null || m_CopiedColor == null || !FieldDomainScreenEffectController.IsRendering)
            {
                return;
            }

            FieldDomainScreenEffectController controller = FieldDomainScreenEffectController.Instance;
            controller?.ApplyToMaterial(m_Material);

            CommandBuffer cmd = CommandBufferPool.Get("FieldDomainEffect");
            RTHandle cameraColor = renderingData.cameraData.renderer.cameraColorTargetHandle;

            using (new ProfilingScope(cmd, profilingSampler))
            {
                Blitter.BlitCameraTexture(cmd, cameraColor, m_CopiedColor, 0f, false);

                CoreUtils.SetRenderTarget(cmd, cameraColor);
                s_PropertyBlock.Clear();
                controller?.ApplyToPropertyBlock(s_PropertyBlock);
                s_PropertyBlock.SetTexture(BlitTextureId, m_CopiedColor);
                s_PropertyBlock.SetVector(BlitScaleBiasId, new Vector4(1f, 1f, 0f, 0f));
                cmd.DrawProcedural(
                    Matrix4x4.identity,
                    m_Material,
                    0,
                    MeshTopology.Triangles,
                    3,
                    1,
                    s_PropertyBlock);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void DisposePass()
        {
            m_CopiedColor?.Release();
            m_CopiedColor = null;
        }
    }
}
