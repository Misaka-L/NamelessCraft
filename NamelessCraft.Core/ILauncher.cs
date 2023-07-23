using System.Diagnostics;

namespace NamelessCraft.Core;

public interface ILauncher
{
    public Task StartAsync();
    public Task<string> GetLaunchCommandLineAsync();

    public event DataReceivedEventHandler? GameOutputDataReceived;
    public event EventHandler<EventArgs>? GameExited;
}