using OcCli.Commands;
using Serilog;

Console.OutputEncoding = System.Text.Encoding.UTF8;

const string Banner = @"
╔══════════════════════════════════════════════════════════════╗
║  oc-cli - OpenCode Session CLI                               ║
║  会话导入导出工具                                             ║
╚══════════════════════════════════════════════════════════════╝";

const string Usage = @"
环境变量:
  GITHUB_TOKEN      GitHub 访问令牌（必须）
  OPENCODE_CLI_PATH opencode CLI 路径（可选，默认自动检测）

用法:
  oc-cli <命令> [选项]

命令:
  export <sessionId>  导出会话到 GitHub Gist
  import <sessionId>  从 GitHub Gist 导入会话
  list               列出已同步的会话
  delete <sessionId>  删除 Gist 中的会话

示例:
  oc-cli export abc123
  oc-cli import abc123
  oc-cli list
  oc-cli delete abc123

退出码:
  0  成功
  1  通用失败
  2  Session 未找到
  3  网络错误
  4  Gist API 错误
  5  超时
";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Level:Abbreviation}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

if (args.Length == 0)
{
    Console.WriteLine(Banner);
    Console.WriteLine(Usage);
    return 0;
}

var command = args[0].ToLowerInvariant();
var exitCode = 0;

try
{
    switch (command)
    {
        case "export":
            exitCode = await ExportCommand.ExecuteAsync(args.Skip(1).ToArray());
            break;
        case "import":
            exitCode = await ImportCommand.ExecuteAsync(args.Skip(1).ToArray());
            break;
        case "list":
            exitCode = ListCommand.Execute(args.Skip(1).ToArray());
            break;
        case "delete":
            exitCode = await DeleteCommand.ExecuteAsync(args.Skip(1).ToArray());
            break;
        case "-h":
        case "--help":
            Console.WriteLine(Banner);
            Console.WriteLine(Usage);
            break;
        default:
            AnsiConsole.Error("[ERROR] Unknown command: {0}[/]", command);
            Console.WriteLine("Use 'oc-cli --help' for usage information.");
            exitCode = 1;
            break;
    }
}
catch (Exception ex)
{
    Log.Logger.Error(ex, "Unhandled exception");
    AnsiConsole.Error("[ERROR] {0}[/]", ex.Message);
    exitCode = 1;
}

return exitCode;
