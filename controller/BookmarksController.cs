using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentApi.Data;
using JobHubPro.Api.Models;
using System.Security.Claims;

namespace JobHubPro.Api.Controllers
{
    [ApiController]
    [Route("api/v1/bookmarks")] // Chuẩn hóa URL
    public class BookmarksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BookmarksController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "CANDIDATE")] // Chỉ ứng viên
        public async Task<IActionResult> GetAll()
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
            var candidate = await _context.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == userId);
            
            if (candidate == null) return Ok(new List<Bookmark>());

            var data = await _context.Bookmarks
                .Include(x => x.Job)
                    .ThenInclude(j => j.Company)
                .Where(x => x.CandidateId == candidate.Id)
                .ToListAsync();

            return Ok(data);
        }

        [HttpPost]
        [Authorize(Roles = "CANDIDATE")]
        public async Task<IActionResult> Create([FromBody] Bookmark model)
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
            var candidate = await _context.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == userId);
            
            if (candidate == null) return BadRequest("Vui lòng cập nhật hồ sơ ứng viên.");

            var exists = await _context.Bookmarks
                .AnyAsync(b => b.CandidateId == candidate.Id && b.JobId == model.JobId);
            
            if (exists) return Conflict(new { message = "Việc làm đã được lưu trước đó." });

            // Tự động gán ID của ứng viên
            model.CandidateId = candidate.Id;
            model.CreatedAt = DateTime.UtcNow;
            
            _context.Bookmarks.Add(model);
            await _context.SaveChangesAsync();
            return Ok(model);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "CANDIDATE")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
            var candidate = await _context.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == userId);
            
            var item = await _context.Bookmarks.FindAsync(id);
            if (item == null) return NotFound();

            // Kiểm tra bảo mật: Không cho xóa bookmark của người khác
            if (candidate == null || item.CandidateId != candidate.Id)
                return Forbid("Bạn không có quyền xóa mục này.");

            _context.Bookmarks.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã bỏ lưu việc làm." });
        }
    }
}