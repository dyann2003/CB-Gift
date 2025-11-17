using CB_Gift.DTOs;

namespace CB_Gift.Services.IService
{
    public interface ILocationService
    {
        Task<IEnumerable<GhnProvince>> GetProvincesAsync();
        Task<IEnumerable<GhnDistrict>> GetDistrictsAsync(int provinceId);
        Task<IEnumerable<GhnWard>> GetWardsAsync(int districtId);
    }
}
