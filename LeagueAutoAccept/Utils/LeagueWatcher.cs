using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace LeagueAutoAccept.Utils;

public class LeagueWatcher : IDisposable
{
    private CancellationTokenSource _cts = new();
    public event Action? LeagueStarted;

    public void Start()
    {
        Task.Run(async () =>
        {
            while (!_cts.IsCancellationRequested)
            {
                if (Process.GetProcessesByName("LeagueClientUx").Length > 0)
                {
                    LeagueStarted?.Invoke();
                    break;
                }
                await Task.Delay(3000, _cts.Token);
            }
        }, _cts.Token);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
} 