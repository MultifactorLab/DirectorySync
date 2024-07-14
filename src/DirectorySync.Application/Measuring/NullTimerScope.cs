namespace DirectorySync.Application.Measuring;

internal sealed class NullTimerScope : ICodeTimerScope
{
    public void Stop() { }
    public void Dispose() { }
}
