using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using IronNestFCS.CustomRecords;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

[assembly: MelonInfo(typeof(IronNestFCS.CustomRecords.CustomRecordsMod), 
    "IronNestFCS.CustomRecords", "1.0.0", "svr2kos2")]
[assembly: MelonGame("Iron Nest", "Iron Nest: Heavy Turret Simulator")]

namespace IronNestFCS.CustomRecords;

/// <summary>
/// 独立的 MelonMod：扫描 UserData/CustomRecords 下所有“带内嵌封面”的音频文件
/// （.mp3 / .wav / .flac），为每个文件克隆一张场景里的 RecordDisk，
/// 用该文件的封面合成唱片贴图、用其解码后的 PCM 作为音轨。
///
/// 用户只需把音频文件丢进 CustomRecords 即可，无需再手工准备 a.wav / diskTexture.png，
/// 也不再从 StreamingAssets 读取素材。
///
/// 解码与标签读取走纯托管库（NAudio / NAudio.Flac / TagLib#），不受本作 IL2CPP 裁剪影响；
/// 只有“把结果塞回 Unity”这一步受 IL2CPP 约束，沿用已验证可用的 API（见各处注释）。
/// 进场景后轮询 RecordDisk，出现时执行一次。
/// </summary>
public class CustomRecordsMod : MelonMod
{
    private readonly List<GameObject> diskClones = new();
    // 每张克隆盘各自持有的流式音轨状态：PCMReaderCallback 按需供数，必须保活托管侧
    // 样本缓冲与读游标，否则被 GC 回收后回调里访问就是野指针。模块存活期间一直持有。
    private readonly List<TrackPlayback> playbacks = new();
    private bool done;

    public override void OnUpdate()
    {
        if (done)
            return;

        // 轮询 RecordDisk 出现即执行一次。
        var src = GameObject.Find("RecordDisk");
        if (src == null)
            return;

        done = true;
        try
        {
            CreateCustomRecordss(src);
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"[CustomRecords] Failed to create custom disks: {ex}");
        }
    }

    public override void OnDeinitializeMelon()
    {
        foreach (var clone in diskClones)
        {
            if (clone != null)
                Object.Destroy(clone);
        }
        diskClones.Clear();
        playbacks.Clear();
    }

    /// <summary>
    /// 列出 CustomRecords 里所有受支持且带封面的文件，逐个克隆 RecordDisk 并装配。
    /// 多张盘沿原盘朝同一方向依次排开，避免叠在一起。
    /// </summary>
    private void CreateCustomRecordss(GameObject src)
    {
        var dir = System.IO.Path.Combine(MelonEnvironment.UserDataDirectory, "CustomRecords");
        if (!System.IO.Directory.Exists(dir))
        {
            System.IO.Directory.CreateDirectory(dir);
            MelonLogger.Msg($"[CustomRecords] Created empty folder, drop audio files here: {dir}");
            return;
        }

        // 枚举受支持扩展名的文件，按文件名排序得到稳定顺序。
        var files = new List<string>();
        foreach (var f in System.IO.Directory.GetFiles(dir))
        {
            if (AudioImport.IsSupported(f))
                files.Add(f);
        }
        files.Sort(StringComparer.OrdinalIgnoreCase);

        if (files.Count == 0)
        {
            MelonLogger.Msg($"[CustomRecords] No supported audio (.mp3/.wav/.flac) in {dir}");
            return;
        }

        int placed = 0;
        foreach (var file in files)
        {
            try
            {
                // 只处理带封面的文件——这是本 mod 的约定。无封面者跳过并提示。
                var cover = TagReader.ReadCover(file);
                if (cover == null)
                {
                    MelonLogger.Warning($"[CustomRecords] Skip (no embedded cover): {System.IO.Path.GetFileName(file)}");
                    continue;
                }

                if (CreateOneDisk(src, file, cover, placed))
                    placed++;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[CustomRecords] Failed on {System.IO.Path.GetFileName(file)}: {ex}");
            }
        }

        MelonLogger.Msg($"[CustomRecords] Done: {placed} disk(s) created from {files.Count} file(s).");
    }

    /// <summary>
    /// 为单个音频文件克隆一张盘，装配合成封面贴图与流式音轨。
    /// <paramref name="index"/> 用于沿原盘前方依次错开摆放。
    /// </summary>
    private bool CreateOneDisk(GameObject src, string file, byte[] cover, int index)
    {
        // 先解码音频，失败就别建盘了。
        float[] samples;
        int channels, sampleRate;
        try
        {
            samples = AudioImport.Decode(file, out channels, out sampleRate);
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"[CustomRecords] Decode failed for {System.IO.Path.GetFileName(file)}: {ex}");
            return false;
        }
        if (samples.Length == 0 || channels <= 0 || sampleRate <= 0)
        {
            MelonLogger.Warning($"[CustomRecords] Empty/invalid audio: {System.IO.Path.GetFileName(file)}");
            return false;
        }

        // 合成封面贴图。
        var tex = CoverImage.Build(cover);
        if (tex == null)
        {
            MelonLogger.Warning($"[CustomRecords] Cover decode failed: {System.IO.Path.GetFileName(file)}");
            return false;
        }

        // 克隆盘并沿原盘 -forward 方向按 index 依次错开 0.5 单位。
        var disk = Object.Instantiate(src);
        diskClones.Add(disk);
        var pos = src.transform.position - Vector3.fwd * (0.2f * ((index > 4 ? 4 : index) + 1))
            + Vector3.up * (0.02f * (index > 4 ? index - 4 : 0));
        disk.transform.position = pos;
        disk.transform.rotation = src.transform.rotation;

        var recordItem = disk.GetComponent<RecordItem>();

        // 装配流式音轨。
        var playback = new TrackPlayback(samples, channels);
        playbacks.Add(playback);
        AssignTrack(recordItem, playback, sampleRate);

        // 替换唱片贴图。
        var renderer = recordItem.transform.FindChild("Record Disk Blend").GetComponent<MeshRenderer>();
        renderer.material.mainTexture = tex;

        // 尽力设置显示名（字段在不同版本可能不同，存在才设）。
        TrySetDisplayName(recordItem, TagReader.ReadTitle(file));

        MelonLogger.Msg($"[CustomRecords] + {System.IO.Path.GetFileName(file)}  " +
                        $"({(float)(samples.Length / channels) / sampleRate:F1}s, {channels}ch@{sampleRate}Hz)");
        return true;
    }

    /// <summary>
    /// 用带 PCMReaderCallback 的 AudioClip.Create 构造流式音轨并写入 RecordItem.tracks。
    /// 不用 DownloadHandlerAudioClip / AudioClip.SetData：本作 IL2CPP 把它们的 ctor / Span
    /// 依赖裁掉了，运行时会 MissingMethodException。PCMReaderCallback 重载是纯
    /// il2cpp_runtime_invoke，无 Span 依赖，Unity 播放时回调拉取 PCM。
    /// </summary>
    private static void AssignTrack(RecordItem recordItem, TrackPlayback playback, int sampleRate)
    {
        int lengthSamples = playback.LengthPerChannel; // 每声道采样数

        // stream=true：Unity 不一次性缓冲，反复回调取数据，适合循环。
        // 回调里禁止访问 IL2CPP 复杂对象，只读写传入的 float 数组，最安全。
        AudioClip.PCMReaderCallback reader = (System.Action<Il2CppStructArray<float>>)playback.PcmRead;
        AudioClip.PCMSetPositionCallback setPos = (System.Action<int>)playback.PcmSetPosition;
        var clip = AudioClip.Create("CustomRecord", lengthSamples, playback.Channels, sampleRate, true, reader, setPos);

        recordItem.tracks = new Il2CppReferenceArray<AudioClip>(new[] { clip });
        recordItem.loop = true;
    }

    /// <summary>
    /// 反射设置常见的显示名字段（displayName / nameText 等）。字段缺失或类型不符就静默跳过——
    /// 这只是锦上添花，失败不影响音轨与贴图。
    /// </summary>
    private static void TrySetDisplayName(RecordItem recordItem, string title)
    {
        try
        {
            var prop = recordItem.GetType().GetProperty("displayName");
            if (prop != null && prop.PropertyType == typeof(string) && prop.CanWrite)
                prop.SetValue(recordItem, title);
        }
        catch
        {
            // 显示名是可选项，忽略任何失败。
        }
    }
}

/// <summary>
/// 单张盘的流式播放状态：持有交错 float 样本与读游标，提供 PCM 回调。
/// 每张盘一个实例，互不干扰。被 CustomRecordsMod 长期持有以防 GC。
/// </summary>
internal sealed class TrackPlayback
{
    private readonly float[] _samples; // 交错样本，范围 [-1,1]
    private int _readPos;

    internal int Channels { get; }
    internal int LengthPerChannel => _samples.Length / Channels;

    internal TrackPlayback(float[] samples, int channels)
    {
        _samples = samples;
        Channels = channels;
    }

    /// <summary>
    /// PCMReaderCallback：Unity 每次要播一段时调用，把接下来的样本写满 data。
    /// 缓冲读完即环回（配合 RecordItem.loop / AudioSource 循环）。游标自增。
    /// </summary>
    internal void PcmRead(Il2CppStructArray<float> data)
    {
        var src = _samples;
        if (src.Length == 0)
        {
            for (int i = 0; i < data.Length; i++) data[i] = 0f;
            return;
        }
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = src[_readPos];
            if (++_readPos >= src.Length) _readPos = 0;
        }
    }

    /// <summary>
    /// PCMSetPositionCallback：Unity seek 或循环回起点时调用，参数是“每声道”采样位置。
    /// 换算成交错缓冲的绝对索引。循环回零时 position 多为 0。
    /// </summary>
    internal void PcmSetPosition(int positionSamples)
    {
        var src = _samples;
        if (src.Length == 0) { _readPos = 0; return; }
        if (positionSamples <= 0) { _readPos = 0; return; }
        // 每声道位置 → 交错绝对索引，并钳制到缓冲范围内。
        long abs = (long)positionSamples * Channels;
        _readPos = (int)Math.Min(abs, src.Length - 1);
    }
}
