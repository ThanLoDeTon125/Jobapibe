using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentApi.Data;
using JobHubPro.Api.Models;
using JobHubPro.Api.DTOs.Applications; // <-- Đã gọi chung thư mục chứa 2 file DTO
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

        [HttpGet("my-applications")]
        [Authorize(Roles = "CANDIDATE")]
        public async Task<IActionResult> GetMyApplications()
        {
            var claimId = User.FindFirst("id")?.Value ?? User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = !string.IsNullOrEmpty(claimId) ? int.Parse(claimId) : 0;
            
            var candidate = await _context.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == userId);
            
            if (candidate == null) return Ok(new List<object>());

            var data = await _context.Applications
                .Include(a => a.Job)
                    .ThenInclude(j => j.Company) 
                .Where(a => a.CandidateId == candidate.Id)
                .OrderByDescending(a => a.AppliedAt)
                .Select(a => new {
                    a.Id,
                    a.Status,
                    a.AppliedAt,
                    Job = new {
                        a.Job.Id,
                        a.Job.Title,
                        a.Job.Location,
                        Company = a.Job.Company != null ? new {
                            a.Job.Company.CompanyName,
                            a.Job.Company.LogoUrl
                        } : null
                    }
                })
                .ToListAsync();

            return Ok(data);
        }

        [HttpPost]
        [Authorize(Roles = "CANDIDATE")]
        public async Task<IActionResult> Create([FromBody] CreateApplicationDto dto)
        {
            var claimId = User.FindFirst("id")?.Value ?? User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = !string.IsNullOrEmpty(claimId) ? int.Parse(claimId) : 0;
            
            var candidate = await _context.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == userId);
            
            if (candidate == null) 
                return BadRequest(new { message = "Vui lòng cập nhật Hồ sơ cá nhân trước khi ứng tuyển!" });

            if (candidate.Id == 0)
                return BadRequest(new { message = "Hồ sơ của bạn bị lỗi dữ liệu (ID = 0). Vui lòng thử lưu lại hồ sơ cá nhân!" });

            var exists = await _context.Applications
                .AnyAsync(a => a.JobId == dto.JobId && a.CandidateId == candidate.Id);
            if (exists)
                return Conflict(new { message = "Bạn đã ứng tuyển vào công việc này rồi." });

            var model = new Application
            {
                JobId = dto.JobId,
                CandidateId = candidate.Id,
                Status = "NEW", 
                AppliedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Applications.Add(model);

            var fkProperty = _context.Model.FindEntityType(typeof(Application))
                ?.GetForeignKeys()
                .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(CandidateProfile))
                ?.Properties.FirstOrDefault()?.Name;

            if (!string.IsNullOrEmpty(fkProperty) && fkProperty != "CandidateId")
            {
                _context.Entry(model).Property(fkProperty).CurrentValue = candidate.Id;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Ứng tuyển thành công!" });
        }

        // ==========================================
        // DÀNH CHO NHÀ TUYỂN DỤNG (EMPLOYER) & ADMIN
        // ==========================================

        [HttpGet("job/{jobId:int}")]
        [Authorize(Roles = "EMPLOYER, ADMIN")]
        public async Task<IActionResult> GetApplicationsByJob(int jobId)
        {
            var claimId = User.FindFirst("id")?.Value ?? User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = !string.IsNullOrEmpty(claimId) ? int.Parse(claimId) : 0;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var job = await _context.Jobs.FindAsync(jobId);
            if (job == null) return NotFound(new { message = "Không tìm thấy công việc." });

            if (userRole != "ADMIN")
            {
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
                if (company == null || job.CompanyId != company.Id)
                    return Forbid(); 
            }

            var applications = await _context.Applications
                .Include(a => a.Candidate)
                    .ThenInclude(c => c.User) 
                .Where(a => a.JobId == jobId)
                .OrderByDescending(a => a.AppliedAt)
                .Select(a => new {
                    a.Id,
                    a.Status,
                    a.AppliedAt,
                    Candidate = a.Candidate != null ? new {
                        a.Candidate.Id,
                        a.Candidate.FullName,
                        a.Candidate.Phone,
                        a.Candidate.CvUrl,
                        a.Candidate.ExperienceYears,
                        Email = a.Candidate.User != null ? a.Candidate.User.Email : "Chưa có email"
                    } : null
                })
                .ToListAsync();

            return Ok(applications);
        }

        [HttpPut("{id:int}/status")]
        [Authorize(Roles = "EMPLOYER, ADMIN")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            var application = await _context.Applications
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null) return NotFound();

            var claimId = User.FindFirst("id")?.Value ?? User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = !string.IsNullOrEmpty(claimId) ? int.Parse(claimId) : 0;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "ADMIN")
            {
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
                if (company == null || application.Job.CompanyId != company.Id)
                    return Forbid();
            }

            var validStatuses = new[] { "NEW", "REVIEWING", "INTERVIEWING", "OFFERED", "HIRED", "REJECTED" };
            var requestedStatus = (dto.Status ?? "").ToUpper();

            if (!validStatuses.Contains(requestedStatus))
                return BadRequest(new { message = $"Trạng thái không hợp lệ. Chỉ chấp nhận: {string.Join(", ", validStatuses)}" });

            application.Status = requestedStatus;
            application.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Cập nhật trạng thái thành {requestedStatus} thành công!" });
        }
    }
}