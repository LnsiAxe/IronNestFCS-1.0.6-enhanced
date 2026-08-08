using System.IO;
using Il2Cpp;
using MelonLoader;
using MelonLoader.Utils;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace IronNestFCS.Logic;

public class UnitEntry
{
    public string Name = "";
    public Vector3 WorldPos;
    public bool IsAlive;
    public EntityLocation? Location;
}

public class TacticalRadar
{
    private readonly FSC fcs;

    private bool showRadar = true;
    private Rect radarRect = new(0, 0, 0, 0);

    public bool AutoPlaceMarkers { get; set; } = true;

    private readonly List<UnitEntry> units = new();
    private float lastScanTime;
    private const float ScanInterval = 3f;

    private static readonly Color ClrTitle = new(0.96f, 0.65f, 0.14f);
    private static readonly Color ClrAlive = new(0.83f, 0.18f, 0.18f);
    private static readonly Color ClrDead = new(0.40f, 0.40f, 0.40f);
    private static readonly Color ClrLabel = new(0.72f, 0.65f, 0.55f);

    private static bool _loggedEntityFields;

    public TacticalRadar(FSC fcs) => this.fcs = fcs;

    public List<UnitEntry> AliveUnits => units.Where(u => u.IsAlive).ToList();

    public void Update()
    {
        if (Time.time - lastScanTime > ScanInterval)
        {
            ScanForUnits();
            lastScanTime = Time.time;
        }
    }

    private void ScanForUnits()
    {
        try
        {
            ScanForUnitsInternal();
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"[Radar] Scan crashed: {ex}");
        }
        FlushLog();
    }

    private void ScanForUnitsInternal()
    {
        units.Clear();

        var fireMissionRoot = GameObject.Find("Fire Mission Root")?.transform;
        var turretRef = GameObject.Find("Player Turret Piece")?.transform;

        Log($"[Radar] Scan start. FireMissionRoot={fireMissionRoot != null} Turret={turretRef != null}");

        if (fireMissionRoot != null)
        {
            Log($"[Radar] FireMissionRoot children: {fireMissionRoot.childCount}");
            for (int i = 0; i < fireMissionRoot.childCount; i++)
            {
                var child = fireMissionRoot.GetChild(i);
                var loc = child.GetComponent<EntityLocation>();
                if (loc == null) continue;
                bool hostile = IsHostile(loc, child);
                bool isAlive = IsUnitAlive(loc, child.gameObject);
                var entityInfo = GetEntityInfo(loc);
                Log($"[Radar] Entity: {child.name}  hostile={hostile}  alive={isAlive}  icon={entityInfo.icon}  role={entityInfo.role}  roleNum={entityInfo.roleNum}");
                if (!hostile) continue;
                units.Add(new UnitEntry
                {
                    Name = child.name,
                    WorldPos = child.position,
                    IsAlive = isAlive,
                    Location = loc
                });
            }
        }

        var allRoots = GameObject.FindObjectsOfType<GameObject>();
        int nameMatchCount = 0;
        foreach (var obj in allRoots)
        {
            if (obj == null) continue;
            var n = obj.name;
            if ((n.StartsWith("Enemy") || n.Contains("Tgt_") || n.Contains("Target_"))
                && obj.transform != null)
            {
                var loc = obj.GetComponent<EntityLocation>();
                if (loc != null) { if (!IsHostile(loc, obj.transform)) continue; }
                if (!units.Exists(u => u.Location == loc))
                {
                    nameMatchCount++;
                    Log($"[Radar] NameMatch: {n}  hasEntityLocation={loc != null}  activeInHierarchy={obj.activeInHierarchy}");
                    units.Add(new UnitEntry
                    {
                        Name = n,
                        WorldPos = obj.transform.position,
                        IsAlive = loc != null ? IsUnitAlive(loc, obj) : obj.activeInHierarchy,
                        Location = loc
                    });
                }
            }
        }

        Log($"[Radar] Total FireMission entities: {units.Count - nameMatchCount}  NameMatch entities: {nameMatchCount}  Total: {units.Count}");

        var alive = units.Where(u => u.IsAlive).ToList();
        Log($"[Radar] Alive hostile count: {alive.Count}");
        for (int i = 0; i < Mathf.Min(alive.Count, 4); i++)
        {
            Vector2 km = GetEntityKmPos(alive[i]);
            Log($"[Radar] Marker {i + 1} -> {alive[i].Name} km=({km.x:F2},{km.y:F2})");
        }
        if (AutoPlaceMarkers)
        {
            for (int i = 1; i <= 4; i++)
            {
                if (i <= alive.Count)
                    fcs.MapTable.SetMarkerWorldPos(i, alive[i - 1].WorldPos);
                else
                    fcs.MapTable.ResetMarker(i);
            }
        }

        FlushLog();
    }

    public void OnGui()
    {
        if (!showRadar) return;

        if (radarRect.width < 10) radarRect = new Rect(Screen.width - 220, 10, 200, 140);

        var aliveUnits = units.Where(u => u.IsAlive).ToList();
        var deadUnits = units.Where(u => !u.IsAlive).ToList();

        float h = 20f;
        float lineH = h + 2f;
        int rowCount = aliveUnits.Count + (deadUnits.Count > 0 ? deadUnits.Count + 1 : 0);
        radarRect.height = 50f + Mathf.Min(rowCount, 14) * lineH;

        GUI.Box(radarRect, "");

        float x = radarRect.x + 8f;
        float w = radarRect.width - 16f;
        float y = radarRect.y + 4f;

        var oldColor = GUI.color;
        GUI.color = ClrTitle;
        GUI.Label(new Rect(x, y, w, h), $"Targets ({aliveUnits.Count} alive) [{(AutoPlaceMarkers ? "Auto" : "Manual")}]");
        GUI.color = oldColor;
        y += lineH + 2f;

        if (aliveUnits.Count == 0 && deadUnits.Count == 0)
        {
            GUI.color = ClrLabel;
            GUI.Label(new Rect(x, y, w, h), "  No targets found");
            GUI.color = oldColor;
            return;
        }

        int maxShow = Mathf.Min(aliveUnits.Count, 8);
        for (int i = 0; i < maxShow; i++)
        {
            GUI.color = ClrAlive;
            GUI.Label(new Rect(x, y, w, h), $"● {aliveUnits[i].Name}");
            GUI.color = oldColor;
            y += lineH;
        }
        if (aliveUnits.Count > 8)
        {
            GUI.color = ClrLabel;
            GUI.Label(new Rect(x, y, w, h), $"  ... +{aliveUnits.Count - 8} more alive");
            GUI.color = oldColor;
            y += lineH;
        }

        if (deadUnits.Count > 0)
        {
            y += 2f;
            GUI.color = ClrLabel;
            GUI.Label(new Rect(x, y, w, h), $"Destroyed ({deadUnits.Count}):");
            GUI.color = oldColor;
            y += lineH;

            int deadShow = Mathf.Min(deadUnits.Count, 3);
            for (int i = 0; i < deadShow; i++)
            {
                GUI.color = ClrDead;
                GUI.Label(new Rect(x, y, w, h), $"○ {deadUnits[i].Name}");
                GUI.color = oldColor;
                y += lineH;
            }
            if (deadUnits.Count > 3)
            {
                GUI.color = ClrLabel;
                GUI.Label(new Rect(x, y, w, h), $"  ... +{deadUnits.Count - 3} more");
                GUI.color = oldColor;
            }
        }
    }

    private static readonly List<string> _logLines = new();
    private static bool _logWritten;
    private static bool _onceLogged;
    private static Vector2 GetEntityKmPos(UnitEntry unit)
    {
        if (unit.Location != null)
        {
            try
            {
                var locProp = unit.Location.GetType().GetProperty("LocalPosition",
                    BindingFlags.Public | BindingFlags.Instance);
                if (locProp != null)
                {
                    var val = locProp.GetValue(unit.Location);
                    if (val is Vector2 v2) return v2;
                }
            }
            catch { }
        }
        return new Vector2(unit.WorldPos.x, unit.WorldPos.y);
    }

    private static void Log(string msg)
    {
        _logLines.Add($"[{System.DateTime.Now:HH:mm:ss}] {msg}");
    }

    private static void FlushLog()
    {
        if (_logLines.Count == 0) return;
        try
        {
            var dir = Path.Combine(MelonEnvironment.UserDataDirectory, "IronNestFCS");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "radar_log.txt");
            File.AppendAllLines(path, _logLines);
            if (!_logWritten)
            {
                Log($"[Radar] Log written to: {path}");
                _logWritten = true;
            }
        }
        catch { }
        _logLines.Clear();
    }

    private struct EntityBrief { public string icon; public string role; public int roleNum; }

    private static EntityBrief GetEntityInfo(EntityLocation loc)
    {
        var brief = new EntityBrief { icon = "?", role = "?", roleNum = -1 };
        try
        {
            var type = loc.GetType();
            var entityProp = type.GetProperty("Entity", BindingFlags.Public | BindingFlags.Instance);
            if (entityProp == null) return brief;
            var entity = entityProp.GetValue(loc);
            if (entity == null) return brief;
            var entType = entity.GetType();

            var iconProp = entType.GetProperty("Icon", BindingFlags.Public | BindingFlags.Instance);
            if (iconProp != null)
            {
                var val = iconProp.GetValue(entity);
                if (val is string s) brief.icon = s;
            }

            var roleProp = entType.GetProperty("Role", BindingFlags.Public | BindingFlags.Instance);
            if (roleProp != null)
            {
                var val = roleProp.GetValue(entity);
                if (val != null)
                {
                    brief.role = val.ToString();
                    if (val is int i) brief.roleNum = i;
                    else if (val is Enum e) brief.roleNum = Convert.ToInt32(e);
                }
            }
        }
        catch { }
        return brief;
    }

    private static bool IsUnitAlive(EntityLocation loc, GameObject go)
    {
        if (!go.activeInHierarchy) return false;

        try
        {
            var type = loc.GetType();

            var enabledProp = type.GetProperty("enabled", BindingFlags.Public | BindingFlags.Instance);
            if (enabledProp != null)
            {
                var enabledVal = enabledProp.GetValue(loc);
                if (enabledVal is bool b && !b) return false;
            }

            var entityProp = type.GetProperty("Entity", BindingFlags.Public | BindingFlags.Instance);
            if (entityProp != null)
            {
                var entity = entityProp.GetValue(loc);
                if (entity != null)
                {
                    if (!_onceLogged)
                    {
                        _onceLogged = true;
                        var entType = entity.GetType();
                        Log($"[Radar] MapEntity type: {entType.FullName}");
                        foreach (var f in entType.GetFields(BindingFlags.Public | BindingFlags.Instance))
                        {
                            try { Log($"[Radar] MapEntity field {f.Name} = {f.GetValue(entity)} ({f.FieldType.Name})"); }
                            catch { }
                        }
                        foreach (var p in entType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                        {
                            try { Log($"[Radar] MapEntity prop {p.Name} = {p.GetValue(entity)} ({p.PropertyType.Name})"); }
                            catch { }
                        }
                    }

                    var entType2 = entity.GetType();
                    foreach (var f in entType2.GetFields(BindingFlags.Public | BindingFlags.Instance))
                    {
                        var fn = f.Name.ToLower();
                        if (fn.Contains("alive") || fn.Contains("dead") || fn.Contains("destroyed") || fn.Contains("health") || fn.Contains("active"))
                        {
                            var val = f.GetValue(entity);
                            if (val is bool bVal) return fn.Contains("alive") || fn.Contains("active") ? bVal : !bVal;
                            if (val is float fVal) return fVal > 0;
                            if (val is int iVal) return iVal > 0;
                        }
                    }
                    foreach (var p in entType2.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        var pn = p.Name.ToLower();
                        if (pn.Contains("alive") || pn.Contains("dead") || pn.Contains("destroyed") || pn.Contains("active"))
                        {
                            var val = p.GetValue(entity);
                            if (val is bool bVal) return pn.Contains("alive") || pn.Contains("active") ? bVal : !bVal;
                        }
                    }
                }
            }
        }
        catch { }

        return go.activeSelf;
    }

    private static bool IsHostile(EntityLocation loc, Transform t)
    {
        var name = t.name;
        const int RoleAlly = 2;
        const int RoleEnemy = 1;
        const int RoleReference = 33554432;
        object? entity = null;

        try
        {
            var type = loc.GetType();

            var entityProp = type.GetProperty("Entity", BindingFlags.Public | BindingFlags.Instance);
            if (entityProp != null)
            {
                entity = entityProp.GetValue(loc);
                if (entity != null)
                {
                    var entType = entity.GetType();

                    // 第一优先：Icon + Role 判断（比字段遍历更可靠）
                    var iconProp = entType.GetProperty("Icon", BindingFlags.Public | BindingFlags.Instance);
                    var roleProp = entType.GetProperty("Role", BindingFlags.Public | BindingFlags.Instance);

                    string? icon = null;
                    int roleVal = -1;
                    if (iconProp != null)
                    {
                        var v = iconProp.GetValue(entity);
                        if (v is string s) icon = s;
                    }
                    if (roleProp != null)
                    {
                        var v = roleProp.GetValue(entity);
                        if (v is int i) roleVal = i;
                        else if (v is Enum e) roleVal = Convert.ToInt32(e);
                    }

                    if (roleVal >= 0)
                    {
                        bool hasAlly = (roleVal & RoleAlly) != 0;
                        bool hasEnemy = (roleVal & RoleEnemy) != 0;
                        bool isReference = (roleVal & RoleReference) != 0;

                        if (isReference) { Log($"[Radar] {name} -> neutral (Reference)"); return false; }
                        if (hasAlly && !hasEnemy) { Log($"[Radar] {name} -> friendly (Ally, roleNum={roleVal})"); return false; }
                        if (hasEnemy) { Log($"[Radar] {name} -> hostile (Enemy, roleNum={roleVal})"); return true; }
                    }

                    if (icon != null)
                    {
                        var iconLower = icon.ToLower();
                        if (iconLower.Contains("frendly") || iconLower.Contains("friendly")) { Log($"[Radar] {name} -> friendly ({icon})"); return false; }
                        if (iconLower.Contains("enemy")) { Log($"[Radar] {name} -> hostile ({icon})"); return true; }
                    }

                    // 第二优先：遍历字段找 team/side/faction/enemy/hostile
                    foreach (var f in entType.GetFields(BindingFlags.Public | BindingFlags.Instance))
                    {
                        var fn = f.Name.ToLower();
                        if (fn.Contains("team") || fn.Contains("side") || fn.Contains("faction"))
                        {
                            var val = f.GetValue(entity);
                            if (val is int iVal) { bool r = iVal != 0; Log($"[Radar] {name}.Entity.{f.Name}={iVal} -> hostile={r}"); return r; }
                        }
                        if (fn.Contains("enemy") || fn.Contains("hostile"))
                        {
                            var val = f.GetValue(entity);
                            if (val is bool bVal) { Log($"[Radar] {name}.Entity.{f.Name}={bVal} -> hostile={bVal}"); return bVal; }
                        }
                    }
                    foreach (var p in entType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        var pn = p.Name.ToLower();
                        if (pn.Contains("team") || pn.Contains("side") || pn.Contains("faction"))
                        {
                            var val = p.GetValue(entity);
                            if (val is int iVal) { bool r = iVal != 0; Log($"[Radar] {name}.Entity.{p.Name}={iVal} -> hostile={r}"); return r; }
                        }
                        if (pn.Contains("isenemy") || pn.Contains("hostile"))
                        {
                            var val = p.GetValue(entity);
                            if (val is bool bVal) { Log($"[Radar] {name}.Entity.{p.Name}={bVal} -> hostile={bVal}"); return bVal; }
                        }
                    }
                }
            }

            foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (p.Name.Contains("Team") || p.Name.Contains("team")
                    || p.Name.Contains("IsEnemy") || p.Name.Contains("isEnemy")
                    || p.Name.Contains("Hostile") || p.Name.Contains("hostile")
                    || p.Name.Contains("Side") || p.Name.Contains("side")
                    || p.Name.Contains("Faction") || p.Name.Contains("faction"))
                {
                    var val = p.GetValue(loc);
                    if (val is int iVal && iVal != 0) { Log($"[Radar] {name}.{p.Name}={iVal} -> hostile=true"); return true; }
                    if (val is bool bVal) { Log($"[Radar] {name}.{p.Name}={bVal} -> hostile={bVal}"); return bVal; }
                }
            }
        }
        catch (Exception ex) { MelonLogger.Warning($"[Radar] IsHostile err {name}: {ex.Message}"); }

        // Entity/Icon 都无结果时，用 DB 数据做名字二次判断
        var nameLower = name.ToLower();
        if (nameLower.Contains("police") || nameLower.Contains("prop")
            || nameLower.Contains("civ") || nameLower.Contains("smoke")
            || nameLower.Contains("reference") || nameLower.Contains("ref"))
        {
            Log($"[Radar] {name} -> neutral/civilian by name");
            return false;
        }
        if (nameLower.Contains("hospital") && !nameLower.Contains("ally"))
        {
            Log($"[Radar] {name} -> neutral hospital by name");
            return false;
        }
        if (nameLower.Contains("enemy") || nameLower.Contains("hostile")
            || nameLower.Contains("artillery") || nameLower.Contains("fdc")
            || nameLower.Contains("target"))
        {
            Log($"[Radar] {name} -> hostile by name");
            return true;
        }
        if (nameLower.Contains("friendly") || nameLower.Contains("ally"))
        {
            Log($"[Radar] {name} -> friendly by name");
            return false;
        }

        Log($"[Radar] {name} -> hostile (no match, assuming hostile)");
        return true;
    }
}

public static class FcsCalc
{
    private static readonly (float factor, float min, float max)[] Config =
    {
        (12.0f, 0, 5),
        (6.0f, 5, 10),
        (4.0f, 10, 15),
        (3.0f, 15, 20),
        (2.4f, 20, 25),
        (2.0f, 25, 30),
    };

    public static int Charge(float distance)
    {
        for (int i = 0; i < Config.Length; i++)
            if (distance > Config[i].min && distance <= Config[i].max)
                return i + 1;
        return distance > 30 ? 6 : 1;
    }

    public static float Elevation(float distance)
    {
        int chg = Charge(distance);
        float el = distance * Config[chg - 1].factor;
        return el > 60 || distance > 30 ? float.NaN : el;
    }
}
