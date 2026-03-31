# AGENTS.md - MaiLuoAgent 开发指南

## 项目结构

```
MaiLuoAgent.slnx
└── OpenCodeSessionMCP/          # MCP 客户端 (ModelContextProtocol SDK)
    ├── OpenCodeSessionMCP.csproj
    ├── Program.cs
    ├── appsettings.json
    ├── Configuration/
    │   └── AppSettings.cs
    ├── Models/
    │   └── SessionData.cs
    └── Services/
        ├── SessionTools.cs       # MCP 工具定义
        ├── SessionSyncService.cs  # 核心业务逻辑
        ├── OpenCodeService.cs    # OpenCode CLI 封装
        ├── GistService.cs        # GitHub Gist API 封装
        └── RegistryService.cs     # 本地注册表
```

## 架构

```
OpenCode → MCP → GitHub Gist API → GitHub Gist
                    ↓
            本地 Registry (sessionId ↔ gistId 映射)
```

## 环境变量

| 变量 | 说明 |
|------|------|
| `GITHUB_TOKEN` | GitHub Personal Access Token (必须) |

## 构建命令

### 构建项目
```bash
dotnet build MaiLuoAgent.slnx
```

### 运行项目
```bash
dotnet run --project OpenCodeSessionMCP
```

### 发布项目
```bash
dotnet publish OpenCodeSessionMCP/OpenCodeSessionMCP.csproj -c Release -o ./publish/mcp
```

## 技术栈

| 项目 | 技术 |
|------|------|
| 框架 | .NET 10.0 |
| C# 版本 | 13.0 |
| MCP 协议 | ModelContextProtocol 1.1.0 |
| 数据库 | Microsoft.Data.Sqlite 10.0.0 |
| 日志 | Serilog |
| JSON | System.Text.Json (camelCase) |

## MCP 工具

| 工具 | 参数 | 说明 |
|------|------|------|
| `hello` | - | 测试工具 |
| `get_session_info` | `sessionPath` | 获取本地 session ID 和 title |
| `export_session` | `sessionId` | 导出到 GitHub Gist |
| `import_session` | `sessionId` | 从 GitHub Gist 导入 |
| `list_sessions` | - | 列出已同步的会话 |
| `delete_session` | `sessionId` | 删除 Gist 中的会话 |

## 代码风格规范

### 命名规范

| 类型 | 规范 | 示例 |
|------|------|------|
| 命名空间 | `项目名.文件夹结构` | `OpenCodeSessionMCP.Services` |
| 类 | PascalCase, `sealed` | `SessionSyncService` |
| 接口 | `I` 前缀 | `IOpenCodeService` |
| 私有字段 | `_camelCase` | `_logger`, `_settings` |
| 记录类型 | PascalCase | `ExportResult`, `UploadRequest` |

### C# 语言特性

- **可空引用类型**: 启用 - 使用 `string?`
- **隐式 using**: 启用
- **可空检查**: 使用 `string.IsNullOrEmpty()` 和 `??` 操作符

### JSON 序列化

使用 `System.Text.Json`，camelCase 命名策略：

```csharp
var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false
};
```

### 日志记录

使用结构化日志 `ILogger<T>`：

```csharp
_logger.LogInformation("操作开始. Id: {Id}", id);
_logger.LogDebug("读取文件完成. 大小: {Size} 字节", size);
_logger.LogWarning("操作失败. 错误: {Error}", error);
_logger.LogError(ex, "发生异常. Id: {Id}", id);
```

### 错误处理

对于可预期的错误，使用结果对象而非抛出异常：

```csharp
public sealed class ExportResult
{
    public bool Success { get; set; }
    public string? RemoteId { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}
```

### 依赖注入

使用构造函数注入：

```csharp
public sealed class SessionSyncService
{
    private readonly OpenCodeService _openCodeService;
    private readonly GistService _gistService;

    public SessionSyncService(
        OpenCodeService openCodeService,
        GistService gistService)
    {
        _openCodeService = openCodeService;
        _gistService = gistService;
    }
}
```

### 文件组织

- 每个文件一个公共类/记录
- 按功能分组：Models、Services
- 配置类放在 `Configuration/` 文件夹

### 异步编程

- I/O 操作使用 `async Task`
- 为可取消操作提供 `CancellationToken` 参数
- 默认值: `CancellationToken ct = default`

### MCP 工具定义

使用 `ModelContextProtocol.Server` 属性：

```csharp
[McpServerToolType]
public static class SessionTools
{
    [McpServerTool, Description("工具描述")]
    public static async Task<string> ToolName(
        IServiceProvider serviceProvider,
        [Description("参数描述")] string? param = null)
    {
        // 实现...
    }
}
```

### SQLite 数据库访问

使用 `Microsoft.Data.Sqlite`：

```csharp
using Microsoft.Data.Sqlite;

using var connection = new SqliteConnection($"Data Source={dbPath}");
connection.Open();

using var command = connection.CreateCommand();
command.CommandText = "SELECT id, title FROM session WHERE directory = @directory LIMIT 1";
command.Parameters.AddWithValue("@directory", sessionPath);

using var reader = command.ExecuteReader();
if (reader.Read())
{
    sessionId = reader.GetString(0);
    title = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
}
```

### GitHub Gist API

使用 `HttpClient` 直接调用 GitHub API：

```csharp
_httpClient.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", _token);
```

## appsettings.json 配置

```json
{
  "App": {
    "GitHub": {
      "TokenEnvVar": "GITHUB_TOKEN"
    },
    "OpenCode": {
      "CliPath": "opencode"
    }
  },
  "Serilog": {
    "MinimumLevel": "Debug",
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "Logs\\.log",
          "rollingInterval": "Day"
        }
      }
    ],
    "Overrides": {
      "Microsoft": "Warning"
    }
  }
}
```

## 本地注册表

存储位置: `%APPDATA%/opencode-sessions/registry.json`

格式:
```json
{
  "sessions": {
    "session_abc123": {
      "gistId": "gist_id_xyz",
      "title": "Session Title",
      "updatedAt": 1234567890
    }
  }
}
```
