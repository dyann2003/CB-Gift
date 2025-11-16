using System.Text.Json.Serialization;

namespace CB_Gift.DTOs
{
    public class GhnProvince
    {
        [JsonPropertyName("ProvinceID")]
        public int ProvinceID { get; set; }
        [JsonPropertyName("ProvinceName")]
        public string ProvinceName { get; set; }
    }

    public class GhnDistrict
    {
        [JsonPropertyName("DistrictID")]
        public int DistrictID { get; set; }
        [JsonPropertyName("DistrictName")]
        public string DistrictName { get; set; }
    }

    public class GhnWard
    {
        [JsonPropertyName("WardCode")]
        public string WardCode { get; set; }
        [JsonPropertyName("WardName")]
        public string WardName { get; set; }
    }
}