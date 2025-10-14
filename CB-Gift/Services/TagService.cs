using AutoMapper;
using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Models;
using CB_Gift.Services.IService;
using Microsoft.EntityFrameworkCore;

namespace CB_Gift.Services
{
    public class TagService : ITagService
    {
        private readonly CBGiftDbContext _context;
        private readonly IMapper _mapper;

        public TagService(CBGiftDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        #region Tag CRUD
        public async Task<TagDto> CreateTagAsync(CreateTagDto createTagDto)
        {
            var existingTag = await _context.Tags
                .FirstOrDefaultAsync(t => t.TagCode.ToLower() == createTagDto.TagCode.ToLower());
            if (existingTag != null)
            {
                throw new Exception("Mã tag này đã tồn tại.");
            }

            var tag = _mapper.Map<Tag>(createTagDto);
            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();
            return _mapper.Map<TagDto>(tag);
        }

        public async Task<bool> DeleteTagAsync(int tagId)
        {
            var tag = await _context.Tags.FindAsync(tagId);
            if (tag == null) return false;

            _context.Tags.Remove(tag);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<TagDto>> GetAllTagsAsync()
        {
            var tags = await _context.Tags.AsNoTracking().ToListAsync();
            return _mapper.Map<IEnumerable<TagDto>>(tags);
        }

        public async Task<TagDto> GetTagByIdAsync(int tagId)
        {
            var tag = await _context.Tags.AsNoTracking().FirstOrDefaultAsync(t => t.TagsId == tagId);
            return _mapper.Map<TagDto>(tag);
        }

        public async Task<bool> UpdateTagAsync(int tagId, UpdateTagDto updateTagDto)
        {
            var tag = await _context.Tags.FindAsync(tagId);
            if (tag == null) return false;

            _mapper.Map(updateTagDto, tag);
            await _context.SaveChangesAsync();
            return true;
        }
        #endregion

        #region Relationship Management
        public async Task<bool> AddTagToProductAsync(int productId, int tagId)
        {
            var product = await _context.Products
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            var tag = await _context.Tags.FindAsync(tagId);

            if (product == null || tag == null) return false;

            // Kiểm tra xem tag đã tồn tại trong sản phẩm chưa
            if (!product.Tags.Any(t => t.TagsId == tagId))
            {
                product.Tags.Add(tag);
                await _context.SaveChangesAsync();
            }
            return true;
        }

        public async Task<bool> RemoveTagFromProductAsync(int productId, int tagId)
        {
            var product = await _context.Products
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null) return false;

            var tagToRemove = product.Tags.FirstOrDefault(t => t.TagsId == tagId);
            if (tagToRemove != null)
            {
                product.Tags.Remove(tagToRemove);
                await _context.SaveChangesAsync();
            }
            return true;
        }

        public async Task<IEnumerable<TagDto>> GetTagsByProductIdAsync(int productId)
        {
            var product = await _context.Products
                .Include(p => p.Tags)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null) return null;

            return _mapper.Map<IEnumerable<TagDto>>(product.Tags);
        }
        #endregion
    }
}
