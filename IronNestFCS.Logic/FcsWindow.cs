using IronNestFCS.Logic.FCS;
using MelonLoader;
using UnityEngine;

namespace IronNestFCS.Logic;

public class FcsWindow
{
    private readonly FSC fcs;

    private bool showWindow = true;
    private Rect panelRect = new(20, 20, 290, 140);

    private static readonly Color ClrTitle = new(0.96f, 0.65f, 0.14f);
    private static readonly Color ClrLabel = new(0.72f, 0.65f, 0.55f);
    private static readonly Color ClrIdle = new(0.35f, 0.50f, 0.35f);
    private static readonly Color ClrActive = new(0.27f, 0.72f, 0.82f);
    private static readonly Color ClrWarning = new(0.96f, 0.65f, 0.14f);
    private static readonly Color ClrFailed = new(0.83f, 0.18f, 0.18f);
    private static readonly Color ClrGreen = new(0.18f, 0.62f, 0.35f);
    private static readonly Color ClrWhite = Color.white;
    private static readonly Color ClrDiv = new(0.33f, 0.22f, 0.14f);
    private static readonly Color ClrSweep = new(0.96f, 0.35f, 0.14f);

    public bool AutoSweepEnabled { get; set; }

    public FcsWindow(FSC fcs) => this.fcs = fcs;

    public void OnGui()
    {
        if (!showWindow) return;

        float h = 22f;
        float lineH = h + 2f;

        float extra = 0f;
        if (fcs.LeftTask != null) extra += lineH * 3;
        else extra += lineH;
        if (fcs.RightTask != null) extra += lineH * 3;
        else extra += lineH;
        extra += lineH * (fcs.PendingCount + 1);
        extra += 12f;

        panelRect.height = 140f + extra;

        GUI.Box(panelRect, "");

        float x = panelRect.x + 8f;
        float w = panelRect.width - 16f;
        float y = panelRect.y + 4f;

        var oldColor = GUI.color;
        GUI.color = ClrTitle;
        GUI.Label(new Rect(x, y, w, h), "IronNest FCS");
        GUI.color = oldColor;
        y += lineH;

        if (AutoSweepEnabled)
        {
            GUI.color = ClrSweep;
            GUI.Label(new Rect(x, y, w, h), "[Sweep ON]");
            GUI.color = oldColor;
            y += lineH;
        }

        DrawDivider(x, y, w);
        y += 4f;

        if (!fcs.IsBound)
        {
            GUI.Label(new Rect(x, y, w, h), "Waiting for scene...");
            y += lineH;
            GUI.Label(new Rect(x, y, w, h), "Press F9 to reload");
            return;
        }

        y = DrawGunRow("Left  ", fcs.LeftTask, x, y, w, h, lineH);
        DrawDivider(x, y, w);
        y += 4f;
        y = DrawGunRow("Right ", fcs.RightTask, x, y, w, h, lineH);
        DrawDivider(x, y, w);
        y += 4f;

        GUI.color = ClrLabel;
        GUI.Label(new Rect(x, y, w, h), $"Queue: {fcs.PendingCount}");
        GUI.color = oldColor;
        y += lineH;

        foreach (var item in fcs.QueueCan)
        {
            GUI.Label(new Rect(x, y, w, h),
                $"  T{item.targetId}  {ConvertPosition(item.position)}  {item.angel,5:F1}°/{item.distance,5:F2}km  {item.bulletType}");
            y += lineH;
        }
    }

    private float DrawGunRow(string label, ArtilleryTask? task, float x, float y, float w, float h, float lineH)
    {
        var oldColor = GUI.color;

        if (task == null)
        {
            GUI.color = ClrIdle;
            GUI.Label(new Rect(x, y, w, h), $"{label} Idle");
            GUI.color = oldColor;
            return y + lineH;
        }

        Color stateColor = task.progress switch
        {
            Progress.Failed => ClrFailed,
            Progress.Finished => ClrGreen,
            Progress.Pending => ClrLabel,
            _ => ClrActive
        };

        GUI.color = stateColor;
        GUI.Label(new Rect(x, y, w, h), $"{label} T{task.targetId}  {task.bulletType}  {task.progress}");
        GUI.color = oldColor;
        y += lineH;

        GUI.color = ClrLabel;
        GUI.Label(new Rect(x + 12f, y, w - 12f, h),
            $"Target: {task.angel:F1}° / {task.distance:F2}km");
        GUI.color = oldColor;
        y += lineH;

        float el = FcsCalc.Elevation(task.distance);
        int chg = FcsCalc.Charge(task.distance);
        GUI.color = ClrWarning;
        GUI.Label(new Rect(x + 12f, y, w - 12f, h),
            $"Fire: {el:F2}°  |  {chg}号药");
        GUI.color = oldColor;
        y += lineH;

        return y;
    }

    private static void DrawDivider(float x, float y, float w)
    {
        var oldColor = GUI.color;
        GUI.color = ClrDiv;
        GUI.Label(new Rect(x, y, w, 1f), "");
        GUI.color = oldColor;
    }

    public static string ConvertPosition(Vector3 position)
    {
        int leterIndex = (int)position.x;
        string zoneCol = leterIndex >= 0 && leterIndex < 26 ? ((char)('A' + leterIndex)).ToString() : "#";
        int zoneRow = (int)position.y + 1;
        int subCol = (int)(position.x * 10) % 10;
        int subRow = (int)(position.y * 10) % 10;
        return $"{zoneCol}{zoneRow} {subCol}:{subRow}";
    }
}
