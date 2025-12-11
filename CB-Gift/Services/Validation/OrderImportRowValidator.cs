using FluentValidation;
using System;
using System.Linq;

namespace CB_Gift.Orders.Import
{
    public class OrderImportRowValidator : AbstractValidator<OrderImportRowDto>
    {
        private readonly ReferenceDataCache _cache;

        public OrderImportRowValidator(ReferenceDataCache cache)
        {
            _cache = cache;

            // 1. Thông tin đơn hàng cơ bản
            RuleFor(x => x.OrderCode)
                .NotEmpty().WithMessage("Mã đơn hàng (OrderCode) là bắt buộc.")
                // Check trùng bằng Cache (Siêu nhanh)
                .Must(BeUniqueOrderCode).WithMessage("Mã đơn hàng '{PropertyValue}' đã tồn tại trong hệ thống.");

            // 2. Thông tin khách hàng
            RuleFor(x => x.CustomerName)
                .NotEmpty().WithMessage("Tên khách hàng là bắt buộc.")
                .Length(3, 50).WithMessage("Tên khách hàng phải từ 3 đến 50 ký tự.")
                // \p{L} match tất cả chữ cái Unicode (bao gồm tiếng Việt), \s là khoảng trắng
                .Matches(@"^[\p{L}\s]+$").WithMessage("Tên khách hàng không được chứa ký tự đặc biệt hoặc số.");

            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage("Số điện thoại là bắt buộc.")
                // Regex: Bắt đầu bằng 0, ký tự thứ 2 là 3,5,7,8,9, sau đó là 8 số bất kỳ (0-9). Tổng 10 số.
                .Matches(@"^0[35789][0-9]{8}$").WithMessage("SĐT phải là số Việt Nam 10 số (VD: 09xx, 03xx...).");

            RuleFor(x => x.Email)
                  .NotEmpty().WithMessage("Email là bắt buộc.")
                  // Giải thích Regex:
                  // ^[^@\s]+  : Bắt đầu bằng chuỗi không chứa @ và khoảng trắng (Tên email)
                  // @         : Bắt buộc có @
                  // [^@\s]+   : Phần domain không chứa @ và khoảng trắng
                  // \.        : Bắt buộc có dấu chấm
                  // [^@\s]+   : Phần đuôi tên miền (com, vn...)
                  .Matches(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")
                  .WithMessage("Email không hợp lệ (phải có dạng abc@domain.com).");

            // 3. Địa chỉ (Logic mới: Province -> District -> Ward)
            RuleFor(x => x.Address)
                .NotEmpty().WithMessage("Địa chỉ chi tiết (Address) là bắt buộc.");

            RuleFor(x => x.Province)
                .NotEmpty().WithMessage("Tỉnh/Thành phố là bắt buộc.")
                .Must(BeValidProvince).WithMessage("Tỉnh/Thành phố không nằm trong danh sách hỗ trợ.");

            RuleFor(x => x.District)
                .NotEmpty().WithMessage("Quận/Huyện là bắt buộc.");

            RuleFor(x => x.Ward)
                .NotEmpty().WithMessage("Phường/Xã là bắt buộc.");

            // 4. Sản phẩm (Quan trọng: Check theo SKU)
            RuleFor(x => x.SKU)
                .Cascade(CascadeMode.Stop) // Nếu rỗng thì dừng, không check DB
                .NotEmpty().WithMessage("SKU là bắt buộc.")
                .Must(BeExistingSku).WithMessage("SKU '{PropertyValue}' không tồn tại trong hệ thống.");

            RuleFor(x => x.Quantity)
                .NotNull().WithMessage("Số lượng là bắt buộc.")
                .InclusiveBetween(1, 20).WithMessage("Số lượng sản phẩm phải từ 1 đến 20.");
        }

        // --- Helper Functions ---

        /// <summary>
        /// Kiểm tra Tên tỉnh có nằm trong Cache không (So sánh không phân biệt hoa thường)
        /// </summary>
        private bool BeValidProvince(string? provinceName)
        {
            if (string.IsNullOrWhiteSpace(provinceName)) return false;

            // Gọi hàm thông minh trong Cache (đã bao gồm logic xử lý "TP.", "Tỉnh"...)
            var provinceId = _cache.FindProvinceId(provinceName);

            return provinceId.HasValue; // Có ID => Hợp lệ
        }

        /// <summary>
        /// Kiểm tra SKU có tồn tại trong ProductVariants Cache không
        /// </summary>
        private bool BeExistingSku(string? sku)
        {
            if (string.IsNullOrWhiteSpace(sku)) return false;

            var inputSku = sku.Trim();

            return _cache.ProductVariants.Any(v =>
                !string.IsNullOrEmpty(v.Sku) &&
                v.Sku.Equals(inputSku, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Kiểm tra trạng thái thanh toán
        /// </summary>
        private bool BeValidPaymentStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return true; // Cho phép rỗng (sẽ default là Pending ở Factory)

            var s = status.Trim().ToLower();
            return s is "pending" or "paid" or "completed"
                     or "unpaid" or "refunded" or "cancelled";
        }
        private bool BeUniqueOrderCode(string? orderCode)
        {
            if (string.IsNullOrWhiteSpace(orderCode)) return true;
            // Check trong HashSet đã load sẵn
            return !_cache.ExistingOrderCodes.Contains(orderCode.Trim());
        }
    }
}