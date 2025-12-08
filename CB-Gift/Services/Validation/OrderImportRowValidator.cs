using FluentValidation;
using System;
using System.Linq;

namespace CB_Gift.Orders.Import
{
    public class OrderImportRowValidator : AbstractValidator<OrderImportRowDto>
    {
        private readonly ReferenceDataCache _cache;

        public  OrderImportRowValidator(ReferenceDataCache cache)
        {
            _cache = cache;

            // 1. Thông tin đơn hàng cơ bản
            RuleFor(x => x.OrderCode)
                .NotEmpty().WithMessage("Mã đơn hàng (OrderCode) là bắt buộc.");

            // 2. Thông tin khách hàng
            RuleFor(x => x.CustomerName)
                .NotEmpty().WithMessage("Tên khách hàng là bắt buộc.");

            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage("Số điện thoại là bắt buộc.")
                .MinimumLength(8).WithMessage("Số điện thoại không hợp lệ (quá ngắn).");

            RuleFor(x => x.Email)
                .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
                .WithMessage("Định dạng Email không hợp lệ.");

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
                .GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0.");
           
        }

        // --- Helper Functions ---

        /// <summary>
        /// Kiểm tra Tên tỉnh có nằm trong Cache không (So sánh không phân biệt hoa thường)
        /// </summary>
        private bool BeValidProvince(string? provinceName)
        {
            if (string.IsNullOrWhiteSpace(provinceName)) return false;

            var input = provinceName.Trim();

            return _cache.Provinces.Any(p =>
                string.Equals(p, input, StringComparison.OrdinalIgnoreCase));
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
    }
}