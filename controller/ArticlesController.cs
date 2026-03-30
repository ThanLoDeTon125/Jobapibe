using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentApi.Data; // Đổi lại namespace nếu AppDbContext của bạn ở chỗ khác
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

        // Ai cũng xem được danh sách tin tức
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] string? keyword)
        {
            var query = _context.Articles.Include(a => a.Author).AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(a => a.Title.Contains(keyword));

            var data = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
            return Ok(data);
        }

        // Xem chi tiết 1 bài viết (Kèm theo danh sách bình luận)
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var article = await _context.Articles
                .Include(a => a.Author)
                .Include(a => a.ArticleComments)
                    .ThenInclude(c => c.User) // Lấy thông tin người bình luận
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null) return NotFound();
            return Ok(article);
        }

        // Chỉ Admin hoặc Nhà tuyển dụng mới được đăng tin
        [HttpPost]
        [Authorize(Roles = "ADMIN, EMPLOYER")]
        public async Task<IActionResult> Create([FromBody] Article model)
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
            
            model.AuthorId = userId; // Tự động gán người viết là user đang đăng nhập
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;

            _context.Articles.Add(model);
            await _context.SaveChangesAsync();
            return Ok(model);
        }

        // Sửa tin tức (Chỉ người viết hoặc Admin mới được sửa)
        [HttpPut("{id}")]
        [Authorize(Roles = "ADMIN, EMPLOYER")]
        public async Task<IActionResult> Update(int id, [FromBody] Article model)
        {
            var item = await _context.Articles.FindAsync(id);
            if (item == null) return NotFound();

            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "ADMIN" && item.AuthorId != userId)
                return Forbid("Bạn không có quyền sửa bài viết này.");

            item.Title = model.Title;
            item.Content = model.Content;
            item.ThumbnailUrl = model.ThumbnailUrl;
            item.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(item);
        }

        // Xóa tin tức
        [HttpDelete("{id}")]
        [Authorize(Roles = "ADMIN, EMPLOYER")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Articles.FindAsync(id);
            if (item == null) return NotFound();

            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "ADMIN" && item.AuthorId != userId)
                return Forbid("Bạn không có quyền xóa bài viết này.");

            _context.Articles.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Xóa bài viết thành công." });
        }
    }
}