using System.Configuration;
using System.Data;
using System.Threading;
using System.Windows;
using Velopack;
using Application = System.Windows.Application;

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
                MessageBox.Show("Приложение уже запущено", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
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
}