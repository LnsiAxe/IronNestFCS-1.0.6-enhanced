namespace IronNestFCS.CustomRecords;

/// <summary>
/// 用 TagLib# 从音频文件里读取内嵌封面与标题。纯托管，不碰 Unity/IL2CPP。
/// TagLib# 只解析元数据，不解码音频——音频解码由 <see cref="AudioImport"/> 负责。
/// </summary>
internal static class TagReader
{
    /// <summary>
    /// 读取 <paramref name="path"/> 的内嵌封面原始字节（PNG/JPEG 等，原样返回）。
    /// 没有封面则返回 null。本 mod 要求“带封面”的文件，无封面者由调用方跳过。
    /// </summary>
    internal static byte[]? ReadCover(string path)
    {
        try
        {
            using var file = TagLib.File.Create(path);
            var pics = file.Tag.Pictures;
            if (pics == null || pics.Length == 0)
                return null;

            // 优先取 FrontCover；没有明确类型则用第一张。
            TagLib.IPicture? chosen = null;
            foreach (var p in pics)
            {
                if (p.Type == TagLib.PictureType.FrontCover)
                {
                    chosen = p;
                    break;
                }
            }
            chosen ??= pics[0];

            var data = chosen.Data?.Data;
            return data is { Length: > 0 } ? data : null;
        }
        catch
        {
            // 损坏的标签 / 不支持的容器：当作无封面处理，让调用方跳过该文件。
            return null;
        }
    }

    /// <summary>
    /// 读取显示用标题：优先 Tag.Title，缺失时回落到不含扩展名的文件名。
    /// </summary>
    internal static string ReadTitle(string path)
    {
        try
        {
            using var file = TagLib.File.Create(path);
            var title = file.Tag.Title;
            if (!string.IsNullOrWhiteSpace(title))
                return title;
        }
        catch
        {
            // ignore，回落到文件名
        }
        return System.IO.Path.GetFileNameWithoutExtension(path);
    }
}
