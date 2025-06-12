using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Velopack;
using Velopack.Sources;
using Wpf.Ui.Controls;
using Application = System.Windows.Application;

namespace LeagueAutoAccept;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : FluentWindow
{
    private CancellationTokenSource? _autoAcceptCts;

    public MainWindow()
    {
        InitializeComponent();
        Debug.WriteLine("MainWindow initialized.");
        
        Closing += (_, _) =>
        {
            Debug.WriteLine("MainWindow closing, cancelling background task.");
            _autoAcceptCts?.Cancel();
            NotifyIcon.Dispose();
        };

        CheckForUpdates();
    }

    protected override void OnStateChanged(EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
            NotifyIcon.Visibility = Visibility.Visible;
        }
        
        base.OnStateChanged(e);
    }
    
    private void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        NotifyIcon.Visibility = Visibility.Collapsed;
    }

    private void MenuItemShow_OnClick(object sender, RoutedEventArgs e)
    {
        ShowWindow();
    }

    private void MenuItemExit_OnClick(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void NotifyIcon_OnTrayMouseDoubleClick(object sender, RoutedEventArgs e)
    {
        ShowWindow();
    }

    private async Task CheckForUpdates()
    {
        try
        {
            // I will use a placeholder for the repository URL.
            // Replace "your-username/your-repo" with your actual GitHub repository.
            var source = new GithubSource("https://github.com/RaspizDIYs/LeagueAutoAccept", null, false);
            var manager = new UpdateManager(source);

            var newVersion = await manager.CheckForUpdatesAsync();
            if (newVersion == null)
            {
                // No updates available.
                return;
            }

            await manager.DownloadUpdatesAsync(newVersion);
            manager.ApplyUpdatesAndRestart(newVersion);
        }
        catch (Exception ex)
        {
            // Handle exceptions, e.g., log them or show a message to the user.
            Debug.WriteLine($"Error checking for updates: {ex.Message}");
        }
    }

    private void AutoAccept_StateChanged(object sender, RoutedEventArgs e)
    {
        if (AutoAcceptToggle.IsChecked == true)
        {
            Debug.WriteLine("Auto-accept toggled ON.");
            var credentials = LcuApi.GetCredentials();
            if (credentials.HasValue)
            {
                Debug.WriteLine("Credentials found, starting auto-accept task.");
                _autoAcceptCts = new CancellationTokenSource();
                var cancellationToken = _autoAcceptCts.Token;
                var (port, authToken) = credentials.Value;

                Task.Run(async () =>
                {
                    Debug.WriteLine("Auto-accept task started.");
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            var response = await LcuApi.Request("GET", "/lol-matchmaking/v1/ready-check", port, authToken);
                            if (!string.IsNullOrEmpty(response))
                            {
                                try
                                {
                                    var json = JObject.Parse(response);
                                    if (json["state"]?.ToString() == "InProgress")
                                    {
                                        Debug.WriteLine("Match ready check is 'InProgress'. Accepting match.");
                                        await LcuApi.Request("POST", "/lol-matchmaking/v1/ready-check/accept", port, authToken);
                                    }
                                }
                                catch (JsonReaderException)
                                {
                                    Debug.WriteLine("Received non-JSON response from ready-check endpoint.");
                                }
                            }
                        }
                        catch (Exception)
                        {
                            Debug.WriteLine("Exception in auto-accept task. Stopping task.");
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                Dispatcher.Invoke(() => AutoAcceptToggle.IsChecked = false);
                            }
                            break;
                        }

                        try
                        {
                            await Task.Delay(1000, cancellationToken);
                        }
                        catch (TaskCanceledException)
                        {
                            Debug.WriteLine("Task.Delay cancelled, exiting loop.");
                            break;
                        }
                    }
                    Debug.WriteLine("Auto-accept task finished.");
                }, cancellationToken);
            }
            else
            {
                Debug.WriteLine("Credentials not found. Toggling switch off.");
                System.Windows.MessageBox.Show("Не удалось найти клиент League of Legends. Убедитесь, что он запущен.");
                AutoAcceptToggle.IsChecked = false;
            }
        }
        else
        {
            Debug.WriteLine("Auto-accept toggled OFF. Cancelling task.");
            _autoAcceptCts?.Cancel();
        }
    }
}