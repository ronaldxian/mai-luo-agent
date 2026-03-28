# AGENTS.md - MaiLuoAgent 开发指南

## 项目结构

```
MaiLuoAgent.slnx
├── OpenCodeSessionMCP/          # MCP 客户端 (ModelContextProtocol SDK)
│   ├── OpenCodeSessionMCP.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Configuration/
│   ├── Models/
│   └── Services/
└── OpenCodeSessionServer/        # REST API 服务器
    ├── OpenCodeSessionServer.csproj
    ├── Program.cs
    ├── appsettings.json
    ├── Controllers/
    ├── Models/
    └── Services/
```

## 构建命令

### 构建整个解决方案
```bash
dotnet build MaiLuoAgent.slnx
```

### 构建单个项目
```bash
dotnet build OpenCodeSessionMCP/OpenCodeSessionMCP.csproj
dotnet build OpenCodeSessionServer/OpenCodeSessionServer.csproj
```

### 运行项目
```bash
# 运行 MCP 客户端
dotnet run --project OpenCodeSessionMCP

# 运行 API 服务器
dotnet run --project OpenCodeSessionServer
```

### 发布项目
```bash
dotnet publish OpenCodeSessionMCP/OpenCodeSessionMCP.csproj -c Release -o ./publish/mcp
dotnet publish OpenCodeSessionServer/OpenCodeSessionServer.csproj -c Release -o ./publish/server
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
public sealed class SessionSyncService : ISessionSyncService
{
    private readonly IOpenCodeService _openCodeService;
    private readonly ILogger<SessionSyncService> _logger;

    public SessionSyncService(
        IOpenCodeService openCodeService,
        ILogger<SessionSyncService> logger)
    {
        _openCodeService = openCodeService;
        _logger = logger;
    }
}
```

### 文件组织

- 每个文件一个公共类/记录
- 按功能分组：Models、Services、Controllers
- 配置类放在 `Configuration/` 文件夹
- 接口与实现放在同一文件夹，使用 `I` 前缀区分

### 异步编程

- I/O 操作使用 `async Task`
- 为可取消操作提供 `CancellationToken` 参数
- 默认值: `CancellationToken ct = default`

### HTTP 客户端使用

- 为每个请求创建新的 `HttpClient()` 实例或使用 `IHttpClientFactory`
- HTTP 错误使用 `HttpRequestException`
- 错误信息包含状态码和响应体

### 环境变量路径

支持 `%环境变量%` 格式的路径：

```csharp
var dbPath = config.DbPath[0] == '%'
    ? Environment.ExpandEnvironmentVariables(config.DbPath)
    : config.DbPath;
```

### MCP 工具定义

使用 `ModelContextProtocol.Server` 属性：

```csharp
[McpServerToolType]
public static class SessionTools
{
    [McpServerTool, Description("工具描述")]
    public static async Task<string> ToolName(
        IServiceProvider serviceProvider,
        ISessionSyncService sessionSyncService,
        [Description("参数描述")] string? param = null)
    {
        var logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger("SessionTools");
        logger?.LogInformation("Tool called. Param: {Param}", param);
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
command.CommandText = "SELECT id FROM session WHERE directory = @dir LIMIT 1";
command.Parameters.AddWithValue("@dir", sessionPath);

var result = command.ExecuteScalar();
```

## 项目特定规范

### OpenCodeSessionMCP

- 使用 Serilog 进行日志记录
- 日志输出到控制台和文件
- 通过 stdio 与 OpenCode 通信
- 支持从 SQLite 数据库查询 sessionId

### OpenCodeSessionServer

- RESTful API 设计
- 文件系统存储，按日期分目录 (`yyyy-MM-dd/`)
- 支持可选 Bearer Token 认证
- Base64 编码传输 session 数据

## 调试技巧

### 查看 MCP 日志
```bash
tail -f OpenCodeSessionMCP/logs/mcp.log
```

### 测试 API 端点
```bash
# 上传会话
curl -X POST http://localhost:5153/sessions/upload \
  -H "Content-Type: application/json" \
  -d '{"sessionId":"test","data":"SGVsbG8gV29ybGQ="}'

# 列出会话
curl http://localhost:5153/sessions

# 下载会话
curl http://localhost:5153/sessions/<id>

# 删除会话
curl -X DELETE http://localhost:5153/sessions/<id>
```

## appsettings.json 配置

### OpenCodeSessionMCP
```json
{
  "RestApi": {
    "BaseUrl": "http://localhost:5153",
    "ApiKey": ""
  },
  "OpenCode": {
    "CliPath": "opencode",
    "DbPath": "%USERPROFILE%\\.local\\share\\opencode\\opencode.db"
  }
}
```

### OpenCodeSessionServer
```json
{
  "Storage": {
    "RootPath": "./sessions"
  },
  "Authentication": {
    "ApiKey": ""
  }
}
```
