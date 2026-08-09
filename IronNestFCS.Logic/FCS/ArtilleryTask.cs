using UnityEngine;

namespace IronNestFCS.Logic.FCS;

public enum Progress {
    Pending,
    Calculating,
    SelectingBullet,
    DumpingWrongShell,
    LoadingBullet,
    LoadingPowder,
    WaitLoading,
    Aiming,
    WaitingForFire,
    BackToIdle,
    Finished,
    Failed,
}

public class ArtilleryTask {
    public int targetId;
    public float angel;
    public float distance;
    public Vector3 position;
    public BulletType bulletType;
    public Progress progress;
    /// <summary>被 AbortGun 放回队首重试的次数，防止失败任务无限循环（每次重试都会重新采购/重新解算）。</summary>
    public int abortCount;
}