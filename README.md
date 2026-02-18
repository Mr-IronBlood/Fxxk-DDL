# DDL Solver

一款基于 AI 的智能任务管理工具，自动从聊天记录和文本中提取截止日期（DDL），帮助用户轻松管理各类任务和作业。

## 功能特性

- **AI 智能提取**：使用 DeepSeek AI 自动分析文本，识别任务和截止日期
- **多视图管理**：提供月视图和周视图，直观展示任务时间安排
- **任务分类**：按重要性（高/中/低）分类，使用颜色标识
- **任务关系**：支持父子任务、依赖关系管理
- **数据本地存储**：所有数据保存在本地，保护隐私

## 截图

> 此处可以添加软件截图

## 安装方法

### 方式一：下载发布版本

1. 前往 [Releases](https://github.com/Mr-IronBlood/Fxxk-DDL/releases) 页面
2. 下载最新版本的安装包
3. 解压并运行 `Fxxk DDL.exe`

### 方式二：从源码编译

```bash
# 克隆仓库
git clone https://github.com/Mr-IronBlood/Fxxk-DDL.git
cd "Fxxk DDL"

# 使用 Visual Studio 打开 Fxxk Dll.sln
# 或使用命令行编译
dotnet build
dotnet run
```

## 使用指南

### 首次使用

1. 打开软件后，进入"设置"页面
2. 配置你的 DeepSeek API 密钥（需要自己在 DeepSeek 官网申请）
3. 返回"输入"页面开始使用

### 提取任务

1. 将聊天记录或任务文本粘贴到输入框
2. 点击"分析"按钮
3. AI 会自动识别任务并添加到列表

### 管理任务

- **月视图**：查看某个月的所有任务，点击日期查看详情
- **周视图**：查看未来 7 天的任务安排，带拖尾效果直观显示
- **任务管理**：查看所有任务，支持编辑、删除、标记完成

## 配置说明

### DeepSeek API 配置

本软件使用 DeepSeek AI 进行任务提取，需要配置 API 密钥：

1. 访问 [DeepSeek 官网](https://platform.deepseek.com/)
2. 注册账号并获取 API Key
3. 在软件设置页面填入 API Key

> 注意：API 密钥保存在本地，不会上传到任何服务器

### 数据存储

- 配置文件：`config.json`
- 任务数据：`tasks.json`
- 文件位置：与可执行文件同级目录

## 技术栈

- **框架**：.NET 8.0 / WPF
- **AI 服务**：DeepSeek API
- **数据格式**：JSON
- **开发工具**：Visual Studio 2022

## 项目结构

```
DDL Solver/
├── FxxkDDL.Core/          # 核心接口和模型
├── FxxkDDL.Services/      # 业务逻辑服务
├── FxxkDDL.Models/        # 数据模型
├── FxxkDDL.Views/         # UI 界面
└── FxxkDDL.ViewModels/    # 视图模型
```

## 开发计划

- [ ] 支持 PDF 文件解析
- [ ] 支持 Word 文件解析
- [ ] 支持 PPT 文件解析
- [ ] 添加任务提醒功能
- [ ] 支持数据云同步
- [ ] 添加主题切换功能

## 常见问题

**Q: 软件免费吗？**

A: 本软件完全开源免费，但使用 DeepSeek API 需要自行申请密钥并支付相关费用。

**Q: 数据安全吗？**

A: 所有数据保存在本地，不会上传到任何服务器。只有调用 AI 分析时，文本内容会发送到 DeepSeek API。

**Q: 支持哪些文件格式？**

A: 目前支持纯文本格式，其他文件格式（PDF、Word、PPT）正在开发中。

## 贡献指南

欢迎贡献代码！请遵循以下步骤：

1. Fork 本仓库
2. 创建你的特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交你的修改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 开启一个 Pull Request

## 许可证

本项目采用 MIT 许可证 - 详见 [LICENSE](LICENSE) 文件

## 致谢

- [DeepSeek](https://www.deepseek.com/) - 提供 AI API 服务
- [Newtonsoft.Json](https://www.newtonsoft.com/json) - JSON 处理库

## 联系方式

- GitHub Issues: [提交问题](https://github.com/Mr-IronBlood/Fxxk-DDL/issues)

## 关于名字 

- 这个项目一开始叫做Fxxk DDL，但是我在第一次打字的时候打错了QAQ
- 变成DLL力QAQ
- 后来为了听起来好点更名为DDL Solver
- 等后续再改名改回为Fxxk DDL

---

如果这个项目对你有帮助，请给个 Star ⭐
