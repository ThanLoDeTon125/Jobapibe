using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentApi.Data;
using JobHubPro.Api.Models;
using System.Security.Claims;

namespace JobHubPro.Api.Controllers
{
    [ApiController]
    [Route("api/v1/article-comments")]
    public class ArticleCommentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ArticleCommentsController(AppDbContext context)
        {
            _context = context;
        }

        // Bất kỳ ai đã đăng nhập đều có thể bình luận
        [HttpPost]
        [Authorize] 
        public async Task<IActionResult> Create([FromBody] ArticleComment model)
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
            
            // Ép ID người bình luận là user đang đăng nhập
            model.UserId = userId;
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;

            _context.ArticleComments.Add(model);
            await _context.SaveChangesAsync();
            return Ok(model);
        }

        // Người bình luận hoặc Admin có thể xóa bình luận
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.ArticleComments.FindAsync(id);
            if (item == null) return NotFound();

            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "ADMIN" && item.UserId != userId)
                return Forbid("Bạn không có quyền xóa bình luận này.");

            _context.ArticleComments.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Xóa bình luận thành công." });
        }
    }
}