# DDL Solver 开发文档

## 项目概述

DDL Solver 是一款基于 WPF 和 .NET 8.0 的桌面应用程序，使用 DeepSeek AI 从文本中自动提取任务和截止日期。

## 技术栈

- **框架**: .NET 8.0 / WPF
- **语言**: C# 12
- **AI 服务**: DeepSeek API
- **数据格式**: JSON
- **开发工具**: Visual Studio 2022
- **架构模式**: MVVM

## 项目结构

```
FxxK Dll/
├── Core/
│   ├── Common/              # 通用类 (ViewModelBase, RelayCommand, ServiceLocator)
│   ├── Interfaces/         # 接口定义 (ITaskService, IConfigService, IDeepSeekService, ICalendarService)
│   └── ViewModels/         # 视图模型 (InputViewModel, TasksViewModel, CalendarViewModel)
├── Models/                  # 数据模型 (DDLTask, CalendarEvent, AppConfig)
├── Services/               # 业务服务 (TaskService, ConfigService, DeepSeekService, CalendarService)
├── Views/                  # UI 界面 (MainWindow, CalendarPage, TasksPage, InputPage, SettingsPage)
└── Assets/                 # 资源文件
```

## 核心设计决策

### 1. 数据模型

#### DDLTask 数据结构
```csharp
public class DDLTask
{
    public string Id { get; set; }                    // 唯一标识符 (GUID)
    public string TaskName { get; set; }             // AI 提炼的任务名称（简短）
    public string TaskDetail { get; set; }           // AI 总结的任务详情（包含所有要点）
    public string OriginalText { get; set; }         // 完整的原文内容
    public DateTime? Deadline { get; set; }          // 解析后的截止时间
    public string DeadlineString { get; set; }       // 原始截止时间字符串
    public string Importance { get; set; }           // 重要度（高/中/低）
    public bool IsCompleted { get; set; }            // 完成状态
    public DateTime? CompletedAt { get; set; }       // 完成时间
    public DateTime CreatedAt { get; set; }          // 创建时间

    // 向后兼容字段
    [Obsolete("请使用 TaskName 替代")]
    public string Description { get; set; }

    [Obsolete("请使用 OriginalText 替代")]
    public string SourceText { get; set; }

    [Obsolete("请使用 TaskDetail 或 OriginalText 替代")]
    public string OriginalContext { get; set; }
}
```

### 2. AI 集成

#### DeepSeek API Prompt 设计
```csharp
系统提示词要求：
- 当前日期：自动获取当天日期
- 返回格式：任务名称||截止时间(YYYY-MM-DD HH:MM)||重要度||任务详情||原文
- 分隔符：必须使用 || (两个竖线)
- 截止时间：使用 YYYY-MM-DD HH:MM 格式，未指定时间默认 23:59
- 重要度判断：3天内=高，7天内=中，其他=低
```

#### AI 响应解析
- 支持两种分隔符：`||` (首选) 和 `|` (备用)
- 至少需要 3 个字段才能解析
- 自动解析截止时间字符串为 DateTime

### 3. 视觉设计

#### 周视图拖尾效果
```csharp
拖尾设计规范：
- 高度：90px
- 圆角半径：height / 4 * 0.6 = 13.5px
- 渐变：从任务结束点 (Alpha=255) 到今天结束点 (Alpha=0)
- ZIndex：正常 0，悬停时 1000-1002
- 缩放：悬停时放大 1.05 倍
  - 未来任务：从右侧锚点缩放
  - 过去任务：从左侧锚点缩放
```

#### 颜色方案
```
高重要度：#E74C3C (红色 RGB: 231, 76, 60)
中重要度：#F1C40F (黄色 RGB: 241, 196, 15)
低重要度：#2ECC71 (绿色 RGB: 46, 204, 113)
默认蓝色：#3498DB (蓝色 RGB: 52, 152, 219)
```

### 4. 数据同步策略

#### 服务实例管理
```csharp
// 问题：多个服务实例缓存导致数据不同步
// 解决方案：
CalendarService 每次获取任务时创建新的 TaskService 实例
ConfigService.GetConfig() 返回缓存值，提供 ReloadConfig() 手动刷新
TaskService.GetXxxTasks() 返回缓存值，提供 ReloadTasks() 手动刷新
```

#### 刷新机制
- 任务操作后立即刷新：调用 `viewModel?.Refresh()`
- 日历视图刷新：调用 `LoadWeekView()` 或 `LoadMonthView()`
- 任务详情关闭后刷新：通过事件订阅 `OnDeleteTask`、`OnEditTask`

### 5. 文件解析

#### 支持的文件格式
- **已实现**: `.txt` 文本文件
- **框架预留**: `.pdf`、`.doc`、`.docx`、`.ppt`、`.pptx`
- **依赖库**:
  - PDF: iText7
  - Word/PPT: NPOI 或 DocumentFormat.OpenXml

#### FileParserService 设计
```csharp
public (bool Success, string Text, string Message) ParseFile(string filePath)
{
    // 返回三元组
    // Success: 是否解析成功
    // Text: 解析后的文本内容
    // Message: 状态或错误消息
}
```

## 用户界面规范

### 输入页面
- 水印提示："请在此粘贴或输入聊天记录..."
- 最小字符数：10
- 分析前检查：API 密钥是否配置
- 分析状态显示：
  - `⏳ 正在分析文本...`
  - `❌ 分析失败: {原因}`
  - `⚠️ 分析完成，但未能提取到明确的DDL任务`
  - 成功提示 MessageBox

### 设置页面
- API 密钥验证：必须以 `sk-` 开头
- 密钥显示：脱敏显示 `sk-****...****`
- 连接测试：发送简单请求验证密钥有效性

### 日历视图
#### 月视图
- 点击日期显示详情对话框
- 按重要性分组显示（高→中→低）
- 每个任务卡片包含：任务名、截止时间、详情预览、操作按钮

#### 周视图
- 显示未来 7 天任务
- 拖尾效果：从任务截止日期延伸到今天
- 复选框：直接标记完成
- 悬停效果：任务放大并显示在最上层

### 任务详情对话框
- 显示内容：任务名称、任务详情、原文内容
- 任务关系：父任务、子任务、依赖任务
- 操作按钮：删除、管理关系、编辑、关闭

## 错误处理规范

### 异常处理原则
1. UI 层：使用 MessageBox 显示用户友好的错误消息
2. Service 层：返回结构化错误信息（Success/Message）
3. 所有异步操作使用 try-catch 包装
4. 不使用 Console.WriteLine 或 Debug.WriteLine（生产代码）

### 用户提示规范
```csharp
// 成功操作
MessageBox.Show("成功提取到 X 个任务!", "分析成功", ...)

// 警告
MessageBox.Show("分析完成，但未能提取到明确的DDL任务", "分析结果", ...)

// 错误
MessageBox.Show($"分析失败: {原因}", "分析结果", ...)
```

## 数据存储

### 文件位置
- 与可执行文件同级目录
- 不存储在用户文件夹，便于便携

### 配置文件 (config.json)
```json
{
  "DeepSeekApiKey": "",        // 用户配置的 API 密钥
  "Model": "deepseek-chat",     // AI 模型
  "MaxTokens": 2000,            // 最大 token 数
  "Temperature": 0.3,           // 温度参数
  "AutoSave": true,
  "Theme": "light",
  "NotificationsEnabled": true,
  "RemindBeforeDeadline": true,
  "RemindDaysBefore": 1,
  "RemindHoursBefore": 3
}
```

### 任务文件 (tasks.json)
```json
[
  {
    "Id": "guid",
    "TaskName": "任务名称",
    "TaskDetail": "任务详情",
    "OriginalText": "原文",
    "DeadlineString": "2025-02-19 23:59",
    "Importance": "高",
    "IsCompleted": false,
    "CreatedAt": "2025-02-18T10:30:00",
    "ParentTaskId": null,
    "SubTaskIds": [],
    "DependencyTaskIds": [],
    "CustomColor": null
  }
]
```

## 待实现功能

### 文件解析
- [ ] 安装 iText7 NuGet 包实现 PDF 解析
- [ ] 安装 NPOI 或 DocumentFormat.OpenXml 实现 Word/PPT 解析
- [ ] 取消注释 FileParserService 中的相关代码

### 其他功能
- [ ] 任务提醒功能
- [ ] 数据云同步
- [ ] 主题切换（深色/浅色）
- [ ] 任务导出（Excel、PDF）
- [ ] 快捷键支持

## 开发注意事项

1. **API 密钥**: 用户自己配置，项目不包含任何密钥
2. **调试代码**: 生产代码不包含 Console.WriteLine
3. **向后兼容**: 保留旧字段标记为 Obsolete
4. **服务实例**: CalendarService 每次创建新的 TaskService 实例
5. **事件订阅**: TaskDetailDialog 的三个事件必须都订阅（OnEditTask、OnManageRelations、OnDeleteTask）
6. **刷新同步**: 任务操作后调用 viewModel?.Refresh() 而不是 InitializePage()
