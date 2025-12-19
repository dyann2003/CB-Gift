namespace CB_Gift.DTOs
{
    public class UpdateShippingStatusDto
    {
        public string OrderCode { get; set; }
        public string NewStatus { get; set; }
        public string? Reason { get; set; }
    }
}
