using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenCodeSessionMCP.Configuration;
using OpenCodeSessionMCP.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddLogging(logging =>
{
    logging.AddSerilog();
    logging.SetMinimumLevel(LogLevel.Trace);
});

builder.Services.AddHttpClient();
builder.Services.Configure<AppSettings>(builder.Configuration);
builder.Services.AddSingleton<IOpenCodeService, OpenCodeService>();
builder.Services.AddSingleton<ISessionSyncService, SessionSyncService>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
