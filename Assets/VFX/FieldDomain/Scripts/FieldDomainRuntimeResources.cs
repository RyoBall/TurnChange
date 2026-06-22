using UnityEngine;

/// <summary>
/// 打包后可用的场域特效资源加载（Resources 目录）。
/// </summary>
public static class FieldDomainRuntimeResources
{
    private const string FlameNoiseResourcePath = "FieldDomain/NoiseSmooth04";

    private static Texture2D s_FlameNoiseTexture;

    public static Texture2D GetFlameNoiseTexture()
    {
        if (s_FlameNoiseTexture == null)
        {
            s_FlameNoiseTexture = Resources.Load<Texture2D>(FlameNoiseResourcePath);
        }

        return s_FlameNoiseTexture;
    }
}
