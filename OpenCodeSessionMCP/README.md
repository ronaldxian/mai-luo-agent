# OpenCodeSessionMCP

C# MCP 工具，用于同步 OpenCode 会话到自定义服务器。使用官方 `ModelContextProtocol` SDK。

## 功能

| 工具 | 描述 |
|------|------|
| `ExportSession` | 导出会话并上传到服务器 |
| `ImportSession` | 从服务器下载并导入会话 |
| `ListRemoteSessions` | 列出服务器上的会话 |

## 配置

编辑 `appsettings.json`:

```json
{
  "RestApi": {
    "BaseUrl": "https://your-server.com/api",
    "ApiKey": "your-bearer-token"
  },
  "OpenCode": {
    "CliPath": "opencode"
  }
}
```

## OpenCode 配置

在 `opencode.json` 中添加:

```json
{
  "mcp": {
    "session-sync": {
      "type": "local",
      "command": ["dotnet", "run", "--project", "/path/to/OpenCodeSessionMCP"],
      "enabled": true
    }
  }
}
```

## REST API 接口

| 方法 | 端点 | 描述 |
|------|------|------|
| POST | `/sessions/upload` | 上传 `{ sessionId, data }` |
| GET | `/sessions` | 列出会话 |
| GET | `/sessions/{id}` | 下载会话 |
| DELETE | `/sessions/{id}` | 删除会话 |

## 构建

```bash
dotnet build --configuration Release
```

## 运行

```bash
dotnet run
```
