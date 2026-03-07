# FxxK Dll 开发提示词

## 项目概述
**FxxK Dll** (最初叫 DDL Solver，因打字误差被命名) 是一个 WPF 桌面应用程序，通过 DeepSeek AI API 从文本中智能提取截止日期任务 (DDL)，并提供可视化日历管理。

### 核心功能
1. **AI 智能提取**: 从文本中提取任务、截止时间、重要性
2. **日历可视化**: 月视图 + 周视图展示任务安排
3. **任务管理**: 完成/删除/修改重要性/任务关系管理
4. **本地存储**: JSON 文件存储 (config.json, tasks.json)
5. **API 集成**: DeepSeek AI API 调用

## 技术栈和架构
- **语言**: C#, .NET 8.0 Windows
- **UI 框架**: WPF (Windows Presentation Foundation)
- **第三方库**: Newtonsoft.Json (v13.0.4)
- **架构模式**: MVVM (Model-View-ViewModel) 模式，包含服务层

### 目录结构
```
FxxK Dll/
├── Core/                    # 核心架构层
│   ├── Common/             # 基础类库
│   │   ├── ObservableObject.cs    # INotifyPropertyChanged基类
│   │   ├── RelayCommand.cs        # ICommand实现
│   │   ├── ServiceLocator.cs      # 简单服务定位器
│   │   └── ViewModelBase.cs       # ViewModel基类
│   ├── Interfaces/         # 服务接口定义
│   │   ├── ICalendarService.cs
│   │   ├── IConfigService.cs
│   │   ├── IDeepSeekService.cs
│   │   └── ITaskService.cs
│   ├── Navigation/         # 导航系统
│   │   ├── DefaultViewFactory.cs  # 视图工厂
│   │   └── NavigationService.cs   # 导航服务
│   └── ViewModels/         # 视图模型
│       ├── CalendarViewModel.cs
│       ├── InputViewModel.cs
│       ├── MainViewModel.cs
│       ├── SettingsViewModel.cs
│       ├── TasksViewModel.cs
│       └── WelcomeViewModel.cs
├── Models/                 # 数据模型层
│   ├── DDLTask.cs          # 核心任务实体
│   ├── ApiResponse.cs      # API请求响应模型
│   ├── AppConfig.cs        # 应用程序配置
│   └── CalendarEvent.cs    # 日历事件模型
├── Services/               # 业务逻辑层
│   ├── DeepSeekService.cs  # AI API集成（关键）
│   ├── TaskService.cs      # 任务CRUD管理
│   ├── CalendarService.cs  # 日历逻辑
│   ├── ConfigService.cs    # 配置管理
│   └── FileParserService.cs # 文件解析服务
├── Views/                  # 界面层
│   ├── WelcomePage.xaml/.cs  # 欢迎页面
│   ├── InputPage.xaml/.cs   # 输入页面
│   ├── TasksPage.xaml/.cs   # 任务管理页面
│   ├── CalendarPage.xaml/.cs # 日历视图页面
│   ├── SettingsPage.xaml/.cs # 设置页面
│   ├── AddTaskDialog.xaml/.cs  # 手动添加任务对话框（2026-02-26新增）
│   ├── TaskDetailDialog.xaml/.cs # 任务详情对话框
│   └── TaskRelationshipDialog.xaml/.cs # 任务关系管理对话框
├── Utils/                  # 工具类
│   └── WeekViewConverter.cs # 周视图转换器
├── Common/                 # 通用工具和转换器
│   └── InverseBoolConverter.cs # 布尔值反转转换器
├── MainWindow.xaml/.cs     # 主窗口
└── App.xaml/.cs            # 应用程序入口
```

---

## 核心数据模型：DDLTask

### 新数据结构（2026-02-18更新）
```csharp
public class DDLTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // === 新的三层数据结构 ===
    public string TaskName { get; set; } = "";           // AI提炼的任务名称（简短5-15字）
    public string TaskDetail { get; set; } = "";          // AI总结概括的任务详情（包含所有要点）
    public string OriginalText { get; set; } = "";         // 完整的原文内容

    // === 旧字段（标记为过时，保留向后兼容）===
    [Obsolete("请使用TaskName替代")]
    public string Description { get => TaskName; set => TaskName = value; }

    [Obsolete("请使用OriginalText替代")]
    public string SourceText { get; set; } = "";

    [Obsolete("请使用TaskDetail或OriginalText替代")]
    public string OriginalContext { get; set; } = "";

    // === 其他字段 ===
    public DateTime? Deadline { get; set; }
    public string DeadlineString { get; set; } = "";
    public string Importance { get; set; } = "中"; // 高/中/低
    public double Confidence { get; set; } = 0.8;
    public bool IsCompleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? CompletedAt { get; set; }
    public string CustomColor { get; set; } = ""; // 格式：#RRGGBB

    // === 任务关系字段 ===
    public string ParentTaskId { get; set; } = "";
    public List<string> SubTaskIds { get; set; } = new();
    public List<string> DependencyIds { get; set; } = new();
    public int TaskOrder { get; set; } = 0;
    public bool IsRootTask { get; set; } = true;
}
```

### 数据结构说明
**设计理念**：AI 提取的任务信息分为三层，满足不同使用场景
1. **TaskName（任务名称）**：简短精炼（5-15字），用于列表显示、日历视图等空间有限的场景
2. **TaskDetail（任务详情）**：完整总结，包含所有要点，用于详情展示、编辑等需要完整信息的场景
3. **OriginalText（原文内容）**：完整保留原始文本，便于后续查看和追溯

**向后兼容**：旧字段 `Description`、`SourceText`、`OriginalContext` 标记为 `Obsolete`，通过属性映射自动转换到新字段，确保旧数据正常读取

---

## 关键服务实现

### 1. DeepSeekService.cs - AI 集成核心

#### 系统提示词（2026-02-18更新）
```csharp
private string GetSystemPrompt()
{
    int currentYear = DateTime.Now.Year;
    int currentMonth = DateTime.Now.Month;
    int currentDay = DateTime.Now.Day;

    return $@"你是一个专业的DDL任务提取助手。从文本、聊天记录或文档中提取任务和截止日期。

当前日期：{currentYear}-{currentMonth:00}-{currentDay:00}

核心要求：
1. 提炼简洁的任务名称（5-15字）
2. 总结详细的任务描述（包含所有关键要点）
3. 保留完整的原文内容用于后续参考

规则：
- 使用{currentYear}年作为默认年份，除非明确指定其他年份
- 相对日期如'下周一'、'月底'、'3天后'、'2月19号'等，基于当前日期计算
- 重要度判断：3天内=高，7天内=中，其他=低
- 任务名称要简洁明了，突出核心内容
- 任务详情要完整包含所有要求和要点
- 原文内容要完整保留，便于后续查看

返回格式（每行一个任务）：
任务名称||截止时间(YYYY-MM-DD HH:MM)||重要度(高/中/低)||任务详情||原文内容

重要：分隔符必须是两个竖线 ||，不是一个竖线 |

示例：
提交期末作业||{currentYear}-12-15 23:59||高||需要完成数据结构课程的期末大作业，包含实验报告和源代码，提交到教学平台||老师：作业截止12月15日晚上12点前，记得上传实验报告和代码
准备英语四级考试||{currentYear}-12-28 23:59||中||复习英语四级考试内容，重点练习听力和阅读，每天至少2小时||期末考试安排通知，英语四级：12月28日

注意：
- 截止时间格式必须是 YYYY-MM-DD HH:MM，如 2025-02-19 23:59
- 如果没有明确时间，默认使用 23:59
- 如果没有明确日期，使用'未指定'
- 任务详情要详细，包含所有要点
- 原文内容保持完整，不要删减
- 分隔符必须是 ||（两个竖线）
- 只提取明确的任务，忽略模糊提及";
}
```

#### 响应解析器（2026-02-18更新）
```csharp
private List<DDLTask> ParseAIResponse(string aiResponse)
{
    var tasks = new List<DDLTask>();

    if (string.IsNullOrWhiteSpace(aiResponse))
        return tasks;

    // 按行分割
    var lines = aiResponse.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

    foreach (var line in lines)
    {
        var trimmedLine = line.Trim();

        // 跳过非任务行
        if (trimmedLine.StartsWith("格式：") || trimmedLine.StartsWith("示例：") ||
            trimmedLine.StartsWith("注意：") || trimmedLine.StartsWith("当前日期：") ||
            trimmedLine.StartsWith("核心要求：") || trimmedLine.StartsWith("规则：") ||
            trimmedLine.StartsWith("返回格式："))
            continue;

        // 尝试使用 || 分隔符
        string[] parts = null;
        if (trimmedLine.Contains("||"))
        {
            parts = trimmedLine.Split(new[] { "||" }, StringSplitOptions.None);
        }
        else if (trimmedLine.Contains('|'))
        {
            // 如果没有 ||，尝试使用单个 | 分隔符
            parts = trimmedLine.Split(new[] { '|' }, StringSplitOptions.None);
        }

        if (parts != null && parts.Length >= 3)
        {
            var task = new DDLTask
            {
                TaskName = parts[0].Trim(),
                DeadlineString = parts.Length > 1 ? parts[1].Trim() : "",
                Importance = parts.Length > 2 ? parts[2].Trim() : "中",
                TaskDetail = parts.Length > 3 ? parts[3].Trim() : parts[0].Trim(),
                OriginalText = parts.Length > 4 ? parts[4].Trim() : trimmedLine,
                SourceText = "AI提取", // 保留兼容性
                OriginalContext = parts.Length > 3 ? parts[3].Trim() : parts[0].Trim() // 保留兼容性
            };

            // 尝试解析截止时间
            task.ParseDeadline();

            tasks.Add(task);
        }
    }

    return tasks;
}
```

### 2. CalendarService.cs - 服务实例管理模式

#### 关键设计模式（2026-02-18更新）
```csharp
public class CalendarService : ICalendarService
{
    /// <summary>
    /// 获取某个月份的所有DDL事件
    /// 每次都创建新的 TaskService 以获取最新数据
    /// </summary>
    public List<CalendarEvent> GetEventsForMonth(int year, int month)
    {
        var events = new List<CalendarEvent>();
        // 关键：每次都创建新的 TaskService 以获取最新数据
        var taskService = new TaskService();
        var tasks = taskService.GetPendingTasks();

        foreach (var task in tasks)
        {
            if (task.Deadline.HasValue &&
                task.Deadline.Value.Year == year &&
                task.Deadline.Value.Month == month)
            {
                events.Add(CreateEventFromTask(task));
            }
        }

        return events.OrderBy(e => e.Date).ThenBy(e => e.StartTime).ToList();
    }

    // 其他方法同样遵循此模式...
}
```

**设计原因**：
- 避免 `TaskService` 实例缓存导致的数据不同步问题
- 确保每次操作都从文件读取最新数据
- 适用于 `GetEventsForWeek`、`GetEventsForToday`、`GetUpcomingEvents` 等所有方法

### 3. SettingsPage.xaml.cs - API 密钥配置刷新修复（2026-02-18）

#### 问题背景
保存新的 API 密钥后，测试连接功能仍使用旧密钥，需要退出重进设置页面才能正确测试

#### 解决方案
```csharp
private async void BtnTestApiKey_Click(object sender, RoutedEventArgs e)
{
    string apiKey = TxtApiKey.Text.Trim();

    if (string.IsNullOrWhiteSpace(apiKey))
    {
        MessageBox.Show("请先输入API密钥", "提示",
                      MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
    }

    // 先保存密钥
    try
    {
        _configService.UpdateApiKey(apiKey);
    }
    catch (ArgumentException ex)
    {
        MessageBox.Show(ex.Message, "密钥格式错误",
                      MessageBoxButton.OK, MessageBoxImage.Error);
        return;
    }

    // 关键修复：创建新的 DeepSeekService 实例以使用最新的 API 密钥
    _deepSeekService = new DeepSeekService();

    // 禁用按钮，显示测试中
    BtnTestApiKey.IsEnabled = false;
    BtnTestApiKey.Content = "测试中...";

    // 测试连接
    var result = await _deepSeekService.TestApiConnectionAsync();

    // 恢复按钮
    BtnTestApiKey.IsEnabled = true;
    BtnTestApiKey.Content = "测试连接";

    // 显示结果
    MessageBox.Show(result.Message,
                  result.Success ? "连接测试成功" : "连接测试失败",
                  MessageBoxButton.OK,
                  result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);

    UpdateStatusText();
}
```

**修复说明**：在保存 API 密钥后，创建新的 `DeepSeekService` 实例，确保测试连接使用最新配置

### 4. InputViewModel.cs - 用户反馈增强（2026-02-18）

#### 分析结果反馈
```csharp
private async void ExecuteAnalyze()
{
    await ExecuteWithBusyAsync(async () =>
    {
        IsAnalyzing = true;

        try
        {
            result = await _deepSeekService.ExtractDDLFromTextAsync(ChatText);

            if (!result.Success)
            {
                // 显示失败消息给用户
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    System.Windows.MessageBox.Show($"分析失败:\n{result.Message}",
                        "分析结果", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return;
            }

            if (result.Tasks == null || result.Tasks.Count == 0)
            {
                // 显示没有找到任务的消息
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    System.Windows.MessageBox.Show("分析完成，但未能提取到明确的DDL任务",
                        "分析结果", MessageBoxButton.OK, MessageBoxImage.Information);
                });
                return;
            }

            SaveTasksToDatabase(result.Tasks);

            // 显示成功消息
            System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                System.Windows.MessageBox.Show($"成功提取到 {result.Tasks.Count} 个任务!",
                    "分析成功", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }
        catch (Exception ex)
        {
            // 显示错误消息
            System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                System.Windows.MessageBox.Show($"分析过程发生错误:\n{ex.Message}",
                    "分析错误", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }
    });
}
```

---

## 我对各个模块的要求汇总

### 1. 日历模块（CalendarPage）

#### 周视图拖尾效果（已完成，v1.1.1 更新）
- **括号形状任务框**：任务所在日期是圆角矩形边框的右侧部分（类似右括号】），透明背景只显示边框
- **荧光拖尾条带**：
  - 拖尾始终从任务位置向左延伸到 X=0（第0列的左边界）
  - 渐变方向：左侧（X=0）透明 → 右侧（任务端）不透明
  - 带荧光效果（DropShadowEffect）
- **防重叠布局**：任务排序按重要性（高>中>低）> 日期先后，拖尾之间不能重叠
- **文字显示**：文字显示在任务所在列内（taskColumnX + 5），包含任务名称、提交时间、勾选框
- **交互统一**：
  - 拖尾和竖线作为视觉整体
  - 悬停时同步高亮变大（拖尾1.05倍，竖线1.1倍）
  - 缩放中心统一为任务端（右侧，RenderTransformOrigin = (1, 0.5)）
  - 鼠标悬停在竖线上时，拖尾也会同步高亮
  - 鼠标悬停在拖尾上时，竖线也会同步高亮
- **稳定切换**：7天/14天视图切换时拖尾位置稳定不跳动（列宽固定120px）
- **完整显示**：小窗口下所有日期必须能滚动查看

#### 月视图
- 42天网格显示，带任务点标记
- 点击日期查看当日任务详情
- 详情弹窗中任务按重要性分组显示

#### 数据刷新
- 使用 `CalendarService` 获取数据（每次创建新 `TaskService` 实例）
- 操作完成后调用 `LoadMonthView()` 或 `LoadWeekView()` 刷新

### 2. 任务管理模块（TasksPage）

#### 架构要求
- **严格遵循 MVVM 架构**：所有 UI 逻辑必须通过 ViewModel 处理
- **双向数据绑定**：任务完成状态、重要性选择使用双向绑定自动保存
- **命令绑定**：所有按钮操作绑定到 ViewModel 命令

#### 功能要求
- **筛选功能**：显示全部/待办/已完成任务，当前筛选按钮高亮显示
- **任务计数**：实时显示任务统计信息（总数、待办数、已完成数）
- **任务操作**：支持删除、编辑、查看详情、标记完成
- **删除同步**：删除任务后必须调用 `viewModel?.Refresh()` 刷新界面

#### 手动添加任务功能（2026-02-26新增）
- **独立对话框**：`AddTaskDialog.xaml/.cs`，模块化设计，可复用
- **FAB 浮动按钮**：右下角圆形蓝色按钮，白色加号图标，带黑色阴影增强立体感
- **按钮样式**（2026-02-26优化）：
  - 42x42px 圆形按钮（更小巧）
  - 蓝色背景 (#3498DB)，悬停变深 (#2980B9)，点击更深 (#21618C)
  - 增强阴影效果：外层黑色阴影 (Opacity 0.5, BlurRadius 12, ShadowDepth 6)
  - 内层边框：#1A5276，2px 粗边框增强层次感
  - 使用 `Grid.RowSpan="3"` 跨越所有行，确保在页面右下角
- **对话框设计**：参考 TaskRelationshipDialog 的美术风格
  - 白色卡片背景，圆角边框
  - 三个字段：任务名称、任务详情、原文内容（可选）
  - 截止时间：日期选择器 + 时间下拉框（30分钟间隔）
  - 重要性下拉框：高/中/低
  - 底部按钮：保存任务（绿色）/ 取消（灰色）
- **输入验证**：
  - 任务名称和详情为必填（带*标记）
  - 空值自动使用默认值："无标题" 或使用任务名称作为详情
  - 原文内容完全可选
- **事件驱动**：通过 `OnTaskCreated` 事件通知父页面刷新数据
- **调用方式**：
  ```csharp
  var addDialog = new AddTaskDialog { Owner = Window.GetWindow(this) };
  addDialog.OnTaskCreated += (task) => viewModel?.Refresh();
  addDialog.ShowDialog();
  ```

#### 颜色系统
- **仅保留重要性颜色**：高（红）、中（黄）、低（绿）
- **移除自定义颜色选项**：简化颜色系统，与日历界面保持一致

### 3. 输入模块（InputPage）

#### 文件处理
- **文本文件（.txt）**：读取内容显示在文本框，等待用户点击分析
- **文档文件（.pdf/.doc/.docx/.ppt/.pptx）**：直接调用 DeepSeek API 自动分析（当前仅支持文本）

#### AI 分析
- **用户反馈**：分析完成后通过 MessageBox 显示结果（成功/失败/无任务），包含分析耗时
- **耗时统计**：显示分析耗时（精确到小数点后1位），如 "⏱️ 分析耗时: 3.2秒"
- **错误处理**：完善的网络错误、API 错误、文件不存在错误处理
- **状态管理**：分析时显示加载指示器，禁用操作按钮

### 4. 设置模块（SettingsPage）

#### API 密钥配置
- **保存后立即生效**：保存密钥后测试连接功能应使用新密钥
- **实例刷新**：创建新的 `DeepSeekService` 实例确保使用最新配置
- **重启提示**（2026-02-26新增）：测试连接成功后显示 "⚠️ 请重启软件以确保新配置生效！" 提示

#### AI 模型配置（2026-02-26更新）
- **默认模型**：`deepseek-reasoner`（高性能推理模型）
- **模型位置**：`AppConfig.Model` 属性，默认值在 `AppConfig.cs` 中设置
- **测试连接**：测试接口同样使用 `deepseek-reasoner` 模型

### 5. 任务详情对话框（TaskDetailDialog）

#### 数据兼容性
```csharp
// 任务名称 - 优先使用新字段
TxtTaskName.Text = !string.IsNullOrWhiteSpace(_task.TaskName)
    ? _task.TaskName
    : (_task.Description ?? "无标题");

// 任务详情 - 优先使用新字段
TxtTaskDetail.Text = !string.IsNullOrWhiteSpace(_task.TaskDetail)
    ? _task.TaskDetail
    : (_task.OriginalContext ?? _task.Description ?? "无详情");

// 原文内容 - 优先使用新字段
string originalText = !string.IsNullOrWhiteSpace(_task.OriginalText)
    ? _task.OriginalText
    : _task.SourceText;
```

#### 功能按钮
- **编辑任务**：打开编辑对话框
- **管理关系**：打开任务关系管理对话框
- **删除任务**：确认后删除并关闭对话框

#### 事件触发
```csharp
public event Action<string> OnEditTask;
public event Action<string> OnManageRelations;
public event Action<string> OnDeleteTask;

// 触发事件
OnEditTask?.Invoke(_task.Id);
OnManageRelations?.Invoke(_task.Id);
OnDeleteTask?.Invoke(_task.Id);
```

---

## 开发规范和约定

### 代码风格
- **命名**: 类名`PascalCase`、方法名`PascalCase()`、变量名`camelCase`
- **私有字段**: 前缀`_`，如`_taskService`
- **注释**: 公共方法使用 XML 注释，复杂逻辑添加行内注释
- **异步**: API 调用使用`async/await`，方法名以`Async`结尾
- **模块化**: 对话框、窗口等复杂 UI 组件应提取为独立的 XAML 文件（而非内联代码创建）

### 模块化对话框设计规范（2026-02-26新增）
- **独立文件**: 对话框应使用独立的 `.xaml` 和 `.xaml.cs` 文件
- **事件驱动**: 使用事件通知父窗口操作结果，而非直接调用父窗口方法
- **可复用性**: 对话框可在多个页面中复用，降低代码重复
- **参考示例**: `AddTaskDialog`、`TaskDetailDialog`、`TaskRelationshipDialog`

**示例**：
```csharp
// 对话框定义事件
public event Action<DDLTask> OnTaskCreated;

// 父窗口调用
var dialog = new AddTaskDialog { Owner = Window.GetWindow(this) };
dialog.OnTaskCreated += (task) => viewModel?.Refresh();
dialog.ShowDialog();
```

### 错误处理规范
```csharp
try
{
    // 业务逻辑
}
catch (Exception ex)
{
    // 显示用户友好的错误信息
    MessageBox.Show($"操作失败: {ex.Message}", "错误",
        MessageBoxButton.OK, MessageBoxImage.Error);
}
```

**禁止使用**：
- ❌ `Console.WriteLine()` - 所有调试代码已清理
- ❌ `Debug.WriteLine()` - 生产代码不应包含调试输出

### 用户反馈规范
所有用户可见的操作必须提供反馈：
- ✅ 成功操作：显示成功消息（如"成功提取到 X 个任务"）
- ⚠️ 失败操作：显示失败原因（如"分析失败: API 密钥未配置"）
- ℹ️ 信息提示：显示必要信息（如"分析完成，但未提取到任务"）

---

## 当前项目状态

### 已完成功能
✅ AI 集成和文本提取（增强版：三层数据结构）
✅ 任务 CRUD 操作和本地存储
✅ 月视图和周视图日历（拖尾效果完整实现）
✅ 颜色管理和重要性系统（简化版）
✅ API 密钥配置管理（修复刷新问题 + 重启提示）
✅ MVVM 架构重构（TasksPage）
✅ 欢迎界面
✅ 任务详情对话框（支持删除）
✅ 数据结构升级（TaskName/TaskDetail/OriginalText）
✅ 用户反馈增强（MessageBox 提示 + 分析耗时显示）
✅ 服务实例管理模式（CalendarService）
✅ 调试代码清理（移除所有 Console.WriteLine）
✅ 开源准备（删除隐私数据、创建文档）
✅ 手动添加任务功能（模块化对话框：AddTaskDialog.xaml/.cs）
✅ AI 模型升级（deepseek-reasoner 高性能推理模型）
✅ FAB 按钮优化（42x42px，增强阴影效果，Grid.RowSpan 定位）
✅ 周视图拖尾效果优化（v1.1.1）：
   - 拖尾始终向左延伸到 X=0（第0列的左边界）
   - 渐变方向：左侧透明 → 右侧（任务端）不透明
   - 文字显示在任务所在列内（taskColumnX + 5）
   - 缩放中心统一为任务端（右侧）
   - 竖线和拖尾交互同步高亮

### 已知限制
1. **文件解析**：当前仅支持文本文件，PDF/Word/PPT 解析功能待实现
2. **任务提醒**：无自动提醒功能
3. **数据统计**：无任务完成率统计
4. **主题切换**：仅支持浅色主题

### 可能需要的改进/扩展
1. **任务提醒通知**: 添加截止时间提醒功能
2. **数据统计**: 任务完成率、时间分布等统计
3. **导入导出**: 支持 CSV、Excel 等格式
4. **主题切换**: 深色/浅色主题支持
5. **批量操作**: 批量删除、批量标记完成
6. **数据备份**: 自动备份和恢复功能
7. **云同步**: 多设备数据同步（可选）
8. **代码质量改进**: 解决 C# 可空性警告（CS8618, CS8625, CS8767等）
9. **单元测试**: 为关键服务（DeepSeekService, TaskService）添加单元测试

---

## 快速上手开发

### 如果要添加新功能，例如"任务提醒":
1. **扩展模型**: 在`AppConfig.cs`添加提醒设置属性
2. **配置服务**: 在`ConfigService.cs`添加提醒配置处理
3. **任务服务**: 在`TaskService.cs`添加提醒相关方法
4. **UI 界面**: 在`SettingsPage.xaml`添加提醒设置控件
5. **提醒逻辑**: 创建新的`ReminderService.cs`处理提醒逻辑
6. **集成测试**: 测试提醒功能与其他模块的集成

### 如果要优化 AI 提取精度:
1. **修改提示词**: 调整`DeepSeekService.GetSystemPrompt()`中的提示词
2. **改进解析**: 优化`ParseAIResponse()`方法处理更多格式
3. **添加验证**: 在提取后添加数据验证逻辑
4. **用户反馈**: 允许用户修正 AI 提取结果

### 如果要改进 UI 体验:
1. **数据绑定**: 使用 MVVM 模式改进数据绑定
2. **响应式设计**: 优化窗口大小调整时的布局
3. **动画效果**: 添加任务完成、删除等动画
4. **快捷键**: 添加常用操作的键盘快捷键

---

## 注意事项

1. **数据兼容性**: 新旧字段必须同时支持，使用 `??` 运算符提供回退
2. **服务实例管理**: CalendarService 等服务每次都创建新的 TaskService 实例
3. **API 密钥刷新**: 保存密钥后必须创建新的 DeepSeekService 实例
4. **用户反馈**: 所有操作必须提供用户可见的反馈（MessageBox）
5. **调试代码**: 不得使用 Console.WriteLine 或 Debug.WriteLine
6. **事件订阅**: TaskDetailDialog 的事件必须在所有调用位置订阅
7. **数据刷新**: 删除任务后必须调用 `viewModel?.Refresh()` 而非 `InitializePage()`

---

## 关于项目名字

- 这个项目一开始叫做 Fxxk DDL，但是第一次打字的时候打错了变成 DDL
- 后来为了听起来好点更名为 DDL Solver
- 后续改回 Fxxk DDL（仓库名 Fxxk-DDL）
- 这段历史记录在 README.md 中有说明

## GitHub 仓库信息

- **仓库地址**: https://github.com/Mr-IronBlood/Fxxk-DDL
- **仓库所有者**: Mr-IronBlood
- **远程仓库**: origin https://github.com/Mr-IronBlood/Fxxk-DDL.git
- **主分支**: main

### 推送命令
```bash
# 添加所有更改
git add .

# 提交更改
git commit -m "提交说明"

# 推送到 GitHub
git push origin main
```

---

**提示词使用说明**: 将此提示词提供给下一个 AI 助手，让其快速了解项目结构、关键文件、开发规范和可扩展方向。基于现有代码库继续开发新功能或优化现有功能。
