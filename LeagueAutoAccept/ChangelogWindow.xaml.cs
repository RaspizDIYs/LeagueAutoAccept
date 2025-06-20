using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;

namespace LeagueAutoAccept;

public partial class ChangelogWindow : Wpf.Ui.Controls.FluentWindow
{
    public ChangelogWindow()
    {
        InitializeComponent();
        _ = LoadReleases();
    }

    private async Task LoadReleases()
    {
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

                var tb = new TextBlock
                {
                    Text = $"{tag} - {name}\n{body}\n\n",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0,0,0,10)
                };
                ItemsPanel.Items.Add(tb);
            }
        }
        catch (Exception ex)
        {
            ItemsPanel.Items.Add(new TextBlock { Text = $"Ошибка загрузки: {ex.Message}", Foreground = System.Windows.Media.Brushes.Red });
        }
    }
} 