using System.Collections;
using Il2Cpp;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;

namespace IronNestFCS.Logic.FCS;


public enum BulletType {
    AP = 1,
    APHE = 2,
    ATMC = 3,
    CLMN = 4,
    CYAN = 5,
    DRIL = 6,
    EQKE = 7,
    FLCH = 8,
    HCHE = 9,
    HE = 10,
    INCN = 11,
    LE = 12,
    PLCM = 13,
    PHGN = 14,
    PRPG = 15,
    SMK = 16,
    STAR = 17,
    TEAR = 18,
    THRM = 19,
    WP = 20,
}

public class GunSystem {
    private string _surfix = "";

    private CylinderShellSelector? shellSelector;
    
    private List<string?> bullets = new();
    private LookAtTarget? nextBulletButton;
    private LookAtTarget? loadBulletButton;
    private List<LookAtTarget> powderButtons = new();
    private LookAtTarget? loadPowderButton;
    private GunController? gunController;
    private LinearSliderInteractable? elevationLever;
    private OdometerDisplay? remainingCharges;

    private TextMeshPro shellId;

    public bool TryBind(string surfix) {
        this._surfix = surfix;
        
        var gunSystem = GameObject.Find("Gun System " + surfix).transform;
        var reloadingConsole = gunSystem.Find("--Reloading Console");
        if (reloadingConsole == null) {
            MelonLogger.Error($"[FCS] GunSystem {surfix}: Can't find --Reloading Console");
            return false;
        }

        remainingCharges = reloadingConsole.GetComponentInChildren<OdometerDisplay>();
        
        nextBulletButton = 
            reloadingConsole.Find("Universal Button Move Cylinder")
                .GetComponent<LookAtTarget>();    
        shellSelector = gunSystem.GetComponentInChildren<CylinderShellSelector>();
        
        shellId = GameObject.Find("Shell ID " + surfix)
            .GetComponent<TextMeshPro>();
        var loadShell = reloadingConsole.FindChild("Universal Button Load shell Rammer");
        if (loadShell == null) {
            MelonLogger.Error($"[FCS] GunSystem {surfix}: Can't find Universal Button Load shell Rammer");
            return false;
        }
        loadBulletButton = loadShell.GetComponent<LookAtTarget>();

        var powderController = reloadingConsole.Find("PowderChargeController");
        for (var i = 0; i < powderController.childCount; ++i) {
            var child = powderController.GetChild(i);
            if (!child.name.StartsWith("Button Dispencer")) continue;
            var button = child.GetComponent<LookAtTarget>();
            if (button == null) {
                MelonLogger.Error($"[FCS] GunSystem {surfix}: Found {child.name} but lack of LookAtTarget Component");
                return false;
            }
            powderButtons.Add(button);
        }

        loadPowderButton = reloadingConsole.FindChild("Universal Button Charge Rammer (1)").GetComponent<LookAtTarget>();
        gunController = GameObject.Find("Gun"+surfix).GetComponent<GunController>();
        elevationLever = GameObject.Find(".Elevation Lever Baseplate")?.transform.FindChild(".Elevation Lever " + surfix)
            .GetComponent<LinearSliderInteractable>();
        return true;
    }
    
    public bool CanFire() {
        return gunController != null && gunController.CanFire;
    }

    public IEnumerator SetElevation(float elevation) {
        if (elevationLever == null || gunController == null) {
            MelonLogger.Error($"[FCS] GunSystem {_surfix}: Elevation lever or gun controller unbound");
            yield break;
        }
        elevationLever.SetSliderValue(elevation);
        yield return new WaitForSeconds(0.1f);
        // 无进展检测:慢速瞄准(仰角一直在动)可以等任意久;只有连续一段时间
        // 仰角几乎无变化(判定卡死)才放弃,避免无限等待与后续超时误杀。
        var lastElevation = gunController.CurrentElevation;
        var stagnantFor = 0f;
        const float stagnationThreshold = 10f; // 秒
        const float progressEpsilon = 0.2f;    // 度
        while (!Mathf.Approximately(gunController.CurrentElevation, elevation)) {
            elevationLever.SetSliderValue(elevation);
            yield return new WaitForSeconds(1f);
            var current = gunController.CurrentElevation;
            if (Mathf.Abs(current - lastElevation) < progressEpsilon) {
                stagnantFor += 1f;
                if (stagnantFor >= stagnationThreshold) {
                    MelonLogger.Error($"[FCS] GunSystem {_surfix}: 升仰角无进展 {stagnantFor:F0}s，" +
                                      $"当前 {current:F2}° 目标 {elevation:F2}°，放弃本次瞄准。");
                    yield break;
                }
            }
            else {
                stagnantFor = 0f;
            }
            lastElevation = current;
        }
    }
    
    public string? BulletInChamber() {
        return gunController?.ChamberedShellBlueprint?.shellDefinition?.ShellId;
    }
    
    public bool IsChamberEmpty() {
        return BulletInChamber() == null;
    }

    private void RefreshBullets() {
        bullets.Clear();
        if (shellSelector == null) return;
        foreach (var shell in shellSelector.bullets) {
            bullets.Add(shell?.GetComponent<ShellBlueprint>()?.shellDefinition?.ShellId);
        }
    }

    public void NextBullet() {
        if (nextBulletButton == null) return;
        nextBulletButton.OnClickDown();
    }
    
    /// <summary>
    /// 装填指定弹种：先把弹仓转到目标弹，再按装填。转弹仓每步之间要等 1 秒
    /// （游戏有转动动画/物理）。返回 IEnumerator，调用方用 yield return 等待它跑完。
    /// 必须走协程而非 async：continuation 要留在主线程才能安全访问 IL2CPP 对象。
    /// </summary>
    public IEnumerator LoadBullet(BulletType type) {
        RefreshBullets();
        var index = bullets.IndexOf(type.ToString());
        if (index == -1) {
            MelonLogger.Error($"[FCS] GunSystem {_surfix}: " +
                              $"No {type} available in cylinder, current bullets: {string.Join(", ", bullets)}");
            yield break;
        }
        
        for (var i = 0; i < bullets.Count; ++i) {
            if (bullets[0] == type.ToString()) {
                break;
            };
            NextBullet();
            yield return new WaitForSeconds(1.5f);
            RefreshBullets();
        }
        if (bullets[0] != type.ToString()) {
            MelonLogger.Error($"[FCS] GunSystem {_surfix}: Can't find {type} after rotation, " +
                              $"current: {string.Join(", ", bullets)}");
            yield break;
        }
        yield return FcsSceneInteractor.WaitAndClick(loadBulletButton!);
    }

    private IEnumerator SelectPowder(int count) {
        for (var i = 0; i < count; i++) {
            // 装药按钮引用可能因 reload 重建而失效，失效时重新扫描绑定
            if (i >= powderButtons.Count || powderButtons[i] == null || powderButtons[i].gameObject == null) {
                RefreshPowderButtons();
                if (i >= powderButtons.Count || powderButtons[i] == null) {
                    MelonLogger.Error($"[GunSystem] SelectPowder: button {i} invalid after refresh");
                    yield break;
                }
            }
            yield return FcsSceneInteractor.WaitAndClick(powderButtons[i]);
        }
    }

    /// <summary>重新扫描装药按钮（PowderChargeController 下的 Button Dispencer）。</summary>
    private void RefreshPowderButtons() {
        powderButtons.Clear();
        var gunSystem = GameObject.Find("Gun System " + _surfix)?.transform;
        var reloadingConsole = gunSystem?.Find("--Reloading Console");
        var powderController = reloadingConsole?.Find("PowderChargeController");
        if (powderController == null) return;
        for (var i = 0; i < powderController.childCount; ++i) {
            var child = powderController.GetChild(i);
            if (!child.name.StartsWith("Button Dispencer")) continue;
            var button = child.GetComponent<LookAtTarget>();
            if (button != null) powderButtons.Add(button);
        }
    }

    public IEnumerator LoadPowder(int count) {
        // 推药杆引用可能因 reload 重建而失效，重新绑定
        if (loadPowderButton == null || loadPowderButton.gameObject == null) {
            var gunSystem = GameObject.Find("Gun System " + _surfix)?.transform;
            var reloadingConsole = gunSystem?.Find("--Reloading Console");
            loadPowderButton = reloadingConsole?.FindChild("Universal Button Charge Rammer (1)")
                ?.GetComponent<LookAtTarget>();
            if (loadPowderButton == null) {
                MelonLogger.Error($"[GunSystem] LoadPowder: rammer button missing");
                yield break;
            }
        }
        yield return SelectPowder(count);
        yield return FcsSceneInteractor.WaitAndClick(loadPowderButton);
    }

    public bool HaveBulletInCylinder(BulletType type) {
        RefreshBullets();
        return bullets.Contains(type.ToString());
    }
    
    public bool HaveEmptyShellInCylinder() {
        RefreshBullets();
        return bullets.Contains(null);
    }

    public IEnumerator WaitBackToIdle() {
        while (gunController != null && gunController.elevationChangeVelocity != 0) {
            yield return new WaitForSeconds(0.1f);
        }
        yield return new WaitForSeconds(13);
    }

    public IEnumerator WaitFire() {
        while (gunController != null && !gunController.pendingReload) {
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    public int RemainingCharges() {
        return (int)remainingCharges.CurrentNumber;
    }

    /// <summary>炮管当前状态快照（用于智能跳过装填与面板显示）。</summary>
    public struct GunState {
        public string? ChamberedShell;
        public bool CanFire;
        public bool PendingReload;
        public float ElevationVelocity;
        public float CurrentElevation;
        public int ChargesRemaining;
        public string[] CylinderBullets;
    }

    public GunState GetState() {
        RefreshBullets();
        return new GunState {
            ChamberedShell = BulletInChamber(),
            CanFire = gunController != null && gunController.CanFire,
            PendingReload = gunController != null && gunController.pendingReload,
            ElevationVelocity = gunController != null ? gunController.elevationChangeVelocity : 0f,
            CurrentElevation = gunController != null ? gunController.CurrentElevation : 0f,
            ChargesRemaining = RemainingCharges(),
            CylinderBullets = bullets.Where(b => b != null).ToArray()!
        };
    }

}
