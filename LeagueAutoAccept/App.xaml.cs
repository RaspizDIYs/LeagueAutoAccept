using System.Configuration;
using System.Data;
using System.Windows;
using Velopack;

namespace LeagueAutoAccept;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        VelopackApp.Build().Run();
    }
}