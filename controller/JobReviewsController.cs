using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentApi.Data;
using JobHubPro.Api.Models;
using System.Security.Claims;

namespace JobHubPro.Api.Controllers
{
    [ApiController]
    [Route("api/v1/job-reviews")]
    public class JobReviewsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public JobReviewsController(AppDbContext context)
        {
            _context = context;
        }

        // Xem toàn bộ đánh giá của 1 Job cụ thể
        [HttpGet("job/{jobId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetReviewsByJob(int jobId)
        {
            var reviews = await _context.JobReviews
                .Include(r => r.Candidate)
                .Where(r => r.JobId == jobId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(reviews);
        }
        // DÀNH CHO ADMIN: Lấy tất cả đánh giá trong hệ thống
        [HttpGet]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetAllReviews()
        {
            var reviews = await _context.JobReviews
                .Include(r => r.Candidate)
                .Include(r => r.Job) // Lấy kèm tên công việc
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(reviews);
        }

        // Ứng viên viết đánh giá
        [HttpPost]
        [Authorize(Roles = "CANDIDATE")]
        public async Task<IActionResult> Create([FromBody] JobReview model)
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
            var candidate = await _context.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == userId);

            if (candidate == null) return BadRequest("Bạn cần tạo hồ sơ ứng viên trước.");

            // Ép ID người đánh giá là ứng viên đang đăng nhập
            model.CandidateId = candidate.Id;
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;

            _context.JobReviews.Add(model);
            await _context.SaveChangesAsync();
            return Ok(model);
        }

        // Xóa đánh giá (Chỉ Admin hoặc chính ứng viên đó mới được xóa)
        [HttpDelete("{id}")]
        [Authorize(Roles = "ADMIN, CANDIDATE")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.JobReviews.FindAsync(id);
            if (item == null) return NotFound();

            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
            var candidate = await _context.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == userId);
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "ADMIN" && item.CandidateId != candidate?.Id)
                return Forbid("Bạn không có quyền xóa đánh giá này.");

            _context.JobReviews.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Xóa đánh giá thành công." });
        }
    }
}