# RabbitWolfTiger

基于 Godot 4.7.2（Mono / C#）的游戏项目。

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
