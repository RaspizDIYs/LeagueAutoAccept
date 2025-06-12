using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using LeagueAutoAccept.Utils;
using Newtonsoft.Json.Linq;

namespace LeagueAutoAccept.Services;

public class AutoAcceptService
{
    private readonly HttpClient _httpClient;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _autoAcceptTask;
    private bool _isRunning;

    public AutoAcceptService()
    {
        _httpClient = new HttpClient();
    }

    public async Task StartAutoAccept()
    {
        if (_isRunning) return;
        
        try
        {
            var credentials = await LcuUtils.GetLcuCredentials();
            if (credentials == null)
            {
                MessageBox.Show("Не удалось получить доступ к League Client. Убедитесь, что клиент запущен.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _isRunning = true;
            _autoAcceptTask = Task.Run(() => AutoAcceptLoop(credentials, _cancellationTokenSource.Token));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при запуске автопринятия: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            StopAutoAccept();
        }
    }

    public void StopAutoAccept()
    {
        if (!_isRunning) return;
        
        try
        {
            _cancellationTokenSource?.Cancel();
            _autoAcceptTask?.Wait(1000); // Даем время на корректное завершение
            _cancellationTokenSource?.Dispose();
            _isRunning = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при остановке автопринятия: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task AutoAcceptLoop(LcuCredentials credentials, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"{credentials.Protocol}://127.0.0.1:{credentials.Port}/lol-matchmaking/v1/ready-check",
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var json = JObject.Parse(content);
                    
                    if (json["state"]?.ToString() == "InProgress")
                    {
                        await AcceptMatch(credentials, cancellationToken);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // Игнорируем ошибки 404 и другие временные проблемы
                if (!ex.Message.Contains("404"))
                {
                    MessageBox.Show($"Ошибка при проверке матча: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
                }
            }

            await Task.Delay(1000, cancellationToken);
        }
    }

    private async Task AcceptMatch(LcuCredentials credentials, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"{credentials.Protocol}://127.0.0.1:{credentials.Port}/lol-matchmaking/v1/ready-check/accept",
                new StringContent(""),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Ошибка при принятии матча: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при принятии матча: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
} 