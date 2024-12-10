namespace DirectorySync.Services;

internal class OrderBoard
{
    private readonly ILogger<OrderBoard> _logger;
    private readonly HashSet<OrderDescription> _orders = new(new OrderDescriptionEqualityComparer());
    private readonly object _locker = new();

    public OrderBoard(ILogger<OrderBoard> logger)
    {
        _logger = logger;
    }

    public void Place(WorkloadKind workload)
    {
        if (workload == WorkloadKind.Empty)
        {
            return;
        }
        
        lock (_locker)
        {
            if (_orders.Add(new OrderDescription(DateTime.Now, workload)))
            {
                _logger.LogDebug("Workload '{Workload}' was placed. All workloads: {Workloads:l}", 
                    workload,
                    string.Join(", ", _orders.Select(x => x.Workload)));
            }
        }
    }

    public WorkloadKind Take()
    {
        lock (_locker)
        {
            if (_orders.Count == 0)
            {
                return WorkloadKind.Empty;
            }

            var firstPlaced = _orders.OrderBy(x => x.Placed).First();
            return firstPlaced.Workload;
        }
    }

    public void Done(WorkloadKind workload)
    {
        if (workload == WorkloadKind.Empty)
        {
            return;
        }
        
        lock (_locker)
        {
            _orders.RemoveWhere(x => x.Workload == workload);
        }
    }

    private record OrderDescription(DateTime Placed, WorkloadKind Workload);

    private class OrderDescriptionEqualityComparer : IEqualityComparer<OrderDescription>
    {
        public bool Equals(OrderDescription? x, OrderDescription? y)
        {
            if (x == null || y == null)
            {
                return false;
            }

            return x.Workload == y.Workload;
        }

        public int GetHashCode(OrderDescription obj)
        {
            return obj.Workload.GetHashCode();
        }
    }
}
