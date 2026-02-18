using FxxkDDL.Core.Interfaces;
using FxxkDDL.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace FxxkDDL.Services
{
    public class DeepSeekService : IDeepSeekService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfigService _configService;
        private const string ApiUrl = "https://api.deepseek.com/v1/chat/completions";

        public DeepSeekService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _configService = new ConfigService();
        }

        /// <summary>
        /// 测试API连接是否正常
        /// </summary>
        public async Task<(bool Success, string Message)> TestApiConnectionAsync()
        {
            var config = _configService.GetConfig();

            if (string.IsNullOrWhiteSpace(config.DeepSeekApiKey))
            {
                return (false, "未配置API密钥");
            }

            try
            {
                // 发送一个简单的测试请求
                var testRequest = new
                {
                    model = "deepseek-chat",
                    messages = new[]
                    {
                        new { role = "user", content = "测试连接，请回复'连接成功'" }
                    },
                    max_tokens = 10,
                    temperature = 0.1
                };

                string jsonRequest = JsonConvert.SerializeObject(testRequest);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.DeepSeekApiKey}");

                var response = await _httpClient.PostAsync(ApiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    return (true, "✅ API连接测试成功！");
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    return (false, $"❌ API连接失败: {response.StatusCode}\n{errorResponse}");
                }
            }
            catch (HttpRequestException ex)
            {
                return (false, $"❌ 网络连接失败: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                return (false, "❌ 请求超时，请检查网络连接");
            }
            catch (Exception ex)
            {
                return (false, $"❌ 连接测试异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 从聊天记录中提取DDL任务
        /// </summary>
        public async Task<(bool Success, List<DDLTask> Tasks, string Message)> ExtractDDLFromTextAsync(string chatText)
        {
            var config = _configService.GetConfig();

            if (string.IsNullOrWhiteSpace(config.DeepSeekApiKey))
            {
                return (false, null, "请先在设置中配置DeepSeek API密钥");
            }

            try
            {
                // 准备请求
                string systemPrompt = GetSystemPrompt();
                string userPrompt = $"请从以下聊天记录中提取DDL任务：\n\n{chatText}";

                var request = new
                {
                    model = config.Model,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt }
                    },
                    temperature = config.Temperature,
                    max_tokens = config.MaxTokens
                };

                string jsonRequest = JsonConvert.SerializeObject(request);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.DeepSeekApiKey}");

                // 发送请求
                var response = await _httpClient.PostAsync(ApiUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    return (false, null, $"API请求失败: {response.StatusCode}\n{errorResponse}");
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();

                // 解析响应
                var result = JsonConvert.DeserializeObject<dynamic>(jsonResponse);
                string aiResponse = result.choices[0].message.content;

                // 解析AI返回的结果为任务列表
                var tasks = ParseAIResponse(aiResponse);

                if (tasks.Count == 0)
                {
                    return (true, tasks, "分析完成，但未提取到明确的DDL任务");
                }

                return (true, tasks, $"✅ 成功提取到 {tasks.Count} 个DDL任务");
            }
            catch (HttpRequestException ex)
            {
                return (false, null, $"网络请求失败: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                return (false, null, "请求超时，请检查网络连接");
            }
            catch (Exception ex)
            {
                return (false, null, $"提取DDL失败: {ex.Message}");
            }
        }

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

        public async Task<(bool Success, List<DDLTask> Tasks, string Message)> ExtractDDLFromFileAsync(string filePath)
        {
            var config = _configService.GetConfig();

            if (string.IsNullOrWhiteSpace(config.DeepSeekApiKey))
            {
                return (false, null, "请先在设置中配置DeepSeek API密钥");
            }

            if (!File.Exists(filePath))
            {
                return (false, null, $"文件不存在: {filePath}");
            }

            try
            {
                // 读取文件字节
                byte[] fileBytes = File.ReadAllBytes(filePath);
                string fileName = Path.GetFileName(filePath);
                string fileExtension = Path.GetExtension(filePath).ToLower();

                // 准备系统提示词
                string systemPrompt = GetSystemPrompt();

                // 构建multipart/form-data请求
                using var formData = new MultipartFormDataContent();

                // 添加系统提示词
                formData.Add(new StringContent(systemPrompt), "system_prompt");

                // 添加文件
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(GetMimeType(fileExtension));
                formData.Add(fileContent, "file", fileName);

                // 添加其他参数
                formData.Add(new StringContent(config.Model), "model");
                formData.Add(new StringContent(config.Temperature.ToString()), "temperature");
                formData.Add(new StringContent(config.MaxTokens.ToString()), "max_tokens");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.DeepSeekApiKey}");

                // 发送请求到DeepSeek API（假设支持文件上传的端点）
                // 注意：需要确认DeepSeek API实际的文件上传端点
                var response = await _httpClient.PostAsync(ApiUrl, formData);

                if (!response.IsSuccessStatusCode)
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    return (false, null, $"API请求失败: {response.StatusCode}\n{errorResponse}");
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();

                // 解析响应
                var result = JsonConvert.DeserializeObject<dynamic>(jsonResponse);
                string aiResponse = result.choices[0].message.content;

                // 解析AI返回的结果为任务列表
                var tasks = ParseAIResponse(aiResponse);

                if (tasks.Count == 0)
                {
                    return (true, tasks, "分析完成，但未从文档中提取到明确的DDL任务");
                }

                return (true, tasks, $"✅ 成功从文档提取到 {tasks.Count} 个DDL任务");
            }
            catch (HttpRequestException ex)
            {
                return (false, null, $"网络请求失败: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                return (false, null, "请求超时，请检查网络连接");
            }
            catch (Exception ex)
            {
                return (false, null, $"文件分析失败: {ex.Message}");
            }
        }

        private string GetMimeType(string fileExtension)
        {
            return fileExtension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }

        public string GetApiStatus()
        {
            var config = _configService.GetConfig();

            if (string.IsNullOrWhiteSpace(config.DeepSeekApiKey))
            {
                return "❌ 未配置API密钥";
            }

            return $"✅ API已配置 ({_configService.GetMaskedApiKey()})";
        }
    }
}
