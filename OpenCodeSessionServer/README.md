# OpenCodeSessionServer

ASP.NET Core Web API 服务器，用于存储 OpenCode 会话数据。

## 功能

- 上传、下载、列出、删除会话
- 文件系统存储，按日期分目录
- 可选 Bearer Token 认证

## 配置

`appsettings.json`:

```json
{
  "Storage": {
    "RootPath": "./sessions"
  },
  "Authentication": {
    "ApiKey": "your-api-key"
  }
}
```

| 配置项 | 说明 | 默认值 |
|--------|------|--------|
| `Storage:RootPath` | 存储根目录 | `./sessions` |
| `Authentication:ApiKey` | Bearer Token (留空则禁用认证) | (空) |

## API 端点

| 方法 | 路径 | 描述 | 请求体 | 响应 |
|------|------|------|--------|------|
| POST | `/sessions/upload` | 上传会话 | `{ sessionId, data: base64 }` | `{ id, message }` |
| GET | `/sessions` | 列出当天会话 | - | `[{ id, sessionId, title, timeCreated }]` |
| GET | `/sessions/{id}` | 下载会话 | - | `{ sessionId, data: base64 }` |
| DELETE | `/sessions/{id}` | 删除会话 | - | 204 No Content |

## 存储结构

```
./sessions/
└── 2026-03-27/
    └── abc123_1743072000000.json
```

## 构建

```bash
dotnet build --configuration Release
```

## 运行

```bash
dotnet run
```

## 测试

```bash
# 上传
curl -X POST http://localhost:5153/sessions/upload \
  -H "Content-Type: application/json" \
  -d '{"sessionId":"test","data":"SGVsbG8gV29ybGQ="}'

# 列表
curl http://localhost:5153/sessions

# 下载
curl http://localhost:5153/sessions/test_1743072000000

# 删除
curl -X DELETE http://localhost:5153/sessions/test_1743072000000
```
