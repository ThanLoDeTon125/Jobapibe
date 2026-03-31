using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentApi.Data;
using JobHubPro.Api.Models;
using System.Security.Claims;
using JobHubPro.Api.DTOs.Jobs;

namespace JobHubPro.Api.Controllers
{
    [ApiController]
    [Route("api/v1/jobs")]
    public class JobsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public JobsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] string? keyword, [FromQuery] string? location)
        {
            var query = _context.Jobs.Include(x => x.Company).AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(x => x.Title != null && x.Title.Contains(keyword));

            if (!string.IsNullOrEmpty(location))
                query = query.Where(x => x.Location != null && x.Location.Contains(location));

            var data = await query.OrderByDescending(x => x.CreatedAt)
                .Select(j => new {
                    j.Id, j.Title, j.Location, j.SalaryMin, j.SalaryMax, j.EmploymentType, j.CreatedAt,
                    Company = j.Company != null ? new { j.Company.CompanyName, j.Company.LogoUrl } : null
                })
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.Jobs.Include(x => x.Company).FirstOrDefaultAsync(x => x.Id == id);
            if (item == null) return NotFound();
            
            return Ok(new {
                item.Id, item.Title, item.Description, item.Requirements, item.Location, item.SalaryMin, item.SalaryMax, item.EmploymentType, item.ExperienceLevel, item.CreatedAt,
                Company = item.Company != null ? new { item.Company.Id, item.Company.CompanyName, item.Company.LogoUrl, item.Company.Address, item.Company.CompanySize } : null
            });
        }

        // ĐÂY LÀ HÀM BỊ THIẾU GÂY LỖI 404
        [HttpGet("my-jobs")]
        [Authorize(Roles = "EMPLOYER")]
        public async Task<IActionResult> GetMyJobs()
        {
            var claimId = User.FindFirst("id")?.Value ?? User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = !string.IsNullOrEmpty(claimId) ? int.Parse(claimId) : 0;

            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
            
            // Nếu chưa có công ty thì trả về mảng rỗng để web không bị sập
            if (company == null) return Ok(new List<object>());

            var jobs = await _context.Jobs
                .Where(j => j.CompanyId == company.Id)
                .OrderByDescending(j => j.CreatedAt)
                .Select(j => new {
                    j.Id, j.Title, j.Location, j.SalaryMin, j.SalaryMax, j.EmploymentType, j.Status, j.CreatedAt
                })
                .ToListAsync();

            return Ok(jobs);
        }

        [HttpPost]
        [Authorize(Roles = "EMPLOYER, ADMIN")]
        public async Task<IActionResult> Create([FromBody] createJobDto model)
        {
            var claimId = User.FindFirst("id")?.Value ?? User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = !string.IsNullOrEmpty(claimId) ? int.Parse(claimId) : 0;

            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
            if (company == null)
                return BadRequest(new { message = "Bạn cần cập nhật hồ sơ công ty trước khi đăng tin tuyển dụng." });

            // Tự động tạo Slug
            string generatedSlug = (model.Title ?? "job-moi")
                .ToLower()
                .Replace(" ", "-")
                .Replace("đ", "d")
                + "-" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var job = new Job
            {
                CompanyId = company.Id,
                Slug = generatedSlug,
                Title = model.Title ?? "Untitled",
                Description = model.Description ?? "Chưa có mô tả",
                Requirements = model.Requirements ?? "Chưa có yêu cầu",
                Location = model.Location ?? "Chưa xác định",
                SalaryMin = model.SalaryMin ?? 0,
                SalaryMax = model.SalaryMax ?? 0,
                EmploymentType = model.EmploymentType ?? "FULL-TIME",
                ExperienceLevel = model.ExperienceLevel ?? "JUNIOR",
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Tạo tin tuyển dụng thành công", id = job.Id });
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "EMPLOYER, ADMIN")]
        public async Task<IActionResult> Update(int id, [FromBody] updateJobDto model)
        {
            var item = await _context.Jobs.FindAsync(id);
            if (item == null) return NotFound();

            var claimId = User.FindFirst("id")?.Value ?? User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = !string.IsNullOrEmpty(claimId) ? int.Parse(claimId) : 0;

            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "ADMIN" && item.CompanyId != company?.Id)
                return Forbid("Bạn không có quyền sửa tin này.");

            item.Title = model.Title ?? item.Title;
            item.Description = model.Description;
            item.Requirements = model.Requirements;
            item.Location = model.Location;
            item.SalaryMin = model.SalaryMin;
            item.SalaryMax = model.SalaryMax;
            item.EmploymentType = model.EmploymentType;
            item.ExperienceLevel = model.ExperienceLevel;
            if (!string.IsNullOrEmpty(model.Status)) item.Status = model.Status;

            item.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thành công!" });
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "EMPLOYER, ADMIN")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Jobs.FindAsync(id);
            if (item == null) return NotFound();

            var claimId = User.FindFirst("id")?.Value ?? User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = !string.IsNullOrEmpty(claimId) ? int.Parse(claimId) : 0;

            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "ADMIN" && item.CompanyId != company?.Id)
                return Forbid("Bạn không có quyền xóa tin này.");

            _context.Jobs.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Xóa việc làm thành công" });
        }
    }
}