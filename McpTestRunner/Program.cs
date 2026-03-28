using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

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
            CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var input = process.StandardInput;
        var output = process.StandardOutput;
        var error = process.StandardError;

        // Read error stream in background
        var errorTask = Task.Run(() => {
            string line;
            while ((line = error.ReadLine()) != null) {
                Console.WriteLine("[STDERR] " + line);
            }
        });

        var initMsg = new { jsonrpc = "2.0", id = "1", method = "initialize", @params = new { protocolVersion = "2024-11-05", capabilities = new { }, clientInfo = new { name = "test", version = "1.0.0" } } };
        Console.WriteLine("Sending: " + JsonSerializer.Serialize(initMsg));
        await input.WriteLineAsync(JsonSerializer.Serialize(initMsg));
        await input.FlushAsync();

        await Task.Delay(1000);

        var notifMsg = new { jsonrpc = "2.0", method = "notifications/initialized", @params = new { } };
        Console.WriteLine("Sending: " + JsonSerializer.Serialize(notifMsg));
        await input.WriteLineAsync(JsonSerializer.Serialize(notifMsg));
        await input.FlushAsync();

        await Task.Delay(1000);

        var listMsg = new { jsonrpc = "2.0", id = "2", method = "tools/list", @params = new { } };
        Console.WriteLine("Sending: " + JsonSerializer.Serialize(listMsg));
        await input.WriteLineAsync(JsonSerializer.Serialize(listMsg));
        await input.FlushAsync();

        await Task.Delay(2000);
        
        // Read all available output
        Console.WriteLine("Reading output...");
        while (!output.EndOfStream)
        {
            var line = output.ReadLine();
            if (line != null)
            {
                Console.WriteLine("[STDOUT] " + line);
            }
            else
            {
                break;
            }
        }

        var helloMsg = new { jsonrpc = "2.0", id = "3", method = "tools/call", @params = new { name = "hello", arguments = new { } } };
        Console.WriteLine("Sending: " + JsonSerializer.Serialize(helloMsg));
        await input.WriteLineAsync(JsonSerializer.Serialize(helloMsg));
        await input.FlushAsync();

        await Task.Delay(2000);
        
        while (!output.EndOfStream)
        {
            var line = output.ReadLine();
            if (line != null)
            {
                Console.WriteLine("[STDOUT] " + line);
            }
            else
            {
                break;
            }
        }

        var exportMsg = new { jsonrpc = "2.0", id = "4", method = "tools/call", @params = new { name = "export_session_from_project_path", arguments = new { sessionPath = @"D:\Projects\mai-luo-agent" } } };
        Console.WriteLine("Sending: " + JsonSerializer.Serialize(exportMsg));
        await input.WriteLineAsync(JsonSerializer.Serialize(exportMsg));
        await input.FlushAsync();

        await Task.Delay(15000);
        
        while (!output.EndOfStream)
        {
            var line = output.ReadLine();
            if (line != null)
            {
                Console.WriteLine("[STDOUT] " + line);
            }
            else
            {
                break;
            }
        }

        var listRemoteMsg = new { jsonrpc = "2.0", id = "5", method = "tools/call", @params = new { name = "list_remote_sessions", arguments = new { } } };
        Console.WriteLine("Sending: " + JsonSerializer.Serialize(listRemoteMsg));
        await input.WriteLineAsync(JsonSerializer.Serialize(listRemoteMsg));
        await input.FlushAsync();

        await Task.Delay(3000);
        
        while (!output.EndOfStream)
        {
            var line = output.ReadLine();
            if (line != null)
            {
                Console.WriteLine("[STDOUT] " + line);
            }
            else
            {
                break;
            }
        }

        input.WriteLine("null");
        await input.FlushAsync();

        try {
            process.WaitForExit(5000);
        } catch {}
        if (!process.HasExited)
        {
            process.Kill();
        }

        Console.WriteLine("Test complete");
    }
}
