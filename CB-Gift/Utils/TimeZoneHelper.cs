
namespace CB_Gift.Utils
{
    public static class TimeZoneHelper
    {
        private static readonly TimeZoneInfo VietnamTimeZone;

        static TimeZoneHelper()
        {
            // Tên chuẩn cho múi giờ Việt Nam (GMT+7)
            try
            {
                // Dùng "SE Asia Standard Time" cho môi trường Windows, hoặc "Asia/Ho_Chi_Minh"
                VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                // Fallback nếu tên chuẩn không tìm thấy
                VietnamTimeZone = TimeZoneInfo.CreateCustomTimeZone("GMT+7", TimeSpan.FromHours(7), "GMT+7", "GMT+7");
            }
        }

        // Hàm chuyển đổi từ UTC sang Giờ Việt Nam
        public static DateTime ConvertUtcToVietnamTime(DateTime utcDateTime)
        {
            // Đảm bảo loại Kind là UTC trước khi chuyển đổi
            if (utcDateTime.Kind == DateTimeKind.Unspecified || utcDateTime.Kind == DateTimeKind.Local)
            {
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            }

            if (utcDateTime.Kind == DateTimeKind.Utc)
            {
                return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, VietnamTimeZone);
            }

            return utcDateTime;
        }
    }
}