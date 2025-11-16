using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CB_Gift.DTOs
{
    // Cấu trúc chung cho mọi response từ GHN
    public class GhnBaseResponse<T>
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }
        [JsonPropertyName("message")]
        public string Message { get; set; }
        [JsonPropertyName("data")]
        public T Data { get; set; }
    }

    // --- Dữ liệu React gửi lên ---
    public class CreateOrderRequest
    {

        // =======================
        // NGƯỜI NHẬN (TO)
        // =======================

        [Required(ErrorMessage = "Tên người nhận là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự")]
        public string ToName { get; set; }

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [RegularExpression(@"(0[3|5|7|8|9])+([0-9]{8})\b",
            ErrorMessage = "Số điện thoại không đúng định dạng (10 số)")]
        public string ToPhone { get; set; }

        [Required(ErrorMessage = "Địa chỉ người nhận là bắt buộc")]
        [StringLength(200, ErrorMessage = "Địa chỉ không được vượt quá 200 ký tự")]
        public string ToAddress { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Phường/Xã")]
        public string ToWardCode { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn Quận/Huyện")]
        public int ToDistrictId { get; set; }

        // =======================
        // KIỆN HÀNG
        // =======================

        [Range(1, 50000, ErrorMessage = "Cân nặng phải lớn hơn 0 và nhỏ hơn 50.000 gram")]
        public int WeightInGrams { get; set; }

        [Range(1, 200, ErrorMessage = "Chiều dài phải lớn hơn 0 (cm)")]
        public int Length { get; set; }

        [Range(1, 200, ErrorMessage = "Chiều rộng phải lớn hơn 0 (cm)")]
        public int Width { get; set; }

        [Range(1, 200, ErrorMessage = "Chiều cao phải lớn hơn 0 (cm)")]
        public int Height { get; set; }

        // ======== DANH SÁCH SẢN PHẨM ========
        [Required(ErrorMessage = "Danh sách sản phẩm là bắt buộc")]
        public List<OrderItemRequest> Items { get; set; }

    }
    public class OrderItemRequest
    {
        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        public string Name { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải >= 1")]
        public int Quantity { get; set; }

        [Range(1, 50000, ErrorMessage = "Cân nặng phải lớn hơn 0 và nhỏ hơn 50.000 gram")]
        public int Weight { get; set; }
    }

    // --- Dữ liệu .NET trả về cho React ---
    public class CreateOrderResult
    {
        public string OrderCode { get; set; }
        public long TotalFee { get; set; }
    }


    public class GhnTrackOrderPayload
    {
        [JsonPropertyName("order_code")]
        public string OrderCode { get; set; }
    }

    // --- Dữ liệu GHN trả về (Responses) ---
    public class GhnCreateOrderData
    {
        [JsonPropertyName("order_code")]
        public string OrderCode { get; set; }
        [JsonPropertyName("total_fee")]
        public long TotalFee { get; set; }
    }

    //public class GhnTrackData
    //{
    //    [JsonPropertyName("order_code")]
    //    public string OrderCode { get; set; }

    //    [JsonPropertyName("status")]
    //    public string Status { get; set; }

    //    [JsonPropertyName("order_date")]
    //    public string OrderDate { get; set; }

    //    [JsonPropertyName("pickup_time")]
    //    public string PickupTime { get; set; }

    //    [JsonPropertyName("leadtime")]
    //    public string Leadtime { get; set; }

    //    [JsonPropertyName("to_name")]
    //    public string ToName { get; set; }

    //    [JsonPropertyName("to_phone")]
    //    public string ToPhone { get; set; }

    //    [JsonPropertyName("to_address")]
    //    public string ToAddress { get; set; }

    //    [JsonPropertyName("required_note")]
    //    public string RequiredNote { get; set; }

    //    [JsonPropertyName("converted_weight")]
    //    public int Weight { get; set; }

    //    [JsonPropertyName("log")]
    //    public List<GhnLog> Log { get; set; } = new();
    //}

    public class TrackingResult
    {
        [JsonPropertyName("order_code")]
        public string OrderCode { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("order_date")]
        public string OrderDate { get; set; }

        [JsonPropertyName("pickup_time")]
        public string PickupTime { get; set; }

        [JsonPropertyName("leadtime")]
        public string Leadtime { get; set; }

        [JsonPropertyName("to_name")]
        public string ToName { get; set; }

        [JsonPropertyName("to_phone")]
        public string ToPhone { get; set; }

        [JsonPropertyName("to_address")]
        public string ToAddress { get; set; }

        [JsonPropertyName("required_note")]
        public string RequiredNote { get; set; }

        [JsonPropertyName("weight")]
        public int Weight { get; set; }

        [JsonPropertyName("items")]
        public List<GhnItem> Items { get; set; } = new();

        [JsonPropertyName("log")]
        public List<GhnLog> Log { get; set; } = new();
    }

    public class GhnLog
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("updated_date")]
        public string UpdatedDate { get; set; }
    }

    public class GhnItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
    }
}
