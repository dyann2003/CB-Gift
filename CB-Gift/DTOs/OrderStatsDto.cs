namespace CB_Gift.DTOs
{
    public class OrderStatsDto
    {
        public int Total { get; set; }
        public Dictionary<string, int> StatusCounts { get; set; } = new();

        public int NeedActionCount { get; set; }
        public int UrgentCount { get; set; }
        public int CompletedCount { get; set; }

        public Dictionary<string, List<StatusCountItem>> StageGroups { get; set; } = new();

        public class StatusCountItem
        {
            public string Status { get; set; } = "";
            public int Count { get; set; }
        }

    }
}
