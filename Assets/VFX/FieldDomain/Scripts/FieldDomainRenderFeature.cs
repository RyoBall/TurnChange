using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FieldDomainRenderFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader effectShader;

    private FieldDomainRenderPass m_RenderPass;
    private Material m_FallbackMaterial;

    public override void Create()
    {
        m_RenderPass ??= new FieldDomainRenderPass();
        EnsureFallbackMaterial();
        m_RenderPass.Setup(ResolveMaterial());
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
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
        m_RenderPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        renderer.EnqueuePass(m_RenderPass);
    }

    protected override void Dispose(bool disposing)
    {
        m_RenderPass?.DisposePass();
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
        private const string ProfilerTag = "FieldDomainEffect";

        private Material m_Material;
        private RTHandle m_TempColorTarget;

        public void Setup(Material material)
        {
            m_Material = material;
            profilingSampler = new ProfilingSampler(ProfilerTag);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (m_Material == null)
            {
                return;
            }

            FieldDomainScreenEffectController.Instance?.ApplyToMaterial(m_Material);

            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(
                ref m_TempColorTarget,
                descriptor,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_FieldDomainTempColor");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_Material == null || !FieldDomainScreenEffectController.IsRendering)
            {
                return;
            }

            FieldDomainScreenEffectController.Instance?.ApplyToMaterial(m_Material);

            CommandBuffer cmd = CommandBufferPool.Get(ProfilerTag);
            RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;

            Blitter.BlitCameraTexture(cmd, source, m_TempColorTarget, m_Material, 0);
            Blitter.BlitCameraTexture(cmd, m_TempColorTarget, source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void DisposePass()
        {
            m_TempColorTarget?.Release();
            m_TempColorTarget = null;
        }
    }
}
