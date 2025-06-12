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
        VelopackApp.Build().Run();
    }
    
    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, AppName, out var createdNew);

        if (!createdNew)
        {
            Current.Shutdown();
        }

        base.OnStartup(e);
    }
}