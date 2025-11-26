namespace CB_Gift.DTOs.Reports
{
    // 1. DTO Tổng
    public class OperationsDashboardDto
    {
        public OperationsKpiDto Kpis { get; set; }
        public List<OrderStatusChartDto> StatusDistribution { get; set; }
        public List<IncomingOutgoingDto> IncomingOutgoing { get; set; }
        public List<IssueBreakdownDto> IssueBreakdown { get; set; }
        public List<CriticalOrderDto> CriticalOrders { get; set; }
    }

    // 2. KPIs
    public class OperationsKpiDto
    {
        public int TotalOrders { get; set; }
        public double ProductionVelocity { get; set; } // Orders/day
        public int Backlog { get; set; } // WIP (Work In Progress)
        public double AvgFulfillmentTime { get; set; } // Days
    }

    // 3. Order Status Chart (Bar Chart)
    public class OrderStatusChartDto
    {
        public string Status { get; set; }
        public int Count { get; set; }
    }

    // 4. Incoming vs Outgoing (Line Chart)
    public class IncomingOutgoingDto
    {
        public string Date { get; set; }
        public int Incoming { get; set; } // New Orders
        public int Outgoing { get; set; } // Shipped Orders
    }

    // 5. Issue Breakdown (Pie Chart)
    public class IssueBreakdownDto
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }

    // 6. Critical Alerts (Table)
    public class CriticalOrderDto
    {
        public string Id { get; set; } // Order Code
        public string Status { get; set; }
        public int DaysInStatus { get; set; }
        public string Seller { get; set; }
    }
}
