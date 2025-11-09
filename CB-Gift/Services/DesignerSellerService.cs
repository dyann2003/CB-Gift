using AutoMapper; 
using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Models;
using CB_Gift.Services.IService;
using Microsoft.EntityFrameworkCore;

namespace CB_Gift.Services
{
    public class DesignerSellerService : IDesignerSellerService
    {
        private readonly CBGiftDbContext _context;
        private readonly IMapper _mapper;

        public DesignerSellerService(CBGiftDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<bool> AssignDesignerToSellerAsync(AssignDesignerDto dto, string managerId)
        {
            var existingAssignment = await _context.DesignerSellers
                .AnyAsync(ds => ds.DesignerUserId == dto.DesignerUserId && ds.SellerUserId == dto.SellerUserId);

            if (existingAssignment)
            {
                // Mối quan hệ đã tồn tại, không cần tạo mới
                return true;
            }

            var assignment = new DesignerSeller
            {
                DesignerUserId = dto.DesignerUserId,
                SellerUserId = dto.SellerUserId,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = managerId
            };

            _context.DesignerSellers.Add(assignment);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveDesignerFromSellerAsync(AssignDesignerDto dto)
        {
            var assignment = await _context.DesignerSellers
                .FirstOrDefaultAsync(ds => ds.DesignerUserId == dto.DesignerUserId && ds.SellerUserId == dto.SellerUserId);

            if (assignment == null)
            {
                return false; // Không tìm thấy để xóa
            }

            _context.DesignerSellers.Remove(assignment);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<DesignerSellerDto>> GetDesignersForSellerAsync(string sellerId)
        {
            var assignments = await _context.DesignerSellers
         .Where(ds => ds.SellerUserId == sellerId)
         .Include(ds => ds.DesignerUser) // Tải dữ liệu của Designer User
         .Include(ds => ds.SellerUser)   // Tải dữ liệu của Seller User
         .AsNoTracking()
         .ToListAsync();
            return _mapper.Map<IEnumerable<DesignerSellerDto>>(assignments);
        }

        public async Task<IEnumerable<DesignerSellerDto>> GetSellersForDesignerAsync(string designerId)
        {
            var assignments = await _context.DesignerSellers
               .Where(ds => ds.DesignerUserId == designerId)
               .AsNoTracking()
               .ToListAsync();
            return _mapper.Map<IEnumerable<DesignerSellerDto>>(assignments);
        }

        public async Task<IEnumerable<SellerDesignerPairDto>> GetAllAssignmentsAsync()
        {
            var pairs = await _context.DesignerSellers
                .Join(_context.Users, ds => ds.SellerUserId, s => s.Id, (ds, s) => new { ds, s })
                .Join(_context.Users, combined => combined.ds.DesignerUserId, d => d.Id, (combined, d) => new SellerDesignerPairDto
                {
                    SellerId = combined.s.Id,
                    SellerName = combined.s.FullName,
                    DesignerId = d.Id,
                    DesignerName = d.FullName
                })
                .ToListAsync();

            return pairs;
        }

        public async Task<(IEnumerable<SellerDesignerPairDto> Data, int Total)> GetAllAssignmentsPagedAsync(
    string? search, string? sellerName, int page, int pageSize)
        {
            var query = _context.DesignerSellers
                .Include(x => x.SellerUser)
                .Include(x => x.DesignerUser)
                .AsNoTracking()
                .AsQueryable();

            // ✅ Lọc theo seller name
            if (!string.IsNullOrWhiteSpace(sellerName))
            {
                var keyword = sellerName.Trim().ToLower();
                query = query.Where(x => x.SellerUser.FullName.ToLower().Contains(keyword));
            }

            // ✅ Lọc hoặc tìm kiếm chung (có thể tìm theo designer hoặc seller name)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim().ToLower();
                query = query.Where(x =>
                    x.SellerUser.FullName.ToLower().Contains(keyword) ||
                    x.DesignerUser.FullName.ToLower().Contains(keyword));
            }

            // ✅ Tổng số bản ghi
            var total = await query.CountAsync();

            // ✅ Phân trang
            var skip = (page - 1) * pageSize;

            var result = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .Select(x => new SellerDesignerPairDto
                {
                    SellerId = x.SellerUserId,
                    SellerName = x.SellerUser.FullName,
                    DesignerId = x.DesignerUserId,
                    DesignerName = x.DesignerUser.FullName
                })
                .ToListAsync();

            return (result, total);
        }

 




    }
}