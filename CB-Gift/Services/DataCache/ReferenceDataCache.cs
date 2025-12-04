using CB_Gift.Data;
using CB_Gift.Models;
using Microsoft.EntityFrameworkCore;

public class ReferenceDataCache
{
    private readonly CBGiftDbContext _db;

    public ReferenceDataCache(CBGiftDbContext db)
    {
        _db = db;
    }

    // Lấy từ DB
    public List<Product> Products { get; private set; } = new();
    public List<ProductVariant> ProductVariants { get; private set; } = new();

    // Dùng list in-memory, KHÔNG lấy từ DB
    public List<string> Provinces { get; } = new()
    {
        "Hà Nội", "Hồ Chí Minh", "Đà Nẵng", "Hải Phòng", "Cần Thơ",
        "An Giang", "Bà Rịa - Vũng Tàu", "Bắc Giang", "Bắc Kạn", "Bạc Liêu",
        "Bắc Ninh", "Bến Tre", "Bình Định", "Bình Dương", "Bình Phước",
        "Bình Thuận", "Cà Mau", "Cao Bằng", "Đắk Lắk", "Đắk Nông",
        "Điện Biên", "Đồng Nai", "Đồng Tháp", "Gia Lai", "Hà Giang",
        "Hà Nam", "Hà Tĩnh", "Hải Dương", "Hậu Giang", "Hòa Bình",
        "Hưng Yên", "Khánh Hòa", "Kiên Giang", "Kon Tum", "Lai Châu",
        "Lâm Đồng", "Lạng Sơn", "Lào Cai", "Long An", "Nam Định",
        "Nghệ An", "Ninh Bình", "Ninh Thuận", "Phú Thọ", "Phú Yên",
        "Quảng Bình", "Quảng Nam", "Quảng Ngãi", "Quảng Ninh", "Quảng Trị",
        "Sóc Trăng", "Sơn La", "Tây Ninh", "Thái Bình", "Thái Nguyên",
        "Thanh Hóa", "Thừa Thiên Huế", "Tiền Giang", "Trà Vinh",
        "Tuyên Quang", "Vĩnh Long", "Vĩnh Phúc", "Yên Bái"
    };

    // Có thể để trống hoặc tự build sau
    public List<string> Cities { get; } = new();

    // Zipcode Việt Nam (ví dụ vài mã, hoặc để trống tùy bạn)
    public List<string> PostalCodes { get; } = new()
    {
        "10000", "70000", "55000", "40000" // ví dụ
    };

    public async Task LoadAsync()
    {
        Products = await _db.Products.ToListAsync();
        ProductVariants = await _db.ProductVariants.ToListAsync();

        // KHÔNG gọi _db.Provinces / _db.Cities / _db.VietNamPostalCodes nữa
    }
}
