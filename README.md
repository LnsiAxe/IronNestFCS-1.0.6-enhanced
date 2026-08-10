# IronNestFCS Enhanced Edition (1.0.6.2)

> **基于 [svr2kos2/IronNestFCS](https://github.com/svr2kos2/IronNestFCS) v1.0.6 的增强修改版，支持正式版游戏**
>
> 增强功能移植自 [gxpppp/IronNestFCS](https://github.com/gxpppp/IronNestFCS)（v1.0.8 增强版，由 gxpppp 基于 1.0.5 开发，**仅支持 Demo 版**）：
> 战术雷达 / 自动扫荡 / 键盘快捷键 / 智能跳装填 / 炮管重置与超时自愈 / 新状态面板。
>
> 保留 v1.0.6 独有的能力：**20 种弹种枚举**、采购卡动态解析（字典式）、弹种按钮网格布局、炮塔字段私有封装。
>
> 原作者：[svr2kos2](https://github.com/svr2kos2) · 1.0.8 增强版作者：[gxpppp](https://github.com/gxpppp) · MIT License

## 📦 最新发布

- **v1.0.6.2**(2026-08-10):适配游戏 8/10 更新;修复铁巢紧急转移后打击失准;自动锁敌排除 Phantom Battery 与 EnemyKillTokens
- **下载与更新日志**:[GitHub Release v1.0.6.2](https://github.com/LnsiAxe/IronNestFCS-1.0.6-enhanced/releases/tag/v1.0.6.2)(详细改动说明见 Release 页面)

[Demo Video](https://www.bilibili.com/video/BV1xc7F6WEET/)

[Iron Nest: Heavy Turret Simulator](https://store.steampowered.com/app/4300500/) 的 [MelonLoader](https://melonwiki.xyz/) Mod，为游戏中的重型炮塔加入一套自动化**火控系统（Fire Control System, FCS）**：在地图上点选目标，Mod 会自动解算弹道、采购/装填炮弹、调整炮塔方向与仰角，并完成确认与击发的全套流程。

> 基于游戏正式版开发（IL2CPP + MelonLoader），兼容 Demo 版与正式版（正式版需 1.0.6 及以上代码基础，1.0.8 增强版本身仅支持 Demo）。

## 功能

- **战术雷达**：每 3 秒自动扫描战场，通过实体 Icon/Role 位掩码精准识别敌方阵营（基于 [Iron Nest DB](https://ironnestdb.com/enemies) 数据），自动排除平民、警察、友军、参照点等非敌对目标。
- **持续扫荡模式**：`Numpad 0` 切换持续扫荡——开启后新出现的敌对目标自动入队打击，已入队目标不会重复添加。面板显示 `[Sweep ON]` 状态。
- **双重标点模式**：`Numpad 5` 切换自动/手动标点——自动模式由雷达自动放 T1-T4 标记到地图上，手动模式保留玩家自拖。右上角雷达标题显示 `[Auto]` / `[Manual]`。
- **优先打击**：自动扫荡按优先级排序——炮兵 & FDC 指挥官（P4）→ 装甲目标（P3）→ 其他敌对（P2）。P3+ 目标自动插队到队列最前面。
- **智能跳过装填**：开火前检查炮管状态——弹药类型匹配且装药充足则跳过装弹/装药，直接调角度开火。
- **炮管强制重置**：`Numpad 7/8/9` 分别重置左炮/右炮/双炮——停止当前协程、释放所有锁、被中止任务自动放回队首重试。
- **一键打击**：点击地图右侧的目标按钮（T1~T4）或按 `Numpad 1-4`，自动为该目标入队一次完整的打击任务。
- **双炮管任务调度**：任务进入队列后由调度器自动派给空闲炮管，一管炮打完一发自动拉取下一个任务，两管炮并行作业。
- **自动弹道解算**：读取目标的方向角与距离，自动设定装药、弹种并解算所需仰角。
- **多弹种支持**：AP / APHE / ATMC / CLMN / CYAN / DRIL / EQKE / FLCH / HCHE / HE / INCN / LE / PLCM / PHGN / PRPG / SMK / STAR / TEAR / THRM / WP（共 20 种），可通过控制台旁的按钮选择当前弹种；弹仓缺弹时自动到采购台购买。
- **自动击发（可选）**：通过面板上的 `Auto Fire` 开关切换是否自动完成最后的击发动作。
- **最大装药（可选）**：通过 `Max Charge` 开关可强制使用 6 号装药，适用于追求极限射程的场景。
- **状态面板**：IMGUI 窗口实时显示两管炮的当前任务、目标参数、预估仰角/装药、队列等待数、扫荡开关状态。
- **热重载开发**：火控逻辑独立成可卸载的程序集，开发时改完代码按 **F9** 即可在不重启游戏的情况下重新加载。
- **自定义唱片机**（附带的独立 Mod）：扫描 `UserData/CustomRecords/` 下的音频文件（.mp3/.wav/.flac），用其内嵌封面与音频替换游戏内的 RecordDisk。

## 键盘快捷键

| 按键 (Numpad) | 笔记本替代 | 功能 |
| --- | --- | --- |
| `F9` | — | 热重载火控逻辑（开发用） |
| `Numpad 0` | `Ctrl+0` | 切换扫荡（面板 `[Sweep ON]`，强制自动标点） |
| `Numpad 1-4` | `Ctrl+1-4` | 击发 T1-T4 |
| `Numpad 5` | `Ctrl+5` | 切换自动/手动标点 |
| `Numpad +` | — | 一键拧开所有蒸汽阀门（最大） |
| `Numpad -` | — | 一键拧紧所有蒸汽阀门（关闭） |
| `Numpad 7` | `Ctrl+7` | 重置左炮 |
| `Numpad 8` | `Ctrl+8` | 重置右炮 |
| `Numpad 9` | `Ctrl+9` | 重置双炮 |

## 敌人阵营识别

雷达通过 `MapEntity.Icon` 和 `MapEntity.Role` 枚举位掩码做精准阵营判断：

| 阵营 | Role 标志位 | 行为 | 典型实体 |
| --- | --- | --- | --- |
| **Hostile** | Enemy (1) | 打击 | hostilebunker, hostileinfatry, hostiletank |
| **Target** | Target (32) + Enemy (1) | 打击 | target1-3 (FDC) |
| **Artillery** | Artillery (128) | 优先打击 (P4) | hostileartillery1-4 |
| **FDC** | Target + icon="Fire Direction" | 优先打击 (P4) | target1-3 |
| **Armored** | Fortification (65536) / Tank (262144) | 次级优先 (P3) | hostilebunker, hostiletank |
| **Ally** | Ally (2) | 不打 | allyhospital, allyinfantry, allytank, spotter |
| **Reference** | Reference (33554432) | 不打 | alpha, RefA, RefB |
| **Civilian/Neutral** | 名字含 police/prop/civ/smoke/hospital | 不打 | police, prop, civ, hospital |

> 数据来源：[Iron Nest DB — Enemies](https://ironnestdb.com/enemies)

## 架构

工程拆分为四个程序集，核心是为**热重载**服务的宿主 / 逻辑分离设计：

| 项目 | 角色 | 说明 |
| --- | --- | --- |
| `IronNestFCS` | **宿主 Mod** | 稳定加载、永不重载。负责首次加载 Logic、监听 F9 触发热重载、监控制台修改、转发生命周期回调。 |
| `IronNestFCS.Abstractions` | **契约** | 仅含 `IFcsModule` 接口。只加载一份，是唯一能安全跨 `AssemblyLoadContext` 边界传递的类型。 |
| `IronNestFCS.Logic` | **火控逻辑** | 所有高频改动的火控代码：弹道解算、任务调度、炮塔/炮管操控、UI、雷达扫描。被装进可回收的 ALC，按 F9 卸载并重载。 |
| `IronNestFCS.CustomRecords` | **独立 Mod** | 与火控无关的场景装饰，扫描自定义音频文件并替换游戏内唱片机的音轨与贴图。 |

### Logic 内部结构

```
IronNestFCS.Logic/
├── FcsModule.cs          # 程序集入口，组装 FSC + FcsWindow + TacticalRadar，实现 IFcsModule
├── FSC.cs                # 火控核心：任务调度、协程编排、炮塔预约、两把协程锁、炮管重置与重新入队
├── FcsWindow.cs          # IMGUI 状态面板（任务进度、队列、扫荡状态）
├── TacticalRadar.cs      # 战术雷达：精准敌我识别（Icon+Role位掩码+DB规则）、存活判定、双模标点
├── FcsSceneInteractor.cs # 场景交互：3D 按钮、点击注册、键盘快捷键分发、按键等待（带超时）
├── ClickRaycaster.cs     # 自定义射线点击检测（走新 Input System，不依赖游戏交互组件）
└── FCS/
    ├── ArtilleryTask.cs      # 任务数据类（目标ID/角度/距离/弹种/进度状态）
    ├── BallisticCalculator.cs # 弹道计算器硬件抽象：拨盘、计算按钮、里程表
    ├── GunSystem.cs          # 单管炮抽象：弹仓、装填、仰角杆、炮管状态检测（用于智能跳装填）
    ├── Turret.cs             # 炮塔硬件抽象：方向角旋控
    ├── TriggerConsole.cs     # 击发确认台抽象：五步确认 + 左右扳机 + spinner null 安全
    ├── PurchaseDeck.cs       # 采购台抽象：弹药/装药购买
    ├── MapTable.cs           # 地图桌抽象：目标标记读写、世界坐标映射
    └── CoroutineLock.cs      # 协程互斥锁（主线程协作式调度，无真正并发）
```

### 热重载关键设计

Logic 程序集从内存字节加载（不锁住磁盘 dll），装进 `isCollectible` 的 `AssemblyLoadContext`；重载时先 `Shutdown`（撤销 Harmony 补丁、停止协程、清空 IL2CPP 引用）再卸载旧 ALC，最后从磁盘重新加载新版本。

> 注意：按 F9 后 `autoSweep` 会重置为关，需再按 Numpad 0 开启。

详见 `LogicReloader.cs` 与 `FSC.cs` 中的注释。关键约束：
- 不要在 Logic 中注册新的 IL2CPP 类型（同一类型进程内只能注册一次）
- 所有对 IL2CPP 对象的引用在 Shutdown 时清空
- 每次实例用独立的 Harmony 实例；Shutdown 时 `UnpatchSelf`
- 协程必须登记到 `_runningCoroutines`（含所属炮管），卸载时 `MelonCoroutines.Stop` 全部停掉

### 任务调度与并发控制

火控任务流程走 Unity 协程（MelonCoroutines），全程在主线程协作式调度，无真正并发，因此用简单的 `CoroutineLock`（一个 `bool`）实现互斥：

- **`_turretLock`**：炮塔方向角锁，任务后台抢占后一直到击发完成才归还，全程与装填/升降角并行。
- **`_deskLock`**：控制台互斥锁，保护弹道计算器、确认开关台、采购台三组全局唯一的硬件串行使用。

防死锁规则：凡同时需要两把锁处，一律"先 turret 后 desk"，无环故不死锁。

### 任务鲁棒性

- **炮管状态感知**：任务开始前检查 `CanFire` + `BulletInChamber()` + `RemainingCharges()`，三项匹配则跳过装填直接调角开火。
- **炮管强制重置**：`AbortGun()` 停止指定炮管全部协程、释放所有锁、被中止任务自动 `EnqueueTaskFront` 放回队首重试。
- **按钮超时**：所有 `WaitAndClick` 增加 10 秒超时保护，超时打日志并放弃，不再永久死锁。

### 点击检测方案

不用 `MonoBehaviour.OnMouseDown`（需注册 IL2CPP 类型）、不用游戏自带的 `LookAtTarget`（外部硬挂不可靠）、不用 IMGUI 按钮（单 pass controlID 错位），而是自写 `ClickRaycaster`：每帧走新 Input System 读鼠标左键，从主相机射线命中已注册的 BoxCollider 后触发回调——纯逻辑、可热重载、不依赖游戏交互组件。

## 构建与安装

### 前置条件

- 已安装 **.NET 6 SDK**（见 [global.json](global.json)）。
- 游戏本体，并已为其安装 **MelonLoader**（IL2CPP）。

### 配置游戏路径

各 `.csproj` 通过 `GameDir` 属性定位游戏目录下的 MelonLoader 程序集。请把以下文件里的 `GameDir` 改成你本机的游戏安装路径：

- [IronNestFCS/IronNestFCS.csproj](IronNestFCS/IronNestFCS.csproj)
- [IronNestFCS.Logic/IronNestFCS.Logic.csproj](IronNestFCS.Logic/IronNestFCS.Logic.csproj)
- [IronNestFCS.CustomRecords/IronNestFCS.CustomRecords.csproj](IronNestFCS.CustomRecords/IronNestFCS.CustomRecords.csproj)

```xml
<GameDir>你的路径\IRON NEST Heavy Turret Simulator Demo</GameDir>
```

### 构建

```bash
dotnet build IronNestFCS.sln -c Release
```

各程序集的输出位置：

- **宿主 Mod**（`IronNestFCS.dll`）：放入游戏的 `Mods/` 目录，由 MelonLoader 自动加载。
- **火控逻辑**（`IronNestFCS.Logic.dll`）：输出到 `UserData/IronNestFCS/`（不放进 `Mods/`，由宿主在运行时反射加载）。
- **契约**（`IronNestFCS.Abstractions.dll`）：放入 `UserLibs/`，确保宿主与逻辑共用同一份接口。
- **自定义唱片机**（`IronNestFCS.CustomRecords.dll`）：放入 `Mods/`。将带内嵌封面的音频文件（.mp3/.wav/.flac）放入 `UserData/CustomRecords/` 即可。

> `IronNestFCS.Logic.csproj` 默认已把 `OutputPath` 指向 `$(GameDir)\UserData\IronNestFCS\`，构建即就位，改完代码进游戏按 F9 即可生效。

### 第三方依赖

| 依赖 | 用途 | 说明 |
| --- | --- | --- |
| [CSCore](https://github.com/filoe/cscore) | 音频解码 (mp3/wav/flac) | 仅 CustomRecords 使用，纯托管库 |
| [TagLibSharp](https://github.com/mono/taglib-sharp) | 音频标签/封面读取 | 仅 CustomRecords 使用，纯托管库 |

## 兼容性与安装要求

> ⚠️ **重要**：本增强版**仅支持正式版游戏（`Iron Nest Heavy Turret Simulator`）**，**不支持 Demo 版**。
> Demo 版的游戏程序集与正式版不同，本 Mod 的 dll 基于正式版编译，在 Demo 版上会加载失败（安装后无反应）。

1. **必须已安装 [MelonLoader](https://melonwiki.xyz/)**（IL2CPP / net6 版）。没装 MelonLoader 的话，dll 放进游戏目录不会被执行。
2. **首次启动**需联网让 MelonLoader 生成 `MelonLoader/Il2CppAssemblies/`（网络不通会卡住），生成成功后在游戏根目录可见该文件夹。
3. 按发布包结构放置（见上文"构建与安装"），三个位置缺一不可：`Mods/`、`UserData/IronNestFCS/`、`UserLibs/`。
4. 启动后先进主菜单属正常（面板显示 `Waiting for scene...`），**进入炮塔关卡场景后**会自动绑定；若未绑定按 **F9**。
5. 排查问题优先看 `MelonLoader/Latest.log`：日志里有 `IronNestFCS` 字样说明 Mod 已加载，报错内容一眼可见。

## 使用

1. 启动已安装 MelonLoader 与本 Mod 的游戏。
2. 进入包含炮塔与地图桌的关卡场景。若火控面板提示 `Waiting for scene...`，按 **F9** 在当前场景重新绑定。
3. 在控制台旁的按钮上选择弹种（默认 AP），并按需开启 `Auto Fire` 和 `Max Charge`。
4. 按 **Numpad 5** 切换标点模式：
   - **Auto**（默认）：雷达自动将 T1-T4 标记放到地图上
   - **Manual**：玩家自己手动拖动标记
5. 按 **Numpad 0** 开启持续扫荡——面板显示 `[Sweep ON]`，炮兵和 FDC 指挥官会被优先锁定。
6. 也可以手动操作：点击地图右侧的目标按钮（T1~T4）或按 `Numpad 1-4` 下达打击任务。
7. 左上角面板实时显示两管炮的任务进度、扫荡开关、队列等待数。
8. 右上角雷达显示存活/已摧毁目标及当前标点模式。
9. 炮管卡住时按 `Numpad 7/8/9` 强制重置——被中止的任务会放回队首重试。

### 开发热重载

修改 `IronNestFCS.Logic` 内的代码后，重新构建该项目（dll 会直接输出到游戏的 `UserData/IronNestFCS/`），切回游戏按 **F9** 即可加载新逻辑，无需重启游戏。

> 注意：F9 热重载后需要重新按 Numpad 0 开启扫荡、按 Numpad 5 切换标点模式。

## 常见问题

| 问题 | 原因 | 解决 |
| --- | --- | --- |
| 构建报 `MSB3270: MSIL/AMD64 不匹配` | 缺少 x64 配置 | 在 csproj 添加 `<PlatformTarget>x64</PlatformTarget>` |
| 装药/装弹卡住 | 协程时序或按钮状态异常 | 按 `Numpad 7/8/9` 重置对应炮管 |
| 笔记本按键无反应 | 无小键盘 | 用 `Ctrl+数字键` 替代（见快捷键表） |
| 构建刷 `NU1701` 警告 | CSCore 框架版本不匹配 | 在 `CustomRecords.csproj` 添加 `<NoWarn>NU1701</NoWarn>` |
| 炮管卡 `WaitLoading` | 推药杆握持太短未推入 | 已修复（1.5s 长握持），更新到最新版 |
| 启动报 `Win32Exception (193)` 且提示 `Cpp2IL.exe` | MelonLoader 首次生成 IL2CPP 程序集时下载 Cpp2IL 中断，留下残缺文件 | 从可用的同版本 MelonLoader 复制完整 `Cpp2IL.exe`（约 14.4MB）到 `MelonLoader\Dependencies\Il2CppAssemblyGenerator\Cpp2IL\`，或保证网络通畅后删除残件让 MelonLoader 重新下载 |
| 安装后无反应 / 面板不出现 | 见上方"兼容性与安装要求"（游戏版本、MelonLoader、首次生成、部署位置、是否进关卡） | 查看 `MelonLoader\Latest.log` 定位 |

## 贡献

欢迎提交 Issue 和 Pull Request。

- 发现 Bug、有功能建议或疑问，请[提交 Issue](../../issues)。
- 改进代码请[提交 Pull Request](../../pulls)。改动火控逻辑时请留意 `FSC.cs` 中关于热重载与协程的约定（不要在 Logic 中注册新的 IL2CPP 类型、协程必须登记以便卸载时停止、跨 ALC 只能传递 `IFcsModule`）。

## 许可

本项目基于 MIT 许可证开源。详见 [LICENSE](LICENSE)。

## 免责声明

本项目为非官方的第三方 Mod，与游戏开发商无关。仅供学习与单机娱乐使用，使用风险自负。
