namespace CB_Gift.DTOs
{
    public class PrintLinkRequest
    {
        public List<string> OrderCodes { get; set; }
        public string Size { get; set; } = "A5";
    }
}
