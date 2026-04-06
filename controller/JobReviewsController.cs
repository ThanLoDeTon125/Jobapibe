using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentApi.Data;
using JobHubPro.Api.Models;
using JobHubPro.Api.DTOs.JobReviews;
using System.Security.Claims;

namespace JobHubPro.Api.Controllers
{
    [ApiController]
    [Route("api/v1/jobreviews")]
    public class JobReviewsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public JobReviewsController(AppDbContext context)
        {
            _context = context;
        }

        // 1. TẢI DANH SÁCH BÌNH LUẬN (ĐÃ ĐỔ BÊ TÔNG CHỐNG SẬP REACT)
        [HttpGet("job/{jobId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByJobId(int jobId)
        {
            var reviews = await _context.JobReviews
                .Include(r => r.Candidate)
                    .ThenInclude(c => c.User)
                .Where(r => r.JobId == jobId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new {
                    r.Id,
                    r.JobId,
                    r.Rating,
                    r.Comment,
                    r.CreatedAt,
                    // 🚀 CHỐT CHẶN BÊ TÔNG: Không bao giờ trả về null, luôn trả ra một Object
                    User = new { 
                        Id = r.Candidate != null ? r.Candidate.Id : 0, 
                        FullName = (r.Candidate != null && !string.IsNullOrEmpty(r.Candidate.FullName)) ? r.Candidate.FullName : "Người dùng ẩn danh",
                        Email = (r.Candidate != null && r.Candidate.User != null) ? r.Candidate.User.Email : "Chưa có email",
                        Role = "CANDIDATE" // Thêm role để Frontend không bị bỡ ngỡ
                    }
                })
                .ToListAsync();

            return Ok(reviews);
        }

        // 1.5. API DÀNH CHO ADMIN: Lấy TẤT CẢ bình luận trên hệ thống
        [HttpGet]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetAllForAdmin()
        {
            var reviews = await _context.JobReviews
                .Include(r => r.Job)
                .Include(r => r.Candidate)
                    .ThenInclude(c => c.User)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new {
                    r.Id,
                    r.Rating,
                    r.Comment,
                    r.CreatedAt,
                    JobTitle = r.Job != null ? r.Job.Title : "Công việc đã bị xóa",
                    User = new { 
                        FullName = (r.Candidate != null && !string.IsNullOrEmpty(r.Candidate.FullName)) ? r.Candidate.FullName : "Người dùng ẩn danh",
                        Email = (r.Candidate != null && r.Candidate.User != null) ? r.Candidate.User.Email : "Chưa có email"
                    }
                })
                .ToListAsync();

            return Ok(reviews);
        }

        // 2. ĐĂNG BÌNH LUẬN MỚI
        [HttpPost]
        [Authorize(Roles = "CANDIDATE")] 
        public async Task<IActionResult> Create([FromBody] CreateJobReviewDto dto)
        {
            var claimId = User.FindFirst("id")?.Value ?? User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = !string.IsNullOrEmpty(claimId) ? int.Parse(claimId) : 0;

            var candidate = await _context.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == userId);
            
            if (candidate == null)
                return BadRequest(new { message = "Bạn cần cập nhật Hồ sơ cá nhân trước khi có thể bình luận." });

            var review = new JobReview
            {
                JobId = dto.JobId,
                CandidateId = candidate.Id,
                Rating = dto.Rating ?? 5,
                Comment = dto.Comment ?? "",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.JobReviews.Add(review);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Bình luận thành công!", id = review.Id });
        }

        // 3. XÓA BÌNH LUẬN
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "CANDIDATE, ADMIN")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.JobReviews.FindAsync(id);
            if (item == null) return NotFound();

            var claimId = User.FindFirst("id")?.Value ?? User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = !string.IsNullOrEmpty(claimId) ? int.Parse(claimId) : 0;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var candidate = await _context.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == userId);

            if (userRole != "ADMIN" && (candidate == null || item.CandidateId != candidate.Id))
                return Forbid();

            _context.JobReviews.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Xóa bình luận thành công!" });
        }
    }
}