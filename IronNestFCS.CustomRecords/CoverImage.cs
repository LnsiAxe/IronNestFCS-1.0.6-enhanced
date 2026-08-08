using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using Object = UnityEngine.Object;

namespace IronNestFCS.CustomRecords;

/// <summary>
/// 把音频内嵌封面字节合成成唱片贴图：
/// 1) 建一张 1024x1024、整张填充 #0B0B0B 的底图；
/// 2) 把封面等比/拉伸缩放到 412x412，贴在正中央；
/// 3) 用直径 412 的圆形遮罩把这块封面裁成圆形（圆外恢复成底色），边缘做 1px 抗锯齿。
///
/// 全程只用了 IL2CPP 裁剪后仍保留的 Texture2D API：LoadImage / GetPixels32 /
/// SetPixels32 / Apply（已验证这些在 UnityEngine.CoreModule interop stub 里存在）。
/// 缩放用自写双线性，避免依赖被裁的 Graphics/Blit-to-Texture2D 读回路径。
/// </summary>
internal static class CoverImage
{
    private const int CanvasSize = 1024;
    private const int CoverSize = 412;
    // 底色 #0B0B0B，完全不透明。
    private static readonly Color32 Background = new Color32(0x0B, 0x0B, 0x0B, 0xFF);

    /// <summary>
    /// 从封面原始字节(PNG/JPEG)构造合成后的 1024x1024 唱片贴图。
    /// 解码失败返回 null（调用方应跳过该文件）。
    /// </summary>
    internal static Texture2D? Build(byte[] coverBytes)
    {
        // 先把封面解码进一张临时可读纹理。LoadImage 会按图实际尺寸 Reinitialize。
        var srcTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!ImageConversion.LoadImage(srcTex, new Il2CppStructArray<byte>(coverBytes), false))
        {
            Object.Destroy(srcTex);
            return null;
        }

        int srcW = srcTex.width, srcH = srcTex.height;
        // GetPixels32 返回交错的 Color32[]，行序自底向上（Unity 约定）。
        // 我们整套合成都在这个“自底向上”的坐标系里做，圆形对称所以上下翻转无所谓。
        Color32[] src = ToManaged(srcTex.GetPixels32());
        Object.Destroy(srcTex); // 源纹理用完即弃。

        // 1) 把封面缩放到 412x412（双线性）。
        Color32[] cover = ScaleBilinear(src, srcW, srcH, CoverSize, CoverSize);

        // 2) 建 1024x1024 底图，整张填 #0B0B0B。
        var canvas = new Color32[CanvasSize * CanvasSize];
        for (int i = 0; i < canvas.Length; i++)
            canvas[i] = Background;

        // 3) 居中贴入并按圆形遮罩混合。
        int offset = (CanvasSize - CoverSize) / 2; // 306
        float radius = CoverSize / 2f;             // 206
        float cx = (CoverSize - 1) / 2f;
        float cy = (CoverSize - 1) / 2f;

        for (int y = 0; y < CoverSize; y++)
        {
            for (int x = 0; x < CoverSize; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // 圆外直接保持底色；圆内用封面色；边缘 1px 做线性 alpha 抗锯齿。
                float coverage = Mathf.Clamp01(radius - dist); // dist<=r-1→1, dist>=r→0
                if (coverage <= 0f)
                    continue;

                int ci = y * CoverSize + x;
                int canvasIdx = (y + offset) * CanvasSize + (x + offset);
                Color32 fg = cover[ci];
                if (coverage >= 1f)
                {
                    canvas[canvasIdx] = new Color32(fg.r, fg.g, fg.b, 0xFF);
                }
                else
                {
                    // 与底色按 coverage 混合，得到柔和圆边。
                    Color32 bg = canvas[canvasIdx];
                    canvas[canvasIdx] = LerpRgb(bg, fg, coverage);
                }
            }
        }

        // 写回一张正式纹理。
        var outTex = new Texture2D(CanvasSize, CanvasSize, TextureFormat.RGBA32, false);
        outTex.SetPixels32(new Il2CppStructArray<Color32>(canvas));
        outTex.Apply();
        return outTex;
    }

    /// <summary>Il2Cpp Color32 数组拷成托管数组，便于在托管侧高速随机访问。</summary>
    private static Color32[] ToManaged(Il2CppStructArray<Color32> arr)
    {
        var managed = new Color32[arr.Length];
        for (int i = 0; i < arr.Length; i++)
            managed[i] = arr[i];
        return managed;
    }

    /// <summary>
    /// 双线性缩放 Color32 位图。源/目标都按行优先、自底向上布局（与 GetPixels32 一致）。
    /// </summary>
    private static Color32[] ScaleBilinear(Color32[] src, int sw, int sh, int dw, int dh)
    {
        var dst = new Color32[dw * dh];
        // 用 dw-1 / dh-1 做比例，保证目标四角精确映射到源四角。
        float sx = sw > 1 ? (float)(sw - 1) / (dw - 1) : 0f;
        float sy = sh > 1 ? (float)(sh - 1) / (dh - 1) : 0f;

        for (int y = 0; y < dh; y++)
        {
            float fy = y * sy;
            int y0 = (int)fy;
            int y1 = Mathf.Min(y0 + 1, sh - 1);
            float wy = fy - y0;

            for (int x = 0; x < dw; x++)
            {
                float fx = x * sx;
                int x0 = (int)fx;
                int x1 = Mathf.Min(x0 + 1, sw - 1);
                float wx = fx - x0;

                Color32 c00 = src[y0 * sw + x0];
                Color32 c10 = src[y0 * sw + x1];
                Color32 c01 = src[y1 * sw + x0];
                Color32 c11 = src[y1 * sw + x1];

                dst[y * dw + x] = BilerpColor(c00, c10, c01, c11, wx, wy);
            }
        }
        return dst;
    }

    private static Color32 BilerpColor(Color32 c00, Color32 c10, Color32 c01, Color32 c11, float wx, float wy)
    {
        float top_r = c00.r + (c10.r - c00.r) * wx;
        float top_g = c00.g + (c10.g - c00.g) * wx;
        float top_b = c00.b + (c10.b - c00.b) * wx;
        float top_a = c00.a + (c10.a - c00.a) * wx;

        float bot_r = c01.r + (c11.r - c01.r) * wx;
        float bot_g = c01.g + (c11.g - c01.g) * wx;
        float bot_b = c01.b + (c11.b - c01.b) * wx;
        float bot_a = c01.a + (c11.a - c01.a) * wx;

        return new Color32(
            (byte)(top_r + (bot_r - top_r) * wy),
            (byte)(top_g + (bot_g - top_g) * wy),
            (byte)(top_b + (bot_b - top_b) * wy),
            (byte)(top_a + (bot_a - top_a) * wy));
    }

    /// <summary>按 t 在两色间线性插值 RGB，结果不透明。</summary>
    private static Color32 LerpRgb(Color32 a, Color32 b, float t)
    {
        return new Color32(
            (byte)(a.r + (b.r - a.r) * t),
            (byte)(a.g + (b.g - a.g) * t),
            (byte)(a.b + (b.b - a.b) * t),
            0xFF);
    }
}
