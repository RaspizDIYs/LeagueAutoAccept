using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Documents;
using Newtonsoft.Json.Linq;
using Velopack;
using Velopack.Sources;

namespace LeagueAutoAccept;

public partial class SettingsWindow : Wpf.Ui.Controls.FluentWindow
{
    private bool _changelogLoaded;

    public SettingsWindow()
    {
        InitializeComponent();
        var ver = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = $"Версия: {ver?.ToString(3)}";

        PreviewMouseDown += SettingsWindow_PreviewMouseDown;
    }

    private async void CheckUpdatesButton_OnClick(object sender, RoutedEventArgs e) => await CheckForUpdates();

    private async Task CheckForUpdates()
    {
        try
        {
            var source = new GithubSource("https://github.com/RaspizDIYs/LeagueAutoAccept", null, false);
            var manager = new UpdateManager(source);

            var newVersion = await manager.CheckForUpdatesAsync();
            if (newVersion == null)
            {
                MessageBox.Show("Обновлений нет", "Проверка обновлений", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            await manager.DownloadUpdatesAsync(newVersion);
            manager.ApplyUpdatesAndRestart(newVersion);
        }
        catch (InvalidOperationException)
        {
            MessageBox.Show("Автообновление доступно только после установки через setup.exe", "Не установлено", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка обновления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void VersionText_OnClick(object sender, MouseButtonEventArgs e)
    {
        ShowChangelog();
    }

    private async void ShowChangelog()
    {
        MainPanel.Visibility = Visibility.Collapsed;
        ChangelogPanel.Visibility = Visibility.Visible;

        if (_changelogLoaded) return;
        _changelogLoaded = true;

        try
        {
            var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("LeagueAutoAccept");
            var json = await http.GetStringAsync("https://api.github.com/repos/RaspizDIYs/LeagueAutoAccept/releases");
            var arr = JArray.Parse(json);
            foreach (var rel in arr)
            {
                var tag = rel["tag_name"]?.ToString();
                var name = rel["name"]?.ToString();
                var body = rel["body"]?.ToString();

                if (body != null)
                {
                    body = body.Replace("<br>", "\n").Replace("<br/>", "\n").Replace("<br />", "\n");
                }

                var tb = new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0,0,0,2)
                };

                if (!string.IsNullOrEmpty(tag))
                {
                    tb.Inlines.Add(new Run(tag) { FontWeight = FontWeights.Bold, FontSize = 14 });
                }
                if (!string.IsNullOrEmpty(name))
                {
                    tb.Inlines.Add(new Run(" " + name) { FontWeight = FontWeights.SemiBold, FontSize = 14 });
                }
                tb.Inlines.Add(new LineBreak());
                if (!string.IsNullOrEmpty(body))
                {
                    tb.Inlines.Add(new Run(body));
                }
                tb.Inlines.Add(new LineBreak());

                ChangelogItems.Items.Add(tb);
            }
        }
        catch (Exception ex)
        {
            ChangelogItems.Items.Add(new TextBlock { Text = $"Ошибка загрузки: {ex.Message}", Foreground = System.Windows.Media.Brushes.Red });
        }
    }

    private void CloseChangelogButton_OnClick(object sender, RoutedEventArgs e) => HideChangelog();

    private void HideChangelog()
    {
        ChangelogPanel.Visibility = Visibility.Collapsed;
        MainPanel.Visibility = Visibility.Visible;
    }

    private void SettingsWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.XButton1 && ChangelogPanel.Visibility == Visibility.Visible)
        {
            HideChangelog();
            e.Handled = true;
        }
        else if (e.ChangedButton == MouseButton.XButton1)
        {
            Close();
            e.Handled = true;
        }
    }
} 