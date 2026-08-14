using System.Collections;
using IronNestFCS;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

[assembly: MelonInfo(typeof(FcsHostMod), "IronNestFCS", "1.0.7.1", "svr2kos2")]
[assembly: MelonGame("Iron Nest", "Iron Nest Heavy Turret Simulator")]

namespace IronNestFCS;

/// <summary>
/// 稳定的宿主 Mod。启动时加载一次，永不重载。
/// 职责：首次加载 Logic、监听 F9 触发热重载、把生命周期回调转发给 Logic。
/// 所有高频改动的火控代码都在 Logic 程序集里。
/// </summary>
public class FcsHostMod : MelonMod
{
    // 游戏启用了新 Input System，旧的 UnityEngine.Input 会直接抛异常，
    // 因此通过 Keyboard.current 读取热重载键（默认 F9，可在 config.ini 里改 Key_Reload）。
    private const string ReloadKeyName = "F9";

    // Logic 程序集放在 UserData 下、而非 Mods/，避免被 MelonLoader 当作 mod 自动加载。
    // 类型全名必须与 Logic 项目里的实现类一致。
    private const string LogicTypeName = "IronNestFCS.Logic.FcsModule";

    private LogicReloader? reloader;

    public override void OnInitializeMelon()
    {
        string logicDir = Path.Combine(MelonEnvironment.UserDataDirectory, "IronNestFCS");
        Directory.CreateDirectory(logicDir);
        string logicDll = Path.Combine(logicDir, "IronNestFCS.Logic.dll");

        MelonLogger.Msg($"IronNestFCS Host Started。Logic path: {logicDll}");
        MelonLogger.Msg($"Press {ReloadKeyName} to hot reload Logic.");

        reloader = new LogicReloader(logicDll, LogicTypeName);
        reloader.Reload();
    }

    /// <summary>从 config.ini(MelonPreferences 分类 IronNestFCS)读取热重载键名。</summary>
    private static string GetReloadKeyName()
    {
        try
        {
            var cat = MelonPreferences.GetCategory("IronNestFCS");
            var entry = cat?.GetEntry<string>("Key_Reload");
            if (entry != null && !string.IsNullOrWhiteSpace(entry.Value))
                return entry.Value;
        }
        catch { /* 配置未生成/读取失败时用默认 */ }
        return ReloadKeyName;
    }

    /// <summary>用新 Input System 读热重载键，避免触碰会抛异常的 UnityEngine.Input。</summary>
    private static bool ReloadKeyPressed()
    {
        Keyboard? kb = Keyboard.current;
        if (kb == null) return false;
        var name = GetReloadKeyName();
        if (!System.Enum.TryParse<Key>(name, out var key)) return false;
        return kb[key].wasPressedThisFrame;
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        MelonCoroutines.Start(ReloadCoroutine());
    }
    
    private IEnumerator ReloadCoroutine()
    {
        yield return new WaitForSeconds(3f);
        reloader?.Reload();
    }

    public override void OnUpdate()
    {
        if (reloader == null)
            return;

        if (ReloadKeyPressed() || reloader.CheckDllUpdated())
        {
            MelonLogger.Msg($"[{ReloadKeyName}] Hot reloading...");
            reloader.Reload();
            return; // 本帧不再 Update，避免对刚换上的实例做半截调用
        }

        try { reloader.Current?.Update(); }
        catch (Exception ex) { MelonLogger.Error($"Logic.Update() exception: {ex}"); }
    }

    public override void OnGUI()
    {
        if (reloader?.Current == null)
            return;

        try { reloader.Current.OnGui(); }
        catch (Exception ex) { MelonLogger.Error($"Logic.OnGui() exception: {ex}"); }
    }

    public override void OnDeinitializeMelon()
    {
        reloader?.Unload();
        reloader = null;
    }
}
