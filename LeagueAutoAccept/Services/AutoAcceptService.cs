using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using LeagueAutoAccept.Utils;
using Newtonsoft.Json.Linq;

namespace LeagueAutoAccept.Services;

public class AutoAcceptService
{
    private HttpClient _httpClient;
    private LcuCredentials? _credentials;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _autoAcceptTask;
    private bool _isRunning;

    public AutoAcceptService()
    {
        _httpClient = CreateHttpClient();
    }

    public async Task StartAutoAccept()
    {
        if (_isRunning) return;
        
        try
        {
            Debug.WriteLine("[AutoAccept] Getting LCU credentials...");
            _credentials = await LcuUtils.GetLcuCredentials();
            if (_credentials == null)
            {
                MessageBox.Show("Не удалось получить доступ к League Client. Убедитесь, что клиент запущен.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"riot:{_credentials.Password}")));

            _cancellationTokenSource = new CancellationTokenSource();
            _isRunning = true;
            Debug.WriteLine("[AutoAccept] Started polling loop");
            _autoAcceptTask = Task.Run(() => AutoAcceptLoop(_cancellationTokenSource.Token));
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
            _cancellationTokenSource?.Dispose();
            _autoAcceptTask = null;
            _isRunning = false;
            Debug.WriteLine("[AutoAccept] Stopped");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AutoAccept] Stop error: {ex.Message}");
        }
    }

    private async Task AutoAcceptLoop(CancellationToken cancellationToken)
    {
        if (_credentials == null)
        {
            // попытка получить креды при старте
            await RefreshCredentialsAsync();
            if (_credentials == null) return; // не удалось — выходим из цикла
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            if (_credentials == null)
            {
                await RefreshCredentialsAsync();
                await Task.Delay(1000, cancellationToken);
                continue;
            }
            try
            {
                Debug.WriteLine("[AutoAccept] Polling gameflow session...");
                var response = await _httpClient.GetAsync($"https://127.0.0.1:{_credentials.Port}/lol-gameflow/v1/session", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var json = JObject.Parse(content);
                    var phase = json["phase"]?.ToString();
                    Debug.WriteLine($"[AutoAccept] Phase: {phase}");

                    if (phase == "ReadyCheck")
                    {
                        await AcceptMatch(cancellationToken);
                    }
                }
                else if ((int)response.StatusCode == 401)
                {
                    // токен устарел — обновляем креды
                    Debug.WriteLine("[AutoAccept] 401 Unauthorized. Refreshing credentials...");
                    await RefreshCredentialsAsync();
                }
                else if ((int)response.StatusCode == 403)
                {
                    Debug.WriteLine("[AutoAccept] 403 Forbidden. Refreshing credentials...");
                    await RefreshCredentialsAsync();
                }
                else if ((int)response.StatusCode != 404)
                {
                    Debug.WriteLine($"[AutoAccept] Session response: {(int)response.StatusCode}");
                }
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (HttpRequestException ex)
            {
                // соединение скорее всего отвалилось из-за закрытия клиента
                Debug.WriteLine($"[AutoAccept] Http error: {ex.Message}. Trying to refresh credentials...");
                await RefreshCredentialsAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AutoAccept] Error: {ex.Message}");
            }

            await Task.Delay(1000, cancellationToken);
        }
    }

    private async Task AcceptMatch(CancellationToken cancellationToken)
    {
        if (_credentials == null) return;
        try
        {
            var response = await _httpClient.PostAsync($"https://127.0.0.1:{_credentials.Port}/lol-matchmaking/v1/ready-check/accept", null, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                Debug.WriteLine("[AutoAccept] Match accepted!");
            }
            else
            {
                // 409 Conflict / 500 Internal may occur if уже приняли или перешли к ChampSelect – игнорируем
                Debug.WriteLine($"[AutoAccept] Accept response {(int)response.StatusCode} – ignored");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AutoAccept] Accept error: {ex.Message}");
        }
    }

    private async Task RefreshCredentialsAsync()
    {
        try
        {
            var creds = await LcuUtils.GetLcuCredentials();
            if (creds == null)
            {
                _credentials = null;
                return;
            }

            // если ничего не изменилось — выходим
            if (_credentials != null && _credentials.Port == creds.Port && _credentials.Password == creds.Password)
            {
                return;
            }

            _credentials = creds;

            _httpClient.Dispose();
            _httpClient = CreateHttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"riot:{_credentials.Password}")));

            Debug.WriteLine($"[AutoAccept] Credentials refreshed. New port: {_credentials.Port}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AutoAccept] Failed to refresh credentials: {ex.Message}");
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        return new HttpClient(handler);
    }
} 