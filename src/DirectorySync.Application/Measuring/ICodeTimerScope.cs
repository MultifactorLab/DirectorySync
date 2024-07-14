namespace DirectorySync.Application.Measuring;

public interface ICodeTimerScope : IDisposable
{
    void Stop();
}
