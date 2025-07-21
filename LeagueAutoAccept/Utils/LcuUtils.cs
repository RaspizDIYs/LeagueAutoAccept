using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Management;

namespace LeagueAutoAccept.Utils;

public static class LcuUtils
{
    public static Task<LcuCredentials?> GetLcuCredentials()
    {
        try
        {
            string? commandLine = null;

            // Первым делом пытаемся через wmic (быстро и без зависимостей)
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "wmic",
                        Arguments = "PROCESS WHERE name='LeagueClientUx.exe' GET commandline",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                commandLine = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }
            catch (Win32Exception) // wmic отсутствует (Win11)
            {
                commandLine = null;
            }

            // Если wmic не сработал – используем WMI напрямую
            if (string.IsNullOrWhiteSpace(commandLine))
            {
                var query = "SELECT CommandLine FROM Win32_Process WHERE Name = 'LeagueClientUx.exe'";
                using var searcher = new ManagementObjectSearcher(query);
                using var results = searcher.Get();
                foreach (var result in results)
                {
                    commandLine = result["CommandLine"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(commandLine)) break;
                }
            }

            if (string.IsNullOrWhiteSpace(commandLine))
            {
                // Клиент не запущен – молча возвращаем null, вызывающая сторона решит что делать
                return Task.FromResult<LcuCredentials?>(null);
            }

            var portMatch = Regex.Match(commandLine, "--app-port=([0-9]*)");
            var authTokenMatch = Regex.Match(commandLine, "--remoting-auth-token=([\\w-]*)");

            if (!portMatch.Success || !authTokenMatch.Success)
            {
                // Не нашли необходимые параметры – возвращаем null без всплывающих окон
                return Task.FromResult<LcuCredentials?>(null);
            }

            return Task.FromResult(new LcuCredentials
            {
                Port = portMatch.Groups[1].Value,
                Password = authTokenMatch.Groups[1].Value
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ошибка при получении данных клиента: {ex.Message}");
            return Task.FromResult<LcuCredentials?>(null);
        }
    }
} 