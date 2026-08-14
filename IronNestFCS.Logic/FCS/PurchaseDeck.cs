using Il2Cpp;
using MelonLoader;
using UnityEngine;
using System.Collections;
using System.Linq;
using static System.Enum;

namespace IronNestFCS.Logic.FCS;

public class PurchaseDeck {
    private Transform? _powderCard;
    private Dictionary<BulletType, Transform> bulletCards = new();
    private DialInteractable? _purchaseLever; // 采购拉杆(RequisitionSlot -> Locking Lever -> Lever)
    
    public bool TryBind() {
        var requisitionConsole = GameObject.Find("Requisition Console").transform;
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
        // 采购执行控件:拉杆(游戏 8/14 更新后采购台改为"拖卡入槽 + 拉下拉杆",不再是 Universal Button 点击)。
        // 拉杆是 RequisitionSlot 下名为 Lever 的 DialInteractable。
        _purchaseLever = requisitionConsole.GetComponentsInChildren<DialInteractable>(true)
            .FirstOrDefault(d => d != null && d.gameObject != null && d.gameObject.name == "Lever");
        if (_purchaseLever == null) {
            MelonLogger.Error("[FCS] PurchaseDeck: Can't find purchase lever (DialInteractable)");
            return false;
        }
        return true;
    }
    
    private DialInteractable GetLeftRightDial() {
        var consoleBox = GameObject.Find("Console Box").transform;
        return  consoleBox.GetComponentInChildren<DialInteractable>();
    }

    /// <summary>拉下拉杆执行一次采购(拉下=1,复位=0;新卡入槽会自动顶替旧卡)。</summary>
    private IEnumerator PullLever() {
        _purchaseLever!.SetDialValue(1);
        yield return new WaitForSeconds(0.6f);
        _purchaseLever.SetDialValue(0);
        yield return new WaitForSeconds(0.4f);
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
        yield return PullLever();
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
        yield return new WaitForSeconds(0.5f);
        yield return PullLever();
        yield return new WaitForSeconds(2f);
    }
    
}
