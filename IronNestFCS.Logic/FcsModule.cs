using Il2Cpp;
using IronNestFCS.Abstractions;
using IronNestFCS.Logic.FCS;
using MelonLoader;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IronNestFCS.Logic;

public class FcsModule : IFcsModule
{
    private const int RoleArtillery = 128;
    private const int RoleFortification = 65536;
    private const int RoleTank = 262144;
    private const int RoleAlly = 2;
    private const int RoleEnemy = 1;
    private const int RoleTarget = 32;

    private readonly FSC fcs = new();
    private FcsWindow? window;
    private TacticalRadar? radar;

    private bool autoSweep;
    private readonly HashSet<EntityLocation> swept = new(new EntityLocationComparer());

    public bool Initialize()
    {
        window = new FcsWindow(fcs);
        radar = new TacticalRadar(fcs);
        bool bound = fcs.TryBind();
        return bound;
    }

    public void Update()
    {
        fcs.Update();
        radar?.Update();

        if (window != null) window.AutoSweepEnabled = autoSweep;

        if (autoSweep && radar != null && fcs.IsBound)
        {
            var alive = radar.AliveUnits;
            var sorted = alive.OrderByDescending(u => GetPriority(u.Location)).ToList();
            foreach (var unit in sorted)
            {
                if (unit.Location != null && swept.Add(unit.Location))
                {
                    int prio = GetPriority(unit.Location);
                    if (prio >= 3)
                        fcs.FireAtWorldPosFront(swept.Count, unit.WorldPos);
                    else
                        fcs.FireAtWorldPos(swept.Count, unit.WorldPos);
                }
            }
        }

        var kb = Keyboard.current;
        if (kb == null || !fcs.IsBound)
            return;

        bool ctrl = kb.ctrlKey.isPressed;

        if (kb.numpad0Key.wasPressedThisFrame || (ctrl && kb.digit0Key.wasPressedThisFrame))
        {
            autoSweep = !autoSweep;
            if (autoSweep)
            {
                if (radar != null) radar.AutoPlaceMarkers = true;
                SweepAllHostiles();
            }
            return;
        }
        if (kb.numpad5Key.wasPressedThisFrame || (ctrl && kb.digit5Key.wasPressedThisFrame))
        {
            if (radar != null) radar.AutoPlaceMarkers = !radar.AutoPlaceMarkers;
            return;
        }
        if (kb.numpadMinusKey.wasPressedThisFrame) { AdjustAllValves(0f); return; }
        if (kb.numpadPlusKey.wasPressedThisFrame) { AdjustAllValves(999f); return; }
        if (kb.numpad7Key.wasPressedThisFrame || (ctrl && kb.digit7Key.wasPressedThisFrame)) { fcs.AbortGun(LeftRight.Left); return; }
        if (kb.numpad8Key.wasPressedThisFrame || (ctrl && kb.digit8Key.wasPressedThisFrame)) { fcs.AbortGun(LeftRight.Right); return; }
        if (kb.numpad9Key.wasPressedThisFrame || (ctrl && kb.digit9Key.wasPressedThisFrame)) { fcs.AbortGun(LeftRight.Left); fcs.AbortGun(LeftRight.Right); return; }
        if (kb.numpad1Key.wasPressedThisFrame || (ctrl && kb.digit1Key.wasPressedThisFrame)) fcs.FireTarget(1);
        else if (kb.numpad2Key.wasPressedThisFrame || (ctrl && kb.digit2Key.wasPressedThisFrame)) fcs.FireTarget(2);
        else if (kb.numpad3Key.wasPressedThisFrame || (ctrl && kb.digit3Key.wasPressedThisFrame)) fcs.FireTarget(3);
        else if (kb.numpad4Key.wasPressedThisFrame || (ctrl && kb.digit4Key.wasPressedThisFrame)) fcs.FireTarget(4);
    }

    /// <summary>NumpadPlus/Minus: 控制所有蒸汽阀门开/关</summary>
    private static void AdjustAllValves(float value)
    {
        var all = GameObject.FindObjectsOfType<GameObject>();
        MelonLogger.Msg($"[Valve] Setting all valves to {value}...");
        int done = 0;
        foreach (var leak in all)
        {
            if (leak == null || !leak.name.ToLower().Contains("steam leak")) continue;
            DialInteractable? nearestDi = null;
            float minDist = float.MaxValue;
            foreach (var go in all)
            {
                if (go == null) continue;
                var di = go.GetComponent<DialInteractable>();
                if (di == null) continue;
                var d = (go.transform.position - leak.transform.position).magnitude;
                if (d < minDist) { minDist = d; nearestDi = di; }
            }
            if (nearestDi == null) continue;
            nearestDi.SetDialValue(value);
            done++;
        }
        MelonLogger.Msg($"[Valve] Set {done} valves to {value}.");
    }

    private static int GetPriority(EntityLocation? loc)
    {
        if (loc == null) return 1;
        try
        {
            var entityProp = loc.GetType().GetProperty("Entity", BindingFlags.Public | BindingFlags.Instance);
            if (entityProp == null) return 1;
            var entity = entityProp.GetValue(loc);
            if (entity == null) return 1;
            var entType = entity.GetType();

            var roleProp = entType.GetProperty("Role", BindingFlags.Public | BindingFlags.Instance);
            int roleVal = -1;
            if (roleProp != null)
            {
                var v = roleProp.GetValue(entity);
                if (v is int i) roleVal = i;
                else if (v is Enum e) roleVal = Convert.ToInt32(e);
            }

            int stars = 0;
            var starsProp = entType.GetProperty("Stars", BindingFlags.Public | BindingFlags.Instance);
            if (starsProp != null) { var sv = starsProp.GetValue(entity); if (sv is int si) stars = si; }

            bool isFdc = false;
            var iconProp = entType.GetProperty("Icon", BindingFlags.Public | BindingFlags.Instance);
            if (iconProp != null) { var v = iconProp.GetValue(entity); if (v is string s && s.ToLower().Contains("fire direction")) isFdc = true; }

            if (roleVal >= 0)
            {
                if ((roleVal & RoleAlly) != 0) return 0;
                if (stars >= 3) return 4;
                if (isFdc) return 4;
                if ((roleVal & RoleArtillery) != 0) return 4;
                if (stars >= 1) return 3;
                if ((roleVal & RoleEnemy) != 0 || (roleVal & RoleTarget) != 0)
                {
                    bool armored = (roleVal & RoleFortification) != 0 || (roleVal & RoleTank) != 0;
                    return armored ? 3 : 2;
                }
            }
            if (iconProp != null) { var v2 = iconProp.GetValue(entity); if (v2 is string s2 && s2.ToLower().Contains("enemy")) return 2; }
        }
        catch { }
        return 1;
    }

    private void SweepAllHostiles()
    {
        var alive = radar?.AliveUnits;
        if (alive == null || alive.Count == 0) return;
        var sorted = alive.OrderByDescending(u => GetPriority(u.Location)).ToList();
        for (int i = 0; i < sorted.Count; i++)
        {
            if (sorted[i].Location != null)
                swept.Add(sorted[i].Location);
            int prio = GetPriority(sorted[i].Location);
            if (prio >= 3)
                fcs.FireAtWorldPosFront(i + 1, sorted[i].WorldPos);
            else
                fcs.FireAtWorldPos(i + 1, sorted[i].WorldPos);
        }
    }

    public void OnGui()
    {
        window?.OnGui();
        radar?.OnGui();
    }

    public void Shutdown()
    {
        fcs.Dispose();
        window = null;
        radar = null;
    }
}

internal sealed class EntityLocationComparer : IEqualityComparer<EntityLocation>
{
    public bool Equals(EntityLocation? x, EntityLocation? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        return x.Pointer == y.Pointer;
    }

    public int GetHashCode(EntityLocation obj)
    {
        return obj.Pointer.GetHashCode();
    }
}
