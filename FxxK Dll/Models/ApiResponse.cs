using System.Collections.Generic;
using Newtonsoft.Json;

namespace FxxkDDL.Models
{
    // AI返回的任务项
    public class DDLExtractionResult
    {
        [JsonProperty("tasks")]
        public List<ExtractedTask> Tasks { get; set; } = new List<ExtractedTask>();
    }

    public class ExtractedTask
    {
        [JsonProperty("task")]
        public string TaskDescription { get; set; } = "";

        [JsonProperty("deadline")]
        public string Deadline { get; set; } = "";

        [JsonProperty("importance")]
        public string Importance { get; set; } = "中";  // 高/中/低

        [JsonProperty("original_context")]
        public string OriginalContext { get; set; } = "";

        [JsonProperty("confidence")]
        public double Confidence { get; set; } = 0.8;
    }

    // DeepSeek API请求模型
    public class DeepSeekRequest
    {
        [JsonProperty("model")]
        public string Model { get; set; } = "deepseek-chat";

        [JsonProperty("messages")]
        public List<Message> Messages { get; set; }

        [JsonProperty("temperature")]
        public double Temperature { get; set; } = 0.3;

        [JsonProperty("max_tokens")]
        public int MaxTokens { get; set; } = 2000;
    }

    public class Message
    {
        [JsonProperty("role")]
        public string Role { get; set; }  // "user" 或 "assistant"

        [JsonProperty("content")]
        public string Content { get; set; }
    }

    // DeepSeek API响应模型
    public class DeepSeekResponse
    {
        [JsonProperty("choices")]
        public List<Choice> Choices { get; set; }

        [JsonProperty("usage")]
        public Usage Usage { get; set; }
    }

    public class Choice
    {
        [JsonProperty("message")]
        public Message Message { get; set; }
    }

    public class Usage
    {
        [JsonProperty("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonProperty("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonProperty("total_tokens")]
        public int TotalTokens { get; set; }
    }
}
