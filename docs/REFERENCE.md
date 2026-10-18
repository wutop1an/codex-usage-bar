# 完整使用与参数说明

本文件对应 **2.2.0**。数值是编译时默认值，不代表已经提供设置界面。

## 1. 界面中的每一项

| 元素 | 数据 / 计算方式 | 注意事项 |
| --- | --- | --- |
| 周期名 | 10080 分钟显示“周”；整天显示 `N天`；整小时显示 `Nh`；其他显示 `Nm` | 不假定每个账户都有 5 小时和每周两组限额 |
| `限额` | 固定界面标签 | 后面的百分比是剩余量 |
| 百分比 | `100 - usedPercent`，限制在 0–100%，最多一位小数 | 不表示 token 数或订阅费用 |
| 彩色条长度 | 总长度 × 剩余比例 | 左侧保留颜色，右侧消耗部分变灰 |
| 渐变 | 左红、中紫、右蓝，固定在整条轨道坐标中 | 不随剩余长度重新压缩颜色分布 |
| 回转箭头 | 重置时间提示图标 | 装饰性图标，不是拖动柄；点击整条可刷新 |
| 日期时间 | `resetsAt` Unix 秒转电脑本地时区，显示 `MM/dd HH:mm` | 缺少合法时间就显示“时间暂不可用” |
| 星号 `*` | 刷新失败，或数据超过 5 分钟未更新 | 同时把彩色条置灰；不是实时值 |
| “读取额度…” | 首次请求进行中且没有有效数据 | 成功后替换为实际额度 |
| “额度暂不可用” | 没有任何有效额度窗口 | 不把缺失数据当成 0% 使用或 100% 剩余 |
| 悬停说明 | 已用、剩余、重置时间和刷新操作提示 | 失败时注明这是上次数据 |

只显示 `codex` 限额桶；如果接口没有该桶，则回退到首个包含合法窗口的桶。
同一桶内按周期分钟数从小到大排列。不同桶不合并，不推算缺失的窗口。

## 2. 安装、启动、退出和卸载

源码目录需可写，以便生成 `bin/`。首次使用先在 Codex 中完成登录。
双击 `Start.cmd` 会在缺少正式 exe 时自动构建，随后启动后台程序。
如果修改过源码，请先执行 `Stop.cmd`，再运行 `build.ps1`，最后重新启动；
`start.ps1` 不会自动比较源文件的新旧。

程序通过当前 Windows 会话的互斥量避免重复运行。另一个版本仍在运行时，新实例直接退出。
与此前本地原型共用 `Local\CodexUsageBar.2` 和 `Local\CodexUsageBar.Stop.2`，避免同时嵌入两条。

退出有三种方式：`Stop.cmd`、用量条右键退出、系统托盘右键退出。
停止只关闭用量条及它自己启动的 app-server 进程，不关闭 Codex 主程序。
程序不会注册开机启动，也不会修改 Codex 安装包。

卸载时先退出，再删除项目目录；可按需删除 `%LOCALAPPDATA%\CodexUsageBar\` 本地诊断目录。
没有服务、计划任务或注册表安装项需要清理。

## 3. 文件目录逐项说明

| 文件 | 职责 |
| --- | --- |
| `src/Program.cs` | 入口、DPI 模式、单实例锁、停止信号、测试入口条件编译 |
| `src/AppSettings.cs` | 版本号及全部刷新、可见性和动画时间常量 |
| `src/BarApplication.cs` | 托盘、主计时器、Codex 窗口发现、位置布局、异步刷新和本地状态写入 |
| `src/UsageBar.cs` | 原生子控件、鼠标交互、tooltip、动画帧驱动和绘制调用 |
| `src/HostOverlayMonitor.cs` | 后台 UIA 事件与焦点祖先检查；输出允许显示与否 |
| `src/QuotaClient.cs` | 查找 Codex CLI，启动 stdio app-server，发送请求、处理超时、回收子进程 |
| `src/Quota.cs` | `UsageWindow`、`UsageBucket`、`Quota` 数据模型及兼容解析 |
| `src/Presentation.cs` | 布局尺寸、字体、文本缓存、轨道及图标绘制 |
| `src/Palette.cs` | 背景、文字、轨道颜色与非线性渐变曲线 |
| `src/UsageTransition.cs` | 额度变化时的数值插值；相同数据不重启动画 |
| `src/RevealTransition.cs` | 从透明到不透明的淡入插值 |
| `src/Native.cs` | Win32 函数声明和窗口样式常量 |
| `tests/Tests.cs` | 合成数据断言、离屏绘制预览、独立测试窗口验证 |
| `app.manifest` | 普通用户权限、Windows 兼容声明、PerMonitorV2 DPI |
| `assets/reset.svg` | Fluent 原始矢量图标 |
| `assets/reset.png` | 48×48 灰色图标，编译时嵌入 exe |
| `assets/LICENSE-Fluent.txt` | 图标的完整 MIT 声明 |
| `build.ps1` | 调用 .NET Framework 的 x64 C# 编译器 |
| `start.ps1` / `stop.ps1` | 隐藏窗口启动、发送停止信号 |
| `Start.cmd` / `Stop.cmd` | 双击入口，脚本失败时保留错误提示 |
| `test.ps1` | 单独构建测试 exe 并检查退出码 |
| `package.ps1` | 按明确清单打包正式 exe、源码和文档，输出校验值 |
| `.github/workflows/build.yml` | Windows CI 构建、测试、发布包 artifact |
| `.editorconfig` / `.clang-format` | 文本格式与可选代码格式化规则 |
| `.gitignore` | 排除构建产物、日志、本地状态等 |
| `README.md` / `README.en.md` | 中文主页与英文简介 |
| `docs/ARCHITECTURE.md` | 设计决策、线程和数据流程 |
| `docs/assets/preview.png` | 使用合成额度生成的展示图 |
| `CONTRIBUTING.md` | 贡献和验证要求 |
| `CHANGELOG.md` | 版本变化 |
| `LICENSE` / `THIRD_PARTY_NOTICES.md` | 项目与素材的授权说明 |

## 4. 构建参数与命令行

| 命令 | 结果 |
| --- | --- |
| `./build.ps1` | `bin/CodexUsageBar.exe`，不编译测试模块 |
| `./build.ps1 -TestBuild` | `bin/CodexUsageBar.Tests.exe`，定义 `TEST_BUILD` 并包含测试模块 |
| 正式 exe 无参数 | 启动后台窗口管理与额度刷新 |
| 正式 exe `--stop` | 通知同会话中的用量条退出，最多等待 5 秒 |
| 测试 exe `--self-test` | 运行断言，退出码 0 表示通过；报告写入 `self-test.txt` |
| 测试 exe `--render-samples` | 生成测试 exe 同目录的 `appearance-preview.png` |
| `./package.ps1` | 先构建与测试，输出 `dist/` 下 ZIP 和 `.sha256` 文件 |

正式 exe 不支持测试专用参数。构建使用 `/target:winexe`、`/platform:x64`、`/optimize+`、
`/warnaserror+`。依赖均来自系统 .NET Framework：WinForms、Drawing、Web.Extensions、Core，
以及 UIAutomationClient、UIAutomationTypes、WindowsBase。

## 5. 运行时间参数

定义位置：`src/AppSettings.cs`。

| 参数 | 默认值 | 作用 |
| --- | ---: | --- |
| `Version` | 2.2.0 | 本地诊断、RPC 客户端信息、发布包命名 |
| `LayoutIntervalMs` | 100 ms | 主界面检查停止信号、布局及可见性的间隔；不是每次都重绘 |
| `HostScanSeconds` | 3 s | 重新枚举符合条件的 Codex 顶层窗口 |
| `RefreshSeconds` | 120 s | 自动读取账户额度的间隔 |
| `RefreshDebounceSeconds` | 10 s | 两次读取的最短间隔，防止连点 |
| `StaleSeconds` | 300 s | 距上次成功读取超过此时长则标为旧数据 |
| `StatusIntervalSeconds` | 5 s | 写入本地诊断快照的间隔；刷新结束可立即写入 |
| `RequestTimeoutMs` | 25000 ms | 单次 stdio 请求超时 |
| `OverlayPollMs` | 350 ms | 可见性检查的轮询后备间隔；事件可提前唤醒 |
| `OverlayFreshnessSeconds` | 3 s | 过期的 clear 状态不再允许显示 |
| `FocusAncestorLimit` | 40 层 | 限制焦点祖先遍历的最大深度 |
| `FadeInMs` | 180 ms | 图片关闭后淡入；隐藏不做淡出 |
| `FrameIntervalMs` | 16 ms | 动画期间 WinForms 帧计时器间隔，不保证实际 60 FPS |
| `QuotaAnimationMs` | 420 ms | 额度发生变化时的插值时长 |

此外，对话框打开事件之后留有 250 ms 焦点稳定时间，避免旧焦点结果立即恢复显示。
后台窗口沿用最近的可见性状态；再次进入前台后重新检查焦点。
未知状态不会被当成“没有弹窗”。

## 6. 样式参数

所有位置以 96 DPI 下的逻辑像素（DIP）计，实际绘制乘以宿主窗口 DPI / 96。
目前改颜色、字号和布局需修改源码后重新构建，没有颜色选择器。

`src/Presentation.cs`：

| 参数 | 默认值 | 含义 |
| --- | ---: | --- |
| `CellWidth` | 354 | 单个周期所占总宽度 |
| `Gap` | 24 | 相邻周期组之间的空隙 |
| `Height` | 32 | 整个用量条高度 |
| `Top` | 2 | 距宿主客户区顶部的偏移 |
| `Right` | 170 | 距客户区右边缘预留的窗口按钮区域 |
| `TrackX` | 110 | 单组内轨道起点 |
| `TrackWidth` | 110 | 轨道总宽度 |
| `TrackHeight` | 10 | 轨道高度 |
| `LabelWidth` | 102 | 左侧标签可用宽度；标签实际字形右缘距轨道 8 DIP |
| `ResetX` | 256 | 时间文本起点 |
| `ResetWidth` | 98 | 时间文本可用宽度 |
| `FontFamilyName` | Segoe UI | Windows 系统字体；中文由系统回退 |
| `FontSize` | 14 | 正文字号，单位为 DIP |
| `LineHeight` | 20 | 对齐用行高 |

图标位于单组 `x=232, y=8`，大小 16×16 DIP。只在用量条左边缘大于 360 DIP 且客户区高度
大于 200 DIP 时显示，以避开左侧菜单和过小的窗口。

`src/Palette.cs`：

| 项目 | 色值 |
| --- | --- |
| 背景 | `#202020` |
| 文字与图标 | `#8F8F8F` |
| 空轨道 | `#414141` |
| 左端红色 | `#E75B63` |
| 中部紫色 | `#9E77D5` |
| 右端蓝色 | `#527FF0` |
| 旧数据填充色 | `#686868` |

渐变把位置 `u∈[0,1]` 映射为 `t = 0.5 + 4 × (u - 0.5)^3`，再按红、紫、蓝分段插值。
曲线在两端变化快，中间变化慢。消耗时保留轨道左段，故低剩余额度逐渐只留下红色。

## 7. 本地诊断字段

位置：`%LOCALAPPDATA%\CodexUsageBar\status.json`。文件在程序退出后保留，所以查看时先核对
`updatedUtc` 和进程是否仍在运行。该文件不被当成启动缓存，不会用旧文件伪造当前额度。

| 顶层字段 | 含义 |
| --- | --- |
| `version` | 程序版本 |
| `mode` | `native-child`，当前嵌入方式 |
| `appearance` | 当前渐变方向标识 |
| `font`, `fontSize`, `lineHeight` | 字体与逻辑尺寸 |
| `textAlignment` | `visible-ink-center`，按实际字形像素对齐 |
| `labelColor`, `labelBarGap` | 标签颜色和轨道间距 |
| `pid` | 用量条进程编号 |
| `busy` | 是否正在获取额度 |
| `stale` | 当前展示是否为旧数据 |
| `error` | 简短错误类别；不记录账户返回正文 |
| `updatedUtc` | 写入时间，旧 .NET JSON 日期格式 `/Date(Unix毫秒)/` |
| `windows` | 每个宿主窗口及其子控件的状态数组 |
| `windowsQuota` | 当前选择限额桶的额度窗口数组；无数据为 null |

`windows[]` 中的每个字段：

| 字段 | 含义 |
| --- | --- |
| `host`, `handle`, `parent` | 宿主、用量条、实际父窗口的 HWND 数值；正确嵌入时 host=parent |
| `isChild` | 是否设置 WS_CHILD |
| `isTopmost` | 是否为置顶窗口；应为 false |
| `visible` | Win32 可见标志，不代表没有被其他应用遮挡 |
| `dpi` | 子窗口 DPI，例如 144 表示 150% |
| `paintCount` | 绘制回调累计次数，不是 FPS |
| `overlayState` | clear / dialog / checking / unavailable / not-started |
| `overlayQueryMs` | 最近一次成功可见性检查耗时；不是从用户点击到画面更新的端到端延迟 |
| `overlayError` | 最近一次辅助功能查询异常类型；正常为空 |
| `childStyle`, `hostExStyle` | 子窗口样式和宿主扩展样式，十进制位掩码 |
| `x`, `y`, `width`, `height` | 在宿主客户区中的实际像素位置与尺寸 |

`windowsQuota[]`：`used` 为使用百分比，`remaining` 为剩余百分比，`reset` 为本地格式时间，
`minutes` 为额度窗口时长。这些字段包含个人用量信息，分享前可删除整个数组。

## 8. 故障处理

| 现象 | 建议检查 |
| --- | --- |
| 没有显示 | 先激活 Codex、放大窗口；查看 windows 是否为空及 overlayState |
| `windows=[]` | 当前安装方式可能不匹配进程名 / 安装路径；应用也可能尚未打开 |
| `checking` 或 `unavailable` | 焦点没有映射回宿主，或系统辅助功能信息不可用；查看 overlayError |
| 打开图片后遮挡 | 记录 Codex 版本及出现步骤；不同 Chromium / 对话框实现可能需要适配 |
| 一直读取失败 | 确认桌面自带 CLI 存在且已登录；点击刷新；检查本地网络和 error 类别 |
| 与官方数值不同 | 确认 CLI 与桌面账号一致、比较相同周期；注意两分钟刷新间隔 |
| 字体 / 颜色不合适 | 当前仅适配深色界面；检查 DPI。颜色 UI 尚未提供 |
| 改源码后未变化 | 退出旧实例，重新 build，再 start；启动脚本不会自动重编译已有 exe |
| 更新 Codex 后不显示 | 标题栏、进程路径或辅助功能树可能变化，请附版本与脱敏状态字段提 Issue |

不要为排错上传 `auth.json`、浏览器 Cookie、完整 app-server 输出或私人聊天截图。
