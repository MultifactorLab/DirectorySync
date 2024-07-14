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

    public void Place(Order order)
    {
        if (order == Order.Empty)
        {
            return;
        }
        
        lock (_locker)
        {
            if (_orders.Add(new OrderDescription(DateTime.Now, order)))
            {
                _logger.LogDebug("Order '{Order}' was placed. All orders: {Orders:l}", 
                    order,
                    string.Join(", ", _orders.Select(x => $"{{{x.Order}, {x.Placed:hh:mm:ss}}}")));
            }
            else
            {
                _logger.LogDebug("Order '{Order}' already exists", order);
            }
        }
    }

    public Order Take()
    {
        lock (_locker)
        {
            if (_orders.Count == 0)
            {
                return Order.Empty;
            }

            var firstPlaced = _orders.OrderBy(x => x.Placed).First();
            return firstPlaced.Order;
        }
    }

    public void Done(Order order)
    {
        if (order == Order.Empty)
        {
            return;
        }
        
        lock (_locker)
        {
            _orders.RemoveWhere(x => x.Order == order);
            _logger.LogDebug("Order '{Order}' is completed and was removed. All orders: {Orders:l}", 
                order,
                string.Join(", ", _orders.Select(x => $"{{{x.Order}, {x.Placed:hh:mm:ss}}}")));
        }
    }

    private record OrderDescription(DateTime Placed, Order Order);

    private class OrderDescriptionEqualityComparer : IEqualityComparer<OrderDescription>
    {
        public bool Equals(OrderDescription? x, OrderDescription? y)
        {
            if (x == null || y == null)
            {
                return false;
            }

            return x.Order == y.Order;
        }

        public int GetHashCode(OrderDescription obj)
        {
            return obj.Order.GetHashCode();
        }
    }
}
