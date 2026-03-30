using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentApi.Data;
using JobHubPro.Api.Models;
using JobHubPro.Api.DTOs.Applications;
using System.Security.Claims;

namespace JobHubPro.Api.Controllers
{
    [ApiController]
    [Route("api/v1/applications")]
    public class ApplicationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ApplicationsController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // DÀNH CHO ỨNG VIÊN (CANDIDATE)
        // ==========================================

        // Lấy danh sách các công việc MÌNH ĐÃ NỘP
        [HttpGet("my-applications")]
        [Authorize(Roles = "CANDIDATE")]
        public async Task<IActionResult> GetMyApplications()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            // Tìm Profile ứng viên
            var candidate = await _context.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == userId);
            if (candidate == null) return BadRequest("Không tìm thấy hồ sơ ứng viên.");

            var data = await _context.Applications
                .Include(a => a.Job)
                    .ThenInclude(j => j.Company) // Lấy luôn tên công ty để hiển thị
                .Where(a => a.CandidateId == candidate.Id)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            return Ok(data);
        }

        // Ứng viên nộp đơn mới
        [HttpPost]
        [Authorize(Roles = "CANDIDATE")]
        public async Task<IActionResult> Create(Application model)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var candidate = await _context.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == userId);
            
            if (candidate == null) 
                return BadRequest("Vui lòng cập nhật hồ sơ cá nhân trước khi ứng tuyển.");

            // Kiểm tra xem ứng viên đã nộp vào job này chưa (Tránh nộp trùng 2 lần)
            var exists = await _context.Applications
                .AnyAsync(a => a.JobId == model.JobId && a.CandidateId == candidate.Id);
            if (exists)
                return Conflict(new { message = "Bạn đã ứng tuyển vào công việc này rồi." });

            model.CandidateId = candidate.Id;
            model.Status = "NEW"; // Trạng thái mặc định ban đầu của ATS
            model.AppliedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;

            _context.Applications.Add(model);
            await _context.SaveChangesAsync();
            return Ok(model);
        }

        // ==========================================
        // DÀNH CHO NHÀ TUYỂN DỤNG (EMPLOYER) & ADMIN
        // ==========================================

        // HR lấy danh sách ứng viên nộp vào MỘT CÔNG VIỆC CỤ THỂ của công ty họ
        [HttpGet("job/{jobId}")]
        [Authorize(Roles = "EMPLOYER, ADMIN")]
        public async Task<IActionResult> GetApplicationsByJob(int jobId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Kiểm tra Job này có tồn tại và có thuộc về Công ty của HR này không
            var job = await _context.Jobs.FindAsync(jobId);
            if (job == null) return NotFound("Không tìm thấy công việc.");

            if (userRole != "ADMIN")
            {
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
                if (company == null || job.CompanyId != company.Id)
                    return Forbid("Bạn không có quyền xem đơn ứng tuyển của công việc này.");
            }

            var applications = await _context.Applications
                .Include(a => a.Candidate) // Lấy thông tin ứng viên
                .Where(a => a.JobId == jobId)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            return Ok(applications);
        }

        // HR cập nhật trạng thái đơn (Kéo thả ứng viên qua các cột NEW -> REVIEWING -> INTERVIEWING...)
        [HttpPut("{id}/status")]
        [Authorize(Roles = "EMPLOYER, ADMIN")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            var application = await _context.Applications
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null) return NotFound();

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Bảo mật: Chỉ người tạo ra Job đó mới được quyền duyệt đơn
            if (userRole != "ADMIN")
            {
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
                if (company == null || application.Job.CompanyId != company.Id)
                    return Forbid("Bạn không có quyền duyệt đơn ứng tuyển này.");
            }

            // Danh sách các trạng thái hợp lệ của ATS
            var validStatuses = new[] { "NEW", "REVIEWING", "INTERVIEWING", "OFFERED", "HIRED", "REJECTED" };
            var requestedStatus = dto.Status.ToUpper();

            if (!validStatuses.Contains(requestedStatus))
                return BadRequest($"Trạng thái không hợp lệ. Chỉ chấp nhận: {string.Join(", ", validStatuses)}");

            application.Status = requestedStatus;
            application.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Cập nhật trạng thái thành {requestedStatus} thành công!", application });
        }
    }
}