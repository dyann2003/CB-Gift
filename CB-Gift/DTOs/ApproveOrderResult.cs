namespace CB_Gift.DTOs
{
    public class ApproveOrderResult
    {
        public bool IsSuccess { get; set; }
        public bool OrderFound { get; set; } = true;
        public bool CanApprove { get; set; } = true;
        public string ErrorMessage { get; set; }
    }
}
