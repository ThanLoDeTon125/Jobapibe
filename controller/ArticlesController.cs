using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentApi.Data;
using JobHubPro.Api.Models;
using System.Security.Claims;

namespace JobHubPro.Api.Controllers
{
    [ApiController]
    [Route("api/v1/articles")]
    public class ArticlesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ArticlesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] string? keyword)
        {
            var query = _context.Articles.Include(a => a.Author).AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(a => a.Title.Contains(keyword));

            var data = await query.OrderByDescending(a => a.CreatedAt)
                .Select(a => new {
                    a.Id,
                    a.Title,
                    a.Content,
                    a.ThumbnailUrl,
                    a.CreatedAt,
                    AuthorId = a.AuthorId,
                    // Check null tại đây
                    Author = a.Author != null ? new { Email = a.Author.Email } : null 
                })
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var article = await _context.Articles
                .Include(a => a.Author)
                .Include(a => a.ArticleComments)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null) return NotFound();

            return Ok(new {
                article.Id,
                article.Title,
                article.Content,
                article.ThumbnailUrl,
                article.CreatedAt,
                AuthorId = article.AuthorId,
                // Check null tại đây
                Author = article.Author != null ? new { Email = article.Author.Email } : null,
                ArticleComments = article.ArticleComments.Select(c => new {
                    c.Id,
                    c.Content,
                    c.CreatedAt,
                    // Check null tại đây
                    User = c.User != null ? new { Email = c.User.Email } : null
                })
            });
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN, EMPLOYER")]
        public async Task<IActionResult> Create([FromBody] Article model)
        {
            var claimId = User.FindFirst("id")?.Value 
                       ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                       ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(claimId) || !int.TryParse(claimId, out int userId))
                return Unauthorized(new { message = "Không thể xác định danh tính tác giả." });

            model.AuthorId = userId;
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;

            _context.Articles.Add(model);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Tạo bài viết thành công!", id = model.Id });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "ADMIN, EMPLOYER")]
        public async Task<IActionResult> Update(int id, [FromBody] Article model)
        {
            var item = await _context.Articles.FindAsync(id);
            if (item == null) return NotFound();

            var claimId = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            int userId = !string.IsNullOrEmpty(claimId) ? int.Parse(claimId) : 0;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "ADMIN" && item.AuthorId != userId)
                return Forbid("Bạn không có quyền sửa bài viết này.");

            item.Title = model.Title;
            item.Content = model.Content;
            item.ThumbnailUrl = model.ThumbnailUrl;
            item.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thành công!" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "ADMIN, EMPLOYER")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Articles.FindAsync(id);
            if (item == null) return NotFound();

            var claimId = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            int userId = !string.IsNullOrEmpty(claimId) ? int.Parse(claimId) : 0;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "ADMIN" && item.AuthorId != userId)
                return Forbid("Bạn không có quyền xóa bài viết này.");

            _context.Articles.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa bài viết." });
        }
    }
}