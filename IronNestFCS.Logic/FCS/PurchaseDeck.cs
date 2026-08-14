using Il2Cpp;
using MelonLoader;
using UnityEngine;
using System.Collections;
using static System.Enum;

namespace IronNestFCS.Logic.FCS;

public class PurchaseDeck {
    private Transform? _powderCard;
    private Dictionary<BulletType, Transform> bulletCards = new();
    private LookAtTarget? _buyButton;
    
    
    public bool TryBind() {
        var requisitionConsole = GameObject.Find("Requisition Console").transform;
        DumpConsoleTree(requisitionConsole, 0); // 临时诊断:定位"拉杆"控件
        var cards = requisitionConsole.GetComponentsInChildren<PunchcardRuntime>();
        foreach (var card in cards) {
            if (TryParse(
                    card.CurrentDefinition.ID.Replace("SMOKE", "SMK").Replace("Shell", ""),
                    out BulletType type
                )) {
                bulletCards[type] = card.transform;
            }
            else if (card.CurrentDefinition.ID == "PowderCharges") {
                _powderCard = card.transform;
            }
        }
        _buyButton = requisitionConsole.FindChild("Universal Button").GetComponent<LookAtTarget>();
        
        return true;
    }

    // ===== 临时诊断:打印采购台对象树与 HandleInteractable 方法,定位新版"拉杆"采购 =====
    private static void DumpConsoleTree(Transform root, int depth) {
        if (root == null || depth > 5) return;
        try {
            string comps = string.Join(",", root.GetComponents<Component>().Select(c => c != null ? c.GetType().Name : "?"));
            MelonLogger.Msg($"[FCS][Dump] {new string(' ', depth * 2)}{root.name}  <{comps}>");
            foreach (var c in root.GetComponents<Component>()) {
                if (c == null || c.GetType().Name != "HandleInteractable") continue;
                MelonLogger.Msg($"[FCS][Dump] >> HandleInteractable 在 {root.name}:");
                foreach (var m in c.GetType().GetMethods(
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                    MelonLogger.Msg($"[FCS][Dump]    Method: {m.Name}");
                foreach (var p in c.GetType().GetProperties(
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                    MelonLogger.Msg($"[FCS][Dump]    Prop: {p.Name}");
            }
        } catch (Exception ex) { MelonLogger.Error($"[FCS][Dump] err: {ex.Message}"); }
        for (int i = 0; i < root.childCount; i++) DumpConsoleTree(root.GetChild(i), depth + 1);
    }
    
    private DialInteractable GetLeftRightDial() {
        var consoleBox = GameObject.Find("Console Box").transform;
        return  consoleBox.GetComponentInChildren<DialInteractable>();
    }

    public IEnumerator BuyShell(BulletType type, LeftRight leftRight) {
        var card = bulletCards.GetValueOrDefault(type);
        if (card == null) {
            MelonLogger.Error($"[FCS] BuyShell: Can't find {type} card");
            yield break;
        }
        var target = new Vector3(6.4814f, -2.4675f, -22.0968f);
        card.position = target;
        card.GetComponent<DraggableItem>().MoveToSlot();
        yield return new WaitForSeconds(0.5f);
        
        switch (leftRight) {
            case LeftRight.Left:
                GetLeftRightDial().SetDialValue(0);
                break;
            case LeftRight.Right:
                GetLeftRightDial().SetDialValue(1);
                break;
        }
        yield return FcsSceneInteractor.WaitAndClick(_buyButton);
        yield return new WaitForSeconds(2f);
    }

    public IEnumerator BuyPowders() {
        if (_powderCard == null) {
            MelonLogger.Error("[FCS] BuyPowders: Can't find PowderCharges card");
            yield break;
        }
        _powderCard.position = new Vector3(6.4814f, -2.4675f, -22.0968f);
        _powderCard.GetComponent<DraggableItem>().MoveToSlot();
        // 药包为两炮共享,不需要拨盘选择炮管;新卡拖入槽位会自动顶替旧卡。
        // 与 BuyShell 一致：等卡牌入槽稳定后再执行采购,避免操作早于入槽导致本次采购无效。
        yield return new WaitForSeconds(0.5f);
        yield return FcsSceneInteractor.WaitAndClick(_buyButton);
        yield return new WaitForSeconds(2f);
    }
    
}