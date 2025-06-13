using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
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
                MessageBox.Show("League Client не запущен или не удалось получить параметры запуска.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return Task.FromResult<LcuCredentials?>(null);
            }

            var portMatch = Regex.Match(commandLine, "--app-port=([0-9]*)");
            var authTokenMatch = Regex.Match(commandLine, "--remoting-auth-token=([\\w-]*)");

            if (!portMatch.Success || !authTokenMatch.Success)
            {
                MessageBox.Show("Не удалось найти порт или токен авторизации в параметрах запуска.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
            MessageBox.Show($"Ошибка при получении данных клиента: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            return Task.FromResult<LcuCredentials?>(null);
        }
    }
} 