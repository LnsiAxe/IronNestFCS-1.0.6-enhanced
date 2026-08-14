using MelonLoader;

namespace IronNestFCS.Logic;

/// <summary>
/// 全局配置(通过 MelonLoader 的 MelonPreferences 生成 UserData/MelonPreferences.cfg,
/// 即 ini 格式的 config 文件,首次运行自动生成,可手改后重启游戏/F9 重载生效)。
/// 包含:快捷键映射 + 进关卡默认开启的功能。
/// </summary>
public static class FcsConfig
{
    private static MelonPreferences_Category? _cat;

    // ---- 键位 ----
    public static MelonPreferences_Entry<string> KeySweep = null!;
    public static MelonPreferences_Entry<string> KeyMarker = null!;
    public static MelonPreferences_Entry<string> KeyValveOff = null!;
    public static MelonPreferences_Entry<string> KeyValveOn = null!;
    public static MelonPreferences_Entry<string> KeyAbortLeft = null!;
    public static MelonPreferences_Entry<string> KeyAbortRight = null!;
    public static MelonPreferences_Entry<string> KeyAbortBoth = null!;
    public static MelonPreferences_Entry<string> KeyFire1 = null!;
    public static MelonPreferences_Entry<string> KeyFire2 = null!;
    public static MelonPreferences_Entry<string> KeyFire3 = null!;
    public static MelonPreferences_Entry<string> KeyFire4 = null!;
    public static MelonPreferences_Entry<string> KeyReload = null!;

    // ---- 默认开启的功能 ----
    public static MelonPreferences_Entry<bool> AutoFireDefault = null!;
    public static MelonPreferences_Entry<bool> AutoMarkersDefault = null!;
    public static MelonPreferences_Entry<bool> AutoSweepDefault = null!;
    public static MelonPreferences_Entry<bool> MaxChargeDefault = null!;

    private static bool _initialized;

    /// <summary>创建/读取配置分类。可重复调用(幂等,兼容 F9 热重载后 Logic 重新加载)。</summary>
    public static void Init()
    {
        if (_initialized) return;
        _initialized = true;

        // 分类已存在(热重载后)则复用,避免 CreateCategory/CreateEntry 重复创建报错。
        // 不手动 SetFilePath:MelonPreferences 默认保存到 UserData/MelonPreferences.cfg。
        _cat = MelonPreferences.GetCategory("IronNestFCS")
               ?? MelonPreferences.CreateCategory("IronNestFCS", "IronNestFCS 火控 Mod 设置");

        KeySweep = GetOrCreate("Key_SweepToggle", "Numpad0", "持续扫荡开关(键名参考 UnityEngine.InputSystem.Key 枚举)");
        KeyMarker = GetOrCreate("Key_MarkerToggle", "Numpad5", "雷达自动标点切换");
        KeyValveOff = GetOrCreate("Key_ValveOff", "NumpadMinus", "关闭全部蒸汽阀门");
        KeyValveOn = GetOrCreate("Key_ValveOn", "NumpadPlus", "打开全部蒸汽阀门");
        KeyAbortLeft = GetOrCreate("Key_AbortLeft", "Numpad7", "重置左炮");
        KeyAbortRight = GetOrCreate("Key_AbortRight", "Numpad8", "重置右炮");
        KeyAbortBoth = GetOrCreate("Key_AbortBoth", "Numpad9", "重置双炮");
        KeyFire1 = GetOrCreate("Key_FireTarget1", "Numpad1", "打击目标 1(其余目标 2/3/4 同理)");
        KeyFire2 = GetOrCreate("Key_FireTarget2", "Numpad2");
        KeyFire3 = GetOrCreate("Key_FireTarget3", "Numpad3");
        KeyFire4 = GetOrCreate("Key_FireTarget4", "Numpad4");
        KeyReload = GetOrCreate("Key_Reload", "F9", "热重载火控逻辑");

        AutoFireDefault = GetOrCreate("AutoFire_Default", true, "进关卡后默认开启自动开火");
        AutoMarkersDefault = GetOrCreate("AutoMarkers_Default", true, "进关卡后默认开启雷达自动标点");
        AutoSweepDefault = GetOrCreate("AutoSweep_Default", false, "进关卡后默认开启持续扫荡");
        MaxChargeDefault = GetOrCreate("MaxCharge_Default", false, "进关卡后默认开启最大装药");

        _cat.SaveToFile(false);
    }

    /// <summary>条目已存在则读取,否则创建(兼容热重载)。</summary>
    private static MelonPreferences_Entry<T> GetOrCreate<T>(string name, T defaultValue, string description = "")
    {
        var existing = _cat.GetEntry<T>(name);
        if (existing != null) return existing;
        return string.IsNullOrEmpty(description)
            ? _cat.CreateEntry(name, defaultValue)
            : _cat.CreateEntry(name, defaultValue, description);
    }
}
