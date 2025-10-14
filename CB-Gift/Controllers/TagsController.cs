using CB_Gift.DTOs;
using CB_Gift.Services;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class TagsController : ControllerBase
{
    private readonly ITagService _tagService;

    public TagsController(ITagService tagService)
    {
        _tagService = tagService;
    }

    // GET: api/Tags
    [HttpGet]
    public async Task<IActionResult> GetAllTags()
    {
        var tags = await _tagService.GetAllTagsAsync();
        return Ok(tags);
    }

    // GET: api/Tags/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTagById(int id)
    {
        var tag = await _tagService.GetTagByIdAsync(id);
        if (tag == null) return NotFound();
        return Ok(tag);
    }

    // POST: api/Tags
    [HttpPost]
    public async Task<IActionResult> CreateTag([FromBody] CreateTagDto createTagDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var newTag = await _tagService.CreateTagAsync(createTagDto);
        return CreatedAtAction(nameof(GetTagById), new { id = newTag.TagsId }, newTag);
    }

    // PUT: api/Tags/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTag(int id, [FromBody] UpdateTagDto updateTagDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var success = await _tagService.UpdateTagAsync(id, updateTagDto);
        if (!success) return NotFound();
        return NoContent();
    }

    // DELETE: api/Tags/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTag(int id)
    {
        var success = await _tagService.DeleteTagAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }
}