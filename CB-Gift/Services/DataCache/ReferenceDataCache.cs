using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Models;
using CB_Gift.Services.IService;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

public class ReferenceDataCache
{
    private readonly CBGiftDbContext _db;
    private readonly ILocationService _locationService;

    // Cache tạm trong RAM (chỉ sống trong phạm vi 1 request Import)
    private List<GhnProvince> _ghnProvinces = new();
    private Dictionary<int, List<GhnDistrict>> _ghnDistricts = new(); // Key: ProvinceId
    private Dictionary<int, List<GhnWard>> _ghnWards = new();         // Key: DistrictId

    public ReferenceDataCache(CBGiftDbContext db, ILocationService locationService)
    {
        _db = db;
        _locationService = locationService;
    }

    public List<Product> Products { get; private set; } = new();
    public List<ProductVariant> ProductVariants { get; private set; } = new();

    public async Task LoadAsync()
    {
        // 1. Load Data từ DB local
        Products = await _db.Products.ToListAsync();
        ProductVariants = await _db.ProductVariants.ToListAsync();

        // 2. Load Provinces từ GHN (Load ngay từ đầu vì list này nhẹ)
        if (!_ghnProvinces.Any())
        {
            var provinces = await _locationService.GetProvincesAsync();
            _ghnProvinces = provinces.ToList();
        }
    }

    // --- CÁC HÀM TÌM KIẾM ID TỪ TÊN (MAPPING LOGIC) ---

    // 1. Tìm Province ID
    public int? FindProvinceId(string provinceName)
    {
        if (string.IsNullOrWhiteSpace(provinceName)) return null;
        var match = _ghnProvinces.FirstOrDefault(p => IsLocationMatch(p.ProvinceName, provinceName));
        return match?.ProvinceID;
    }

    // 2. Tìm District ID (Cần ProvinceId cha)
    public async Task<int?> FindDistrictIdAsync(int provinceId, string districtName)
    {
        if (string.IsNullOrWhiteSpace(districtName)) return null;

        // Lazy load: Nếu chưa có quận của tỉnh này thì mới gọi API
        if (!_ghnDistricts.ContainsKey(provinceId))
        {
            var districts = await _locationService.GetDistrictsAsync(provinceId);
            _ghnDistricts[provinceId] = districts.ToList();
        }

        var match = _ghnDistricts[provinceId].FirstOrDefault(d => IsLocationMatch(d.DistrictName, districtName));
        return match?.DistrictID;
    }

    // 3. Tìm Ward Code (Cần DistrictId cha)
    public async Task<string?> FindWardCodeAsync(int districtId, string wardName)
    {
        if (string.IsNullOrWhiteSpace(wardName)) return null;

        // Lazy load: Nếu chưa có phường của quận này thì mới gọi API
        if (!_ghnWards.ContainsKey(districtId))
        {
            var wards = await _locationService.GetWardsAsync(districtId);
            _ghnWards[districtId] = wards.ToList();
        }

        var match = _ghnWards[districtId].FirstOrDefault(w => IsLocationMatch(w.WardName, wardName));
        return match?.WardCode;
    }

    // --- THUẬT TOÁN SO SÁNH CHUỖI ---
    private bool IsLocationMatch(string ghnName, string inputName)
    {
        if (string.IsNullOrEmpty(ghnName) || string.IsNullOrEmpty(inputName)) return false;

        string Normalize(string s)
        {
            // Chuyển về chữ thường, bỏ khoảng trắng thừa
            s = s.ToLower().Trim();

            // Bỏ dấu tiếng Việt (Optional - nếu muốn chính xác tuyệt đối thì bỏ đoạn này đi)
            // s = RemoveSign4VietnameseString(s); 

            // Loại bỏ các tiền tố hành chính để so sánh nội dung cốt lõi
            string[] prefixes = { "tỉnh", "thành phố", "tp.", "tp ", "quận", "huyện", "thị xã", "phường", "xã", "thị trấn" };
            foreach (var p in prefixes)
            {
                if (s.StartsWith(p))
                {
                    s = s.Substring(p.Length).Trim();
                    break; // Chỉ bỏ tiền tố đầu tiên tìm thấy
                }
            }
            return s;
        }

        return Normalize(ghnName) == Normalize(inputName);
    }
}