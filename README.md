# RabbitWolfTiger

基于 Godot 4.7.2（Mono / C#）的游戏项目。

## 嘴组件与脸色规则（本轮）

项目运行后直接进入八旅客表情展示首页。按 **F2** 或首页 Rules 按钮进入独立规则示例；示例中可换嘴、换脸、重置，F2 / Return 返回旅客展示首页。
示例使用用户提供图谱的独立脸、嘴、眼白、瞳孔切片，不执行渡船回合或死亡结算。
左右眼各有独立 Eye 根节点，眼白与褐色虹膜/黑瞳切片分层；此独立规则页不播放眼球追踪动画；旅客展示首页支持独立视线和眨眼。

- `entities/diet/` 为不依赖 Godot 的纯 C# 规则：脸与嘴分别替换；食谱随嘴走。
- `MouthComponent`：None/Small/Medium/Large 严格对应 0/1/2/3 个不同的合法脸色；重复、未知颜色、错误数量会拒绝。
- `DietBody.CanEat` 只判断当前嘴食谱是否包含目标当前脸色，不按物种、体型或 RGB 近似比较；不能吃自己。
- `resources/diet/initial_diets.json` 是可调初始装配表，按物种创建角色后不再以物种决定摄食。每个人初始共用 Skin 色；兔 Cream、狼 Gray、萝卜 Orange、虎 GoldenOrange。
- 初始示例食谱：兔小嘴吃 Orange；狼中嘴吃 Cream/Skin；虎大嘴吃 Gray/Cream/Skin；萝卜无嘴。**人类暂用 None（待定配置，并非最终食性决定）**。
- 难民、刺客、国王、护卫都使用同一个 Human 初始装配模板（Skin + 暂定 None）；本轮未实现身份特殊能力。
- `CanEat` 是摄食资格，不会直接造成死亡。调用方仍须保证船上安全、受看管岸安全，以及到岸先结算无人看管阵容、播放事件动画再下船的顺序。本轮没有实现这些回合系统。
- JSON 被打包导出时，请在 Godot 导出预设的非资源文件过滤器中包含 `resources/diet/*.json`。

### 规则测试

纯规则测试不需要 Godot 或第三方测试包：

```sh
dotnet run --project tests/diet/DietRules.Tests.csproj
```

Godot 集成检查仍按先构建再导入的顺序（使用 4.7.2 Mono）：

```sh
dotnet build RabbitWolfTiger.csproj
godot --headless --path . --import
godot --headless --path . res://tests/diet/diet_rules_headless.tscn
godot --headless --path . res://tests/diet/diet_demo_integration.tscn
godot --headless --path . --quit-after 3
```

新增 C# 脚本的 `.uid` 应由匹配版本 Godot 导入生成并提交，不手写 UID。
集成测试触发实际按钮，覆盖换嘴、换脸、重置、分层素材、食谱文本与最坏组合下的控件布局。

## 环境

- 引擎：Godot 4.7.2 stable mono，exe 与 `GodotSharp/` 放在仓库根目录，均已 gitignore，不入库
- .NET：TargetFramework `net8.0`，LangVersion 12
- 显示：1280x720，stretch `canvas_items` / `expand`；物理固定 60 TPS
- 输入：`move_left/right/up/down`（WASD + 方向键，physical_keycode 布局无关）

## 目录结构（按 feature 组织，不按文件类型）

```
autoloads/   全局单例。GameEvents 为全局信号总线：仅生命周期事件，上限 15 个
common/      跨 feature 通用工具
entities/    实体。每个实体一个子文件夹：场景 + 脚本 + 资源 + 测试
levels/      关卡与主场景（levels/main/）
resources/   数据资源（.tres 与数据类脚本）
tests/       headless 冒烟 / 回归测试
ui/          UI 场景
```

## 常用命令

```powershell
# 编译 C#（新增/修改脚本后、headless import 前必须先跑）
dotnet build RabbitWolfTiger.csproj

# 重导入资源（新增任何资源/脚本后必须执行，否则运行时 GD.Load 为 null）
.\Godot_v4.7.2-stable_mono_win64_console.exe --headless --path . --import

# headless 冒烟（跑 3 帧退出，验证主场景与 autoload 可加载）
.\Godot_v4.7.2-stable_mono_win64_console.exe --headless --path . --quit-after 3

# 打开编辑器 / 运行游戏
.\Godot_v4.7.2-stable_mono_win64.exe -e --path .
.\Godot_v4.7.2-stable_mono_win64.exe --path .
```

## 约定

- 节点名 PascalCase；文件与文件夹 snake_case（C# 类文件为唯一例外：PascalCase，且类名必须与文件名严格一致，区分大小写）
- 信号上行（signal up）、调用下行（call down）；跨场景/跨系统事件走 `GameEvents`，feature 内部用自己的局部信号，禁止兄弟节点硬引用
- UI 不直接修改数据，只监听信号做投影
- 引用节点用 `%SceneUniqueName` 或 `@export`，禁止绝对路径 `get_node`
- 新增 C# 脚本或资源后的提交顺序：`dotnet build` → `--import` → 把生成的 `.uid` / `.import` 元数据一并提交进库


## 分层人物/动物表情初版

**默认运行首页**为八角色画廊（兔、狼、虎、萝卜、难民、刺客、国王、护卫），不再默认进入旧移动方块场景。五种表情、独立瞳孔跟随与眨眼、暂停/继续，支持全员或选择单个角色切表情。换脸、换嘴、换头饰不重置表情/视线；每卡始终显示真实玩法嘴大小与可食颜色。

人类共用Skin +暂定None，无身份能力。None仍可显示装饰表情嘴，但不能捕食。表情和食性彻底分离；旧F2示例与原规则保持不变。技术选择、素材来源、分层限制与接口见 [素材接入说明](resources/portrait/art/README.md)。

### 首页与导航

- `project.godot` 默认场景为 `levels/portrait_gallery/portrait_gallery.tscn`。运行项目立即显示八旅客和表情控制。
- 首页 Rules / F2 → 食性规则页；规则页 F2 / Return → 旅客首页。
- 首页 Legacy movement / F4 → 明确标注的旧移动实验场；该页 Home / F3 → 旅客首页。旧场景仅保留用于移动测试。
- F3 在旅客首页不再跳回方块场景。
- 本次仅修正入口/导航；人物食谱、动画、美术均未改动。截图验收须运行真实图形渲染，headless逻辑通过不能替代截图。

### 参考忠实度返工

默认画廊现采用头像模式，动物没有统一布袍；八头像从中性正视开始。黑瞳、白口鼻垫、中性嘴与虎颊纹为明确新增的代码原生组件，原PNG未修改；每种角色有独立配准参数，保留独立视线与眨眼。Small/Medium/Large嘴在中性状态仍按10/14/18宽分级（张口表情为13.5/21.6/29.7），食性数据不变，人类食谱仍临时。

首页 Reference QA / F5 打开同脸宽、中性正视暂停的实际Godot对照场景；F5返回首页。左侧合成参考切片只用于验收，右侧始终使用可换件组件。运行该场景并传 --reference-capture=/absolute/reference.png 可在真实GL环境保存截图，禁止headless伪截图。

## 默认简体中文

旅客、表情、按钮、食性颜色/嘴等级、原图对照、旧版移动页均默认使用简体中文。显示文案集中在 `ui/localization/UiText.cs`，内部enum、角色标识、节点名、日志marker和玩法不翻译。应用显示名为“兔狼虎”，C#程序集仍为RabbitWolfTiger。

中文字体随项目打包，使用从官方Noto Sans CJK SC派生并改名的 `RwtSansSC-Regular.otf` 界面子集，不依赖系统中文字库；版权许可、固定来源、字形集合、可复现重建脚本见 `resources/fonts/`。新增文案后重建子集并运行中文界面字形/布局检查，防止换件后的动态文字漏字。
