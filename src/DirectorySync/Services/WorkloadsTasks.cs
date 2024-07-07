namespace DirectorySync.Services;

internal class WorkloadsTasks
{
    private readonly HashSet<Workload> _workloads = new();
    private readonly object _locker = new();

    public void Add(Workload workload)
    {
        if (workload == Workload.None)
        {
            return;
        }
        
        lock (_locker)
        {
            _workloads.Add(workload);
        }
    }

    public bool IsBusy()
    {
        lock (_locker)
        {
            return _workloads.Count != 0;
        }
    }

    public void Complete(Workload workload)
    {
        if (workload == Workload.None)
        {
            return;
        }
        
        lock (_locker)
        {
            _workloads.Remove(workload);
        }
    }
}
