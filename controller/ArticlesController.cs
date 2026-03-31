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

        // Lấy danh sách tin tức (Fix lỗi vòng lặp JSON bằng .Select)
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
                    // Chỉ lấy đúng Email của tác giả, cắt đứt vòng lặp
                    Author = new { Email = a.Author.Email } 
                })
                .ToListAsync();

            return Ok(data);
        }

        // Xem chi tiết bài viết (Kèm comment)
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
                Author = new { Email = article.Author.Email },
                ArticleComments = article.ArticleComments.Select(c => new {
                    c.Id,
                    c.Content,
                    c.CreatedAt,
                    User = new { Email = c.User.Email }
                })
            });
        }

        // Viết bài mới (Fix lỗi không lấy được ID tác giả)
        [HttpPost]
        [Authorize(Roles = "ADMIN, EMPLOYER")]
        public async Task<IActionResult> Create([FromBody] Article model)
        {
            // Bắt trọn mọi loại định dạng ID từ JWT Token
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
            
            // Trả về Object thông báo thay vì toàn bộ Model để tránh lỗi Serialization
            return Ok(new { message = "Tạo bài viết thành công!", id = model.Id });
        }

        // Sửa bài viết
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

        // Xóa bài viết
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