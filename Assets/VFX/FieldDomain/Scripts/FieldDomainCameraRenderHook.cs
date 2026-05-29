using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 运行时兜底：即使 Renderer Feature 未正确注册，也通过 endCameraRendering 执行全屏 Blit。
/// </summary>
[DisallowMultipleComponent]
public class FieldDomainCameraRenderHook : MonoBehaviour
{
    [SerializeField] private FieldDomainScreenEffectController effectController;
    [SerializeField] private Camera targetCamera;

    private static FieldDomainCameraRenderHook s_Instance;

    private void Awake()
    {
        if (effectController == null)
        {
            effectController = GetComponent<FieldDomainScreenEffectController>();
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void OnEnable()
    {
        s_Instance = this;
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        if (s_Instance == this)
        {
            s_Instance = null;
        }
    }

    private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera == null || camera.cameraType != CameraType.Game)
        {
            return;
        }

        if (targetCamera != null && camera != targetCamera)
        {
            return;
        }

        if (!FieldDomainScreenEffectController.IsRendering)
        {
            return;
        }

        FieldDomainScreenEffectController controller = effectController != null
            ? effectController
            : FieldDomainScreenEffectController.Instance;
        if (controller == null)
        {
            return;
        }

        Material material = controller.GetEffectMaterial();
        if (material == null)
        {
            return;
        }

        controller.ApplyToMaterial(material);

        CommandBuffer cmd = CommandBufferPool.Get("FieldDomainEffect");
        cmd.Blit(BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.CameraTarget, material, 0);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}
