using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class McpTest
{
    static async Task Main(string[] args)
    {
        var serverDll = @"D:\Projects\mai-luo-agent\OpenCodeSessionMCP\bin\Debug\net10.0\OpenCodeSessionMCP.dll";
        var workingDir = @"D:\Projects\mai-luo-agent\OpenCodeSessionMCP\bin\Debug\net10.0";

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = serverDll,
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var input = process.StandardInput;
        var output = process.StandardOutput;
        var error = process.StandardError;

        // Send initialize
        var initMsg = new { jsonrpc = "2.0", id = "1", method = "initialize", params = new { protocolVersion = "2024-11-05", capabilities = new { }, clientInfo = new { name = "test", version = "1.0.0" } } };
        await input.WriteLineAsync(JsonSerializer.Serialize(initMsg));
        await input.FlushAsync();

        await Task.Delay(500);

        // Send initialized notification
        var notifMsg = new { jsonrpc = "2.0", method = "notifications/initialized", params = new { } };
        await input.WriteLineAsync(JsonSerializer.Serialize(notifMsg));
        await input.FlushAsync();

        await Task.Delay(500);

        // Send tools/list
        var listMsg = new { jsonrpc = "2.0", id = "2", method = "tools/list", params = new { } };
        await input.WriteLineAsync(JsonSerializer.Serialize(listMsg));
        await input.FlushAsync();

        // Read response
        var response = await output.ReadLineAsync();
        Console.WriteLine("Received: " + response);

        // Send hello tool call
        var helloMsg = new { jsonrpc = "2.0", id = "3", method = "tools/call", params = new { name = "hello", arguments = new { } } };
        await input.WriteLineAsync(JsonSerializer.Serialize(helloMsg));
        await input.FlushAsync();

        await Task.Delay(500);
        response = await output.ReadLineAsync();
        Console.WriteLine("Hello response: " + response);

        // Send export_session_from_project_path tool call
        var exportMsg = new { jsonrpc = "2.0", id = "4", method = "tools/call", params = new { name = "export_session_from_project_path", arguments = new { sessionPath = @"D:\Projects\mai-luo-agent" } } };
        await input.WriteLineAsync(JsonSerializer.Serialize(exportMsg));
        await input.FlushAsync();

        // Wait for export to complete
        await Task.Delay(10000);
        response = await output.ReadLineAsync();
        Console.WriteLine("Export response: " + response);

        // Send list_remote_sessions
        var listRemoteMsg = new { jsonrpc = "2.0", id = "5", method = "tools/call", params = new { name = "list_remote_sessions", arguments = new { } } };
        await input.WriteLineAsync(JsonSerializer.Serialize(listRemoteMsg));
        await input.FlushAsync();

        await Task.Delay(2000);
        response = await output.ReadLineAsync();
        Console.WriteLine("List remote response: " + response);

        input.WriteLine("null");
        await input.FlushAsync();

        process.WaitForExit(5000);
        if (!process.HasExited)
        {
            process.Kill();
        }

        Console.WriteLine("Test complete");
    }
}
