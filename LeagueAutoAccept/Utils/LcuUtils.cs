using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;

namespace LeagueAutoAccept.Utils;

public static class LcuUtils
{
    public static async Task<LcuCredentials?> GetLcuCredentials()
    {
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
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (string.IsNullOrWhiteSpace(output))
            {
                MessageBox.Show("League Client не запущен. Запустите клиент и попробуйте снова.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            var portMatch = Regex.Match(output, "--app-port=([0-9]*)");
            var authTokenMatch = Regex.Match(output, "--remoting-auth-token=([\\w-]*)");

            if (!portMatch.Success || !authTokenMatch.Success)
            {
                MessageBox.Show("Не удалось найти порт или токен авторизации в параметрах запуска.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            return new LcuCredentials
            {
                Port = portMatch.Groups[1].Value,
                Password = authTokenMatch.Groups[1].Value
            };
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при получении данных клиента: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
    }
} 