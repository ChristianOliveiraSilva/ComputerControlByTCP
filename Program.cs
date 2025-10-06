using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using System.Threading;

class TcpActionServer
{
    const int Port = 5000;
    const string Secret = "troque_esse_seguro_token";
    static bool running = true;

    static void Main()
    {
        Console.CancelKeyPress += (s,e) => { running = false; e.Cancel = true; };
        var listener = new TcpListener(IPAddress.Any, Port);
        listener.Start();
        Console.WriteLine($"Listening on port {Port}. Ctrl+C to stop.");

        while (running)
        {
            if (!listener.Pending())
            {
                Thread.Sleep(100);
                continue;
            }

            using var client = listener.AcceptTcpClient();
            using var ns = client.GetStream();
            using var reader = new StreamReader(ns, Encoding.UTF8);
            string line = reader.ReadLine();
            if (string.IsNullOrEmpty(line)) continue;

            var parts = line.Split(':', 2);
            if (parts.Length < 2)
            {
                SendResponse(ns, "ERR:format");
                continue;
            }

            if (parts[0] != Secret)
            {
                SendResponse(ns, "ERR:auth");
                continue;
            }

            var cmd = parts[1].Trim();
            try
            {
                var result = ExecuteCommand(cmd);
                SendResponse(ns, "OK:" + result);
            }
            catch (Exception ex)
            {
                SendResponse(ns, "ERR:" + ex.Message.Replace("\n"," "));
            }
        }

        listener.Stop();
        Console.WriteLine("Stopped.");
    }

    static void SendResponse(NetworkStream ns, string msg)
    {
        var data = Encoding.UTF8.GetBytes(msg + "\n");
        ns.Write(data, 0, data.Length);
        ns.Flush();
    }

    static string ExecuteCommand(string cmd)
    {
        if (cmd.Equals("notepad", StringComparison.OrdinalIgnoreCase))
        {
            Process.Start(new ProcessStartInfo("notepad") { UseShellExecute = true });
            return "launched_notepad";
        }

        if (cmd.Equals("calc", StringComparison.OrdinalIgnoreCase) || cmd.Equals("calculator", StringComparison.OrdinalIgnoreCase))
        {
            Process.Start(new ProcessStartInfo("calc") { UseShellExecute = true });
            return "launched_calc";
        }

        if (cmd.StartsWith("create-file ", StringComparison.OrdinalIgnoreCase))
        {
            var fname = cmd.Substring(12).Trim();
            if (string.IsNullOrEmpty(fname)) fname = "default.txt";
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var path = Path.Combine(desktop, fname);
            File.WriteAllText(path, $"Created by TcpActionServer at {DateTime.UtcNow:O}");
            return "file_created:" + path;
        }

        if (cmd.StartsWith("echo ", StringComparison.OrdinalIgnoreCase))
        {
            return cmd.Substring(5);
        }

        throw new InvalidOperationException("unknown_command");
    }
}
