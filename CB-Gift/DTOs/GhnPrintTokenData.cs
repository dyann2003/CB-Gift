using System.Text.Json.Serialization;

namespace CB_Gift.DTOs
{
    public class GhnPrintTokenData
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }
    }
}
