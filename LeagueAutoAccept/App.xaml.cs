using System.Configuration;
using System.Data;
using System.Threading;
using System.Windows;
using Velopack;
using Application = System.Windows.Application;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace LeagueAutoAccept;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private const string AppName = "LeagueAutoAccept";
    private static Mutex? _mutex;
    
    public App()
    {
    }
    
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            _mutex = new Mutex(true, AppName, out var createdNew);

            if (!createdNew)
            {
                ActivateOtherInstance();
                Current.Shutdown();
                return;
            }

            base.OnStartup(e);
            
            try
            {
                VelopackApp.Build().Run();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления: {ex.Message}", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при запуске: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.Dispose();
        base.OnExit(e);
    }

    private static void ActivateOtherInstance()
    {
        try
        {
            var current = Process.GetCurrentProcess();
            var others = Process.GetProcessesByName(current.ProcessName).Where(p => p.Id != current.Id);
            foreach (var p in others)
            {
                var handle = p.MainWindowHandle;
                if (handle != IntPtr.Zero)
                {
                    ShowWindowAsync(handle, SW_RESTORE);
                    SetForegroundWindow(handle);
                    break;
                }
            }
        }
        catch
        {
            // ignore
        }
    }

    private const int SW_RESTORE = 9;
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
}