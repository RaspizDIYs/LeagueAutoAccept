using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using Velopack;
using Velopack.Sources;
using Wpf.Ui.Controls;
using Application = System.Windows.Application;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Hardcodet.Wpf.TaskbarNotification;
using LeagueAutoAccept.Services;

namespace LeagueAutoAccept;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : FluentWindow, INotifyPropertyChanged
{
    private readonly AutoAcceptService _autoAcceptService;
    private bool _isAutoAcceptEnabled;
    public bool IsAutoAcceptEnabled
    {
        get => _isAutoAcceptEnabled;
        set
        {
            Debug.WriteLine($"[IsAutoAcceptEnabled] Current: {_isAutoAcceptEnabled}, New: {value}");
            if (_isAutoAcceptEnabled == value) return;
            _isAutoAcceptEnabled = value;
            OnPropertyChanged();
            
            if (_isAutoAcceptEnabled)
            {
                Debug.WriteLine("[IsAutoAcceptEnabled] Starting auto accept service");
                _autoAcceptService.StartAutoAccept();
            }
            else
            {
                Debug.WriteLine("[IsAutoAcceptEnabled] Stopping auto accept service");
                _autoAcceptService.StopAutoAccept();
            }
        }
    }

    public MainWindow()
    {
        InitializeComponent();
        Debug.WriteLine("MainWindow initialized.");
        
        Closing += (_, _) =>
        {
            Debug.WriteLine("MainWindow closing.");
            NotifyIcon.Dispose();
        };

        DataContext = this;
        _autoAcceptService = new AutoAcceptService();
        _ = CheckForUpdates();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await CheckForUpdates();
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
            var source = new GithubSource("https://github.com/RaspizDIYs/LeagueAutoAccept", null, false);
            var manager = new UpdateManager(source);

            var newVersion = await manager.CheckForUpdatesAsync();
            if (newVersion == null)
            {
                return;
            }

            await manager.DownloadUpdatesAsync(newVersion);
            manager.ApplyUpdatesAndRestart(newVersion);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error checking for updates: {ex.Message}");
        }
    }

    private void ToggleSwitch_OnToggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggleSwitch)
        {
            IsAutoAcceptEnabled = toggleSwitch.IsChecked ?? false;
        }
    }

    private void TrayMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem)
        {
            IsAutoAcceptEnabled = menuItem.IsChecked;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}