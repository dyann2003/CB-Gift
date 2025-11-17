using FluentValidation;
using System.Globalization;
namespace CB_Gift.Orders.Import
{
    public class OrderImportRowValidator : AbstractValidator<OrderImportRowDto>
    {
        private readonly ReferenceDataCache _cache;

        public OrderImportRowValidator(ReferenceDataCache cache)
        {
            _cache = cache;
            RuleFor(x => x.OrderID).NotEmpty().WithMessage("OrderID không được để trống.");
            // Required fields
            RuleFor(x => x.OrderCode).NotEmpty().WithMessage("OrderCode không được để trống.");
            RuleFor(x => x.Phone).NotEmpty().WithMessage("Phone không được để trống.");
            RuleFor(x => x.Address).NotEmpty().WithMessage("Address không được để trống.");

            RuleFor(x => x.Email)
                .EmailAddress()
                .When(x => !string.IsNullOrWhiteSpace(x.Email))
                .WithMessage("Email không hợp lệ.");

            // ShipCountry = Việt Nam
            RuleFor(x => x.ShipCountry)
                .NotEmpty()
                .Must(BeVietnam)
                .WithMessage("ShipCountry phải là Việt Nam (Vietnam / Viet Nam / Việt Nam / VN).");

            // Tỉnh/Thành Việt Nam
            RuleFor(x => x.ShipState)
                .NotEmpty()
                .Must(BeValidProvince)
                .WithMessage("ShipState phải là tên một tỉnh/thành hợp lệ của Việt Nam.");

            // City – chỉ yêu cầu không rỗng
            RuleFor(x => x.ShipCity)
                .NotEmpty()
                .WithMessage("ShipCity không được để trống.");

            // Zipcode
            RuleFor(x => x.Zipcode)
                .NotEmpty()
                .Matches(@"^\d{5}$").WithMessage("Zipcode phải gồm 5 chữ số.");

            // Payment Status
            RuleFor(x => x.PaymentStatus)
                .Must(BeValidPaymentStatus)
                .When(x => !string.IsNullOrWhiteSpace(x.PaymentStatus))
                .WithMessage("PaymentStatus không hợp lệ. (Pending, Paid, Refunded, Cancelled, Unpaid)");

            // ProductName
            RuleFor(x => x.ProductName)
                .NotEmpty()
                .Must(BeExistingProduct)
                .WithMessage("ProductName không tồn tại trong hệ thống.");

            // ProductVariant phải khớp ProductName + SizeInch
            RuleFor(x => x)
                .Must(HaveValidProductVariant)
                .WithMessage("Không tìm thấy ProductVariant phù hợp với ProductName + SizeInch.");

            // TotalCost
            RuleFor(x => x.TotalCost)
                .GreaterThanOrEqualTo(0)
                .When(x => x.TotalCost.HasValue);

            // TotalAmount
            RuleFor(x => x.TotalAmount)
                .GreaterThanOrEqualTo(0)
                .When(x => x.TotalAmount.HasValue);
        }

        private bool BeVietnam(string shipCountry)
        {
            if (string.IsNullOrWhiteSpace(shipCountry)) return false;

            var normalized = shipCountry.Trim().ToLower();

            return normalized == "vietnam"
                || normalized == "viet nam"
                || normalized == "việt nam"
                || normalized == "vn";
        }

        private bool BeValidProvince(string provinceName)
        {
            if (string.IsNullOrWhiteSpace(provinceName))
                return false;

            var province = provinceName.Trim();

            return _cache.Provinces.Any(p =>
                string.Equals(p, province, StringComparison.OrdinalIgnoreCase));
        }

        private bool BeValidCityInProvince(OrderImportRowDto dto)
        {
            // Bạn chưa có danh sách quận/huyện → chỉ check khác rỗng
            return !string.IsNullOrWhiteSpace(dto.ShipCity);
        }

        private bool BeValidPaymentStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return true;

            var normalized = status.Trim().ToLower();

            return normalized is "pending" or "paid"
                or "refunded" or "cancelled" or "unpaid";
        }

        private bool BeExistingProduct(string? productName)
        {
            if (string.IsNullOrWhiteSpace(productName))
                return false;

            return _cache.Products.Any(p =>
                string.Equals(p.ProductName, productName.Trim(),
                    StringComparison.OrdinalIgnoreCase));
        }

        // helper chuẩn hoá size: bỏ "IN", "inch", đổi về số 0.## (5.90 -> "5.9", 5.9IN -> "5.9")
        private string NormalizeSize(string? size)
        {
            if (string.IsNullOrWhiteSpace(size))
                return string.Empty;

            var s = size.Trim().ToLower();

            // bỏ chữ "inch", "in" nếu có
            s = s.Replace("inch", "").Replace("in", "").Trim();

            // parse số: xử lý 4, 4.0, 4.00, 4,0 ...
            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
            {
                return val.ToString("0.##", CultureInfo.InvariantCulture); // 4.00 -> "4", 5.90 -> "5.9"
            }

            return s;
        }
        private bool HaveValidProductVariant(OrderImportRowDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ProductName) ||
                string.IsNullOrWhiteSpace(dto.SizeInch))
                return false;

            // tìm product theo tên
            var product = _cache.Products.FirstOrDefault(p =>
                p.ProductName.Equals(dto.ProductName.Trim(), StringComparison.OrdinalIgnoreCase));

            if (product == null) return false;

            var dtoSize = NormalizeSize(dto.SizeInch);

            // so sánh size đã chuẩn hoá
            return _cache.ProductVariants.Any(v =>
                v.ProductId == product.ProductId &&
                NormalizeSize(v.SizeInch) == dtoSize);
        }

    }
}

