using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Wpf.Ui.Controls;

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
        };
    }

    private void AutoAcceptToggle_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch toggleSwitch) return;

        if (toggleSwitch.IsChecked == true)
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
                                Dispatcher.Invoke(() => toggleSwitch.IsChecked = false);
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
                toggleSwitch.IsChecked = false;
            }
        }
        else
        {
            Debug.WriteLine("Auto-accept toggled OFF. Cancelling task.");
            _autoAcceptCts?.Cancel();
        }
    }
}