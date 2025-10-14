using CB_Gift.DTOs;

namespace CB_Gift.Services.IService
{
    public interface ITagService
    {
        // CRUD operations for Tags
        Task<IEnumerable<TagDto>> GetAllTagsAsync();
        Task<TagDto> GetTagByIdAsync(int tagId);
        Task<TagDto> CreateTagAsync(CreateTagDto createTagDto);
        Task<bool> UpdateTagAsync(int tagId, UpdateTagDto updateTagDto);
        Task<bool> DeleteTagAsync(int tagId);

        // Relationship management
        Task<bool> AddTagToProductAsync(int productId, int tagId);
        Task<bool> RemoveTagFromProductAsync(int productId, int tagId);
        Task<IEnumerable<TagDto>> GetTagsByProductIdAsync(int productId);
    }
}
