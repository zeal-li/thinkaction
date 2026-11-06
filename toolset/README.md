# ToolSet

桌面版开发工具集（WPF）。

当前为**框架/布局阶段**：左侧模块导航、顶部分支选择、模块内容区、运行日志已搭好，具体功能尚未接入。

---

## 技术栈

| 项目 | 说明 |
| --- | --- |
| 语言 | C# |
| UI 框架 | WPF（`UseWPF`） |
| 目标框架 | `net8.0-windows` |
| 架构 | MVVM（手写 `RelayCommand` / `ViewModelBase`，**无任何 NuGet 依赖**） |
| 工程格式 | SDK 风格 csproj（源码通配自动收录） |

## 环境要求

- **Visual Studio 2022 17.8 及以上**（或 VS 2026）
  - 低于 17.8 的 VS 2022 不支持 .NET 8，需把 TFM 改为 `net6.0-windows`（见「目标框架调整」）
- .NET 8 SDK（安装 VS 时勾选「.NET 桌面开发」工作负载即可）
- `generate_vs.bat` 需要 `dotnet` 命令（即 .NET SDK）

---

## 目录结构

```
toolset\
├── generate_vs.bat                  # 生成 src\ToolSet.sln（双击运行）
├── build.sh                         # 编译 Debug 版 exe 到 build\（bash build.sh）
├── publish.bat                      # 发布自包含单文件 exe（双击运行）
├── gitpush.bat                      # 提交并推送
├── README.md
├── .gitignore                       # 忽略 bin/ obj/ publish/ .vs/ *.user ToolSet.sln
└── src\
    ├── ToolSet.sln                  # VS 解决方案（脚本生成，可随时删除重建）
    ├── ToolSet.csproj               # 工程定义（手写，项目本体）
    ├── App.xaml / App.xaml.cs       # 应用入口：资源 + 模块 DataTemplate 映射
    ├── MainWindow.xaml(.cs)         # 主窗口外壳
    ├── Infrastructure\              # 框架底座
    │   ├── ViewModelBase.cs         # INotifyPropertyChanged 基类
    │   ├── RelayCommand.cs          # ICommand 实现
    │   ├── OutputLog.cs             # 运行日志
    │   └── IShell.cs                # 宿主接口（写日志）
    ├── Styles\Theme.xaml            # 配色 + 全部控件模板
    ├── ViewModels\
    │   ├── MainViewModel.cs         # 模块列表、分支选择、打开目录
    │   ├── ModuleViewModelBase.cs   # 模块基类
    │   └── PlaceholderModuleViewModel.cs
    └── Views\
        └── PlaceholderModuleView.xaml(.cs)
```

编译产物（已忽略）：

```
src\bin\Debug\net8.0-windows\ToolSet.exe
src\obj\
.vs\
```

---

## 快速开始

1. 双击 `generate_vs.bat` 生成 `src\ToolSet.sln`
2. 双击 `src\ToolSet.sln` 用 VS 打开
3. `F5` 编译运行

命令行等价操作：

```bat
dotnet build src\ToolSet.sln -c Debug
:: 产物: src\bin\Debug\net8.0-windows\ToolSet.exe
```

---

## 发布（分发到其他电脑）

双击 `publish.bat`，产出 `publish\ToolSet.exe` —— **自包含单文件，约 70MB**，目标机不需要安装 .NET 运行时，Win10/11 x64 双击即可运行。

```bat
publish.bat              :: 默认 win-x64
publish.bat win-arm64    :: 打 ARM64 版本
```

脚本做的事：

```bat
rd /s /q publish                                    :: 清掉旧产物
dotnet publish src\ToolSet.csproj -c Release -r win-x64 ^
    --self-contained true ^                          :: 运行时打进包里
    -p:PublishSingleFile=true ^                      :: 单文件
    -p:IncludeNativeLibrariesForSelfExtract=true ^   :: 原生库一起打包
    -p:EnableCompressionInSingleFile=true ^          :: 压缩，150MB → 70MB
    -p:DebugType=None -p:DebugSymbols=false ^        :: 不输出 pdb
    -o publish
```

注意：`bin\` 下的产物是**框架依赖**版，只有 4 个文件（exe + dll + 2 个 json），拷过去必须配合目标机的 .NET 8+ Desktop Runtime 才能跑，不适合直接分发。

---

## 关于 generate_vs.bat

它**只生成 `ToolSet.sln`**，不生成也不修改 `ToolSet.csproj`。

```bat
dotnet new sln --force [--format sln] -n ToolSet -o src
dotnet sln src\ToolSet.sln add src\ToolSet.csproj
```

- `--format sln` 仅 .NET 10+ 的 SDK 需要（否则默认生成 `.slnx`），脚本会按 `dotnet --version` 的主版本自动决定
- 生成位置固定在 `src\`，与 csproj 同目录，因此 VS 解决方案资源管理器里没有多余的 `src` 文件夹层级
- `ToolSet.sln` 可以随时删除，重跑脚本即可恢复；`ToolSet.csproj` 丢失则脚本会直接报错退出

---

## 新增文件与新增模块

### 新增代码文件

直接把 `.cs` / `.xaml` 放到 `src\` 下任意目录即可，**自动纳入工程**，无需修改 csproj、也无需在 VS 里手动「添加现有项」。

原理：SDK 风格工程默认用 `**/*.cs`、`**/*.xaml` 通配收录，并自动排除 `bin` / `obj`。

### 新增功能模块（左侧导航项）

1. 继承 `ModuleViewModelBase` 写模块 ViewModel
2. 新建对应 `UserControl`（放在 `Views\`）
3. 在 `App.xaml` 注册 `<DataTemplate DataType="{x:Type vm:XXXViewModel}">`
4. 在 `MainViewModel.Modules` 里加入该模块实例

模块内需要写日志时，通过构造函数注入的 `IShell` 访问 `Log`。

---

## 目标框架调整

改 `src\ToolSet.csproj` 中的 `<TargetFramework>` 一行即可，代码无需改动：

| TFM | 最低 VS 版本 | 备注 |
| --- | --- | --- |
| `net6.0-windows` | VS 2022 17.0+ | 覆盖全部 VS 2022；.NET 6 已 EOL |
| **`net8.0-windows`** | **VS 2022 17.8+** | 当前配置，LTS，官方支持至 2026-11 |
| `net9.0-windows` | VS 2022 17.12+ | 已 EOL |
| `net10.0-windows` | VS 2026 | LTS，支持至 2028-11 |

其他相关属性：

```xml
<RollForward>LatestMajor</RollForward>          <!-- 允许在只装了更高版本运行时的机器上运行 -->
<CheckEolTargetFramework>false</CheckEolTargetFramework>  <!-- 屏蔽 TFM 停止支持的提示 -->
```

---

## 配置说明

- 程序集名 / exe 名：`ToolSet`（`<AssemblyName>`）
- 根命名空间：`ToolSet`（`<RootNamespace>`），子命名空间为 `ToolSet.Infrastructure`、`ToolSet.ViewModels`、`ToolSet.Views`
- 版本号：改 csproj 的 `<Version>` 即可
- 工具集路径 `MainViewModel.ToolSetPath` 目前**硬编码**为 `C:\data\ikun_01_cn\common\ToolSet`（「打开目录」按钮使用），后续计划改为读取配置文件（对应原 `scripts/config.ini`）

---

## 开发注意事项

- **日志的线程约束**：`OutputLog.Entries` 是 `ObservableCollection`，只能在 UI 线程修改。后续接异步命令执行时，回调里必须先 `Application.Current.Dispatcher.Invoke(...)` 再写日志，否则会抛跨线程异常。
- **不加兜底逻辑**：路径不存在、进程启动失败等情况按设计直接抛异常暴露，不做静默降级或包装提示。
- **样式复用**：`Styles\Theme.xaml` 中 `AppCheckBox`、`SectionTitleText` 目前无引用，是为后续模块的「复选框 + 分组标题」预留，新模块可直接 `{StaticResource ...}` 使用。
- **XAML 与命名空间**：改命名空间时必须同步修改 `.xaml` 里的 `x:Class` 与 `clr-namespace`，否则编译期报找不到类型。

---

## 后续待办

- [ ] 接入配置文件（`config.ini`）读取项目名、SSH、MySQL、导表路径等
- [ ] 「管理项目」模块
- [ ] 「辅助工具」模块（导出表 / 导出 Sproto / 表结构导出）
- [ ] 命令执行器：调用外部进程并把输出实时写入运行日志
- [ ] 应用图标（当前为 VS 默认空白图标）
