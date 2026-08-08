using CSCore;
using CSCore.Codecs;

namespace IronNestFCS.CustomRecords;

/// <summary>
/// 用 CSCore 把磁盘上的音频文件解码成交错(interleaved) float 样本数组。
/// 支持 .mp3 / .wav / .flac（由 CSCore 的 CodecFactory 按扩展名自动选择解码器）。
/// 纯托管解码路径，不碰 Unity/IL2CPP，所以不受本作 IL2CPP 裁剪
///（DownloadHandlerAudioClip / AudioClip.SetData 等被裁）影响。
///
/// 选 CSCore 而非 NAudio + NAudio.Flac：后者的 NAudio.Flac 只兼容 NAudio 1.7.3，
/// 其 FlacReader 继承的 WaveStream 在 NAudio 2.x 里移到了 NAudio.Core，类型冲突无法共存。
/// CSCore 自带 mp3/wav/flac 解码，自给自足，无此版本矛盾。
///
/// 输出样本布局：交错、范围 [-1,1]、float32，直接喂给 AudioClip 的 PCMReaderCallback。
/// </summary>
internal static class AudioImport
{
    /// <summary>支持的音频扩展名（小写，含点）。</summary>
    internal static readonly string[] SupportedExtensions = { ".mp3", ".wav", ".flac" };

    internal static bool IsSupported(string path)
    {
        var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
        return Array.IndexOf(SupportedExtensions, ext) >= 0;
    }

    /// <summary>
    /// 解码 <paramref name="path"/>，返回交错 float 样本；声道数与采样率经 out 返回。
    /// CodecFactory 按扩展名挑解码器；ToSampleSource() 统一转成 float 流，
    /// 屏蔽 16/24/32-bit 与字节序差异。
    /// </summary>
    internal static float[] Decode(string path, out int channels, out int sampleRate)
    {
        // GetCodec 返回 IWaveSource；用 using 确保底层文件流释放。
        using IWaveSource waveSource = CodecFactory.Instance.GetCodec(path);
        ISampleSource sampleSource = waveSource.ToSampleSource();

        var fmt = sampleSource.WaveFormat;
        channels = fmt.Channels;
        sampleRate = fmt.SampleRate;

        // Length 是字节；按 4 字节/float 估算交错样本总数用于预分配。估不准也无妨。
        var all = new List<float>(EstimateSampleCount(sampleSource));
        var buffer = new float[16384];
        int read;
        while ((read = sampleSource.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (int i = 0; i < read; i++)
                all.Add(buffer[i]);
        }

        return all.ToArray();
    }

    /// <summary>
    /// 粗略估算交错样本总数，仅用于 List 预分配以减少扩容。失败则给保守初值。
    /// </summary>
    private static int EstimateSampleCount(ISampleSource source)
    {
        try
        {
            // ISampleSource.Length 以“样本”为单位（已含所有声道）。
            long len = source.Length;
            if (len > 0 && len < int.MaxValue) return (int)len;
        }
        catch
        {
            // 某些流不支持 Length，忽略，走默认初值。
        }
        return 1 << 20; // 1M 样本，约 5.9s 立体声 44.1k，足够避免早期频繁扩容。
    }
}
