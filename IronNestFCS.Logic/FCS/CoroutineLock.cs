using System.Collections;

namespace IronNestFCS.Logic.FCS;

/// <summary>
/// 协程级互斥锁。MelonCoroutines 全部在 Unity 主线程上协作式调度，没有真正并发，
/// 因此一个 bool 足以实现互斥——不需要任何并发原语（lock/Interlocked/SemaphoreSlim）。
///
/// 用法（务必配 try/finally，保证协程被 Stop / yield break 时也能释放）：
/// <code>
/// yield return deskLock.Acquire();
/// try { /* 临界区，可含 yield return */ }
/// finally { deskLock.Release(); }
/// </code>
/// 迭代器被 MelonCoroutines.Stop 停掉时会 Dispose，finally 块照常执行 → 锁不会泄漏。
/// </summary>
public sealed class CoroutineLock {
    private bool _held;

    /// <summary>等待直到拿到锁。拿到后立即占用，调用方负责在 finally 里 Release。</summary>
    public IEnumerator Acquire() {
        // 每帧重试一次。持锁方在主线程推进，释放后下一帧本协程即可抢到。
        while (_held) {
            yield return null;
        }
        _held = true;
    }

    public void Release() {
        _held = false;
    }

    /// <summary>重绑定（热重载）时强制复位，防止上一轮异常残留导致死锁。</summary>
    public void Reset() {
        _held = false;
    }
}
